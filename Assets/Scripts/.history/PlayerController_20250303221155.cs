using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float alignmentSpeed = 20f;
    [SerializeField] private float maxSlopeAngle = 75f;
    
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayLength = 1.5f;
    [SerializeField] private float rayOffset = 0.1f;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Components")]
    [SerializeField] private AnimationController animController;
    
    private Rigidbody rb;
    private Vector3 moveDirection;
    private Vector3 surfaceNormal = Vector3.up;
    private bool isGrounded;
    
    private void Start()
    {
        // Get required components
        rb = GetComponent<Rigidbody>();
        
        // If no camera is assigned, try to find the main camera
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
            
        // Optional: Lock cursor for first-person view
        // Cursor.lockState = CursorLockMode.Locked;
        
        if (animController == null)
            animController = GetComponent<AnimationController>();
            
        // Configure rigidbody for character movement
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent tipping over
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Smoother movement
        }
    }
    
    private void Update()
    {
        if (animController.IsAnimationPlaying("death"))
            return; // Don't allow control if dead
            
        // Check if we're on a surface and get its normal
        CheckGroundNormal();
        
        HandleInput();
        HandleActions();
    }
    
    private void FixedUpdate()
    {
        // Apply movement in FixedUpdate for physics consistency
        ApplyMovement();
        
        // Align to surface
        AlignToSurface();
    }
    
    private void CheckGroundNormal()
    {
        // Use multiple raycasts for better surface detection
        Vector3 averageNormal = Vector3.zero;
        int hitCount = 0;
        
        // Cast rays in a pattern
        for (int i = 0; i < 5; i++)
        {
            Vector3 rayStart = transform.position + Vector3.up * rayOffset;
            Vector3 rayDir = Vector3.down;
            
            // Create a pattern: center, front, back, left, right
            if (i == 1) rayStart += transform.forward * 0.3f;
            if (i == 2) rayStart += -transform.forward * 0.3f;
            if (i == 3) rayStart += transform.right * 0.3f;
            if (i == 4) rayStart += -transform.right * 0.3f;
            
            RaycastHit hit;
            if (Physics.Raycast(rayStart, rayDir, out hit, rayLength, groundLayer))
            {
                averageNormal += hit.normal;
                hitCount++;
                Debug.DrawRay(rayStart, rayDir * hit.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(rayStart, rayDir * rayLength, Color.red);
            }
        }
        
        if (hitCount > 0)
        {
            isGrounded = true;
            surfaceNormal = (averageNormal / hitCount).normalized;
            
            // Check if slope is too steep
            float slopeAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            if (slopeAngle > maxSlopeAngle)
            {
                surfaceNormal = Vector3.up; // Too steep, use up vector
            }
        }
        else
        {
            isGrounded = false;
            surfaceNormal = Vector3.up; // Default to up when not grounded
        }
    }
    
    private void HandleInput()
    {
        // Get input axes
        float horizontal = Input.GetAxis("Horizontal"); // A and D keys
        float vertical = Input.GetAxis("Vertical");     // W and S keys
        
        // Skip if no input
        if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
        {
            moveDirection = Vector3.zero;
            animController.SetWalking(false);
            return;
        }
        
        // Skip if eating or attacking
        if (animController.IsAnimationPlaying("eat") || animController.IsAnimationPlaying("attack"))
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
        Vector3 inputDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
        
        // Project the movement direction onto the surface plane
        moveDirection = Vector3.ProjectOnPlane(inputDirection, surfaceNormal).normalized;
        
        // Set walking animation
        animController.SetWalking(true);
    }
    
    private void ApplyMovement()
    {
        if (moveDirection != Vector3.zero)
        {
            // Rotate to face movement direction while respecting surface normal
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, surfaceNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // Move using physics (preserves y velocity for gravity)
            Vector3 targetVelocity = moveDirection * moveSpeed;
            
            // Preserve the current y velocity (for gravity)
            targetVelocity.y = rb.velocity.y;
            
            // Apply the velocity
            rb.velocity = targetVelocity;
        }
        else
        {
            // Stop horizontal movement but keep vertical velocity
            Vector3 velocity = rb.velocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.velocity = velocity;
        }
    }
    
    private void AlignToSurface()
    {
        // Only align rotation to surface if we're not moving
        // (when moving, ApplyMovement handles rotation)
        if (moveDirection.magnitude < 0.1f && isGrounded)
        {
            // Create rotation that aligns up direction with surface normal
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, surfaceNormal) * transform.rotation;
            
            // Smoothly rotate to target orientation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.deltaTime);
        }
    }
    
    private void HandleActions()
    {
        // Attack on left mouse button
        if (Input.GetMouseButtonDown(0) && !animController.IsAnimationPlaying("attack"))
        {
            animController.SetAttacking(true);
            // You might want to add a timer or animation event to set attacking back to false
            Invoke("StopAttacking", 1.0f); // Example: 1 second attack animation
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
    
    private void StopAttacking()
    {
        animController.SetAttacking(false);
    }
}