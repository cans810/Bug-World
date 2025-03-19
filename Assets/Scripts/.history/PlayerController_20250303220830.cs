using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Components")]
    [SerializeField] private AnimationController animController;
    
    private Rigidbody rb;
    private Vector3 moveDirection;
    
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
            
        HandleInput();
        HandleActions();
    }
    
    private void FixedUpdate()
    {
        // Apply movement in FixedUpdate for physics consistency
        ApplyMovement();
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
        moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
        
        // Set walking animation
        animController.SetWalking(true);
    }
    
    private void ApplyMovement()
    {
        if (moveDirection != Vector3.zero)
        {
            // Rotate to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
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