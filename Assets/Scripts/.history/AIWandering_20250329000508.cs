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
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Animation")]
    [SerializeField] private AnimationController animationController;

    [SerializeField] public LivingEntity livingEntity;
    
    [Header("Spawn Area Restriction")]
    [SerializeField] private bool restrictToSpawnArea = true;
    [SerializeField] private float maxWanderRadius = 20f; // Maximum distance from spawn point
    [SerializeField] private bool showSpawnAreaGizmo = true; // For debugging

    // State management
    private enum WanderState { Idle, Turning, Moving, BorderHandling }
    private WanderState currentState = WanderState.Idle;
    
    // Waypoint tracking
    private Vector3 currentWaypoint;
    private Vector3 spawnPosition;
    private float groundY;
    
    // Component references
    private Animator animator;
    private AllyAI allyAI;
    
    private void Start()
    {
        // Get components
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
        
        allyAI = GetComponent<AllyAI>();
        
        // Store spawn position
        spawnPosition = transform.position;
        
        // Find ground position
        FindGroundY();
        
        // Start wandering
        StartCoroutine(WanderingStateMachine());
    }
    
    private void FindGroundY()
    {
        // Use raycast to find actual ground position
        RaycastHit hit;
        groundY = transform.position.y;
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 5f, LayerMask.GetMask("Default", "Ground")))
        {
            groundY = hit.point.y;
            // Snap to ground
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
        }
    }
    
    private void Update()
    {
        // Skip if entity is dead
        if (livingEntity != null && livingEntity.IsDead)
        {
            StopAllCoroutines();
            UpdateAnimationState(false);
            return;
        }
        
        // Skip if AllyAI is handling movement
        if (allyAI != null && allyAI.HasAppliedMovementThisFrame)
            return;
        
        // Maintain Y position
        MaintainGroundY();
    }
    
    private void MaintainGroundY()
    {
        if (transform.position.y != groundY)
        {
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
        }
    }
    
    private IEnumerator WanderingStateMachine()
    {
        while (true)
        {
            // Skip if entity is dead
            if (livingEntity != null && livingEntity.IsDead)
            {
                currentState = WanderState.Idle;
                UpdateAnimationState(false);
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            switch (currentState)
            {
                case WanderState.Idle:
                    yield return StartCoroutine(IdleState());
                    break;
                    
                case WanderState.Turning:
                    yield return StartCoroutine(TurningState());
                    break;
                    
                case WanderState.Moving:
                    yield return StartCoroutine(MovingState());
                    break;
                    
                case WanderState.BorderHandling:
                    yield return StartCoroutine(BorderHandlingState());
                    break;
            }
            
            yield return null;
        }
    }
    
    private IEnumerator IdleState()
    {
        // Enter idle state
        UpdateAnimationState(false);
        
        // Wait for random time
        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        float timer = 0;
        
        while (timer < waitTime)
        {
            MaintainGroundY();
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Select new waypoint
        currentWaypoint = SelectNewWaypoint();
        
        // Transition to turning state
        currentState = WanderState.Turning;
    }
    
    private IEnumerator TurningState()
    {
        // Calculate direction to waypoint
        Vector3 direction = currentWaypoint - transform.position;
        direction.y = 0;
        
        if (direction.magnitude < 0.1f)
        {
            // Too close, move directly to idle
            currentState = WanderState.Idle;
            yield break;
        }
        
        // Calculate target rotation
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion startRotation = transform.rotation;
        
        // Smoothly rotate to target
        float rotationTime = 0.5f;
        float timer = 0;
        
        while (timer < rotationTime)
        {
            // Smooth rotation with easing
            float t = EaseInOutCubic(timer / rotationTime);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            MaintainGroundY();
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final rotation is exact
        transform.rotation = targetRotation;
        
        // Transition to moving state
        currentState = WanderState.Moving;
    }
    
    private IEnumerator MovingState()
    {
        // Start moving animation
        UpdateAnimationState(true);
        
        while (true)
        {
            // Calculate distance to waypoint (horizontally)
            Vector3 horizontalPosition = transform.position;
            horizontalPosition.y = 0;
            
            Vector3 horizontalWaypoint = currentWaypoint;
            horizontalWaypoint.y = 0;
            
            float distance = Vector3.Distance(horizontalPosition, horizontalWaypoint);
            
            // Check if we've reached the waypoint
            if (distance <= waypointReachedDistance)
            {
                // Waypoint reached, transition to idle
                currentState = WanderState.Idle;
                break;
            }
            
            // Check for border crossing
            if (restrictToSpawnArea)
            {
                // Calculate next position BEFORE moving
                Vector3 nextPosition = transform.position + transform.forward * (livingEntity.moveSpeed * Time.deltaTime);
                float nextDistanceFromSpawn = Vector2.Distance(
                    new Vector2(nextPosition.x, nextPosition.z),
                    new Vector2(spawnPosition.x, spawnPosition.z)
                );
                
                // If next position would exceed border, enter border handling
                if (nextDistanceFromSpawn >= maxWanderRadius * 0.95f)
                {
                    currentState = WanderState.BorderHandling;
                    break;
                }
            }
            
            // Move forward
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(transform.forward, 1.0f);
            }
            else
            {
                // Fallback movement
                transform.position += transform.forward * 2f * Time.deltaTime;
            }
            
            // Maintain Y position
            MaintainGroundY();
            
            yield return null;
        }
    }
    
    private IEnumerator BorderHandlingState()
    {
        // Stop and switch to idle animation
        UpdateAnimationState(false);
        
        // Short pause before turning
        yield return new WaitForSeconds(0.5f);
        
        // Calculate direction toward spawn (center)
        Vector3 directionToCenter = (spawnPosition - transform.position).normalized;
        directionToCenter.y = 0;
        
        // Add slight random variation (±15 degrees)
        float randomAngle = Random.Range(-15f, 15f);
        Vector3 targetDirection = Quaternion.Euler(0, randomAngle, 0) * directionToCenter;
        
        // Smooth rotation toward center
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        
        float rotationTime = 1f;
        float timer = 0;
        
        while (timer < rotationTime)
        {
            // Apply smooth rotation with easing
            float t = EaseInOutCubic(timer / rotationTime);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            MaintainGroundY();
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final rotation is exact
        transform.rotation = targetRotation;
        
        // Short pause after rotation
        yield return new WaitForSeconds(0.3f);
        
        // Choose a waypoint in the safe zone toward center
        float safeDistance = Random.Range(maxWanderRadius * 0.3f, maxWanderRadius * 0.6f);
        Vector3 safeDirection = transform.forward;
        currentWaypoint = spawnPosition + safeDirection * safeDistance;
        currentWaypoint.y = groundY;
        
        // Transition to moving state
        currentState = WanderState.Moving;
    }
    
    private Vector3 SelectNewWaypoint()
    {
        // Try several times to find a good waypoint
        for (int i = 0; i < 5; i++)
        {
            // Random direction
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            
            // Scale distance based on distance from spawn
            float distanceFromSpawn = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            float maxDistanceScale = 1.0f;
            
            // If we're already near the edge, reduce max distance
            if (distanceFromSpawn > maxWanderRadius * 0.7f)
            {
                // Add bias toward spawn center
                Vector3 toCenter = (spawnPosition - transform.position).normalized;
                randomDirection = Vector3.Slerp(randomDirection, toCenter, 0.7f);
                
                // Also reduce max distance
                maxDistanceScale = 0.5f;
            }
            
            // Calculate random distance within range
            float distance = Random.Range(minWanderDistance, maxWanderDistance * maxDistanceScale);
            
            // Calculate potential waypoint
            Vector3 waypoint = transform.position + randomDirection * distance;
            waypoint.y = groundY;
            
            // Check if waypoint is within spawn radius
            float waypointDistanceFromSpawn = Vector2.Distance(
                new Vector2(waypoint.x, waypoint.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            if (waypointDistanceFromSpawn <= maxWanderRadius * 0.85f)
            {
                return waypoint;
            }
        }
        
        // Fallback: return a point closer to spawn
        Vector3 centerDirection = (spawnPosition - transform.position).normalized;
        return transform.position + centerDirection * minWanderDistance * 2;
    }
    
    // Helper for easing functions
    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }
    
    private void UpdateAnimationState(bool isWalking)
    {
        if (animationController != null)
        {
            if (isWalking)
            {
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
    
    // Public methods for external control
    public void ForceStop()
    {
        StopAllCoroutines();
        
        currentState = WanderState.Idle;
        UpdateAnimationState(false);
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }
        
        StartCoroutine(WanderingStateMachine());
    }
    
    public void ForceRestartWandering()
    {
        StopAllCoroutines();
        currentState = WanderState.Idle;
        StartCoroutine(WanderingStateMachine());
    }
    
    public void ForceNewWaypointInDirection(Vector3 direction)
    {
        StopAllCoroutines();
        
        // Force horizontal direction
        direction.y = 0;
        direction.Normalize();
        
        // Calculate distance
        float distance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Calculate waypoint
        Vector3 waypoint = transform.position + direction * distance;
        waypoint.y = groundY;
        
        // Ensure within bounds
        if (restrictToSpawnArea)
        {
            float distanceFromSpawn = Vector2.Distance(
                new Vector2(waypoint.x, waypoint.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            if (distanceFromSpawn > maxWanderRadius * 0.85f)
            {
                Vector3 directionFromSpawn = (waypoint - spawnPosition).normalized;
                waypoint = spawnPosition + directionFromSpawn * (maxWanderRadius * 0.8f);
                waypoint.y = groundY;
            }
        }
        
        // Set waypoint and state
        currentWaypoint = waypoint;
        currentState = WanderState.Turning;
        
        // Restart state machine
        StartCoroutine(WanderingStateMachine());
    }
    
    public void ForceStartWithIdle()
    {
        StopAllCoroutines();
        currentState = WanderState.Idle;
        StartCoroutine(WanderingStateMachine());
    }
    
    public void ForceNewWaypoint()
    {
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        ForceNewWaypointInDirection(randomDirection);
    }
    
    public bool IsCurrentlyMoving()
    {
        return currentState == WanderState.Moving;
    }
    
    public void SetWanderingEnabled(bool enabled)
    {
        if (enabled)
        {
            if (currentState == WanderState.Idle)
            {
                StopAllCoroutines();
                StartCoroutine(WanderingStateMachine());
            }
        }
        else
        {
            StopAllCoroutines();
            currentState = WanderState.Idle;
            UpdateAnimationState(false);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (showSpawnAreaGizmo && restrictToSpawnArea)
        {
            // Draw the spawn area in yellow
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Transparent yellow
            
            // In editor, use current position; in play mode, use stored spawn position
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            
#if UNITY_EDITOR
            // Draw a disc for the spawn area
            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.3f);
            UnityEditor.Handles.DrawSolidDisc(center, Vector3.up, maxWanderRadius);
            
            // Draw the outline
            Gizmos.color = Color.yellow;
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(center, Vector3.up, maxWanderRadius);
#else
            // Fallback for builds - just draw a wire sphere
            Gizmos.DrawWireSphere(center, maxWanderRadius);
#endif
        }
    }
}