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

    // New method to check if a position is too close to the nest
    private bool IsPositionTooCloseToNest(Vector3 position)
    {
        if (!avoidPlayerNest || playerNestTransform == null)
            return false;
        
        float distanceToNest = Vector3.Distance(position, playerNestTransform.position);
        return distanceToNest < (nestRadius + nestAvoidanceDistance);
    }

    // Modify SetRandomWaypoint to avoid the nest
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
            
            // Check if waypoint is within map boundaries and not too close to nest
            bool withinBounds = mapBoundary == null || mapBoundary.IsWithinBounds(randomWaypoint);
            bool notTooCloseToNest = !IsPositionTooCloseToNest(randomWaypoint);
            
            if (withinBounds && notTooCloseToNest)
            {
                validWaypointFound = true;
            }
            else if (withinBounds && !notTooCloseToNest)
            {
                // If only issue is nest proximity, try to find a point farther from nest
                Vector3 directionAwayFromNest = (randomWaypoint - playerNestTransform.position).normalized;
                randomWaypoint = playerNestTransform.position + 
                                 directionAwayFromNest * (nestRadius + nestAvoidanceDistance + 1f);
                
                // Check if this new point is within bounds
                if (mapBoundary == null || mapBoundary.IsWithinBounds(randomWaypoint))
                {
                    validWaypointFound = true;
                }
            }
            else if (!withinBounds)
            {
                // If outside bounds, adjust to be inside
                randomWaypoint = mapBoundary.GetNearestPointInBounds(randomWaypoint);
                
                // Check if this adjusted point is not too close to nest
                if (!IsPositionTooCloseToNest(randomWaypoint))
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
        
        // Rotate towards the waypoint
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, livingEntity.rotationSpeed * Time.deltaTime);
        }
        
        // Move with boundary checking
        MoveWithBoundaryCheck(transform.forward);
    }
    
    // Modify MoveWithBoundaryCheck to also avoid the nest
    private void MoveWithBoundaryCheck(Vector3 direction)
    {
        // Calculate the next position
        Vector3 nextPosition = transform.position + direction * livingEntity.moveSpeed * Time.deltaTime;
        
        bool isTooCloseToNest = IsPositionTooCloseToNest(nextPosition);
        bool isOutsideBoundary = mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition);
        
        if (isTooCloseToNest)
        {
            // We're getting too close to the nest, pick a new waypoint
            SetRandomWaypoint();
            return;
        }
        else if (isOutsideBoundary)
        {
            // Get the nearest safe position inside the boundary
            Vector3 safePosition = mapBoundary.GetNearestPointInBounds(nextPosition);
            
            // Calculate new direction along the boundary
            Vector3 redirectedDirection = (safePosition - transform.position).normalized;
            
            // Update facing direction
            transform.forward = redirectedDirection;
            
            // Move in the redirected direction
            transform.position += redirectedDirection * livingEntity.moveSpeed * Time.deltaTime;
        }
        else
        {
            // Move normally if within bounds
            transform.position += direction * livingEntity.moveSpeed * Time.deltaTime;
        }
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
            animationController.SetWalking(isWalking);
        }
        // Fall back to direct Animator control if no AnimationController is available
        else if (animator != null)
        {
            animator.SetBool("Walk", isWalking);
            animator.SetBool("Idle", !isWalking);
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
}