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
        ForceUpdateAnimation(false);
        
        // Find ally index to determine position around player
        AssignAllyIndex();
        
        // Add this to ensure animations are working properly
        StartCoroutine(EnsureAnimationsWork());
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
        // Reset movement flag
        hasAppliedMovementThisFrame = false;
        
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
            
        // Apply separation from overlapping allies
        SeparateFromOverlappingAllies();
        
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
        
        // Calculate movement based on joystick input
        MoveWithJoystick(joystickInput);
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
        
        // Calculate position offset based on ally index
        float angleDegrees = (allyIndex * 60f) % 360f; // Distribute allies around player
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        
        // Calculate offset direction
        Vector3 offsetDirection = new Vector3(
            Mathf.Sin(angleRadians),
            0,
            Mathf.Cos(angleRadians)
        ).normalized;
        
        // Calculate target position: player position plus offset in movement direction
        Vector3 targetPosition = playerTransform.position + (offsetDirection * followDistance);
        
        // Adjust movement based on position relative to target
        Vector3 toTarget = targetPosition - transform.position;
        float distanceToTarget = toTarget.magnitude;
        
        // If too far from target position, move directly toward it
        if (distanceToTarget > followDistance * 1.5f)
        {
            moveDirection = toTarget.normalized;
        }
        // If at moderate distance, blend between joystick direction and catch-up direction
        else if (distanceToTarget > followDistance * 0.8f)
        {
            float blendFactor = Mathf.InverseLerp(followDistance * 0.8f, followDistance * 1.5f, distanceToTarget);
            moveDirection = Vector3.Lerp(moveDirection, toTarget.normalized, blendFactor);
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
        
        // Always use constant speed factor of 1.0 for consistent movement speed
        float speedFactor = 1.0f;
        
        // Apply movement
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
    
    private void MoveToFollowPosition()
    {
        // When player stops, allies should also stop moving if they're reasonably close
        if (playerTransform == null) return;
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // If already reasonably close to player, just stop and face player
        if (distanceToPlayer <= followDistance * 1.5f)
        {
            // Stop moving
            isMoving = false;
            ForceUpdateAnimation(false);
            
            // Just face the player when stopped
            Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
            livingEntity.RotateTowards(toPlayer, 1.0f);
            return;
        }
        
        // If too far from player, move closer
        Vector3 moveDirection = (playerTransform.position - transform.position).normalized;
        
        // Apply smoothing and avoidance
        if (useMovementSmoothing)
        {
            smoothedMoveDirection = Vector3.SmoothDamp(
                smoothedMoveDirection, 
                moveDirection, 
                ref directionVelocity, 
                movementSmoothTime);
                
            moveDirection = smoothedMoveDirection;
        }
        
        if (avoidOtherAllies)
        {
            moveDirection = ApplyAllyAvoidance(moveDirection, allyDetectionRadius);
        }
        
        // Rotate and move to get within reasonable distance of player
        livingEntity.RotateTowards(moveDirection, 3.0f);
        
        // Use constant speed factor of 1.0 for consistent movement
        livingEntity.MoveInDirection(moveDirection, 1.0f);
        
        ForceUpdateAnimation(true);
        hasAppliedMovementThisFrame = true;
    }
    
    // New method to calculate avoidance direction
    private Vector3 ApplyAllyAvoidance(Vector3 currentDirection, float detectionRadius)
    {
        if (!avoidOtherAllies)
            return currentDirection;
        
        // Use a simple avoidance system - only avoid extreme closeness
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
        
        // Blend avoidance into the current direction
        Vector3 result = (currentDirection * 0.85f) + (avoidanceDir.normalized * 0.15f);
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
}
