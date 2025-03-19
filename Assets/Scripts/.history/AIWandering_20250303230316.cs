using UnityEngine;
using System.Collections;

public class AIWandering : MonoBehaviour
{
    [Header("Wandering Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;
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
    [SerializeField] private LayerMask obstacleLayer; // Set this to "InsectObstacles" layer
    
    [Header("Animation")]
    [SerializeField] private string walkAnimationParameter = "Walk";
    [SerializeField] private string idleAnimationParameter = "Idle";
    
    // Internal variables
    private Vector3 currentWaypoint;
    private bool isWandering = false;
    private bool isWaiting = false;
    private Animator animator;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        
        // Start the wandering behavior
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
            // Wait at current position
            isWaiting = true;
            SetAnimationState(false);
            
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // Start wandering to a new position
            isWaiting = false;
            isWandering = true;
            SetAnimationState(true);
            
            // Find a new random waypoint
            FindNewWaypoint();
            
            // Wait until we reach the waypoint or get stuck
            float stuckTimer = 0f;
            float maxStuckTime = 10f; // Maximum time to try reaching a waypoint before giving up
            
            while (Vector3.Distance(transform.position, currentWaypoint) > waypointReachedDistance)
            {
                stuckTimer += Time.deltaTime;
                
                // If we're stuck for too long, find a new waypoint
                if (stuckTimer > maxStuckTime)
                {
                    FindNewWaypoint();
                    stuckTimer = 0f;
                }
                
                // Periodically check for obstacles (but not other insects)
                if (avoidObstacles && Random.value < pathfindingUpdateInterval * Time.deltaTime)
                {
                    AvoidObstacles();
                }
                
                yield return null;
            }
            
            // Reached the waypoint
            isWandering = false;
        }
    }
    
    private void FindNewWaypoint()
    {
        float wanderDistance = Random.Range(minWanderDistance, maxWanderDistance);
        Vector3 randomDirection = Random.insideUnitSphere * wanderDistance;
        randomDirection.y = 0; // Keep on the same Y level
        
        currentWaypoint = transform.position + randomDirection;
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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Move towards the waypoint
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
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
    
    private void SetAnimationState(bool isWalking)
    {
        if (animator != null)
        {
            animator.SetBool(walkAnimationParameter, isWalking);
            animator.SetBool(idleAnimationParameter, !isWalking);
        }
    }
    
    // Optional: Public method to control the AI from other scripts
    public void SetWanderingEnabled(bool enabled)
    {
        isWandering = enabled;
        SetAnimationState(enabled && !isWaiting);
    }
}