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
    [SerializeField] private LayerMask obstacleLayer; // Set this to "InsectObstacles" layer
    
    [Header("Animation")]
    [SerializeField] private AnimationController animationController;

    [SerializeField] private LivingEntity livingEntity;
    
    // Internal variables
    private Vector3 currentWaypoint;
    private bool isWandering = false;
    private bool isWaiting = false;
    private Animator animator;
    
    private void Start()
    {
        // Get reference to the animator
        animator = GetComponent<Animator>();

        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
        
        // Get reference to the animation controller if not set
        if (animationController == null)
        {
            animationController = GetComponent<AnimationController>();
            if (animationController == null)
            {
                animationController = GetComponentInChildren<AnimationController>();
            }
        }
        
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
            UpdateAnimationState(false);
            
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // Start wandering to a new position
            isWaiting = false;
            isWandering = true;
            UpdateAnimationState(true);
            
            // Pick a random point to wander to
            SetRandomWaypoint();
            
            // Wait until we reach the waypoint
            while (Vector3.Distance(transform.position, currentWaypoint) > waypointReachedDistance)
            {
                // Handle obstacle avoidance
                if (avoidObstacles)
                {
                    AvoidObstacles();
                }
                
                // Update path at regular intervals
                yield return new WaitForSeconds(pathfindingUpdateInterval);
                
                // Stop if wandering is disabled
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