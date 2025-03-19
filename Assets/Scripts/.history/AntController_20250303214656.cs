using UnityEngine;

public class AntController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float climbSpeed = 3f; // Speed when climbing steep surfaces
    [SerializeField] private float turnSpeed = 3f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float maxSlopeAngle = 75f;
    
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.3f;
    
    [Header("Components")]
    [SerializeField] private PlayerAnimationController animController;
    
    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isClimbing;
    private RaycastHit groundHit;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (animController == null)
            animController = GetComponent<PlayerAnimationController>();
            
        // Configure rigidbody for climbing
        rb.useGravity = false; // We'll apply our own gravity
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent tipping over
    }
    
    private void Update()
    {
        // Check if we're on a surface
        CheckGrounding();
        
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Calculate move direction relative to camera
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;
        
        // Project camera directions onto the plane defined by the surface normal
        if (isGrounded)
        {
            cameraForward = Vector3.ProjectOnPlane(cameraForward, groundHit.normal).normalized;
            cameraRight = Vector3.ProjectOnPlane(cameraRight, groundHit.normal).normalized;
        }
        
        // Calculate movement direction
        moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
        
        // Handle animations
        if (moveDirection.magnitude > 0.1f)
        {
            animController.SetWalking(true);
            
            // Rotate to face movement direction
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, isGrounded ? groundHit.normal : Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
        else
        {
            animController.SetWalking(false);
        }
        
        // Handle other inputs
        if (Input.GetMouseButtonDown(0))
        {
            animController.SetAttacking(true);
            Invoke("StopAttacking", 1.0f);
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            animController.SetEating(true);
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            animController.SetEating(false);
        }
    }
    
    private void FixedUpdate()
    {
        // Apply movement
        if (moveDirection.magnitude > 0.1f && !animController.IsAnimationPlaying("attack"))
        {
            // Determine speed based on whether we're climbing
            float currentSpeed = isClimbing ? climbSpeed : moveSpeed;
            
            // Move along the surface
            Vector3 movement = moveDirection * currentSpeed;
            
            // Apply movement
            rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
        }
        else
        {
            // Stop horizontal movement
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
        
        // Apply gravity relative to surface normal when grounded
        if (isGrounded)
        {
            // Calculate gravity direction (down the slope)
            Vector3 gravityDirection = Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
            
            // If on a steep slope, apply gravity down the slope
            float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            if (slopeAngle > 0)
            {
                rb.AddForce(gravityDirection * gravity * slopeAngle / 90.0f, ForceMode.Acceleration);
            }
        }
        else
        {
            // Apply normal gravity when in air
            rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        }
    }
    
    private void CheckGrounding()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out groundHit, groundCheckDistance, groundLayer);
        
        if (isGrounded)
        {
            // Check if we're on a climbable slope
            float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            isClimbing = slopeAngle > 45f && slopeAngle <= maxSlopeAngle;
        }
        else
        {
            isClimbing = false;
        }
    }
    
    private void StopAttacking()
    {
        animController.SetAttacking(false);
    }
}