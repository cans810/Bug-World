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
        // but also allow for slower movement with partial stick/key presses
        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();
    }
    
    private void HandleMovement()
    {
        // Calculate target speed based on input magnitude
        float targetSpeed = moveDirection.magnitude * livingEntity.moveSpeed;
        
        // Smoothly adjust current speed using acceleration/deceleration
        if (targetSpeed > currentSpeed)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, deceleration * Time.deltaTime);
        
        // Update animation state
        if (animController != null)
        {
            animController.SetWalking(currentSpeed > 0.1f);
        }
        
        // If we have movement input, update rotation
        if (moveDirection.magnitude > 0.1f)
        {
            // Ants turn in a more segmented way - slightly more abrupt
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, livingEntity.rotationSpeed * Time.deltaTime);
        }
        
        // Apply movement with current speed
        if (currentSpeed > 0.01f)
        {
            Vector3 motion = moveDirection.normalized * currentSpeed * Time.deltaTime;
            
            // Check if the new position would be inside the boundary before moving
            Vector3 newPosition = transform.position + motion;
            if (IsPositionWithinBoundary(newPosition))
            {
                transform.position += motion;
            }
            else
            {
                // If we would go outside boundary, try to slide along the boundary
                Vector3 safeMotion = CalculateSafeMotion(motion);
                if (safeMotion.magnitude > 0.01f)
                {
                    transform.position += safeMotion;
                }
                
                // Show boundary message
                if (uiHelper != null && uiHelper.informPlayerText != null)
                {
                    uiHelper.ShowInformText("You have not unlocked this part of the map yet.");

                }
            }
        }
    }
    
    // Check if a position is within the map boundary
    private bool IsPositionWithinBoundary(Vector3 position)
    {
        // Find all map boundaries in the scene
        MapBoundary[] boundaries = FindObjectsOfType<MapBoundary>();
        
        // Check against each boundary
        foreach (MapBoundary boundary in boundaries)
        {
            // Assuming MapBoundary has a method or property to check if a position is inside
            if (boundary.IsPointOutside(position))
            {
                return false;
            }
        }
        
        return true;
    }

    // Calculate a safe motion vector that slides along boundaries
    private Vector3 CalculateSafeMotion(Vector3 originalMotion)
    {
        // Try horizontal movement only
        Vector3 horizontalMotion = new Vector3(originalMotion.x, 0, 0);
        if (IsPositionWithinBoundary(transform.position + horizontalMotion))
        {
            return horizontalMotion;
        }
        
        // Try vertical movement only
        Vector3 verticalMotion = new Vector3(0, 0, originalMotion.z);
        if (IsPositionWithinBoundary(transform.position + verticalMotion))
        {
            return verticalMotion;
        }
        
        // If neither works, return zero motion
        return Vector3.zero;
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

    public void Attack()
    {
        // Existing attack code...
        
        // Add haptic feedback when player attacks
        HapticFeedback.MediumFeedback();
    }

    // Add this public method to be called by the Attack button
    public void OnAttackButtonPressed()
    {
        if (animController == null || animController.IsAnimationPlaying("attack") || 
            animController.IsAnimationPlaying("death"))
            return;
        
        if (livingEntity != null)
        {
            livingEntity.TryAttack();
            
            // Add haptic feedback when player attacks
            HapticFeedback.MediumFeedback();
        }
    }
}