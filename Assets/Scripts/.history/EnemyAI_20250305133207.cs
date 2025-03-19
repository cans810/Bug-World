using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Wandering Settings")]
    [SerializeField] private bool enableWandering = true;
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float minWanderTime = 2f;
    [SerializeField] private float maxWanderTime = 5f;
    [SerializeField] private float idleTimeMin = 1f;
    [SerializeField] private float idleTimeMax = 3f;
    
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask playerLayer;
    
    // State management
    private enum AIState { Idle, Wandering, Chasing, Attacking }
    private AIState currentState = AIState.Idle;
    
    // Component references
    private Rigidbody rb;
    private Animator animator;
    private NavMeshAgent navAgent;
    private LivingEntity livingEntity; // Reference to LivingEntity for movement speeds
    
    // Wandering variables
    private Vector3 wanderTarget;
    private float stateChangeTimer;
    
    // Target reference
    private Transform playerTarget;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        livingEntity = GetComponent<LivingEntity>();
        
        if (livingEntity == null)
        {
            Debug.LogError("LivingEntity component missing from EnemyAI!");
        }
        
        // Configure NavMeshAgent if present
        UpdateNavAgentSpeeds();
    }
    
    private void Start()
    {
        // Initialize with wandering if enabled
        if (enableWandering)
        {
            SetNewWanderTime();
        }
    }
    
    private void Update()
    {
        // Update NavMeshAgent speeds if they might have changed
        UpdateNavAgentSpeeds();
        
        // State timer logic
        stateChangeTimer -= Time.deltaTime;
        
        // State machine
        switch (currentState)
        {
            case AIState.Idle:
                HandleIdleState();
                break;
                
            case AIState.Wandering:
                HandleWanderingState();
                break;
                
            case AIState.Chasing:
                HandleChasingState();
                break;
                
            case AIState.Attacking:
                HandleAttackingState();
                break;
        }
        
        // Always check for player detection
        DetectPlayer();
    }
    
    private void UpdateNavAgentSpeeds()
    {
        if (navAgent != null && livingEntity != null)
        {
            navAgent.speed = livingEntity.GetModifiedMoveSpeed();
            navAgent.angularSpeed = livingEntity.GetModifiedRotationSpeed();
        }
    }
    
    private void HandleIdleState()
    {
        // If using animator
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
        }
        
        // When timer expires, start wandering
        if (stateChangeTimer <= 0 && enableWandering)
        {
            SetNewWanderTarget();
            currentState = AIState.Wandering;
        }
    }
    
    private void HandleWanderingState()
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", true);
        }
        
        // Use NavMesh if available, otherwise direct movement
        if (navAgent != null && navAgent.enabled)
        {
            if (!navAgent.hasPath)
            {
                navAgent.SetDestination(wanderTarget);
            }
            
            // If we've reached the target or timer expired
            if (Vector3.Distance(transform.position, wanderTarget) < 1f || stateChangeTimer <= 0)
            {
                navAgent.ResetPath();
                EnterIdleState();
            }
        }
        else if (livingEntity != null)
        {
            // Manual movement logic (from AIWandering)
            Vector3 direction = (wanderTarget - transform.position).normalized;
            
            // Rotate towards target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    livingEntity.GetModifiedRotationSpeed() * Time.deltaTime
                );
            }
            
            // Move forward
            transform.position += transform.forward * livingEntity.GetModifiedMoveSpeed() * Time.deltaTime;
            
            // If we've reached the target or timer expired
            if (Vector3.Distance(transform.position, wanderTarget) < 1f || stateChangeTimer <= 0)
            {
                EnterIdleState();
            }
        }
    }
    
    private void HandleChasingState()
    {
        if (playerTarget == null)
        {
            EnterIdleState();
            return;
        }
        
        // Set animator
        if (animator != null)
        {
            animator.SetBool("IsMoving", true);
        }
        
        // Get distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        
        // If close enough to attack
        if (distanceToPlayer <= attackRange)
        {
            currentState = AIState.Attacking;
            return;
        }
        
        // If player out of detection range
        if (distanceToPlayer > detectionRadius)
        {
            EnterIdleState();
            return;
        }
        
        // Chase the player
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.SetDestination(playerTarget.position);
        }
        else if (livingEntity != null)
        {
            // Manual movement towards player
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            
            // Rotate towards player
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    livingEntity.GetModifiedRotationSpeed() * Time.deltaTime
                );
            }
            
            // Move forward
            transform.position += transform.forward * livingEntity.GetModifiedMoveSpeed() * Time.deltaTime;
        }
    }
    
    private void HandleAttackingState()
    {
        if (playerTarget == null)
        {
            EnterIdleState();
            return;
        }
        
        // Face the player
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        if (direction != Vector3.zero && livingEntity != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                livingEntity.GetModifiedRotationSpeed() * 2f * Time.deltaTime  // Faster rotation when attacking
            );
        }
        
        // If using animator, trigger attack animation
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetTrigger("Attack");
        }
        
        // Check distance - if player moved out of attack range
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > attackRange)
        {
            currentState = AIState.Chasing;
        }
    }
    
    private void DetectPlayer()
    {
        // Only detect if not already attacking or chasing
        if (currentState == AIState.Attacking || currentState == AIState.Chasing)
            return;
            
        // Look for player within detection radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        if (hitColliders.Length > 0)
        {
            // Found player
            playerTarget = hitColliders[0].transform;
            currentState = AIState.Chasing;
            
            // Stop wandering if using NavMesh
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.ResetPath();
            }
        }
    }
    
    private void SetNewWanderTarget()
    {
        // Get random position within wanderRadius
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;
        
        // Find valid position on NavMesh if using it
        if (navAgent != null && navAgent.enabled)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                wanderTarget = hit.position;
            }
            else
            {
                // If no valid position found, try again
                SetNewWanderTarget();
                return;
            }
        }
        else
        {
            // For non-NavMesh movement, keep the Y position the same
            randomDirection.y = transform.position.y;
            wanderTarget = randomDirection;
        }
        
        // Set the wander timer
        stateChangeTimer = Random.Range(minWanderTime, maxWanderTime);
    }
    
    private void EnterIdleState()
    {
        currentState = AIState.Idle;
        stateChangeTimer = Random.Range(idleTimeMin, idleTimeMax);
    }
    
    private void SetNewWanderTime()
    {
        stateChangeTimer = Random.Range(minWanderTime, maxWanderTime);
    }
    
    // Visualization helpers
    private void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw wander radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
} 