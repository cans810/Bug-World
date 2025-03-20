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
    [SerializeField] private bool enableAllyAvoidance = true;   // Toggle for ally avoidance
    [SerializeField] private float avoidanceRadius = 1.5f;      // How far to check for other allies
    [SerializeField] private float avoidanceStrength = 1.0f;    // How strongly to avoid (higher = stronger)
    [SerializeField] private LayerMask allyLayer;              // Set this to the "Ally" layer
    
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
        
        // Try to automatically set the ally layer if not set
        if (allyLayer == 0)
        {
            allyLayer = LayerMask.GetMask("Ally");
            if (allyLayer == 0)
            {
                Debug.LogWarning("AllyAI: Could not automatically find 'Ally' layer. Please set it manually in the inspector.");
            }
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
            
            // When stopped, gradually rotate to face the player
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            if (directionToPlayer.magnitude > 0.1f)
            {
                directionToPlayer.y = 0;
                targetRotation = Quaternion.LookRotation(directionToPlayer.normalized);
                ApplySmoothRotation();
            }
        }
        
        // Apply ally avoidance (even when stationary, to prevent allies stacking)
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
        // Find all nearby allies
        Collider[] nearbyAllies = Physics.OverlapSphere(transform.position, avoidanceRadius, allyLayer);
        
        if (nearbyAllies.Length <= 1) // Just us, or no allies detected
            return;
            
        Vector3 avoidanceDirection = Vector3.zero;
        int avoidanceCount = 0;
        
        foreach (Collider allyCollider in nearbyAllies)
        {
            // Skip ourselves
            if (allyCollider.gameObject == gameObject)
                continue;
                
            // Calculate direction away from other ally
            Vector3 directionAway = transform.position - allyCollider.transform.position;
            
            // If we're at the exact same position, move in a random direction
            if (directionAway.magnitude < 0.001f)
            {
                directionAway = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            }
            else
            {
                // Keep on the horizontal plane
                directionAway.y = 0;
                
                // Scale force by proximity (closer = stronger avoidance)
                float distance = directionAway.magnitude;
                float avoidanceFactor = 1.0f - Mathf.Clamp01(distance / avoidanceRadius);
                directionAway = directionAway.normalized * avoidanceFactor * avoidanceStrength;
            }
            
            avoidanceDirection += directionAway;
            avoidanceCount++;
        }
        
        // Apply avoidance if we have any
        if (avoidanceCount > 0 && avoidanceDirection.magnitude > 0.01f)
        {
            // Normalize the direction and apply it with reduced strength
            avoidanceDirection = avoidanceDirection.normalized * 0.5f;
            
            // Move using the LivingEntity component
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(avoidanceDirection, 0.5f);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
    }
}
