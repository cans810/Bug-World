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
    [SerializeField] private float rotationDuration = 1.5f; // Longer rotation time for smoother effect
    
    [Header("Animation")]
    [SerializeField] private AnimationController animationController;

    [SerializeField] public LivingEntity livingEntity;
    
    [Header("Spawn Area")]
    [SerializeField] private bool restrictToSpawnArea = true;
    [SerializeField] private float maxWanderRadius = 20f;
    [SerializeField] private bool showSpawnAreaGizmo = true;

    [Header("Recovery Settings")]
    [SerializeField] private float maxIdleTime = 10f; // Max time to be idle before forced restart

    // Simple state machine
    private enum WanderState { Idle, Walking, ReturnToCenter }
    private WanderState currentState = WanderState.Idle;
    
    // Core variables
    private Vector3 spawnPosition;
    private Vector3 targetPosition;
    private float groundY;
    
    // Components
    private Animator animator;
    private AllyAI allyAI;
    
    // Rotation tracking
    private bool isRotating = false;
    
    // Recovery tracking
    private float idleTimeCounter = 0f;
    
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
        
        // Auto-recovery if we've been idle too long
        if (currentState == WanderState.Idle)
        {
            idleTimeCounter += Time.deltaTime;
            if (idleTimeCounter > maxIdleTime)
            {
                Debug.Log($"AIWandering for {gameObject.name} was idle too long, forcing restart");
                ForceRestartWandering();
                idleTimeCounter = 0f;
            }
        }
        else
        {
            idleTimeCounter = 0f;
        }
        
        // Skip if currently rotating (handled by coroutine)
        if (isRotating)
            return;
        
        // Maintain ground Y position
        MaintainGroundY();
        
        // Only handle walking states here - rotations are handled by coroutines
        if (currentState == WanderState.Walking || currentState == WanderState.ReturnToCenter)
        {
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
                    StartCoroutine(PickNewWaypointWithRotation());
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
        StartCoroutine(PickNewWaypointWithRotation());
    }
    
    private IEnumerator PickNewWaypointWithRotation()
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
        
        if (directionToTarget.magnitude < 0.1f)
        {
            // Target too close, pick another
            StartCoroutine(PickNewWaypointWithRotation());
            yield break;
        }
        
        // Smoothly rotate to face target
        yield return StartCoroutine(SmoothlyRotate(directionToTarget));
        
        // Start walking after rotation completes
        currentState = WanderState.Walking;
        SetAnimation(true);
    }
    
    private void ReturnToCenter()
    {
        // Stop walking
        currentState = WanderState.Idle;
        SetAnimation(false);
        
        // Start coroutine to handle returning to center
        StartCoroutine(ReturnToCenterRoutine());
    }
    
    private IEnumerator ReturnToCenterRoutine()
    {
        // Short pause before turning back
        yield return new WaitForSeconds(0.5f);
        
        // Calculate direction back to center
        Vector3 directionToCenter = (spawnPosition - transform.position).normalized;
        directionToCenter.y = 0;
        
        // Add slight random variation (±15 degrees)
        float randomAngle = Random.Range(-15f, 15f);
        Vector3 targetDirection = Quaternion.Euler(0, randomAngle, 0) * directionToCenter;
        
        // Smoothly rotate to face center
        yield return StartCoroutine(SmoothlyRotate(targetDirection));
        
        // Calculate a safe position to return to (closer to center)
        float safeDistance = Vector3.Distance(transform.position, spawnPosition) * 0.6f;
        targetPosition = spawnPosition - directionToCenter * Random.Range(1f, 3f);
        targetPosition.y = groundY;
        
        // Start walking toward center
        currentState = WanderState.ReturnToCenter;
        SetAnimation(true);
    }
    
    private IEnumerator SmoothlyRotate(Vector3 direction)
    {
        // Mark that rotation is in progress
        isRotating = true;
        
        // Calculate start and target rotations
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        // Use timer-based approach for consistent speed regardless of framerate
        float elapsedTime = 0f;
        
        // Use custom curve for more natural motion
        // Start slow, accelerate, then decelerate at end
        while (elapsedTime < rotationDuration)
        {
            // Calculate t with smooth ease in/out
            float t = elapsedTime / rotationDuration;
            float smoothT = EaseInOutCubic(t);
            
            // Apply rotation
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
            
            // Update timer
            elapsedTime += Time.deltaTime;
            
            // Maintain ground position
            MaintainGroundY();
            
            yield return null;
        }
        
        // Ensure final rotation is exact
        transform.rotation = targetRotation;
        
        // Mark rotation as complete
        isRotating = false;
    }
    
    // Easing function for more natural rotation
    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
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
        isRotating = false;
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
        isRotating = false;
        StartWandering();
    }
    
    public void ForceNewWaypointInDirection(Vector3 direction)
    {
        StopAllCoroutines();
        isRotating = false;
        
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
        
        // Start the rotation coroutine
        StartCoroutine(RotateAndMoveInDirection(direction));
    }
    
    private IEnumerator RotateAndMoveInDirection(Vector3 direction)
    {
        // Rotate smoothly to the direction
        yield return StartCoroutine(SmoothlyRotate(direction));
        
        // Start walking
        currentState = WanderState.Walking;
        SetAnimation(true);
    }
    
    public void ForceStartWithIdle()
    {
        StopAllCoroutines();
        isRotating = false;
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
        isRotating = false;
        
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
            isRotating = false;
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

    // Add this method to detect and handle MapBorder collision
    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit a MapBorder
        if (other.gameObject.layer == LayerMask.NameToLayer("MapBorder"))
        {
            // Return to the center of wander area
            ReturnToSpawnCenter();
        }
    }

    // Add this new method to force return to spawn center
    private void ReturnToSpawnCenter()
    {
        // Stop all current movement
        StopAllCoroutines();
        
        // Reset states
        isRotating = false;
        currentState = WanderState.Idle;
        SetAnimation(false);
        
        // Start coroutine to return to spawn center
        StartCoroutine(ReturnToSpawnCenterRoutine());
    }

    // Add this routine to handle returning to the exact center
    private IEnumerator ReturnToSpawnCenterRoutine()
    {
        // Short pause before turning
        yield return new WaitForSeconds(0.5f);
        
        // Calculate direction exactly to spawn center
        Vector3 directionToCenter = (spawnPosition - transform.position).normalized;
        directionToCenter.y = 0;
        
        // Smoothly rotate to face exact center (no random variation)
        yield return StartCoroutine(SmoothlyRotate(directionToCenter));
        
        // Set target to the exact center of the spawn area
        targetPosition = spawnPosition;
        targetPosition.y = groundY;
        
        // Start walking toward center
        currentState = WanderState.ReturnToCenter;
        SetAnimation(true);
        
        // Debug visualization
        Debug.DrawLine(transform.position, spawnPosition, Color.red, 3f);
        Debug.Log($"{gameObject.name} hit map border, returning to spawn center");
    }
}