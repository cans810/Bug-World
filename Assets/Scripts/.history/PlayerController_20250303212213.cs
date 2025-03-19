using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    
    [SerializeField] private PlayerAnimationController animController;
    
    private void Start()
    {
        // If no camera is assigned, try to find the main camera
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
            
        // Optional: Lock cursor for first-person view
        // Cursor.lockState = CursorLockMode.Locked;
        
        if (animController == null)
            animController = GetComponent<AnimationController>();
    }
    
    private void Update()
    {
        if (animController.IsAnimationPlaying("death"))
            return; // Don't allow control if dead
            
        HandleMovement();
        HandleActions();
    }
    
    private void HandleMovement()
    {
        // Get input axes
        float horizontal = Input.GetAxis("Horizontal"); // A and D keys
        float vertical = Input.GetAxis("Vertical");     // W and S keys
        
        // Calculate movement direction
        Vector3 movement = new Vector3(horizontal, 0f, vertical);
        
        // Normalize movement vector to prevent faster diagonal movement
        if (movement.magnitude > 1f)
            movement.Normalize();
            
        // Move the player if not eating or attacking
        if (movement.magnitude > 0.1f && !animController.IsAnimationPlaying("eat") && 
            !animController.IsAnimationPlaying("attack"))
        {
            transform.Translate(movement * moveSpeed * Time.deltaTime, Space.Self);
            transform.forward = movement.normalized; // Face movement direction
            
            // Set walking animation
            animController.SetWalking(true);
        }
        else if (!animController.IsAnimationPlaying("eat") && 
                 !animController.IsAnimationPlaying("attack"))
        {
            // Set idle animation when not moving and not performing other actions
            animController.SetWalking(false);
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