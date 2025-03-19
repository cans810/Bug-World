using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack
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
    [SerializeField] private EntityHitbox entityHitbox; // Add reference to hitbox
    
    [Header("Nest Avoidance")]
    [SerializeField] private bool avoidPlayerNest = true;
    [SerializeField] private float nestAvoidanceDistance = 0f;
    [SerializeField] private string nestLayerName = "PlayerNest1";
    [SerializeField] private string nestObjectName = "Player Nest 1";
    
    // Internal states
    private bool isMoving = false;
    private bool isWandering = false;
    private float lastAttackTime = -999f;
    private LivingEntity currentTarget = null;
    private AIWandering wanderingBehavior;
    private bool hasBeenAttacked = false;
    private float lastAttackedTime = -999f;
    
    private Transform playerNestTransform;
    private float nestRadius = 0f;
    
    // Add a new state tracking variable at the class level, near other state variables
    private bool isStandingAfterKill = false;
    private bool isPlayerInHitbox = false; // New variable to track if player is in hitbox
    
    // Enum to define different enemy behavior modes
    public enum EnemyBehaviorMode
    {
        Aggressive,  // Always attacks player on sight
        Passive,     // Only attacks if player attacks first
        Territorial  // Only attacks if player gets too close (future implementation)
    }
    
    // We're keeping boundary redirection logic but using world-space logic
    private bool isHittingBoundary = false;
    private float boundaryRedirectionTime = 0f;
    private const float BOUNDARY_REDIRECT_DURATION = 1.5f; // Time to spend redirecting after hitting boundary
    
    // Add this at the class level near other state variables
    private bool inCombatStance = false;
    private Vector3 lastTargetPosition;
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
            
        if (entityHitbox == null)
            entityHitbox = GetComponentInChildren<EntityHitbox>();
            
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
        
        // Register for hitbox events
        if (entityHitbox != null)
        {
            entityHitbox.OnPlayerEnterHitbox += HandlePlayerEnterHitbox;
            entityHitbox.OnPlayerExitHitbox += HandlePlayerExitHitbox;
        }
        
        // Start idle
        UpdateAnimation(false);
        
        // Find the player nest
        FindPlayerNest();
        
        // Enable wandering behavior
        if (wanderingBehavior != null)
        {
            wanderingBehavior.SetWanderingEnabled(true);
            isWandering = true;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandleDeath);
            livingEntity.OnDamaged.RemoveListener(HandleDamaged);
        }
        
        // Unsubscribe from hitbox events
        if (entityHitbox != null)
        {
            entityHitbox.OnPlayerEnterHitbox -= HandlePlayerEnterHitbox;
            entityHitbox.OnPlayerExitHitbox -= HandlePlayerExitHitbox;
        }
    }
    
    private void HandleDeath()
    {
        // Stop all movement states but don't disable components
        isMoving = false;
        isWandering = false;
        
        // Stop wandering without disabling the component
        if (wanderingBehavior != null)
            wanderingBehavior.SetWanderingEnabled(false);
        
        // Update animation to idle/death state if applicable
        UpdateAnimation(false);
        
        // We don't disable the script anymore
        // enabled = false; 
    }
    
    private void HandleDamaged()
    {
        // Mark as attacked and store the time
        hasBeenAttacked = true;
        lastAttackedTime = Time.time;
        
        // No longer initiate chase behavior when damaged
    }
    
    private void Update()
    {
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
        
        // If player is in hitbox, do NOTHING in Update
        if (isPlayerInHitbox)
            return;
        
        // Check if our current target died
        if (currentTarget != null && currentTarget.IsDead)
        {
            // Target died, stand still for a few seconds before returning to wandering
            StartCoroutine(StandStillAfterKill());
            return;
        }
        
        // If we're hitting a boundary, handle redirection
        if (isHittingBoundary)
        {
            if (Time.time > boundaryRedirectionTime)
            {
                isHittingBoundary = false;
                
                // Return to wandering after boundary redirection
                ReturnToWandering();
            }
            return;
        }
        
        // When in combat stance, maintain facing toward the last known target position
        if (inCombatStance && currentTarget != null && !currentTarget.IsDead)
        {
            // Update the last target position
            lastTargetPosition = currentTarget.transform.position;
            
            // Face the target continuously
            FaceTarget(lastTargetPosition);
            
            // Ensure we're still in attack range and attack if possible
            AttackIfInRange();
            
            // Important: Return early to prevent wandering logic when in combat
            return;
        }
        else if (inCombatStance && (currentTarget == null || currentTarget.IsDead))
        {
            // Target is gone but we're still in combat stance - exit it
            ExitCombatStance();
        }
        
        // PRIORITY CHECK: First check if there are any targets in attack range
        // This ensures combat takes precedence over wandering
        if (livingEntity.HasTargetsInRange())
        {
            // Get the closest valid target from the livingEntity
            LivingEntity potentialTarget = livingEntity.GetClosestValidTarget();
            
            // If we found a target, decide whether to attack based on behavior mode
            if (potentialTarget != null && !potentialTarget.IsDead)
            {
                bool shouldAttack = false;
                
                switch (behaviorMode)
                {
                    case EnemyBehaviorMode.Aggressive:
                        // Always attack if in range
                        shouldAttack = true;
                        break;
                        
                    case EnemyBehaviorMode.Passive:
                        // Only attack if we've been attacked
                        shouldAttack = hasBeenAttacked;
                        break;
                }
                
                if (shouldAttack)
                {
                    // Set current target for attack
                    currentTarget = potentialTarget;
                    
                    // Enter combat stance when target is detected
                    if (!inCombatStance)
                    {
                        EnterCombatStance();
                    }
                    
                    // Face the target and attack if in range
                    FaceTarget(currentTarget.transform.position);
                    AttackIfInRange();
                    
                    // Important: Return early to prevent wandering logic from executing
                    return;
                }
            }
        }
        
        // Only run wandering logic if we're not in combat
        // If we're not already wandering, start wandering
        if (!isWandering && wanderingBehavior != null && !wanderingBehavior.IsCurrentlyMoving())
        {
            wanderingBehavior.enabled = true;
            wanderingBehavior.SetWanderingEnabled(true);
            isWandering = true;
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
    
    // Modify AttackIfInRange to maintain combat stance and consistent rotation
    private void AttackIfInRange()
    {
        // If we have a valid target in attack range, attack it
        if (currentTarget != null && !currentTarget.IsDead)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            
            // If we're close enough to attack
            if (distanceToTarget <= attackDistance) // Use attackDistance instead of minAttackDistance
            {
                // Enter combat stance if not already in it
                if (!inCombatStance)
                {
                    EnterCombatStance();
                }
                
                // Store the target position for consistent facing
                lastTargetPosition = currentTarget.transform.position;
                
                // Face the target directly
                FaceTarget(lastTargetPosition);
                
                // Try to attack if cooldown has passed
                if (Time.time >= lastAttackTime + attackInterval)
                {
                    if (livingEntity.TryAttack())
                    {
                        lastAttackTime = Time.time;
                    }
                }
            }
            else if (inCombatStance && distanceToTarget > attackDistance * 1.5f)
            {
                // Target moved too far away, exit combat stance
                ExitCombatStance();
            }
        }
        else
        {
            // No valid target, exit combat stance
            ExitCombatStance();
        }
    }
    
    // Make EnterCombatStance more forceful in stopping all movement
    private void EnterCombatStance()
    {
        inCombatStance = true;
        
        // Stop all movement
        isMoving = false;
        isWandering = false;
        UpdateAnimation(false);
        
        // Completely disable wandering behavior
        if (wanderingBehavior != null)
        {
            wanderingBehavior.SetWanderingEnabled(false);
            wanderingBehavior.enabled = false;
        }
        
        // Cancel any coroutines that might interfere with rotation
        StopAllCoroutines();
        
        // Lock rotation to prevent unwanted changes
        if (livingEntity != null)
        {
            livingEntity.SetRotationLocked(true);
        }
        
        // Zero out all physics forces
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Fix position - no more movement whatsoever
        // Store the current position to prevent any accidental movement
        StartCoroutine(MaintainPositionDuringCombat());
    }

    // Modify the MaintainPositionDuringCombat coroutine to avoid interfering with rotation
    private IEnumerator MaintainPositionDuringCombat()
    {
        Vector3 combatPosition = transform.position;
        
        while (inCombatStance)
        {
            // Force position to remain fixed during combat, but don't touch rotation
            Vector3 currentPosition = transform.position;
            if (currentPosition != combatPosition)
            {
                // Only adjust position without affecting rotation
                transform.position = combatPosition;
            }
            yield return null;
        }
    }
    
    // Modify ExitCombatStance to preserve the current rotation when exiting combat
    private void ExitCombatStance()
    {
        if (inCombatStance)
        {
            inCombatStance = false;
            
            // Store the current rotation before exiting combat stance
            Quaternion exitRotation = transform.rotation;
            
            // Unlock rotation for normal movement
            if (livingEntity != null)
            {
                livingEntity.SetRotationLocked(false);
            }
            
            // Re-enable wandering
            if (wanderingBehavior != null && !wanderingBehavior.enabled)
            {
                wanderingBehavior.enabled = true;
                wanderingBehavior.SetWanderingEnabled(true);
                isWandering = true;
                
                // Tell the wandering behavior to respect our current rotation
                wanderingBehavior.ForceStartWithCurrentRotation(exitRotation);
            }
        }
    }
    
    // Modify ReturnToWandering to simplify it
    private void ReturnToWandering()
    {
        currentTarget = null;
        isStandingAfterKill = false; // Reset the standing after kill state
        
        // Stop moving
        isMoving = false;
        
        // Force animation update to idle immediately
        UpdateAnimation(false);
        
        // Cancel any pending coroutines
        StopAllCoroutines();
        
        // Start the pause-then-wander sequence
        StartCoroutine(PauseAfterChase());
    }
    
    // Rename to better reflect its purpose
    private IEnumerator PauseAfterChase()
    {
        // Ensure wandering behavior is disabled during the pause
        if (wanderingBehavior != null)
        {
            wanderingBehavior.enabled = false;
            isWandering = false;
        }
        
        // Make sure we're in idle animation
        UpdateAnimation(false);
        
        // Pause for 2 seconds
        float pauseDuration = 2.0f;
        float timer = 0f;
        
        // During this pause, we'll keep the enemy in place
        Vector3 pausePosition = transform.position;
        Quaternion pauseRotation = transform.rotation;
        
        while (timer < pauseDuration)
        {
            // Ensure position and rotation stay fixed
            transform.position = pausePosition;
            transform.rotation = pauseRotation;
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        // After the pause, pick a new direction to face
        Vector3 newForward = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;
        
        // Smoothly rotate to this new direction
        float rotationTime = 0.5f;
        timer = 0f;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(newForward);
        
        while (timer < rotationTime)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, timer / rotationTime);
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final rotation is exactly what we want
        transform.rotation = targetRotation;
        
        // Now re-enable wandering with proper initialization
        if (wanderingBehavior != null)
        {
            wanderingBehavior.enabled = true;
            wanderingBehavior.SetWanderingEnabled(true);
            isWandering = true;
            
            // Force the wandering behavior to start in idle state
            wanderingBehavior.ForceStartWithIdle();
            
            // Force the wandering behavior to pick a new waypoint in the direction we're facing
            wanderingBehavior.ForceNewWaypointInDirection(transform.forward);
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
    
    // Modify FaceTarget to maintain perfect facing throughout combat
    private void FaceTarget(Vector3 target)
    {
        // Skip rotation if we're standing still after a kill
        if (isStandingAfterKill)
            return;
        
        Vector3 directionToTarget = target - transform.position;
        directionToTarget.y = 0; // Keep rotation on Y axis only
        
        if (directionToTarget.magnitude > 0.1f)
        {
            // Calculate the desired rotation
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            // If in combat stance, use direct rotation to maintain perfect facing
            if (inCombatStance)
            {
                // Directly set rotation for combat - no interpolation
                transform.rotation = targetRotation;
                
                // Extra precaution to clear any angular velocity
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.angularVelocity = Vector3.zero;
                }
                
                return; // Skip the rest of the method
            }
            
            // For non-combat situations, use the smoother rotation logic
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            
            if (angleDifference > 1.0f)
            {
                livingEntity.RotateTowards(directionToTarget, targetRotationSpeed);
                
                // Check if we're close enough to snap
                float newAngleDifference = Quaternion.Angle(transform.rotation, targetRotation);
                if (newAngleDifference < 0.5f)
                {
                    transform.rotation = targetRotation;
                }
            }
            else
            {
                transform.rotation = targetRotation;
            }
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
        }
    }
    
    // Modify StandStillAfterKill method to be more thorough in stopping all movement
    private IEnumerator StandStillAfterKill()
    {
        // Stop all movement and reset state
        isMoving = false;
        isWandering = false;
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
        
        // Return to wandering
        ReturnToWandering();
    }

    // Replace the current boundary handling methods with this simpler version
    // Remove OnTriggerEnter and OnCollisionEnter handlers

    // Keep only the OnTriggerExit method
    private void OnTriggerExit(Collider other)
    {
        // Check if the collider is in the MapBorder layer
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            // Calculate direction from enemy to center of border - this is our reflection normal
            Vector3 reflectionNormal = (other.bounds.center - transform.position).normalized;
            reflectionNormal.y = 0; // Keep on horizontal plane
            
            // Move back inside by a small amount, not a huge teleport
            transform.position += reflectionNormal * 0.3f;
            
            // Current forward direction before reflection
            Vector3 currentDirection = transform.forward;
            
            // Calculate the reflection direction using standard Vector3.Reflect
            Vector3 reflectionDirection = Vector3.Reflect(currentDirection, reflectionNormal);
            
            // Add a tiny bit of randomness to prevent predictable patterns
            reflectionDirection = Quaternion.Euler(0, Random.Range(-10f, 10f), 0) * reflectionDirection;
            
            // Set new rotation immediately
            transform.rotation = Quaternion.LookRotation(reflectionDirection);
            
            // If we have a wandering behavior, tell it to continue in this direction
            if (wanderingBehavior != null)
            {
                wanderingBehavior.SetWanderingEnabled(true);
                wanderingBehavior.ForceNewWaypointInDirection(reflectionDirection);
            }
        }
    }

    // Modified method to handle player exit
    private void HandlePlayerExitHitbox(LivingEntity playerEntity)
    {
        Debug.Log($"PLAYER EXITED HITBOX of {gameObject.name}");
        
        isPlayerInHitbox = false;
        
        // Stop attacking animation
        if (animController != null)
        {
            animController.SetAttacking(false);
        }
        
        // Unlock rotation
        if (livingEntity != null)
        {
            livingEntity.SetRotationLocked(false);
        }
        
        // We'll wait a moment before returning to normal behavior
        StartCoroutine(ReturnToWanderingAfterDelay(0.5f));
    }

    // Modified coroutine for cleaner transition back to wandering
    private IEnumerator ReturnToWanderingAfterDelay(float delay)
    {
        // Wait the specified delay
        yield return new WaitForSeconds(delay);
        
        // Only return to wandering if player is still not in hitbox and we're not dead
        if (!isPlayerInHitbox && !livingEntity.IsDead)
        {
            // Enable wandering script first
            if (wanderingBehavior != null)
            {
                // Make sure the component is enabled
                wanderingBehavior.enabled = true;
                
                // Force stop any existing coroutines to ensure a clean start
                wanderingBehavior.StopAllCoroutines();
                
                // Enable wandering and force a completely new wandering cycle
                wanderingBehavior.SetWanderingEnabled(true);
                isWandering = true;
                
                // This is the key part - force the wandering behavior to restart its main routine
                // which will ensure the enemy actually starts moving again
                wanderingBehavior.ForceRestartWandering();
            }
        }
    }

    // Modified method to face player while attacking
    private void HandlePlayerEnterHitbox(LivingEntity playerEntity)
    {
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;

        Debug.Log($"PLAYER ENTERED HITBOX of {gameObject.name}");
        
        isPlayerInHitbox = true;
        
        // Stop ALL movement states
        isMoving = false;
        isWandering = false;
        inCombatStance = false;
        isHittingBoundary = false;
        
        // Completely disable and stop the AIWandering component
        if (wanderingBehavior != null)
        {
            // Force stop all wandering coroutines first
            wanderingBehavior.StopAllCoroutines();
            
            // Disable wandering completely
            wanderingBehavior.SetWanderingEnabled(false);
            wanderingBehavior.enabled = false;
        }
        
        // Cancel ALL coroutines on this object
        StopAllCoroutines();
        
        // We'll handle rotation manually to face the player
        if (livingEntity != null)
        {
            livingEntity.SetRotationLocked(true);
        }
        
        // Start attacking in place after a short delay
        StartCoroutine(StartAttackAfterDelay(0.2f));
        
        // Start the position maintenance coroutine
        StartCoroutine(MaintainPositionWhilePlayerInHitbox());
        
        // Start the rotation coroutine to face the player
        StartCoroutine(FacePlayerWhileInHitbox(playerEntity));
    }

    // Updated coroutine to make enemy face the player with smoother rotation
    // New coroutine to make enemy face the player while in hitbox
    private IEnumerator FacePlayerWhileInHitbox(LivingEntity playerEntity)
    {
        while (isPlayerInHitbox && !livingEntity.IsDead && playerEntity != null && !playerEntity.IsDead)
        {
            // Calculate direction to player
            Vector3 directionToPlayer = playerEntity.transform.position - transform.position;
            directionToPlayer.y = 0; // Keep rotation on Y axis only
            
            if (directionToPlayer.magnitude > 0.1f)
            {
                // Directly face the player with no smoothing for responsive combat
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = targetRotation;
            }
            
            yield return null;
        }
    }

    // Renamed coroutine to avoid conflict
    private IEnumerator MaintainPositionWhilePlayerInHitbox()
    {
        Vector3 combatPosition = transform.position;
        
        while (isPlayerInHitbox)
        {
            // Force position to remain fixed during combat
            if (transform.position != combatPosition)
            {
                transform.position = combatPosition;
            }
            yield return null;
        }
    }

    // Add this method to start attacking after a short delay
    private IEnumerator StartAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Only start attack if player is still in hitbox
        if (isPlayerInHitbox && !livingEntity.IsDead)
        {
            // Start attacking in place
            StartCoroutine(AttackInPlace());
        }
    }

    // Add this method to handle attacking when player is in hitbox
    private IEnumerator AttackInPlace()
    {
        while (isPlayerInHitbox && !livingEntity.IsDead)
        {
            // Trigger attack if cooldown has passed
            if (Time.time >= lastAttackTime + attackInterval)
            {
                if (livingEntity.TryAttack())
                {
                    lastAttackTime = Time.time;
                    
                    // Set attacking animation
                    if (animController != null)
                    {
                        animController.SetAttacking(true);
                    }
                }
            }
            
            yield return null;
        }
    }
} 