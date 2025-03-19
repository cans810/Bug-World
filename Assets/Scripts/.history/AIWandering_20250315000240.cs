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

    [Header("Border Avoidance")]
    [SerializeField] private bool avoidBorders = true;
    [SerializeField] private float borderDetectionDistance = 5f;
    [SerializeField] private LayerMask borderLayer;

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
        
        // Set border layer if not set in inspector
        if (borderLayer == 0)
        {
            borderLayer = 1 << LayerMask.NameToLayer("MapBorder");
        }
        
        StartCoroutine(WanderingRoutine());
    }
    
    private void Update()
    {
        // Skip if entity is dead
        if (livingEntity != null && livingEntity.IsDead)
        {
            // Ensure we're not showing a walking animation when dead
            if (isWandering)
            {
                isWandering = false;
                UpdateAnimationState(false);
            }
            return;
        }
        
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
            // Check if entity is dead and skip movement if so
            if (livingEntity != null && livingEntity.IsDead)
            {
                // Stop moving if dead
                isWaiting = true;
                isWandering = false;
                UpdateAnimationState(false);
                
                // Just yield for a short time and check again
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 1. STOP AND WAIT
            isWaiting = true;
            isWandering = false;
            UpdateAnimationState(false);
            
            // Wait for a random time
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // Check again if entity died during waiting
            if (livingEntity != null && livingEntity.IsDead)
                continue;
            
            // CRITICAL: First check if we're already too close to a border
            bool nearBorder = false;
            Vector3 safeDirection = Vector3.zero;
            
            // Check in all 8 cardinal directions for nearby borders
            for (int i = 0; i < 8; i++)
            {
                Vector3 checkDir = Quaternion.Euler(0, i * 45, 0) * Vector3.forward;
                if (IsBorderInDirection(checkDir, borderDetectionDistance * 0.5f)) // Check at half distance for "too close"
                {
                    nearBorder = true;
                    
                    // The safe direction is opposite to where the border was detected
                    safeDirection -= checkDir;
                }
            }
            
            Vector3 randomDirection;
            
            // If already near borders, prioritize moving away
            if (nearBorder && safeDirection.magnitude > 0.1f)
            {
                safeDirection.Normalize();
                randomDirection = safeDirection;
                
                Debug.Log($"Entity {gameObject.name} is near border, moving away in direction: {safeDirection}");
            }
            else
            {
                // Normal direction selection with improved border checks
                randomDirection = GetRandomDirection();
                
                // Make sure the direction doesn't lead into the nest
                if (!IsAlly() && playerNestTransform != null && avoidPlayerNest) 
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
                
                // Double-check border direction even after GetRandomDirection
                if (avoidBorders && IsBorderInDirection(randomDirection, borderDetectionDistance))
                {
                    // Try the FindSafeDirection method which has multiple approaches
                    randomDirection = FindSafeDirection();
                    
                    // Log diagnostic info
                    Debug.Log($"Entity {gameObject.name} using FindSafeDirection, result: {randomDirection}");
                }
            }
            
            // 4. TURN TO FACE NEW DIRECTION
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
            
            // 5. MOVE IN STRAIGHT LINE with frequent border checks
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
            
            // Move with more frequent border checks
            while (moveTimer < moveDuration && distanceMoved < moveDistance)
            {
                if (!isWandering) // Handle interruptions
                    break;
                    
                // MORE FREQUENT BORDER CHECKS - check every frame while moving
                if (avoidBorders)
                {
                    // Use a shorter detection distance while moving for quicker reaction
                    float movingDetectionDistance = Mathf.Min(borderDetectionDistance, 
                                                   (moveDistance - distanceMoved) + 1f); // Add 1m for safety
                    
                    if (IsBorderInDirection(transform.forward, movingDetectionDistance))
                    {
                        // Border detected ahead - immediately break movement and start new wandering cycle
                        Debug.Log($"Entity {gameObject.name} detected border while walking, stopping movement");
                        break;
                    }
                }
                
                // Check for obstacles
                if (avoidObstacles && IsObstacleAhead())
                    break;
                
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
                    livingEntity.MoveInDirection(transform.forward, 1.0f);
                    
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
            // First rotate completely towards the waypoint
            if (livingEntity != null)
            {
                // Use a higher rotation speed for more responsive turning
                livingEntity.RotateTowards(direction, 3.0f);
                
                // Then move forward in the direction we're facing
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

    // COMPLETELY REVISED border detection system
    private bool IsBorderInDirection(Vector3 direction, float distance)
    {
        // Origin point slightly elevated to avoid ground issues
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        
        // Draw a highly visible debug ray
        Debug.DrawRay(rayOrigin, direction * distance, Color.red, 2.0f);
        
        // Cast a ray in the specified direction
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, direction, out hit, distance, borderLayer))
        {
            // Log the hit for debugging
            Debug.Log($"{gameObject.name}: Border detected at distance {hit.distance} in direction {direction}");
            return true;
        }
        
        return false;
    }

    // Create a specialized method to explicitly ensure waypoints are safe
    private bool IsWaypointSafe(Vector3 targetPoint)
    {
        // Direction from current position to waypoint
        Vector3 directionToWaypoint = (targetPoint - transform.position).normalized;
        float distanceToWaypoint = Vector3.Distance(transform.position, targetPoint);
        
        // Add a safety margin
        float safetyMargin = 2.0f;
        float checkDistance = distanceToWaypoint + safetyMargin;
        
        // Check direct line to waypoint
        if (IsBorderInDirection(directionToWaypoint, checkDistance))
        {
            Debug.LogWarning($"{gameObject.name}: Waypoint at {targetPoint} is unsafe - border detected");
            return false;
        }
        
        // Check a cone of directions around the waypoint to ensure the area is safe
        for (int i = 0; i < 4; i++)
        {
            Vector3 offsetDirection = Quaternion.Euler(0, i * 90, 0) * directionToWaypoint;
            if (IsBorderInDirection(offsetDirection, checkDistance))
            {
                Debug.LogWarning($"{gameObject.name}: Area around waypoint unsafe - border detected in direction {i*90}°");
                return false;
            }
        }
        
        return true;
    }

    // Completely revised waypoint generation method
    private Vector3 GetSafeWaypoint()
    {
        // Track number of attempts
        int maxAttempts = 20;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate a potential waypoint
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0;
            randomDirection.Normalize();
            
            // Choose distance based on attempt number - start with longer distances
            // and gradually try shorter ones if we can't find safe points
            float distance = Mathf.Lerp(maxWanderDistance, minWanderDistance, (float)attempt / maxAttempts);
            
            // Calculate potential waypoint
            Vector3 potentialWaypoint = transform.position + randomDirection * distance;
            
            // Check if this waypoint is safe
            if (IsWaypointSafe(potentialWaypoint) && 
                (!avoidPlayerNest || !IsPositionTooCloseToNest(potentialWaypoint)))
            {
                Debug.Log($"{gameObject.name}: Found safe waypoint at attempt {attempt}");
                return potentialWaypoint;
            }
        }
        
        // If all attempts fail, return a very safe fallback - move toward center but only a short distance
        Debug.LogError($"{gameObject.name}: Failed to find safe waypoint after {maxAttempts} attempts");
        
        // Move toward center of play area if we have a reference point
        if (playerNestTransform != null)
        {
            Vector3 directionToCenter = (playerNestTransform.position - transform.position).normalized;
            return transform.position + directionToCenter * minWanderDistance * 0.5f;
        }
        
        // Emergency fallback - stay in place
        return transform.position;
    }

    // Override ForceNewWaypointInDirection to ensure safety
    public void ForceNewWaypointInDirection(Vector3 direction)
    {
        // Get the desired distance
        float desiredDistance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Calculate potential waypoint
        Vector3 potentialWaypoint = transform.position + direction.normalized * desiredDistance;
        
        // Check if waypoint is safe
        if (!IsWaypointSafe(potentialWaypoint))
        {
            Debug.LogWarning($"{gameObject.name}: Forced waypoint unsafe, finding alternative");
            
            // Try shorter distances
            for (int i = 1; i <= 5; i++)
            {
                float reducedDistance = desiredDistance * (1f - (i * 0.15f));
                Vector3 shorterWaypoint = transform.position + direction.normalized * reducedDistance;
                
                if (IsWaypointSafe(shorterWaypoint))
                {
                    potentialWaypoint = shorterWaypoint;
                    Debug.Log($"{gameObject.name}: Found safer waypoint at {reducedDistance}m");
                    break;
                }
            }
        }
        
        // Also check nest avoidance
        if (avoidPlayerNest && !IsAlly() && IsPositionTooCloseToNest(potentialWaypoint))
        {
            Debug.LogWarning($"{gameObject.name}: Waypoint too close to nest, finding alternative");
            
            // Get direction away from nest
            Vector3 awayFromNest = GetDirectionAwayFromNest(transform.position);
            
            // Blend directions
            Vector3 blendedDirection = (direction.normalized + awayFromNest).normalized;
            potentialWaypoint = transform.position + blendedDirection * desiredDistance;
        }
        
        // Set the waypoint
        currentWaypoint = potentialWaypoint;
        
        // Update state
        isWaiting = false;
        isWandering = true;
        UpdateAnimationState(true);
        
        // Start movement
        StopAllCoroutines();
        StartCoroutine(MoveToForcedWaypoint(potentialWaypoint));
    }

    // New coroutine for forced waypoint movement with safety checks
    private IEnumerator MoveToForcedWaypoint(Vector3 waypoint)
    {
        // Get direction to waypoint
        Vector3 directionToWaypoint = (waypoint - transform.position).normalized;
        
        // Turn toward waypoint
        float turnDuration = 0.3f; // Faster turning for forced movement
        float turnTimer = 0;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
        
        while (turnTimer < turnDuration)
        {
            float turnProgress = turnTimer / turnDuration;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnProgress);
            turnTimer += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final rotation is exact
        transform.rotation = targetRotation;
        
        // Calculate movement parameters
        float distanceToWaypoint = Vector3.Distance(transform.position, waypoint);
        float moveDuration = distanceToWaypoint / livingEntity.moveSpeed;
        float moveTimer = 0;
        
        // Move with continual safety checks
        while (moveTimer < moveDuration && isWandering)
        {
            // Frequently check if path is still safe
            if (moveTimer % 0.5f < Time.deltaTime)
            {
                // Remaining distance
                float remainingDistance = Vector3.Distance(transform.position, waypoint);
                
                // Check if path ahead is still clear
                if (IsBorderInDirection(transform.forward, remainingDistance + 1f))
                {
                    Debug.LogWarning($"{gameObject.name}: Border detected during forced movement - stopping");
                    break;
                }
            }
            
            // Move step
            float step = livingEntity.moveSpeed * Time.deltaTime;
            
            // Update position
            if (livingEntity != null)
            {
                Quaternion previousRotation = transform.rotation;
                livingEntity.MoveInDirection(transform.forward, 1.0f);
                transform.rotation = previousRotation; // Maintain rotation
            }
            else
            {
                transform.position += transform.forward * step;
            }
            
            moveTimer += Time.deltaTime;
            
            // Check if we've arrived
            if (Vector3.Distance(transform.position, waypoint) < waypointReachedDistance)
            {
                break;
            }
            
            yield return null;
        }
        
        // Return to normal wandering
        StartCoroutine(WanderingRoutine());
    }

    // Add this method to AIWandering class
    public void ForceStartWithCurrentRotation(Quaternion currentRotation)
    {
        // Stop all coroutines to interrupt any ongoing movement
        StopAllCoroutines();
        
        // Set state to waiting/idle
        isWaiting = true;
        isWandering = false;
        
        // Update animation state to idle
        UpdateAnimationState(false);
        
        // Force the current rotation to be preserved
        transform.rotation = currentRotation;
        
        // Start a coroutine that will begin with waiting before picking a new direction
        // that maintains the current facing direction
        StartCoroutine(WaitThenWanderInCurrentDirection());
    }

    // New coroutine to wait then wander in the current forward direction
    private IEnumerator WaitThenWanderInCurrentDirection()
    {
        // Wait for a short moment
        yield return new WaitForSeconds(Random.Range(minWaitTime * 0.5f, minWaitTime));
        
        // Store the forward direction
        Vector3 currentForward = transform.forward;
        
        // Now pick a waypoint roughly in the current forward direction
        // with some minor variation
        Vector3 wanderDirection = Quaternion.Euler(0, Random.Range(-30f, 30f), 0) * currentForward;
        
        // Set the wander direction directly
        ForceNewWaypointInDirection(wanderDirection);
    }

    // Add this method to force immediate stopping of all wandering
    public void ForceStop()
    {
        // Stop all coroutines
        StopAllCoroutines();
        
        // Set state to not wandering and not waiting
        isWandering = false;
        isWaiting = false;
        
        // Update animation to idle
        UpdateAnimationState(false);
        
        // Zero out any potential physics movement
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // Add this method to force a complete restart of the wandering behavior
    public void ForceRestartWandering()
    {
        // Stop all coroutines to ensure a clean start
        StopAllCoroutines();
        
        // Reset state
        isWaiting = false;
        isWandering = true;
        
        // Update animation to walking
        UpdateAnimationState(true);
        
        // Start the wandering routine fresh
        StartCoroutine(WanderingRoutine());
    }

    // Improved FindSafeDirection method
    private Vector3 FindSafeDirection()
    {
        // First, get a potential safe direction by checking for the clearest path
        Vector3 clearestDirection = Vector3.zero;
        float longestClearDistance = 0f;
        
        // Check 12 directions (every 30 degrees) to find the direction with the most clearance
        for (int i = 0; i < 12; i++)
        {
            Vector3 checkDir = Quaternion.Euler(0, i * 30, 0) * Vector3.forward;
            RaycastHit hit;
            
            // Cast a ray and see how far we can go
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, checkDir, out hit, borderDetectionDistance * 2f, borderLayer))
            {
                // If this direction has more clearance than previous best, update
                if (hit.distance > longestClearDistance)
                {
                    longestClearDistance = hit.distance;
                    clearestDirection = checkDir;
                }
            }
            else
            {
                // No hit means clear path - this is a good direction
                return checkDir;
            }
        }
        
        // If we found a direction with decent clearance, use it
        if (longestClearDistance > borderDetectionDistance * 0.8f)
        {
            return clearestDirection;
        }
        
        // If no good direction found and we have a nest reference, use that for orientation
        if (playerNestTransform != null)
        {
            // Direction toward the center of the play area
            return (playerNestTransform.position - transform.position).normalized;
        }
        
        // Absolute last resort - pick a random direction
        return Quaternion.Euler(0, Random.Range(0, 360), 0) * Vector3.forward;
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

    // Border handling to stop at boundary with no bouncing
    private void OnTriggerExit(Collider other)
    {
        // Check if the collider is in the MapBorder layer
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            // Calculate the inward direction based on collider type
            Vector3 inwardDirection;
            
            if (other is SphereCollider sphereCollider)
            {
                // Get the center of the sphere in world space
                Vector3 sphereCenter = other.transform.TransformPoint(sphereCollider.center);
                // Calculate direction from entity to sphere center
                inwardDirection = (sphereCenter - transform.position).normalized;
            }
            else
            {
                // For non-sphere colliders, calculate inward direction
                Vector3 closestPoint = other.ClosestPoint(transform.position);
                inwardDirection = (closestPoint - transform.position).normalized;
            }
            
            // Ensure we're in the horizontal plane
            inwardDirection.y = 0;
            inwardDirection.Normalize();
            
            // Just enough push to get back inside
            transform.position += inwardDirection * 0.2f;
            
            // Immediately stop all movement
            StopAllCoroutines();
            
            // Set state to idle and stop wandering
            isWaiting = true;
            isWandering = false;
            
            // Update animation to idle
            UpdateAnimationState(false);
            
            // Don't force rotation or movement in any direction
            
            // After a delay, resume wandering naturally - no forced direction
            StartCoroutine(ResumeNormalWanderingAfterDelay());
        }
    }

    // New simplified method to resume wandering with no bouncing effect
    private IEnumerator ResumeNormalWanderingAfterDelay()
    {
        // Wait for a natural pause before resuming
        yield return new WaitForSeconds(2.0f);
        
        // Only resume if we're still in waiting state
        if (isWaiting)
        {
            // Simply restart the normal wandering routine with no forced direction
            isWaiting = false;
            isWandering = true;
            StartCoroutine(WanderingRoutine());
        }
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

    // Add this method to generate random directions
    private Vector3 GetRandomDirection()
    {
        // Generate basic random direction in the horizontal plane
        float randomAngle = Random.Range(0f, 360f);
        Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
        
        // If border avoidance is enabled, check for borders
        if (avoidBorders)
        {
            // Check if this direction leads to a border
            if (IsBorderInDirection(direction, borderDetectionDistance))
            {
                // Try to find another direction
                for (int i = 0; i < 5; i++) // Try up to 5 different angles
                {
                    // Try a new random angle
                    randomAngle = Random.Range(0f, 360f);
                    Vector3 newDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
                    
                    // If this direction is clear of borders, use it
                    if (!IsBorderInDirection(newDirection, borderDetectionDistance))
                    {
                        return newDirection;
                    }
                }
                
                // If all attempts failed, return direction toward center
                if (playerNestTransform != null)
                {
                    return (playerNestTransform.position - transform.position).normalized;
                }
            }
        }
        
        return direction;
    }
}