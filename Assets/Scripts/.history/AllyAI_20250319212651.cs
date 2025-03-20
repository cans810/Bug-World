using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    public enum AIMode
    {
        Follow,             // Follow player
        ReturnToPlayer      // Return to player when too far away
    }
    
    [Header("AI Behavior")]
    [SerializeField] private AIMode currentMode = AIMode.Follow;
    
    [Header("Movement Settings")]
    [SerializeField] private float followDistance = 3f; // Distance to maintain from player
    [SerializeField] private float offsetAngle = 45f; // Angle offset from player's movement direction
    [SerializeField] private float speedMultiplier = 1.2f; // Move slightly faster to keep up with player
    [SerializeField] private float maxAllowedDistance = 12f; // Distance at which ally will teleport back
    [SerializeField] private float returnThreshold = 8f; // Distance at which ally will prioritize returning
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    [SerializeField] private OnScreenJoystick joystick; // Reference to the same joystick player uses
    
    // Internal states
    private bool isMoving = false;
    private Transform playerTransform;
    private PlayerController playerController;
    private int allyIndex = -1; // Used to distribute allies around the player
    
    [Header("Ally Avoidance")]
    [SerializeField] private bool avoidOtherAllies = true;
    [SerializeField] private float allyAvoidanceDistance = 1.0f;
    [SerializeField] private float allyDetectionRadius = 1.2f;
    [SerializeField] private LayerMask allyLayer;
    
    // Track if we've applied movement this frame
    private bool hasAppliedMovementThisFrame = false;
    
    // Public property to check if movement has been applied this frame
    public bool HasAppliedMovementThisFrame => hasAppliedMovementThisFrame;
    
    [Header("Movement Smoothing")]
    [SerializeField] private bool useMovementSmoothing = true;
    [SerializeField] private float movementSmoothTime = 0.2f;
    
    // For smoothing
    private Vector3 smoothedMoveDirection;
    private Vector3 directionVelocity;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo = false;
    
    [Header("Combat Settings")]
    [SerializeField] private bool enableAutoAttack = true;
    [SerializeField] private float attackRangeBuffer = 1.0f; // Increased from 0.5f
    [SerializeField] private float attackCheckInterval = 0.2f; // Add this for more frequent checks
    private List<LivingEntity> enemiesInRange = new List<LivingEntity>();
    private Coroutine autoAttackCoroutine;
    private bool isAttacking = false;
    private EntityHitbox allyHitbox;
    private float lastAttackAttemptTime = 0f;
    
    private void Start()
    {
        // Find player transform and controller
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform != null)
        {
            playerController = playerTransform.GetComponent<PlayerController>();
        }
        
        // Find joystick if not assigned
        if (joystick == null)
        {
            joystick = FindObjectOfType<OnScreenJoystick>();
            if (joystick == null)
            {
                Debug.LogWarning($"AllyAI on {gameObject.name} cannot find OnScreenJoystick");
            }
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

        // Set move speed based on player's speed
        if (playerTransform != null)
        {
            LivingEntity playerEntity = playerTransform.GetComponent<LivingEntity>();
            if (playerEntity != null && playerEntity.moveSpeed > 0.1f)
            {
                livingEntity.moveSpeed = playerEntity.moveSpeed * speedMultiplier;
                Debug.Log($"Set {gameObject.name} speed to {livingEntity.moveSpeed}");
            }
            else
            {
                livingEntity.moveSpeed = 2.0f * speedMultiplier;
            }
        }
        
        // Start idle
        ForceUpdateAnimation(false);
        
        // Find ally index to determine position around player
        AssignAllyIndex();
        
        // Add this to ensure animations are working properly
        StartCoroutine(EnsureAnimationsWork());
        
        // Find and set up the hitbox for combat detection
        allyHitbox = GetComponentInChildren<EntityHitbox>();
        if (allyHitbox != null)
        {
            allyHitbox.OnPlayerEnterHitbox += HandleEnemyDetected;
            allyHitbox.OnPlayerExitHitbox += HandleEnemyLost;
            
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name}: Subscribed to hitbox events");
        }
        else
        {
            Debug.LogWarning($"Ally {gameObject.name}: No EntityHitbox found in children, auto-attack won't work");
            
            // Try to find hitbox in a different way
            EntityHitbox[] hitboxes = GetComponentsInChildren<EntityHitbox>(true);
            if (hitboxes.Length > 0)
            {
                allyHitbox = hitboxes[0];
                allyHitbox.OnPlayerEnterHitbox += HandleEnemyDetected;
                allyHitbox.OnPlayerExitHitbox += HandleEnemyLost;
                Debug.Log($"Ally {gameObject.name}: Found hitbox through alternative method");
            }
        }
    }
    
    private void AssignAllyIndex()
    {
        // Find all ally ants and determine this ant's index
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        for (int i = 0; i < allAllies.Length; i++)
        {
            if (allAllies[i] == this)
            {
                allyIndex = i;
                break;
            }
        }
        
        if (allyIndex == -1) allyIndex = 0; // Fallback
        
        if (showDebugInfo)
        {
            Debug.Log($"Ally {gameObject.name} assigned index: {allyIndex}");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandleDeath);
        }
        
        // Unsubscribe from hitbox events to prevent memory leaks
        if (allyHitbox != null)
        {
            allyHitbox.OnPlayerEnterHitbox -= HandleEnemyDetected;
            allyHitbox.OnPlayerExitHitbox -= HandleEnemyLost;
        }
        
        // Stop any running coroutines
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }
    }
    
    private void HandleDeath()
    {
        isMoving = false;
        enabled = false;
    }
    
    private void Update()
    {
        // Reset movement flag
        hasAppliedMovementThisFrame = false;
        
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
            
        // Apply separation from overlapping allies
        SeparateFromOverlappingAllies();
        
        // Don't move if currently attacking (unless in ReturnToPlayer mode)
        if (isAttacking && currentMode != AIMode.ReturnToPlayer)
            return;
            
        // Ensure player reference is valid
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null) 
            {
                return;
            }
            
            playerController = playerTransform.GetComponent<PlayerController>();
        }
        
        // Check distance to player and update mode if necessary
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // If too far from player, teleport immediately back to player
        if (distanceToPlayer > maxAllowedDistance)
        {
            TeleportToFormationPosition();
            return;
        }
        
        // Update AI mode based on distance to player
        if (distanceToPlayer > returnThreshold)
        {
            // Too far, prioritize returning to player
            if (currentMode != AIMode.ReturnToPlayer)
            {
                currentMode = AIMode.ReturnToPlayer;
                if (showDebugInfo)
                    Debug.Log($"Ally {gameObject.name} is too far ({distanceToPlayer:F1}m), returning to player");
            }
        }
        else if (currentMode == AIMode.ReturnToPlayer && distanceToPlayer < (returnThreshold * 0.7f))
        {
            // Close enough, resume normal following
            currentMode = AIMode.Follow;
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name} is close enough ({distanceToPlayer:F1}m), resuming normal follow");
        }
        
        // Apply different movement based on current mode
        if (currentMode == AIMode.ReturnToPlayer)
        {
            MoveTowardPlayer(distanceToPlayer);
        }
        else
        {
            // Normal follow mode - get joystick input from either the joystick component or player controller
            Vector3 joystickDirection = Vector3.zero;
            
            if (joystick != null && joystick.HasInput())
            {
                joystickDirection = new Vector3(joystick.Horizontal, 0, joystick.Vertical).normalized;
            }
            else if (playerController != null)
            {
                joystickDirection = playerController.GetMovementDirection();
            }
            
            FollowPlayerWithFormation(joystickDirection);
        }
    }
    
    private void MoveTowardPlayer(float distanceToPlayer)
    {
        // Clear any attacking state - prioritize returning to player
        isAttacking = false;
        
        // Direct the ally to move straight toward the player's position
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0; // Keep movement on the ground plane
        
        // Normalize and apply a faster speed when further away
        Vector3 moveDirection = toPlayer.normalized;
        
        // Apply smoothing to movement direction
        if (useMovementSmoothing)
        {
            smoothedMoveDirection = Vector3.SmoothDamp(
                smoothedMoveDirection, 
                moveDirection, 
                ref directionVelocity, 
                movementSmoothTime);
                
            moveDirection = smoothedMoveDirection;
        }
        
        // Calculate speed multiplier based on distance - move faster when further away
        float speedFactor = Mathf.Lerp(1.2f, 1.8f, Mathf.InverseLerp(returnThreshold * 0.7f, returnThreshold, distanceToPlayer));
        
        // Apply ally avoidance
        if (avoidOtherAllies)
        {
            moveDirection = ApplyAllyAvoidance(moveDirection, allyDetectionRadius);
        }
        
        // Make ally face movement direction
        livingEntity.RotateTowards(moveDirection, 5.0f);
        
        // Apply movement with increased speed to catch up quickly
        livingEntity.MoveInDirection(moveDirection, speedFactor);
        
        // Update animation to walk
        ForceUpdateAnimation(true);
        hasAppliedMovementThisFrame = true;
        
        // Debug visualization
        if (showDebugInfo)
        {
            Debug.DrawLine(transform.position, transform.position + moveDirection * 2f, Color.red);
            Debug.DrawLine(transform.position, playerTransform.position, Color.yellow);
        }
    }
    
    private void FollowPlayerWithFormation(Vector3 inputDirection)
    {
        // Calculate target position based on formation
        Vector3 targetPosition = GetFormationPosition();
        
        // Determine if we should move
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0; // Keep on the same Y level
        
        // Combine player's input direction with the need to stay in formation
        Vector3 moveDirection = inputDirection;
        
        // Adjust the distance we want to maintain based on player input
        float actualFollowDistance = followDistance;
        if (inputDirection.magnitude > 0.1f)
        {
            // If player is moving, try to follow input direction but adjust for being in formation
            actualFollowDistance = followDistance * 0.8f; // Stay a bit closer when moving
        }
        else
        {
            // If player isn't moving, focus more on getting into formation
            moveDirection = toTarget.normalized;
        }
        
        float distanceToTarget = toTarget.magnitude;
        
        // Variable speed based on distance to ideal position
        float speedFactor = 1.0f;
        
        // If too far from target position, move directly toward it and speed up
        if (distanceToTarget > actualFollowDistance * 1.5f)
        {
            moveDirection = toTarget.normalized;
            speedFactor = 1.5f; // Speed up to catch up
        }
        // If at moderate distance, blend between player direction and catch-up direction
        else if (distanceToTarget > actualFollowDistance * 0.8f)
        {
            float blendFactor = Mathf.InverseLerp(actualFollowDistance * 0.8f, actualFollowDistance * 1.5f, distanceToTarget);
            moveDirection = Vector3.Lerp(moveDirection, toTarget.normalized, blendFactor);
            speedFactor = Mathf.Lerp(1.0f, 1.3f, blendFactor); // Smoothly increase speed
        }
        
        // Apply smoothing to movement direction
        if (useMovementSmoothing)
        {
            smoothedMoveDirection = Vector3.SmoothDamp(
                smoothedMoveDirection, 
                moveDirection, 
                ref directionVelocity, 
                movementSmoothTime);
                
            moveDirection = smoothedMoveDirection;
        }
        
        // Apply ally avoidance
        if (avoidOtherAllies)
        {
            moveDirection = ApplyAllyAvoidance(moveDirection, allyDetectionRadius);
        }
        
        // Make ally face movement direction
        livingEntity.RotateTowards(moveDirection, 5.0f);
        
        // Apply movement with the calculated speed factor for more dynamic following
        livingEntity.MoveInDirection(moveDirection, speedFactor);
        
        // Always update animation to walk when there's joystick input
        ForceUpdateAnimation(true);
        hasAppliedMovementThisFrame = true;
        
        // Debug visualization
        if (showDebugInfo)
        {
            Debug.DrawLine(transform.position, transform.position + moveDirection * 2f, Color.red);
            Debug.DrawLine(transform.position, targetPosition, Color.yellow);
        }
    }
    
    private void TeleportToFormationPosition()
    {
        // Calculate the ideal formation position
        Vector3 targetPosition = GetFormationPosition();
        
        // Teleport to that position
        transform.position = targetPosition;
        
        // Switch to follow mode
        currentMode = AIMode.Follow;
        
        if (showDebugInfo)
            Debug.Log($"Ally {gameObject.name} teleported back to formation position");
    }
    
    // Auto-attack coroutine
    private IEnumerator AutoAttackCoroutine()
    {
        while (true)
        {
            // Wait for the attack check interval
            yield return new WaitForSeconds(attackCheckInterval);
            
            // Don't attack if in ReturnToPlayer mode
            if (currentMode == AIMode.ReturnToPlayer)
            {
                continue;
            }
            
            // If we have enemies in range and not already attacking
            if (enemiesInRange.Count > 0 && !isAttacking)
            {
                // Find the closest alive enemy
                LivingEntity closestEnemy = GetClosestAliveEnemy();
                
                if (closestEnemy != null)
                {
                    // Calculate distance to enemy
                    float distanceToEnemy = Vector3.Distance(transform.position, closestEnemy.transform.position);
                    
                    // Check if within attack range (using enemy's attack range + buffer)
                    if (distanceToEnemy <= livingEntity.AttackRange + attackRangeBuffer)
                    {
                        // First turn toward the enemy
                        Vector3 directionToEnemy = (closestEnemy.transform.position - transform.position).normalized;
                        directionToEnemy.y = 0;
                        
                        // Rotate to face enemy
                        livingEntity.RotateTowards(directionToEnemy, 5.0f);
                        
                        // Attempt attack with a slight delay to allow rotation
                        yield return new WaitForSeconds(0.1f);
                        
                        // Now try to attack
                        isAttacking = AttemptAttack(closestEnemy);
                        
                        if (isAttacking)
                        {
                            // Wait for attack animation to complete (use attack cooldown as a proxy)
                            yield return new WaitForSeconds(livingEntity.AttackCooldown * 0.8f);
                            isAttacking = false;
                        }
                    }
                }
            }
        }
    }
    
    // Helper to get closest alive enemy
    private LivingEntity GetClosestAliveEnemy()
    {
        LivingEntity closestEnemy = null;
        float closestDistance = float.MaxValue;
        
        // Filter out dead enemies and find the closest
        for (int i = enemiesInRange.Count - 1; i >= 0; i--)
        {
            LivingEntity enemy = enemiesInRange[i];
            
            // Remove dead or null enemies
            if (enemy == null || enemy.IsDead)
            {
                enemiesInRange.RemoveAt(i);
                continue;
            }
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }
        
        return closestEnemy;
    }
    
    // Helper to check if an enemy is in attack range
    private bool IsInAttackRange(LivingEntity entity)
    {
        if (entity == null) return false;
        
        float distance = Vector3.Distance(transform.position, entity.transform.position);
        return distance <= livingEntity.AttackRange + attackRangeBuffer;
    }
    
    // Implement this method to handle avoiding other allies
    private Vector3 ApplyAllyAvoidance(Vector3 moveDirection, float detectionRadius)
    {
        // Find all allies within the detection radius
        Collider[] allyColliders = Physics.OverlapSphere(transform.position, detectionRadius, allyLayer);
        
        if (allyColliders.Length <= 1) // Just ourselves
        {
            return moveDirection;
        }
        
        Vector3 avoidanceDirection = Vector3.zero;
        int otherAllyCount = 0;
        
        foreach (Collider allyCollider in allyColliders)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
            
            // Get direction and distance from this ally
            Vector3 directionToAlly = transform.position - allyCollider.transform.position;
            float distance = directionToAlly.magnitude;
            
            // Only avoid if close enough
            if (distance < detectionRadius)
            {
                // Add weighted avoidance force (inversely proportional to distance)
                avoidanceDirection += directionToAlly.normalized * (1.0f - distance / detectionRadius);
                otherAllyCount++;
            }
        }
        
        // Apply avoidance only if other allies found
        if (otherAllyCount > 0)
        {
            // Average the avoidance direction and normalize
            avoidanceDirection /= otherAllyCount;
            
            // Blend with original movement direction - 70% original, 30% avoidance
            Vector3 blendedDirection = (moveDirection * 0.7f + avoidanceDirection * 0.3f).normalized;
            return blendedDirection;
        }
        
        return moveDirection;
    }
    
    // This method ensures allies maintain minimum separation when overlapping
    private void SeparateFromOverlappingAllies()
    {
        if (!avoidOtherAllies)
            return;
            
        // Find all allies that are too close (about half the avoidance distance)
        Collider[] allyColliders = Physics.OverlapSphere(transform.position, allyAvoidanceDistance * 0.5f, allyLayer);
        
        if (allyColliders.Length <= 1) // Just ourselves
            return;
            
        Vector3 separationMove = Vector3.zero;
        int separationCount = 0;
        
        foreach (Collider allyCollider in allyColliders)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
                
            // Get direction and distance from this ally
            Vector3 awayDirection = transform.position - allyCollider.transform.position;
            float distance = awayDirection.magnitude;
            
            // Apply stronger separation force when very close
            if (distance < allyAvoidanceDistance * 0.5f)
            {
                // Normalize and scale by how close we are
                awayDirection = awayDirection.normalized * Mathf.Pow((1.0f - distance/(allyAvoidanceDistance * 0.5f)), 0.7f);
            }
            else
            {
                continue; // Not close enough to need separation
            }
            
            separationMove += awayDirection;
            separationCount++;
        }
        
        // Apply separation movement if needed
        if (separationMove.magnitude > 0.01f && separationCount > 0)
        {
            // Normalize and apply using LivingEntity's movement
            separationMove = separationMove.normalized;
            
            // Use a smaller movement to avoid jitter
            livingEntity.MoveInDirection(separationMove, 0.3f);
            
            hasAppliedMovementThisFrame = true;
        }
    }
    
    // Add this method to AllyAI class for more direct animation control
    private void ForceUpdateAnimation(bool isWalking)
    {
        if (animController == null)
        {
            animController = GetComponent<AnimationController>();
            if (animController == null) return;
        }
        
        // Force animation state through multiple methods for redundancy
        if (isWalking)
        {
            // Direct animator access if available
            Animator animator = animController.Animator;
            if (animator != null)
            {
                // Set multiple parameters to ensure walking
                animator.SetBool("Walk", true);
                animator.SetBool("Idle", false);
                animator.SetFloat("WalkSpeed", 1f);
                
                // Try direct play as well
                animator.Play("Walk", 0, 0);
            }
            
            // Also use the controller's method
            animController.SetWalking(true);
            
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name}: Forcing WALK animation");
        }
        else
        {
            // Direct animator access if available
            Animator animator = animController.Animator;
            if (animator != null)
            {
                // Set multiple parameters to ensure idle
                animator.SetBool("Walk", false);
                animator.SetBool("Idle", true);
                animator.SetFloat("WalkSpeed", 0f);
                
                // Try direct play as well
                animator.Play("Idle", 0, 0);
            }
            
            // Also use the controller's method
            animController.SetWalking(false);
            
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name}: Forcing IDLE animation");
        }
    }
    
    // Add this coroutine to periodically check and fix animations
    private System.Collections.IEnumerator EnsureAnimationsWork()
    {
        yield return new WaitForSeconds(1f); // Wait for initialization
        
        while (true)
        {
            // If we're moving but the walk animation isn't playing, force it
            if (isMoving && animController != null && !animController.IsAnimationPlaying("walk"))
            {
                ForceUpdateAnimation(true);
                if (showDebugInfo)
                    Debug.Log($"Ally {gameObject.name}: Fixed missing walk animation");
            }
            
            // If we're not moving but idle animation isn't playing, force it
            if (!isMoving && animController != null && !animController.IsAnimationPlaying("idle"))
            {
                ForceUpdateAnimation(false);
                if (showDebugInfo)
                    Debug.Log($"Ally {gameObject.name}: Fixed missing idle animation");
            }
            
            yield return new WaitForSeconds(0.5f); // Check every half second
        }
    }
    
    // Handle enemy detection from hitbox
    private void HandleEnemyDetected(LivingEntity enemy)
    {
        // Don't attack when in ReturnToPlayer mode
        if (currentMode == AIMode.ReturnToPlayer)
            return;
            
        if (!enableAutoAttack || enemy == null || enemy.IsDead) 
            return;
        
        // Skip if this is not an insect (should be on Insects layer)
        if (!IsInsectLayer(enemy.gameObject))
            return;
            
        // Add to our list if not already there
        if (!enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Add(enemy);
            
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name}: Detected enemy {enemy.name}");
                
            // Start auto-attack if not already attacking
            if (autoAttackCoroutine == null)
            {
                autoAttackCoroutine = StartCoroutine(AutoAttackCoroutine());
            }
        }
    }
    
    // Handle enemy leaving detection range
    private void HandleEnemyLost(LivingEntity enemy)
    {
        if (enemy != null && enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Remove(enemy);
            
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name}: Lost enemy {enemy.name}, {enemiesInRange.Count} enemies remain");
        }
        
        // If no more enemies in range, stop auto-attacking
        if (enemiesInRange.Count == 0 && autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
            isAttacking = false;
        }
    }
    
    // Helper to check if object is on Insects layer
    private bool IsInsectLayer(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("Insects");
    }
    
    // Update the AttemptAttack method to be more reliable
    private bool AttemptAttack(LivingEntity target)
    {
        // Don't attack when in ReturnToPlayer mode
        if (currentMode == AIMode.ReturnToPlayer)
            return false;
            
        if (livingEntity == null || target == null || target.IsDead)
        {
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name}: Attack failed - invalid target or self");
            return false;
        }
        
        // Check if on cooldown with a small threshold to avoid floating point issues
        if (livingEntity.RemainingAttackCooldown > 0.05f)
        {
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name}: Attack on cooldown, remaining time: {livingEntity.RemainingAttackCooldown:F2}s");
            return false;
        }
        
        // Set the current target
        livingEntity.AddTargetInRange(target);
        
        // Try direct attack first for maximum reliability
        bool attackTriggered = livingEntity.TryAttack();
        
        // Then handle animation if attack was successful
        if (attackTriggered && animController != null)
        {
            animController.SetAttacking(true);
            
            // This should trigger the attack animation, which will call the damage event
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name}: Successfully attacking {target.name}");
            
            return true;
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name}: Attack failed - attack not triggered");
            return false;
        }
    }
    
    // Add a new method to calculate a better formation position
    private Vector3 GetFormationPosition()
    {
        if (playerTransform == null)
            return transform.position;
        
        // Get player's forward direction (or movement direction if available)
        Vector3 playerForward = playerTransform.forward;
        if (playerController != null && playerController.GetMovementDirection().magnitude > 0.1f)
        {
            playerForward = playerController.GetMovementDirection().normalized;
        }
        
        // Calculate formation offset based on ally index
        float angle;
        float distanceMult;
        
        // If we have more than 3 allies, use a more complex formation
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        if (allAllies.Length > 3)
        {
            // Create two rows of allies - first 3 in front row, rest in back row
            if (allyIndex < 3)
            {
                // Front row - spread out in an arc behind player
                angle = Mathf.Lerp(-60f, 60f, allyIndex / 2f);
                distanceMult = 1.0f;
            }
            else
            {
                // Back row - position in gaps between front row
                angle = Mathf.Lerp(-40f, 40f, (allyIndex - 3) / (float)(allAllies.Length - 3));
                distanceMult = 1.5f; // Place further back
            }
        }
        else
        {
            // Simple V formation for 1-3 allies
            angle = (allyIndex % 2 == 0) ? -30f * (1 + allyIndex/2) : 30f * (1 + allyIndex/2);
            distanceMult = 1.0f + (allyIndex * 0.1f); // Slightly increase distance for each ally
        }
        
        // Calculate the position offset based on the angle
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Vector3 offset = rotation * -playerForward * (followDistance * distanceMult);
        
        // Return the final position
        return playerTransform.position + offset;
    }

    // Add this to update the ally's position during the game
    private void OnEnable()
    {
        // This ensures allies redistribute when enabled/disabled
        ReassignFormationPositions();
    }

    // Add this method to help allies redistribute themselves
    private void ReassignFormationPositions()
    {
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        int aliveAllies = 0;
        
        // Count alive allies and find our index
        for (int i = 0; i < allAllies.Length; i++)
        {
            if (allAllies[i] != null && 
                allAllies[i].livingEntity != null && 
                !allAllies[i].livingEntity.IsDead)
            {
                if (allAllies[i] == this)
                {
                    allyIndex = aliveAllies;
                }
                aliveAllies++;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Ally {gameObject.name} reassigned to index: {allyIndex} of {aliveAllies} alive allies");
        }
    }
}
