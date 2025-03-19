using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    [SerializeField] private bool allowModeSwitch = true;
    private AIMode originalMode;
    
    [Header("Movement Settings")]
    [SerializeField] private float followDistance = 3f;
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float detectionFollowRange = 10f;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private string[] friendlyTags = new string[] { "Player", "Ally" };
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    [Header("Base Settings")]
    public Transform baseTransform;
    
    [Header("Loot Collection")]
    [SerializeField] private bool lootModeEnabled = true;
    [SerializeField] private LayerMask lootLayer;
    [SerializeField] private float lootDropDistance = 1.0f;
    [SerializeField] private float lootCollectionCooldown = 3.0f;
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
    private AIMode previousMode;
    private bool isInCombat = false;
    private Vector3 lastKnownEnemyPosition;
    
    private MapBoundary mapBoundary;
    private Transform detectedLoot = null;
    
    // Add this field for AIWandering
    private bool hasAppliedMovementThisFrame = false;
    
    // Public property to check if movement has been applied this frame
    public bool HasAppliedMovementThisFrame => hasAppliedMovementThisFrame;
    
    // Add this simple class at the top of the file, outside the AllyAI class
    [System.Serializable]
    public class LootData
    {
        public string resourceType = "Chitin";
        public int resourceAmount = 1;
    }
    
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
        
        // Set move speed
        if (playerTransform != null)
        {
            LivingEntity playerEntity = playerTransform.GetComponent<LivingEntity>();
            if (playerEntity != null && playerEntity.moveSpeed > 0.1f)
            {
                livingEntity.moveSpeed = playerEntity.moveSpeed;
            }
            else
            {
                livingEntity.moveSpeed = 2.0f;
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
        
        if (currentMode == AIMode.Wander)
        {
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(true);
        }
        else if (currentMode == AIMode.Follow)
        {
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);
        }
    }
    
    private void OnDestroy()
    {
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
        
        // If we're already carrying loot, skip everything else and go to base
        if (carriedLoot != null)
        {
            if (currentMode != AIMode.Carrying)
            {
                originalMode = previousMode;
                SetMode(AIMode.Carrying);
            }
            CarryLootToBase();
            return;
        }
        
        // Check for loot if conditions are right
        if (carriedLoot == null && !isInCombat && !isChasing && Time.time > nextLootCollectionTime 
            && currentMode != AIMode.GoingToLoot && lootModeEnabled)
        {
            CheckForLoot();
        }
        
        // Check if we have any enemy targets in range
        if (livingEntity.HasTargetsInRange())
        {
            LivingEntity potentialTarget = livingEntity.GetClosestValidTarget();
            
            // Skip friendly targets
            if (potentialTarget != null && !potentialTarget.IsDead && !IsFriendlyEntity(potentialTarget))
            {
                if (!isInCombat)
                {
                    originalMode = currentMode;
                }
                previousMode = currentMode;
                isInCombat = true;
                
                if (wanderingBehavior != null)
                    wanderingBehavior.SetWanderingEnabled(false);
                
                currentEnemyTarget = potentialTarget;
                isChasing = true;
                lastKnownEnemyPosition = currentEnemyTarget.transform.position;
                
                ChaseAndAttackEnemy();
                return;
            }
        }
        else if (isChasing && currentEnemyTarget == null)
        {
            float distanceToLastKnown = Vector3.Distance(transform.position, lastKnownEnemyPosition);
            
            if (distanceToLastKnown > attackDistance && distanceToLastKnown < detectionFollowRange)
            {
                isMoving = true;
                UpdateAnimation(true);
                
                // Use exact enemy movement code
                FaceTarget(lastKnownEnemyPosition);
                MoveWithBoundaryCheck(transform.forward);
                
                // Mark that we've applied movement this frame
                hasAppliedMovementThisFrame = true;
                
                if (distanceToLastKnown <= attackDistance)
                {
                    EndChaseAndReturnToMode();
                }
                
                return;
            }
            else
            {
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
                if (currentEnemyTarget == null || currentEnemyTarget.IsDead)
                {
                    EndChaseAndReturnToMode();
                }
                break;
        }
    }
    
    private void EndChaseAndReturnToMode()
    {
        isInCombat = false;
        isChasing = false;
        currentEnemyTarget = null;
        
        if (originalMode == AIMode.Wander && wanderingBehavior != null)
        {
            wanderingBehavior.enabled = true;
        }
        
        SetMode(originalMode);
    }
    
    private void FollowPlayerBehavior()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
                return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distanceToPlayer <= followDistance)
        {
            isMoving = false;
            UpdateAnimation(false);
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
            }
        }
        else
        {
            isMoving = true;
            UpdateAnimation(true);
            
            // Exact EnemyAI movement code
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            FaceTarget(playerTransform.position);
            MoveWithBoundaryCheck(transform.forward);
            
            // Mark that we've applied movement this frame
            hasAppliedMovementThisFrame = true;
        }
    }
    
    private void WanderBehavior()
    {
        if (wanderingBehavior != null)
        {
            wanderingBehavior.enabled = true;
            wanderingBehavior.SetWanderingEnabled(true);
            
            // Mark that we've applied movement through wandering
            hasAppliedMovementThisFrame = true;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} has no AIWandering component");
            SetMode(AIMode.Follow);
        }
    }
    
    private void MoveWithBoundaryCheck(Vector3 direction)
    {
        if (direction.magnitude < 0.1f)
            return;
        
        direction.Normalize();
        
        Vector3 nextPosition = transform.position + direction * livingEntity.moveSpeed * Time.deltaTime;
        
        bool isOutsideBoundary = mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition);
        
        if (isOutsideBoundary)
        {
            Vector3 safePosition = mapBoundary.GetNearestPointInBounds(nextPosition);
            Vector3 redirectedDirection = (safePosition - transform.position).normalized;
            
            livingEntity.RotateTowards(redirectedDirection, 2.0f);
            livingEntity.MoveInDirection(redirectedDirection);
        }
        else
        {
            livingEntity.MoveInDirection(direction);
        }
    }
    
    private void FaceTarget(Vector3 target)
    {
        Vector3 directionToTarget = target - transform.position;
        directionToTarget.y = 0; // Keep rotation on Y axis only
        
        if (directionToTarget.magnitude > 0.1f)
        {
            livingEntity.RotateTowards(directionToTarget, 1.5f);
        }
    }
    
    private void ChaseAndAttackEnemy()
    {
        if (currentEnemyTarget == null || currentEnemyTarget.IsDead)
        {
            EndChaseAndReturnToMode();
            return;
        }

        lastKnownEnemyPosition = currentEnemyTarget.transform.position;
        
        if (currentMode != AIMode.Attacking)
        {
            if (!isInCombat)
            {
                originalMode = currentMode;
            }
            previousMode = currentMode;
            SetMode(AIMode.Attacking);
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, currentEnemyTarget.transform.position);
        
        if (distanceToTarget <= attackDistance)
        {
            isMoving = false;
            UpdateAnimation(false);
            
            FaceTarget(currentEnemyTarget.transform.position);
            
            if (Time.time >= lastAttackTime + attackInterval)
            {
                if (livingEntity.TryAttack())
                {
                    lastAttackTime = Time.time;
                }
            }
        }
        else
        {
            isMoving = true;
            UpdateAnimation(true);
            
            FaceTarget(currentEnemyTarget.transform.position);
            MoveWithBoundaryCheck(transform.forward);
            
            // Mark that we've applied movement this frame
            hasAppliedMovementThisFrame = true;
        }
    }
    
    private void UpdateAnimation(bool isWalking)
    {
        if (animController != null)
        {
            animController.SetWalking(isWalking);
        }
    }
    
    public void ToggleMode()
    {
        if (currentMode == AIMode.Follow)
            SetMode(AIMode.Wander);
        else
            SetMode(AIMode.Follow);
    }
    
    public void SetMode(AIMode newMode)
    {
        if (newMode == currentMode)
            return;
        
        previousMode = currentMode;
        currentMode = newMode;
        
        if (newMode == AIMode.Wander)
        {
            if (wanderingBehavior != null)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                }
                
                if (animController != null)
                {
                    animController.SetIdle();
                }
                
                wanderingBehavior.enabled = true;
                wanderingBehavior.SetWanderingEnabled(true);
                wanderingBehavior.ForceNewWaypoint();
            }
        }
        else
        {
            if (wanderingBehavior != null)
            {
                wanderingBehavior.SetWanderingEnabled(false);
                wanderingBehavior.enabled = false;
            }
        }
    }
    
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
    
    // LOOT CARRYING METHODS
    
    // This method is called when loot is detected by the EntityHitbox
    public void LootDetected(Transform loot)
    {
        // Skip if we're already chasing something or carrying loot
        if (isInCombat || isChasing || carriedLoot != null || !lootModeEnabled)
            return;
        
        // Store the detected loot
        detectedLoot = loot;
        
        // Change to going-to-loot mode
        SetMode(AIMode.GoingToLoot);
    }

    // Called when loot leaves detection range
    public void LootLeftRange(Transform loot)
    {
        // If this is the loot we're currently targeting, clear it
        if (detectedLoot == loot)
        {
            ResetLootGoalState();
        }
    }
    
    private void CheckForLoot()
    {
        if (!lootModeEnabled || isInCombat || isChasing)
            return;
            
        // Find the nearest loot in range
        Collider[] nearbyLoot = Physics.OverlapSphere(transform.position, detectionFollowRange, lootLayer);
        
        if (nearbyLoot.Length == 0)
            return;
        
        // Find the closest loot
        Transform closestLoot = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider lootCollider in nearbyLoot)
        {
            // Skip if already being carried
            if (lootCollider.transform.parent != null && lootCollider.transform.parent.CompareTag("Ally"))
                continue;
                
            float distance = Vector3.Distance(transform.position, lootCollider.transform.position);
            if (distance < closestDistance)
            {
                closestLoot = lootCollider.transform;
                closestDistance = distance;
            }
        }
        
        if (closestLoot != null)
        {
            LootDetected(closestLoot);
        }
    }
    
    private void GoToLootBehavior()
    {
        // If loot reference is lost or was destroyed, go back to previous mode
        if (detectedLoot == null || detectedLoot.gameObject == null)
        {
            ResetLootGoalState();
            return;
        }
        
        // If the loot's layer is no longer the Loot layer, stop going for it
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
        
        // Use the same movement code as enemy AI
        FaceTarget(detectedLoot.position);
        MoveWithBoundaryCheck(transform.forward);
        
        // Mark that we've applied movement this frame
        hasAppliedMovementThisFrame = true;
    }
    
    private void PickUpLoot(Transform loot)
    {
        // Change layer to show it's being carried
        loot.gameObject.layer = LayerMask.NameToLayer("CarriedLoot");
        
        // Attach to this ally
        loot.SetParent(transform);
        
        // Position just behind the ally
        loot.localPosition = new Vector3(0, 0.5f, -0.5f);
        
        // Disable physics on the loot
        Rigidbody lootRb = loot.GetComponent<Rigidbody>();
        if (lootRb != null)
        {
            lootRb.isKinematic = true;
            lootRb.detectCollisions = false;
        }
        
        // Disable any colliders
        Collider[] lootColliders = loot.GetComponentsInChildren<Collider>();
        foreach (Collider col in lootColliders)
        {
            col.enabled = false;
        }
        
        // Store reference to carried loot
        carriedLoot = loot;
        
        // Switch to carrying mode
        SetMode(AIMode.Carrying);
    }
    
    private void CarryLootToBase()
    {
        if (carriedLoot == null || baseTransform == null)
        {
            // If we lost the loot or don't have a base, return to normal mode
            if (carriedLoot == null && baseTransform == null)
            {
                SetMode(originalMode);
            }
            return;
        }
        
        float distanceToBase = Vector3.Distance(transform.position, baseTransform.position);
        
        // If we've reached the base
        if (distanceToBase <= lootDropDistance)
        {
            // Stop moving
            isMoving = false;
            UpdateAnimation(false);
            
            // Drop the loot
            DropLootAtBase();
            
            // Return to original mode
            SetMode(originalMode);
            
            // Set cooldown before collecting another loot
            nextLootCollectionTime = Time.time + lootCollectionCooldown;
        }
        else
        {
            // Move towards the base
            isMoving = true;
            UpdateAnimation(true);
            
            // Use the same movement code as enemy AI
            FaceTarget(baseTransform.position);
            MoveWithBoundaryCheck(transform.forward);
            
            // Mark that we've applied movement this frame
            hasAppliedMovementThisFrame = true;
        }
    }
    
    private void DropLootAtBase()
    {
        if (carriedLoot == null)
            return;
        
        // Detach from ally
        carriedLoot.SetParent(null);
        
        // Position at the base
        carriedLoot.position = baseTransform.position;
        
        // Re-enable physics
        Rigidbody lootRb = carriedLoot.GetComponent<Rigidbody>();
        if (lootRb != null)
        {
            lootRb.isKinematic = false;
            lootRb.detectCollisions = true;
        }
        
        // Process the loot (add resources or whatever happens when loot is delivered)
        ProcessLootDelivery(carriedLoot);
        
        // Clear the reference
        carriedLoot = null;
        
        // Return to previous mode
        SetMode(originalMode);
    }
    
    private void ProcessLootDelivery(Transform loot)
    {
        // Simplified resource processing - adapt to your game's systems
        
        // Try to get loot data component if it exists
        LootData lootData = loot.GetComponent<LootData>();
        
        // Find any resource manager in the scene
        ResourceManager resourceManager = FindObjectOfType<ResourceManager>();
        if (resourceManager != null)
        {
            // Use your resource system to add resources
            if (lootData != null)
            {
                resourceManager.AddResource(lootData.resourceType, lootData.resourceAmount);
                Debug.Log($"Added {lootData.resourceAmount} {lootData.resourceType} resources from loot");
            }
            else
            {
                // Default resource if no LootData component
                resourceManager.AddResource("Chitin", 1);
                Debug.Log("Added 1 Chitin resource from loot (default)");
            }
        }
        else
        {
            // Fallback if no resource system is found
            Debug.Log("Loot delivered to base, but no ResourceManager found to process it");
            
            // Instead, we can try to find GameManager and use it directly
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                // Use SendMessage to try calling a method on GameManager without knowing its exact properties
                gameManager.SendMessage("AddChitin", 1, SendMessageOptions.DontRequireReceiver);
                Debug.Log("Attempted to add resources via GameManager.SendMessage");
            }
        }
        
        // Destroy the loot object
        Destroy(loot.gameObject);
    }
    
    // Helper method to reset GoingToLoot state properly
    private void ResetLootGoalState()
    {
        detectedLoot = null;
        
        // Return to previous mode if we were going to this loot
        if (currentMode == AIMode.GoingToLoot)
        {
            SetMode(previousMode);
        }
    }
    
    // Public methods to get and set current state
    public AIMode GetCurrentMode()
    {
        return currentMode;
    }
    
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
}