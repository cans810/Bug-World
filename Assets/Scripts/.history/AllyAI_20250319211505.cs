using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    public enum AIMode
    {
        Follow,     // Follow player
        CatchingUp  // Moving directly to player when too far away
    }
    
    [Header("AI Behavior")]
    [SerializeField] private AIMode currentMode = AIMode.Follow;
    
    [Header("Movement Settings")]
    [SerializeField] private float followDistance = 3f; // Distance to maintain from player
    [SerializeField] private float offsetAngle = 45f; // Angle offset from player's movement direction
    [SerializeField] private float speedMultiplier = 1.2f; // Move slightly faster to keep up with player
    [SerializeField] private float maxDistanceBeforeCatchup = 10f; // Distance at which ally will switch to catch-up mode
    [SerializeField] private float catchupSpeedMultiplier = 1.8f; // How much faster to move when catching up
    [SerializeField] private float distanceToResumeFormation = 5f; // Distance at which ally returns to formation
    
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
        
        // Don't move if currently attacking
        if (isAttacking)
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
        
        // Check distance to player and determine mode
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Check if we need to switch to catchup mode
        if (distanceToPlayer > maxDistanceBeforeCatchup && currentMode != AIMode.CatchingUp)
        {
            currentMode = AIMode.CatchingUp;
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name} switching to CATCHUP mode - Distance: {distanceToPlayer:F1}");
        }
        // Check if we can return to formation mode
        else if (distanceToPlayer < distanceToResumeFormation && currentMode == AIMode.CatchingUp)
        {
            currentMode = AIMode.Follow;
            if (showDebugInfo)
                Debug.Log($"Ally {gameObject.name} returning to FOLLOW mode - Distance: {distanceToPlayer:F1}");
        }
        
        // Get joystick input from either the joystick component or player controller
        Vector2 joystickInput = Vector2.zero;
        if (joystick != null && joystick.IsDragging)
        {
            joystickInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        }
        else if (playerController != null)
        {
            // If we can't get direct joystick input, use the player's movement
            Vector3 playerMovement = playerController.GetMovementDirection();
            joystickInput = new Vector2(playerMovement.x, playerMovement.z);
        }
        
        // Handle movement based on current mode
        if (currentMode == AIMode.CatchingUp)
        {
            // Directly move toward player in catchup mode
            MoveDirectlyToPlayer();
        }
        else
        {
            // Use regular formation-based movement in follow mode
            MoveWithJoystick(joystickInput);
        }
    }
    
    // New method for direct movement to player when catching up
    private void MoveDirectlyToPlayer()
    {
        if (playerTransform == null) return;
        
        // Calculate direct path to player
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0; // Keep on the same Y plane
        
        // Skip if we're already very close
        float distance = directionToPlayer.magnitude;
        if (distance < 0.5f)
        {
            // We're close enough, just stop
            ForceUpdateAnimation(false);
            return;
        }
        
        // Normalize direction
        directionToPlayer.Normalize();
        
        // Apply ally avoidance (but less strongly during catchup)
        directionToPlayer = ApplyAllyAvoidance(directionToPlayer, allyDetectionRadius * 0.8f);
        
        // Rotate toward target
        livingEntity.RotateTowards(directionToPlayer, livingEntity.rotationSpeed * 1.5f);
        
        // Move directly toward player with increased speed
        livingEntity.MoveInDirection(directionToPlayer, catchupSpeedMultiplier);
        
        // Update animation
        ForceUpdateAnimation(true);
        hasAppliedMovementThisFrame = true;
    }
    
    private void MoveWithJoystick(Vector2 joystickInput)
    {
        // If no input, stop moving
        if (joystickInput.magnitude < 0.1f)
        {
            // Stop movement
            isMoving = false;
            ForceUpdateAnimation(false);
            
            // For a natural following behavior, move toward follow position when player stops
            MoveToFollowPosition();
            return;
        }
        
        // Since there's joystick input, we're definitely moving
        isMoving = true;
        
        // Get camera for direction calculations
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;
            
        // Get camera forward and right vectors (ignore y component)
        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        
        Vector3 cameraRight = mainCamera.transform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();
        
        // Calculate base movement direction from joystick relative to camera
        Vector3 moveDirection = (cameraForward * joystickInput.y + cameraRight * joystickInput.x).normalized;
        
        // Calculate position offset based on ally index and player's movement direction
        // This creates a more natural formation that adjusts based on player movement
        float baseAngleDegrees = (allyIndex * 60f) % 360f; // Base distribution
        
        // Adjust formation based on player movement - narrow formation when moving
        Vector3 playerMoveDir = new Vector3(joystickInput.x, 0, joystickInput.y).normalized;
        float formationAngle = baseAngleDegrees;
        
        // Calculate formation point behind player
        Quaternion rotation = Quaternion.Euler(0, formationAngle + 180f, 0);
        Vector3 formationOffset = rotation * playerMoveDir * followDistance;
        if (playerMoveDir.magnitude < 0.1f)
        {
            // If player is stationary, use their forward direction instead
            formationOffset = rotation * -playerTransform.forward * followDistance;
        }
        
        // Get target position for this ally
        Vector3 targetPosition = playerTransform.position + formationOffset;
        
        // Calculate direction to target position
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0; // Keep movement on the XZ plane
        
        // Skip if we're already very close to target
        float distanceToTarget = directionToTarget.magnitude;
        if (distanceToTarget < 0.1f)
        {
            ForceUpdateAnimation(false);
            return;
        }
        
        directionToTarget.Normalize();
        
        // Apply other ally avoidance to prevent crowding
        Vector3 moveDirectionWithAvoidance = ApplyAllyAvoidance(directionToTarget, allyDetectionRadius);
        
        // Apply smoothing if enabled
        if (useMovementSmoothing)
        {
            smoothedMoveDirection = Vector3.SmoothDamp(
                smoothedMoveDirection,
                moveDirectionWithAvoidance,
                ref directionVelocity,
                movementSmoothTime
            );
            moveDirectionWithAvoidance = smoothedMoveDirection.normalized;
        }
        
        // Rotate toward movement direction
        livingEntity.RotateTowards(moveDirectionWithAvoidance, livingEntity.rotationSpeed);
        
        // Use a dynamic speed - faster when further away, slower when close
        float speedFactor = Mathf.Clamp(distanceToTarget / followDistance, 0.5f, 1.2f);
        livingEntity.MoveInDirection(moveDirectionWithAvoidance, speedFactor);
        
        // Only set animation to walking if we're actually moving a significant distance
        if (distanceToTarget > 0.3f)
        {
            ForceUpdateAnimation(true);
            hasAppliedMovementThisFrame = true;
        }
        else
        {
            ForceUpdateAnimation(false);
        }
    }
    
    // Improve the ally avoidance calculation to be more responsive
    private Vector3 ApplyAllyAvoidance(Vector3 currentDirection, float detectionRadius)
    {
        if (!avoidOtherAllies)
            return currentDirection;
        
        // Detect other allies in the vicinity
        Collider[] nearbyAllies = Physics.OverlapSphere(transform.position, detectionRadius, allyLayer);
        
        if (nearbyAllies.Length <= 1) // Only this ally detected or none
            return currentDirection;
        
        Vector3 avoidanceDir = Vector3.zero;
        int avoidanceCount = 0;
        
        foreach (Collider allyCollider in nearbyAllies)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
            
            // Get direction away from ally
            Vector3 dirAway = transform.position - allyCollider.transform.position;
            dirAway.y = 0;
            
            float distance = dirAway.magnitude;
            
            // Progressive avoidance strength based on distance
            if (distance < detectionRadius)
            {
                // Calculate avoidance strength - stronger when closer
                float strength = 1f - (distance / detectionRadius);
                strength = Mathf.Pow(strength, 1.5f); // Non-linear falloff (sharper at close distances)
                
                // Check if the ally is in our movement path (in front of us)
                float dotProduct = Vector3.Dot(currentDirection.normalized, dirAway.normalized);
                
                // If ally is in front of us, avoid more strongly
                if (dotProduct < 0)
                    strength *= 1.5f;
                
                avoidanceDir += dirAway.normalized * strength;
                avoidanceCount++;
            }
        }
        
        if (avoidanceCount == 0)
            return currentDirection;
        
        // Calculate final avoidance direction
        avoidanceDir = avoidanceDir / avoidanceCount;
        
        // Blend current direction with avoidance direction - adaptive weighting
        float avoidanceWeight = Mathf.Clamp01(avoidanceDir.magnitude);
        Vector3 result = Vector3.Lerp(currentDirection, avoidanceDir.normalized, avoidanceWeight * 0.6f);
        
        return result.normalized;
    }
    
    private void SeparateFromOverlappingAllies()
    {
        if (!avoidOtherAllies)
            return;
        
        // Find allies that are too close
        Collider[] overlappingAllies = Physics.OverlapSphere(transform.position, allyAvoidanceDistance * 0.5f, allyLayer);
        
        Vector3 separationMove = Vector3.zero;
        int separationCount = 0;
        
        foreach (Collider allyCollider in overlappingAllies)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
            
            // Calculate direction away from the ally
            Vector3 awayDirection = transform.position - allyCollider.transform.position;
            awayDirection.y = 0; // Keep on the same Y level
            
            float distance = awayDirection.magnitude;
            
            // If we're very close, apply immediate separation
            if (distance < 0.01f)
            {
                // If exactly overlapping, move in a random direction
                awayDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            }
            else if (distance < allyAvoidanceDistance * 0.5f)
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
            if (isMoving)
            {
                ForceUpdateAnimation(true);
            }
            
            yield return new WaitForSeconds(2f); // Check every 2 seconds
        }
    }
    
    // Method to move ally to default follow position when player is not moving
    private void MoveToFollowPosition()
    {
        if (playerTransform == null)
            return;
            
        // Get the optimal formation position
        Vector3 formationPosition = GetFormationPosition();
        
        // Calculate direction to position
        Vector3 directionToPosition = formationPosition - transform.position;
        directionToPosition.y = 0;
        
        // Don't move if already close
        float distance = directionToPosition.magnitude;
        if (distance < 0.5f)
            return;
            
        // Normalize and apply avoidance
        directionToPosition.Normalize();
        directionToPosition = ApplyAllyAvoidance(directionToPosition, allyDetectionRadius);
        
        // Rotate toward target direction
        livingEntity.RotateTowards(directionToPosition, livingEntity.rotationSpeed);
        
        // Apply move - faster when further away
        float speedFactor = Mathf.Clamp(distance / followDistance, 0.5f, 1.0f);
        livingEntity.MoveInDirection(directionToPosition, speedFactor);
        
        // Set movement animation
        if (distance > 0.5f)
        {
            ForceUpdateAnimation(true);
            hasAppliedMovementThisFrame = true;
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
    
    // Add methods for handling enemy detection (needed for combat)
    private void HandleEnemyDetected(GameObject enemy)
    {
        // Implement combat detection logic here
    }
    
    private void HandleEnemyLost(GameObject enemy)
    {
        // Implement enemy lost logic here
    }
}
