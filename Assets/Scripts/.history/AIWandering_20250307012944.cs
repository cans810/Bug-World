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
    
    // Internal variables
    private Vector3 currentWaypoint;
    private bool isWandering = false;
    private bool isWaiting = false;
    private Animator animator;
    private MapBoundary mapBoundary;
    
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
        
        mapBoundary = FindObjectOfType<MapBoundary>();
        
        StartCoroutine(WanderingRoutine());
    }
    
    private void Update()
    {
        if (isWandering && !isWaiting)
        {
            MoveTowardsWaypoint();
        }
    }
    
    private IEnumerator WanderingRoutine()
    {
        while (true)
        {
            // wait at current position
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
                
                // NOTE: I don't know how this works, check it later
                yield return new WaitForSeconds(pathfindingUpdateInterval);
                
                // if the enemy is not wandering, stop the wandering routine
                if (!isWandering)
                {
                    break;
                }
            }
        }
    }
    
    private void SetRandomWaypoint()
    {
        // Find a random point in a circle around the current position
        float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        Vector3 offset = new Vector3(
            Mathf.Cos(randomAngle) * randomDistance,
            0f, // Keep on the same Y level
            Mathf.Sin(randomAngle) * randomDistance
        );
        
        currentWaypoint = transform.position + offset;
        
        // Make sure waypoint is within map boundaries
        if (mapBoundary != null && !mapBoundary.IsWithinBounds(currentWaypoint))
        {
            // If the waypoint is outside bounds, adjust it to be inside
            currentWaypoint = mapBoundary.GetNearestPointInBounds(currentWaypoint);
        }
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
    
    // Method to handle movement with boundary checking (same as in EnemyAI)
    private void MoveWithBoundaryCheck(Vector3 direction)
    {
        // Calculate the next position
        Vector3 nextPosition = transform.position + direction * livingEntity.moveSpeed * Time.deltaTime;
        
        // Check if the next position is within bounds
        if (mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition))
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
}