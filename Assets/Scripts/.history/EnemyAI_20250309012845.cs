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
            livingEntity.OnDamaged.AddListener((damage, attacker) => HandleDamaged(damage, attacker));
            
            // Make sure destruction settings are properly configured
            livingEntity.SetDestroyOnDeath(true, 5f);
        }
        
        // Start idle
        UpdateAnimation(false);
        
        // Find the boundary
        mapBoundary = FindObjectOfType<MapBoundary>();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandleDeath);
            
            // Use a lambda to match the exact delegate that was added
            livingEntity.OnDamaged.RemoveListener((damage, attacker) => HandleDamaged(damage, attacker));
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
    
    private void HandleDamaged(float damage, GameObject attacker)
    {
        // Mark as attacked and store the time
        hasBeenAttacked = true;
        lastAttackedTime = Time.time;
        
        // If we're passive and get attacked, immediately target the attacker if it's a LivingEntity
        if (behaviorMode == EnemyBehaviorMode.Passive && attacker != null)
        {
            LivingEntity attackerEntity = attacker.GetComponent<LivingEntity>();
            if (attackerEntity != null && !attackerEntity.IsDead)
            {
                currentTarget = attackerEntity;
                isChasing = true;
                lastKnownPlayerPosition = attackerEntity.transform.position;
                
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
    
    // New method to handle movement with boundary checking
    private void MoveWithBoundaryCheck(Vector3 direction)
    {
        // Calculate the next position
        Vector3 nextPosition = transform.position + direction * livingEntity.moveSpeed * Time.deltaTime;
        
        // Check if the next position is within bounds
        if (mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition))
        {
            // Get the nearest safe position inside the boundary
            Vector3 safePosition = mapBoundary.GetNearestPointInBounds(nextPosition);
            
            // Calculate new direction along the boundary
            Vector3 redirectedDirection = (safePosition - transform.position).normalized;
            
            // Update facing direction
            transform.forward = redirectedDirection;
            
            // Move in the redirected direction
            transform.position += redirectedDirection * livingEntity.moveSpeed * Time.deltaTime;
        }
        else
        {
            // Move normally if within bounds
            transform.position += direction * livingEntity.moveSpeed * Time.deltaTime;
        }
    }
    
    private void ChaseAndAttackTarget()
    {
        // Get distance to target
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
            
            // Move with boundary check instead of direct position change
            MoveWithBoundaryCheck(transform.forward);
        }
    }
    
    private void ReturnToWandering()
    {
        isChasing = false;
        currentTarget = null;
        
        // Stop moving
        isMoving = false;
        UpdateAnimation(false);
        
        // Re-enable wandering behavior
        if (wanderingBehavior != null)
            wanderingBehavior.SetWanderingEnabled(true);
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
        
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                livingEntity.rotationSpeed * Time.deltaTime
            );
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