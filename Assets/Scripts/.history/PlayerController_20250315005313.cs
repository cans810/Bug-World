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
    // Add reference to player inventory to check levels
    [SerializeField] private PlayerInventory playerInventory;

    // Movement variables
    private Vector3 moveDirection;
    private float currentSpeed;
    private Transform bodyTransform; // Optional: if you have a separate body mesh

    public Transform spawnPoint;
    
    // Add this variable to track if we're already showing a boundary message
    private bool isShowingBoundaryMessage = false;

    private void Start()
    {
        // Find references if not assigned
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
        
        // Find player inventory if not assigned
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();
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

    // Revised border detection system
    private void OnTriggerExit(Collider other)
    {
        // Check if the collider is in the MapBorder layer
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            // Only restrict exit if it's not MapBorder1
            if (other.gameObject.name != "MapBorder1")
            {
                // We're trying to exit the playable area
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
                    livingEntity.MoveInDirection(borderNormal, 0.5f);
                }
                else
                {
                    // Fallback direct position adjustment with stronger push
                    transform.position += borderNormal * 0.5f;
                }
                
                // Only show messages if enabled and not on cooldown
                if (showBorderMessages && Time.time >= lastBorderMessageTime + borderMessageCooldown)
                {
                    // Show the error message in debug console
                    Debug.LogError($"Player attempted to exit playable area: {other.gameObject.name}");
                    
                    // Display UI message if we have a UI helper
                    if (uiHelper != null)
                    {
                        // Show a more specific message based on the border
                        string message = GetBorderLevelRequirementMessage(other.gameObject.name);
                        uiHelper.ShowInformText(message);
                    }
                    
                    // Add haptic feedback for mobile
                    HapticFeedback.LightFeedback();
                    
                    // Set the cooldown
                    lastBorderMessageTime = Time.time;
                }
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider is in the MapBorder layer
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            // Always allow entry to MapBorder1
            if (other.gameObject.name == "MapBorder1")
            {
                isAtMapBorder = false;
                borderNormal = Vector3.zero;
                return;
            }
            
            // Check if player is allowed to enter this border based on level
            bool canEnter = CanEnterBorder(other.gameObject.name);
            
            if (!canEnter)
            {
                // Player is trying to enter a restricted area
                isAtMapBorder = true;
                
                // Calculate the direction to push the player back
                if (other is SphereCollider sphereCollider)
                {
                    // Get the center of the sphere in world space
                    Vector3 sphereCenter = other.transform.TransformPoint(sphereCollider.center);
                    // Calculate direction AWAY from the center of the next area
                    borderNormal = (transform.position - sphereCenter).normalized;
                }
                else
                {
                    // For non-sphere colliders, try to get closest point on the collider
                    Vector3 closestPoint = other.ClosestPoint(transform.position);
                    // Direction away from the border
                    borderNormal = (transform.position - closestPoint).normalized;
                }
                
                // Push player back
                if (livingEntity != null)
                {
                    livingEntity.MoveInDirection(borderNormal, 0.5f);
                }
                else
                {
                    transform.position += borderNormal * 0.5f;
                }
                
                // Show appropriate message
                if (showBorderMessages && Time.time >= lastBorderMessageTime + borderMessageCooldown)
                {
                    // Display message based on border
                    if (uiHelper != null)
                    {
                        string message = GetBorderLevelRequirementMessage(other.gameObject.name);
                        uiHelper.ShowInformText(message);
                    }
                    
                    // Add haptic feedback for mobile
                    HapticFeedback.LightFeedback();
                    
                    // Set the cooldown
                    lastBorderMessageTime = Time.time;
                }
            }
            else
            {
                // Player can enter this area, reset border status
                isAtMapBorder = false;
                borderNormal = Vector3.zero;
            }
        }
    }

    // Check if player can enter a specific border based on level
    private bool CanEnterBorder(string borderName)
    {
        // If it's MapBorder1, always allow entry
        if (borderName == "MapBorder1")
            return true;
        
        // Get player level
        int playerLevel = 1;
        if (playerInventory != null)
        {
            playerLevel = playerInventory.CurrentLevel;
        }
        
        // Check level requirements for different borders
        switch (borderName)
        {
            case "MapBorder2":
                return playerLevel >= 3;
            case "MapBorder3":
                return playerLevel >= 4;
            case "MapBorder4":
                return playerLevel >= 5;
            case "MapBorder5":
                return playerLevel >= 6;
            default:
                return false; // For unknown borders, deny entry
        }
    }

    // Get appropriate message for border level requirement
    private string GetBorderLevelRequirementMessage(string borderName)
    {
        switch (borderName)
        {
            case "MapBorder2":
                return "This area requires level 3";
            case "MapBorder3":
                return "This area requires level 4";
            case "MapBorder4":
                return "This area requires level 5";
            case "MapBorder5":
                return "This area requires level 6";
            default:
                return "This area is restricted";
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
}