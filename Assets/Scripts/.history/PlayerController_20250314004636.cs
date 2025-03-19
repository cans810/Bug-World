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
    [SerializeField] private UIHelper uiHelper;

    [Header("Mobile Controls")]
    [SerializeField] private OnScreenJoystick joystick;

    // Movement variables
    private Vector3 moveDirection;
    private float currentSpeed;
    private Transform bodyTransform; // Optional: if you have a separate body mesh

    public Transform spawnPoint;
    
    // Add this variable to track if we're already showing a boundary message
    private bool isShowingBoundaryMessage = false;

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
        
        // Use joystick input if available, otherwise use keyboard
        if (joystick != null && joystick.IsDragging)
        {
            horizontal = joystick.Horizontal;
            vertical = joystick.Vertical;
            
            // Add a small deadzone to prevent drift
            if (Mathf.Abs(horizontal) < 0.1f) horizontal = 0;
            if (Mathf.Abs(vertical) < 0.1f) vertical = 0;
        }
        else
        {
            // Traditional keyboard input
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
        }
        
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
                livingEntity.RotateTowards(moveDirection, 5.0f); // Increased from 2.0f
            }
        }
        
        // Calculate target speed based on input magnitude
        float targetSpeed = moveDirection.magnitude * livingEntity.moveSpeed;
        
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
        
        // Before applying movement, check if the new position would be within bounds
        Vector3 intendedPosition = transform.position + transform.forward * currentSpeed * Time.deltaTime;
        
        // Check if this position would be outside the boundary
        MapBoundary[] boundaries = FindObjectsOfType<MapBoundary>();
        bool wouldBeOutsideBoundary = false;
        
        foreach (MapBoundary boundary in boundaries)
        {
            if (!boundary.IsWithinBounds(intendedPosition))
            {
                wouldBeOutsideBoundary = true;
                break;
            }
        }
        
        if (wouldBeOutsideBoundary)
        {
            // If we would exit the boundary, calculate a sliding motion along the boundary
            // Find the nearest point on the boundary
            Vector3 nearestSafePosition = GetNearestBoundaryPosition(intendedPosition);
            
            // Move to that position instead
            if (Vector3.Distance(transform.position, nearestSafePosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, 
                    nearestSafePosition, 
                    currentSpeed * Time.deltaTime
                );
            }
            
            // Show a message to the player if not already showing one
            if (!isShowingBoundaryMessage && uiHelper != null)
            {
                StartCoroutine(ShowBoundaryMessage("You've reached the boundary of this area!"));
            }
        }
        else
        {
            // Normal movement
            Vector3 motion = transform.forward * currentSpeed * Time.deltaTime;
            transform.position += motion;
        }
    }
    
    private Vector3 GetNearestBoundaryPosition(Vector3 position)
    {
        MapBoundary[] boundaries = FindObjectsOfType<MapBoundary>();
        Vector3 nearestPoint = position;
        float nearestDistance = float.MaxValue;
        
        foreach (MapBoundary boundary in boundaries)
        {
            Vector3 boundaryPoint = boundary.GetNearestPointInBounds(position);
            float distance = Vector3.Distance(transform.position, boundaryPoint);
            
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPoint = boundaryPoint;
            }
        }
        
        return nearestPoint;
    }

    private IEnumerator ShowBoundaryMessage(string message)
    {
        isShowingBoundaryMessage = true;
        
        if (uiHelper != null)
            uiHelper.ShowInformText(message);
        
        yield return new WaitForSeconds(3.0f);
        
        isShowingBoundaryMessage = false;
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
        if (animController == null || animController.IsAnimationPlaying("attack") || 
            animController.IsAnimationPlaying("death"))
            return;
        
        if (livingEntity != null)
        {
            // Try to attack and get whether it was successful
            bool attackResult = livingEntity.TryAttack();
            
            // If attack failed due to cooldown, inform the player
            if (!attackResult && uiHelper != null)
            {
                // Get the remaining cooldown time (assuming LivingEntity has this property)
                float remainingCooldown = livingEntity.RemainingAttackCooldown;
                
                // Only show the message if cooldown is the reason (not something else like stamina)
                if (remainingCooldown > 0)
                {
                    // Format the cooldown time to one decimal place
                    string cooldownText = remainingCooldown.ToString("F1");
                    uiHelper.ShowInformText($"Attack on cooldown! Ready in {cooldownText}s");
                }
            }
            
            // Note: We no longer trigger haptic feedback here
            // The feedback will now be triggered when the attack actually lands
        }
    }

    // Add this method to be called when an attack successfully lands
    public void OnAttackLanded()
    {
        // Trigger haptic feedback when the attack actually lands
        HapticFeedback.MediumFeedback();
    }
}