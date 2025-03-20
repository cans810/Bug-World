using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    public enum AIState
    {
        Following,  // Following the player
        Attacking,  // Attacking an enemy
        Returning   // Returning to player after getting too far
    }
    
    [Header("AI Behavior")]
    [SerializeField] private AIState currentState = AIState.Following;
    
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 3f;      // Target distance from player when following
    [SerializeField] private float maxDistanceFromPlayer = 10f; // Max distance before returning to player
    [SerializeField] private float formationSpread = 60f;    // Angle spread for formation
    [SerializeField] private float moveSpeed = 3f;           // Base move speed
    [SerializeField] private float rotationSpeed = 5f;       // How fast ally rotates
    
    [Header("Combat Settings")]
    [SerializeField] private bool enableCombat = true;
    [SerializeField] private float attackRange = 2f;         // Attack range
    [SerializeField] private float detectionRange = 5f;      // Range to detect enemies
    [SerializeField] private LayerMask enemyLayer;           // Layers that contain enemies
    [SerializeField] private float attackCooldown = 1.5f;    // Time between attacks
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal states
    private Transform playerTransform;
    private LivingEntity playerEntity;
    private int allyIndex = -1;                              // Used for formation position
    private bool isMoving = false;
    private Vector3 targetPosition;
    private LivingEntity currentTarget;
    private float lastAttackTime;
    
    [Header("Movement Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.2f;
    [SerializeField] private float formationUpdateInterval = 0.5f;
    private Vector3 moveVelocity;
    private float formationUpdateTimer;
    
    [Header("Ally Avoidance")]
    [SerializeField] private bool avoidOtherAllies = true;
    [SerializeField] private float avoidanceRadius = 1.2f;
    [SerializeField] private LayerMask allyLayer;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo = false;

    private void Start()
    {
        // Find player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform != null)
        {
            playerEntity = playerTransform.GetComponent<LivingEntity>();
        }
        else
        {
            Debug.LogWarning("AllyAI could not find player!");
        }
        
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
        
        // Subscribe to death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(HandleDeath);
        }

        // Set up move speed based on player's speed if available
        if (playerEntity != null && playerEntity.moveSpeed > 0.1f)
        {
            moveSpeed = playerEntity.moveSpeed * 1.1f; // Slightly faster to catch up
            livingEntity.moveSpeed = moveSpeed;
        }
        
        // Find ally index for formation
        AssignAllyIndex();
        
        // Start idle animation
        UpdateAnimation(false);
        
        // Synchronize attack range and cooldown with LivingEntity if present
        if (livingEntity != null)
        {
            attackRange = livingEntity.AttackRange;
            attackCooldown = livingEntity.AttackCooldown;
        }
        
        // Initialize target position
        if (playerTransform != null)
        {
            targetPosition = CalculateFormationPosition();
        }
    }
    
    private void Update()
    {
        // Skip if dead
        if (livingEntity == null || livingEntity.IsDead || playerTransform == null)
            return;
            
        // Update the formation position periodically instead of every frame
        formationUpdateTimer += Time.deltaTime;
        if (formationUpdateTimer >= formationUpdateInterval)
        {
            formationUpdateTimer = 0f;
            targetPosition = CalculateFormationPosition();
        }
        
        // Check distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Handle state transitions
        switch (currentState)
        {
            case AIState.Following:
                if (enableCombat)
                {
                    // Look for enemies to attack
                    LivingEntity enemy = FindNearestEnemy();
                    if (enemy != null && distanceToPlayer < maxDistanceFromPlayer)
                    {
                        currentTarget = enemy;
                        currentState = AIState.Attacking;
                        if (showDebugInfo)
                            Debug.Log($"Ally {gameObject.name}: Found target, switching to attack mode");
                    }
                }
                
                // Follow the player
                FollowPlayer();
                break;
                
            case AIState.Attacking:
                // Check if target is still valid
                if (currentTarget == null || currentTarget.IsDead || distanceToPlayer > maxDistanceFromPlayer)
                {
                    currentTarget = null;
                    currentState = AIState.Following;
                    if (showDebugInfo)
                        Debug.Log($"Ally {gameObject.name}: Lost target or too far from player, returning to follow mode");
                    break;
                }
                
                // Attack the target
                AttackTarget();
                break;
                
            case AIState.Returning:
                // Move directly toward player
                MoveToPosition(playerTransform.position);
                
                // Once close enough, switch back to following
                if (distanceToPlayer < followDistance * 1.5f)
                {
                    currentState = AIState.Following;
                    if (showDebugInfo)
                        Debug.Log($"Ally {gameObject.name}: Reached player, switching to follow mode");
                }
                break;
        }
        
        // Handle ally avoidance regardless of state
        ApplyAllyAvoidance();
    }
    
    private void FollowPlayer()
    {
        // Move toward the calculated formation position
        MoveToPosition(targetPosition);
    }
    
    private void MoveToPosition(Vector3 position)
    {
        // Calculate distance to target
        Vector3 directionToTarget = position - transform.position;
        directionToTarget.y = 0; // Keep movement on horizontal plane
        float distance = directionToTarget.magnitude;
        
        // Only move if we're far enough away
        if (distance > 0.3f)
        {
            // Normalize direction
            Vector3 moveDirection = directionToTarget.normalized;
            
            // Smooth the movement
            Vector3 targetVelocity = moveDirection * moveSpeed;
            
            // Rotate towards movement direction
            if (rotationSpeed > 0 && moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // Move using LivingEntity's movement method if available
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(moveDirection, 1.0f);
            }
            else
            {
                // Fallback direct movement
                transform.position = Vector3.SmoothDamp(transform.position, position, ref moveVelocity, positionSmoothTime);
            }
            
            // Set animation
            isMoving = true;
            UpdateAnimation(true);
        }
        else
        {
            // We've reached the target, stop moving
            isMoving = false;
            UpdateAnimation(false);
        }
    }
    
    private Vector3 CalculateFormationPosition()
    {
        if (playerTransform == null) 
            return transform.position;
            
        // Get total number of allies for formation calculation
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        int aliveAllies = CountAliveAllies(allAllies);
        
        // Calculate angle based on index
        float angleStep = formationSpread / Mathf.Max(1, aliveAllies - 1);
        float angle = -formationSpread / 2 + (allyIndex * angleStep);
        
        // Calculate offset based on player's forward direction
        Quaternion offsetRotation = Quaternion.Euler(0, angle, 0);
        Vector3 offset = offsetRotation * (-playerTransform.forward) * followDistance;
        
        // Return final position
        return playerTransform.position + offset;
    }
    
    private int CountAliveAllies(AllyAI[] allies)
    {
        int count = 0;
        foreach (AllyAI ally in allies)
        {
            if (ally != null && ally.livingEntity != null && !ally.livingEntity.IsDead)
                count++;
        }
        return count;
    }
    
    private void AttackTarget()
    {
        if (currentTarget == null || livingEntity == null)
            return;
            
        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        
        if (distanceToTarget <= attackRange)
        {
            // Within attack range, try to attack
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                // Face the target
                Vector3 directionToTarget = currentTarget.transform.position - transform.position;
                directionToTarget.y = 0;
                if (directionToTarget.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2 * Time.deltaTime);
                }
                
                // Try to attack
                bool attacked = livingEntity.TryAttack();
                if (attacked)
                {
                    lastAttackTime = Time.time;
                    if (showDebugInfo)
                        Debug.Log($"Ally {gameObject.name}: Attacking {currentTarget.name}");
                }
                
                // Don't move while attacking
                isMoving = false;
                UpdateAnimation(false);
            }
        }
        else
        {
            // Move closer to the target
            isMoving = true;
            UpdateAnimation(true);
            
            // Use LivingEntity's movement
            Vector3 directionToTarget = currentTarget.transform.position - transform.position;
            directionToTarget.y = 0;
            livingEntity.MoveInDirection(directionToTarget.normalized, 1.0f);
            
            // Also rotate towards target
            if (directionToTarget.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    private LivingEntity FindNearestEnemy()
    {
        // Find all colliders in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange, enemyLayer);
        LivingEntity nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider col in colliders)
        {
            // Skip self and player
            if (col.gameObject == gameObject || col.CompareTag("Player"))
                continue;
                
            // Get LivingEntity component
            LivingEntity entity = col.GetComponent<LivingEntity>();
            if (entity != null && !entity.IsDead)
            {
                // Check if it's an enemy (not on the same team)
                if (!entity.CompareTag(tag) && !entity.CompareTag("Player"))
                {
                    float distance = Vector3.Distance(transform.position, entity.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = entity;
                    }
                }
            }
        }
        
        return nearestEnemy;
    }
    
    private void ApplyAllyAvoidance()
    {
        if (!avoidOtherAllies)
            return;
            
        Collider[] nearbyAllies = Physics.OverlapSphere(transform.position, avoidanceRadius, allyLayer);
        Vector3 avoidanceDirection = Vector3.zero;
        int avoidanceCount = 0;
        
        foreach (Collider allyCollider in nearbyAllies)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
                
            // Get direction away from ally
            Vector3 dirAway = transform.position - allyCollider.transform.position;
            if (dirAway.magnitude < 0.01f)
            {
                // Avoid exact same position
                dirAway = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            }
            else
            {
                dirAway.y = 0; // Keep on horizontal plane
                dirAway.Normalize();
                
                // Scale by how close we are
                float distance = Vector3.Distance(transform.position, allyCollider.transform.position);
                dirAway *= (avoidanceRadius - distance) / avoidanceRadius;
            }
            
            avoidanceDirection += dirAway;
            avoidanceCount++;
        }
        
        // Apply avoidance if needed
        if (avoidanceCount > 0 && avoidanceDirection.magnitude > 0.01f)
        {
            avoidanceDirection /= avoidanceCount;
            avoidanceDirection.Normalize();
            
            // Apply subtle avoidance - just enough to prevent overlap
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(avoidanceDirection, 0.3f);
            }
            else
            {
                // Fallback
                transform.position += avoidanceDirection * 0.3f * moveSpeed * Time.deltaTime;
            }
        }
    }
    
    private void AssignAllyIndex()
    {
        // Find all ally AIs and determine this one's index
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        int aliveIndex = 0;
        
        for (int i = 0; i < allAllies.Length; i++)
        {
            if (allAllies[i] == this)
            {
                allyIndex = aliveIndex;
                break;
            }
            
            // Only count living allies for the index
            if (!allAllies[i].livingEntity.IsDead)
            {
                aliveIndex++;
            }
        }
        
        if (allyIndex == -1) allyIndex = 0; // Fallback
        
        if (showDebugInfo)
            Debug.Log($"Ally {gameObject.name} assigned index: {allyIndex}");
    }
    
    private void UpdateAnimation(bool isWalking)
    {
        if (animController == null)
            return;
            
        if (isWalking)
        {
            animController.SetWalking(true);
        }
        else
        {
            animController.SetWalking(false);
        }
    }
    
    private void HandleDeath()
    {
        // Stop all behavior when dead
        isMoving = false;
        enabled = false;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandleDeath);
        }
    }
    
    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // Draw follow distance
        Gizmos.color = Color.green;
        if (playerTransform != null)
            Gizmos.DrawWireSphere(playerTransform.position, followDistance);
            
        // Draw max distance
        Gizmos.color = Color.red;
        if (playerTransform != null)
            Gizmos.DrawWireSphere(playerTransform.position, maxDistanceFromPlayer);
            
        // Draw attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw detection range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw target position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(targetPosition, 0.2f);
        }
    }
}
