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
    
    [Header("Spawn Area Restriction")]
    [SerializeField] private bool restrictToSpawnArea = true;
    [SerializeField] private float maxWanderRadius = 2f; // Maximum distance from spawn point
    [SerializeField] private bool showSpawnAreaGizmo = true; // For debugging

    // Internal variables
    private Vector3 currentWaypoint;
    private bool isWandering = false;
    private bool isWaiting = false;
    private Animator animator;
    private AllyAI allyAI;
    
    private Vector3 spawnPosition; // Store the original spawn position
    
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
        
        // Store the spawn position
        spawnPosition = transform.position;
        
        StartCoroutine(WanderingRoutine());
    }
    
    private void Update()
    {
        // Skip if entity is dead
        if (livingEntity != null && livingEntity.IsDead)
        {
            // Ensure we're not showing a walking animation when dead
            if (isWandering)
            {
                isWandering = false;
                UpdateAnimationState(false);
            }
            return;
        }
        
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
    }
    
    private IEnumerator WanderingRoutine()
    {
        while (true)
        {
            // Skip if entity is dead
            if (livingEntity != null && livingEntity.IsDead)
            {
                isWaiting = true;
                isWandering = false;
                UpdateAnimationState(false);
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // --- WAITING PHASE ---
            isWaiting = true;
            isWandering = false;
            UpdateAnimationState(false);
            
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // Skip if entity died during waiting
            if (livingEntity != null && livingEntity.IsDead)
                continue;
            
            // --- WAYPOINT SELECTION PHASE ---
            Vector3 targetWaypoint = GetSafeWaypoint();
            // Ensure Y position is same as current to prevent vertical movement
            targetWaypoint.y = transform.position.y;
            Debug.Log($"{gameObject.name}: Selected waypoint at {targetWaypoint}");
            
            // --- TURNING PHASE ---
            Vector3 directionToWaypoint = (targetWaypoint - transform.position).normalized;
            // Ensure we only rotate on the horizontal plane
            directionToWaypoint.y = 0;
            directionToWaypoint.Normalize();
            
            float turnDuration = 0.5f;
            float turnTimer = 0;
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
            
            while (turnTimer < turnDuration)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer / turnDuration);
                turnTimer += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final rotation is exact
            transform.rotation = targetRotation;
            
            // --- MOVEMENT PHASE ---
            isWaiting = false;
            isWandering = true;
            UpdateAnimationState(true);
            
            float distanceToWaypoint = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z), 
                new Vector3(targetWaypoint.x, 0, targetWaypoint.z)
            );
            float moveDuration = distanceToWaypoint / livingEntity.moveSpeed;
            float moveTimer = 0;
            
            Vector3 startPosition = transform.position;
            
            while (moveTimer < moveDuration && isWandering)
            {
                // Check if we'd exceed max wander radius with next movement
                Vector3 nextPosition = Vector3.Lerp(startPosition, targetWaypoint, (moveTimer + Time.deltaTime) / moveDuration);
                float distanceFromSpawn = Vector3.Distance(
                    new Vector3(nextPosition.x, 0, nextPosition.z), 
                    new Vector3(spawnPosition.x, 0, spawnPosition.z)
                );
                
                if (restrictToSpawnArea && distanceFromSpawn > maxWanderRadius * 0.95f)
                {
                    Debug.LogWarning($"{gameObject.name}: Reached wander radius border - turning back");
                    
                    // Instead of just stopping, turn toward the spawn point
                    Vector3 directionToSpawn = (spawnPosition - transform.position).normalized;
                    directionToSpawn.y = 0;
                    
                    // Set a new waypoint in the direction of the spawn point
                    Vector3 newWaypoint = spawnPosition + (directionToSpawn * -1 * Random.Range(minWanderDistance, maxWanderRadius * 0.7f));
                    
                    // Turn toward spawn
                    StartCoroutine(TurnAndMoveInward());
                    
                    yield break; // Exit this coroutine - the TurnAndMoveInward will start a new wandering sequence
                }
                
                // Standard movement with forced y position
                float step = livingEntity.moveSpeed * Time.deltaTime;
                
                if (livingEntity != null)
                {
                    livingEntity.MoveInDirection(transform.forward, 1.0f);
                    // Force Y position to remain constant
                    Vector3 pos = transform.position;
                    pos.y = spawnPosition.y;
                    transform.position = pos;
                }
                else
                {
                    Vector3 movement = transform.forward * step;
                    movement.y = 0; // Zero out any vertical movement
                    transform.position += movement;
                }
                
                moveTimer += Time.deltaTime;
                
                // Check if we've reached the waypoint - use horizontal distance only
                float horizontalDistance = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(targetWaypoint.x, 0, targetWaypoint.z)
                );
                if (horizontalDistance < waypointReachedDistance)
                    break;
                    
                yield return null;
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
    
    private Vector3 GetSafeWaypoint()
    {
        // Try up to 10 random directions
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // Generate random direction
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            
            // Generate random distance - use smaller distances when closer to the border
            float distanceFromSpawn = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            float maxDistanceMultiplier = 1f;
            
            // If we're already near the border, use shorter distances
            if (distanceFromSpawn > maxWanderRadius * 0.7f)
            {
                // Scale down max distance as we get closer to border
                maxDistanceMultiplier = Mathf.Lerp(0.8f, 0.3f, 
                    (distanceFromSpawn - maxWanderRadius * 0.7f) / (maxWanderRadius * 0.3f));
                
                // Prefer directions toward center when near border
                Vector3 towardsCenter = (spawnPosition - transform.position).normalized;
                randomDirection = Vector3.Slerp(randomDirection, towardsCenter, 0.7f);
            }
            
            float randomDistance = Random.Range(
                minWanderDistance, 
                Mathf.Min(maxWanderDistance, maxWanderRadius * maxDistanceMultiplier)
            );
            
            // Calculate potential waypoint - but force Y to match spawn point
            Vector3 potentialWaypoint = transform.position + randomDirection * randomDistance;
            potentialWaypoint.y = spawnPosition.y; // Force same Y position as spawn
            
            // Spawn area check
            if (restrictToSpawnArea)
            {
                // Use horizontal distance only for comparison
                float waypointDistanceFromSpawn = Vector2.Distance(
                    new Vector2(potentialWaypoint.x, potentialWaypoint.z),
                    new Vector2(spawnPosition.x, spawnPosition.z)
                );
                
                if (waypointDistanceFromSpawn > maxWanderRadius * 0.85f) // Stay within 85% of radius for safety margin
                {
                    // Point is outside spawn radius, adjust it
                    Vector3 directionFromSpawn = (potentialWaypoint - spawnPosition).normalized;
                    directionFromSpawn.y = 0;
                    
                    // Reduce radius by 15% for safety
                    potentialWaypoint = spawnPosition + directionFromSpawn * (maxWanderRadius * 0.85f);
                    potentialWaypoint.y = spawnPosition.y;
                    
                    // Make sure we don't go below minimum distance
                    if (Vector3.Distance(transform.position, potentialWaypoint) < minWanderDistance * 0.5f)
                    {
                        // Skip this attempt if it's too close to current position
                        continue;
                    }
                }
            }
            
            // Check for obstacles if enabled
            if (avoidObstacles)
            {
                bool hasObstacle = false;
                Vector3 direction = (potentialWaypoint - transform.position).normalized;
                
                // Cast rays in multiple directions to detect obstacles
                for (int i = -1; i <= 1; i++)
                {
                    float angle = i * 30f; // Check at -30, 0, and 30 degrees
                    Vector3 rayDir = Quaternion.Euler(0, angle, 0) * direction;
                    
                    if (Physics.Raycast(transform.position + Vector3.up * 0.5f, rayDir, 
                        Vector3.Distance(transform.position, potentialWaypoint), obstacleLayer))
                    {
                        hasObstacle = true;
                        break;
                    }
                }
                
                if (hasObstacle)
                    continue; // Skip this waypoint, try again
            }
            
            // If we got here, the waypoint is valid
            return potentialWaypoint;
        }
        
        // Fallback: if all else fails, move toward center of spawn area
        Vector3 towardsCenter = (spawnPosition - transform.position).normalized;
        towardsCenter.y = 0;
        Vector3 safeWaypoint = transform.position + towardsCenter * (minWanderDistance * 0.8f);
        safeWaypoint.y = spawnPosition.y;
        
        Debug.DrawLine(transform.position, safeWaypoint, Color.yellow, 3f);
        return safeWaypoint;
    }

    // Force stop all movement and wandering
    public void ForceStop()
    {
        // Stop all coroutines
        StopAllCoroutines();
        
        // Disable wandering
        isWandering = false;
        isWaiting = true;
        
        // Update animation to idle
        UpdateAnimationState(false);
        
        // Stop physical movement if there's a Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }
        
        Debug.Log($"{gameObject.name}: Forced to stop wandering");
    }

    // Force restart the wandering behavior
    public void ForceRestartWandering()
    {
        // Stop any current routines
        StopAllCoroutines();
        
        // Enable wandering
        isWaiting = false;
        isWandering = true;
        
        // Update animation
        UpdateAnimationState(true);
        
        // Restart the wandering routine
        StartCoroutine(WanderingRoutine());
        
        Debug.Log($"{gameObject.name}: Forced to restart wandering");
    }

    // Add a method to force a specific waypoint
    public void ForceNewWaypointInDirection(Vector3 direction)
    {
        // Stop current movement
        StopAllCoroutines();
        
        // Get a safe distance
        float distance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Force horizontal direction
        direction.y = 0;
        direction.Normalize();
        
        // Calculate target position with forced Y coordinate
        Vector3 targetWaypoint = transform.position + direction.normalized * distance;
        targetWaypoint.y = spawnPosition.y; // Force Y position
        
        // Restrict to spawn area if needed
        if (restrictToSpawnArea)
        {
            float distanceFromSpawn = Vector2.Distance(
                new Vector2(targetWaypoint.x, targetWaypoint.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            if (distanceFromSpawn > maxWanderRadius * 0.85f)
            {
                Vector3 directionFromSpawn = (targetWaypoint - spawnPosition).normalized;
                directionFromSpawn.y = 0;
                targetWaypoint = spawnPosition + directionFromSpawn * (maxWanderRadius * 0.85f);
                targetWaypoint.y = spawnPosition.y;
            }
        }
        
        // Log for debugging
        Debug.Log($"{gameObject.name}: Forced new waypoint in direction {direction}");
        
        // Start moving to this waypoint
        StartCoroutine(MoveToWaypointRoutine(targetWaypoint));
    }
    
    // Helper routine to move to a specific waypoint
    private IEnumerator MoveToWaypointRoutine(Vector3 waypoint)
    {
        // Turn toward waypoint
        Vector3 directionToWaypoint = (waypoint - transform.position).normalized;
        directionToWaypoint.y = 0; // Ensure horizontal movement
        
        float turnDuration = 0.3f;
        float turnTimer = 0;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
        
        while (turnTimer < turnDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer / turnDuration);
            turnTimer += Time.deltaTime;
            yield return null;
        }
        
        // Set to exact rotation
        transform.rotation = targetRotation;
        
        // Enter moving state
        isWaiting = false;
        isWandering = true;
        UpdateAnimationState(true);
        
        // Movement logic
        float distanceToWaypoint = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(waypoint.x, waypoint.z)
        );
        float moveDuration = distanceToWaypoint / livingEntity.moveSpeed;
        float moveTimer = 0;
        
        while (moveTimer < moveDuration && isWandering)
        {
            // Check for spawn area boundary
            if (restrictToSpawnArea)
            {
                Vector3 nextPosition = Vector3.Lerp(transform.position, waypoint, 
                    (moveTimer + Time.deltaTime) / moveDuration);
                
                float distanceFromSpawn = Vector2.Distance(
                    new Vector2(nextPosition.x, nextPosition.z),
                    new Vector2(spawnPosition.x, spawnPosition.z)
                );
                
                if (distanceFromSpawn > maxWanderRadius)
                {
                    Debug.LogWarning($"{gameObject.name}: Would exceed spawn radius, stopping");
                    break;
                }
            }
            
            // Move step
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(transform.forward, 1.0f);
                // Force Y position
                Vector3 pos = transform.position;
                pos.y = spawnPosition.y;
                transform.position = pos;
            }
            else
            {
                float step = 2f * Time.deltaTime; // Fallback speed
                Vector3 movement = transform.forward * step;
                movement.y = 0; // Zero out vertical movement
                transform.position += movement;
            }
            
            moveTimer += Time.deltaTime;
            
            // Check if arrived - use horizontal distance
            float horizontalDistance = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(waypoint.x, waypoint.z)
            );
            
            if (horizontalDistance < waypointReachedDistance)
                break;
                
            yield return null;
        }
        
        // Return to normal wandering
        StartCoroutine(WanderingRoutine());
    }

    // Force start wandering but begin with idle state
    public void ForceStartWithIdle()
    {
        // Stop any current routines
        StopAllCoroutines();
        
        // Start in idle state
        isWaiting = true;
        isWandering = false;
        
        // Update animation
        UpdateAnimationState(false);
        
        // Wait briefly then start normal wandering
        StartCoroutine(IdleThenWanderRoutine());
        
        Debug.Log($"{gameObject.name}: Forced to start wandering with idle first");
    }

    // Helper routine to start with idle then transition to wandering
    private IEnumerator IdleThenWanderRoutine()
    {
        // Wait in idle state
        float idleTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(idleTime);
        
        // Then start normal wandering
        StartCoroutine(WanderingRoutine());
    }

    // Add a public method to force setting a new waypoint
    public void ForceNewWaypoint()
    {
        // Calculate a random direction
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        
        // Use the direction-based method
        ForceNewWaypointInDirection(randomDirection);
    }

    // Add this new method to the AIWandering class
    public void ForceStartWithDirection(Vector3 direction)
    {
        // Use the existing method
        ForceNewWaypointInDirection(direction);
    }

    // Helper for editor visualization
    private void OnDrawGizmosSelected()
    {
        if (showSpawnAreaGizmo && restrictToSpawnArea)
        {
            // Draw the spawn area in yellow
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Transparent yellow
            
            // In editor, use current position; in play mode, use stored spawn position
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            
            // Draw a disc for the spawn area
            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.3f);
            UnityEditor.Handles.DrawSolidDisc(center, Vector3.up, maxWanderRadius);
            
            // Draw the outline
            Gizmos.color = Color.yellow;
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(center, Vector3.up, maxWanderRadius);
        }
    }

    // Add this property to check if currently moving
    public bool IsCurrentlyMoving()
    {
        return isWandering && !isWaiting;
    }

    // Add this method to enable/disable wandering behavior
    public void SetWanderingEnabled(bool enabled)
    {
        if (enabled)
        {
            // If we're enabling wandering and it's not already running
            if (!isWandering && !isWaiting)
            {
                // Stop any current routines
                StopAllCoroutines();
                
                // Start wandering
                StartCoroutine(WanderingRoutine());
            }
        }
        else
        {
            // If we're disabling wandering
            if (isWandering || isWaiting)
            {
                // Stop all wandering coroutines
                StopAllCoroutines();
                
                // Set states to disabled
                isWandering = false;
                isWaiting = false;
                
                // Update animation to idle
                UpdateAnimationState(false);
            }
        }
    }

    // Add a new method for the turn-back behavior
    private IEnumerator TurnAndMoveInward()
    {
        // Calculate direction towards spawn (center of wander area)
        Vector3 directionToSpawn = (spawnPosition - transform.position).normalized;
        directionToSpawn.y = 0;
        directionToSpawn.Normalize();
        
        // Pick a random angle variation to avoid always heading straight back
        float randomAngle = Random.Range(-60f, 60f);
        Vector3 finalDirection = Quaternion.Euler(0, randomAngle, 0) * directionToSpawn;
        
        // Get a new distance (between 30-70% of max radius)
        float turnBackDistance = maxWanderRadius * Random.Range(0.3f, 0.7f);
        Vector3 newWaypoint = transform.position + finalDirection * turnBackDistance;
        
        // Make sure the new waypoint is within our wander radius
        float distanceFromSpawn = Vector3.Distance(newWaypoint, spawnPosition);
        if (distanceFromSpawn > maxWanderRadius * 0.8f)
        {
            // Adjust to ensure we stay within radius
            newWaypoint = spawnPosition + (newWaypoint - spawnPosition).normalized * (maxWanderRadius * 0.8f);
        }
        
        // Ensure Y position is maintained
        newWaypoint.y = transform.position.y;
        
        // Visual feedback for debugging
        Debug.DrawLine(transform.position, newWaypoint, Color.red, 3f);
        
        // Turn toward the new direction
        float turnDuration = 0.3f;
        float turnTimer = 0;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
        
        // Perform the turn
        while (turnTimer < turnDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer / turnDuration);
            turnTimer += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final rotation is exact
        transform.rotation = targetRotation;
        
        // Move a short distance in the new direction
        float moveTime = 0.5f;
        float moveTimer = 0;
        Vector3 startPos = transform.position;
        
        while (moveTimer < moveTime)
        {
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(transform.forward, 1.0f);
                // Keep Y position constant
                Vector3 pos = transform.position;
                pos.y = spawnPosition.y;
                transform.position = pos;
            }
            
            moveTimer += Time.deltaTime;
            yield return null;
        }
        
        // Now restart the normal wandering behavior
        StartCoroutine(WanderingRoutine());
    }
}