using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 3f;     // Distance to maintain from player
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 3f;      // Reduced default speed for smoother turning
    [SerializeField] private float rotationSmoothTime = 0.3f; // Added smoothing parameter
    private Quaternion currentRotationVelocity;             // For rotation smoothing
    private Quaternion targetRotation;                      // Store the target rotation
    
    [Header("Ally Avoidance")]
    [SerializeField] private bool enableAllyAvoidance = true;  // Toggle for ally avoidance
    [SerializeField] private float avoidanceRadius = 1.5f;     // How close allies can get
    [SerializeField] private float avoidanceStrength = 1.5f;   // How strongly allies push each other away
    [SerializeField] private LayerMask allyLayer;              // The layer that contains allies
    
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
        
        // Initialize rotation
        targetRotation = transform.rotation;
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
            
            // When stopped, gradually rotate to face the player
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            if (directionToPlayer.magnitude > 0.1f)
            {
                directionToPlayer.y = 0;
                targetRotation = Quaternion.LookRotation(directionToPlayer.normalized);
                ApplySmoothRotation();
            }
        }
        
        // Apply ally avoidance if enabled
        if (enableAllyAvoidance)
        {
            ApplyAllyAvoidance();
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
        
        // Set the target rotation based on movement direction
        if (moveDirection.magnitude > 0.01f)
        {
            targetRotation = Quaternion.LookRotation(moveDirection);
            
            // Apply smooth rotation
            ApplySmoothRotation();
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
    
    private void ApplyAllyAvoidance()
    {
        // Find all allies within avoidance radius
        Collider[] nearbyAllies = Physics.OverlapSphere(transform.position, avoidanceRadius, allyLayer);
        
        if (nearbyAllies.Length <= 1) // Just this ally or none
            return;
            
        Vector3 avoidanceDirection = Vector3.zero;
        int avoidanceCount = 0;
        
        foreach (Collider allyCollider in nearbyAllies)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
                
            // Calculate distance and direction away from other ally
            Vector3 otherAllyPos = allyCollider.transform.position;
            float distance = Vector3.Distance(transform.position, otherAllyPos);
            
            // Only avoid if actually too close
            if (distance < avoidanceRadius)
            {
                // Direction away from the other ally
                Vector3 directionAway = transform.position - otherAllyPos;
                
                // Handle case of exact overlap (unlikely but possible)
                if (directionAway.magnitude < 0.001f)
                {
                    // Generate a random direction
                    directionAway = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                }
                
                // Normalize and make y-axis zero to keep allies on the same plane
                directionAway.y = 0;
                directionAway.Normalize();
                
                // Scale force by distance (closer = stronger)
                float avoidScale = 1.0f - (distance / avoidanceRadius);
                directionAway *= avoidScale * avoidanceStrength;
                
                avoidanceDirection += directionAway;
                avoidanceCount++;
            }
        }
        
        // If we found allies to avoid
        if (avoidanceCount > 0)
        {
            // Average the avoidance direction
            avoidanceDirection /= avoidanceCount;
            
            // Apply avoidance movement
            if (livingEntity != null && avoidanceDirection.magnitude > 0.01f)
            {
                livingEntity.MoveInDirection(avoidanceDirection.normalized, 0.5f);
                hasAppliedMovementThisFrame = true;
            }
        }
    }
    
    private void ApplySmoothRotation()
    {
        // Apply a very smooth rotation using Slerp with adjustable parameters
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            rotationSpeed * Time.deltaTime
        );
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
        
        // Show ally avoidance radius
        if (enableAllyAvoidance)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
        }
    }
}
