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
    [SerializeField] private float allyAvoidanceDistance = 1.0f;
    [SerializeField] private float allyDetectionRadius = 1.2f;
    [SerializeField] private LayerMask allyLayer;
    [SerializeField] private float avoidanceStrength = 1.5f;
    
    [Header("Idle Behavior")]
    [SerializeField] private bool lookAroundWhenIdle = true;
    [SerializeField] private float idleLookSpeed = 0.5f;
    [SerializeField] private float maxLookAngle = 60f;
    private float idleLookTimer = 0f;
    private Quaternion targetIdleRotation;
    private bool isLookingAround = false;
    
    private Coroutine lootPositionCoroutine;
    
    // Add this field to track if we've already applied movement this frame
    private bool hasAppliedMovementThisFrame = false;
    
    // Public property to check if movement has been applied this frame
    public bool HasAppliedMovementThisFrame => hasAppliedMovementThisFrame;
    
    [Header("Stuck Detection")]
    [SerializeField] private float stuckDetectionRadius = 0.8f;
    [SerializeField] private int stuckThreshold = 4; // Number of allies needed to consider "stuck"
    [SerializeField] private float stuckCooldown = 1.0f; // How long to wait before moving again
    private bool isStuck = false;
    private float stuckTimer = 0f;
    
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

        // Set move speed immediately and ensure it's a reasonable value
        if (playerTransform != null)
        {
            LivingEntity playerEntity = playerTransform.GetComponent<LivingEntity>();
            if (playerEntity != null)
            {
                float playerSpeed = playerEntity.moveSpeed;
                // Ensure we're not getting a zero or negative speed
                if (playerSpeed > 0.1f)
                {
                    livingEntity.moveSpeed = playerSpeed;
                    Debug.Log($"Set {gameObject.name} speed to match player: {playerSpeed}");
                }
                else
                {
                    // Use a default speed if player speed is invalid
                    livingEntity.moveSpeed = 2.0f;
                    Debug.LogWarning($"Player speed invalid ({playerSpeed}), using default speed: {livingEntity.moveSpeed}");
                }
            }
        }
        
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
        // Reset the movement flag at the beginning of each frame
        hasAppliedMovementThisFrame = false;
        
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
            
        // Apply separation from overlapping allies
        SeparateFromOverlappingAllies();
        
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
        EnsureConsistentSpeed();
        
        if (ShouldSkipMovement())
            return;
        
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
        // Store the previous mode
        previousMode = currentMode;
        
        // Set the new mode
        currentMode = newMode;
        
        // Always disable wandering behavior for any mode except Wander
        if (wanderingBehavior != null)
        {
            if (newMode == AIMode.Wander)
            {
                wanderingBehavior.enabled = true;
                wanderingBehavior.SetWanderingEnabled(true);
            }
            else
            {
                wanderingBehavior.SetWanderingEnabled(false);
                
                // Completely disable the wandering component for carrying mode
                if (newMode == AIMode.Carrying)
                {
                    wanderingBehavior.enabled = false;
                }
            }
        }
        
        // Log the mode change for debugging
        Debug.Log($"{gameObject.name} changed mode from {previousMode} to {currentMode}");
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
    private void UpdateAnimation(bool isMoving)
    {
        // Always override to idle animation if stuck
        if (isStuck && Time.time < stuckTimer)
        {
            // Force idle animation when stuck
            isMoving = false;
            
            // Debug log to verify animation state
            if (Debug.isDebugBuild && Time.frameCount % 60 == 0)
            {
                Debug.Log($"{gameObject.name} is stuck - forcing idle animation");
            }
        }
        
        if (animController != null)
        {
            animController.SetWalking(isMoving);
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
        // Look for loot objects in a radius around the ally, but ONLY on the Loot layer
        int lootLayerMask = 1 << LayerMask.NameToLayer("Loot");
        Collider[] lootColliders = Physics.OverlapSphere(transform.position, 1.5f, lootLayerMask);
        
        if (lootColliders.Length > 0)
        {
            // Find the closest loot
            Transform closestLoot = null;
            float closestDistance = float.MaxValue;
            
            foreach (Collider lootCollider in lootColliders)
            {
                // Double-check that it's on the Loot layer
                if (lootCollider.gameObject.layer != LayerMask.NameToLayer("Loot"))
                    continue;
                    
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
        Debug.LogError($"{gameObject.name} picked up {loot.name}");

        // First, disable any scripts that might interfere with positioning
        ChitinCollectible chitinCollectible = loot.GetComponent<ChitinCollectible>();
        if (chitinCollectible != null)
        {
            chitinCollectible.enabled = false;
        }
        
        // Disable physics on the loot BEFORE parenting
        Rigidbody lootRb = loot.GetComponent<Rigidbody>();
        if (lootRb != null)
        {
            lootRb.isKinematic = true;
            lootRb.velocity = Vector3.zero;
            lootRb.angularVelocity = Vector3.zero;
        }
        
        // Disable collider to prevent further interactions
        Collider lootCollider = loot.GetComponent<Collider>();
        if (lootCollider != null)
        {
            lootCollider.enabled = false;
        }
        
        // Change the layer to prevent other allies from targeting it
        loot.gameObject.layer = LayerMask.NameToLayer("CarriedLoot");
        
        // Set the loot as a child of this ally
        loot.SetParent(transform);
        
        // Force the position update in a single frame
        loot.localPosition = new Vector3(0f, 0.5f, 0f);
        
        // Force a transform update to ensure the position is applied
        loot.transform.hasChanged = true;
        
        // Log the position for debugging
        Debug.LogError($"Set {loot.name} local position to {loot.localPosition}");
        
        // Store the reference to the carried loot
        carriedLoot = loot;
        
        // Switch to carrying mode
        SetMode(AIMode.Carrying);
        
        Debug.Log($"{gameObject.name} picked up {loot.name}");
        
        // Start a coroutine to ensure the position is maintained
        if (lootPositionCoroutine != null)
        {
            StopCoroutine(lootPositionCoroutine);
        }
        lootPositionCoroutine = StartCoroutine(MaintainLootPosition());
    }
    
    private IEnumerator MaintainLootPosition()
    {
        // Run for a short time to ensure the position is maintained
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration && carriedLoot != null)
        {
            // Force the position update
            carriedLoot.localPosition = new Vector3(0f, 0.5f, 0f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        lootPositionCoroutine = null;
    }
    
    private void CarryLootToBase()
    {
        EnsureConsistentSpeed();
        
        if (ShouldSkipMovement())
            return;
        
        if (baseTransform == null || carriedLoot == null)
        {
            // If we somehow lost our carried loot or base reference, reset state
            if (carriedLoot == null)
            {
                Debug.LogError($"{gameObject.name} lost carried loot reference in CarryLootToBase");
                SetMode(originalMode);
            }
            return;
        }
        
        // Calculate distance to base
        float distanceToBase = Vector3.Distance(transform.position, baseTransform.position);
        
        // If we're close enough to drop off
        if (distanceToBase <= lootDropDistance)
        {
            // Stop moving
            isMoving = false;
            UpdateAnimation(false);
            
            // Drop the loot at the base
            DropLoot(baseTransform.position);
            return;
        }
        
        // Otherwise, move towards the base
        isMoving = true;
        UpdateAnimation(true);
        
        // Face and move towards base
        FaceTarget(baseTransform.position);
        
        // Calculate movement direction
        Vector3 moveDirection = transform.forward;
        
        // Apply ally avoidance
        moveDirection = ApplyAllyAvoidance(moveDirection);
        
        // Ensure we're using a consistent movement speed
        float currentSpeed = livingEntity.moveSpeed;
        if (currentSpeed <= 0.1f)
        {
            // If speed is too low, reset it to a reasonable value
            livingEntity.moveSpeed = 2.0f;
            Debug.LogWarning($"Detected abnormal speed ({currentSpeed}), reset to {livingEntity.moveSpeed}");
        }
        
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
            // Use direct transform position for consistent movement
            transform.position = nextPosition;
        }
        
        // Mark that we've applied movement this frame
        hasAppliedMovementThisFrame = true;
        
        // More frequent debug logging during carrying
        if (Time.frameCount % 30 == 0) // Log twice per second at 60 FPS
        {
            Debug.Log($"{gameObject.name} carrying speed: {livingEntity.moveSpeed}");
        }
    }
    
    private void DropLoot(Vector3 dropPosition)
    {
        if (carriedLoot == null)
            return;
        
        // Stop the position maintenance coroutine
        if (lootPositionCoroutine != null)
        {
            StopCoroutine(lootPositionCoroutine);
            lootPositionCoroutine = null;
        }
        
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
            lootRb.isKinematic = true;
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
        // First check if the loot is on the Loot layer
        if (loot.gameObject.layer != LayerMask.NameToLayer("Loot"))
        {
            Debug.Log($"{gameObject.name} ignoring loot {loot.name} because it's not on the Loot layer");
            return;
        }
        
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
        EnsureConsistentSpeed();
        
        if (ShouldSkipMovement())
            return;
        
        // If loot reference is lost or was destroyed, go back to previous mode
        if (detectedLoot == null || detectedLoot.gameObject == null)
        {
            ResetLootGoalState();
            return;
        }
        
        // If the loot's layer is no longer the Loot layer, stop going for it
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
        
        // Apply ally avoidance
        moveDirection = ApplyAllyAvoidance(moveDirection);
        
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
        if (!avoidOtherAllies)
            return currentDirection;
        
        // Find nearby allies
        Collider[] nearbyAllies = Physics.OverlapSphere(transform.position, allyDetectionRadius, allyLayer);
        
        if (nearbyAllies.Length <= 1) // Only this ally detected or none
            return currentDirection;
        
        Vector3 avoidanceDirection = Vector3.zero;
        int avoidanceCount = 0;
        
        // Debug visualization
        if (Debug.isDebugBuild && Time.frameCount % 30 == 0)
        {
            Debug.DrawRay(transform.position, currentDirection * 2f, Color.blue, 0.5f);
        }
        
        foreach (Collider allyCollider in nearbyAllies)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
            
            // Calculate direction and distance to the other ally
            Vector3 directionToAlly = allyCollider.transform.position - transform.position;
            
            // Only consider XZ plane for avoidance
            directionToAlly.y = 0;
            
            float distanceToAlly = directionToAlly.magnitude;
            
            // Only avoid if within the avoidance distance
            if (distanceToAlly < allyAvoidanceDistance)
            {
                // Add a repulsion vector (stronger when closer)
                float repulsionStrength = (1.0f - (distanceToAlly / allyAvoidanceDistance)) * avoidanceStrength;
                
                // Normalize direction before applying strength
                Vector3 normalizedDirection = directionToAlly.normalized;
                
                // Apply repulsion in opposite direction
                avoidanceDirection -= normalizedDirection * repulsionStrength;
                
                // Debug visualization
                if (Debug.isDebugBuild && Time.frameCount % 30 == 0)
                {
                    Debug.DrawRay(transform.position, -normalizedDirection * repulsionStrength, Color.red, 0.5f);
                }
                
                avoidanceCount++;
            }
        }
        
        // If we need to avoid allies
        if (avoidanceCount > 0)
        {
            // Normalize the avoidance direction if it's not zero
            if (avoidanceDirection.magnitude > 0.01f)
            {
                avoidanceDirection.Normalize();
            }
            
            // Debug visualization of final avoidance direction
            if (Debug.isDebugBuild && Time.frameCount % 30 == 0)
            {
                Debug.DrawRay(transform.position, avoidanceDirection * 2f, Color.yellow, 0.5f);
            }
            
            // Blend between the original direction and avoidance direction
            // More weight to avoidance when allies are very close
            float blendFactor = Mathf.Min(0.8f, 0.4f * avoidanceCount); // Cap at 0.8 to maintain some original direction
            Vector3 blendedDirection = Vector3.Lerp(currentDirection, avoidanceDirection, blendFactor);
            
            // Ensure we're still generally moving in a reasonable direction
            if (blendedDirection.magnitude < 0.1f)
            {
                // If the blended direction is too small, use a perpendicular direction
                blendedDirection = Vector3.Cross(Vector3.up, currentDirection).normalized;
            }
            
            // Debug visualization of final blended direction
            if (Debug.isDebugBuild && Time.frameCount % 30 == 0)
            {
                Debug.DrawRay(transform.position, blendedDirection * 2f, Color.green, 0.5f);
            }
            
            return blendedDirection.normalized;
        }
        
        return currentDirection;
    }

    // Add this method to the AllyAI class
    private void EnsureConsistentSpeed()
    {
        // Check if speed is valid
        if (livingEntity.moveSpeed <= 0.1f)
        {
            // Try to get player speed again
            if (playerTransform != null)
            {
                LivingEntity playerEntity = playerTransform.GetComponent<LivingEntity>();
                if (playerEntity != null && playerEntity.moveSpeed > 0.1f)
                {
                    livingEntity.moveSpeed = playerEntity.moveSpeed;
                    Debug.LogWarning($"Reset {gameObject.name} speed to match player: {livingEntity.moveSpeed}");
                }
                else
                {
                    // Use default speed
                    livingEntity.moveSpeed = 2.0f;
                    Debug.LogWarning($"Reset {gameObject.name} to default speed: {livingEntity.moveSpeed}");
                }
            }
            else
            {
                // Use default speed
                livingEntity.moveSpeed = 2.0f;
                Debug.LogWarning($"Reset {gameObject.name} to default speed: {livingEntity.moveSpeed}");
            }
        }
    }

    // Replace the SeparateFromOverlappingAllies method with this improved version
    private void SeparateFromOverlappingAllies()
    {
        if (!avoidOtherAllies)
            return;
        
        // Find allies that are too close
        Collider[] overlappingAllies = Physics.OverlapSphere(transform.position, allyAvoidanceDistance * 0.5f, allyLayer);
        
        // Count allies that are very close (for stuck detection)
        Collider[] veryCloseAllies = Physics.OverlapSphere(transform.position, stuckDetectionRadius, allyLayer);
        int nearbyAllyCount = 0;
        
        foreach (Collider ally in veryCloseAllies)
        {
            if (ally.gameObject != gameObject)
                nearbyAllyCount++;
        }
        
        // Check if we're stuck (surrounded by too many allies)
        if (nearbyAllyCount >= stuckThreshold)
        {
            if (!isStuck)
            {
                isStuck = true;
                stuckTimer = Time.time + stuckCooldown;
                Debug.Log($"{gameObject.name} is stuck with {nearbyAllyCount} allies nearby - waiting");
            }
            
            // If we're stuck, don't try to move until the cooldown expires
            if (Time.time < stuckTimer)
            {
                // Visual indicator for stuck state
                if (Debug.isDebugBuild)
                {
                    Debug.DrawRay(transform.position + Vector3.up * 0.5f, Vector3.up * 0.5f, Color.red);
                }
                return;
            }
            else
            {
                // Cooldown expired, try moving again
                isStuck = false;
            }
        }
        else
        {
            // Not stuck anymore
            isStuck = false;
        }
        
        Vector3 separationMove = Vector3.zero;
        
        foreach (Collider allyCollider in overlappingAllies)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
            
            // Calculate direction away from the ally
            Vector3 awayDirection = transform.position - allyCollider.transform.position;
            awayDirection.y = 0; // Keep on the same Y level
            
            float distance = awayDirection.magnitude;
            
            // If we're very close, apply immediate separation
            if (distance < 0.01f)
            {
                // If exactly overlapping, move in a random direction
                awayDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            }
            else if (distance < allyAvoidanceDistance * 0.5f)
            {
                // Normalize and scale by how close we are
                awayDirection = awayDirection.normalized * (1.0f - distance/(allyAvoidanceDistance * 0.5f));
            }
            else
            {
                continue; // Not close enough to need separation
            }
            
            separationMove += awayDirection;
        }
        
        // Apply separation movement if needed
        if (separationMove.magnitude > 0.01f)
        {
            // Normalize and scale the movement
            separationMove = separationMove.normalized * 0.05f; // Small fixed movement to avoid jitter
            
            // Apply the movement directly
            transform.position += separationMove;
            
            // Debug visualization
            if (Debug.isDebugBuild)
            {
                Debug.DrawRay(transform.position, separationMove * 10f, Color.magenta, 0.1f);
            }
        }
    }

    // Add this check to the beginning of each movement method
    private bool ShouldSkipMovement()
    {
        // Skip movement if we're stuck
        if (isStuck && Time.time < stuckTimer)
        {
            // Visual indicator for stuck state
            if (Debug.isDebugBuild && Time.frameCount % 10 == 0)
            {
                Debug.DrawRay(transform.position + Vector3.up * 0.5f, Vector3.up * 0.5f, Color.red);
            }
            return true;
        }
        
        // Skip if we've already moved this frame
        if (hasAppliedMovementThisFrame)
        {
            return true;
        }
        
        return false;
    }
}
