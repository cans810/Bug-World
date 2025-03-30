using UnityEngine;
using System.Collections;

public class AIWandering : MonoBehaviour
{
    [Header("Wandering Settings")]
    [SerializeField] private float minWanderDistance = 3f;
    [SerializeField] private float maxWanderDistance = 10f;
    [SerializeField] private float waypointReachedDistance = 0.5f;
    [SerializeField] private float rotationSpeed = 3f;
    
    [Header("Timing")]
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 5f;
    
    [Header("Animation")]
    [SerializeField] private AnimationController animationController;

    [SerializeField] public LivingEntity livingEntity;
    
    [Header("Spawn Area")]
    [SerializeField] private bool restrictToSpawnArea = true;
    [SerializeField] private float maxWanderRadius = 20f;
    [SerializeField] private bool showSpawnAreaGizmo = true;

    // Simple state machine
    private enum WanderState { Idle, Rotating, Walking, ReturnToCenter }
    private WanderState currentState = WanderState.Idle;
    
    // Core variables
    private Vector3 spawnPosition;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float groundY;
    
    // Components
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
                animationController = GetComponentInChildren<AnimationController>();
        }
        
        allyAI = GetComponent<AllyAI>();
        
        // Store spawn position
        spawnPosition = transform.position;
        
        // Find ground Y level
        FindGroundY();
        
        // Start the wandering process
        StartWandering();
    }
    
    private void FindGroundY()
    {
        groundY = transform.position.y;
        RaycastHit hit;
        
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
        // Skip if dead
        if (livingEntity != null && livingEntity.IsDead)
            return;
        
        // Skip if AllyAI is handling movement
        if (allyAI != null && allyAI.HasAppliedMovementThisFrame)
            return;
        
        // Maintain ground Y position
        MaintainGroundY();
        
        // Simple state machine in Update
        switch (currentState)
        {
            case WanderState.Idle:
                // Do nothing, waiting for coroutine
                break;
                
            case WanderState.Rotating:
                // Smoothly rotate toward target
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                
                // Check if rotation is complete
                if (Quaternion.Angle(transform.rotation, targetRotation) < 2f)
                {
                    // Snap to exact rotation
                    transform.rotation = targetRotation;
                    
                    // Start walking
                    currentState = WanderState.Walking;
                    SetAnimation(true);
                }
                break;
                
            case WanderState.Walking:
            case WanderState.ReturnToCenter:
                // Move forward
                if (livingEntity != null)
                {
                    livingEntity.MoveInDirection(transform.forward, 1.0f);
                }
                else
                {
                    transform.position += transform.forward * 2f * Time.deltaTime;
                }
                
                // Check if we reached destination
                float distanceToTarget = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(targetPosition.x, targetPosition.z)
                );
                
                if (distanceToTarget <= waypointReachedDistance)
                {
                    // We've reached the target
                    if (currentState == WanderState.ReturnToCenter)
                    {
                        // If returning to center, pick a new point once we arrive
                        PickNewWaypoint();
                    }
                    else
                    {
                        // If regular walking, go to idle state
                        currentState = WanderState.Idle;
                        SetAnimation(false);
                        StartCoroutine(WaitThenPickNewWaypoint());
                    }
                }
                
                // Check if we're hitting the boundary
                if (restrictToSpawnArea && currentState == WanderState.Walking)
                {
                    float distanceFromSpawn = Vector2.Distance(
                        new Vector2(transform.position.x, transform.position.z),
                        new Vector2(spawnPosition.x, spawnPosition.z)
                    );
                    
                    if (distanceFromSpawn >= maxWanderRadius * 0.9f)
                    {
                        // Stop walking and return to center
                        ReturnToCenter();
                    }
                }
                break;
        }
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
    
    private void StartWandering()
    {
        currentState = WanderState.Idle;
        SetAnimation(false);
        StartCoroutine(WaitThenPickNewWaypoint());
    }
    
    private IEnumerator WaitThenPickNewWaypoint()
    {
        // Wait for random time
        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime);
        
        // Pick a new waypoint
        PickNewWaypoint();
    }
    
    private void PickNewWaypoint()
    {
        // Generate random direction
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        
        // Scale distance based on distance from spawn
        float distanceFromSpawn = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(spawnPosition.x, spawnPosition.z)
        );
        
        float distance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // If we're already far from spawn, bias toward center
        if (distanceFromSpawn > maxWanderRadius * 0.6f)
        {
            Vector3 toCenter = (spawnPosition - transform.position).normalized;
            randomDirection = Vector3.Slerp(randomDirection, toCenter, 0.5f);
            distance *= 0.7f; // Shorter distance when far from center
        }
        
        // Calculate target position
        targetPosition = transform.position + randomDirection * distance;
        targetPosition.y = groundY;
        
        // Ensure target is within radius
        float targetDistanceFromSpawn = Vector2.Distance(
            new Vector2(targetPosition.x, targetPosition.z),
            new Vector2(spawnPosition.x, spawnPosition.z)
        );
        
        if (targetDistanceFromSpawn > maxWanderRadius * 0.8f)
        {
            // Clamp to safe radius
            Vector3 directionFromSpawn = (targetPosition - spawnPosition).normalized;
            targetPosition = spawnPosition + directionFromSpawn * (maxWanderRadius * 0.7f);
            targetPosition.y = groundY;
        }
        
        // Set target rotation
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0;
        targetRotation = Quaternion.LookRotation(directionToTarget);
        
        // Start rotating
        currentState = WanderState.Rotating;
    }
    
    private void ReturnToCenter()
    {
        // Stop and go idle briefly
        currentState = WanderState.Idle;
        SetAnimation(false);
        
        // Start coroutine to handle the return to center with a short delay
        StartCoroutine(DelayedReturnToCenter());
    }
    
    private IEnumerator DelayedReturnToCenter()
    {
        // Short pause before turning back
        yield return new WaitForSeconds(0.5f);
        
        // Set target to a point near center
        Vector3 directionToCenter = (spawnPosition - transform.position).normalized;
        float returnDistance = Vector3.Distance(transform.position, spawnPosition) * 0.7f;
        
        targetPosition = spawnPosition - directionToCenter * Random.Range(1f, 3f); // Slightly off-center
        targetPosition.y = groundY;
        
        // Set rotation toward center
        directionToCenter.y = 0;
        targetRotation = Quaternion.LookRotation(directionToCenter);
        
        // Start rotating
        currentState = WanderState.Rotating;
        
        // Once rotation is complete, the Update method will automatically change to walking
        // and the state will be ReturnToCenter
        yield return new WaitUntil(() => currentState == WanderState.Walking);
        
        // Change to ReturnToCenter state
        currentState = WanderState.ReturnToCenter;
    }
    
    private void SetAnimation(bool isWalking)
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
    
    // PUBLIC API
    
    public void ForceStop()
    {
        StopAllCoroutines();
        currentState = WanderState.Idle;
        SetAnimation(false);
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    public void ForceRestartWandering()
    {
        StopAllCoroutines();
        StartWandering();
    }
    
    public void ForceNewWaypointInDirection(Vector3 direction)
    {
        StopAllCoroutines();
        
        // Force horizontal direction
        direction.y = 0;
        direction.Normalize();
        
        // Calculate distance
        float distance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Set target position
        targetPosition = transform.position + direction * distance;
        targetPosition.y = groundY;
        
        // Ensure within bounds
        if (restrictToSpawnArea)
        {
            float distanceFromSpawn = Vector2.Distance(
                new Vector2(targetPosition.x, targetPosition.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            if (distanceFromSpawn > maxWanderRadius * 0.8f)
            {
                // Recalculate to be within bounds
                Vector3 dirFromSpawn = (targetPosition - spawnPosition).normalized;
                targetPosition = spawnPosition + dirFromSpawn * (maxWanderRadius * 0.7f);
                targetPosition.y = groundY;
            }
        }
        
        // Set rotation
        targetRotation = Quaternion.LookRotation(direction);
        
        // Start rotating
        currentState = WanderState.Rotating;
        SetAnimation(false);
    }
    
    public void ForceStartWithIdle()
    {
        StopAllCoroutines();
        currentState = WanderState.Idle;
        SetAnimation(false);
        StartCoroutine(WaitThenPickNewWaypoint());
    }
    
    public void ForceNewWaypoint()
    {
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        ForceNewWaypointInDirection(randomDirection);
    }
    
    public void ForceStartWithDirection(Vector3 direction)
    {
        StopAllCoroutines();
        
        // Force horizontal direction
        direction.y = 0;
        direction.Normalize();
        
        // Calculate distance
        float distance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Set target position
        targetPosition = transform.position + direction * distance;
        targetPosition.y = groundY;
        
        // Ensure within bounds
        if (restrictToSpawnArea)
        {
            float distanceFromSpawn = Vector2.Distance(
                new Vector2(targetPosition.x, targetPosition.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            if (distanceFromSpawn > maxWanderRadius * 0.8f)
            {
                // Recalculate to be within bounds
                Vector3 dirFromSpawn = (targetPosition - spawnPosition).normalized;
                targetPosition = spawnPosition + dirFromSpawn * (maxWanderRadius * 0.7f);
                targetPosition.y = groundY;
            }
        }
        
        // Immediately set rotation (skip the rotation phase)
        transform.rotation = Quaternion.LookRotation(direction);
        
        // Start walking immediately
        currentState = WanderState.Walking;
        SetAnimation(true);
    }
    
    public bool IsCurrentlyMoving()
    {
        return currentState == WanderState.Walking || currentState == WanderState.ReturnToCenter;
    }
    
    public void SetWanderingEnabled(bool enabled)
    {
        if (enabled)
        {
            if (currentState == WanderState.Idle)
            {
                StartWandering();
            }
        }
        else
        {
            StopAllCoroutines();
            currentState = WanderState.Idle;
            SetAnimation(false);
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