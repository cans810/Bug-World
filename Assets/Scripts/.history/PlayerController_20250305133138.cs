using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private float inputSmoothTime = 0.1f;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    
    // Components
    private PlayerInput playerInput;
    private Rigidbody rb;
    private Animator animator;
    private LivingEntity livingEntity;
    
    // Input variables
    private Vector2 moveInput;
    private Vector2 currentMovement;
    private Vector2 smoothMoveVelocity;
    
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        livingEntity = GetComponent<LivingEntity>();
        
        if (livingEntity == null)
        {
            Debug.LogError("LivingEntity component missing from PlayerController!");
        }
        
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }
    
    private void Update()
    {
        HandleMovementInput();
        UpdateAnimator();
    }
    
    private void FixedUpdate()
    {
        MovePlayer();
    }
    
    private void HandleMovementInput()
    {
        // Smooth input
        currentMovement = Vector2.SmoothDamp(
            currentMovement, 
            moveInput, 
            ref smoothMoveVelocity, 
            inputSmoothTime
        );
    }
    
    private void MovePlayer()
    {
        if (livingEntity == null) return;
        
        // Get movement speed from LivingEntity
        float speed = livingEntity.GetModifiedMoveSpeed();
        float rotationSpeed = livingEntity.GetModifiedRotationSpeed();
        
        // Calculate movement direction relative to camera
        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        
        Vector3 moveDirection = (cameraForward * currentMovement.y + cameraRight * currentMovement.x).normalized;
        
        // Only rotate if we're trying to move
        if (moveDirection.magnitude > 0.1f)
        {
            // Rotation
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.fixedDeltaTime
            );
            
            // Movement - only move forward in the direction we're facing
            Vector3 movement = transform.forward * speed * moveDirection.magnitude * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
        }
    }
    
    private void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", currentMovement.magnitude);
        }
    }
    
    // Input System callbacks
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
    
    public void OnJump(InputValue value)
    {
        // Handle jump input if needed
    }
    
    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            // Handle attack input
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
        }
    }
}