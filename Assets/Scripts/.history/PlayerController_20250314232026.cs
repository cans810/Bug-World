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

    // Add this new field to track if we've added the necessary components
    private Rigidbody cachedRigidbody;

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

        // Cache the rigidbody reference
        cachedRigidbody = GetComponent<Rigidbody>();
        
        // Check for rigidbody - needed for trigger interactions
        if (cachedRigidbody == null)
        {
            Debug.LogWarning("PlayerController: No Rigidbody found! Adding one for border collision detection.");
            cachedRigidbody = gameObject.AddComponent<Rigidbody>();
            cachedRigidbody.mass = 1.0f;
            cachedRigidbody.drag = 1.0f;
            cachedRigidbody.angularDrag = 0.05f;
            cachedRigidbody.useGravity = true;
            cachedRigidbody.isKinematic = false; // Must be non-kinematic for trigger events
            cachedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            cachedRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        // Add diagnostic logging for physics layers
        Debug.Log($"Player layer: {LayerMask.LayerToName(gameObject.layer)} (index: {gameObject.layer})");
        Debug.Log($"MapBorder layer exists: {LayerMask.NameToLayer("MapBorder") != -1}, index: {LayerMask.NameToLayer("MapBorder")}");
        
        // Log the player's collider setup
        Collider[] playerColliders = GetComponentsInChildren<Collider>();
        Debug.Log($"Player has {playerColliders.Length} colliders:");
        foreach (Collider col in playerColliders)
        {
            Debug.Log($"- {col.name}: isTrigger={col.isTrigger}, enabled={col.enabled}, bounds={col.bounds.size}");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandlePlayerDeath);
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
        
        // Set health to non-zero value to trigger auto-revive in LivingEntity
        if (livingEntity != null)
        {
            // We can set health directly to 50% of max health
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

            transform.position = spawnPoint.position;
            
            // Clear the UI message
            if (uiHelper != null && uiHelper.informPlayerText != null)
                uiHelper.ShowInformText("You have been revived!");
            
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

        // Add diagnostic raycast to check for borders
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.blue);
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 2f))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
            {
                Debug.Log($"Raycast hit map border: {hit.collider.name} at distance {hit.distance}");
                
                // Force update border normal from raycast data
                borderNormal = -hit.normal;
                isAtMapBorder = true;
                
                // Force stop movement toward border
                StopMovementTowardsBorder();
            }
        }
    }
    
    private void HandleInput()
    {
        float horizontal = 0f;
        float vertical = 0f;
        
        // Skip if eating or attacking
        if (animController != null && 
            (animController.IsAnimationPlaying("eat") || animController.IsAnimationPlaying("attack")))
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
        // Skip if we're dead or in another non-movable state
        if (livingEntity == null || livingEntity.IsDead)
            return;
        
        // Handle rotation independently of movement
        // Rotate in place even if not moving forward
        if (moveDirection.magnitude > 0.1f)
        {
            if (livingEntity != null)
            {
                livingEntity.RotateTowards(moveDirection, 5.0f);
            }
        }
        
        // Calculate target speed based on input magnitude
        float targetSpeed = moveDirection.magnitude * livingEntity.moveSpeed;
        
        // Enhanced border collision handling
        if (isAtMapBorder && moveDirection.magnitude > 0.1f)
        {
            // Check if movement direction is against the border (dot product negative)
            float movementThroughBorder = Vector3.Dot(moveDirection, -borderNormal);
            
            if (movementThroughBorder > 0)
            {
                // Calculate component of movement trying to go through the border
                Vector3 invalidMovement = -borderNormal * movementThroughBorder;
                
                // Remove the invalid component from the movement direction
                Vector3 validMovement = moveDirection - invalidMovement;
                
                // Update the movement direction to only include the valid component
                // If the resulting direction is too small, stop movement entirely
                if (validMovement.magnitude > 0.1f)
                {
                    moveDirection = validMovement.normalized * moveDirection.magnitude;
                    Debug.Log($"Border restricting movement. Original: {moveDirection + invalidMovement}, New: {moveDirection}");
                }
                else
                {
                    // If barely any valid movement, stop completely
                    moveDirection = Vector3.zero;
                    Debug.Log("Border blocking all movement - stopping player");
                }
                
                // Recalculate target speed with adjusted movement direction
                targetSpeed = moveDirection.magnitude * livingEntity.moveSpeed;
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
            if (livingEntity != null)
            {
                // Move only in the forward direction after rotation is applied
                livingEntity.MoveInDirection(transform.forward, currentSpeed / livingEntity.moveSpeed);
            }
            else
            {
                // Fallback if living entity not available
                Vector3 motion = transform.forward * currentSpeed * Time.deltaTime;
                transform.position += motion;
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

    // Add this public method to be called by the Attack button
    public void OnAttackButtonPressed()
    {
        // Don't attack if dead
        if (animController == null || animController.IsAnimationPlaying("death"))
            return;
        
        // Check if we're already in the attack or walkAttack animation
        if (animController.IsAnimationPlaying("attack") || animController.IsAnimationPlaying("walkAttack"))
            return;
        
        if (livingEntity != null)
        {
            // Check if we're walking to determine which animation to play
            bool isWalking = currentSpeed > 0.1f;
            
            // Try to attack and get whether it was successful
            bool attackResult = livingEntity.TryAttack();
            
            // If attack was successful, play the appropriate animation
            if (attackResult)
            {
                // If walking, set walkAttack bool to true, otherwise play regular attack
                if (isWalking && animController != null)
                {
                    animController.SetWalkAttacking(true);
                    
                    // Start a coroutine to turn off the walkAttack after animation completes
                    StartCoroutine(ResetWalkAttackAfterDelay());
                }
                // Regular attack is already handled by livingEntity.TryAttack()
            }
            else if (uiHelper != null)
            {
                // Handle cooldown as before
                float remainingCooldown = livingEntity.RemainingAttackCooldown;
                if (remainingCooldown > 0)
                {
                    StartCoroutine(UpdateCooldownTimerText(remainingCooldown));
                }
            }
        }
    }

    // Add this coroutine to reset walkAttack bool after animation completes
    private IEnumerator ResetWalkAttackAfterDelay()
    {
        // Wait for attack animation duration (adjust this value to match your animation length)
        float attackDuration = 0.5f; // Typical attack animation length in seconds
        yield return new WaitForSeconds(attackDuration);
        
        // Reset walkAttack bool
        if (animController != null)
        {
            animController.SetWalkAttacking(false);
        }
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

    // Add this method to manually trigger collision detection since triggers might not be working
    private void FixedUpdate()
    {
        // Manual overlap check for map borders
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.0f);
        bool foundBorder = false;
        
        foreach (Collider collider in hitColliders)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
            {
                foundBorder = true;
                
                // We've found a map border through manual detection
                if (!isAtMapBorder)
                {
                    Debug.LogWarning($"Manual detection found map border: {collider.name}");
                    isAtMapBorder = true;
                    
                    // Calculate direction from closest point
                    Vector3 closestPoint = collider.ClosestPoint(transform.position);
                    borderNormal = (transform.position - closestPoint).normalized;
                    
                    if (borderNormal.magnitude < 0.1f)
                    {
                        borderNormal = transform.position - collider.transform.position;
                        borderNormal.y = 0;
                        borderNormal = borderNormal.normalized;
                    }
                    
                    // Show message if needed
                    if (showBorderMessages && Time.time >= lastBorderMessageTime + borderMessageCooldown)
                    {
                        if (uiHelper != null)
                        {
                            uiHelper.ShowInformText("You've reached the boundary of the playable area!");
                        }
                        
                        HapticFeedback.LightFeedback();
                        lastBorderMessageTime = Time.time;
                    }
                }
                
                // Always update the normal and stop movement when near a border
                Vector3 newClosestPoint = collider.ClosestPoint(transform.position);
                Vector3 newBorderNormal = (transform.position - newClosestPoint).normalized;
                
                if (newBorderNormal.magnitude > 0.1f)
                {
                    borderNormal = newBorderNormal;
                }
                
                StopMovementTowardsBorder();
                break;
            }
        }
        
        // If we no longer detect a border but isAtMapBorder is true, reset it
        if (!foundBorder && isAtMapBorder)
        {
            Debug.Log("No longer detecting map border, resetting border state");
            isAtMapBorder = false;
            borderNormal = Vector3.zero;
        }
    }

    // Enhanced StopMovementTowardsBorder method that works even without proper trigger detection
    private void StopMovementTowardsBorder()
    {
        // Ensure borderNormal is valid
        if (borderNormal.magnitude < 0.1f)
        {
            Debug.LogWarning("Border normal is invalid in StopMovementTowardsBorder!");
            return;
        }
        
        // First handle rigidbody velocity
        if (cachedRigidbody != null && !cachedRigidbody.isKinematic)
        {
            float movementThroughBorder = Vector3.Dot(cachedRigidbody.velocity, -borderNormal);
            
            if (movementThroughBorder > 0)
            {
                Vector3 cancelVelocity = -borderNormal * movementThroughBorder;
                cachedRigidbody.velocity += cancelVelocity;
                
                // Add slight push away from border
                cachedRigidbody.velocity += borderNormal * 0.3f;
                
                Debug.Log($"Stopping rigidbody velocity toward border. Before: {cachedRigidbody.velocity - cancelVelocity}, After: {cachedRigidbody.velocity}");
            }
        }
        
        // Add fallback position correction to ensure the player doesn't pass through
        if (isAtMapBorder)
        {
            // Calculate current movement toward border
            float movingThroughBorder = Vector3.Dot(moveDirection, -borderNormal);
            
            if (movingThroughBorder > 0)
            {
                // Completely cancel this movement direction
                moveDirection = Vector3.zero;
                currentSpeed = 0;
                
                // Force the player's position to move slightly away from the border
                transform.position += borderNormal * 0.05f;
                
                Debug.Log("Force-stopping player at border and pushing back slightly");
            }
        }
    }
}