using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float alignmentSpeed = 20f;
    [SerializeField] private float maxSlopeAngle = 75f;
    [SerializeField] private float surfaceAdhesionForce = 20f; // Force pushing character to surface
    
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayLength = 1.5f;
    [SerializeField] private float rayOffset = 0.1f;
    [SerializeField] private float maxGroundDistance = 0.5f; // Max distance to be considered "on ground"
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Components")]
    [SerializeField] private AnimationController animController;
    
    private Rigidbody rb;
    private Vector3 moveDirection;
    private Vector3 surfaceNormal = Vector3.up;
    private bool isGrounded;
    private RaycastHit groundHit;
    
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
            
        // Configure rigidbody for insect-like movement
        if (rb != null)
        {
            rb.useGravity = false; // We'll apply our own gravity/adhesion
            rb.constraints = RigidbodyConstraints.None; // Allow full rotation
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
        // Apply surface adhesion (stick to surfaces like an insect)
        ApplySurfaceAdhesion();
        
        // Apply movement in FixedUpdate for physics consistency
        ApplyMovement();
        
        // Align to surface - insects fully align their bodies to surfaces
        AlignToSurface();
    }
    
    private void CheckGroundNormal()
    {
        // Use multiple raycasts for better surface detection
        Vector3 averageNormal = Vector3.zero;
        float closestDistance = float.MaxValue;
        int hitCount = 0;
        bool foundGround = false;
        
        // Cast rays in a pattern
        for (int i = 0; i < 5; i++)
        {
            Vector3 rayStart = transform.position;
            Vector3 rayDir = -transform.up; // Cast in the direction opposite to the character's up
            
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
                
                // Keep track of the closest hit
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    groundHit = hit;
                    foundGround = true;
                }
                
                Debug.DrawRay(rayStart, rayDir * hit.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(rayStart, rayDir * rayLength, Color.red);
            }
        }
        
        if (hitCount > 0)
        {
            surfaceNormal = (averageNormal / hitCount).normalized;
            
            // Check if slope is too steep
            float slopeAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            if (slopeAngle > maxSlopeAngle)
            {
                // For insects, we don't reset to up - they can walk on any surface
                // But we might want to limit extremely steep angles
                surfaceNormal = Vector3.Slerp(Vector3.up, surfaceNormal, maxSlopeAngle / 180f);
            }
            
            // We're grounded if we're close enough to the surface
            isGrounded = foundGround && closestDistance <= maxGroundDistance;
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
        
        // Get camera forward and right vectors
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        
        // Project camera directions onto the surface plane
        Vector3 projectedForward = Vector3.ProjectOnPlane(cameraForward, surfaceNormal).normalized;
        Vector3 projectedRight = Vector3.ProjectOnPlane(cameraRight, surfaceNormal).normalized;
        
        // Calculate movement direction relative to camera and surface
        moveDirection = (projectedForward * vertical + projectedRight * horizontal).normalized;
        
        // Set walking animation
        animController.SetWalking(true);
    }
    
    private void ApplyMovement()
    {
        if (moveDirection != Vector3.zero && isGrounded)
        {
            // Rotate to face movement direction while respecting surface normal
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, surfaceNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // Move along the surface
            Vector3 velocity = moveDirection * moveSpeed;
            
            // Apply the velocity - for insects, we move along the surface
            rb.velocity = velocity;
        }
        else
        {
            // When not moving intentionally, maintain position relative to surface
            rb.velocity = Vector3.zero;
        }
    }
    
    private void ApplySurfaceAdhesion()
    {
        if (isGrounded)
        {
            // Calculate the adhesion force (toward the surface)
            Vector3 adhesionForce = -surfaceNormal * surfaceAdhesionForce;
            
            // Apply the force to stick to the surface
            rb.AddForce(adhesionForce, ForceMode.Acceleration);
            
            // Optional: Apply a slight force along the surface in the direction of the slope
            // This simulates gravity affecting the insect on slopes
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, surfaceNormal).normalized;
            float slopeAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            float slopeFactor = slopeAngle / 90.0f; // 0 on flat, 1 on vertical
            
            rb.AddForce(slopeDirection * slopeFactor * 2f, ForceMode.Acceleration);
        }
        else
        {
            // When not on a surface, apply normal gravity
            rb.AddForce(Physics.gravity, ForceMode.Acceleration);
        }
    }
    
    private void AlignToSurface()
    {
        if (isGrounded)
        {
            // For insects, we want to fully align to the surface at all times
            Quaternion targetRotation;
            
            if (moveDirection.magnitude > 0.1f)
            {
                // When moving, face movement direction and align up to surface normal
                targetRotation = Quaternion.LookRotation(moveDirection, surfaceNormal);
            }
            else
            {
                // When stationary, maintain forward direction but align up to surface normal
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;
                if (forward.magnitude < 0.1f) // If forward would be zero, use another direction
                    forward = Vector3.ProjectOnPlane(transform.right, surfaceNormal).normalized;
                
                targetRotation = Quaternion.LookRotation(forward, surfaceNormal);
            }
            
            // Apply rotation with high alignment speed for insect-like behavior
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.deltaTime);
        }
    }
    
    private void HandleActions()
    {
        // Attack on left mouse button
        if (Input.GetMouseButtonDown(0) && !animController.IsAnimationPlaying("attack"))
        {
            animController.SetAttacking(true);
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