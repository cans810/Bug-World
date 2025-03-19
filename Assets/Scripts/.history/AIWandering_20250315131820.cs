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

    [Header("Enhanced Border and Nest Avoidance")]
    [SerializeField] private float nestSafetyMargin = 3f; // Extra buffer distance from player nest
    [SerializeField] private float borderSafetyMargin = 3f; // Extra buffer distance from map borders
    [SerializeField] private bool showAvoidanceDebug = true; // Show visual debugging for avoidance

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
    
    // Internal variables for border detection
    private Transform mapBorderTransform;
    private float mapBorderRadius = 0f;
    
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
        
        // Find the map border
        FindMapBorder();
        
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
            // Get a waypoint that respects both nest and border boundaries
            Vector3 targetWaypoint = GetSafeWaypoint();
            
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
                // Check for borders or nest ahead during movement - if detected, stop and get new waypoint
                if (IsBorderAhead(remainingDistance: Vector3.Distance(transform.position, targetWaypoint)))
                {
                    if (showAvoidanceDebug)
                    {
                        Debug.LogWarning($"{gameObject.name}: Border or nest detected during movement - getting new waypoint");
                    }
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

    private void FindMapBorder()
    {
        // Find the map border by layer instead of tag
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == LayerMask.NameToLayer("MapBorder"))
            {
                mapBorderTransform = obj.transform;
                
                // Get the sphere collider to determine radius
                SphereCollider borderCollider = obj.GetComponent<SphereCollider>();
                if (borderCollider != null)
                {
                    mapBorderRadius = borderCollider.radius * Mathf.Max(
                        obj.transform.lossyScale.x,
                        obj.transform.lossyScale.y,
                        obj.transform.lossyScale.z
                    );
                    
                    Debug.Log($"{gameObject.name}: Found map border with radius: {mapBorderRadius}");
                    break;
                }
            }
        }
        
        if (mapBorderTransform == null)
        {
            Debug.LogWarning("No map border objects found with MapBorder layer");
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
        // Check for map border edge ahead
        if (mapBorderTransform != null)
        {
            // Calculate the position we would end up at
            Vector3 potentialPosition = transform.position + transform.forward * remainingDistance;
            
            // Calculate distance from that position to border center
            float distanceToBorderCenter = Vector3.Distance(potentialPosition, mapBorderTransform.position);
            
            // If that position would be too close to the edge, return true
            if (distanceToBorderCenter > (mapBorderRadius - borderSafetyMargin))
            {
                if (showAvoidanceDebug)
                {
                    Debug.DrawLine(transform.position, potentialPosition, Color.red, 0.5f);
                    Debug.Log($"Border edge ahead: {distanceToBorderCenter} vs {mapBorderRadius - borderSafetyMargin}");
                }
                return true;
            }
        }
        
        // For non-allies, also check for nest ahead
        if (!IsAlly() && avoidPlayerNest && playerNestTransform != null)
        {
            Vector3 directionToNest = playerNestTransform.position - transform.position;
            float distanceToNest = directionToNest.magnitude;
            
            // Check if nest is ahead and too close
            float dotProduct = Vector3.Dot(transform.forward.normalized, directionToNest.normalized);
            if (dotProduct > 0.7f && distanceToNest < (nestRadius + nestAvoidanceDistance + remainingDistance))
                return true;
        }
        
        return false;
    }

    // Modify the GetSafeWaypoint method to consider both nest and border
    private Vector3 GetSafeWaypoint()
    {
        // Try up to 10 random directions
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // Generate random direction
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            
            // Generate random distance
            float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
            
            // Calculate potential waypoint
            Vector3 potentialWaypoint = transform.position + randomDirection * randomDistance;
            
            // Check if waypoint is safe from both nest and border
            if (IsWaypointSafeFromBorders(potentialWaypoint) && IsWaypointSafeFromNest(potentialWaypoint))
            {
                // Visualize the selected waypoint
                if (showAvoidanceDebug)
                {
                    Debug.DrawLine(transform.position, potentialWaypoint, Color.green, 3f);
                }
                return potentialWaypoint;
            }
        }
        
        // Fallback: If no good waypoint found, try a short-distance move away from borders
        Vector3 safeDirection = GetSafeDirection();
        Vector3 shortWaypoint = transform.position + safeDirection * minWanderDistance * 0.5f;
        
        if (showAvoidanceDebug)
        {
            Debug.DrawLine(transform.position, shortWaypoint, Color.yellow, 3f);
        }
        return shortWaypoint;
    }

    // Modify IsWaypointSafeFromBorders to keep entities away from the edge
    private bool IsWaypointSafeFromBorders(Vector3 waypoint)
    {
        if (mapBorderTransform == null)
            return true; // No border to check against
        
        // Calculate distance from waypoint to border center
        float distanceToBorderCenter = Vector3.Distance(waypoint, mapBorderTransform.position);
        
        // For a sphere border, we want to stay away from the edge by borderSafetyMargin
        // This means staying INSIDE the border by at least borderSafetyMargin
        float safeDistance = mapBorderRadius - borderSafetyMargin;
        
        // The waypoint is safe if it's LESS than the safe distance from center
        // This keeps entities INSIDE the border with a safety margin
        bool isSafe = distanceToBorderCenter < safeDistance;
        
        if (showAvoidanceDebug && !isSafe)
        {
            Debug.DrawLine(waypoint, mapBorderTransform.position, Color.red, 0.5f);
            Debug.Log($"Waypoint at distance {distanceToBorderCenter} from border center, safe distance is {safeDistance}");
        }
        
        return isSafe;
    }

    // New method to check if waypoint is safe from player nest
    private bool IsWaypointSafeFromNest(Vector3 waypoint)
    {
        // Skip nest avoidance for allies
        if (IsAlly())
            return true;
        
        if (!avoidPlayerNest || playerNestTransform == null)
            return true; // No nest to avoid
        
        float distanceToNest = Vector3.Distance(waypoint, playerNestTransform.position);
        bool isSafe = distanceToNest > (nestRadius + nestAvoidanceDistance + nestSafetyMargin);
        
        if (showAvoidanceDebug && !isSafe)
        {
            Debug.DrawLine(waypoint, playerNestTransform.position, Color.magenta, 0.5f);
        }
        
        return isSafe;
    }

    // Update GetSafeDirection to correctly handle border edge avoidance
    private Vector3 GetSafeDirection()
    {
        Vector3 directionFromBorder = Vector3.zero;
        Vector3 directionFromNest = Vector3.zero;
        
        // Get direction from border edge (if applicable)
        if (mapBorderTransform != null)
        {
            // Calculate direction from current position to border center
            Vector3 directionToBorderCenter = (mapBorderTransform.position - transform.position).normalized;
            
            // Calculate distance to border center
            float distanceToBorderCenter = Vector3.Distance(transform.position, mapBorderTransform.position);
            
            // If we're too close to the edge, move toward center
            if (distanceToBorderCenter > (mapBorderRadius - borderSafetyMargin - 1f))
            {
                directionFromBorder = directionToBorderCenter; // Move TOWARD center when near edge
                
                if (showAvoidanceDebug)
                {
                    Debug.DrawRay(transform.position, directionFromBorder * 3f, Color.red, 1f);
                    Debug.Log($"Too close to border edge: {distanceToBorderCenter} vs {mapBorderRadius - borderSafetyMargin}");
                }
            }
        }
        
        // Get direction from nest (if applicable and not an ally)
        if (!IsAlly() && avoidPlayerNest && playerNestTransform != null)
        {
            directionFromNest = (transform.position - playerNestTransform.position).normalized;
        }
        
        // Combine directions (if both exist)
        Vector3 safeDirection;
        if (directionFromBorder != Vector3.zero && directionFromNest != Vector3.zero)
        {
            safeDirection = (directionFromBorder + directionFromNest).normalized;
        }
        else if (directionFromBorder != Vector3.zero)
        {
            safeDirection = directionFromBorder;
        }
        else if (directionFromNest != Vector3.zero)
        {
            safeDirection = directionFromNest;
        }
        else
        {
            // Fallback to random direction if neither border nor nest is relevant
            safeDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        }
        
        if (showAvoidanceDebug)
        {
            Debug.DrawRay(transform.position, safeDirection * 3f, Color.blue, 1f);
        }
        
        return safeDirection;
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

    // Add this method to AIWandering class
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
        
        // Check if it's valid using the BorderVisualizer
        if (BorderVisualizer.IsWaypointValid(transform.position, targetPosition))
        {
            return targetPosition;
        }
        else
        {
            // Get closest safe position
            Vector3 safePosition = BorderVisualizer.GetClosestSafePosition(targetPosition, transform.position);
            
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
}