using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float moveSpeed = 8f; // Increased from 5f
    [SerializeField] private float rotationSpeed = 25f; // Increased from 20f
    [SerializeField] private float alignmentSpeed = 25f; // Increased from 20f
    [SerializeField] private float maxSlopeAngle = 85f; // Increased from 75f
    
    [Header("Physics Settings")]
    [SerializeField] private float surfaceAdhesionForce = 100f; // Doubled from 50f
    [SerializeField] private float gravityForce = 50f; // Increased from 30f
    [SerializeField] private float slopeGravityMultiplier = 8f; // Increased from 5f
    [SerializeField] private float groundedVelocityY = -1f; // Small downward velocity when grounded
    
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayLength = 2.0f; // Increased from 1.5f
    [SerializeField] private float maxGroundDistance = 0.8f; // Increased from 0.5f
    
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
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
            
        // Configure rigidbody for insect-like movement
        if (rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
            rb.mass = 0.5f; // Lighter mass for better responsiveness
            rb.drag = 0.5f; // Less drag for smoother movement
        }
    }
    
    private void Update()
    {
        if (animController != null && animController.IsAnimationPlaying("death"))
            return;
            
        CheckGroundNormal();
        HandleInput();
        HandleActions();
    }
    
    private void FixedUpdate()
    {
        ApplySurfaceAdhesion();
        ApplyMovement();
        AlignToSurface();
    }
    
    private void CheckGroundNormal()
    {
        Vector3 averageNormal = Vector3.zero;
        int hitCount = 0;
        bool foundGround = false;
        float closestDistance = float.MaxValue;
        
        // Use more raycasts for better detection
        for (int i = 0; i < 9; i++)
        {
            Vector3 rayStart = transform.position;
            Vector3 rayDir = -transform.up;
            
            // Create a 3x3 grid pattern of raycasts
            float offset = 0.3f;
            if (i % 3 == 0) rayStart += transform.forward * offset;
            if (i % 3 == 2) rayStart += -transform.forward * offset;
            if (i / 3 == 0) rayStart += transform.right * offset;
            if (i / 3 == 2) rayStart += -transform.right * offset;
            
            RaycastHit hit;
            if (Physics.Raycast(rayStart, rayDir, out hit, rayLength, groundLayer))
            {
                averageNormal += hit.normal;
                hitCount++;
                
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
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
            isGrounded = foundGround && closestDistance <= maxGroundDistance;
        }
        else
        {
            isGrounded = false;
            surfaceNormal = Vector3.up;
        }
    }
    
    private void HandleInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
        {
            moveDirection = Vector3.zero;
            animController?.SetWalking(false);
            return;
        }
        
        if (animController != null && 
            (animController.IsAnimationPlaying("eat") || animController.IsAnimationPlaying("attack")))
        {
            moveDirection = Vector3.zero;
            return;
        }
        
        // Get camera directions
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        
        // Project onto surface plane
        Vector3 projectedForward = Vector3.ProjectOnPlane(cameraForward, surfaceNormal).normalized;
        Vector3 projectedRight = Vector3.ProjectOnPlane(cameraRight, surfaceNormal).normalized;
        
        // Calculate movement direction
        moveDirection = (projectedForward * vertical + projectedRight * horizontal).normalized;
        
        animController?.SetWalking(true);
    }
    
    private void ApplyMovement()
    {
        if (!isGrounded)
        {
            // When in air, just apply gravity and don't override existing velocity
            return;
        }
        
        Vector3 targetVelocity;
        
        if (moveDirection != Vector3.zero)
        {
            // Calculate target velocity along the surface
            targetVelocity = moveDirection * moveSpeed;
            
            // Rotate to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, surfaceNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // When not moving, maintain position
            targetVelocity = Vector3.zero;
        }
        
        // Always apply a small downward velocity when grounded to maintain contact
        targetVelocity += -surfaceNormal * groundedVelocityY;
        
        // Apply the velocity directly
        rb.velocity = targetVelocity;
    }
    
    private void ApplySurfaceAdhesion()
    {
        if (isGrounded)
        {
            // Strong adhesion force toward the surface
            Vector3 adhesionForce = -surfaceNormal * surfaceAdhesionForce;
            rb.AddForce(adhesionForce, ForceMode.Acceleration);
            
            // Apply slope gravity
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, surfaceNormal).normalized;
            float slopeAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            float slopeFactor = slopeAngle / 90.0f;
            
            rb.AddForce(slopeDirection * slopeFactor * slopeGravityMultiplier, ForceMode.Acceleration);
        }
        else
        {
            // Strong gravity when not grounded
            rb.AddForce(Vector3.down * gravityForce, ForceMode.Acceleration);
        }
    }
    
    private void AlignToSurface()
    {
        if (!isGrounded)
            return;
            
        Quaternion targetRotation;
        
        if (moveDirection.magnitude > 0.1f)
        {
            // When moving, face movement direction and align to surface
            targetRotation = Quaternion.LookRotation(moveDirection, surfaceNormal);
        }
        else
        {
            // When stationary, maintain forward direction but align to surface
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;
            if (forward.magnitude < 0.1f)
                forward = Vector3.ProjectOnPlane(transform.right, surfaceNormal).normalized;
                
            targetRotation = Quaternion.LookRotation(forward, surfaceNormal);
        }
        
        // Apply rotation with high alignment speed
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.deltaTime);
    }
    
    private void HandleActions()
    {
        if (animController == null)
            return;
            
        // Attack on left mouse button
        if (Input.GetMouseButtonDown(0) && !animController.IsAnimationPlaying("attack"))
        {
            animController.SetAttacking(true);
            Invoke("StopAttacking", 1.0f);
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
        if (animController != null)
            animController.SetAttacking(false);
    }
    
    // Optional: Add this method to help with debugging
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), "Grounded: " + isGrounded);
        GUI.Label(new Rect(10, 30, 300, 20), "Surface Normal: " + surfaceNormal.ToString("F2"));
        GUI.Label(new Rect(10, 50, 300, 20), "Slope Angle: " + Vector3.Angle(surfaceNormal, Vector3.up).ToString("F1") + "°");
    }
}