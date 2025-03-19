using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Ant Movement Settings")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 10f;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Animation")]
    [SerializeField] private AnimationController animController;
    
    [Header("Combat")]
    [SerializeField] private LivingEntity livingEntity;
    
    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private bool resetInventoryOnDeath = false;
    
    // Movement variables
    private Vector3 moveDirection;
    private float currentSpeed;
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
        
        // Start the respawn coroutine
        StartCoroutine(RespawnAfterDelay());
    }
    
    private IEnumerator RespawnAfterDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(respawnDelay);
        
        // Respawn player at base
        RespawnPlayer();
    }
    
    private void RespawnPlayer()
    {
        // Check if respawn point is assigned
        if (respawnPoint == null)
        {
            Debug.LogWarning("No respawn point assigned! Using current position instead.");
            respawnPoint = transform;
        }
        
        // Move player to respawn point
        transform.position = respawnPoint.position;
        transform.rotation = respawnPoint.rotation;
        
        // Reset inventory if option enabled
        if (resetInventoryOnDeath)
        {
            PlayerInventory inventory = GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                // Reset counts to zero or a percentage of current values
                inventory.RemoveCrumb(inventory.CrumbCount);
                inventory.RemoveChitin(inventory.ChitinCount);
            }
        }
        
        // Restore health via LivingEntity
        if (livingEntity != null)
        {
            // Instead of RestoreFullHealth() which doesn't exist
            // Set current health to max health
            livingEntity.currentHealth = livingEntity.maxHealth;
        }
        
        // Reset movement state
        currentSpeed = 0f;
        moveDirection = Vector3.zero;
        
        // Reset animation state if needed
        if (animController != null)
        {
            animController.SetWalking(false);
            animController.SetEating(false);
            // Instead of ResetDeathState() which doesn't exist
            // Simply set death animation to false
            animController.SetDead(false); // Assuming SetDead can take a parameter to toggle
        }
        
        // Re-enable player control
        enabled = true;
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
        }
        
        // If we have movement input, update rotation
        if (moveDirection.magnitude > 0.1f)
        {
            // Ants turn in a more segmented way - slightly more abrupt
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, livingEntity.rotationSpeed * Time.deltaTime);
        }
        
        // Apply movement with current speed
        if (currentSpeed > 0.01f)
        {
            Vector3 motion = moveDirection.normalized * currentSpeed * Time.deltaTime;
            transform.position += motion;
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