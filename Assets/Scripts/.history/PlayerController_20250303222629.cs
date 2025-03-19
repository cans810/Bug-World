using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Animation")]
    [SerializeField] private AnimationController animController;
    
    private Vector3 moveDirection;
    
    private void Start()
    {
        // If no camera is assigned, try to find the main camera
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
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
        // Get input axes
        float horizontal = Input.GetAxis("Horizontal"); // A and D keys
        float vertical = Input.GetAxis("Vertical");     // W and S keys
        
        // Skip if no input
        if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
        {
            moveDirection = Vector3.zero;
            animController?.SetWalking(false);
            return;
        }
        
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
        moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
        
        // Set walking animation
        animController?.SetWalking(true);
    }
    
    private void HandleMovement()
    {
        if (moveDirection == Vector3.zero)
            return;
            
        // Move the character
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        
        // Rotate to face movement direction
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
    
    private void HandleActions()
    {
        if (animController == null)
            return;
            
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
        if (animController != null)
            animController.SetAttacking(false);
    }
}