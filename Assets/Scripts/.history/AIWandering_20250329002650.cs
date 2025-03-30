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
    [SerializeField] private float rotationDuration = 0.8f;
    
    [Header("Animation")]
    [SerializeField] private AnimationController animationController;

    [SerializeField] public LivingEntity livingEntity;
    
    [Header("Spawn Area Restriction")]
    [SerializeField] private bool restrictToSpawnArea = true;
    [SerializeField] private float maxWanderRadius = 20f;
    [SerializeField] private bool showSpawnAreaGizmo = true;

    // Internal variables
    private Vector3 spawnPosition;
    private Vector3 currentWaypoint;
    private float groundY;
    private bool isWandering = false;
    private bool isWaiting = false;
    private bool isTurning = false;
    
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
                animationController = GetComponentInChildren<AnimationController>();
        }
        
        allyAI = GetComponent<AllyAI>();
        
        // Store spawn position and find ground level
        spawnPosition = transform.position;
        FindGroundY();
        
        // Start wandering behavior
        StartCoroutine(WanderingRoutine());
    }
    
    private void FindGroundY()
    {
        groundY = transform.position.y;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 5f, LayerMask.GetMask("Default", "Ground")))
        {
            groundY = hit.point.y;
            // Snap to ground level
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
        
        // Maintain ground position
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
    
    private IEnumerator WanderingRoutine()
    {
        while (true)
        {
            // Skip if dead
            if (livingEntity != null && livingEntity.IsDead)
            {
                UpdateAnimation(false);
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // === IDLE PHASE ===
            isWaiting = true;
            isWandering = false;
            isTurning = false;
            UpdateAnimation(false);
            
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // Skip if entity died during waiting
            if (livingEntity != null && livingEntity.IsDead)
                continue;
            
            // === WAYPOINT SELECTION ===
            Vector3 waypoint = SelectSafeWaypoint();
            
            // === TURNING PHASE ===
            isTurning = true;
            
            Vector3 directionToWaypoint = (waypoint - transform.position);
            directionToWaypoint.y = 0;
            
            if (directionToWaypoint.magnitude > 0.1f)
            {
                Quaternion startRotation = transform.rotation;
                Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
                
                float turnTimer = 0;
                
                while (turnTimer < rotationDuration)
                {
                    // Smooth rotation with easing
                    float t = EaseInOutQuad(turnTimer / rotationDuration);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                    
                    MaintainGroundY();
                    turnTimer += Time.deltaTime;
                    yield return null;
                }
                
                // Set final rotation
                transform.rotation = targetRotation;
            }
            
            isTurning = false;
            
            // === MOVEMENT PHASE ===
            isWaiting = false;
            isWandering = true;
            UpdateAnimation(true);
            
            currentWaypoint = waypoint;
            bool reachedWaypoint = false;
            bool hitBoundary = false;
            
            while (isWandering && !reachedWaypoint && !hitBoundary)
            {
                // Check if we've reached the waypoint
                float distanceToWaypoint = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(currentWaypoint.x, currentWaypoint.z)
                );
                
                if (distanceToWaypoint <= waypointReachedDistance)
                {
                    reachedWaypoint = true;
                    break;
                }
                
                // Check if next step would cross boundary
                if (restrictToSpawnArea)
                {
                    Vector3 nextPosition = transform.position + transform.forward * (livingEntity.moveSpeed * Time.deltaTime);
                    float nextDistanceFromSpawn = Vector2.Distance(
                        new Vector2(nextPosition.x, nextPosition.z),
                        new Vector2(spawnPosition.x, spawnPosition.z)
                    );
                    
                    if (nextDistanceFromSpawn >= maxWanderRadius * 0.95f)
                    {
                        hitBoundary = true;
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
                    transform.position += transform.forward * 2f * Time.deltaTime;
                }
                
                MaintainGroundY();
                yield return null;
            }
            
            // If we hit the border, handle it
            if (hitBoundary)
            {
                yield return StartCoroutine(HandleBorderReached());
            }
        }
    }
    
    private IEnumerator HandleBorderReached()
    {
        // Stop and show idle animation
        isWandering = false;
        isWaiting = true;
        UpdateAnimation(false);
        
        // Short pause
        yield return new WaitForSeconds(0.5f);
        
        // Get direction toward center
        Vector3 directionToCenter = (spawnPosition - transform.position).normalized;
        directionToCenter.y = 0;
        
        // Add some small random variation (±20 degrees)
        float randomAngle = Random.Range(-20f, 20f);
        Vector3 targetDirection = Quaternion.Euler(0, randomAngle, 0) * directionToCenter;
        
        // Start turning toward center
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        
        float turnTimer = 0;
        
        while (turnTimer < rotationDuration * 1.5f) // Slightly longer rotation for border cases
        {
            // Smooth rotation with easing
            float t = EaseInOutQuad(turnTimer / (rotationDuration * 1.5f));
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            MaintainGroundY();
            turnTimer += Time.deltaTime;
            yield return null;
        }
        
        // Set final rotation
        transform.rotation = targetRotation;
        
        // Brief pause to show we've completed the turn
        yield return new WaitForSeconds(0.3f);
        
        // Now move toward a safe position within the boundary
        isWaiting = false;
        isWandering = true;
        UpdateAnimation(true);
        
        // Calculate a safe destination (40-60% of max radius)
        float safeDistance = maxWanderRadius * Random.Range(0.4f, 0.6f);
        Vector3 safeDestination = spawnPosition + transform.forward * safeDistance;
        safeDestination.y = groundY;
        
        float moveTimer = 0;
        float moveDuration = 2f; // Move for 2 seconds or until we reach a safe area
        
        while (moveTimer < moveDuration)
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
            
            MaintainGroundY();
            
            // Check if we're now at a safe distance from the border
            float currentDistanceFromSpawn = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            // If we're now at a safe distance, stop this movement
            if (currentDistanceFromSpawn < maxWanderRadius * 0.7f)
                break;
            
            moveTimer += Time.deltaTime;
            yield return null;
        }
    }
    
    private Vector3 SelectSafeWaypoint()
    {
        // Try several attempts to find a good waypoint
        for (int i = 0; i < 5; i++)
        {
            // Random direction
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            
            // Calculate current distance from spawn
            float distanceFromSpawn = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            // Adjust direction and distance based on current position
            float distanceMultiplier = 1.0f;
            
            // If close to border, bias direction toward center
            if (distanceFromSpawn > maxWanderRadius * 0.7f)
            {
                Vector3 toCenter = (spawnPosition - transform.position).normalized;
                randomDirection = Vector3.Slerp(randomDirection, toCenter, 0.7f);
                distanceMultiplier = 0.5f; // Shorter distance when near border
            }
            
            // Calculate distance for this waypoint
            float distance = Random.Range(minWanderDistance, maxWanderDistance * distanceMultiplier);
            
            // Calculate potential waypoint
            Vector3 waypoint = transform.position + randomDirection * distance;
            waypoint.y = groundY;
            
            // Check if waypoint is within the wander radius
            float waypointDistanceFromSpawn = Vector2.Distance(
                new Vector2(waypoint.x, waypoint.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            if (waypointDistanceFromSpawn <= maxWanderRadius * 0.85f)
            {
                return waypoint;
            }
        }
        
        // Fallback: head toward spawn point
        return spawnPosition;
    }
    
    // Smoother easing function
    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
    
    private void UpdateAnimation(bool isWalking)
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
    
    // PUBLIC METHODS FOR EXTERNAL CONTROL
    
    public void ForceStop()
    {
        StopAllCoroutines();
        
        isWandering = false;
        isWaiting = true;
        isTurning = false;
        
        UpdateAnimation(false);
        
        // Reset rigidbody velocities
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
        StartCoroutine(WanderingRoutine());
    }
    
    public void ForceNewWaypointInDirection(Vector3 direction)
    {
        StopAllCoroutines();
        
        // Force horizontal direction
        direction.y = 0;
        direction.Normalize();
        
        // Calculate distance
        float distance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Calculate target position
        Vector3 targetPos = transform.position + direction * distance;
        targetPos.y = groundY;
        
        // Ensure within bounds
        if (restrictToSpawnArea)
        {
            float distanceFromSpawn = Vector2.Distance(
                new Vector2(targetPos.x, targetPos.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            if (distanceFromSpawn > maxWanderRadius * 0.85f)
            {
                // Recalculate to be within bounds
                Vector3 dirFromSpawn = (targetPos - spawnPosition).normalized;
                targetPos = spawnPosition + dirFromSpawn * (maxWanderRadius * 0.8f);
                targetPos.y = groundY;
            }
        }
        
        // Start coroutine to rotate and move to the target
        StartCoroutine(RotateAndMoveToTarget(targetPos));
    }
    
    private IEnumerator RotateAndMoveToTarget(Vector3 target)
    {
        // Stop current state
        isWandering = false;
        isWaiting = false;
        isTurning = true;
        
        // Calculate direction to target
        Vector3 directionToTarget = (target - transform.position);
        directionToTarget.y = 0;
        
        if (directionToTarget.magnitude > 0.1f)
        {
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            float turnTimer = 0;
            
            while (turnTimer < rotationDuration)
            {
                // Smooth rotation with easing
                float t = EaseInOutQuad(turnTimer / rotationDuration);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                
                MaintainGroundY();
                turnTimer += Time.deltaTime;
                yield return null;
            }
            
            // Set final rotation
            transform.rotation = targetRotation;
        }
        
        isTurning = false;
        
        // Now move to the target
        isWandering = true;
        UpdateAnimation(true);
        
        currentWaypoint = target;
        float moveTime = 0;
        float maxMoveTime = 5f; // Safety timeout
        
        while (moveTime < maxMoveTime)
        {
            // Check if we've reached the waypoint
            float distanceToWaypoint = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(currentWaypoint.x, currentWaypoint.z)
            );
            
            if (distanceToWaypoint <= waypointReachedDistance)
            {
                break;
            }
            
            // Check for border
            if (restrictToSpawnArea)
            {
                Vector3 nextPosition = transform.position + transform.forward * (livingEntity.moveSpeed * Time.deltaTime);
                float nextDistanceFromSpawn = Vector2.Distance(
                    new Vector2(nextPosition.x, nextPosition.z),
                    new Vector2(spawnPosition.x, spawnPosition.z)
                );
                
                if (nextDistanceFromSpawn >= maxWanderRadius * 0.95f)
                {
                    yield return StartCoroutine(HandleBorderReached());
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
                transform.position += transform.forward * 2f * Time.deltaTime;
            }
            
            MaintainGroundY();
            moveTime += Time.deltaTime;
            yield return null;
        }
        
        // Return to normal wandering
        StartCoroutine(WanderingRoutine());
    }
    
    public void ForceStartWithIdle()
    {
        StopAllCoroutines();
        
        isWandering = false;
        isWaiting = true;
        isTurning = false;
        
        UpdateAnimation(false);
        
        // Start with idle then wandering after delay
        StartCoroutine(IdleThenWander());
    }
    
    private IEnumerator IdleThenWander()
    {
        yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        StartCoroutine(WanderingRoutine());
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
        
        // Calculate target position
        Vector3 targetPos = transform.position + direction * distance;
        targetPos.y = groundY;
        
        // Ensure within bounds
        if (restrictToSpawnArea)
        {
            float distanceFromSpawn = Vector2.Distance(
                new Vector2(targetPos.x, targetPos.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            if (distanceFromSpawn > maxWanderRadius * 0.85f)
            {
                // Recalculate to be within bounds
                Vector3 dirFromSpawn = (targetPos - spawnPosition).normalized;
                targetPos = spawnPosition + dirFromSpawn * (maxWanderRadius * 0.8f);
                targetPos.y = groundY;
            }
        }
        
        // Immediately face the direction (no rotation animation)
        transform.rotation = Quaternion.LookRotation(direction);
        
        // Start walking immediately
        isWandering = true;
        isWaiting = false;
        isTurning = false;
        UpdateAnimation(true);
        
        // Start coroutine to move to the target
        StartCoroutine(MoveToTargetWithoutRotation(targetPos));
    }
    
    private IEnumerator MoveToTargetWithoutRotation(Vector3 target)
    {
        currentWaypoint = target;
        float moveTime = 0;
        float maxMoveTime = 5f; // Safety timeout
        
        while (moveTime < maxMoveTime)
        {
            // Check if we've reached the waypoint
            float distanceToWaypoint = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(currentWaypoint.x, currentWaypoint.z)
            );
            
            if (distanceToWaypoint <= waypointReachedDistance)
            {
                break;
            }
            
            // Check for border
            if (restrictToSpawnArea)
            {
                Vector3 nextPosition = transform.position + transform.forward * (livingEntity.moveSpeed * Time.deltaTime);
                float nextDistanceFromSpawn = Vector2.Distance(
                    new Vector2(nextPosition.x, nextPosition.z),
                    new Vector2(spawnPosition.x, spawnPosition.z)
                );
                
                if (nextDistanceFromSpawn >= maxWanderRadius * 0.95f)
                {
                    yield return StartCoroutine(HandleBorderReached());
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
                transform.position += transform.forward * 2f * Time.deltaTime;
            }
            
            MaintainGroundY();
            moveTime += Time.deltaTime;
            yield return null;
        }
        
        // Return to normal wandering
        StartCoroutine(WanderingRoutine());
    }
    
    public bool IsCurrentlyMoving()
    {
        return isWandering && !isWaiting && !isTurning;
    }
    
    public void SetWanderingEnabled(bool enabled)
    {
        if (enabled)
        {
            StopAllCoroutines();
            StartCoroutine(WanderingRoutine());
        }
        else
        {
            StopAllCoroutines();
            isWandering = false;
            isWaiting = true;
            isTurning = false;
            UpdateAnimation(false);
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