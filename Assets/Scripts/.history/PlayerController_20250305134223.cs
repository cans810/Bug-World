using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Ant Movement Settings")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float bodyWobbleAmount = 0.05f; // Subtle body wobble when moving
    [SerializeField] private float bodyWobbleSpeed = 8f; // Speed of body wobble
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Animation")]
    [SerializeField] private AnimationController animController;
    
    [Header("Combat")]
    [SerializeField] private LivingEntity livingEntity;
    
    // Movement variables
    private Vector3 moveDirection;
    private float currentSpeed;
    private float wobbleTime;
    private Vector3 originalBodyPosition;
    private Transform bodyTransform; // Optional: if you have a separate body mesh
    
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
            
        originalBodyPosition = bodyTransform.localPosition;
        
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
        // Disable player input and movement
        enabled = false;
        
        // Any player-specific death handling can go here
        // The animation transition is already handled by LivingEntity
    }
    
    private void Update()
    {
        // Don't allow control if dead
        if (animController != null && animController.IsAnimationPlaying("death"))
            return;
            
        HandleInput();
        HandleMovement();
        ApplyAntMovementEffects();
        HandleActions();
    }
    
    private void HandleInput()
    {
        // Get input axes
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Skip if eating or attacking
        if (animController != null && 
            (animController.IsAnimationPlaying("eat") || animController.IsAnimationPlaying("attack")))
        {
            moveDirection = Vector3.zero;
            return;
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
            
            // Note: SetAnimationSpeed calls removed as they're not supported
        }
        
        // If we have movement input, update rotation
        if (moveDirection.magnitude > 0.1f)
        {
            // Ants turn in a more segmented way - slightly more abrupt
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Apply movement with current speed
        if (currentSpeed > 0.01f)
        {
            Vector3 motion = moveDirection.normalized * currentSpeed * Time.deltaTime;
            transform.position += motion;
        }
    }
    
    private void ApplyAntMovementEffects()
    {
        // Only apply effects when moving
        if (currentSpeed > 0.1f)
        {
            // Update wobble time
            wobbleTime += Time.deltaTime * bodyWobbleSpeed * (currentSpeed / moveSpeed);
            
            // Calculate wobble offset - ants have a slight side-to-side motion when walking
            float xOffset = Mathf.Sin(wobbleTime * 2f) * bodyWobbleAmount * (currentSpeed / moveSpeed);
            float yOffset = Mathf.Abs(Mathf.Sin(wobbleTime)) * bodyWobbleAmount * 0.5f * (currentSpeed / moveSpeed);
            
            // Apply wobble to body
            if (bodyTransform != null && bodyTransform != transform)
            {
                bodyTransform.localPosition = originalBodyPosition + new Vector3(xOffset, yOffset, 0);
            }
        }
        else
        {
            // Reset body position when not moving
            if (bodyTransform != null && bodyTransform != transform)
            {
                bodyTransform.localPosition = originalBodyPosition;
            }
            
            // Reset wobble time
            wobbleTime = 0;
        }
    }
    
    private void HandleActions()
    {
        if (animController == null)
            return;
            
        // Attack on left mouse button - delegate to LivingEntity instead
        if (Input.GetMouseButtonDown(0) && !animController.IsAnimationPlaying("attack"))
        {
            if (livingEntity != null)
            {
                livingEntity.TryAttack();
            }
        }
        
        // Eat on E key press/release
        if (Input.GetKeyDown(KeyCode.E) && !animController.IsAnimationPlaying("attack"))
        {
            animController.SetEating(true);
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            animController.SetEating(false);
        }
        
        // Die on K key (for testing)
        if (Input.GetKeyDown(KeyCode.K) && !animController.IsAnimationPlaying("death"))
        {
            animController.SetDead();
        }
    }
}