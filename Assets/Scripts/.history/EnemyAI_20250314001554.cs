using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack
    [SerializeField] private float detectionFollowRange = 10f; // Max range to follow player after detection
    [SerializeField] private float targetRotationSpeed = 5.0f; // Increased rotation speed for sharper turns
    
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
    
    [Header("Territory Settings")]
    [SerializeField] private bool respectTerritory = true;
    [SerializeField] private float territoryReturnDistance = 3.0f; // How far to allow going beyond territory
    [SerializeField] private float maxDistanceFromTerritory = 10.0f; // Max distance before forced return
    
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
    
    // Add a new state tracking variable at the class level, near other state variables
    private bool isStandingAfterKill = false;
    
    // Territory tracking
    private EntityGenerator homeGenerator;
    private Vector3 territoryCenter;
    private float territoryRadius;
    private bool hasTerritory = false;
    private bool isReturningToTerritory = false;
    
    // Enum to define different enemy behavior modes
    public enum EnemyBehaviorMode
    {
        Aggressive,  // Always attacks player on sight
        Passive,     // Only attacks if player attacks first
        Territorial  // Only attacks if player gets too close (future implementation)
    }
    
    // Add these class variables
    private bool isRingTerritory = false;
    private float innerTerritoryRadius = 0f;
    
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
        
        // Check if our current target died
        if (currentTarget != null && currentTarget.IsDead)
        {
            // Target died, stand still for a few seconds before returning to wandering
            StartCoroutine(StandStillAfterKill());
            return;
        }
        
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
    
    // Add this method to detect and respond to territory boundaries during movement
    private bool CheckAndRespectTerritory(Vector3 moveDirection)
    {
        if (!hasTerritory || !respectTerritory || isReturningToTerritory) return true;
        
        // Calculate current position and next position
        Vector3 currentPos = transform.position;
        Vector3 nextPos = currentPos + moveDirection * livingEntity.moveSpeed * Time.deltaTime;
        
        // Calculate distance to territory center (ignore Y)
        Vector3 flatNextPos = nextPos;
        Vector3 flatCenter = territoryCenter;
        flatNextPos.y = flatCenter.y;
        
        float distanceToCenter = Vector3.Distance(flatNextPos, flatCenter);
        
        // Check if the next position would cross a boundary
        bool wouldCrossBoundary = false;
        
        if (isRingTerritory)
        {
            // For ring territory, check both inner and outer boundaries
            wouldCrossBoundary = distanceToCenter <= innerTerritoryRadius || 
                                distanceToCenter >= territoryRadius;
        }
        else
        {
            // For regular territory, check only outer boundary
            wouldCrossBoundary = distanceToCenter >= territoryRadius;
        }
        
        // If we would cross boundary, handle it immediately
        if (wouldCrossBoundary)
        {
            // Determine the direction away from the boundary
            Vector3 awayFromBoundary;
            
            if (isRingTerritory && distanceToCenter <= innerTerritoryRadius)
            {
                // Too close to inner boundary, get direction away from center
                awayFromBoundary = (flatNextPos - flatCenter).normalized;
            }
            else
            {
                // Too close to outer boundary, get direction toward center
                awayFromBoundary = (flatCenter - flatNextPos).normalized;
            }
            
            // If chasing a target, decide whether to continue chase or turn back
            if (currentTarget != null)
            {
                Vector3 targetPos = currentTarget.transform.position;
                Vector3 flatTargetPos = targetPos;
                flatTargetPos.y = flatCenter.y;
                
                float targetDistanceToCenter = Vector3.Distance(flatTargetPos, flatCenter);
                bool targetInsideTerritory = true;
                
                if (isRingTerritory)
                {
                    targetInsideTerritory = targetDistanceToCenter >= innerTerritoryRadius && 
                                           targetDistanceToCenter <= territoryRadius;
                }
                else
                {
                    targetInsideTerritory = targetDistanceToCenter <= territoryRadius;
                }
                
                // If target is outside territory, stop chasing and turn back
                if (!targetInsideTerritory)
                {
                    // Lost interest in target due to territory boundary
                    currentTarget = null;
                    isChasing = false;
                    
                    // Force a turn in the opposite direction
                    FaceTarget(transform.position + awayFromBoundary);
                    return false;
                }
            }
            
            // Even if target is still inside territory, stop at boundary
            FaceTarget(transform.position + awayFromBoundary);
            return false;
        }
        
        // No boundary issues, can continue moving
        return true;
    }
    
    // Update MoveWithBoundaryCheck to respect territory
    private void MoveWithBoundaryCheck(Vector3 direction)
    {
        // Check for very small movement to avoid normalization issues
        if (direction.magnitude < 0.1f)
            return;
        
        direction.Normalize();
        
        // First check territory boundaries
        if (!CheckAndRespectTerritory(direction))
        {
            // Territory boundary detected, already handled in CheckAndRespectTerritory
            return;
        }
        
        // Continue with the rest of the boundary checks (map boundaries, nest, etc.)
        // ... existing map boundary and nest code ...
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
            livingEntity.RotateTowards(awayFromNest, 2.0f);
            
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
            livingEntity.RotateTowards(awayFromNest, 2.0f);
            
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
            livingEntity.MoveInDirection(transform.forward);
        }
    }
    
    // Modify ReturnToWandering to be more thorough in resetting state
    private void ReturnToWandering()
    {
        isChasing = false;
        currentTarget = null;
        lastKnownPlayerPosition = Vector3.zero; // Reset last known position
        isStandingAfterKill = false; // Reset the standing after kill state
        
        // Stop moving
        isMoving = false;
        UpdateAnimation(false);
        
        // Cancel any pending coroutines that might re-enable chasing
        StopAllCoroutines();
        
        // Make sure wandering behavior is enabled and properly reset
        if (wanderingBehavior != null)
        {
            wanderingBehavior.enabled = true;
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
    
    // Modify FaceTarget method to use the higher rotation speed value
    private void FaceTarget(Vector3 target)
    {
        // Skip rotation if we're standing still after a kill
        if (isStandingAfterKill)
            return;
        
        Vector3 directionToTarget = target - transform.position;
        directionToTarget.y = 0; // Keep rotation on Y axis only
        
        if (directionToTarget.magnitude > 0.1f)
        {
            livingEntity.RotateTowards(directionToTarget, targetRotationSpeed);
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
    
    // Modify StandStillAfterKill method to be more thorough in stopping all movement
    private IEnumerator StandStillAfterKill()
    {
        // Stop all movement and reset state
        isMoving = false;
        isChasing = false;
        isStandingAfterKill = true; // Set the new state flag
        UpdateAnimation(false);
        
        // Completely disable the wandering behavior component
        if (wanderingBehavior != null)
        {
            wanderingBehavior.enabled = false;
        }
        
        // Store the dead target reference temporarily, then clear current target
        LivingEntity deadTarget = currentTarget;
        currentTarget = null;
        
        // Save the current rotation and position to prevent any movement/spinning
        Quaternion frozenRotation = transform.rotation;
        Vector3 frozenPosition = transform.position;
        
        // Temporarily disable any components that might cause movement
        Rigidbody rb = GetComponent<Rigidbody>();
        bool hadRigidbodyKinematic = false;
        if (rb != null)
        {
            hadRigidbodyKinematic = rb.isKinematic;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // Tell the living entity not to rotate for this duration
        if (livingEntity != null)
        {
            livingEntity.SetRotationLocked(true);
        }
        
        // Temporarily disable this script (except for the coroutine)
        this.enabled = false;
        
        // Wait period - during this time, we'll continuously enforce the frozen rotation and position
        float timer = 0;
        float waitDuration = 3f;
        while (timer < waitDuration)
        {
            // Force the rotation and position to stay the same
            transform.rotation = frozenRotation;
            transform.position = frozenPosition;
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Re-enable this script
        this.enabled = true;
        
        // Reset rotation control
        if (livingEntity != null)
        {
            livingEntity.SetRotationLocked(false);
        }
        
        // Restore rigidbody state if it was changed
        if (rb != null)
        {
            rb.isKinematic = hadRigidbodyKinematic;
        }
        
        // Reset the standing after kill state
        isStandingAfterKill = false;
        
        // If we haven't acquired a new target during the wait, return to wandering
        if (currentTarget == null)
        {
            ReturnToWandering();
        }
    }
    
    // Update SetHomeTerritory to store ring information
    public void SetHomeTerritory(EntityGenerator generator, Vector3 center, float radius)
    {
        homeGenerator = generator;
        territoryCenter = center;
        territoryRadius = radius;
        hasTerritory = true;
        
        // Check if this is a ring territory
        isRingTerritory = generator.IsRingTerritory();
        innerTerritoryRadius = generator.GetInnerRadius();
    }
    
    // Update ReturnToTerritory to handle ring territories
    private IEnumerator ReturnToTerritory()
    {
        // Set flag so we don't re-trigger while returning
        isReturningToTerritory = true;
        
        // Save the current state
        bool wasChasing = isChasing;
        LivingEntity previousTarget = currentTarget;
        
        // Reset state
        isChasing = false;
        currentTarget = null;
        
        // Disable wandering during return
        if (wanderingBehavior != null)
            wanderingBehavior.SetWanderingEnabled(false);
            
        // Update the target position logic for ring territories
        float safeReturnBuffer;
        Vector3 targetPosition;
        
        if (isRingTerritory)
        {
            // For ring territories, aim for the middle of the ring
            float midRingRadius = (innerTerritoryRadius + territoryRadius) / 2;
            
            // Calculate direction from center to current position
            Vector3 dirFromCenter = (transform.position - territoryCenter).normalized;
            
            // Target position is in the middle of the ring, in the same direction from center
            targetPosition = territoryCenter + dirFromCenter * midRingRadius;
            
            // Use a smaller buffer for ring territories
            safeReturnBuffer = 1.0f;
        }
        else
        {
            // For regular territories, head toward 70% of radius as before
            targetPosition = territoryCenter;
            safeReturnBuffer = territoryRadius * 0.7f;
        }
        
        // Update animation
        isMoving = true;
        UpdateAnimation(true);
        
        // Keep moving toward target position until we're within safe buffer
        while (Vector3.Distance(transform.position, targetPosition) > safeReturnBuffer)
        {
            // If entity dies during return, break out
            if (livingEntity == null || livingEntity.IsDead)
                yield break;
                
            // Update return direction
            Vector3 returnDirection = (targetPosition - transform.position).normalized;
            returnDirection.y = 0;
            
            // Face the return direction
            FaceTarget(transform.position + returnDirection);
            
            // Move toward target position
            livingEntity.MoveInDirection(transform.forward);
            
            yield return null;
        }
        
        // We're back in territory, resume normal behavior
        isReturningToTerritory = false;
        
        // Re-enable wandering
        if (wanderingBehavior != null)
        {
            wanderingBehavior.enabled = true;
            wanderingBehavior.SetWanderingEnabled(true);
            wanderingBehavior.ForceNewWaypoint();
        }
        
        // Stop moving for a moment after returning
        isMoving = false;
        UpdateAnimation(false);
    }
} 