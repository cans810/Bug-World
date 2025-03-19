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
        }
    }
    
    private void WanderBehavior()
    {
        if (wanderingBehavior != null)
        {
            wanderingBehavior.enabled = true;
            wanderingBehavior.SetWanderingEnabled(true);
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
    
    // Your existing loot-related methods would go here
    
    // Rest of the ally-specific methods...
}