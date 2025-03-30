using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using CandyCoded.HapticFeedback;

public class PlayerController : MonoBehaviour
{
    [Header("Ant Movement Settings")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 10f;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Animation")]
    [SerializeField] private AnimationController animController;
    
    [Header("Combat")]
    [SerializeField] private LivingEntity livingEntity;
    private bool autoAttackEnabled = true; // Flag to enable/disable auto-attack
    private Coroutine autoAttackCoroutine; // Reference to auto-attack coroutine
    private EntityHitbox playerHitbox; // Reference to player's hitbox

    [Header("UI Helper")]
    [SerializeField] public UIHelper uiHelper;

    [Header("Mobile Controls")]
    [SerializeField] private OnScreenJoystick joystick;

    [Header("Border Settings")]
    [SerializeField] private bool showBorderMessages = true;
    [SerializeField] private float borderMessageCooldown = 0.5f;
    private float lastBorderMessageTime = 0f;
    // Add this to track collision with map borders
    private bool isAtMapBorder = false;
    // Track the normal direction of the boundary
    private Vector3 borderNormal = Vector3.zero;

    // Movement variables
    private Vector3 moveDirection;
    private float currentSpeed;
    private Transform bodyTransform; // Optional: if you have a separate body mesh

    public Transform spawnPoint;
    
    // Add this variable to track if we're already showing a boundary message
    private bool isShowingBoundaryMessage = false;

    // Add this field to track enemies in range
    private List<LivingEntity> enemiesInRange = new List<LivingEntity>();

    // Add these debug variables
    [Header("Auto Attack")]
    [SerializeField] private bool showDebugMessages = true;

    [Header("Border Level Requirements")]
    [SerializeField] private PlayerInventory playerInventory; // To check player's level

    [Header("Level Area Arrow Manager")]
    [SerializeField] private LevelAreaArrowManager levelAreaArrowManager;

    [Header("Auto-Facing")]
    [SerializeField] private bool isAutoFacing = false; // Set this in the inspector if needed
    private Transform targetToFace;
    private bool isMoving = false;

    [Header("Metamorphosis Settings")]
    [SerializeField] private bool enableMetamorphosis = true;
    [SerializeField] private float[] metamorphosisLevels = new float[] { 2f, 7f, 12f, 17f, 22f, 27f, 32f, 37f, 42f, 47f, 52f };
    [SerializeField] private float scaleIncreaseMultiplier = 1.1f; // Each metamorphosis increases size by 10%
    private Vector3 baseScale;
    private int currentMetamorphosisStage = 0;

    // Add this field to track when player controls should be disabled
    private bool isControlsDisabled = false;

    // Add this field to track if the scale is being initialized
    private bool isInitializingScale = false;

    // Add this new field to PlayerController class
    private bool isInitializing = true;

    private void Start()
    {
        // If no camera is assigned, try to find the main camera
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
            
        // Optional: If you have a separate body mesh transform
        bodyTransform = transform.Find("Body");
        if (bodyTransform == null)
            bodyTransform = transform; // Use main transform if no body found
        
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
        
        // Subscribe to death event if not already done
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(HandlePlayerDeath);
        }
        
        // Get the player's hitbox and subscribe to enemy detection events
        playerHitbox = GetComponentInChildren<EntityHitbox>();
        if (playerHitbox != null)
        {
            playerHitbox.OnPlayerEnterHitbox += HandleEnemyDetected;
            playerHitbox.OnPlayerExitHitbox += HandleEnemyLost;
            
            if (showDebugMessages)
            {
                Debug.Log("Successfully subscribed to hitbox events");
            }
        }
        else
        {
            Debug.LogError("Player hitbox not found. Auto-attack functionality will not work.");
            
            // Try to find the hitbox in a different way
            EntityHitbox[] hitboxes = GetComponentsInChildren<EntityHitbox>(true);
            if (hitboxes.Length > 0)
            {
                playerHitbox = hitboxes[0];
                playerHitbox.OnPlayerEnterHitbox += HandleEnemyDetected;
                playerHitbox.OnPlayerExitHitbox += HandleEnemyLost;
                Debug.Log("Found hitbox through alternative method");
            }
        }
        
        // Start a periodic check for enemies in range
        StartCoroutine(CheckForEnemiesInRange());
        
        // Make sure auto-attack is enabled by default
        autoAttackEnabled = true;

        // Check if playerInventory is null and try to find it
        if (playerInventory == null)
        {
            playerInventory = GetComponent<PlayerInventory>();
            if (playerInventory == null)
            {
                playerInventory = FindObjectOfType<PlayerInventory>();
                if (playerInventory != null)
                {
                    playerInventory.OnLevelUp += OnPlayerLevelUp;
                    Debug.Log("Found PlayerInventory through FindObjectOfType");
                }
                else
                {
                    Debug.LogError("PlayerInventory not found! Metamorphosis won't work.");
                }
            }
        }
        
        // Store the original scale when the game starts and log it
        baseScale = transform.localScale;
        Debug.Log($"Initial base scale set to: {baseScale}");
        
        // Initialize borders after a short delay
        Invoke("DisableBordersForCurrentLevel", 1.0f);
        
        // Ensure proper initialization of player size and camera
        if (playerInventory != null)
        {
            // Call immediate initialization (important!)
            EnsureCorrectSizeAndCamera();
            
            // Schedule a double-check after a short delay to ensure settings are applied
            // This helps when other systems might be initializing in parallel
            Invoke("EnsureCorrectSizeAndCamera", 0.5f);
        }
        else
        {
            Debug.LogError("Cannot initialize player size and camera: playerInventory is null");
        }

        // Set the initialization flag to true when game starts
        isInitializing = true;
        
        // Schedule turning off initialization flag after a short delay
        Invoke("CompleteInitialization", 2.0f);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandlePlayerDeath);
        }
        
        // Unsubscribe from hitbox events
        if (playerHitbox != null)
        {
            playerHitbox.OnPlayerEnterHitbox -= HandleEnemyDetected;
            playerHitbox.OnPlayerExitHitbox -= HandleEnemyLost;
        }
        
        // Stop auto-attack coroutine if it's running
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }

        // Unsubscribe from level up event
        if (playerInventory != null)
        {
            playerInventory.OnLevelUp -= OnPlayerLevelUp;
        }
    }
    
    private void HandlePlayerDeath()
    {
        enabled = false;
        
        // Show the revival panel
        if (uiHelper != null && uiHelper.revivalPanel != null)
        {
            uiHelper.ShowRevivalPanel(true);
        }
        
        // Note: We're no longer starting the ReviveAfterDelay coroutine
        // The player must choose a revival option from the UI
    }

    private void Update()
    {
        // Don't allow control if dead
        if (animController != null && animController.IsAnimationPlaying("death"))
            return;
            
        HandleInput();
        HandleMovement();
        HandleActions();

        // Add this to prevent unwanted rotation
        if (!isMoving) // Only freeze rotation when not intentionally moving
        {
            // If using Rigidbody
            if (GetComponent<Rigidbody>() != null)
            {
                GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                // Or completely freeze rotation
                GetComponent<Rigidbody>().freezeRotation = true;
            }
            
            // If using CharacterController, ensure rotation isn't changed unintentionally
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }

        // Look for code like this in Update/FixedUpdate methods
        // Make sure it only runs when you want the player to face a target
        if (isAutoFacing && targetToFace != null)
        {
            // Look at target code
            Vector3 direction = (targetToFace.position - transform.position).normalized;
            direction.y = 0; // Keep rotation on horizontal plane
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        // Find camera-related code and ensure it doesn't rotate the player
        // unless you're actively controlling it

        // Example fix:
        RotateWithCamera();
    }
    
    private void HandleInput()
    {
        // Skip input handling if controls are disabled
        if (isControlsDisabled)
        {
            moveDirection = Vector3.zero;
            return;
        }
        
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Add a dead zone to ignore tiny inputs
        if (Mathf.Abs(horizontalInput) < 0.1f)
            horizontalInput = 0;
        if (Mathf.Abs(verticalInput) < 0.1f)
            verticalInput = 0;
        
        // Remove the check for attacking/eating states
        // Players should be able to move while attacking
        if (animController != null && animController.IsAnimationPlaying("eat"))
        {
            moveDirection = Vector3.zero;
            return;
        }
        
        // For PC platforms, always check for keyboard input
        if (!Application.isMobilePlatform)
        {
            // Direct key checks for more responsive controls
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                verticalInput += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                verticalInput -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                horizontalInput -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                horizontalInput += 1f;
        }
        
        // Use joystick input if available and add it to the keyboard input (if any)
        if (joystick != null && joystick.IsDragging)
        {
            horizontalInput += joystick.Horizontal;
            verticalInput += joystick.Vertical;
            
            // Add a small deadzone to prevent drift
            if (Mathf.Abs(horizontalInput) < 0.1f) horizontalInput = 0;
            if (Mathf.Abs(verticalInput) < 0.1f) verticalInput = 0;
        }
        // For mobile platforms without joystick active, fall back to regular input
        else if (Application.isMobilePlatform)
        {
            // Traditional input system for mobile
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");
        }
        
        // Normalize input if it exceeds magnitude of 1
        Vector2 inputVector = new Vector2(horizontalInput, verticalInput);
        if (inputVector.magnitude > 1f)
            inputVector = inputVector.normalized;
        
        horizontalInput = inputVector.x;
        verticalInput = inputVector.y;
        
        // Get camera forward and right vectors (ignore y component)
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        
        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();
        
        // Calculate movement direction relative to camera
        moveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput);
        
        // Normalize only if magnitude > 1 to allow for diagonal movement at same speed
        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();
    }
    
    private void HandleMovement()
    {
        // Skip if we're dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
        
        // Set isMoving based on movement direction magnitude
        isMoving = moveDirection.magnitude > 0.1f;
        
        // Handle rotation independently of movement
        if (isMoving)
        {
            if (livingEntity != null)
            {
                // Increase rotation speed for snappier turning
                livingEntity.RotateTowards(moveDirection, 5.0f);
            }
        }
        
        // Calculate target speed based on input magnitude
        float targetSpeed = moveDirection.magnitude * livingEntity.moveSpeed;
        
        // If at map border, check if trying to move through border
        if (isAtMapBorder && moveDirection.magnitude > 0.1f)
        {
            // Check if movement direction is against the border
            float movementWithBorder = Vector3.Dot(moveDirection, borderNormal);
            
            // If trying to move away from the playable area (negative dot product)
            if (movementWithBorder < 0)
            {
                // Use a stronger correction for movement against the border
                Vector3 correctedDirection = moveDirection - (borderNormal * movementWithBorder * 2f);
                
                // Add a small inward bias to ensure the player moves away from the boundary
                correctedDirection += borderNormal * 0.2f;
                
                // Update movement direction with the corrected version
                moveDirection = correctedDirection;
                
                // Normalize if needed
                if (moveDirection.magnitude > 1f)
                    moveDirection.Normalize();
                
                // Recalculate target speed with adjusted movement direction
                targetSpeed = moveDirection.magnitude * livingEntity.moveSpeed;
                
                // Reduce speed when trying to move against the boundary
                targetSpeed *= 0.5f;
            }
            else
            {
                // Player is moving back toward playable area, gradually reset border state
                isAtMapBorder = false;
            }
        }
        
        // Smoothly adjust current speed using acceleration/deceleration
        float accelerationFactor = acceleration * Time.deltaTime;
        float decelerationFactor = deceleration * Time.deltaTime;
        
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 
            targetSpeed > currentSpeed ? accelerationFactor : decelerationFactor);
        
        // Update animation state
        if (animController != null)
        {
            bool shouldBeWalking = currentSpeed > 0.1f;
            if (shouldBeWalking != animController.IsAnimationPlaying("walk"))
            {
                animController.SetWalking(shouldBeWalking);
            }
        }
        
        // Apply movement with current speed, but only in the forward direction
        if (currentSpeed > 0.01f)
        {
            // One final boundary check before applying movement
            Vector3 finalMoveDirection = transform.forward;
            bool shouldMove = true;
            
            if (isAtMapBorder)
            {
                float moveThroughBorder = Vector3.Dot(finalMoveDirection, borderNormal);
                if (moveThroughBorder < -0.1f) // If strongly moving against the boundary
                {
                    // If trying to move outward from the boundary, significantly reduce movement
                    shouldMove = false;
                    currentSpeed = 0;
                }
            }
            
            if (shouldMove)
            {
                if (livingEntity != null)
                {
                    // Apply the final movement with current speed
                    livingEntity.MoveInDirection(finalMoveDirection, currentSpeed / livingEntity.moveSpeed);
                }
                else
                {
                    // Fallback if living entity not available
                    Vector3 motion = transform.forward * currentSpeed * Time.deltaTime;
                    transform.position += motion;
                }
            }
        }
        else
        {
            // Stop movement when speed is near zero
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
    }
    
    private void HandleActions()
    {
        if (animController == null)
            return;
    }

    // Updated helper method to detect UI clicks/touches for both desktop and mobile
    private bool IsPointerOverUIElement()
    {
        // Check if the pointer is over a UI element
        if (EventSystem.current == null)
            return false;

        // Handle mobile touch input
        if (Application.isMobilePlatform)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                int id = touch.fingerId;
                return EventSystem.current.IsPointerOverGameObject(id);
            }
            return false;
        }
        // Handle desktop mouse input
        else
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
    }

    // Modify the OnAttackButtonPressed method to not stop movement
    public bool OnAttackButtonPressed()
    {
        // Don't attack if dead
        if (animController == null || animController.IsAnimationPlaying("death"))
            return false;
        
        // Check if we're already in the attack animation
        if (animController.IsAnimationPlaying("attack"))
            return false;
        
        if (livingEntity != null)
        {
            // Remove the movement stopping code
            // Let the player keep moving while attacking
            
            // Try to attack and get whether it was successful
            bool attackResult = livingEntity.TryAttack();
            
            float remainingCooldown = livingEntity.RemainingAttackCooldown;
            if (remainingCooldown > 0)
            {
                StartCoroutine(UpdateCooldownTimerText(remainingCooldown));
            }
        }
        return false;
    }

    // New coroutine to update the cooldown timer text
    private IEnumerator UpdateCooldownTimerText(float initialCooldown)
    {
        // Store initial cooldown value
        float remainingCooldown = initialCooldown;
        
        // Update the UI approximately every 0.1 seconds
        while (remainingCooldown > 0)
        {
            // Format the cooldown time to one decimal place
            string cooldownText = remainingCooldown.ToString("F1");
            uiHelper.ShowAttackCooldownText($"{cooldownText}s");
            
            // Wait a short time before updating again
            yield return new WaitForSeconds(0.1f);
            
            // Reduce the remaining cooldown by the elapsed time
            remainingCooldown -= 0.1f;
            
            // Ensure we don't go below zero
            remainingCooldown = Mathf.Max(0, remainingCooldown);
            
            // Break early if player is dead or the component is disabled
            if (!enabled || (livingEntity != null && livingEntity.IsDead))
                break;
        }
        
        // When cooldown is done, just provide haptic feedback without the "Attack ready!" message
        if (enabled && livingEntity != null && !livingEntity.IsDead)
        {
            // Provide haptic feedback to indicate attack is ready
            HapticFeedback.LightFeedback();
            
            // Clear the cooldown message
            uiHelper.HideInformText();
        }
    }

    // Add this method to be called when an attack successfully lands
    public void OnAttackLanded()
    {
        // Trigger haptic feedback when the attack actually lands
        HapticFeedback.MediumFeedback();
    }

    // Updated border detection system
    private void OnTriggerExit(Collider other)
    {
        // Check if this is a MapBorder
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            // Reset only our player state variables, never disable the border itself
            if (isAtMapBorder)
            {
                isAtMapBorder = false;
                borderNormal = Vector3.zero;
            }
            
            // Ensure the MapBorder collider stays enabled
            if (!other.enabled)
            {
                Debug.LogWarning($"MapBorder collider was disabled! Re-enabling: {other.gameObject.name}");
                other.enabled = true;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // First check if this is a MapBorder
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            string borderName = other.gameObject.name;
            
            // Get required level for this border
            int requiredLevel = 0;
            if (borderName == "MapBorder2")
                requiredLevel = 5;
            else if (borderName == "MapBorder3")
                requiredLevel = 10;
            else if (borderName == "MapBorder4")
                requiredLevel = 15;
            else if (borderName == "MapBorder5")
                requiredLevel = 20;
            else if (borderName == "MapBorder6")
                requiredLevel = 25;
            else if (borderName == "MapBorder7")
                requiredLevel = 30;
            else if (borderName == "MapBorder9")
                requiredLevel = 35;
            else if (borderName == "MapBorder10")
                requiredLevel = 40;
            else if (borderName == "MapBorder11")
                requiredLevel = 45;
            else if (borderName == "MapBorder12")
                requiredLevel = 50;
            else if (borderName == "MapBorder13")
                requiredLevel = 55;
            
            // Get player level - ENSURE we have the reference
            int playerLevel = 1;
            if (playerInventory == null)
            {
                playerInventory = GetComponent<PlayerInventory>();
                if (playerInventory == null)
                {
                    playerInventory = FindObjectOfType<PlayerInventory>();
                }
            }
            
            if (playerInventory != null)
            {
                playerLevel = playerInventory.CurrentLevel;
            }
            
            // Debug log to help diagnose issues
            Debug.Log($"Border: {borderName}, Required Level: {requiredLevel}, Player Level: {playerLevel}");
            
            // If this is a level-restricted border and player isn't high enough level
            if (requiredLevel > 0 && playerLevel < requiredLevel)
            {
                // We're trying to enter a restricted border
                isAtMapBorder = true;
                
                // Calculate strong repulsion away from border
                Vector3 playerToColliderCenter = transform.position - other.bounds.center;
                borderNormal = playerToColliderCenter.normalized;
                
                // Apply a stronger repulsion force
                if (livingEntity != null)
                {
                    livingEntity.MoveInDirection(borderNormal, 0.8f); // Increased force
                }
                else
                {
                    transform.position += borderNormal * 0.8f; // Increased force
                }
                
                // Show level requirement message
                if (uiHelper != null)
                {
                    uiHelper.ShowInformText($"This area requires level {requiredLevel}!");
                    
                    // Add haptic feedback
                    HapticFeedback.MediumFeedback();
                }
                
                // Log the restriction
                Debug.LogWarning($"BLOCKED: Player (Level {playerLevel}) attempted to enter {borderName} (Level {requiredLevel})");
            }
            else if (requiredLevel > 0 && playerLevel >= requiredLevel)
            {
                // Allow entry but NEVER disable the border
                isAtMapBorder = false;
                borderNormal = Vector3.zero;
                
                // Update player's current border
                PlayerAttributes playerAttributes = GetComponent<PlayerAttributes>();
                if (playerAttributes != null)
                {
                    playerAttributes.borderObjectName = borderName;
                    Debug.Log($"Player entered {borderName} (Level {requiredLevel})");
                }
            }
        }
        
        // Rest of your OnTriggerEnter code...
    }

    // Revised OnTriggerStay for gentle border restriction and border tracking
    private void OnTriggerStay(Collider other)
    {
        // Check if this is a MapBorder
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            string borderName = other.gameObject.name;
            
            // Get required level for this border
            int requiredLevel = 0;
            if (borderName == "MapBorder2")
                requiredLevel = 5;
            else if (borderName == "MapBorder3")
                requiredLevel = 10;
            else if (borderName == "MapBorder4")
                requiredLevel = 15;
            else if (borderName == "MapBorder5")
                requiredLevel = 20;
            else if (borderName == "MapBorder6")
                requiredLevel = 25;
            else if (borderName == "MapBorder7")
                requiredLevel = 30;
            else if (borderName == "MapBorder9")
                requiredLevel = 35;
            else if (borderName == "MapBorder10")
                requiredLevel = 40;
            else if (borderName == "MapBorder11")
                requiredLevel = 45;
            else if (borderName == "MapBorder12")
                requiredLevel = 50;
            else if (borderName == "MapBorder13")
                requiredLevel = 55;
            
            // Get player level
            int playerLevel = 1;
            if (playerInventory != null)
            {
                playerLevel = playerInventory.CurrentLevel;
            }
            
            // If player meets level requirement or there is no level requirement
            if (requiredLevel == 0 || playerLevel >= requiredLevel)
            {
                // Player is allowed to be here, continuously update their border object name
                PlayerAttributes playerAttributes = GetComponent<PlayerAttributes>();
                if (playerAttributes != null && playerAttributes.borderObjectName != borderName)
                {
                    // Only update if it's different from the current value
                    playerAttributes.borderObjectName = borderName;
                    Debug.Log($"Player is inside {borderName} - updated border tracking");
                }
                
                // Clear border restriction state
                isAtMapBorder = false;
                borderNormal = Vector3.zero;
            }
            // If player doesn't meet level requirement
            else if (playerLevel < requiredLevel) 
            {
                // All your existing code for preventing movement when under-leveled...
                // Get border normal direction (points away from border)
                Vector3 playerToColliderCenter = transform.position - other.bounds.center;
                playerToColliderCenter.y = 0; // Keep it horizontal
                borderNormal = playerToColliderCenter.normalized;
                
                // Find the closest point on the border collider to the player
                Vector3 closestPoint = other.ClosestPoint(transform.position);
                
                // Set the border state (used by movement handling code)
                isAtMapBorder = true;
                
                // If player is trying to move into the border (dot product with normal is negative)
                if (Vector3.Dot(moveDirection, borderNormal) < 0)
                {
                    // Cancel movement by zeroing moveDirection
                    moveDirection = Vector3.zero;
                    
                    // If using Rigidbody, zero the velocity
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                        
                        // Only zero horizontal velocity if player is moving toward border
                        if (Vector3.Dot(horizontalVelocity, borderNormal) < 0)
                        {
                            rb.velocity = new Vector3(0, rb.velocity.y, 0);
                        }
                    }
                }
                
                // Show message every 2 seconds
                if (Time.time >= lastBorderMessageTime + 2.0f)
                {
                    if (uiHelper != null)
                    {
                        uiHelper.ShowInformText($"This area requires level {requiredLevel}!");
                        HapticFeedback.LightFeedback(); // Lighter feedback
                        lastBorderMessageTime = Time.time;
                    }
                }
            }
        }
    }

    // Replace the old CheckForBoundaries method
    private void CheckForBoundaries()
    {
        // This method is now handled by the trigger collision detection
        return;
    }

    // Modify the CheckForEnemiesInRange method to rely solely on the hitbox detection
    private IEnumerator CheckForEnemiesInRange()
    {
        while (enabled)
        {
            yield return new WaitForSeconds(1.0f); // Less frequent checks since hitbox is primary detection
            
            // This is now just a backup system to clean up the enemies list
            // and restart auto-attack if needed
            
            // Clean up dead enemies and enemies that moved too far away
            for (int i = enemiesInRange.Count - 1; i >= 0; i--)
            {
                LivingEntity enemy = enemiesInRange[i];
                if (enemy == null || enemy.IsDead)
                {
                    enemiesInRange.RemoveAt(i);
                }
            }
            
            // If we have enemies in range but auto-attack isn't running, restart it
            if (enemiesInRange.Count > 0 && autoAttackEnabled && 
                livingEntity != null && !livingEntity.IsDead && 
                autoAttackCoroutine == null)
            {
                autoAttackCoroutine = StartCoroutine(AutoAttackCoroutine());
            }
        }
    }

    // Handle enemy detection from hitbox
    private void HandleEnemyDetected(LivingEntity enemy)
    {
        if (showDebugMessages)
        {
            Debug.Log($"Enemy detected: {enemy.name}, Layer: {LayerMask.LayerToName(enemy.gameObject.layer)}");
        }
        
        // Don't start auto-attack if player is dead or auto-attack is disabled
        if (livingEntity == null || livingEntity.IsDead || !autoAttackEnabled)
            return;
        
        // Check if this is an enemy (try both by tag and by layer)
        bool isEnemy = enemy.gameObject.layer == LayerMask.NameToLayer("Insects") || 
                       enemy.CompareTag("Enemy");
        
        if (isEnemy && !enemy.IsDead)
        {
            if (showDebugMessages)
            {
                Debug.Log($"Valid enemy detected for auto-attack: {enemy.name}");
            }
            
            // Add enemy to our tracking list if not already there
            if (!enemiesInRange.Contains(enemy))
            {
                enemiesInRange.Add(enemy);
            }
            
            // Start auto-attack coroutine if not already running
            if (autoAttackCoroutine == null)
            {
                autoAttackCoroutine = StartCoroutine(AutoAttackCoroutine());
            }
        }
    }

    // Handle enemy leaving hitbox
    private void HandleEnemyLost(LivingEntity enemy)
    {
        // Remove the enemy from our tracking list
        if (enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Remove(enemy);
        }
        
        // If no more enemies in range, stop auto-attacking
        if (enemiesInRange.Count == 0)
        {
            // Stop auto-attack coroutine if it's running
            if (autoAttackCoroutine != null)
            {
                StopCoroutine(autoAttackCoroutine);
                autoAttackCoroutine = null;
            }
        }
    }

    // Fixed auto-attack coroutine to prevent double damage
    private IEnumerator AutoAttackCoroutine()
    {
        if (showDebugMessages)
        {
            Debug.Log("Auto-attack coroutine started");
        }
        
        while (enabled && livingEntity != null && !livingEntity.IsDead && autoAttackEnabled)
        {
            // Clean up any dead enemies from our list
            enemiesInRange.RemoveAll(enemy => enemy == null || enemy.IsDead);
            
            // Check if there are enemies in range
            if (enemiesInRange.Count > 0)
            {
                // Find the closest enemy
                LivingEntity closestEnemy = null;
                float closestDistance = float.MaxValue;
                
                foreach (LivingEntity enemy in enemiesInRange)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy;
                    }
                }
                
                // Attack without rotating towards the enemy
                if (closestEnemy != null)
                {
                    // Ensure the enemy is set as the current target
                    livingEntity.AddTargetInRange(closestEnemy);
                    
                    // Just trigger the attack animation and let the animation event handle damage
                    bool attackStarted = OnAttackButtonPressed();
                    
                    if (attackStarted)
                    {
                        // Wait for the attack animation to complete
                        // We don't need to manually apply damage - the animation event will do it
                        
                        // Wait for the attack cooldown to finish
                        while (livingEntity.RemainingAttackCooldown > 0)
                        {
                            yield return null; // Wait one frame
                        }
                    }
                    else
                    {
                        // If attack wasn't successful (likely on cooldown), wait a short time before trying again
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                if (showDebugMessages)
                {
                    Debug.Log("No enemies in range, exiting auto-attack");
                }
                // No targets in range, exit the coroutine
                break;
            }
        }
        
        // Reset the coroutine reference
        autoAttackCoroutine = null;
        
        if (showDebugMessages)
        {
            Debug.Log("Auto-attack coroutine ended");
        }
    }

    // Public method to toggle auto-attack
    public void ToggleAutoAttack()
    {
        autoAttackEnabled = !autoAttackEnabled;
        
        // If disabled, stop any ongoing auto-attack
        if (!autoAttackEnabled && autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }
        // If enabled and there are enemies in range, start auto-attacking
        else if (autoAttackEnabled && livingEntity != null && 
                 enemiesInRange.Count > 0 && 
                 autoAttackCoroutine == null)
        {
            autoAttackCoroutine = StartCoroutine(AutoAttackCoroutine());
        }
        
        // Inform the player of the auto-attack state change
        if (uiHelper != null)
        {
            string message = autoAttackEnabled ? "Auto-attack enabled" : "Auto-attack disabled";
            uiHelper.ShowInformText(message);
        }
    }

    // Add this method to PlayerController.cs to expose the movement direction to other scripts
    public Vector3 GetMovementDirection()
    {
        return moveDirection;
    }

    // Update the attack method to check for matching border
    public void PerformAttack()
    {
        if (livingEntity == null) return;
        
        // Find closest enemy or use current target from auto-attack system
        LivingEntity target = null;
        
        // Try to get target from enemies in range
        if (enemiesInRange != null && enemiesInRange.Count > 0)
        {
            // Get the first non-dead enemy in range that's in the same border
            foreach (var enemy in enemiesInRange)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    // Check if enemy is in the same border area
                    bool inSameBorder = CheckSameBorderAs(enemy.gameObject);
                    if (inSameBorder)
                    {
                        target = enemy;
                        break;
                    }
                }
            }
        }
        
        // If we have a valid target, attack it
        if (target != null && !target.IsDead)
        {
            // Deal damage to the target, passing the player gameObject as the damage source
            float damage = livingEntity.AttackDamage;
            target.TakeDamage(damage, this.gameObject);
            
            // Log the attack
            Debug.Log($"Player attacked {target.name} for {damage} damage");
            
            // Play attack animation if we have an animation controller
            if (animController != null)
            {
                animController.SetAttacking(true);
                // Reset attack animation after a short delay
                StartCoroutine(ResetAttackAnimation(0.5f));
            }
        }
    }

    // Helper method to reset attack animation
    private IEnumerator ResetAttackAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animController != null)
        {
            animController.SetAttacking(false);
        }
    }

    // Add this helper method to PlayerController.cs
    private bool CheckSameBorderAs(GameObject other)
    {
        if (other == null) return false;
        
        // Get player's border name
        PlayerAttributes playerAttrib = GetComponent<PlayerAttributes>();
        string playerBorderName = playerAttrib?.borderObjectName ?? "";
        
        // Check if the entity has an EnemyAI component
        EnemyAI enemyAI = other.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            return enemyAI.borderObjectName == playerBorderName;
        }
        
        // If no EnemyAI component is found, default to true (allow attack)
        return true;
    }

    // Add this method to PlayerController.cs
    private void DisableBordersForCurrentLevel()
    {
        // Get the player's current level
        PlayerInventory playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory == null) return;
        
        int playerLevel = playerInventory.CurrentLevel;
        
        // Find all border visualizers in the scene
        BorderVisualizer[] borders = FindObjectsOfType<BorderVisualizer>();
        foreach (var border in borders)
        {
            // Get the border game object name
            string borderName = border.gameObject.name;
            
            // Set required level for this border if not already set
            if (border.GetRequiredLevel() == 0)
            {
                if (borderName == "MapBorder2")
                    border.SetRequiredLevel(5);
                else if (borderName == "MapBorder3")
                    border.SetRequiredLevel(10);
                else if (borderName == "MapBorder4")
                    border.SetRequiredLevel(15);
                else if (borderName == "MapBorder5")
                    border.SetRequiredLevel(20);
                else if (borderName == "MapBorder6")
                    border.SetRequiredLevel(25);
                else if (borderName == "MapBorder7")
                    border.SetRequiredLevel(30);
                else if (borderName == "MapBorder9")
                    border.SetRequiredLevel(35);
                else if (borderName == "MapBorder10")
                    border.SetRequiredLevel(40);
                else if (borderName == "MapBorder11")
                    border.SetRequiredLevel(45);
                else if (borderName == "MapBorder12")
                    border.SetRequiredLevel(50);
                else if (borderName == "MapBorder13")
                    border.SetRequiredLevel(55);
            }
            
        }
    }

    // Handle level up event
    private void OnPlayerLevelUp(int newLevel)
    {
        Debug.Log($"OnPlayerLevelUp triggered with level {newLevel}");
        
        // Disable borders that the player can now access
        DisableBordersForCurrentLevel();

        // Check for metamorphosis on level up
        CheckMetamorphosis(newLevel);
    }

    // Fix the StopAllAttacks method to use proper variables
    public void StopAllAttacks()
    {
        // Reset all animation states related to attacking
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("Attack", false);  // Include other possible animation parameters
            
            // Reset trigger parameters that might be set
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("AttackTrigger");
        }
        
        // Cancel any coroutines that might be related to attacking
        try
        {
            StopCoroutine("AttackCoroutine");
            StopCoroutine("PerformAttack");
        }
        catch (System.Exception)
        {
            // Ignore errors if coroutines weren't running
        }
        
        // Get the animation controller and set idle state
        AnimationController animController = GetComponent<AnimationController>();
        if (animController != null)
        {
            animController.SetIdle();
        }
        
        Debug.Log("Stopped all player attacks");
    }

    // Find camera-related code and ensure it doesn't rotate the player
    // unless you're actively controlling it

    // Example fix:
    void RotateWithCamera()
    {
        // Only rotate player with camera when actually moving AND we're not auto-facing something
        if (isMoving && !isAutoFacing)
        {
            // Don't rotate the player directly, use the livingEntity method instead
            // This was likely causing your unwanted rotation
        }
    }

    // Add a method to set the auto-facing target
    public void SetFacingTarget(Transform target, bool enableAutoFacing = true)
    {
        targetToFace = target;
        isAutoFacing = enableAutoFacing;
    }

    // Add a method to clear the auto-facing target
    public void ClearFacingTarget()
    {
        targetToFace = null;
        isAutoFacing = false;
    }

    // Ensure all attack cooldown messages use ShowAttackCooldownText
    public void HandleAttackCooldown()
    {
        if (uiHelper != null)
        {
            // Use ShowAttackCooldownText instead of ShowInformText
            uiHelper.ShowAttackCooldownText($"Cooldown: {livingEntity.RemainingAttackCooldown:F1}s");
        }
    }

    // Add this method to handle metamorphosis scale changes
    private void ApplyMetamorphosisScale(int level)
    {
        // Reset stage counter
        int previousStage = currentMetamorphosisStage;
        currentMetamorphosisStage = 0;
        
        // Count how many metamorphosis stages the player has reached
        for (int i = 0; i < metamorphosisLevels.Length; i++)
        {
            if (level >= metamorphosisLevels[i])
            {
                currentMetamorphosisStage++;
            }
        }
        
        // Calculate the cumulative scale multiplier
        float cumulativeScale = Mathf.Pow(scaleIncreaseMultiplier, currentMetamorphosisStage);
        Vector3 newScale = baseScale * cumulativeScale;
        
        // Apply the new scale
        transform.localScale = newScale;
        
        // Log detailed information
        if (previousStage != currentMetamorphosisStage)
        {
            Debug.Log($"<color=cyan>Applied metamorphosis scale change - Level: {level}, Stage: {currentMetamorphosisStage}, Scale: {cumulativeScale}x</color>");
        }
        else
        {
            Debug.Log($"Applied metamorphosis scale {cumulativeScale}x for level {level} (Stage {currentMetamorphosisStage})");
        }
        
        // Update camera settings based on level
        UpdateCameraSettings(level);
    }

    // Update the metamorphosis method to debug the level checks
    public void CheckMetamorphosis(int level)
    {
        // Skip metamorphosis checks during initialization/game loading
        if (isInitializing)
        {
            Debug.Log("Skipping metamorphosis check during initialization");
            return;
        }
        
        if (!enableMetamorphosis) 
        {
            Debug.Log("Metamorphosis is disabled in the inspector");
            return;
        }
        
        Debug.Log($"Checking metamorphosis for level {level}. Current stage: {currentMetamorphosisStage}");
        
        // Check if this level is a metamorphosis level
        bool foundMatch = false;
        for (int i = 0; i < metamorphosisLevels.Length; i++)
        {
            if (level == metamorphosisLevels[i])
            {
                foundMatch = true;
                
                // Play metamorphosis effect
                PlayMetamorphosisEffect();
                
                // Apply new scale - increase by percentage from current scale
                currentMetamorphosisStage++;
                float cumulativeScale = Mathf.Pow(scaleIncreaseMultiplier, currentMetamorphosisStage);
                Vector3 newScale = baseScale * cumulativeScale;
                
                // Apply the scale
                transform.localScale = newScale;
                
                Debug.Log($"<color=yellow>METAMORPHOSIS TRIGGERED: Level {level} reached! New scale: {cumulativeScale}x (Stage {currentMetamorphosisStage})</color>");
                
                // Update camera settings
                UpdateCameraSettings(level);
                break;
            }
        }
        
        if (!foundMatch)
        {
            Debug.Log($"Level {level} is not a metamorphosis level. Available levels: {string.Join(", ", metamorphosisLevels)}");
        }
    }

    // Add this method to update camera settings based on player level
    private void UpdateCameraSettings(int level)
    {
        // Find the camera controller
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null) return;
        
        // Set camera parameters based on level
        if (level >= 52)
        {
            cameraController.SetCameraParameters(6.96f, 9.26f);
        }
        else if (level >= 47)
        {
            cameraController.SetCameraParameters(5.7f, 8.11f);
        }
        else if (level >= 42)
        {
            cameraController.SetCameraParameters(5.16f, 7.13f);
        }
        else if (level >= 37)
        {
            cameraController.SetCameraParameters(4.62f, 6.45f);
        }
        else if (level >= 32)
        {
            cameraController.SetCameraParameters(4.18f, 5.72f);
        }
        else if (level >= 27)
        {
            cameraController.SetCameraParameters(3.73f, 4.89f);
        }
        else if (level >= 22)
        {
            cameraController.SetCameraParameters(3.03f, 4.08f);
        }
        else if (level >= 17)
        {
            cameraController.SetCameraParameters(3.13f, 4.18f);
        }
        else if (level >= 12)
        {
            cameraController.SetCameraParameters(2.55f, 3.33f);
        }
        else if (level >= 7)
        {
            cameraController.SetCameraParameters(2.16f, 2.85f);
        }
        else if (level >= 2)
        {
            cameraController.SetCameraParameters(2.04f, 2.62f);
        }
        else
        {
            // Default settings for levels 1-4
            cameraController.SetCameraParameters(1.6f, 2.1f);
        }
    }

    // Split metamorphosis effect into camera animation and size-up
    private void PlayMetamorphosisEffect()
    {
        // Play sound effect
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("Metamorphosis");
        }

        // Show "Size Up!" text only when there's an actual stage change
        if (uiHelper != null && uiHelper.sizeUpText != null)
        {
            // Show the Size Up text
            uiHelper.sizeUpText.gameObject.SetActive(true);
            uiHelper.sizeUpText.text = "SIZE UP!";
            
            // Hide it after a few seconds
            StartCoroutine(HideSizeUpTextAfterDelay(3.0f));
        }
        
        // Play metamorphosis camera animation first, then size up
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null)
        {
            // Pass a callback that will be invoked when camera is in position
            cameraAnimations.AnimateMetamorphosis(transform, PerformSizeUp);
        }
        else
        {
            // Fallback to immediate size-up with camera shake if animations component not found
            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController != null)
            {
                if (cameraController.GetType().GetMethod("ShakeCamera") != null)
                {
                    cameraController.SendMessage("ShakeCamera", 0.3f, SendMessageOptions.DontRequireReceiver);
                }
            }
            
            // Immediate size up if no animation controller
            PerformSizeUp();
        }
    }

    // Modify PerformSizeUp to check the initialization flag
    private void PerformSizeUp()
    {
        // Skip the visual effects if we're just initializing
        if (isInitializingScale)
        {
            Debug.Log("Skipping size up animation during initialization");
            return;
        }

        // Calculate and apply new scale here
        if (enableMetamorphosis)
        {
            // Calculate metamorphosis stage
            int currentLevel = playerInventory.CurrentLevel;
            bool didStageChange = false;
            
            int previousStage = currentMetamorphosisStage;
            currentMetamorphosisStage = 0;
            
            for (int i = 0; i < metamorphosisLevels.Length; i++)
            {
                if (currentLevel >= metamorphosisLevels[i])
                {
                    currentMetamorphosisStage++;
                }
            }
            
            didStageChange = previousStage != currentMetamorphosisStage;
            
            if (didStageChange)
            {
                // Apply the new scale
                float cumulativeScale = Mathf.Pow(scaleIncreaseMultiplier, currentMetamorphosisStage);
                Vector3 newScale = baseScale * cumulativeScale;
                transform.localScale = newScale;
                
                Debug.Log($"<color=green>Player metamorphosis size up! New scale: {newScale}, Stage: {currentMetamorphosisStage}</color>");
            }
        }
    }

    // Helper coroutine to hide the size up text after a delay
    private IEnumerator HideSizeUpTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (uiHelper != null && uiHelper.sizeUpText != null)
        {
            uiHelper.sizeUpText.gameObject.SetActive(false);
        }
    }

    // Add this method to ensure proper initialization of player size and camera view
    private void EnsureCorrectSizeAndCamera()
    {
        if (playerInventory == null)
        {
            Debug.LogError("Cannot initialize size and camera: playerInventory is null");
            return;
        }

        int currentLevel = playerInventory.CurrentLevel;
        Debug.Log($"Ensuring correct size and camera for player level: {currentLevel}");

        // Apply the correct scale based on the player's current level WITHOUT animation
        if (enableMetamorphosis)
        {
            // Calculate the metamorphosis stage based on current level
            currentMetamorphosisStage = 0;
            for (int i = 0; i < metamorphosisLevels.Length; i++)
            {
                if (currentLevel >= metamorphosisLevels[i])
                {
                    currentMetamorphosisStage++;
                }
            }

            // Apply the scale without animation
            float cumulativeScale = Mathf.Pow(scaleIncreaseMultiplier, currentMetamorphosisStage);
            Vector3 newScale = baseScale * cumulativeScale;
            transform.localScale = newScale;

            // Set a flag to indicate this was an initialization, not a real metamorphosis
            isInitializingScale = true;

            Debug.Log($"<color=orange>Initialized player scale: {newScale} (Stage: {currentMetamorphosisStage})</color>");
        }

        // Force camera settings to match current level immediately
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            // Call the immediate version to avoid transitions on initialization
            UpdateCameraSettingsImmediate(currentLevel);
            Debug.Log($"<color=orange>Initialized camera settings for player level: {currentLevel}</color>");
        }
        else
        {
            Debug.LogError("Failed to find CameraController for initialization");
        }
        
        // Reset initialization flag
        isInitializingScale = false;
    }

    // Add this method for immediate camera settings (no transition)
    private void UpdateCameraSettingsImmediate(int level)
    {
        // Find the camera controller
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null) return;
        
        float zoomLevel = 1.6f;
        float heightOffset = 2.1f;
        
        // Determine the proper camera parameters based on level
        if (level >= 52)
        {
            zoomLevel = 6.96f;
            heightOffset = 9.26f;
        }
        else if (level >= 47)
        {
            zoomLevel = 5.7f;
            heightOffset = 8.11f;
        }
        else if (level >= 42)
        {
            zoomLevel = 5.16f;
            heightOffset = 7.13f;
        }
        else if (level >= 37)
        {
            zoomLevel = 4.62f;
            heightOffset = 6.45f;
        }
        else if (level >= 32)
        {
            zoomLevel = 4.18f;
            heightOffset = 5.72f;
        }
        else if (level >= 27)
        {
            zoomLevel = 3.73f;
            heightOffset = 4.89f;
        }
        else if (level >= 22)
        {
            zoomLevel = 3.03f;
            heightOffset = 4.08f;
        }
        else if (level >= 17)
        {
            zoomLevel = 3.13f;
            heightOffset = 4.18f;
        }
        else if (level >= 12)
        {
            zoomLevel = 2.55f;
            heightOffset = 3.33f;
        }
        else if (level >= 7)
        {
            zoomLevel = 2.16f;
            heightOffset = 2.85f;
        }
        else if (level >= 2)
        {
            zoomLevel = 2.04f;
            heightOffset = 2.62f;
        }
        
        // Use the immediate version to avoid transition animation
        cameraController.SetCameraParametersImmediate(zoomLevel, heightOffset);
        Debug.Log($"Camera parameters set immediately: Zoom = {zoomLevel}, Height = {heightOffset} for level {level}");
    }

    // Add a public method to disable/enable player controls
    public void SetControlsEnabled(bool enabled)
    {
        isControlsDisabled = !enabled;
        
        // Log the state change
        Debug.Log($"Player controls {(enabled ? "enabled" : "disabled")}");
        
        // If disabling controls, also stop any current movement
        if (!enabled)
        {
            // Reset movement variables
            moveDirection = Vector3.zero;
            currentSpeed = 0f;
            isMoving = false;
            
            // Stop rigidbody motion if we have one
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
    }

    // Add this method to complete initialization
    private void CompleteInitialization()
    {
        isInitializing = false;
        Debug.Log("Player initialization completed - metamorphosis checks now active");
    }
}