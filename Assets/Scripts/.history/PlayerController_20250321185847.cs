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

        // Subscribe to level up event
        if (playerInventory != null)
        {
            playerInventory.OnLevelUp += OnPlayerLevelUp;
        }
        
        // Check borders after a short delay (to ensure all are initialized)
        Invoke("DisableBordersForCurrentLevel", 1.0f);
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
        
        StartCoroutine(ReviveAfterDelay(5f));
        
    }

    private IEnumerator ReviveAfterDelay(float delay)
    {
        // Show initial death message
        if (uiHelper != null && uiHelper.informPlayerText != null)
            uiHelper.ShowInformText($"You are dead. Revive in {delay} seconds.");
        
        // Countdown logic
        float remainingTime = delay;
        while (remainingTime > 0)
        {
            // Wait one second
            yield return new WaitForSeconds(1f);
            
            // Decrease the counter
            remainingTime -= 1f;
            
            // Update the text with the new countdown value
            if (uiHelper != null && uiHelper.informPlayerText != null)
                uiHelper.ShowInformText($"You are dead. Revive in {Mathf.CeilToInt(remainingTime)} seconds.");
        }
        
        // IMPORTANT: First teleport the player to the spawn point
        transform.position = spawnPoint.position;
        
        // Clear any enemies in range to prevent immediate attacks
        enemiesInRange.Clear();
        
        // If auto-attack is running, stop it
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }
        
        // Wait additional time after teleporting to ensure all systems reset
        yield return new WaitForSeconds(0.45f);
        
        // Now revive the player
        if (livingEntity != null)
        {
            // Set health to non-zero value to trigger auto-revive in LivingEntity
            livingEntity.SetHealth(livingEntity.MaxHealth * 0.5f);
            
            // Force update the health bar UI
            PlayerHealthBarController healthBar = FindObjectOfType<PlayerHealthBarController>();
            if (healthBar != null)
            {
                healthBar.UpdateHealthBar();
            }
            
            // Make sure animation transitions back to idle state
            if (animController != null)
            {
                // Wait a frame to ensure LivingEntity has processed the revive
                yield return null;
                
                // Explicitly set idle animation
                animController.SetIdle();
            }
            
            // Re-enable player input
            enabled = true;
            
            // Clear the UI message
            if (uiHelper != null && uiHelper.informPlayerText != null)
                uiHelper.ShowInformText("You have been revived!");
            
            // Add temporary invulnerability
            livingEntity.SetTemporaryInvulnerability(2f); // Increased from 1 to 2 seconds
        }
    }

    private void Update()
    {
        // Don't allow control if dead
        if (animController != null && animController.IsAnimationPlaying("death"))
            return;
            
        HandleInput();
        HandleMovement();
        HandleActions();
    }
    
    private void HandleInput()
    {
        float horizontal = 0f;
        float vertical = 0f;
        
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
                vertical += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                vertical -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                horizontal += 1f;
        }
        
        // Use joystick input if available and add it to the keyboard input (if any)
        if (joystick != null && joystick.IsDragging)
        {
            horizontal += joystick.Horizontal;
            vertical += joystick.Vertical;
            
            // Add a small deadzone to prevent drift
            if (Mathf.Abs(horizontal) < 0.1f) horizontal = 0;
            if (Mathf.Abs(vertical) < 0.1f) vertical = 0;
        }
        // For mobile platforms without joystick active, fall back to regular input
        else if (Application.isMobilePlatform)
        {
            // Traditional input system for mobile
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
        }
        
        // Normalize input if it exceeds magnitude of 1
        Vector2 inputVector = new Vector2(horizontal, vertical);
        if (inputVector.magnitude > 1f)
            inputVector = inputVector.normalized;
        
        horizontal = inputVector.x;
        vertical = inputVector.y;
        
        // Get camera forward and right vectors (ignore y component)
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        
        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();
        
        // Calculate movement direction relative to camera
        moveDirection = (cameraForward * vertical + cameraRight * horizontal);
        
        // Normalize only if magnitude > 1 to allow for diagonal movement at same speed
        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();
    }
    
    private void HandleMovement()
    {
        // Skip if we're dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
        
        // Handle rotation independently of movement
        if (moveDirection.magnitude > 0.1f)
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
            // Since borderNormal now points inward, we WANT positive dot product
            float movementWithBorder = Vector3.Dot(moveDirection, borderNormal);
            
            // If trying to move away from the center (negative dot product)
            if (movementWithBorder < 0)
            {
                // Use a stronger correction - completely cancel the outward component
                // and add a small force inward for better boundary enforcement
                Vector3 correctedDirection = moveDirection - (borderNormal * movementWithBorder);
                
                // Add a small inward bias to ensure the player moves away from the boundary
                correctedDirection += borderNormal * 0.1f;
                
                // Update movement direction with the corrected version
                moveDirection = correctedDirection;
                
                // Make sure movement remains normalized if needed
                if (moveDirection.magnitude > 1f)
                    moveDirection.Normalize();
                
                // If movement becomes negligible after restriction, stop
                if (moveDirection.magnitude < 0.01f)
                    moveDirection = Vector3.zero;
                
                // Recalculate target speed with adjusted movement direction
                targetSpeed = moveDirection.magnitude * livingEntity.moveSpeed;
                
                // Reduce speed when trying to move against the boundary
                targetSpeed *= 0.5f;
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
            uiHelper.ShowInformText($"Attack on cooldown! Ready in {cooldownText}s");
            
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
        // Check if the collider is in the MapBorder layer
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            string borderName = other.gameObject.name;
            
            // For MapBorder1, allow free movement in both directions
            if (borderName == "MapBorder1")
            {
                isAtMapBorder = false;
                borderNormal = Vector3.zero;
                return;
            }
            
            // For MapBorder2-5, allow exiting but not entering
            if (borderName.StartsWith("MapBorder") && 
               (borderName == "MapBorder2" || 
                borderName == "MapBorder3" || 
                borderName == "MapBorder4" || 
                borderName == "MapBorder5" || 
                borderName == "MapBorder6" || 
                borderName == "MapBorder7" ||  
                borderName == "MapBorder9" || 
                borderName == "MapBorder10" ||
                borderName == "MapBorder11" ||
                borderName == "MapBorder12" ||
                borderName == "MapBorder13"))
            {
                // We're trying to exit a restricted border - this is ALLOWED
                isAtMapBorder = false;
                borderNormal = Vector3.zero;
                return;
            }
            
            // For any other border, use the existing restriction logic
            isAtMapBorder = true;
            
            // The normal should point back INTO the sphere (toward center)
            if (other is SphereCollider sphereCollider)
            {
                // Get the center of the sphere in world space
                Vector3 sphereCenter = other.transform.TransformPoint(sphereCollider.center);
                // Calculate direction from player to sphere center
                borderNormal = (sphereCenter - transform.position).normalized;
            }
            else
            {
                // For non-sphere colliders, try to get closest point on the collider
                Vector3 closestPoint = other.ClosestPoint(transform.position);
                // Direction from player to closest point (back toward inside)
                borderNormal = (closestPoint - transform.position).normalized;
            }
            
            // Immediately move the player back inside the boundary with a stronger force
            if (livingEntity != null)
            {
                // Push the player back toward the center with a stronger force
                livingEntity.MoveInDirection(borderNormal, 0.5f); // Increased from 0.2f
            }
            else
            {
                // Fallback direct position adjustment with stronger push
                transform.position += borderNormal * 0.5f; // Increased from 0.2f
            }
            
            // Only show messages if enabled and not on cooldown
            if (showBorderMessages && Time.time >= lastBorderMessageTime + borderMessageCooldown)
            {
                // Show the error message in debug console
                Debug.LogError($"Player attempted to exit playable area: {other.gameObject.name}");
                
                // Display UI message if we have a UI helper
                if (uiHelper != null)
                {
                    uiHelper.ShowInformText("You've reached the boundary of the playable area!");
                }
                
                // Add haptic feedback for mobile
                HapticFeedback.LightFeedback();
                
                // Set the cooldown
                lastBorderMessageTime = Time.time;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider is in the MapBorder layer
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            string borderName = other.gameObject.name;
            
            // Allow entry into MapBorder1 freely
            if (borderName == "MapBorder1")
            {
                isAtMapBorder = false;
                borderNormal = Vector3.zero;
                return;
            }
            
            // Check level requirements for restricted borders
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

            // Get player level (if playerInventory reference exists)
            int playerLevel = 1; // Default level
            if (playerInventory != null)
                playerLevel = playerInventory.CurrentLevel;
            else if (FindObjectOfType<PlayerInventory>() != null)
            {
                playerInventory = FindObjectOfType<PlayerInventory>(); // Try to find it
                playerLevel = playerInventory.CurrentLevel;
            }
                
            // If this is a level-restricted border and player isn't high enough level
            if (requiredLevel > 0 && playerLevel < requiredLevel)
            {
                // We're trying to enter a restricted border - calculate repulsion force
                isAtMapBorder = true;
                
                // Calculate the normal pointing AWAY from the border (opposite of usual)
                Vector3 playerToColliderCenter = transform.position - other.bounds.center;
                borderNormal = playerToColliderCenter.normalized;
                
                // Apply a strong force to push player away from restricted border
                if (livingEntity != null)
                {
                    livingEntity.MoveInDirection(borderNormal, 3f);
                }
                else
                {
                    transform.position += borderNormal * 1.1f;
                }
                
                // Show level requirement message
                if (showBorderMessages && Time.time >= lastBorderMessageTime + borderMessageCooldown)
                {
                    if (uiHelper != null)
                    {
                        uiHelper.ShowInformText($"This area requires level {requiredLevel}!");
                    }
                    
                    // Add haptic feedback for mobile
                    HapticFeedback.MediumFeedback();
                    
                    // Set the cooldown
                    lastBorderMessageTime = Time.time;
                }
                
                // Notify the LevelAreaArrowManager that we've entered this border
                if (levelAreaArrowManager != null)
                {
                    levelAreaArrowManager.CheckAreaEntry(borderName);
                }
                else
                {
                    // Try to find the manager if not assigned
                    levelAreaArrowManager = FindObjectOfType<LevelAreaArrowManager>();
                    if (levelAreaArrowManager != null)
                    {
                        levelAreaArrowManager.CheckAreaEntry(borderName);
                    }
                }
                
                return;
            }
            else if (requiredLevel > 0 && playerLevel >= requiredLevel)
            {
                // Player meets the level requirement for this border - ALLOW ENTRY
                isAtMapBorder = false;
                borderNormal = Vector3.zero;

                // Set the border name in PlayerAttributes
                PlayerAttributes playerAttributes = GetComponent<PlayerAttributes>();
                if (playerAttributes != null)
                {
                    playerAttributes.borderObjectName = borderName;
                    
                    if (showDebugMessages)
                    {
                        Debug.Log($"Set player's border object name to: {borderName}");
                    }
                }
                return;
            }
            
            // For borders without specific level requirements, default to the original prevention logic
            if (borderName.StartsWith("MapBorder") && 
               (borderName == "MapBorder2" || 
                borderName == "MapBorder3" || 
                borderName == "MapBorder4" || 
                borderName == "MapBorder5" || 
                borderName == "MapBorder6" || 
                borderName == "MapBorder7" || 
                borderName == "MapBorder9" || 
                borderName == "MapBorder10" ||
                borderName == "MapBorder11" ||
                borderName == "MapBorder12" ||
                borderName == "MapBorder13"))
            {
                // This code should only run for MapBorders with other types of restrictions
                // that aren't level-based
                
                // We're trying to enter a restricted border - calculate repulsion force
                isAtMapBorder = true;
                
                // Calculate the normal pointing AWAY from the border (opposite of usual)
                Vector3 playerToColliderCenter = transform.position - other.bounds.center;
                borderNormal = playerToColliderCenter.normalized;
                
                // Apply a strong force to push player away from restricted border
                if (livingEntity != null)
                {
                    livingEntity.MoveInDirection(borderNormal, 0.8f);
                }
                else
                {
                    transform.position += borderNormal * 0.8f;
                }
                
                // Show warning message
                if (showBorderMessages && Time.time >= lastBorderMessageTime + borderMessageCooldown)
                {
                    if (uiHelper != null)
                    {
                        uiHelper.ShowInformText("This area is currently not accessible!");
                    }
                    
                    // Add haptic feedback for mobile
                    HapticFeedback.MediumFeedback();
                    
                    // Set the cooldown
                    lastBorderMessageTime = Time.time;
                }
                
                // Notify the LevelAreaArrowManager that we've entered this border
                if (levelAreaArrowManager != null)
                {
                    levelAreaArrowManager.CheckAreaEntry(borderName);
                }
                else
                {
                    // Try to find the manager if not assigned
                    levelAreaArrowManager = FindObjectOfType<LevelAreaArrowManager>();
                    if (levelAreaArrowManager != null)
                    {
                        levelAreaArrowManager.CheckAreaEntry(borderName);
                    }
                }
                
                return;
            }
            
            // Default behavior for other borders
            isAtMapBorder = false;
            borderNormal = Vector3.zero;
        }
    }

    // We don't need OnTriggerStay for this approach
    private void OnTriggerStay(Collider other)
    {
        // This method is intentionally empty
        // We only care about exit and re-entry
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
            
            // Check if this border should be visible
            border.CheckVisibilityForPlayerLevel(playerLevel);
        }
    }

    // Handle level up event
    private void OnPlayerLevelUp(int newLevel)
    {
        // Disable borders that the player can now access
        DisableBordersForCurrentLevel();
    }
}