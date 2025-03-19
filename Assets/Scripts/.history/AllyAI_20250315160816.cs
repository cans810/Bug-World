using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    public enum AIMode
    {
        Follow    // Follow player
        // Removed other modes (Wander, Carrying, GoingToLoot, Attacking)
    }
    
    [Header("AI Behavior")]
    [SerializeField] private AIMode currentMode = AIMode.Follow;
    
    [Header("Movement Settings")]
    [SerializeField] private float followDistance = 3f; // Distance to maintain from player
    [SerializeField] private float offsetAngle = 45f; // Angle offset from player's movement direction
    [SerializeField] private float speedMultiplier = 1.2f; // Move slightly faster to keep up with player
    
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
        UpdateAnimation(false);
        
        // Find ally index to determine position around player
        AssignAllyIndex();
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
    }
    
    private void HandleDeath()
    {
        isMoving = false;
        enabled = false;
    }
    
    private void Update()
    {
        // Reset the movement flag at the beginning of each frame
        hasAppliedMovementThisFrame = false;
        
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
            
        // Apply separation from overlapping allies
        SeparateFromOverlappingAllies();
        
        // If player reference is lost, try to find it again
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null) 
            {
                // Can't follow without player
                return;
            }
        }
        
        // Execute follow behavior since it's the only mode now
        FollowPlayerBehavior();
    }
    
    private void FollowPlayerBehavior()
    {
        // If player is not found, try to find again
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
                return; // Still can't find player
        }
        
        // Get player controller to access joystick movement data
        PlayerController playerCtrl = playerTransform.GetComponent<PlayerController>();
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Get player's movement direction from the joystick (if available)
        Vector3 playerMovementDirection = Vector3.zero;
        bool playerIsMoving = false;
        
        if (playerCtrl != null)
        {
            playerMovementDirection = playerCtrl.GetMovementDirection();
            playerIsMoving = playerMovementDirection.magnitude > 0.1f;
        }
        
        // Calculate ideal position based on player movement
        Vector3 idealPosition;
        
        if (playerIsMoving)
        {
            // When player is moving with joystick, position behind their movement direction
            idealPosition = playerTransform.position - playerMovementDirection.normalized * followDistance;
        }
        else
        {
            // When player is stationary, use their facing direction
            idealPosition = playerTransform.position - playerTransform.forward * followDistance;
        }
        
        // Update our target position with smoothing
        if (targetPosition == Vector3.zero) // Initialize on first use
        {
            targetPosition = transform.position;
        }
        
        // Apply smoothing to target position for more natural movement
        targetPosition = Vector3.SmoothDamp(targetPosition, idealPosition, ref smoothVelocity, positionUpdateInterval);
        
        // If we're close enough to the player or we've reached the target position, stop moving
        if (distanceToPlayer <= followDistance || 
            Vector3.Distance(transform.position, targetPosition) < minimumMoveDistance)
        {
            // Stop moving
            isMoving = false;
            
            // Update animation
            UpdateAnimation(false);
            
            // Stop velocity
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
            }
            
            // When stopped, gradually rotate to match player's movement direction (from joystick)
            // or forward direction if not moving
            Vector3 directionToFace = playerIsMoving && playerMovementDirection.magnitude > 0.1f ? 
                playerMovementDirection : playerTransform.forward;
            
            directionToFace.y = 0; // Keep on same Y level
            
            // Only rotate if the direction is significant
            if (directionToFace.magnitude > 0.1f)
            {
                // Use livingEntity.RotateTowards for consistent rotation
                livingEntity.RotateTowards(directionToFace, rotationMultiplier);
            }
        }
        // If we're too far or haven't reached target position, move towards the target
        else
        {
            isMoving = true;
            
            // Update animation
            UpdateAnimation(true);
            
            // Calculate direction to target position
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            directionToTarget.y = 0; // Keep on same Y level
            
            // If player is moving with joystick, blend in their movement direction
            // for more responsive following
            if (playerIsMoving)
            {
                // Blend target position direction with player's movement direction
                // More weight on player movement for better responsiveness
                Vector3 blendedDirection = (directionToTarget * 0.4f + 
                                            playerMovementDirection.normalized * 0.6f).normalized;
                
                directionToTarget = blendedDirection;
            }
            
            // Apply movement with avoidance
            if (avoidOtherAllies)
            {
                directionToTarget = ApplyAllyAvoidance(directionToTarget, allyDetectionRadius);
            }
            
            // First rotate consistently using the consistent rotation method
            livingEntity.RotateTowards(directionToTarget, rotationMultiplier);
            
            // Calculate movement speed based on distance to player
            float distanceSpeedMultiplier = 1.0f;
            
            // Move faster when far from player to catch up
            if (distanceToPlayer > maxDistanceToPlayer * 1.5f)
            {
                distanceSpeedMultiplier = 2.0f; // Boost speed to catch up
            }
            
            // Move using the consistent movement method
            livingEntity.MoveInDirection(transform.forward, speedMultiplier * distanceSpeedMultiplier);
            
            // Mark that we've applied movement this frame
            hasAppliedMovementThisFrame = true;
        }
        
        // Debug visualization
        if (showDebugInfo)
        {
            Debug.DrawLine(transform.position, targetPosition, Color.yellow);
            Debug.DrawLine(transform.position, playerTransform.position, Color.blue);
            Debug.DrawRay(transform.position, transform.forward * 2f, Color.green);
            if (playerIsMoving)
            {
                Debug.DrawRay(playerTransform.position, playerMovementDirection * 3f, Color.red);
            }
        }
    }
    
    // New method to calculate avoidance direction
    private Vector3 ApplyAllyAvoidance(Vector3 currentDirection, float detectionRadius)
    {
        if (!avoidOtherAllies)
            return currentDirection;
        
        // Use a very simple avoidance system - only avoid extreme closeness
        Collider[] nearbyAllies = Physics.OverlapSphere(transform.position, allyAvoidanceDistance * 0.5f, allyLayer);
        
        if (nearbyAllies.Length <= 1) // Only this ally detected or none
            return currentDirection;
        
        Vector3 avoidanceDir = Vector3.zero;
        
        foreach (Collider allyCollider in nearbyAllies)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
            
            // Get direction away from ally
            Vector3 dirAway = transform.position - allyCollider.transform.position;
            dirAway.y = 0;
            
            float distance = dirAway.magnitude;
            
            // Only avoid if extremely close
            if (distance < allyAvoidanceDistance * 0.4f)
            {
                // Simple scaling - stronger when closer
                float strength = 1f - (distance / (allyAvoidanceDistance * 0.4f));
                avoidanceDir += dirAway.normalized * strength;
            }
        }
        
        if (avoidanceDir.magnitude < 0.01f)
            return currentDirection;
        
        // Blend avoidance into the current direction very subtly
        Vector3 result = (currentDirection * 0.85f) + (avoidanceDir.normalized * 0.15f);
        return result.normalized;
    }

    // Add this method to the AllyAI class
    private void EnsureConsistentSpeed()
    {
        // Check if speed is valid
        if (livingEntity.moveSpeed <= 0.1f)
        {
            // Try to get player speed again
            if (playerTransform != null)
            {
                LivingEntity playerEntity = playerTransform.GetComponent<LivingEntity>();
                if (playerEntity != null && playerEntity.moveSpeed > 0.1f)
                {
                    livingEntity.moveSpeed = playerEntity.moveSpeed;
                    Debug.LogWarning($"Reset {gameObject.name} speed to match player: {livingEntity.moveSpeed}");
                }
                else
                {
                    // Use default speed
                    livingEntity.moveSpeed = 2.0f;
                    Debug.LogWarning($"Reset {gameObject.name} to default speed: {livingEntity.moveSpeed}");
                }
            }
            else
            {
                // Use default speed
                livingEntity.moveSpeed = 2.0f;
                Debug.LogWarning($"Reset {gameObject.name} to default speed: {livingEntity.moveSpeed}");
            }
        }
    }

    // Add this method to the AllyAI class
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
                // Normalize and scale by how close we are (gentler curve)
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
            // Normalize and apply using LivingEntity's movement, but gentler
            separationMove = separationMove.normalized;
            
            // Use a smaller movement to avoid jitter
            livingEntity.MoveInDirection(separationMove, 0.3f);
            
            hasAppliedMovementThisFrame = true;
        }
    }

    // Add this method to handle animation updates
    private void UpdateAnimation(bool isWalking)
    {
        if (animController == null)
            return;
        
        try
        {
            // Only update if the state is changing
            if (animController.IsAnimationPlaying("walk") != isWalking)
            {
                // Use direct parameter setting instead of CrossFade
                Animator animator = animController.Animator;
                if (animator != null)
                {
                    animator.SetBool("Walk", isWalking);
                    animator.SetBool("Idle", !isWalking);
                }
                else
                {
                    // Fallback to AnimationController method
                    animController.SetWalking(isWalking);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating animation on {gameObject.name}: {e.Message}");
        }
    }
}
