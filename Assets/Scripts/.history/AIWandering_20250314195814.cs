using UnityEngine;
using System.Collections;

public class AIWandering : MonoBehaviour
{
    [Header("Wandering Settings")]
    [SerializeField] private float minWanderDistance = 3f;
    [SerializeField] private float maxWanderDistance = 10f;
    [SerializeField] private float waypointReachedDistance = 0.5f;
    
    [Header("Timing")]
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 5f;
    [SerializeField] private float pathfindingUpdateInterval = 0.5f;
    
    [Header("Obstacle Avoidance")]
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField] private float obstacleDetectionRange = 2f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Animation")]
    [SerializeField] private AnimationController animationController;

    [SerializeField] public LivingEntity livingEntity;
    
    [Header("Nest Avoidance")]
    [SerializeField] private bool avoidPlayerNest = true;
    [SerializeField] private float nestAvoidanceDistance = 3f;
    [SerializeField] private string nestObjectName = "Player Nest 1";

    // Internal variables
    private Vector3 currentWaypoint;
    private bool isWandering = false;
    private bool isWaiting = false;
    private Animator animator;
    private AllyAI allyAI;
    
    private Transform playerNestTransform;
    private float nestRadius = 0f;
    
    private bool isHittingBoundary = false;
    private float boundaryRedirectionTime = 0f;
    private const float BOUNDARY_REDIRECT_DURATION = 1.0f;
    
    private void Start()
    {
        animator = GetComponent<Animator>();

        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
        
        if (animationController == null)
        {
            animationController = GetComponent<AnimationController>();
            if (animationController == null)
            {
                animationController = GetComponentInChildren<AnimationController>();
            }
        }
        
        // Get reference to AllyAI component
        allyAI = GetComponent<AllyAI>();
        
        // Find the player nest
        FindPlayerNest();
        
        StartCoroutine(WanderingRoutine());
    }
    
    private void Update()
    {
        // Skip movement if AllyAI has already moved this frame
        if (allyAI != null && allyAI.HasAppliedMovementThisFrame)
        {
            return;
        }
        
        // Skip if not visible to camera (optimization)
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null && !renderer.isVisible)
        {
            return;
        }
        
        // IMPORTANT: We remove the automatic movement here to prevent interference
        // with our coroutine-based straight-line movement
        
        // We no longer call MoveTowardsWaypoint() here
    }
    
    private IEnumerator WanderingRoutine()
    {
        while (true)
        {
            // 1. STOP AND WAIT
            isWaiting = true;
            isWandering = false;
            UpdateAnimationState(false);
            
            // Wait for a random time
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // 2. PICK A NEW RANDOM DIRECTION
            Vector3 randomDirection = GetRandomDirection();
            
            // Make sure the direction doesn't lead into the nest or boundaries
            if (!IsAlly()) 
            {
                // Check if direction would lead toward nest
                if (playerNestTransform != null && avoidPlayerNest)
                {
                    Vector3 dirToNest = (playerNestTransform.position - transform.position).normalized;
                    float dotProduct = Vector3.Dot(randomDirection, dirToNest);
                    
                    // If pointing toward nest (dot product > 0), pick a different direction
                    if (dotProduct > 0.5f)
                    {
                        // Get direction away from nest instead
                        randomDirection = -dirToNest;
                        
                        // Add some randomness to the away-from-nest direction
                        randomDirection = Quaternion.Euler(0, Random.Range(-45f, 45f), 0) * randomDirection;
                    }
                }
            }
            
            // 3. TURN TO FACE NEW DIRECTION
            float turnDuration = 0.5f;
            float turnTimer = 0;
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(randomDirection);
            
            // Turn toward new direction over time
            while (turnTimer < turnDuration)
            {
                float turnProgress = turnTimer / turnDuration;
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnProgress);
                turnTimer += Time.deltaTime;
                yield return null;
            }
            
            // Make sure rotation is exactly correct at the end
            transform.rotation = targetRotation;
            
            // 4. MOVE IN STRAIGHT LINE
            isWaiting = false;
            isWandering = true;
            UpdateAnimationState(true);
            
            // Store the starting position for true straight-line movement
            Vector3 startPosition = transform.position;
            Vector3 moveDirection = transform.forward;
            
            // Calculate how long to walk (distance-based)
            float moveDistance = Random.Range(minWanderDistance, maxWanderDistance);
            float moveDuration = moveDistance / livingEntity.moveSpeed;
            float moveTimer = 0;
            float distanceMoved = 0;
            
            // Move in the straight line for the calculated time
            while (moveTimer < moveDuration && distanceMoved < moveDistance)
            {
                if (!isWandering) // Handle interruptions
                    break;
                    
                if (avoidObstacles)
                {
                    // Check for obstacles without changing direction
                    if (IsObstacleAhead())
                    {
                        // If obstacle detected, break out of movement loop
                        break;
                    }
                }
                
                // Calculate the exact movement step for this frame
                float stepDistance = livingEntity.moveSpeed * Time.deltaTime;
                
                // Apply movement directly to transform for perfectly straight movement
                // Disable any physics or other systems that might interfere
                Vector3 newPosition = transform.position + moveDirection * stepDistance;
                
                // Apply the movement - either direct transform manipulation or livingEntity method
                if (livingEntity != null)
                {
                    // Prevent any rotation during movement
                    Quaternion previousRotation = transform.rotation;
                    
                    // Move using living entity
                    livingEntity.MoveInDirection(moveDirection, 1.0f);
                    
                    // Ensure rotation didn't change during movement
                    transform.rotation = previousRotation;
                }
                else
                {
                    // Direct transform manipulation as fallback
                    transform.position = newPosition;
                }
                
                // Update distance moved by calculating actual movement this frame
                distanceMoved += Vector3.Distance(transform.position, startPosition + moveDirection * distanceMoved);
                
                moveTimer += Time.deltaTime;
                yield return null;
            }
        }
    }
    
    private void FindPlayerNest()
    {
        // Find the player nest by name
        GameObject nestObject = GameObject.Find(nestObjectName);
        if (nestObject != null)
        {
            playerNestTransform = nestObject.transform;
            
            // Get the sphere collider to determine radius
            SphereCollider nestCollider = nestObject.GetComponent<SphereCollider>();
            if (nestCollider != null)
            {
                nestRadius = nestCollider.radius * Mathf.Max(
                    nestObject.transform.lossyScale.x,
                    nestObject.transform.lossyScale.y,
                    nestObject.transform.lossyScale.z
                );
            }
            else
            {
                // Default radius if no collider found
                nestRadius = 5f;
                Debug.LogWarning("Player nest found but has no SphereCollider, using default radius");
            }
        }
        else
        {
            Debug.LogWarning($"Player nest '{nestObjectName}' not found in scene");
        }
    }

    // Add this method to check if this entity is an ally
    private bool IsAlly()
    {
        // Check if this entity is on the Ally layer
        return gameObject.layer == LayerMask.NameToLayer("Ally");
    }

    // Modify IsPositionTooCloseToNest to skip for allies
    private bool IsPositionTooCloseToNest(Vector3 position)
    {
        // Skip nest avoidance for allies
        if (IsAlly())
            return false;
        
        // Original check for non-allies
        if (!avoidPlayerNest || playerNestTransform == null)
            return false;
        
        float distanceToNest = Vector3.Distance(position, playerNestTransform.position);
        return distanceToNest < (nestRadius + nestAvoidanceDistance);
    }

    // Modify MoveWithBoundaryCheck to check for ally status first
    private void MoveWithBoundaryCheck(Vector3 direction)
    {
        // Skip if we're redirecting after hitting boundary
        if (isHittingBoundary)
        {
            if (Time.time > boundaryRedirectionTime)
            {
                isHittingBoundary = false;
            }
            else
            {
                return; // Skip movement while redirecting
            }
        }
        
        // Calculate movement based on frame time
        Vector3 movement = direction * livingEntity.moveSpeed * Time.deltaTime;
        
        // Calculate the next position
        Vector3 nextPosition = transform.position + movement;
        
        // Check only for nest proximity now
        bool isTooCloseToNest = IsPositionTooCloseToNest(nextPosition);
        
        if (isTooCloseToNest && !IsAlly())
        {
            // We've hit the nest boundary, turn around
            Vector3 oppositeDirection = -transform.forward;
            transform.forward = oppositeDirection;
            SetWaypointInDirection(oppositeDirection);
        }
        else
        {
            // Move normally - no boundary check
            transform.position += movement;
        }
    }
    
    // Add a helper method to get direction away from nest
    private Vector3 GetDirectionAwayFromNest(Vector3 currentPosition)
    {
        if (playerNestTransform == null)
            return -transform.forward; // Default to opposite of current direction
        
        return (currentPosition - playerNestTransform.position).normalized;
    }

    // Modify SetRandomWaypoint to consider ally status
    private void SetRandomWaypoint()
    {
        Vector3 randomWaypoint = Vector3.zero;
        bool validWaypointFound = false;
        int maxAttempts = 10;
        int attempts = 0;
        
        while (!validWaypointFound && attempts < maxAttempts)
        {
            float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Cos(randomAngle) * randomDistance,
                0f, // keep on the same Y level
                Mathf.Sin(randomAngle) * randomDistance
            );
            
            randomWaypoint = transform.position + offset;
            
            // For allies, we have no boundary checks now
            if (IsAlly())
            {
                validWaypointFound = true;
            }
            // For enemies, check nest proximity only
            else if (!IsAlly())
            {
                bool notTooCloseToNest = !IsPositionTooCloseToNest(randomWaypoint);
                if (notTooCloseToNest)
                {
                    validWaypointFound = true;
                }
            }
            
            attempts++;
        }
        
        // If we couldn't find a valid waypoint, use current position
        if (!validWaypointFound)
        {
            randomWaypoint = transform.position;
        }
        
        currentWaypoint = randomWaypoint;
    }
    
    private void MoveTowardsWaypoint()
    {
        // Calculate direction to the waypoint
        Vector3 direction = (currentWaypoint - transform.position).normalized;
        direction.y = 0; // Keep on the same Y level
        
        // Only rotate and move if we have a valid direction
        if (direction.magnitude > 0.1f)
        {
            // First rotate towards the waypoint
            if (livingEntity != null)
            {
                livingEntity.RotateTowards(direction, 1.5f);
                
                // Then move forward using the entity's forward direction
                livingEntity.MoveInDirection(transform.forward, 1.0f);
            }
            else
            {
                // Fallback if living entity missing
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 4f * Time.deltaTime);
                transform.position += transform.forward * 2f * Time.deltaTime;
            }
        }
    }
    
    // Modify SetWaypointInDirection to consider ally status
    private void SetWaypointInDirection(Vector3 direction)
    {
        // Set a waypoint at a random distance in the given direction
        float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
        Vector3 newWaypoint = transform.position + direction * randomDistance;
        
        // For allies, skip nest proximity check
        if (!IsAlly())
        {
            // Make sure it's not too close to nest (only for non-allies)
            if (IsPositionTooCloseToNest(newWaypoint) && playerNestTransform != null)
            {
                // Try to find a point that's away from the nest but still in the desired direction
                Vector3 awayFromNest = GetDirectionAwayFromNest(transform.position);
                Vector3 blendedDirection = (direction + awayFromNest).normalized;
                
                newWaypoint = transform.position + blendedDirection * randomDistance;
            }
        }
        
        currentWaypoint = newWaypoint;
    }
    
    private void AvoidObstacles()
    {
        RaycastHit hit;
        Vector3 rayDirection = transform.forward;
        
        // Cast rays in multiple directions to detect obstacles (but not other insects)
        for (int i = -2; i <= 2; i++)
        {
            float angle = i * 30f; // Check at -60, -30, 0, 30, and 60 degrees
            Vector3 direction = Quaternion.Euler(0, angle, 0) * rayDirection;
            
            Debug.DrawRay(transform.position + Vector3.up * 0.5f, direction * obstacleDetectionRange, Color.red, 0.1f);
            
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, out hit, obstacleDetectionRange, obstacleLayer))
            {
                // Found an obstacle, adjust waypoint
                Vector3 avoidanceDirection = Vector3.Cross(Vector3.up, direction);
                
                // Randomly choose left or right to avoid
                if (Random.value < 0.5f)
                    avoidanceDirection = -avoidanceDirection;
                    
                currentWaypoint = transform.position + avoidanceDirection.normalized * Random.Range(minWanderDistance, maxWanderDistance);
                
                break;
            }
        }
    }

    // this is the animation controller for the wandering AI, it determines if the enemy anim will play the walk or idle animation
    private void UpdateAnimationState(bool isWalking)
    {
        if (animationController != null)
        {
            if (isWalking)
            {
                // Use direct methods rather than wrappers when possible
                animationController.SetWalking(true);
            }
            else
            {
                animationController.SetIdle();
            }
        }
        else if (animator != null)
        {
            animator.SetBool("Walk", isWalking);
            animator.SetBool("Idle", !isWalking);
        }
    }
    
    // Make SetWanderingEnabled more forceful in taking control
    public void SetWanderingEnabled(bool enabled)
    {
        isWandering = enabled;
        
        // Force animation update immediately
        UpdateAnimationState(enabled && !isWaiting);
        
        // If disabling, make sure to stop movement
        if (!enabled)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
            }
        }
    }

    public bool IsCurrentlyMoving()
    {
        // Only return true when the AI is actually moving (wandering and not waiting)
        return isWandering && !isWaiting;
    }

    public void SetWanderDistances(float min, float max)
    {
        minWanderDistance = min;
        maxWanderDistance = max;
    }

    // Add a public method to force setting a new waypoint
    public void ForceNewWaypoint()
    {
        // Set a new random waypoint
        SetRandomWaypoint();
        
        // Reset waiting state to start moving immediately
        isWaiting = false;
        isWandering = true;
        
        // Update animation to walking
        UpdateAnimationState(true);
    }

    // Add this helper method to get a random direction
    private Vector3 GetRandomDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        return Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
    }

    // Add this method to check for obstacles directly ahead
    private bool IsObstacleAhead()
    {
        RaycastHit hit;
        Vector3 rayDirection = transform.forward;
        
        // Cast a ray directly forward
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, rayDirection, out hit, obstacleDetectionRange, obstacleLayer))
        {
            return true;
        }
        
        return false;
    }

    // Add these methods to handle border collisions
    private void OnTriggerExit(Collider other)
    {
        // Check if the collider is in the MapBorder layer
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            // Calculate direction from center of border to enemy
            Vector3 directionToCenter = (other.bounds.center - transform.position).normalized;
            directionToCenter.y = 0; // Keep on same Y level
            
            // Get current forward direction
            Vector3 currentDirection = transform.forward;
            
            // Calculate reflection direction (bounce)
            Vector3 reflectionDirection = Vector3.Reflect(currentDirection, directionToCenter);
            
            // Move enemy back slightly inside boundary
            transform.position += directionToCenter * 0.2f;
            
            // Rotate to face new direction with a slight random variation
            float randomVariation = Random.Range(-30f, 30f);
            Vector3 newDirection = Quaternion.Euler(0, randomVariation, 0) * reflectionDirection;
            transform.rotation = Quaternion.LookRotation(newDirection);
            
            // Interrupt current movement and start a new straight line
            StopCurrentMovementAndRedirect(newDirection);
        }
    }

    // New method to handle interruption and redirection
    private void StopCurrentMovementAndRedirect(Vector3 newDirection)
    {
        // Stop all coroutines to interrupt current movement pattern
        StopAllCoroutines();
        
        // Set movement state
        isWaiting = false;
        isWandering = true;
        UpdateAnimationState(true);
        
        // Start a new short movement in the reflection direction
        StartCoroutine(BounceMovement(newDirection));
    }

    // Coroutine for brief movement after bouncing
    private IEnumerator BounceMovement(Vector3 direction)
    {
        // Store starting position for straight-line movement
        Vector3 startPosition = transform.position;
        Vector3 moveDirection = direction.normalized;
        
        // Use a shorter distance for the bounce movement
        float moveDistance = Random.Range(minWanderDistance * 0.5f, minWanderDistance);
        float moveDuration = moveDistance / livingEntity.moveSpeed;
        float moveTimer = 0;
        float distanceMoved = 0;
        
        // Move in straight line after bounce
        while (moveTimer < moveDuration && distanceMoved < moveDistance)
        {
            if (!isWandering)
                break;
            
            // Check for obstacles
            if (avoidObstacles && IsObstacleAhead())
                break;
            
            // Use the same movement logic as our main movement routine
            float stepDistance = livingEntity.moveSpeed * Time.deltaTime;
            
            if (livingEntity != null)
            {
                Quaternion previousRotation = transform.rotation;
                livingEntity.MoveInDirection(moveDirection, 1.0f);
                transform.rotation = previousRotation;
            }
            else
            {
                transform.position += moveDirection * stepDistance;
            }
            
            distanceMoved += Vector3.Distance(transform.position, startPosition + moveDirection * distanceMoved);
            moveTimer += Time.deltaTime;
            yield return null;
        }
        
        // After bouncing, resume normal wandering behavior
        StartCoroutine(WanderingRoutine());
    }

    // Add this method to force the wandering behavior to start in idle state
    public void ForceStartWithIdle()
    {
        // Stop all coroutines to interrupt any ongoing movement
        StopAllCoroutines();
        
        // Set state to waiting/idle
        isWaiting = true;
        isWandering = false;
        
        // Update animation state to idle
        UpdateAnimationState(false);
        
        // Restart the wandering routine to begin with the waiting phase
        StartCoroutine(WanderingRoutine());
    }
}