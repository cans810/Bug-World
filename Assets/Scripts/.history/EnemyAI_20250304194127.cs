using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float chaseRadius = 12f; // How far they'll chase before giving up
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack
    [SerializeField] private LayerMask playerLayer; // Set to layer that contains the player
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 8f;
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
    private enum AIState { Idle, Wander, Chase, Attack, Retreat }
    private AIState currentState = AIState.Idle;
    
    // Target tracking
    private Transform playerTransform;
    private LivingEntity playerEntity;
    private Vector3 lastKnownPlayerPosition;
    private bool playerInAttackRange = false;
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
            
        // Check for player detection
        DetectPlayer();
        
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
                // Wander around until we detect the player
                if (isWandering)
                {
                    WanderBehavior();
                }
                break;
                
            case AIState.Chase:
                // Chase the player
                ChasePlayer();
                break;
                
            case AIState.Attack:
                // Face the player and attack
                if (playerTransform != null)
                {
                    FaceTarget(playerTransform.position);
                    AttemptAttack();
                }
                else
                {
                    // Lost track of player during attack
                    currentState = AIState.Idle;
                }
                break;
                
            case AIState.Retreat:
                // Run away logic (not implemented in this version)
                // Could be used for fleeing when low health
                break;
        }
    }
    
    private void DetectPlayer()
    {
        // First check if player is already tracked by our hitbox
        if (livingEntity.CurrentTarget != null && !livingEntity.CurrentTarget.IsDead)
        {
            playerEntity = livingEntity.CurrentTarget;
            playerTransform = playerEntity.transform;
            lastKnownPlayerPosition = playerTransform.position;
            
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // Check if we're close enough to attack
            if (distanceToPlayer <= attackDistance)
            {
                playerInAttackRange = true;
                currentState = AIState.Attack;
            }
            else
            {
                playerInAttackRange = false;
                currentState = AIState.Chase;
            }
            
            return;
        }
        
        // If not tracked by hitbox, use spherecast for wider detection
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        if (hitColliders.Length > 0)
        {
            // Get the first player found
            Transform potentialTarget = hitColliders[0].transform;
            LivingEntity targetEntity = potentialTarget.GetComponent<LivingEntity>();
            if (targetEntity == null)
                targetEntity = potentialTarget.GetComponentInParent<LivingEntity>();
                
            if (targetEntity != null && !targetEntity.IsDead)
            {
                playerEntity = targetEntity;
                playerTransform = targetEntity.transform;
                lastKnownPlayerPosition = playerTransform.position;
                
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                
                // Transition to chase state
                currentState = AIState.Chase;
                
                // Check if we're close enough to attack
                if (distanceToPlayer <= attackDistance)
                {
                    playerInAttackRange = true;
                    currentState = AIState.Attack;
                }
            }
        }
        else if (currentState == AIState.Chase)
        {
            // If we were chasing but lost the player, go back to wandering
            float distanceToLastKnown = Vector3.Distance(transform.position, lastKnownPlayerPosition);
            if (distanceToLastKnown < 1.0f)
            {
                // We've reached the last known position and lost the player
                currentState = AIState.Idle;
                nextWanderTime = Time.time + 1.0f;
            }
        }
    }
    
    private void ChasePlayer()
    {
        if (playerTransform == null || playerEntity.IsDead)
        {
            // Target is lost or dead
            currentState = AIState.Idle;
            return;
        }
        
        Vector3 targetPosition = playerTransform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, targetPosition);
        
        // If too far, stop chasing
        if (distanceToPlayer > chaseRadius)
        {
            currentState = AIState.Idle;
            return;
        }
        
        // If close enough to attack
        if (distanceToPlayer <= attackDistance)
        {
            playerInAttackRange = true;
            currentState = AIState.Attack;
            return;
        }
        
        // Move towards player
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
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // Keep on same Y level
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    private Vector3 AvoidObstacles(Vector3 currentDirection)
    {
        RaycastHit hit;
        float rayDistance = obstacleDetectionRange;
        Vector3 modifiedDirection = currentDirection;
        
        // Cast rays in 3 directions: forward, left forward, right forward
        for (int i = -1; i <= 1; i++)
        {
            Vector3 rayDirection = Quaternion.Euler(0, i * 30, 0) * currentDirection;
            Debug.DrawRay(transform.position + Vector3.up * 0.5f, rayDirection * rayDistance, Color.yellow, 0.1f);
            
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, rayDirection, out hit, rayDistance, obstacleLayer))
            {
                // Found an obstacle - adjust direction
                Vector3 avoidDirection = Vector3.Cross(Vector3.up, rayDirection).normalized;
                
                // If obstacle on left, steer right and vice versa
                if (i < 0) avoidDirection = -avoidDirection;
                
                modifiedDirection += avoidDirection * 0.75f;
                modifiedDirection.Normalize();
                break;
            }
        }
        
        return modifiedDirection;
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
        if (!livingEntity.IsAttacking && playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > attackDistance)
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
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw chase radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
        
        // Draw attack radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
} 