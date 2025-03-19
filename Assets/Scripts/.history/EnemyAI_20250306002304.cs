using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack
    [SerializeField] private float detectionFollowRange = 10f; // Max range to follow player after detection
    [SerializeField] private float chaseSpeedMultiplier = 1.3f; // Multiplier to boost speed when chasing
    
    [Header("Attack Settings")]
    [SerializeField] private float attackInterval = 1.5f; // Minimum time between attacks
    [SerializeField] private float minAttackDistance = 1.0f; // Minimum distance required to attack player
    [SerializeField] private float preAttackDelay = 0.5f; // Delay between stopping and initiating attack
    
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
    private bool isPreparingAttack = false; // New state for attack preparation
    private float attackPreparationStartTime; // When the attack preparation started
    
    private MapBoundary mapBoundary;
    
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
        isPreparingAttack = false; // Reset preparation state on death
        
        enabled = false;
    }
    
    private void Update()
    {
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
        
        // Check if we have any targets in range detected by our hitbox
        if (livingEntity.HasTargetsInRange())
        {
            // Get the closest valid target from the livingEntity
            currentTarget = livingEntity.GetClosestValidTarget();
            
            // If we found a target, start chasing
            if (currentTarget != null && !currentTarget.IsDead)
            {
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
    
    // Modified method to handle movement with boundary check and chase speed
    private void MoveWithBoundaryCheck(Vector3 direction, bool isChasing = false)
    {
        // Apply chase speed multiplier when chasing
        float currentSpeed = livingEntity.moveSpeed;
        if (isChasing) 
        {
            currentSpeed *= chaseSpeedMultiplier;
        }
        
        // Calculate the next position
        Vector3 nextPosition = transform.position + direction * currentSpeed * Time.deltaTime;
        
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
            transform.position += redirectedDirection * currentSpeed * Time.deltaTime;
        }
        else
        {
            // Move normally if within bounds
            transform.position += direction * currentSpeed * Time.deltaTime;
        }
    }
    
    private void ChaseAndAttackTarget()
    {
        // Update the last known position continuously while chasing
        lastKnownPlayerPosition = currentTarget.transform.position;
        
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
            
            // If we're not yet preparing to attack, start the preparation phase
            if (!isPreparingAttack && Time.time >= lastAttackTime + attackInterval)
            {
                isPreparingAttack = true;
                attackPreparationStartTime = Time.time;
            }
            
            // If we're in the preparation phase and enough time has passed, execute the attack
            if (isPreparingAttack && Time.time >= attackPreparationStartTime + preAttackDelay)
            {
                // Reset the preparation state
                isPreparingAttack = false;
                
                // Try to attack
                if (livingEntity.TryAttack())
                {
                    lastAttackTime = Time.time;
                }
            }
        }
        // Otherwise, move towards the target
        else
        {
            // Cancel attack preparation if we moved out of range
            if (isPreparingAttack)
            {
                isPreparingAttack = false;
            }
            
            isMoving = true;
            UpdateAnimation(true);
            
            // Face and move towards target - calculate direction directly toward player
            Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
            directionToTarget.y = 0; // Keep movement on ground plane
            
            FaceTarget(currentTarget.transform.position);
            
            // Use chase speed when pursuing the player
            MoveWithBoundaryCheck(directionToTarget, true);
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
} 