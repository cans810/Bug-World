using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float followDistance = 2.0f;
    [SerializeField] private float minDistanceToPlayer = 1.0f;
    [SerializeField] private float maxDistanceToPlayer = 5.0f;
    [SerializeField] private float followSpeed = 1.5f;
    [SerializeField] private float positionSmoothTime = 0.5f;
    
    [Header("Movement Settings")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Combat")]
    [SerializeField] private bool assistInCombat = true;
    [SerializeField] private float attackDistance = 2.0f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // References
    private LivingEntity livingEntity;
    private AnimationController animController;
    private PlayerController playerController;
    
    // Movement variables
    private Vector3 currentVelocity;
    private float currentSpeed;
    private Vector3 targetPosition;
    
    // Position smoothing
    private Vector3 smoothDampVelocity = Vector3.zero;
    
    // State tracking
    private bool isMoving = false;
    private bool isFollowingPlayer = true;
    
    private void Start()
    {
        // Find the player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerController = player.GetComponent<PlayerController>();
            }
            else
            {
                Debug.LogError("AllyAI: Player not found! Ally will not move.");
                enabled = false;
                return;
            }
        }
        
        // Get required components
        livingEntity = GetComponent<LivingEntity>();
        animController = GetComponent<AnimationController>();
        
        if (livingEntity == null)
        {
            Debug.LogError("AllyAI: LivingEntity component not found! Ally will have limited functionality.");
        }
        
        if (animController == null)
        {
            Debug.LogWarning("AllyAI: AnimationController not found. Animations will not play.");
        }
        
        // Initialize position
        if (playerTransform != null)
        {
            targetPosition = playerTransform.position - playerTransform.forward * followDistance;
        }
    }
    
    private void Update()
    {
        if (playerTransform == null || playerController == null)
            return;
            
        // Skip if dead
        if (livingEntity != null && livingEntity.IsDead)
            return;
            
        // Movement behavior
        FollowPlayer();
        
        // Combat behavior
        if (assistInCombat)
        {
            AssistPlayerInCombat();
        }
    }
    
    private void FollowPlayer()
    {
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Determine if we need to move
        bool shouldMove = (distanceToPlayer > maxDistanceToPlayer);
        
        // If player is moving, adjust the target position based on player's movement direction
        bool playerIsMoving = false;
        Vector3 playerMovementDirection = Vector3.zero;
        
        if (playerController != null)
        {
            playerMovementDirection = playerController.GetMovementDirection();
            playerIsMoving = playerMovementDirection.magnitude > 0.1f;
        }
        
        // Calculate the ideal position - behind the player
        Vector3 idealPosition;
        
        if (playerIsMoving)
        {
            // When player is moving, target a position behind them based on their direction
            idealPosition = playerTransform.position - playerMovementDirection.normalized * followDistance;
        }
        else
        {
            // When player is stationary, use a position behind them based on their facing
            idealPosition = playerTransform.position - playerTransform.forward * followDistance;
        }
        
        // Smooth the target position for more natural movement
        targetPosition = Vector3.SmoothDamp(targetPosition, idealPosition, ref smoothDampVelocity, positionSmoothTime);
        
        // Calculate direction to target
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0; // Keep movement on the horizontal plane
        float distanceToTarget = directionToTarget.magnitude;
        
        // Only move if we're not too close to the player and not too close to the target
        if (shouldMove || (distanceToTarget > minDistanceToPlayer && distanceToPlayer > minDistanceToPlayer))
        {
            // Calculate target speed based on distance
            float targetSpeed = followSpeed;
            
            // Adjust speed based on distance to player (move faster when far away)
            if (distanceToPlayer > maxDistanceToPlayer * 1.5f)
            {
                targetSpeed *= 1.5f; // Speed up when too far
            }
            else if (distanceToTarget < minDistanceToPlayer * 2f)
            {
                targetSpeed *= 0.5f; // Slow down when getting close
            }
            
            // Smoothly adjust current speed
            float accelerationFactor = acceleration * Time.deltaTime;
            float decelerationFactor = deceleration * Time.deltaTime;
            
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 
                targetSpeed > currentSpeed ? accelerationFactor : decelerationFactor);
            
            // Only move if we have a significant direction
            if (directionToTarget.magnitude > 0.1f)
            {
                // Rotate towards the direction
                if (livingEntity != null)
                {
                    livingEntity.RotateTowards(directionToTarget, rotationSpeed);
                }
                else
                {
                    // Fallback rotation
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                
                // Move towards the target
                if (livingEntity != null)
                {
                    livingEntity.MoveInDirection(transform.forward, currentSpeed / followSpeed);
                }
                else
                {
                    // Fallback movement
                    transform.position += transform.forward * currentSpeed * Time.deltaTime;
                }
                
                isMoving = true;
            }
            else
            {
                isMoving = false;
            }
        }
        else
        {
            // We're close enough, stop moving
            currentSpeed = 0;
            isMoving = false;
        }
        
        // Update animation state
        if (animController != null)
        {
            bool shouldBeWalking = isMoving && currentSpeed > 0.1f;
            if (shouldBeWalking != animController.IsAnimationPlaying("walk"))
            {
                animController.SetWalking(shouldBeWalking);
            }
        }
        
        // Debug info
        if (showDebugInfo)
        {
            Debug.DrawLine(transform.position, targetPosition, Color.yellow);
            Debug.DrawLine(transform.position, playerTransform.position, Color.blue);
            Debug.DrawRay(transform.position, transform.forward * 2f, Color.green);
        }
    }
    
    private void AssistPlayerInCombat()
    {
        // Skip if we can't attack
        if (livingEntity == null || !livingEntity.HasTargetsInRange())
            return;
            
        // Try to get a target to attack
        LivingEntity target = livingEntity.GetClosestValidTarget();
        
        if (target != null && !target.IsDead)
        {
            // Calculate distance to target
            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            
            // If within attack distance, try to attack
            if (distanceToTarget <= attackDistance)
            {
                // Look at target
                Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
                directionToTarget.y = 0;
                
                livingEntity.RotateTowards(directionToTarget, rotationSpeed * 2f);
                
                // Try to attack
                if (livingEntity.RemainingAttackCooldown <= 0)
                {
                    // Perform attack
                    livingEntity.TryAttack();
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Ally attacking target: {target.name}");
                    }
                }
            }
        }
    }
    
    // Public methods for external control
    
    public void SetFollowDistance(float distance)
    {
        followDistance = Mathf.Max(0.5f, distance);
    }
    
    public void SetFollowSpeed(float speed)
    {
        followSpeed = Mathf.Max(0.5f, speed);
    }
    
    public void SetAssistInCombat(bool assist)
    {
        assistInCombat = assist;
    }
    
    public void ToggleFollowPlayer()
    {
        isFollowingPlayer = !isFollowingPlayer;
    }
    
    // Method to manually set the ally's target position
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
    }
}
