using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    public enum AIState
    {
        Idle,       // Standing still when player is not moving
        Following,  // Following the player when they're moving
        Attacking,  // Attacking an enemy
        Returning   // Returning to player after getting too far
    }
    
    [Header("AI Behavior")]
    [SerializeField] private AIState currentState = AIState.Idle;
    
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 3f;         // Target distance from player when following
    [SerializeField] private float maxDistanceFromPlayer = 10f; // Max distance before returning to player
    [SerializeField] private float formationSpread = 60f;       // Angle spread for formation
    [SerializeField] private float moveSpeed = 3f;              // Base move speed
    [SerializeField] private float rotationSpeed = 5f;          // How fast ally rotates
    
    [Header("Movement Behavior")]
    [SerializeField] private float playerStationaryThreshold = 0.1f;  // Speed below which player is considered stopped
    [SerializeField] private float randomMovementAmount = 1.0f;       // How much random movement to add
    [SerializeField] private float movementDelayMin = 0.1f;           // Min delay before following when player moves
    [SerializeField] private float movementDelayMax = 0.5f;           // Max delay before following when player moves
    [SerializeField] private float naturalMovementChance = 0.2f;      // Chance per second to make small movements when idle
    [SerializeField] private float idleMovementDistance = 0.5f;       // How far to move during idle movements
    
    [Header("Combat Settings")]
    [SerializeField] private bool enableCombat = true;
    [SerializeField] private float attackRange = 2f;            // Attack range
    [SerializeField] private float detectionRange = 5f;         // Range to detect enemies
    [SerializeField] private LayerMask enemyLayer;              // Layers that contain enemies
    [SerializeField] private float attackCooldown = 1.5f;       // Time between attacks
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal states
    private Transform playerTransform;
    private LivingEntity playerEntity;
    private Rigidbody playerRigidbody;
    private CharacterController playerCharController;
    private int allyIndex = -1;                                 // Used for formation position
    private bool isMoving = false;
    private Vector3 targetPosition;
    private LivingEntity currentTarget;
    private float lastAttackTime;
    private Vector3 lastPlayerPosition;
    private bool wasPlayerMoving = false;
    private float movementStartDelay = 0f;
    private float currentMovementDelay = 0f;
    private Vector3 randomOffset;
    private float nextRandomMoveTime = 0f;
    
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

    // Track movement for compatibility with other scripts
    private bool hasAppliedMovementThisFrame = false;
    public bool HasAppliedMovementThisFrame => hasAppliedMovementThisFrame;

    private void Start()
    {
        // Find player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform != null)
        {
            playerEntity = playerTransform.GetComponent<LivingEntity>();
            playerRigidbody = playerTransform.GetComponent<Rigidbody>();
            playerCharController = playerTransform.GetComponent<CharacterController>();
            lastPlayerPosition = playerTransform.position;
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
        
        // Set random delay for this ally
        movementStartDelay = Random.Range(movementDelayMin, movementDelayMax);
        
        // Generate random movement offset
        RegenerateRandomOffset();
        
        // Start idle animation
        UpdateAnimation(false);
        currentState = AIState.Idle;
        
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
        // Reset the movement flag at the beginning of each frame
        hasAppliedMovementThisFrame = false;
        
        // Skip if dead
        if (livingEntity == null || livingEntity.IsDead || playerTransform == null)
            return;
        
        // Check if player is moving
        bool isPlayerMoving = IsPlayerMoving();
        
        // Handle transitions between moving and stopped
        if (isPlayerMoving != wasPlayerMoving)
        {
            if (isPlayerMoving)
            {
                // Player just started moving
                currentMovementDelay = movementStartDelay; // Set delay before following
                RegenerateRandomOffset(); // New random movement pattern
                
                if (showDebugInfo)
                    Debug.Log($"Ally {gameObject.name}: Player started moving, will follow in {currentMovementDelay:F2}s");
            }
            else
            {
                // Player just stopped
                if (currentState == AIState.Following)
                {
                    currentState = AIState.Idle;
                    UpdateAnimation(false);
                    
                    if (showDebugInfo)
                        Debug.Log($"Ally {gameObject.name}: Player stopped, entering idle state");
                }
            }
            
            wasPlayerMoving = isPlayerMoving;
        }
        
        // Handle movement delay
        if (isPlayerMoving && currentState == AIState.Idle && currentMovementDelay > 0)
        {
            currentMovementDelay -= Time.deltaTime;
            if (currentMovementDelay <= 0)
            {
                currentState = AIState.Following;
                
                if (showDebugInfo)
                    Debug.Log($"Ally {gameObject.name}: Delay finished, now following player");
            }
        }
        
        // Random idle movement
        if (currentState == AIState.Idle && Time.time > nextRandomMoveTime)
        {
            // Set next time to consider random movement
            nextRandomMoveTime = Time.time + 1f;
            
            // Small chance to make a small movement
            if (Random.value < naturalMovementChance)
            {
                StartCoroutine(MakeIdleMovement());
            }
        }
            
        // Update the formation position periodically
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
            case AIState.Idle:
                // Just stand still, animation already handled
                break;
                
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
                    
                    // Go back to following if player is moving, otherwise idle
                    currentState = isPlayerMoving ? AIState.Following : AIState.Idle;
                    
                    if (showDebugInfo)
                        Debug.Log($"Ally {gameObject.name}: Lost target or too far from player, returning to " + 
                                 (isPlayerMoving ? "follow" : "idle") + " mode");
                    break;
                }
                
                // Attack the target
                AttackTarget();
                break;
                
            case AIState.Returning:
                // Move directly toward player
                MoveToPosition(playerTransform.position);
                
                // Once close enough, switch back to appropriate state
                if (distanceToPlayer < followDistance * 1.5f)
                {
                    currentState = isPlayerMoving ? AIState.Following : AIState.Idle;
                    if (showDebugInfo)
                        Debug.Log($"Ally {gameObject.name}: Reached player, switching to " + 
                                 (isPlayerMoving ? "follow" : "idle") + " mode");
                }
                break;
        }
        
        // Handle ally avoidance regardless of state
        if (currentState != AIState.Idle)
        {
            ApplyAllyAvoidance();
        }
    }
    
    private IEnumerator MakeIdleMovement()
    {
        if (showDebugInfo)
            Debug.Log($"Ally {gameObject.name}: Making small idle movement");
            
        // Mark that we're in a temporary movement
        bool wasIdle = (currentState == AIState.Idle);
        AIState previousState = currentState;
        
        // Choose a random direction and small distance
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        Vector3 moveTarget = transform.position + randomDirection * Random.Range(0.3f, idleMovementDistance);
        
        // Make sure we don't go too far from player
        float distToPlayer = Vector3.Distance(moveTarget, playerTransform.position);
        if (distToPlayer > followDistance * 1.5f)
        {
            // Adjust position to stay within range
            moveTarget = transform.position + randomDirection * (followDistance * 0.5f);
        }
        
        // Enable walking animation
        UpdateAnimation(true);
        
        // Move to the position
        float duration = Random.Range(0.5f, 1.0f);
        float timer = 0;
        Vector3 startPos = transform.position;
        
        while (timer < duration)
        {
            // Stop if we're now in a non-idle state (like attacking)
            if (!wasIdle || currentState != AIState.Idle)
                break;
                
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // Apply smoothing curve
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // Move with simple lerp for this small movement
            transform.position = Vector3.Lerp(startPos, moveTarget, smoothT);
            
            // Rotate toward movement direction
            Vector3 moveDir = (moveTarget - startPos).normalized;
            if (moveDir.magnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
            
            yield return null;
        }
        
        // Return to idle animation
        UpdateAnimation(false);
        
        // Wait a while before considering another movement
        nextRandomMoveTime = Time.time + Random.Range(3f, 7f);
    }
    
    private bool IsPlayerMoving()
    {
        if (playerTransform == null)
            return false;
            
        // Check velocity based on available components
        if (playerRigidbody != null)
        {
            // Use rigidbody velocity
            Vector3 velocity = playerRigidbody.velocity;
            velocity.y = 0; // Ignore vertical movement
            return velocity.magnitude > playerStationaryThreshold;
        }
        else if (playerCharController != null)
        {
            // Use character controller velocity
            Vector3 velocity = playerCharController.velocity;
            velocity.y = 0; // Ignore vertical movement
            return velocity.magnitude > playerStationaryThreshold;
        }
        else
        {
            // Fallback to position comparison
            Vector3 movement = playerTransform.position - lastPlayerPosition;
            movement.y = 0; // Ignore vertical movement
            float speed = movement.magnitude / Time.deltaTime;
            lastPlayerPosition = playerTransform.position;
            return speed > playerStationaryThreshold;
        }
    }
    
    private void RegenerateRandomOffset()
    {
        // Create a random offset for this ally's movement
        randomOffset = new Vector3(
            Random.Range(-randomMovementAmount, randomMovementAmount),
            0,
            Random.Range(-randomMovementAmount, randomMovementAmount)
        );
    }
    
    private void FollowPlayer()
    {
        // Apply random offset to position to make movement more natural
        Vector3 targetWithOffset = targetPosition + randomOffset;
        
        // Move toward the calculated formation position
        MoveToPosition(targetWithOffset);
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
                hasAppliedMovementThisFrame = true;
            }
            else
            {
                // Fallback direct movement
                transform.position = Vector3.SmoothDamp(transform.position, position, ref moveVelocity, positionSmoothTime);
                hasAppliedMovementThisFrame = true;
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
            
        // Get number of allies for better spacing
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        int aliveAlliesCount = 0;
        
        foreach (AllyAI ally in allAllies)
        {
            if (ally != null && ally.livingEntity != null && !ally.livingEntity.IsDead)
            {
                aliveAlliesCount++;
            }
        }
        
        // Default to a single ally if something went wrong
        if (aliveAlliesCount <= 0) aliveAlliesCount = 1;
        
        // Calculate angle based on ally index and total count
        float angleStep = formationSpread / aliveAlliesCount;
        float angle = -formationSpread/2 + (allyIndex * angleStep) + (angleStep/2);
        
        // Calculate direction based on angle (negative Z is behind player)
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Vector3 direction = rotation * Vector3.back;
        
        // Return position at specified distance from player
        return playerTransform.position + (direction * followDistance);
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
