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

    [Header("Enhanced Border Avoidance")]
    [SerializeField] private bool useEnhancedBorderAvoidance = true;
    [SerializeField] private float safetyMargin = 5f; // Extra buffer distance from borders
    [SerializeField] private bool showDebugVisuals = true; // Show visual debugging

    [Header("Spawn Area Restriction")]
    [SerializeField] private bool restrictToSpawnArea = true;
    [SerializeField] private float maxWanderRadius = 2f; // Maximum distance from spawn point
    [SerializeField] private bool showSpawnAreaGizmo = true; // For debugging

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
    
    private Vector3 spawnPosition; // Store the original spawn position
    
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
        
        // Store the spawn position
        spawnPosition = transform.position;
        
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
            // Skip if entity is dead
            if (livingEntity != null && livingEntity.IsDead)
            {
                isWaiting = true;
                isWandering = false;
                UpdateAnimationState(false);
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // --- WAITING PHASE ---
            isWaiting = true;
            isWandering = false;
            UpdateAnimationState(false);
            
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // Skip if entity died during waiting
            if (livingEntity != null && livingEntity.IsDead)
                continue;
            
            // --- WAYPOINT SELECTION PHASE ---
            // Simple waypoint selection with border check
            Vector3 targetWaypoint = GetSafeWaypoint();
            Debug.Log($"{gameObject.name}: Selected waypoint at {targetWaypoint}");
            
            // --- TURNING PHASE ---
            Vector3 directionToWaypoint = (targetWaypoint - transform.position).normalized;
            float turnDuration = 0.5f;
            float turnTimer = 0;
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
            
            while (turnTimer < turnDuration)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer / turnDuration);
                turnTimer += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final rotation is exact
            transform.rotation = targetRotation;
            
            // --- MOVEMENT PHASE ---
            isWaiting = false;
            isWandering = true;
            UpdateAnimationState(true);
            
            float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint);
            float moveDuration = distanceToWaypoint / livingEntity.moveSpeed;
            float moveTimer = 0;
            
            while (moveTimer < moveDuration && isWandering)
            {
                // Check for borders ahead during movement - if detected, stop and get new waypoint
                if (IsBorderAhead(remainingDistance: Vector3.Distance(transform.position, targetWaypoint)))
                {
                    Debug.LogWarning($"{gameObject.name}: Border detected during movement - getting new waypoint");
                    break; // Exit movement loop and get new waypoint
                }
                
                // Standard movement
                float step = livingEntity.moveSpeed * Time.deltaTime;
                
                if (livingEntity != null)
                {
                    livingEntity.MoveInDirection(transform.forward, 1.0f);
                }
                else
                {
                    transform.position += transform.forward * step;
                }
                
                moveTimer += Time.deltaTime;
                
                // Check if we've reached the waypoint
                if (Vector3.Distance(transform.position, targetWaypoint) < waypointReachedDistance)
                    break;
                    
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

    // New helper method for checking borders ahead
    private bool IsBorderAhead(float remainingDistance)
    {
        // Center ray
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, 
                            remainingDistance + 2f, borderLayer))
            return true;
            
        // Left and right rays (30 degree spread)
        Vector3 leftDir = Quaternion.Euler(0, -30, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, 30, 0) * transform.forward;
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, leftDir, remainingDistance + 1f, borderLayer) ||
            Physics.Raycast(transform.position + Vector3.up * 0.5f, rightDir, remainingDistance + 1f, borderLayer))
            return true;
            
        return false;
    }

    // Modify GetSafeWaypoint to respect spawn area restriction
    private Vector3 GetSafeWaypoint()
    {
        // Try up to 8 random directions
        for (int attempt = 0; attempt < 8; attempt++)
        {
            // Generate random direction
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            
            // Generate random distance
            float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
            
            // Calculate potential waypoint
            Vector3 potentialWaypoint = transform.position + randomDirection * randomDistance;
            
            // If we're restricting to spawn area, check distance from spawn
            if (restrictToSpawnArea)
            {
                float distanceFromSpawn = Vector3.Distance(potentialWaypoint, spawnPosition);
                if (distanceFromSpawn > maxWanderRadius)
                {
                    // Point is outside spawn radius, adjust it
                    Vector3 directionFromSpawn = (potentialWaypoint - spawnPosition).normalized;
                    potentialWaypoint = spawnPosition + directionFromSpawn * maxWanderRadius * 0.9f;
                    
                    // Make sure we don't go below minimum distance
                    if (Vector3.Distance(transform.position, potentialWaypoint) < minWanderDistance * 0.5f)
                    {
                        // Skip this attempt if it's too close to current position
                        continue;
                    }
                }
            }
            
            // Check if waypoint is safe using BorderVisualizer
            if (IsWaypointSafe(potentialWaypoint))
            {
                // Visualize the selected waypoint
                Debug.DrawLine(transform.position, potentialWaypoint, Color.green, 3f);
                return potentialWaypoint;
            }
        }
        
        // Fallback: If no good waypoint found, try a short-distance move
        Vector3 shortDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        Vector3 shortWaypoint = transform.position + shortDirection * minWanderDistance * 0.5f;
        
        // If restricting to spawn area, make sure even the short waypoint is valid
        if (restrictToSpawnArea)
        {
            float distanceFromSpawn = Vector3.Distance(shortWaypoint, spawnPosition);
            if (distanceFromSpawn > maxWanderRadius)
            {
                // Just move slightly toward spawn point
                shortWaypoint = transform.position + (spawnPosition - transform.position).normalized * minWanderDistance * 0.5f;
            }
        }
        
        // Verify even the short waypoint
        if (IsWaypointSafe(shortWaypoint))
        {
            Debug.DrawLine(transform.position, shortWaypoint, Color.yellow, 3f);
            return shortWaypoint;
        }
        
        // Last resort fallback
        Debug.LogWarning($"{gameObject.name}: Could not find any safe waypoint, using minimal movement");
        return transform.position + Vector3.right * 0.5f; // Tiny default movement
    }

    // Helper to check if a waypoint is safe
    private bool IsWaypointSafe(Vector3 waypoint)
    {
        // First check - is the waypoint itself valid
        if (!BorderVisualizer.IsPositionSafe(waypoint))
            return false;
            
        // Second check - is the path to it valid
        Vector3 direction = (waypoint - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, waypoint);
        
        // Cast three rays in a spread to check the path
        RaycastHit hit;
        
        // Center ray
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, out hit, distance, borderLayer))
            return false;
            
        // Left and right rays at 15 degrees
        Vector3 leftDir = Quaternion.Euler(0, -15, 0) * direction;
        Vector3 rightDir = Quaternion.Euler(0, 15, 0) * direction;
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, leftDir, distance, borderLayer) ||
            Physics.Raycast(transform.position + Vector3.up * 0.5f, rightDir, distance, borderLayer))
            return false;
            
        // Also check surrounding area near the waypoint by casting rays in a circle
        for (int i = 0; i < 4; i++) // Check 4 directions around the waypoint
        {
            float angle = i * 90f;
            Vector3 checkDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            if (Physics.Raycast(waypoint, checkDir, 2f, borderLayer))
                return false; // Border too close to the waypoint
        }
        
        return true; // The waypoint and path to it are safe
    }

    // Modify GetSafeWaypointUsingVisualizer to respect spawn radius
    private Vector3 GetSafeWaypointUsingVisualizer(Vector3 direction, float distance)
    {
        // Check if BorderVisualizer has been added to any objects
        bool borderVisualizerExists = FindObjectOfType<BorderVisualizer>() != null;
        if (!borderVisualizerExists)
        {
            Debug.LogError("No BorderVisualizer component found in scene! Add it to your MapBorder objects.");
            return transform.position + direction.normalized * distance * 0.5f;
        }

        // Calculate raw target position
        Vector3 targetPosition = transform.position + direction.normalized * distance;
        
        // If we're restricting to spawn area, check distance from spawn
        if (restrictToSpawnArea)
        {
            float distanceFromSpawn = Vector3.Distance(targetPosition, spawnPosition);
            if (distanceFromSpawn > maxWanderRadius)
            {
                // Point is outside spawn radius, adjust it to stay within the circle
                Vector3 directionFromSpawn = (targetPosition - spawnPosition).normalized;
                targetPosition = spawnPosition + directionFromSpawn * maxWanderRadius * 0.9f;
            }
        }
        
        // Check if it's valid using the BorderVisualizer
        if (BorderVisualizer.IsWaypointValid(transform.position, targetPosition))
        {
            return targetPosition;
        }
        else
        {
            // Get closest safe position
            Vector3 safePosition = BorderVisualizer.GetClosestSafePosition(targetPosition, transform.position);
            
            // If restricting to spawn area, ensure the safe position is within radius
            if (restrictToSpawnArea)
            {
                float distanceFromSpawn = Vector3.Distance(safePosition, spawnPosition);
                if (distanceFromSpawn > maxWanderRadius)
                {
                    // Clamp to spawn radius
                    Vector3 directionFromSpawn = (safePosition - spawnPosition).normalized;
                    safePosition = spawnPosition + directionFromSpawn * maxWanderRadius * 0.9f;
                }
            }
            
            // Ensure we get SOME movement - never return current position
            if (Vector3.Distance(safePosition, transform.position) < 0.5f)
            {
                // If safe position is too close to current position, move a short distance in random direction
                Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                return transform.position + randomDir * distance * 0.3f;
            }
            
            return safePosition;
        }
    }

    // Force stop all movement and wandering
    public void ForceStop()
    {
        // Stop all coroutines
        StopAllCoroutines();
        
        // Disable wandering
        isWandering = false;
        isWaiting = true;
        
        // Update animation to idle
        UpdateAnimationState(false);
        
        // Stop physical movement if there's a Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }
        
        Debug.Log($"{gameObject.name}: Forced to stop wandering");
    }

    // Force restart the wandering behavior
    public void ForceRestartWandering()
    {
        // Stop any current routines
        StopAllCoroutines();
        
        // Enable wandering
        isWaiting = false;
        isWandering = true;
        
        // Update animation
        UpdateAnimationState(true);
        
        // Restart the wandering routine
        StartCoroutine(WanderingRoutine());
        
        Debug.Log($"{gameObject.name}: Forced to restart wandering");
    }

    // Add a method to force a specific waypoint
    public void ForceNewWaypointInDirection(Vector3 direction)
    {
        // Stop current movement
        StopAllCoroutines();
        
        // Get a safe distance
        float distance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Calculate waypoint using existing safety methods
        Vector3 targetWaypoint = GetSafeWaypointUsingVisualizer(direction, distance);
        
        // Log for debugging
        Debug.Log($"{gameObject.name}: Forced new waypoint in direction {direction}");
        
        // Start moving to this waypoint
        StartCoroutine(MoveToWaypointRoutine(targetWaypoint));
    }

    // Helper routine to move to a specific waypoint
    private IEnumerator MoveToWaypointRoutine(Vector3 waypoint)
    {
        // Turn toward waypoint
        Vector3 directionToWaypoint = (waypoint - transform.position).normalized;
        float turnDuration = 0.3f;
        float turnTimer = 0;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
        
        while (turnTimer < turnDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer / turnDuration);
            turnTimer += Time.deltaTime;
            yield return null;
        }
        
        // Set to exact rotation
        transform.rotation = targetRotation;
        
        // Enter moving state
        isWaiting = false;
        isWandering = true;
        UpdateAnimationState(true);
        
        // Movement logic
        float distanceToWaypoint = Vector3.Distance(transform.position, waypoint);
        float moveDuration = distanceToWaypoint / livingEntity.moveSpeed;
        float moveTimer = 0;
        
        while (moveTimer < moveDuration && isWandering)
        {
            // Border check
            if (IsBorderAhead(Vector3.Distance(transform.position, waypoint)))
            {
                Debug.LogWarning($"{gameObject.name}: Border detected during directed movement - stopping");
                break;
            }
            
            // Move step
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(transform.forward, 1.0f);
            }
            else
            {
                float step = livingEntity.moveSpeed * Time.deltaTime;
                transform.position += transform.forward * step;
            }
            
            moveTimer += Time.deltaTime;
            
            // Check if arrived
            if (Vector3.Distance(transform.position, waypoint) < waypointReachedDistance)
                break;
                
            yield return null;
        }
        
        // Return to normal wandering
        StartCoroutine(WanderingRoutine());
    }

    // Modify the ForceStartWithCurrentRotation method to accept a boolean parameter
    public void ForceStartWithCurrentRotation(bool useTargetWaypoint)
    {
        // Stop any current routines
        StopAllCoroutines();
        
        // Enable wandering but start with the current rotation
        isWaiting = false;
        isWandering = true;
        
        // Update animation
        UpdateAnimationState(true);
        
        // Start a modified wandering routine that keeps current rotation
        StartCoroutine(WanderingRoutineWithCurrentRotation(useTargetWaypoint));
        
        Debug.Log($"{gameObject.name}: Forced to start wandering with current rotation, useTargetWaypoint: {useTargetWaypoint}");
    }

    // Update the corresponding routine to accept the parameter
    private IEnumerator WanderingRoutineWithCurrentRotation(bool useTargetWaypoint)
    {
        Vector3 initialWaypoint;
        
        // Choose waypoint based on the parameter
        if (useTargetWaypoint && currentWaypoint != Vector3.zero)
        {
            // Use the existing waypoint if specified and valid
            initialWaypoint = currentWaypoint;
            Debug.Log($"{gameObject.name}: Using existing waypoint at {initialWaypoint}");
        }
        else
        {
            // Generate a new waypoint in the direction we're currently facing
            Vector3 initialDirection = transform.forward;
            float initialDistance = Random.Range(minWanderDistance, maxWanderDistance);
            initialWaypoint = GetSafeWaypointUsingVisualizer(initialDirection, initialDistance);
            Debug.Log($"{gameObject.name}: Generated new waypoint at {initialWaypoint}");
        }
        
        // Rest of method remains the same
        // ...existing movement code...
        
        // Start moving immediately (no turning phase)
        isWaiting = false;
        isWandering = true;
        UpdateAnimationState(true);
        
        // Movement to first waypoint
        float distanceToWaypoint = Vector3.Distance(transform.position, initialWaypoint);
        float moveDuration = distanceToWaypoint / livingEntity.moveSpeed;
        float moveTimer = 0;
        
        while (moveTimer < moveDuration && isWandering)
        {
            // Border check
            if (IsBorderAhead(Vector3.Distance(transform.position, initialWaypoint)))
            {
                break;
            }
            
            // Move step
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(transform.forward, 1.0f);
            }
            else
            {
                float step = livingEntity.moveSpeed * Time.deltaTime;
                transform.position += transform.forward * step;
            }
            
            moveTimer += Time.deltaTime;
            
            // Check if arrived
            if (Vector3.Distance(transform.position, initialWaypoint) < waypointReachedDistance)
                break;
                
            yield return null;
        }
        
        // After initial movement, return to normal wandering
        StartCoroutine(WanderingRoutine());
    }

    // Force start wandering but begin with idle state
    public void ForceStartWithIdle()
    {
        // Stop any current routines
        StopAllCoroutines();
        
        // Start in idle state
        isWaiting = true;
        isWandering = false;
        
        // Update animation
        UpdateAnimationState(false);
        
        // Wait briefly then start normal wandering
        StartCoroutine(IdleThenWanderRoutine());
        
        Debug.Log($"{gameObject.name}: Forced to start wandering with idle first");
    }

    // Helper routine to start with idle then transition to wandering
    private IEnumerator IdleThenWanderRoutine()
    {
        // Wait in idle state
        float idleTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(idleTime);
        
        // Then start normal wandering
        StartCoroutine(WanderingRoutine());
    }

    // Add this new method to the AIWandering class
    public void ForceStartWithDirection(Vector3 direction)
    {
        // Stop any current routines
        StopAllCoroutines();
        
        // Enable wandering
        isWaiting = false;
        isWandering = true;
        
        // Update animation
        UpdateAnimationState(true);
        
        // Get a safe distance
        float distance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Calculate waypoint using existing safety methods
        Vector3 targetWaypoint = GetSafeWaypointUsingVisualizer(direction, distance);
        
        // Log for debugging
        Debug.Log($"{gameObject.name}: Forced new direction {direction}");
        
        // Start moving to this waypoint
        StartCoroutine(MoveToWaypointRoutine(targetWaypoint));
    }

    // Add OnDrawGizmosSelected to visualize the spawn radius
    private void OnDrawGizmosSelected()
    {
        if (showSpawnAreaGizmo && restrictToSpawnArea)
        {
            // Draw the spawn area in yellow
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Transparent yellow
            
            // In editor, use current position; in play mode, use stored spawn position
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            
            // Draw a disc for the spawn area
            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.3f);
            UnityEditor.Handles.DrawSolidDisc(center, Vector3.up, maxWanderRadius);
            
            // Draw the outline
            Gizmos.color = Color.yellow;
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(center, Vector3.up, maxWanderRadius);
        }
    }
}