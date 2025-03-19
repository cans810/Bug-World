using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField] private float obstacleDetectionRange = 1.5f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackInterval = 1.5f; // Minimum time between attacks
    
    [Header("Wander Settings")]
    [SerializeField] private bool wanderWhenIdle = true;
    [SerializeField] private float minWanderDistance = 3f;
    [SerializeField] private float maxWanderDistance = 8f;
    [SerializeField] private float wanderWaitTime = 3f;
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    [SerializeField] private EntityHitbox hitbox;
    
    // AI States
    private enum AIState { Idle, Wander, Chase, Attack }
    private AIState currentState = AIState.Idle;
    
    // Target tracking
    private LivingEntity targetEntity;
    private float lastAttackTime = -999f;
    
    // Wandering
    private Vector3 wanderTarget;
    private bool isWandering = false;
    private float nextWanderTime = 0f;
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
            
        if (hitbox == null)
            hitbox = GetComponentInChildren<EntityHitbox>();
            
        // Initialize
        if (wanderWhenIdle)
            StartCoroutine(WanderRoutine());
    }
    
    private void Update()
    {
        // Don't do anything if dead
        if (livingEntity.IsDead)
            return;
            
        // Check if we have a target in range (relies on EntityHitbox)
        CheckForTarget();
        
        // Handle AI state behavior
        switch (currentState)
        {
            case AIState.Idle:
                // Do nothing, or consider wandering
                if (wanderWhenIdle && Time.time > nextWanderTime && !isWandering)
                {
                    SetWanderTarget();
                    isWandering = true;
                    currentState = AIState.Wander;
                }
                break;
                
            case AIState.Wander:
                // Wander around until we detect a target
                if (isWandering)
                {
                    WanderBehavior();
                }
                break;
                
            case AIState.Chase:
                // Chase the target
                ChaseTarget();
                break;
                
            case AIState.Attack:
                // Face the target and attack
                if (targetEntity != null)
                {
                    FaceTarget(targetEntity.transform.position);
                    AttemptAttack();
                }
                else
                {
                    // Lost track of target during attack
                    currentState = AIState.Idle;
                }
                break;
        }
    }
    
    private void CheckForTarget()
    {
        // Don't look for targets if we're already attacking
        if (currentState == AIState.Attack && livingEntity.IsAttacking)
            return;
        
        // Instead of accessing targetsInRange directly, use CurrentTarget
        targetEntity = livingEntity.CurrentTarget;
        
        if (targetEntity != null && !targetEntity.IsDead)
        {
            // We have a target, determine if we should chase or attack
            float distanceToTarget = Vector3.Distance(transform.position, targetEntity.transform.position);
            
            if (distanceToTarget <= attackDistance)
            {
                // Close enough to attack
                currentState = AIState.Attack;
            }
            else
            {
                // Too far to attack, chase the target
                currentState = AIState.Chase;
            }
        }
        else
        {
            // No target in range, go back to idle if we were chasing or attacking
            if (currentState == AIState.Chase || currentState == AIState.Attack)
            {
                currentState = AIState.Idle;
                
                // Stop animation if we were attacking
                if (animController != null)
                {
                    animController.SetWalking(false);
                }
            }
        }
    }
    
    private void ChaseTarget()
    {
        if (targetEntity == null || targetEntity.IsDead)
        {
            // Target is lost or dead
            currentState = AIState.Idle;
            return;
        }
        
        Vector3 targetPosition = targetEntity.transform.position;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // If close enough to attack
        if (distanceToTarget <= attackDistance)
        {
            currentState = AIState.Attack;
            return;
        }
        
        // Move towards target
        MoveTowardsPosition(targetPosition);
        
        // Update animation
        if (animController != null)
        {
            animController.SetWalking(true);
        }
    }
    
    private void MoveTowardsPosition(Vector3 targetPosition)
    {
        // Calculate direction
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep on same Y level
        
        // Check for obstacles if enabled
        if (avoidObstacles)
        {
            direction = AvoidObstacles(direction);
        }
        
        // Rotate towards target
        if (direction != Vector3.zero)
        {
            FaceTarget(transform.position + direction);
        }
        
        // Move forwards
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
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
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    private Vector3 AvoidObstacles(Vector3 moveDirection)
    {
        // Cast rays to detect obstacles
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, moveDirection);
        if (Physics.Raycast(ray, obstacleDetectionRange, obstacleLayer))
        {
            // Try a few alternate directions
            for (int i = 30; i <= 150; i += 30)
            {
                Vector3 rightDirection = Quaternion.Euler(0, i, 0) * moveDirection;
                Ray rightRay = new Ray(transform.position + Vector3.up * 0.5f, rightDirection);
                if (!Physics.Raycast(rightRay, obstacleDetectionRange, obstacleLayer))
                {
                    return rightDirection;
                }
                
                Vector3 leftDirection = Quaternion.Euler(0, -i, 0) * moveDirection;
                Ray leftRay = new Ray(transform.position + Vector3.up * 0.5f, leftDirection);
                if (!Physics.Raycast(leftRay, obstacleDetectionRange, obstacleLayer))
                {
                    return leftDirection;
                }
            }
            
            // If all directions are blocked, try going back
            return -moveDirection;
        }
        
        return moveDirection;
    }
    
    private void AttemptAttack()
    {
        // Only attack if not currently attacking and enough time has passed
        if (!livingEntity.IsAttacking && Time.time > lastAttackTime + attackInterval)
        {
            if (livingEntity.TryAttack())
            {
                lastAttackTime = Time.time;
                
                // Make sure we're not wandering
                isWandering = false;
                
                if (animController != null)
                {
                    // Stop walking animation during attack
                    animController.SetWalking(false);
                }
            }
        }
        
        // After attack completion, check if we should return to chase or stay in attack range
        if (!livingEntity.IsAttacking && targetEntity != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetEntity.transform.position);
            if (distanceToTarget > attackDistance)
            {
                currentState = AIState.Chase;
            }
        }
    }
    
    // Wandering behavior when no player is detected
    private void SetWanderTarget()
    {
        float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        Vector3 offset = new Vector3(
            Mathf.Cos(randomAngle) * randomDistance, 
            0f, 
            Mathf.Sin(randomAngle) * randomDistance
        );
        
        wanderTarget = transform.position + offset;
    }
    
    private void WanderBehavior()
    {
        if (!isWandering)
            return;
            
        float distanceToTarget = Vector3.Distance(transform.position, wanderTarget);
        
        // If we've reached the target
        if (distanceToTarget < 1.0f)
        {
            isWandering = false;
            nextWanderTime = Time.time + wanderWaitTime;
            currentState = AIState.Idle;
            
            if (animController != null)
            {
                animController.SetWalking(false);
            }
            return;
        }
        
        // Move towards wander target
        MoveTowardsPosition(wanderTarget);
        
        // Update animation
        if (animController != null)
        {
            animController.SetWalking(true);
        }
    }
    
    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            // Only wander if in idle state and no player detected
            if (currentState == AIState.Idle && !isWandering)
            {
                SetWanderTarget();
                isWandering = true;
                currentState = AIState.Wander;
            }
            
            yield return new WaitForSeconds(Random.Range(3f, 6f));
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw attack radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
} 