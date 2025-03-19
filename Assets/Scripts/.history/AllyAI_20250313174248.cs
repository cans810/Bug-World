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

    
    private Coroutine lootPositionCoroutine;
    
    // Add this field to track if we've already applied movement this frame
    private bool hasAppliedMovementThisFrame = false;
    
    // Public property to check if movement has been applied this frame
    public bool HasAppliedMovementThisFrame => hasAppliedMovementThisFrame;
    
    [Header("Movement Tuning")]
    [SerializeField] private float speedMultiplier = 1.5f; // Increase ally speed
    [SerializeField] private float rotationMultiplier = 2.0f; // Increase rotation speed
    
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
        {
            wanderingBehavior.SetWanderingEnabled(false);
            wanderingBehavior.enabled = false;
        }
        
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
        
        // Make sure the wandering component is re-enabled if we're returning to Wander mode
        if (originalMode == AIMode.Wander && wanderingBehavior != null)
        {
            wanderingBehavior.enabled = true;
        }
        
        // Return to original behavior mode, not just previous
        SetMode(originalMode);
    }
    
    private void FollowPlayerBehavior()
    {
        // If player is not found, try to find again
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
                return; // Still can't find player
        }
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Update logic for following
        if (distanceToPlayer > followDistance + 1.0f) // Add a small buffer to prevent constant stopping/starting
        {
            isMoving = true;
            
            // Calculate direction to player
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            
            // Apply avoidance with other allies
            if (avoidOtherAllies)
            {
                directionToPlayer = ApplyAllyAvoidance(directionToPlayer, allyDetectionRadius);
            }
            
            // Update rotation and movement using LivingEntity
            livingEntity.RotateTowards(directionToPlayer, rotationMultiplier);
            livingEntity.MoveInDirection(directionToPlayer, speedMultiplier);
            
            // Update animation
            if (animController != null && !animController.IsAnimationPlaying("walk"))
            {
                animController.SetWalking(true);
            }
            
            hasAppliedMovementThisFrame = true;
        }
        else
        {
            // Close enough, stop moving but keep facing player
            isMoving = false;
            
            // Calculate direction to player for facing
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            livingEntity.RotateTowards(directionToPlayer, rotationMultiplier * 0.5f);
            
            // Update animation to idle
            if (animController != null && animController.IsAnimationPlaying("walk"))
            {
                animController.SetWalking(false);
            }
            
            // Stop movement
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
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
            // First, make sure the component itself is enabled
            if (newMode == AIMode.Wander || (newMode == originalMode && originalMode == AIMode.Wander))
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
    private void MoveWithBoundaryCheck(Vector3 direction, float speedMultiplier = 1.0f)
    {
        // Skip if direction is too small
        if (direction.magnitude < 0.1f)
            return;
        
        direction.Normalize();
        
        // Calculate the next position
        Vector3 nextPosition = transform.position + direction * livingEntity.moveSpeed * speedMultiplier * Time.deltaTime;
        
        // Check if the next position is outside the boundary
        if (mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition))
        {
            // Get the nearest safe position inside the boundary
            Vector3 safePosition = mapBoundary.GetNearestPointInBounds(nextPosition);
            
            // Calculate new direction along the boundary
            Vector3 redirectedDirection = (safePosition - transform.position).normalized;
            
            // Rotate towards the safe direction
            livingEntity.RotateTowards(redirectedDirection, rotationMultiplier);
            
            // Move in the redirected direction
            livingEntity.MoveInDirection(redirectedDirection, speedMultiplier);
        }
        else
        {
            // Move normally if within bounds using the centralized method
            livingEntity.MoveInDirection(direction, speedMultiplier);
        }
        
        hasAppliedMovementThisFrame = true;
    }
    
    // Helper method to update animations
    private void UpdateAnimation(bool isWalking)
    {
        if (animController == null) return;
        
        try
        {
            // Only update if the state is changing
            if (animController.IsAnimationPlaying("walk") != isWalking)
            {
                // Use direct parameter setting instead of CrossFade
                Animator animator = animController.Animator;
                if (animator != null)
                {
                    animator.SetBool("Walk", isWalking);
                    animator.SetBool("Idle", !isWalking);
                }
                else
                {
                    // Fallback to AnimationController method
                    animController.SetWalking(isWalking);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating animation on {gameObject.name}: {e.Message}");
        }
    }
    
    private void FaceTarget(Vector3 target)
    {
        Vector3 directionToTarget = target - transform.position;
        directionToTarget.y = 0; // Keep rotation on Y axis only
        
        if (directionToTarget.magnitude > 0.1f)
        {
            // Use LivingEntity's rotation method
            livingEntity.RotateTowards(directionToTarget, rotationMultiplier);
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
        
        // If we've already moved this frame, skip movement
        if (hasAppliedMovementThisFrame)
        {
            Debug.LogWarning($"{gameObject.name} skipped movement in CarryLootToBase - already moved this frame");
            return;
        }
        
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
        if (avoidOtherAllies)
        {
            moveDirection = ApplyAllyAvoidance(moveDirection, allyDetectionRadius);
        }
        
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
        
        // Set position with the exact Y coordinate specified
        carriedLoot.position = new Vector3(
            dropPosition.x,
            0.02800143f,
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
        
        // Make sure the wandering component is re-enabled if we're returning to Wander mode
        if (originalMode == AIMode.Wander && wanderingBehavior != null)
        {
            wanderingBehavior.enabled = true;
            wanderingBehavior.SetWanderingEnabled(true);
            // Force a new waypoint to prevent getting stuck
            wanderingBehavior.ForceNewWaypoint();
        }
        else if (originalMode == AIMode.Follow)
        {
            // For Follow mode, ensure wandering is disabled
            if (wanderingBehavior != null)
            {
                wanderingBehavior.enabled = false;
                wanderingBehavior.SetWanderingEnabled(false);
            }
        }
        
        // Return to the original mode
        SetMode(originalMode);
        
        // Force an immediate state update
        isMoving = false;
        UpdateAnimation(false);
        
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
        // If loot reference is lost or was destroyed, go back to previous mode
        if (detectedLoot == null || detectedLoot.gameObject == null)
        {
            ResetLootGoalState();
            return;
        }
        
        // If the loot's layer is no longer the Loot layer, stop going for it
        // This will catch cases where another ally has picked it up (layer changes to CarriedLoot)
        if (detectedLoot.gameObject.layer != LayerMask.NameToLayer("Loot"))
        {
            Debug.Log($"{gameObject.name}: Loot layer changed or picked up by another ally, no longer pursuing it.");
            ResetLootGoalState();
            return;
        }
        
        // Check if the loot has a parent (meaning another ally picked it up)
        if (detectedLoot.parent != null)
        {
            Debug.Log($"{gameObject.name}: Loot was picked up by another ally, no longer pursuing it.");
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
        if (avoidOtherAllies)
        {
            moveDirection = ApplyAllyAvoidance(moveDirection, allyDetectionRadius);
        }
        