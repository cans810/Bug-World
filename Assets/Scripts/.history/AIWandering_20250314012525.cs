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

    [Header("Straight Line Wandering")]
    [SerializeField] private float minStraightLineDistance = 3f;
    [SerializeField] private float maxStraightLineDistance = 8f;
    [SerializeField] private float rotationDuration = 1.0f;

    // Internal variables
    private Vector3 currentWaypoint;
    private bool isWandering = false;
    private bool isWaiting = false;
    private Animator animator;
    private MapBoundary mapBoundary;
    private AllyAI allyAI;
    
    private Transform playerNestTransform;
    private float nestRadius = 0f;
    
    private bool isHittingBoundary = false;
    private float boundaryRedirectionTime = 0f;
    private const float BOUNDARY_REDIRECT_DURATION = 1.0f;
    
    private float currentStraightLineDistance = 0f;
    private float distanceTraveled = 0f;
    private bool isRotating = false;
    
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
            // Start with waiting
            isWaiting = true;
            isRotating = false;
            UpdateAnimationState(false);
            
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // Choose a random distance to travel in a straight line
            currentStraightLineDistance = Random.Range(minStraightLineDistance, maxStraightLineDistance);
            distanceTraveled = 0f;
            
            // Rotate to a random direction
            isRotating = true;
            isWaiting = false;
            isWandering = false;
            UpdateAnimationState(false);
            
            Vector3 randomDirection = GetRandomDirection();
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(randomDirection);
            
            // Perform rotation over time
            float rotationTimer = 0f;
            while (rotationTimer < rotationDuration)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, rotationTimer / rotationDuration);
                rotationTimer += Time.deltaTime;
                yield return null;
            }
            
            // Finish rotation
            transform.rotation = targetRotation;
            isRotating = false;
            
            // Start moving in a straight line
            isWandering = true;
            isWaiting = false;
            UpdateAnimationState(true);
            
            // Move until we've traveled the desired distance or hit an obstacle/boundary
            while (distanceTraveled < currentStraightLineDistance)
            {
                if (avoidObstacles)
                {
                    // Check for obstacles
                    if (IsObstacleAhead())
                    {
                        break; // Stop and choose a new direction
                    }
                }
                
                // Move forward in current direction
                float distanceThisFrame = livingEntity.moveSpeed * Time.deltaTime;
                Vector3 movement = transform.forward * distanceThisFrame;
                
                // Calculate the next position
                Vector3 nextPosition = transform.position + movement;
                
                // Check boundary and nest proximity
                bool isTooCloseToNest = IsPositionTooCloseToNest(nextPosition);
                bool isOutsideBoundary = mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition);
                
                if (isOutsideBoundary || isTooCloseToNest)
                {
                    break; // Stop and choose a new direction
                }
                
                // Move forward
                transform.position += movement;
                distanceTraveled += distanceThisFrame;
                
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

    // Modify MoveWithBoundaryCheck to create a bounce effect
    private void MoveWithBoundaryCheck(Vector3 direction)
    {
        // Calculate movement based on frame time
        Vector3 movement = direction * livingEntity.moveSpeed * Time.deltaTime;
        
        // Calculate the next position
        Vector3 nextPosition = transform.position + movement;
        
        // Check only for map boundary and nest proximity
        bool isTooCloseToNest = IsPositionTooCloseToNest(nextPosition);
        bool isOutsideBoundary = mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition);
        
        if (isOutsideBoundary)
        {
            // Calculate normal vector (from center to entity)
            Vector3 normal = (transform.position - mapBoundary.transform.position).normalized;
            
            // Calculate reflection vector (bounce direction)
            Vector3 reflectionDirection = Vector3.Reflect(direction, normal);
            
            // Add slight random rotation to make it more natural
            reflectionDirection = Quaternion.Euler(0, Random.Range(-20f, 20f), 0) * reflectionDirection;
            
            // Apply new direction
            transform.forward = reflectionDirection;
            
            // Set a new waypoint in the reflection direction
            float newDistance = Random.Range(minWanderDistance, maxWanderDistance);
            currentWaypoint = transform.position + reflectionDirection * newDistance;
            
            // Make sure new waypoint is within bounds
            if (mapBoundary != null && !mapBoundary.IsWithinBounds(currentWaypoint))
            {
                // If waypoint is still outside, move it closer
                currentWaypoint = mapBoundary.GetNearestPointInBounds(currentWaypoint);
            }
            
            // Move in the new reflection direction immediately
            transform.position += reflectionDirection * livingEntity.moveSpeed * Time.deltaTime;
        }
        else if (isTooCloseToNest && !IsAlly())
        {
            // We've hit the nest boundary, bounce away
            Vector3 awayFromNest = GetDirectionAwayFromNest(transform.position);
            transform.forward = awayFromNest;
            SetWaypointInDirection(awayFromNest);
            
            // Move immediately
            transform.position += awayFromNest * livingEntity.moveSpeed * Time.deltaTime;
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
            // For enemies, check both map boundary and nest proximity
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

    private Vector3 GetRandomDirection()
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(
            Mathf.Cos(randomAngle),
            0f, // keep on the same Y level
            Mathf.Sin(randomAngle)
        );
        
        return direction;
    }

    private bool IsObstacleAhead()
    {
        RaycastHit hit;
        Vector3 rayDirection = transform.forward;
        
        // Cast a ray forward to detect obstacles
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, rayDirection, out hit, obstacleDetectionRange, obstacleLayer))
        {
            return true;
        }
        
        return false;
    }
}