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
    private MapBoundary mapBoundary;
    private AllyAI allyAI;
    
    private Transform playerNestTransform;
    private float nestRadius = 0f;
    
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
        
        mapBoundary = FindObjectOfType<MapBoundary>();
        
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
        
        if (isWandering && !isWaiting)
        {
            MoveTowardsWaypoint();
        }
    }
    
    private IEnumerator WanderingRoutine()
    {
        while (true)
        {
            // loop starts by waiting at current position
            isWaiting = true;
            UpdateAnimationState(false); // while waiting, the enemy will play the idle animation
            
            float waitTime = Random.Range(minWaitTime, maxWaitTime); // get a random wait time, this is the time the enemy will wait at the current position
            yield return new WaitForSeconds(waitTime);
            
            // after waiting, the enemy will start wandering to a new position
            isWaiting = false;
            isWandering = true;
            UpdateAnimationState(true); // while wandering, the enemy will play the walk animation
            
            // now time to set a random waypoint for the enemy to wander to
            SetRandomWaypoint();
            
            // after the random waypoint is set, the enemy will move towards the waypoint
            while (Vector3.Distance(transform.position, currentWaypoint) > waypointReachedDistance) // if enemy is close enough to the waypoint, it will stop wandering
            {
                // im not sure if this works correctly, check it later
                if (avoidObstacles)
                {
                    AvoidObstacles();
                }
                
                // TODO: I don't know how this works, check it later
                yield return new WaitForSeconds(pathfindingUpdateInterval);
                
                // if the enemy is not wandering, stop the wandering routine
                if (!isWandering)
                {
                    break;
                }
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
        // Calculate movement based on frame time
        Vector3 movement = direction * livingEntity.moveSpeed * Time.deltaTime;
        
        // Calculate the next position
        Vector3 nextPosition = transform.position + movement;
        
        // For allies, only check map boundaries, not nest boundaries
        if (IsAlly())
        {
            bool isMapBoundaryViolation = mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition);
            
            if (isMapBoundaryViolation)
            {
                // We've hit a boundary, turn around and go the opposite way
                Vector3 oppositeDirection = -transform.forward;
                
                // Turn to face the opposite direction
                transform.forward = oppositeDirection;
                
                // Set a new waypoint in the direction we're now facing
                SetWaypointInDirection(oppositeDirection);
            }
            else
            {
                // Move normally if within bounds
                transform.position += movement;
            }
            return;
        }
        
        // For non-allies (enemies), use the full avoidance logic
        bool isTooCloseToNest = IsPositionTooCloseToNest(nextPosition);
        bool isOutsideBoundary = mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition);
        
        if (isTooCloseToNest)
        {
            // We're getting too close to the nest, turn around and go the opposite way
            Vector3 awayFromNest = GetDirectionAwayFromNest(transform.position);
            
            // Turn to face away from the nest
            transform.forward = awayFromNest;
            
            // Move away from the nest
            transform.position += awayFromNest * livingEntity.moveSpeed * Time.deltaTime;
            
            // Set a new waypoint in the direction we're now facing
            SetWaypointInDirection(awayFromNest);
        }
        else if (isOutsideBoundary)
        {
            // We've hit a boundary, turn around and go the opposite way
            Vector3 oppositeDirection = -transform.forward;
            
            // Turn to face the opposite direction
            transform.forward = oppositeDirection;
            
            // Set a new waypoint in the direction we're now facing
            SetWaypointInDirection(oppositeDirection);
        }
        else
        {
            // Move normally if within bounds and not too close to nest
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
            
            // Check if waypoint is within map boundaries
            bool withinBounds = mapBoundary == null || mapBoundary.IsWithinBounds(randomWaypoint);
            
            // For allies, skip nest proximity check
            if (IsAlly() && withinBounds)
            {
                validWaypointFound = true;
            }
            // For enemies, check both boundaries and nest proximity
            else if (!IsAlly())
            {
                bool notTooCloseToNest = !IsPositionTooCloseToNest(randomWaypoint);
                if (withinBounds && notTooCloseToNest)
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
            // Use the centralized rotation method in LivingEntity
            if (livingEntity != null)
            {
                livingEntity.RotateTowards(direction);
            }
            else
            {
                // Fallback direct rotation if living entity is missing
                float rotationSpeed = 4f * Time.deltaTime;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * 60f);
            }
            
            // Calculate movement with proper time delta
            Vector3 movement = transform.forward * livingEntity.moveSpeed * Time.deltaTime;
            
            // Use Rigidbody for movement if available
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                // Use velocity-based movement for smoother motion
                Vector3 targetVelocity = transform.forward * livingEntity.moveSpeed;
                rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
            }
            else
            {
                // Fallback to direct position update
                transform.position += movement;
            }
        }
    }
    
    // Modify SetWaypointInDirection to consider ally status
    private void SetWaypointInDirection(Vector3 direction)
    {
        // Set a waypoint at a random distance in the given direction
        float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
        Vector3 newWaypoint = transform.position + direction * randomDistance;
        
        // Make sure it's within bounds
        if (mapBoundary != null && !mapBoundary.IsWithinBounds(newWaypoint))
        {
            newWaypoint = mapBoundary.GetNearestPointInBounds(newWaypoint);
        }
        
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
                
                // Final check to ensure it's within bounds
                if (mapBoundary != null && !mapBoundary.IsWithinBounds(newWaypoint))
                {
                    newWaypoint = mapBoundary.GetNearestPointInBounds(newWaypoint);
                }
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
                
                // Make sure the new waypoint is within bounds
                if (mapBoundary != null && !mapBoundary.IsWithinBounds(currentWaypoint))
                {
                    currentWaypoint = mapBoundary.GetNearestPointInBounds(currentWaypoint);
                }
                
                break;
            }
        }
    }

    // this is the animation controller for the wandering AI, it determines if the enemy anim will play the walk or idle animation
    private void UpdateAnimationState(bool isWalking)
    {
        // Use AnimationController if available
        if (animationController != null)
        {
            // Only update if the state is changing
            if (animationController.IsAnimationPlaying("walk") != isWalking)
            {
                animationController.SetWalking(isWalking);
            }
        }
        // Fall back to direct Animator control if no AnimationController is available
        else if (animator != null)
        {
            // Only update if the state is changing
            if (animator.GetBool("Walk") != isWalking)
            {
                animator.SetBool("Walk", isWalking);
                animator.SetBool("Idle", !isWalking);
            }
        }
    }
    
    // Optional: Public method to control the AI from other scripts
    public void SetWanderingEnabled(bool enabled)
    {
        isWandering = enabled;
        UpdateAnimationState(enabled && !isWaiting);
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
}