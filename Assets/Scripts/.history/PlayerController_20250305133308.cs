using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private float inputSmoothTime = 0.1f;
    [SerializeField] private float bodyWobbleAmount = 0.05f; // Subtle body wobble when moving
    [SerializeField] private float bodyWobbleSpeed = 8f; // Speed of body wobble
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private AnimationController animController;
    
    // Components
    private Rigidbody rb;
    private LivingEntity livingEntity;
    
    // Movement variables
    private Vector3 moveDirection;
    private float currentSpeed;
    private Vector2 smoothMoveVelocity;
    private float wobbleTime;
    private Vector3 originalBodyPosition;
    private Transform bodyTransform; // Optional: if you have a separate body mesh
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        livingEntity = GetComponent<LivingEntity>();
        
        if (livingEntity == null)
        {
            Debug.LogError("LivingEntity component missing from PlayerController!");
        }
        
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        if (animController == null)
            animController = GetComponent<AnimationController>();
            
        // Optional: If you have a separate body mesh transform
        bodyTransform = transform.Find("Body");
        if (bodyTransform == null)
            bodyTransform = transform; // Use main transform if no body found
            
        originalBodyPosition = bodyTransform.localPosition;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandlePlayerDeath);
        }
    }
    
    private void Start()
    {
        // Subscribe to death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(HandlePlayerDeath);
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
        if (livingEntity == null) return;
        
        // Get movement speed from LivingEntity
        float moveSpeed = livingEntity.GetModifiedMoveSpeed();
        float rotationSpeed = livingEntity.GetModifiedRotationSpeed();
        
        // Calculate target speed based on input magnitude
        float targetSpeed = moveDirection.magnitude * moveSpeed;
        
        // Smoothly adjust current speed
        if (targetSpeed > currentSpeed)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 8f * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 10f * Time.deltaTime);
        
        // Update animation state
        if (animController != null)
        {
            animController.SetWalking(currentSpeed > 0.1f);
        }
        
        // If we have movement input, update rotation
        if (moveDirection.magnitude > 0.1f)
        {
            // Rotation
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
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
            wobbleTime += Time.deltaTime * bodyWobbleSpeed * (currentSpeed / livingEntity.GetModifiedMoveSpeed());
            
            // Calculate wobble offset - ants have a slight side-to-side motion when walking
            float xOffset = Mathf.Sin(wobbleTime * 2f) * bodyWobbleAmount * (currentSpeed / livingEntity.GetModifiedMoveSpeed());
            float yOffset = Mathf.Abs(Mathf.Sin(wobbleTime)) * bodyWobbleAmount * 0.5f * (currentSpeed / livingEntity.GetModifiedMoveSpeed());
            
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
            
        // Attack on left mouse button - delegate to LivingEntity if possible
        if (Input.GetMouseButtonDown(0) && !animController.IsAnimationPlaying("attack"))
        {
            animController.SetAttacking(true);
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
    }
}