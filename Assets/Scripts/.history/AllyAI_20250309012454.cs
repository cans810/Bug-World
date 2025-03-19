using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    public enum AIMode
    {
        Follow,    // Follow player and attack enemies
        Wander,    // Wander around and attack enemies
        Carrying,  // Carrying loot to base
        GoingToLoot, // Moving towards detected loot
        Attacking   // Actively engaging enemies
    }
    
    [Header("AI Behavior")]
    [SerializeField] private AIMode currentMode = AIMode.Follow;
    [SerializeField] private bool allowModeSwitch = true;  // Allow toggling between modes
    private AIMode originalMode; // Store original mode before loot detection/carrying
    
    [Header("Movement Settings")]
    [SerializeField] private float followDistance = 3f; // Distance to maintain from player
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack enemies
    [SerializeField] private float detectionFollowRange = 10f; // Max range to follow enemies after detection
    
    [Header("Attack Settings")]
    [SerializeField] private float attackInterval = 1.5f; // Minimum time between attacks
    [SerializeField] private string[] friendlyTags = new string[] { "Player", "Ally" }; // Tags of entities NOT to attack
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;

    [Header("Base Settings")]
    public Transform baseTransform;
    
    [Header("Loot Collection")]
    [SerializeField] private bool lootModeEnabled = true; // Whether this ant should collect loot or not
    [SerializeField] private LayerMask lootLayer;
    [SerializeField] private float lootDropDistance = 1.0f;
    [SerializeField] private float lootCollectionCooldown = 3.0f; // Cooldown before collecting another loot
    [Tooltip("How close the ally needs to be to pick up loot")]
    [SerializeField] private float lootPickupDistance = 0.5f;
    private Transform carriedLoot = null;
    private Vector3 lootPickupPosition;
    private float nextLootCollectionTime = 0f;
    
    // Internal states
    private bool isMoving = false;
    private float lastAttackTime = -999f;
    private Transform playerTransform;
    private LivingEntity currentEnemyTarget = null;
    private AIWandering wanderingBehavior;
    private bool isChasing = false;
    private AIMode previousMode; // Store previous mode when in combat
    private bool isInCombat = false;
    private Vector3 lastKnownEnemyPosition; // Track last known position of enemy
    
    private MapBoundary mapBoundary;
    
    private Transform detectedLoot = null;
    
    [Header("Ally Avoidance")]
    [SerializeField] private bool avoidOtherAllies = true;
    [SerializeField] private float allyAvoidanceDistance = 0.5f;
    [SerializeField] private float allyDetectionRadius = 0.5f;
    [SerializeField] private LayerMask allyLayer;
    
    [Header("Idle Behavior")]
    [SerializeField] private bool lookAroundWhenIdle = true;
    [SerializeField] private float idleLookSpeed = 0.5f;
    [SerializeField] private float maxLookAngle = 60f;
    private float idleLookTimer = 0f;
    private Quaternion targetIdleRotation;
    private bool isLookingAround = false;
    
    private void Start()
    {
        // Find player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
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
        }

        if(baseTransform == null)
        {
            baseTransform = GameObject.Find("PlayerNestCarriedItemsPos").transform;
        }

        playerTransform.gameObject.GetComponent<LivingEntity>
        
        // Start idle
        UpdateAnimation(false);
        
        // Find the boundary
        mapBoundary = FindObjectOfType<MapBoundary>();
        
        // Initialize mode
        SetInitialMode();
    }
    
    private void SetInitialMode()
    {
        originalMode = currentMode;
        
        // Set the appropriate initial behavior based on mode
        if (currentMode == AIMode.Wander)
        {
            // Enable wandering behavior
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(true);
        }
        else if (currentMode == AIMode.Follow)
        {
            // Make sure wandering is disabled initially
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from death event to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandleDeath);
        }
    }
    
    private void HandleDeath()
    {
        if (wanderingBehavior != null)
            wanderingBehavior.enabled = false;
        
        isMoving = false;
        isChasing = false;
        isInCombat = false;
        
        enabled = false;
    }
    
    private void Update()
    {
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
            
        // If player reference is lost, try to find it again
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null && currentMode == AIMode.Follow) 
            {
                // Can't follow without player, switch to wander mode
                SetMode(AIMode.Wander);
            }
        }
        
        // If we're already carrying loot, skip everything else and go to base
        if (carriedLoot != null)
        {
            // If we weren't in carrying mode, switch to it
            if (currentMode != AIMode.Carrying)
            {
                originalMode = previousMode; // Store the original mode
                SetMode(AIMode.Carrying);
            }
            CarryLootToBase();
            return;
        }
        
        // Check for loot if we're not carrying anything, not in combat, and cooldown has passed
        // AND loot mode is enabled
        if (carriedLoot == null && !isInCombat && !isChasing && Time.time > nextLootCollectionTime 
            && currentMode != AIMode.GoingToLoot && lootModeEnabled)
        {
            CheckForLoot();
        }
        
        // Check if we have any enemy targets in range detected by our hitbox
        if (livingEntity.HasTargetsInRange())
        {
            // Get the closest valid target from the livingEntity
            LivingEntity potentialTarget = livingEntity.GetClosestValidTarget();
            
            // Skip friendly targets (player and other allies)
            if (potentialTarget != null && !potentialTarget.IsDead && !IsFriendlyEntity(potentialTarget))
            {
                // Enter combat mode
                if (!isInCombat)
                {
                    originalMode = currentMode;
                }
                previousMode = currentMode;
                isInCombat = true;
                
                // Disable wandering behavior while attacking
                if (wanderingBehavior != null)
                    wanderingBehavior.SetWanderingEnabled(false);
                
                currentEnemyTarget = potentialTarget;
                isChasing = true;
                
                // Update last known position of the enemy
                lastKnownEnemyPosition = currentEnemyTarget.transform.position;
                
                // Attack logic
                ChaseAndAttackEnemy();
                return; // Skip normal behavior logic
            }
        }
        // If we were chasing an enemy but lost direct detection, check if we should continue to last known position
        else if (isChasing && currentEnemyTarget == null)
        {
            float distanceToLastKnown = Vector3.Distance(transform.position, lastKnownEnemyPosition);
            
            // If we're still within follow range of the last known position, continue moving there
            if (distanceToLastKnown > attackDistance && distanceToLastKnown < detectionFollowRange)
            {
                isMoving = true;
                UpdateAnimation(true);
                
                // Face and move towards last known position
                FaceTarget(lastKnownEnemyPosition);
                
                // Move with boundary check
                MoveWithBoundaryCheck(transform.forward);
                
                // If we've reached the last known position and still don't see the enemy, give up chase
                if (distanceToLastKnown <= attackDistance)
                {
                    EndChaseAndReturnToMode();
                }
                
                return; // Skip normal behavior
            }
            else
            {
                // Beyond follow range, give up chase
                EndChaseAndReturnToMode();
            }
        }
        
        // Execute current behavior mode
        switch (currentMode)
        {
            case AIMode.Follow:
                FollowPlayerBehavior();
                break;
                
            case AIMode.Wander:
                WanderBehavior();
                break;
                
            case AIMode.GoingToLoot:
                GoToLootBehavior();
                break;
                
            case AIMode.Attacking:
                // If we're in attacking mode but have no target, return to previous mode
                if (currentEnemyTarget == null || currentEnemyTarget.IsDead)
                {
                    EndChaseAndReturnToMode();
                }
                break;
        }
        
        // Debug mode toggle
        if (allowModeSwitch && Input.GetKeyDown(KeyCode.M))
        {
            ToggleMode();
        }
    }
    
    private void EndChaseAndReturnToMode()
    {
        isInCombat = false;
        isChasing = false;
        currentEnemyTarget = null;
        
        // Return to original behavior mode, not just previous
        SetMode(originalMode);
    }
    
    private void FollowPlayerBehavior()
    {
        if (playerTransform == null)
            return;
            
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Check if player is moving
        bool isPlayerMoving = false;
        PlayerController playerController = playerTransform.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Check player's current animation state using IsAnimationPlaying
            AnimationController playerAnimController = playerTransform.GetComponent<AnimationController>();
            if (playerAnimController != null)
            {
                isPlayerMoving = playerAnimController.IsAnimationPlaying("walk");
            }
            else
            {
                // Fallback if AnimationController isn't available - check if player position changed
                Vector3 playerVelocity = playerTransform.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
                isPlayerMoving = playerVelocity.magnitude > 0.1f;
            }
        }
        else
        {
            // Fallback if PlayerController isn't available - check if player position changed
            Vector3 playerVelocity = playerTransform.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
            isPlayerMoving = playerVelocity.magnitude > 0.1f;
        }
        
        // If player is not moving and we're close enough, stop
        if (!isPlayerMoving && distanceToPlayer <= followDistance * 1.5f)
        {
            // Stop movement
            isMoving = false;
            UpdateAnimation(false);
            return;
        }
        
        // If we need to follow (too far from player)
        if (distanceToPlayer > followDistance)
        {
            // Disable wandering behavior while following
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
            
            isMoving = true;
            UpdateAnimation(true);
            
            // Calculate direction to player
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            
            // Apply ally avoidance if enabled
            Vector3 moveDirection = directionToPlayer;
            if (avoidOtherAllies)
            {
                moveDirection = ApplyAllyAvoidance(directionToPlayer);
            }
            
            // Only update rotation if direction changed significantly
            // This prevents jittery rotation
            if (Vector3.Dot(transform.forward, moveDirection) < 0.99f)
            {
                // Smooth rotation toward movement direction
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    livingEntity.rotationSpeed * 0.5f * Time.deltaTime
                );
            }
            
            // Move with boundary check - use the calculated direction, not transform.forward
            MoveWithBoundaryCheck(moveDirection);
        }
        // If we're close enough to player, just stay in place
        else
        {
            // Disable wandering when player is close
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
            
            // Stop movement and animation
            isMoving = false;
            UpdateAnimation(false);
        }
    }
    
    private void WanderBehavior()
    {
        // Let the AIWandering component handle wandering behavior
        if (wanderingBehavior != null && !wanderingBehavior.enabled)
        {
            wanderingBehavior.SetWanderingEnabled(true);
        }
    }
    
    // Switch between Follow and Wander modes
    public void ToggleMode()
    {
        if (currentMode == AIMode.Follow)
            SetMode(AIMode.Wander);
        else
            SetMode(AIMode.Follow);
    }
    
    // Set a specific mode
    public void SetMode(AIMode newMode)
    {
        if (currentMode == newMode)
            return;
            
        currentMode = newMode;
        
        // Apply appropriate behavior for the new mode
        if (currentMode == AIMode.Wander)
        {
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(true);
                
            Debug.Log($"{gameObject.name} switched to Wander mode");
        }
        else if (currentMode == AIMode.Follow)
        {
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
                
            Debug.Log($"{gameObject.name} switched to Follow mode");
        }
        else if (currentMode == AIMode.Carrying)
        {
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
                
            Debug.Log($"{gameObject.name} switched to Carrying mode");
        }
        else if (currentMode == AIMode.GoingToLoot)
        {
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
                
            Debug.Log($"{gameObject.name} switched to GoingToLoot mode");
        }
        else if (currentMode == AIMode.Attacking)
        {
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
                
            Debug.Log($"{gameObject.name} switched to Attacking mode");
        }
    }
    
    // Check if an entity is friendly (player or ally)
    private bool IsFriendlyEntity(LivingEntity entity)
    {
        foreach (string tag in friendlyTags)
        {
            if (entity.gameObject.CompareTag(tag))
            {
                return true;
            }
        }
        return false;
    }
    
    private void ChaseAndAttackEnemy()
    {
        // If target is lost or dead, stop attacking
        if (currentEnemyTarget == null || currentEnemyTarget.IsDead)
        {
            EndChaseAndReturnToMode();
            return;
        }

        // Update last known position while we can see the enemy
        lastKnownEnemyPosition = currentEnemyTarget.transform.position;
        
        // If we weren't already in Attacking mode, switch to it now
        if (currentMode != AIMode.Attacking)
        {
            // Store original mode before attacking begins
            if (!isInCombat)
            {
                originalMode = currentMode;
            }
            previousMode = currentMode;
            SetMode(AIMode.Attacking);
        }
        
        // Get distance to target
        float distanceToTarget = Vector3.Distance(transform.position, currentEnemyTarget.transform.position);
        
        // If we're close enough to attack
        if (distanceToTarget <= attackDistance)
        {
            // Stop moving
            isMoving = false;
            UpdateAnimation(false);
            
            // Face the target
            FaceTarget(currentEnemyTarget.transform.position);
            
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
            FaceTarget(currentEnemyTarget.transform.position);
            
            // Move with boundary check
            MoveWithBoundaryCheck(transform.forward);
        }
    }
    
    // Modified MoveWithBoundaryCheck to use smoother movement
    private void MoveWithBoundaryCheck(Vector3 direction)
    {
        // Calculate the next position with a slight smoothing factor
        Vector3 nextPosition = transform.position + direction * livingEntity.moveSpeed * Time.deltaTime;
        
        // Check if the next position is within bounds
        if (mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition))
        {
            // Get the nearest safe position inside the boundary
            Vector3 safePosition = mapBoundary.GetNearestPointInBounds(nextPosition);
            
            // Calculate new direction along the boundary
            Vector3 redirectedDirection = (safePosition - transform.position).normalized;
            
            // Move in the redirected direction
            transform.position += redirectedDirection * livingEntity.moveSpeed * Time.deltaTime;
        }
        else
        {
            // Move normally if within bounds
            transform.position += direction * livingEntity.moveSpeed * Time.deltaTime;
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
    
    private void CheckForLoot()
    {
        // Look for loot objects in a radius around the ally
        Collider[] lootColliders = Physics.OverlapSphere(transform.position, 1.5f, lootLayer);
        
        if (lootColliders.Length > 0)
        {
            // Find the closest loot
            Transform closestLoot = null;
            float closestDistance = float.MaxValue;
            
            foreach (Collider lootCollider in lootColliders)
            {
                float distance = Vector3.Distance(transform.position, lootCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestLoot = lootCollider.transform;
                }
            }
            
            if (closestLoot != null)
            {
                // Check if we're close enough to pick it up directly
                if (closestDistance <= lootPickupDistance)
                {
                    // We're close enough, pick it up immediately
                    PickUpLoot(closestLoot);
                }
                else
                {
                    // We need to approach the loot first
                    detectedLoot = closestLoot;
                    previousMode = currentMode;
                    SetMode(AIMode.GoingToLoot);
                }
            }
        }
    }
    
    private void PickUpLoot(Transform loot)
    {
        // Store the position where we picked up the loot (for returning later)
        lootPickupPosition = loot.position;
        
        // Change the layer to CarriedLoot immediately when picked up
        loot.gameObject.layer = LayerMask.NameToLayer("CarriedLoot");
        
        // Attach the loot to the ally
        loot.SetParent(transform);
        
        // Check if the loot is Chitin and position it accordingly
        if (loot.name == "Chitin(Clone)")
        {
            // For Chitin(Clone), position it at 0,0.5,0
            loot.localPosition = new Vector3(0, 0.5f, 0);
        }
        else
        {
            // For other loot, use the same position as before
            loot.localPosition = new Vector3(0, 0.5f, 0);
        }
        
        // Disable any physics on the loot
        Rigidbody lootRb = loot.GetComponent<Rigidbody>();
        if (lootRb != null)
        {
            lootRb.isKinematic = true;
        }
        
        Collider lootCollider = loot.GetComponent<Collider>();
        if (lootCollider != null)
        {
            lootCollider.enabled = false;
        }
        
        // Set as carried loot
        carriedLoot = loot;
        
        // Store the original mode before carrying (not just the previous one)
        originalMode = currentMode;
        
        // Switch to carrying mode
        SetMode(AIMode.Carrying);
        
        Debug.Log($"{gameObject.name} picked up loot and is taking it to base, will return to {originalMode} mode when done");
    }
    
    private void CarryLootToBase()
    {
        if (baseTransform == null || carriedLoot == null)
        {
            Debug.LogWarning($"{gameObject.name}: Base transform is null or loot is null. Dropping loot here.");
            if (carriedLoot != null)
            {
                DropLoot(transform.position);
            }
            SetMode(previousMode);
            return;
        }
        
        // Calculate flat distance to base (X and Z only)
        float dx = baseTransform.position.x - transform.position.x;
        float dz = baseTransform.position.z - transform.position.z;
        float flatDistance = Mathf.Sqrt(dx * dx + dz * dz);
        
        // If we've reached the base, drop the loot
        if (flatDistance <= lootDropDistance)
        {
            Debug.Log($"{gameObject.name}: Reached base. Dropping loot.");
            DropLoot(baseTransform.position);
            
            // Return to the original mode (typically Follow)
            SetMode(originalMode);
            return;
        }
        
        // Calculate normalized direction vector (X and Z only)
        float invDist = 1.0f / flatDistance;
        Vector3 moveDirection = new Vector3(
            dx * invDist,
            0f,  // No Y movement
            dz * invDist
        );
        
        // Set rotation immediately (no interpolation)
        transform.forward = moveDirection;
        
        // Update animation
        isMoving = true;
        UpdateAnimation(true);
        
        // DIRECT MOVEMENT WITH SPEED SAFEGUARD
        // Calculate exactly how far to move this frame
        float moveAmount = livingEntity.moveSpeed * Time.deltaTime;
        
        // Use MovePosition to avoid stacking with other movement systems
        Vector3 newPosition = transform.position + moveDirection * moveAmount;
        
        // Apply movement with boundary check if needed
        if (mapBoundary != null && !mapBoundary.IsWithinBounds(newPosition))
        {
            // If we're going out of bounds, use the boundary check method
            MoveWithBoundaryCheck(moveDirection);
        }
        else
        {
            // Otherwise, apply direct movement
            transform.position = newPosition;
        }
    }
    
    private void DropLoot(Vector3 dropPosition)
    {
        if (carriedLoot == null)
            return;
        
        // Detach the loot from the ally
        carriedLoot.SetParent(null);
        
        // Set position with the exact Y coordinate specified (0.02800143)
        carriedLoot.position = new Vector3(
            dropPosition.x,
            0.02800143f, // Set the exact Y position for the loot
            dropPosition.z
        );
        
        // Change the layer to CarriedLoot
        carriedLoot.gameObject.layer = LayerMask.NameToLayer("CarriedLoot");
        
        // Re-enable physics
        Rigidbody lootRb = carriedLoot.GetComponent<Rigidbody>();
        if (lootRb != null)
        {
            lootRb.isKinematic = false;
        }
        
        Collider lootCollider = carriedLoot.GetComponent<Collider>();
        if (lootCollider != null)
        {
            lootCollider.enabled = true;
        }
        
        // Clear the carried loot reference
        carriedLoot = null;
        
        // Set cooldown before picking up more loot
        nextLootCollectionTime = Time.time + lootCollectionCooldown;
        
        // Return to the original mode, not just the previous mode
        SetMode(originalMode);
        
        Debug.Log($"{gameObject.name} dropped loot at base and returning to {originalMode} mode");
    }

    public void LootDetected(Transform loot)
    {
        // Don't target loot if loot mode is disabled or
        // we're already carrying something, in combat,
        // chasing an enemy, on cooldown, or already going to loot
        if (!lootModeEnabled || carriedLoot != null || isInCombat || isChasing || 
            Time.time < nextLootCollectionTime || detectedLoot != null || 
            currentMode == AIMode.GoingToLoot)
            return;
        
        // Store the original mode before the entire loot cycle begins
        originalMode = currentMode;
        
        // Store the immediate previous mode before changing to going-to-loot
        previousMode = currentMode;
        
        // Store the detected loot
        detectedLoot = loot;
        
        // Change to going-to-loot mode
        SetMode(AIMode.GoingToLoot);
    }

    private void GoToLootBehavior()
    {
        // If loot reference is lost or was destroyed, go back to previous mode
        if (detectedLoot == null || detectedLoot.gameObject == null)
        {
            ResetLootGoalState();
            return;
        }
        
        // If the loot's layer is no longer the loot layer, stop going for it
        if (detectedLoot.gameObject.layer != LayerMask.NameToLayer("Loot"))
        {
            Debug.Log($"{gameObject.name}: Loot layer changed, no longer pursuing it.");
            ResetLootGoalState();
            return;
        }
        
        // Calculate distance to loot
        float distanceToLoot = Vector3.Distance(transform.position, detectedLoot.position);
        
        // If we're close enough to pick up
        if (distanceToLoot <= lootPickupDistance)
        {
            // Stop moving before pickup
            isMoving = false;
            UpdateAnimation(false);
            
            // Pick up the loot
            PickUpLoot(detectedLoot);
            detectedLoot = null;
            return;
        }
        
        // Otherwise, move towards the loot
        isMoving = true;
        UpdateAnimation(true);
        
        // Face and move towards loot
        FaceTarget(detectedLoot.position);
        
        // Calculate movement direction
        Vector3 moveDirection = transform.forward;
        
        // Calculate the next position using the livingEntity's moveSpeed directly
        Vector3 nextPosition = transform.position + moveDirection * livingEntity.moveSpeed * Time.deltaTime;
        
        // Apply movement with boundary check
        if (mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition))
        {
            // If we're going out of bounds, use the boundary check method
            MoveWithBoundaryCheck(moveDirection);
        }
        else
        {
            // Otherwise, apply direct movement
            transform.position = nextPosition;
        }
    }

    public void LootLeftRange(Transform loot)
    {
        // If this is the loot we're currently targeting, clear it
        if (detectedLoot == loot)
        {
            ResetLootGoalState();
        }
    }

    // New helper method to reset GoingToLoot state properly
    private void ResetLootGoalState()
    {
        detectedLoot = null;
        
        // Return to previous mode if we were going to this loot
        if (currentMode == AIMode.GoingToLoot)
        {
            SetMode(previousMode);
        }
    }

    public AIMode GetCurrentMode()
    {
        return currentMode;
    }

    // New methods to get and set loot mode
    public bool GetLootModeEnabled()
    {
        return lootModeEnabled;
    }
    
    public void SetLootModeEnabled(bool enabled)
    {
        // Only update if there's an actual change
        if (lootModeEnabled != enabled)
        {
            lootModeEnabled = enabled;
            
            // Log the state change for debugging
            Debug.Log($"{gameObject.name} loot collection mode set to: {(lootModeEnabled ? "ENABLED" : "DISABLED")}");
            
            // If we're currently going for loot but loot mode was disabled, abort the loot mission
            if (!lootModeEnabled && currentMode == AIMode.GoingToLoot)
            {
                Debug.Log($"{gameObject.name} aborting loot collection due to mode change");
                ResetLootGoalState();
            }
        }
    }

    // New method to calculate avoidance direction
    private Vector3 ApplyAllyAvoidance(Vector3 currentDirection)
    {
        // Find nearby allies
        Collider[] nearbyAllies = Physics.OverlapSphere(transform.position, allyDetectionRadius, allyLayer);
        
        if (nearbyAllies.Length <= 1) // Only this ally detected or none
            return currentDirection;
        
        Vector3 avoidanceDirection = Vector3.zero;
        int avoidanceCount = 0;
        
        foreach (Collider allyCollider in nearbyAllies)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
            
            // Calculate direction and distance to the other ally
            Vector3 directionToAlly = allyCollider.transform.position - transform.position;
            float distanceToAlly = directionToAlly.magnitude;
            
            // Only avoid if within the avoidance distance
            if (distanceToAlly < allyAvoidanceDistance)
            {
                // Add a repulsion vector (stronger when closer)
                float repulsionStrength = 1.0f - (distanceToAlly / allyAvoidanceDistance);
                avoidanceDirection -= directionToAlly.normalized * repulsionStrength;
                avoidanceCount++;
            }
        }
        
        // If we need to avoid allies
        if (avoidanceCount > 0)
        {
            // Normalize the avoidance direction
            if (avoidanceDirection.magnitude > 0)
                avoidanceDirection.Normalize();
            
            // Blend between the original direction and avoidance direction
            // More weight to avoidance when allies are very close
            Vector3 blendedDirection = Vector3.Lerp(currentDirection, avoidanceDirection, 0.6f);
            
            // Ensure we're still generally moving toward the player
            if (Vector3.Dot(blendedDirection, currentDirection) < 0)
            {
                // If the avoidance is pushing us away from the player too much,
                // use a perpendicular direction instead
                Vector3 perpendicularDir = Vector3.Cross(Vector3.up, currentDirection).normalized;
                blendedDirection = Vector3.Lerp(currentDirection, perpendicularDir, 0.5f);
            }
            
            return blendedDirection.normalized;
        }
        
        return currentDirection;
    }
}
