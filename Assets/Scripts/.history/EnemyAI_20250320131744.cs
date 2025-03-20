using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] public float attackDistance = 1.5f; // Distance at which to attack
    [SerializeField] public float targetRotationSpeed = 5.0f; // Increased rotation speed for sharper turns
    
    [Header("Attack Settings")]
    [SerializeField] public float attackInterval = 1.5f; // Minimum time between attacks
    [SerializeField] public float minAttackDistance = 1.0f; // Minimum distance required to attack player
    
    [Header("Behavior Settings")]
    [SerializeField] public EnemyBehaviorMode behaviorMode = EnemyBehaviorMode.Aggressive; // Default behavior mode
    [SerializeField] public float aggroMemoryDuration = 30f; // How long the enemy remembers being attacked
    
    [Header("References")]
    [SerializeField] public AnimationController animController;
    [SerializeField] public LivingEntity livingEntity;
    [SerializeField] public EntityHitbox entityHitbox; // Add reference to hitbox
    
    [Header("Nest Avoidance")]
    [SerializeField] public bool avoidPlayerNest = true;
    [SerializeField] public float nestAvoidanceDistance = 0f;
    [SerializeField] public string nestLayerName = "PlayerNest1";
    [SerializeField] public string nestObjectName = "Player Nest 1";
    
    // Internal states
    public bool isMoving = false;
    public bool isWandering = false;
    public float lastAttackTime = -999f;
    public LivingEntity currentTarget = null;
    public AIWandering wanderingBehavior;
    public bool hasBeenAttacked = false;
    public float lastAttackedTime = -999f;
    
    public Transform playerNestTransform;
    public float nestRadius = 0f;
    
    // Add a new state tracking variable at the class level, near other state variables
    public bool isStandingAfterKill = false;
    public bool isPlayerInHitbox = false; // New variable to track if player is in hitbox
    
    // Enum to define different enemy behavior modes
    public enum EnemyBehaviorMode
    {
        Aggressive,   // Always attacks player on sight
        Passive,      // Only attacks if player attacks first
        Territorial,  // Only attacks if player gets too close (future implementation)
        UltraPassive  // Never attacks, only wanders
    }
    
    // We're keeping boundary redirection logic but using world-space logic
    public bool isHittingBoundary = false;
    public float boundaryRedirectionTime = 0f;
    public const float BOUNDARY_REDIRECT_DURATION = 1.5f; // Time to spend redirecting after hitting boundary
    
    // Add this at the class level near other state variables
    public bool inCombatStance = false;
    public Vector3 lastTargetPosition;

    [Header("Map Border Settings")]
    [SerializeField] public string borderObjectName = "MapBorder";
    
    protected virtual void Start()
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
    
    protected virtual void Update()
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
                
                // Simply return to wandering without forcing any direction
                ReturnToWandering();
            }
            return; // Skip the rest of the update while at boundary
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

                    case EnemyBehaviorMode.UltraPassive:
                        // Never attack
                        shouldAttack = false;
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
    protected virtual void AttackIfInRange()
    {
        // If we have a valid target in attack range, attack it
        if (currentTarget != null && !currentTarget.IsDead)
        {
            // Check if target is in the same border area
            bool inSameBorder = CheckSameBorderAs(currentTarget.gameObject);
            if (!inSameBorder)
            {
                // Debug log if in different borders
                if (showDebugInfo)
                {
                    Debug.Log($"{gameObject.name} cannot attack {currentTarget.name} - different border areas");
                }
                return; // Don't attack if in different borders
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            
            // If we're close enough to attack
            if (distanceToTarget <= attackDistance)
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
                
                // Tell the wandering behavior to use our current forward direction
                wanderingBehavior.ForceStartWithDirection(exitRotation * Vector3.forward);
            }
        }
    }
    
    // Modify ReturnToWandering to simplify it
    public void ReturnToWandering()
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

    // Border handling to completely stop at boundary, no bouncing
    private void OnTriggerExit(Collider other)
    {
        // Check if the collider is in the MapBorder layer
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            // Calculate the direction pointing back inside the playable area
            Vector3 inwardDirection;
            
            // Handle different collider types
            if (other is SphereCollider sphereCollider)
            {
                // Get the center of the sphere in world space
                Vector3 sphereCenter = other.transform.TransformPoint(sphereCollider.center);
                // Calculate direction from enemy to sphere center
                inwardDirection = (sphereCenter - transform.position).normalized;
            }
            else
            {
                // For non-sphere colliders, calculate inward direction
                Vector3 closestPoint = other.ClosestPoint(transform.position);
                inwardDirection = (closestPoint - transform.position).normalized;
            }
            
            // Ensure we're working in the horizontal plane only
            inwardDirection.y = 0;
            inwardDirection.Normalize();
            
            // JUST enough push to get back inside the boundary
            transform.position += inwardDirection * 0.2f;
            
            // Completely stop all movement
            isHittingBoundary = true;
            boundaryRedirectionTime = Time.time + BOUNDARY_REDIRECT_DURATION;
            
            // Don't change rotation - just freeze in current direction
            // transform.rotation = Quaternion.LookRotation(inwardDirection); <-- removed this
            
            // If we have a wandering behavior, tell it to completely stop
            if (wanderingBehavior != null)
            {
                wanderingBehavior.StopAllCoroutines();
                wanderingBehavior.SetWanderingEnabled(false);
                wanderingBehavior.ForceStop(); // Make sure it's completely stopped
            }
            
            // Temporarily stop any chase behavior
            if (currentTarget != null)
            {
                // Just store the target but stop pursuing
                StartCoroutine(ResumeChasingAfterRedirection());
            }
        }
    }

    // Add this helper coroutine to resume chasing after the boundary redirection
    private IEnumerator ResumeChasingAfterRedirection()
    {
        // Wait until we're no longer redirecting from a boundary hit
        yield return new WaitUntil(() => !isHittingBoundary);
        
        // Add a small additional delay before resuming chase
        yield return new WaitForSeconds(0.5f);
        
        // Only resume chasing if the target is still valid
        if (currentTarget != null && !currentTarget.IsDead)
        {
            // Re-enter combat stance if appropriate
            if (!inCombatStance)
            {
                EnterCombatStance();
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
    protected virtual void HandlePlayerEnterHitbox(LivingEntity playerEntity)
    {
        // Don't do anything if dead or if player is dead
        if (livingEntity == null || livingEntity.IsDead || playerEntity == null || playerEntity.IsDead)
            return;

        // If in UltraPassive mode, don't enter combat or attack
        if (behaviorMode == EnemyBehaviorMode.UltraPassive)
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
    public IEnumerator FacePlayerWhileInHitbox(LivingEntity playerEntity)
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
    public IEnumerator MaintainPositionWhilePlayerInHitbox()
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
    public IEnumerator StartAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Only start attack if player is still in hitbox
        if (isPlayerInHitbox && !livingEntity.IsDead)
        {
            // Start attacking in place
            StartCoroutine(AttackInPlace());
        }
    }

    // Update AttackInPlace coroutine to include invulnerability check
    private IEnumerator AttackInPlace()
    {
        // Get reference to player from the scene if we don't have it already
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        LivingEntity playerEntity = playerObject?.GetComponent<LivingEntity>();
        
        // Main attack loop
        while (isPlayerInHitbox && !livingEntity.IsDead)
        {
            // Check if player is still alive or invulnerable
            if (playerEntity == null || playerEntity.IsDead || playerEntity.IsInvulnerable)
            {
                // Reset attack animation
                if (animController != null)
                {
                    animController.SetAttacking(false);
                }
                
                // If player is invulnerable but not dead, wait briefly and continue
                if (playerEntity != null && !playerEntity.IsDead && playerEntity.IsInvulnerable)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                
                // Exit the loop if player is dead
                break;
            }
            
            // Check if player is in the same border area
            bool inSameBorder = CheckSameBorderAs(playerObject);
            if (!inSameBorder)
            {
                // If in different borders, wait briefly and continue
                yield return new WaitForSeconds(0.5f);
                continue;
            }
            
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
        
        // Make sure to reset attack animation when exiting
        if (animController != null)
        {
            animController.SetAttacking(false);
        }
    }

    // Helper method to check if two objects are in the same border area
    private bool CheckSameBorderAs(GameObject otherObject)
    {
        if (otherObject == null) return false;
        
        // Check if the entity has a PlayerAttributes component (for player)
        PlayerAttributes playerAttrib = otherObject.GetComponent<PlayerAttributes>();
        if (playerAttrib != null)
        {
            return playerAttrib.borderObjectName == borderObjectName;
        }
        
        // Check if the entity has an EnemyAI component (for enemies)
        EnemyAI enemyAI = otherObject.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            return enemyAI.borderObjectName == borderObjectName;
        }
        
        // If neither component is found, default to true (allow attack)
        return true;
    }
} 