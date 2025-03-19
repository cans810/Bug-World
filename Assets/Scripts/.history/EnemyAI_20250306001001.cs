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
    [SerializeField] private bool canAttackWhileMoving = false; // Whether this enemy can attack while moving
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] public LivingEntity livingEntity;
    
    // Internal states
    private bool isMoving = false;
    private bool isAttacking = false;
    private float lastAttackTime = -999f;
    private LivingEntity currentTarget = null;
    private AIWandering wanderingBehavior;
    private Vector3 lastKnownPlayerPosition;
    private bool isChasing = false;
    
    // Animation capabilities check
    private bool hasAttackWalkingAnimation = false;
    
    private MapBoundary mapBoundary;
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
            
        // Check if we have the attack walking animation with more detailed logging
        if (animController != null)
        {
            hasAttackWalkingAnimation = animController.HasAnimation("AttackWalking");
            
            // Log for debugging - more detailed
            Debug.Log($"{gameObject.name} checking for AttackWalking animation - Result: {hasAttackWalkingAnimation}");
            
            if (hasAttackWalkingAnimation)
                Debug.Log($"{gameObject.name} has AttackWalking animation - can attack while moving");
            else
                Debug.Log($"{gameObject.name} does NOT have AttackWalking animation - available params: {GetAnimatorParameterNames()}");
        }
        
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
        UpdateAnimation(false, false);
        
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
        isAttacking = false;
        isChasing = false;
        
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
                isAttacking = false;
                UpdateAnimation(true, false);
                
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
        
        // Face the target always
        FaceTarget(currentTarget.transform.position);
        
        // If we're close enough to attack
        if (distanceToTarget <= minAttackDistance)
        {
            // Try to attack if cooldown has passed
            if (Time.time >= lastAttackTime + attackInterval)
            {
                isAttacking = true;
                
                // If we can attack while moving and have the animation
                if (canAttackWhileMoving && hasAttackWalkingAnimation)
                {
                    // Continue moving while attacking if not at minimum distance
                    if (distanceToTarget > attackDistance)
                    {
                        isMoving = true;
                        
                        // COMPLETELY disable wandering component to prevent interference
                        if (wanderingBehavior != null)
                            wanderingBehavior.enabled = false;
                            
                        MoveWithBoundaryCheck(transform.forward);
                    }
                    else
                    {
                        isMoving = false;
                    }
                }
                else
                {
                    // Traditional attack: stop to attack
                    isMoving = false;
                }
                
                // Update animation state
                UpdateAnimation(isMoving, isAttacking);
                
                // Perform the attack
                if (livingEntity.TryAttack())
                {
                    lastAttackTime = Time.time;
                }
            }
            // If on attack cooldown but can move while attacking
            else if (canAttackWhileMoving && hasAttackWalkingAnimation)
            {
                // Continue moving while in attack cooldown
                if (distanceToTarget > attackDistance)
                {
                    isMoving = true;
                    isAttacking = true; // Keep attack state true for animation blending
                    
                    // COMPLETELY disable wandering component to prevent interference
                    if (wanderingBehavior != null)
                        wanderingBehavior.enabled = false;
                        
                    MoveWithBoundaryCheck(transform.forward);
                    
                    // Force all other animation bools to false before setting attack walking
                    if (animController != null && animController.Animator != null)
                    {
                        animController.Animator.SetBool("Idle", false);
                        animController.Animator.SetBool("Walk", false);
                        animController.Animator.SetBool("Attack", false);
                    }
                    
                    UpdateAnimation(true, true); // Explicitly set both to true for attack walking
                }
                else
                {
                    isMoving = false;
                    isAttacking = true; // Keep attack state true for animation
                    UpdateAnimation(false, isAttacking);
                }
            }
            // If on attack cooldown and can't move while attacking
            else
            {
                // Wait for attack cooldown while standing
                isMoving = false;
                UpdateAnimation(false, false);
            }
        }
        // If not in attack range, just chase
        else
        {
            isMoving = true;
            isAttacking = false;
            UpdateAnimation(true, false);
            
            // Move with boundary check
            MoveWithBoundaryCheck(transform.forward);
        }
    }
    
    private void ReturnToWandering()
    {
        isChasing = false;
        currentTarget = null;
        isAttacking = false;
        
        // Stop moving
        isMoving = false;
        UpdateAnimation(false, false);
        
        // Re-enable wandering behavior
        if (wanderingBehavior != null)
        {
            wanderingBehavior.enabled = true;
            wanderingBehavior.SetWanderingEnabled(true);
        }
    }
    
    // Updated helper method to work with Any State transitions to AttackWalking
    private void UpdateAnimation(bool isWalking, bool isAttacking)
    {
        if (animController == null)
            return;
            
        Debug.Log($"{gameObject.name} - Animation request: Walking={isWalking}, Attacking={isAttacking}, HasAttackWalkingAnim={hasAttackWalkingAnimation}");
        
        // For attack walking state, we need a special approach since it uses Any State transitions
        if (isWalking && isAttacking && hasAttackWalkingAnimation)
        {
            // Turn off ALL animations first to force a return to "Any State"
            Animator animator = animController.Animator;
            if (animator != null)
            {
                // Turn off all current animations to force return to Any State
                animator.SetBool("Idle", false);
                animator.SetBool("Walk", false);
                animator.SetBool("Attack", false);
                
                // Force an update to apply the changes and get back to Any State
                animator.Update(0.0f);
                
                // Now set the AttackWalking trigger to transition from Any State
                animator.SetBool("AttackWalking", true);
                
                Debug.Log("Set AttackWalking=true - transition from Any State should occur");
            }
        }
        else
        {
            // Make sure AttackWalking is turned off
            if (hasAttackWalkingAnimation && animController.Animator != null)
            {
                animController.Animator.SetBool("AttackWalking", false);
            }
            
            // Normal animation handling
            animController.SetWalking(isWalking);
            animController.SetAttacking(isAttacking);
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
    
    // Helper method to get all animator parameter names for debugging
    private string GetAnimatorParameterNames()
    {
        if (animController == null || animController.Animator == null)
            return "No Animator";
        
        string paramNames = "";
        foreach (AnimatorControllerParameter param in animController.Animator.parameters)
        {
            paramNames += param.name + ", ";
        }
        return paramNames;
    }
} 