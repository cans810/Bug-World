using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    public enum AIMode
    {
        Follow,  // Follow player and attack enemies
        Wander,  // Wander around and attack enemies
        Carrying  // Carrying loot to base
    }
    
    [Header("AI Behavior")]
    [SerializeField] private AIMode currentMode = AIMode.Follow;
    [SerializeField] private bool allowModeSwitch = true;  // Allow toggling between modes
    
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
    [SerializeField] private LayerMask lootLayer;
    [SerializeField] private float lootDetectionRadius = 1.5f;
    [SerializeField] private float lootDropDistance = 1.0f;
    [SerializeField] private float lootCollectionCooldown = 3.0f; // Cooldown before collecting another loot
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
        
        // Start idle
        UpdateAnimation(false);
        
        // Find the boundary
        mapBoundary = FindObjectOfType<MapBoundary>();
        
        // Initialize mode
        SetInitialMode();
    }
    
    private void SetInitialMode()
    {
        previousMode = currentMode;
        
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
                previousMode = currentMode;
                SetMode(AIMode.Carrying);
            }
            CarryLootToBase();
            return;
        }
        
        // Check for loot if we're not carrying anything, not in combat, and cooldown has passed
        if (carriedLoot == null && !isInCombat && !isChasing && Time.time > nextLootCollectionTime)
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
                    previousMode = currentMode;
                    isInCombat = true;
                }
                
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
        
        // Return to previous behavior mode
        SetMode(previousMode);
    }
    
    private void FollowPlayerBehavior()
    {
        if (playerTransform == null)
            return;
            
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // If we need to follow (too far from player)
        if (distanceToPlayer > followDistance)
        {
            // Disable wandering behavior while following
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
            
            isMoving = true;
            UpdateAnimation(true);
            
            // Face and move towards player
            FaceTarget(playerTransform.position);
            MoveWithBoundaryCheck(transform.forward);
        }
        // If we're close enough to player, just stay in place
        else
        {
            // Disable wandering when player is close
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
            
            // Look at player but don't move
            FaceTarget(playerTransform.position);
            
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
    
    // Method to handle movement with boundary checking (same as EnemyAI)
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
        Collider[] lootColliders = Physics.OverlapSphere(transform.position, lootDetectionRadius, lootLayer);
        
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
                // Pick up the loot
                PickUpLoot(closestLoot);
            }
        }
    }
    
    private void PickUpLoot(Transform loot)
    {
        // Store the position where we picked up the loot (for returning later)
        lootPickupPosition = loot.position;
        
        // Attach the loot to the ally
        loot.SetParent(transform);
        loot.localPosition = new Vector3(0, 0.5f, 0); // Position slightly above the ally
        
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
        
        // Switch to carrying mode
        previousMode = currentMode;
        SetMode(AIMode.Carrying);
        
        Debug.Log($"{gameObject.name} picked up loot and is taking it to base");
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
            SetMode(previousMode);
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
        
        // MOST BASIC DIRECT MOVEMENT POSSIBLE
        // Calculate exactly how far to move this frame
        float moveAmount = livingEntity.moveSpeed * Time.deltaTime;
        
        // Apply direct movement (bypassing any other systems)
        transform.position += moveDirection * moveAmount;
    }
    
    private void DropLoot(Vector3 dropPosition)
    {
        if (carriedLoot == null)
            return;
        
        // Detach the loot from the ally
        carriedLoot.SetParent(null);
        
        // Only use the X and Z coordinates from drop position, keep original Y position
        carriedLoot.position = new Vector3(
            dropPosition.x,
            carriedLoot.position.y, // Keep the current Y position
            dropPosition.z
        );
        
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
        
        Debug.Log($"{gameObject.name} dropped loot at base and returning to {previousMode} mode");
    }
}
