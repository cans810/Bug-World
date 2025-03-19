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
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal states
    private bool isMoving = false;
    private Transform playerTransform;
    
    [Header("Ally Avoidance")]
    [SerializeField] private bool avoidOtherAllies = true;
    [SerializeField] private float allyAvoidanceDistance = 1.0f;
    [SerializeField] private float allyDetectionRadius = 1.2f;
    [SerializeField] private LayerMask allyLayer;
    [SerializeField] private float avoidanceStrength = 1.5f;
    
    // Add this field to track if we've already applied movement this frame
    private bool hasAppliedMovementThisFrame = false;
    
    // Public property to check if movement has been applied this frame
    public bool HasAppliedMovementThisFrame => hasAppliedMovementThisFrame;
    
    [Header("Movement Tuning")]
    [SerializeField] private float speedMultiplier = 1.5f; // Increase ally speed
    [SerializeField] private float rotationMultiplier = 2.0f; // Increase rotation speed
    
    [Header("Movement Smoothing")]
    [SerializeField] private bool useMovementSmoothing = true;
    [SerializeField] private float directionSmoothTime = 0.2f;
    [SerializeField] private float minimumMoveDistance = 0.05f;

    // Add these fields to store smoothed values
    private Vector3 smoothedMoveDirection;
    private Vector3 directionVelocity; // Used for SmoothDamp
    private float positionRecalculationInterval = 0.1f; // Only recalculate position every 0.1 seconds
    private float lastPositionUpdateTime = 0f;
    
    [Header("Follow Behavior")]
    [SerializeField] private bool stopWhenPlayerStops = true;
    [SerializeField] private float playerMovementThreshold = 0.1f; // How much the player needs to move to be considered "moving"
    [SerializeField] private float playerStationaryStopDistance = 4.0f; // Stop further away when player is stationary

    // Add fields to track player movement
    private Vector3 lastPlayerPosition;
    private bool isPlayerMoving = false;
    private float playerVelocityCheckInterval = 0.2f;
    private float lastPlayerVelocityCheckTime = 0f;
    
    [Header("Smoothing Settings")]
    [SerializeField] private float positionUpdateInterval = 0.3f; // Less frequent updates
    [SerializeField] private float rotationSmoothFactor = 0.1f; // Very smooth rotation
    [SerializeField] private float movementSmoothTime = 0.5f; // Longer smooth time for movement
    [SerializeField] private float velocitySmoothFactor = 0.7f; // Smooth velocity changes

    // Add these fields for smoothing
    private Vector3 targetPosition;
    private Vector3 smoothVelocity;
    private float lastMajorUpdateTime = 0f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private float maxDistanceToPlayer = 10f;
    
    private void Start()
    {
        // Find player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
        
        // Ensure entity is set to be destroyed after death
        if (livingEntity != null)
        {
            // Subscribe to death event
            livingEntity.OnDeath.AddListener(HandleDeath);
        }

        // Set move speed immediately and ensure it's a reasonable value
        if (playerTransform != null)
        {
            LivingEntity playerEntity = playerTransform.GetComponent<LivingEntity>();
            if (playerEntity != null)
            {
                float playerSpeed = playerEntity.moveSpeed;
                // Ensure we're not getting a zero or negative speed
                if (playerSpeed > 0.1f)
                {
                    livingEntity.moveSpeed = playerSpeed;
                    Debug.Log($"Set {gameObject.name} speed to match player: {playerSpeed}");
                }
                else
                {
                    // Use a default speed if player speed is invalid
                    livingEntity.moveSpeed = 2.0f;
                    Debug.LogWarning($"Player speed invalid ({playerSpeed}), using default speed: {livingEntity.moveSpeed}");
                }
            }
        }
        
        // Start idle
        UpdateAnimation(false);
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
