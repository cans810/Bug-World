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
        // Add debugging output
        if (isWandering && !isWaiting)
        {
            // Debug.Log($"AIWandering active on {gameObject.name}: Moving to {currentWaypoint}, distance: {Vector3.Distance(transform.position, currentWaypoint)}");
            
            // Check if AllyAI is blocking our movement
            if (allyAI != null && allyAI.HasAppliedMovementThisFrame)
            {
                // Debug.LogWarning("AllyAI has already moved this frame - skipping AIWandering movement");
                return;
            }
            
            // Skip if not visible to camera (optimization)
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null && !renderer.isVisible)
            {
                return;
            }
            
            // FORCE MOVEMENT - no conditions, no checks
            MoveTowardsWaypointDirect();
        }

        // Add this to the Update method right after the visibility check
        OnDrawGizmos();
    }
    
    private IEnumerator WanderingRoutine()
    {
        while (true)
        {
            // Start with waiting
            isWaiting = true;
            UpdateAnimationState(false);
            
            // Notify ally AI component if it exists
            NotifyAllyAIOfStateChange();
            
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // Start wandering
            isWaiting = false;
            isWandering = true;
            UpdateAnimationState(true);
            
            // Notify ally AI component if it exists
            NotifyAllyAIOfStateChange();
            
            SetRandomWaypoint();
            
            // Move until we reach the waypoint
            while (Vector3.Distance(transform.position, currentWaypoint) > waypointReachedDistance)
            {
                if (avoidObstacles)
                {
                    AvoidObstacles();
                }
                
                // No movement here - it happens in Update instead
                
                if (!isWandering)
                {
                    break;
                }
                
                // Use a very small wait time for more frequent updates (smoother movement)
                yield return new WaitForSeconds(0.05f); // Much shorter interval for smoother movement
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
        // Create a valid waypoint 100% of the time
        Vector3 randomWaypoint;
        
        // Start with a random direction
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
        
        Vector3 offset = new Vector3(
            Mathf.Cos(randomAngle) * randomDistance,
            0f, // keep on the same Y level
            Mathf.Sin(randomAngle) * randomDistance
        );
        
        randomWaypoint = transform.position + offset;
        
        // Ensure it's within bounds
        if (mapBoundary != null && !mapBoundary.IsWithinBounds(randomWaypoint))
        {
            randomWaypoint = mapBoundary.GetNearestPointInBounds(randomWaypoint);
        }
        
        // Ensure the waypoint is different from the current position
        if (Vector3.Distance(randomWaypoint, transform.position) < 1f)
        {
            // If too close, push it out a bit more
            randomWaypoint = transform.position + transform.forward * minWanderDistance * 1.5f;
        }
        
        // Debug log the new waypoint
        Debug.Log($"New waypoint set: {randomWaypoint}, distance: {Vector3.Distance(transform.position, randomWaypoint)}");
        
        currentWaypoint = randomWaypoint;
    }
    
    private void MoveTowardsWaypoint()
    {
        // First ensure we have a valid waypoint
        if (currentWaypoint == Vector3.zero)
        {
            SetRandomWaypoint();
            return;
        }
        
        // Calculate distance and direction to waypoint
        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
        
        // If we've reached the waypoint, set a new one
        if (distanceToWaypoint <= waypointReachedDistance)
        {
            SetRandomWaypoint();
            return;
        }
        
        // Calculate direction to the waypoint
        Vector3 direction = (currentWaypoint - transform.position);
        direction.y = 0; // Keep on the same Y level
        direction.Normalize();
        
        // Debug output for movement
        Debug.DrawLine(transform.position, currentWaypoint, Color.green);
        
        // First rotate toward the waypoint
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 4f * Time.deltaTime);
        
        // SUPER SIMPLE direct movement - guaranteed to work
        float moveSpeed = livingEntity != null ? livingEntity.moveSpeed : 2f;
        transform.position += direction * moveSpeed * Time.deltaTime;
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

    // Add this new direct movement method that bypasses all the complexity
    private void MoveTowardsWaypointDirect()
    {
        if (currentWaypoint == Vector3.zero)
        {
            SetRandomWaypoint();
        }
        
        // Calculate direction to target
        Vector3 direction = currentWaypoint - transform.position;
        direction.y = 0; // Keep on same Y level
        float distance = direction.magnitude;
        
        // If we've reached the waypoint, set a new one
        if (distance < waypointReachedDistance)
        {
            SetRandomWaypoint();
            return;
        }
        
        // Normalize direction
        direction.Normalize();
        
        // Rotate towards target
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            Time.deltaTime * 5f
        );
        
        // GUARANTEED MOVEMENT - apply directly to transform
        float speed = livingEntity != null ? livingEntity.moveSpeed : 2f;
        Vector3 movement = direction * speed * Time.deltaTime;
        
        // Apply movement DIRECTLY - no physics, no checks
        transform.position += movement;
        
        // Draw debug visualization
        Debug.DrawLine(transform.position, currentWaypoint, Color.green);
        
        // Log movement for debugging
        // Debug.Log($"Moving {gameObject.name} by {movement.magnitude} units toward waypoint");
    }

    // And modify the ForceNewWaypoint method to ensure movement begins immediately
    public void ForceNewWaypoint()
    {
        // Generate a completely new waypoint
        SetRandomWaypoint();
        
        // Force immediate movement state
        isWaiting = false;
        isWandering = true;
        
        // Force an animation update
        UpdateAnimationState(true);
        
        // Force a direct movement update immediately
        MoveTowardsWaypointDirect();
        
        // Log for debugging
        Debug.Log($"{gameObject.name} FORCED new waypoint at {currentWaypoint}");
    }

    // Add this new method to notify AllyAI of state changes
    private void NotifyAllyAIOfStateChange()
    {
        AllyAI allyAI = GetComponent<AllyAI>();
        if (allyAI != null)
        {
            allyAI.NotifyWanderingStateChange(isWandering, isWaiting);
        }
    }

    // Add this to the Update method right after the visibility check
    private void OnDrawGizmos()
    {
        // Draw the waypoint in the scene for debugging
        if (currentWaypoint != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentWaypoint, 0.3f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }
    }
}