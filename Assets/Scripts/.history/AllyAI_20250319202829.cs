using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 3f;     // Distance to maintain from player
    [SerializeField] private float rotationSpeed = 5f;      // How fast ally rotates
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal states
    private Transform playerTransform;
    private bool isMoving = false;
    
    // For compatibility with AIWandering.cs
    private bool hasAppliedMovementThisFrame = false;
    public bool HasAppliedMovementThisFrame => hasAppliedMovementThisFrame;
    
    private void Start()
    {
        // Find player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("AllyAI could not find player!");
        }
        
        // Get required components if not assigned
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
        
        // Subscribe to death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(OnDeath);
        }
    }
    
    private void Update()
    {
        // Reset movement flag
        hasAppliedMovementThisFrame = false;
        
        // Skip if no player or ally is dead
        if (playerTransform == null || livingEntity == null || livingEntity.IsDead)
            return;
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Determine if we need to move
        if (distanceToPlayer > followDistance)
        {
            // Move toward player
            MoveTowardPlayer();
        }
        else
        {
            // Stop moving
            StopMoving();
        }
    }
    
    private void MoveTowardPlayer()
    {
        // Get direction to player
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0; // Stay on the same vertical plane
        
        // Calculate target position (just before the follow distance)
        Vector3 targetPosition = playerTransform.position - (directionToPlayer.normalized * followDistance * 0.8f);
        
        // Calculate movement direction
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        
        // Rotate toward movement direction
        if (moveDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Apply movement using LivingEntity
        if (livingEntity != null)
        {
            livingEntity.MoveInDirection(moveDirection, 1.0f);
            hasAppliedMovementThisFrame = true;
            
            // Update animation
            if (!isMoving)
            {
                isMoving = true;
                if (animController != null)
                    animController.SetWalking(true);
            }
        }
    }
    
    private void StopMoving()
    {
        // Update state and animation
        if (isMoving)
        {
            isMoving = false;
            if (animController != null)
                animController.SetWalking(false);
        }
    }
    
    private void OnDeath()
    {
        // Stop all behavior
        isMoving = false;
        if (animController != null)
            animController.SetWalking(false);
        
        enabled = false;
    }
    
    private void OnDestroy()
    {
        // Clean up event listeners
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(OnDeath);
        }
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Show follow distance
        Gizmos.color = Color.green;
        if (playerTransform != null)
            Gizmos.DrawWireSphere(playerTransform.position, followDistance);
        else if (Application.isEditor && !Application.isPlaying)
        {
            // Try to find player in editor
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                Gizmos.DrawWireSphere(player.transform.position, followDistance);
        }
    }
}
