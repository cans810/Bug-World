using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack
    [SerializeField] private float detectionFollowRange = 10f; // Max range to follow player after detection
    
    [Header("Attack Settings")]
    [SerializeField] private float attackInterval = 1.5f; // Minimum time between attacks
    [SerializeField] private float minAttackDistance = 1.0f; // Minimum distance required to attack player
    
    [Header("Behavior Settings")]
    [SerializeField] private EnemyBehaviorMode behaviorMode = EnemyBehaviorMode.Aggressive; // Default behavior mode
    [SerializeField] private float aggroMemoryDuration = 30f; // How long the enemy remembers being attacked
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] public LivingEntity livingEntity;
    
    [Header("Nest Avoidance")]
    [SerializeField] private bool avoidPlayerNest = true;
    [SerializeField] private float nestAvoidanceDistance = 0f;
    [SerializeField] private string nestLayerName = "PlayerNest1";
    [SerializeField] private string nestObjectName = "Player Nest 1";
    
    // Internal states
    private bool isMoving = false;
    private float lastAttackTime = -999f;
    private LivingEntity currentTarget = null;
    private AIWandering wanderingBehavior;
    private Vector3 lastKnownPlayerPosition;
    private bool isChasing = false;
    private bool hasBeenAttacked = false;
    private float lastAttackedTime = -999f;
    
    private MapBoundary mapBoundary;
    
    private Transform playerNestTransform;
    private float nestRadius = 0f;
    
    // Enum to define different enemy behavior modes
    public enum EnemyBehaviorMode
    {
        Aggressive,  // Always attacks player on sight
        Passive,     // Only attacks if player attacks first
        Territorial  // Only attacks if player gets too close (future implementation)
    }
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
            
        // Get reference to the wandering behavior
        wanderingBehavior = GetComponent<AIWandering>();
        
        // Ensure entity is set to be destroyed after death
        if (livingEntity != null)
        {
            // Subscribe to death event
            livingEntity.OnDeath.AddListener(HandleDeath);
            
            // Subscribe to damage event to detect when attacked
            livingEntity.OnDamaged.AddListener(HandleDamaged);
            
            // Make sure destruction settings are properly configured
            livingEntity.SetDestroyOnDeath(true, 5f);
        }
        
        // Start idle
        UpdateAnimation(false);
        
        // Find the boundary
        mapBoundary = FindObjectOfType<MapBoundary>();
        
        // Find the player nest
        FindPlayerNest();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandleDeath);
            livingEntity.OnDamaged.RemoveListener(HandleDamaged);
        }
    }
    
    private void HandleDeath()
    {
        if (wanderingBehavior != null)
            wanderingBehavior.enabled = false;
        
        isMoving = false;
        isChasing = false;
        
        enabled = false;
    }
    
    private void HandleDamaged()
    {
        // Mark as attacked and store the time
        hasBeenAttacked = true;
        lastAttackedTime = Time.time;
        
        // If we're passive and get attacked, try to find the attacker
        if (behaviorMode == EnemyBehaviorMode.Passive)
        {
            // Since we don't have the attacker directly, assume it's the player or closest entity
            LivingEntity potentialAttacker = livingEntity.GetClosestValidTarget();
            if (potentialAttacker != null && !potentialAttacker.IsDead)
            {
                currentTarget = potentialAttacker;
                isChasing = true;
                lastKnownPlayerPosition = potentialAttacker.transform.position;
                
                // Disable wandering behavior while chasing
                if (wanderingBehavior != null)
                    wanderingBehavior.SetWanderingEnabled(false);
            }
        }
    }
    
    private void Update()
    {
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
        
        // Check if aggro has expired for passive enemies
        if (behaviorMode == EnemyBehaviorMode.Passive && hasBeenAttacked && 
            Time.time > lastAttackedTime + aggroMemoryDuration)
        {
            hasBeenAttacked = false;
            
            // If we're chasing but aggro expired, return to wandering
            if (isChasing && currentTarget != null)
            {
                ReturnToWandering();
            }
        }
        
        // Check if we have any targets in range detected by our hitbox
        if (livingEntity.HasTargetsInRange())
        {
            // Get the closest valid target from the livingEntity
            LivingEntity potentialTarget = livingEntity.GetClosestValidTarget();
            
            // If we found a target, decide whether to chase based on behavior mode
            if (potentialTarget != null && !potentialTarget.IsDead)
            {
                bool shouldChase = false;
                
                switch (behaviorMode)
                {
                    case EnemyBehaviorMode.Aggressive:
                        // Always chase
                        shouldChase = true;
                        break;
                        
                    case EnemyBehaviorMode.Passive:
                        // Only chase if we've been attacked
                        shouldChase = hasBeenAttacked;
                        break;
                        
                    // Add more behavior modes here as needed
                }
                
                if (shouldChase)
                {
                    // Set current target and start chasing
                    currentTarget = potentialTarget;
                    
                    // Disable wandering behavior while chasing
                    if (wanderingBehavior != null)
                        wanderingBehavior.SetWanderingEnabled(false);
                    
                    isChasing = true;
                    lastKnownPlayerPosition = currentTarget.transform.position;
                    
                    // Chase and attack logic
                    ChaseAndAttackTarget();
                }
                else
                {
                    // For passive enemies that haven't been attacked, just continue wandering
                    if (wanderingBehavior != null && !wanderingBehavior.IsCurrentlyMoving())
                        wanderingBehavior.SetWanderingEnabled(true);
                }
            }
            else
            {
                ReturnToWandering();
            }
        }
        // If we were chasing but lost the target, check if we should continue to last known position
        else if (isChasing)
        {
            float distanceToLastKnown = Vector3.Distance(transform.position, lastKnownPlayerPosition);
            
            // If we're still within follow range of the last known position, continue moving there
            if (distanceToLastKnown > attackDistance && distanceToLastKnown < detectionFollowRange)
            {
                isMoving = true;
                UpdateAnimation(true);
                
                // Face and move towards last known position
                FaceTarget(lastKnownPlayerPosition);
                
                // Check bounds before moving
                MoveWithBoundaryCheck(transform.forward);
                
                // If we've reached the last known position and still don't see the player, return to wandering
                if (distanceToLastKnown <= attackDistance)
                {
                    ReturnToWandering();
                }
            }
            else
            {
                ReturnToWandering();
            }
        }
    }
    
    private void FindPlayerNest()
    {
        // Find the player nest by name
        GameObject nestObject = GameObject.Find(nestObjectName);
        if (nestObject != null)
        {
            playerNestTransform = nestObject.transform;
            
            // Get the sphere collider to determine radius
            SphereCollider nestCollider = nestObject.GetComponent<SphereCollider>();
            if (nestCollider != null)
            {
                nestRadius = nestCollider.radius * Mathf.Max(
                    nestObject.transform.lossyScale.x,
                    nestObject.transform.lossyScale.y,
                    nestObject.transform.lossyScale.z
                );
                
                Debug.Log($"Found player nest with radius: {nestRadius}");
            }
            else
            {
                // Default radius if no collider found
                nestRadius = 5f;
                Debug.LogWarning("Player nest found but has no SphereCollider, using default radius");
            }
        }
        else
        {
            Debug.LogWarning($"Player nest '{nestObjectName}' not found in scene");
        }
    }
    
    // New method to check if a position is too close to the nest
    private bool IsPositionTooCloseToNest(Vector3 position)
    {
        if (!avoidPlayerNest || playerNestTransform == null)
            return false;
        
        float distanceToNest = Vector3.Distance(position, playerNestTransform.position);
        return distanceToNest < (nestRadius + nestAvoidanceDistance);
    }
    
    // New method to get a safe direction away from the nest
    private Vector3 GetDirectionAwayFromNest(Vector3 currentPosition)
    {
        if (playerNestTransform == null)
            return Vector3.zero;
        
        return (currentPosition - playerNestTransform.position).normalized;
    }
    
    // Modified method to handle movement with boundary and nest checking
    private void MoveWithBoundaryCheck(Vector3 direction)
    {
        // Check for very small movement to avoid normalization issues
        if (direction.magnitude < 0.1f)
        // Calculate the next position
        Vector3 nextPosition = transform.position + direction * livingEntity.moveSpeed * Time.deltaTime;
        
        bool isTooCloseToNest = IsPositionTooCloseToNest(nextPosition);
        bool isOutsideBoundary = mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition);
        
        if (isTooCloseToNest)
        {
            // Get direction away from nest
            Vector3 awayFromNest = GetDirectionAwayFromNest(transform.position);
            
            // Always stop chasing when hitting nest boundary
            if (isChasing)
            {
                // Turn to face away from the nest
                transform.forward = awayFromNest;
                
                // Return to wandering immediately
                ReturnToWandering();
                return;
            }
            else
            {
                // Just move away from nest
                transform.forward = awayFromNest;
                transform.position += awayFromNest * livingEntity.moveSpeed * Time.deltaTime;
            }
        }
        else if (isOutsideBoundary)
        {
            // Handle boundary as before
            Vector3 safePosition = mapBoundary.GetNearestPointInBounds(nextPosition);
            Vector3 redirectedDirection = (safePosition - transform.position).normalized;
            
            transform.forward = redirectedDirection;
            transform.position += redirectedDirection * livingEntity.moveSpeed * Time.deltaTime;
        }
        else
        {
            // Move normally if within bounds and not too close to nest
            transform.position += direction * livingEntity.moveSpeed * Time.deltaTime;
        }
    }
    
    // Modify ChaseAndAttackTarget to completely stop chasing when hitting nest boundary
    private void ChaseAndAttackTarget()
    {
        // If target is inside the nest and we're outside, don't chase further
        if (currentTarget != null && 
            playerNestTransform != null && 
            avoidPlayerNest &&
            IsPositionTooCloseToNest(currentTarget.transform.position) &&
            !IsPositionTooCloseToNest(transform.position))
        {
            // Stop at the edge of the nest
            isMoving = false;
            UpdateAnimation(false);
            
            // Turn away from the nest instead of facing the target
            Vector3 awayFromNest = GetDirectionAwayFromNest(transform.position);
            transform.forward = awayFromNest;
            
            // Immediately return to wandering instead of waiting
            ReturnToWandering();
            return;
        }

        // If we're too close to the nest ourselves, turn back and wander
        if (IsPositionTooCloseToNest(transform.position))
        {
            // Get direction away from nest
            Vector3 awayFromNest = GetDirectionAwayFromNest(transform.position);
            
            // Turn to face away from the nest
            transform.forward = awayFromNest;
            
            // Return to wandering
            ReturnToWandering();
            return;
        }

        // Original chase and attack logic
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        
        // If we're close enough to attack
        if (distanceToTarget <= minAttackDistance)
        {
            // Stop moving
            isMoving = false;
            UpdateAnimation(false);
            
            // Face the target
            FaceTarget(currentTarget.transform.position);
            
            // Try to attack if cooldown has passed
            if (Time.time >= lastAttackTime + attackInterval)
            {
                if (livingEntity.TryAttack())
                {
                    lastAttackTime = Time.time;
                }
            }
        }
        // Otherwise, move towards the target
        else
        {
            isMoving = true;
            UpdateAnimation(true);
            
            // Face and move towards target
            FaceTarget(currentTarget.transform.position);
            
            // Use the consistent movement method
            MoveInDirection(transform.forward);
        }
    }
    
    // Modify ReturnToWandering to be more thorough in resetting state
    private void ReturnToWandering()
    {
        isChasing = false;
        currentTarget = null;
        lastKnownPlayerPosition = Vector3.zero; // Reset last known position
        
        // Stop moving
        isMoving = false;
        UpdateAnimation(false);
        
        // Cancel any pending coroutines that might re-enable chasing
        StopAllCoroutines();
        
        // Re-enable wandering behavior
        if (wanderingBehavior != null)
        {
            wanderingBehavior.SetWanderingEnabled(true);
            // Force the wandering behavior to pick a new waypoint away from the nest
            wanderingBehavior.ForceNewWaypoint();
        }
    }
    
    // Helper method to update animations
    private void UpdateAnimation(bool isWalking)
    {
        if (animController != null)
        {
            animController.SetWalking(isWalking);
        }
    }
    
    private void FaceTarget(Vector3 target)
    {
        Vector3 directionToTarget = target - transform.position;
        directionToTarget.y = 0; // Keep rotation on Y axis only
        
        if (directionToTarget.magnitude > 0.1f)
        {
            // Use the centralized rotation method in LivingEntity
            livingEntity.RotateTowards(directionToTarget);
        }
    }
    
    // Add a consistent MoveInDirection method to standardize movement application
    private void MoveInDirection(Vector3 direction)
    {
        if (direction.magnitude < 0.1f)
            return;
        
        // Calculate movement with proper time delta
        Vector3 movement = direction * livingEntity.moveSpeed * Time.deltaTime;
        
        // Use Rigidbody for movement if available
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            // Use velocity-based movement for smoother motion
            Vector3 targetVelocity = direction * livingEntity.moveSpeed;
            rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
        }
        else
        {
            // Fallback to direct position update
            transform.position += movement;
        }
    }
    
    // Public method to set behavior mode at runtime
    public void SetBehaviorMode(EnemyBehaviorMode mode)
    {
        behaviorMode = mode;
    }
    
    // Public method to check current behavior mode
    public EnemyBehaviorMode GetBehaviorMode()
    {
        return behaviorMode;
    }
    
    // Public method to manually trigger aggro (for use by other systems)
    public void TriggerAggro(LivingEntity aggressor)
    {
        hasBeenAttacked = true;
        lastAttackedTime = Time.time;
        
        if (aggressor != null && !aggressor.IsDead)
        {
            currentTarget = aggressor;
            isChasing = true;
            lastKnownPlayerPosition = aggressor.transform.position;
            
            // Disable wandering behavior while chasing
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
        }
    }
} 