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

    [Header("Border Avoidance")]
    [SerializeField] private float borderAvoidanceDistance = 5f; // How far to stay away from borders
    [SerializeField] private LayerMask borderLayer; // Layer for the map borders

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
        
        // CRITICAL: Always enforce boundary limits, even in Update
        EnforceBoundaryLimits();
    }
    
    private IEnumerator WanderingRoutine()
    {
        // Wait for physics to settle before starting movement
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        
        // Use raycast to find actual ground position
        RaycastHit hit;
        float groundY = transform.position.y;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 5f, LayerMask.GetMask("Default", "Ground")))
        {
            groundY = hit.point.y;
            // Immediately snap to the ground
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
        }
        
        // Store the proper ground Y position
        float fixedYPosition = groundY;
        
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

            // Check if we're too close to a border
            if (IsPositionTooCloseToMapBorder(transform.position))
            {
                // If too close to border, immediately get a safe waypoint
                Vector3 safeWaypoint = GetSafeWaypoint();
                Vector3 directionToSafe = (safeWaypoint - transform.position).normalized;
                
                // Turn and move away from border
                float turnDuration = 0.5f;
                float turnTimer = 0;
                Quaternion startRotation = transform.rotation;
                Quaternion targetRotation = Quaternion.LookRotation(directionToSafe);
                
                while (turnTimer < turnDuration)
                {
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer / turnDuration);
                    turnTimer += Time.deltaTime;
                    yield return null;
                }
                
                // Move away from border
                isWandering = true;
                UpdateAnimationState(true);
                
                while (IsPositionTooCloseToMapBorder(transform.position))
                {
                    if (livingEntity != null)
                    {
                        livingEntity.MoveInDirection(transform.forward, 1.0f);
                    }
                    yield return null;
                }
                
                continue; // Restart the wandering routine
            }

            // --- WAITING PHASE ---
            isWaiting = true;
            isWandering = false;
            UpdateAnimationState(false);
            
            // Ensure Y position is maintained
            Vector3 pos = transform.position;
            pos.y = fixedYPosition;
            transform.position = pos;
            
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // Skip if entity died during waiting
            if (livingEntity != null && livingEntity.IsDead)
                continue;
            
            // --- WAYPOINT SELECTION PHASE ---
            Vector3 targetWaypoint = GetSafeWaypoint();
            targetWaypoint.y = fixedYPosition; // Use the stored Y position
            
            // --- TURNING PHASE ---
            Vector3 directionToWaypoint = (targetWaypoint - transform.position).normalized;
            directionToWaypoint.y = 0;
            
            float turnDuration = 0.5f;
            float turnTimer = 0;
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
            
            while (turnTimer < turnDuration)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer / turnDuration);
                turnTimer += Time.deltaTime;
                
                // Ensure Y position is maintained during turning
                pos = transform.position;
                if (pos.y != fixedYPosition)
                {
                    pos.y = fixedYPosition;
                    transform.position = pos;
                }
                
                yield return null;
            }
            
            transform.rotation = targetRotation;
            
            // --- MOVEMENT PHASE ---
            isWaiting = false;
            isWandering = true;
            UpdateAnimationState(true);
            
            while (isWandering)
            {
                // Calculate next position BEFORE moving
                Vector3 nextPosition = transform.position + transform.forward * (livingEntity.moveSpeed * Time.deltaTime);
                float nextDistanceFromSpawn = Vector2.Distance(
                    new Vector2(nextPosition.x, nextPosition.z),
                    new Vector2(spawnPosition.x, spawnPosition.z)
                );
                
                // If next position would exceed border, stop immediately without repositioning
                if (restrictToSpawnArea && nextDistanceFromSpawn >= maxWanderRadius * 0.98f)
                {
                    StartCoroutine(TurnAndMoveInward());
                    yield break; // Exit this coroutine
                }
                
                // Only move if we're not going to hit the border
                if (livingEntity != null)
                {
                    livingEntity.MoveInDirection(transform.forward, 1.0f);
                    
                    // Force Y position to remain exactly at the fixed Y
                    pos = transform.position;
                    pos.y = fixedYPosition;
                    transform.position = pos;
                }
                
                // Check if we've been walking for too long (random duration)
                if (Random.value < 0.005f) // Small chance each frame to stop and change direction
                {
                    break; // Exit movement loop to get a new waypoint
                }
                
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
    
    private Vector3 GetSafeWaypoint(float customRadius = 0f)
    {
        // Use a much more conservative radius (80% of max) to ensure we stay well away from the border
        float effectiveRadius = (customRadius > 0 ? customRadius : maxWanderRadius) * 0.8f;
        
        // Try up to 15 random directions (increased attempts for better results)
        for (int attempt = 0; attempt < 15; attempt++)
        {
            // Generate random direction
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            
            // Generate random distance
            float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
            
            // Calculate potential waypoint
            Vector3 potentialWaypoint = transform.position + randomDirection * randomDistance;
            potentialWaypoint.y = spawnPosition.y;
            
            // Check if waypoint is too close to any border
            if (!IsPositionTooCloseToMapBorder(potentialWaypoint))
            {
                // Also check the path to the waypoint
                Vector3 directionToWaypoint = (potentialWaypoint - transform.position).normalized;
                float distanceToWaypoint = Vector3.Distance(transform.position, potentialWaypoint);
                
                // Check a few points along the path
                bool pathIsSafe = true;
                for (float d = 2f; d < distanceToWaypoint; d += 2f)
                {
                    Vector3 pathPoint = transform.position + directionToWaypoint * d;
                    if (IsPositionTooCloseToMapBorder(pathPoint))
                    {
                        pathIsSafe = false;
                        break;
                    }
                }
                
                if (pathIsSafe)
                {
                    return potentialWaypoint;
                }
            }
        }
        
        // If we couldn't find a safe waypoint, move toward center of current area
        Vector3 awayFromBorder = -Physics.ClosestPoint(transform.position).normalized;
        return transform.position + awayFromBorder * minWanderDistance;
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
        
        
        // Start moving to this waypoint
        StartCoroutine(MoveToWaypointRoutine(targetWaypoint));
    }
    
    // Helper routine to move to a specific waypoint
    private IEnumerator MoveToWaypointRoutine(Vector3 waypoint)
    {
        // Turn toward waypoint
        Vector3 directionToWaypoint = (waypoint - transform.position).normalized;
        directionToWaypoint.y = 0;
        
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
        
        transform.rotation = targetRotation;
        
        // Enter moving state
        isWaiting = false;
        isWandering = true;
        UpdateAnimationState(true);
        
        float distanceToWaypoint = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(waypoint.x, waypoint.z)
        );
        float moveDuration = distanceToWaypoint / livingEntity.moveSpeed;
        float moveTimer = 0;
        
        while (moveTimer < moveDuration && isWandering)
        {
            // Check for spawn area boundary - use stricter threshold
            if (restrictToSpawnArea)
            {
                Vector3 nextPosition = Vector3.Lerp(transform.position, waypoint, 
                    (moveTimer + Time.deltaTime) / moveDuration);
                
                float distanceFromSpawn = Vector2.Distance(
                    new Vector2(nextPosition.x, nextPosition.z),
                    new Vector2(spawnPosition.x, spawnPosition.z)
                );
                
                if (distanceFromSpawn > maxWanderRadius * 0.9f)
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
                float step = 2f * Time.deltaTime;
                Vector3 movement = transform.forward * step;
                movement.y = 0;
                transform.position += movement;
            }
            
            // CRITICAL: Enforce boundary after every movement step
            EnforceBoundaryLimits();
            
            moveTimer += Time.deltaTime;
            
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

    // Modify the TurnAndMoveInward method to strictly maintain Y position
    private IEnumerator TurnAndMoveInward()
    {
        // Store the current Y position at the start
        float fixedYPosition = transform.position.y;
        
        // Stop moving first without rotation
        isWandering = false;
        isWaiting = true;
        
        // Update animation to idle
        UpdateAnimationState(false);
        
        // Store initial rotation
        Quaternion initialRotation = transform.rotation;
        
        // Ensure Y position is maintained
        Vector3 pos = transform.position;
        pos.y = fixedYPosition;
        transform.position = pos;
        
        // Wait for random time between 1-2 seconds
        float idleTime = Random.Range(1.0f, 2.0f);
        
        // Check Y position every frame during waiting
        float waitTimer = 0;
        while (waitTimer < idleTime)
        {
            // Ensure Y position is maintained during waiting
            pos = transform.position;
            if (pos.y != fixedYPosition)
            {
                pos.y = fixedYPosition;
                transform.position = pos;
            }
            
            // Keep initial rotation during waiting
            transform.rotation = initialRotation;
            
            waitTimer += Time.deltaTime;
            yield return null;
        }
        
        // Select a new waypoint well inside the wander area (30-70% of radius)
        float newWaypointDistance = maxWanderRadius * Random.Range(0.3f, 0.7f);
        
        // Pick a random direction that points somewhat inward
        Vector3 directionToSpawn = (spawnPosition - transform.position).normalized;
        directionToSpawn.y = 0;
        
        // Reduce the random angle range for more natural turning
        float randomAngle = Random.Range(-45f, 45f);
        Vector3 newDirection = Quaternion.Euler(0, randomAngle, 0) * directionToSpawn;
        
        // Calculate new waypoint
        Vector3 newWaypoint = spawnPosition + newDirection * newWaypointDistance;
        newWaypoint.y = fixedYPosition;
        
        // Visual feedback
        Debug.DrawLine(transform.position, newWaypoint, Color.green, 3f);
        
        // Now rotate to face the new waypoint
        Vector3 directionToWaypoint = (newWaypoint - transform.position).normalized;
        directionToWaypoint.y = 0;
        
        float turnDuration = 0.5f;
        float turnTimer = 0;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
        
        // Animate the turn
        while (turnTimer < turnDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer / turnDuration);
            turnTimer += Time.deltaTime;
            
            // Ensure Y position is maintained during turning
            pos = transform.position;
            if (pos.y != fixedYPosition)
            {
                pos.y = fixedYPosition;
                transform.position = pos;
            }
            
            yield return null;
        }
        
        // Ensure exact final rotation
        transform.rotation = targetRotation;
        
        // Start walking to the new waypoint
        isWaiting = false;
        isWandering = true;
        UpdateAnimationState(true);
        
        // Move toward the new waypoint
        float distanceToWaypoint = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(newWaypoint.x, newWaypoint.z)
        );
        
        float moveDuration = distanceToWaypoint / livingEntity.moveSpeed;
        float moveTimer = 0;
        
        while (moveTimer < moveDuration && isWandering)
        {
            // Move step
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(transform.forward, 1.0f);
                
                // Force Y position to remain exactly at the fixed Y
                pos = transform.position;
                pos.y = fixedYPosition;
                transform.position = pos;
            }
            
            moveTimer += Time.deltaTime;
            
            // Check if we've reached the waypoint
            float horizontalDistance = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(newWaypoint.x, newWaypoint.z)
            );
            
            if (horizontalDistance < waypointReachedDistance)
                break;
                
            yield return null;
        }
        
        // Return to normal wandering
        StartCoroutine(WanderingRoutine());
    }

    // Modify EnforceBoundaryLimits to be less aggressive and only apply in extreme cases
    private void EnforceBoundaryLimits()
    {
        if (!restrictToSpawnArea)
            return;
        
        // Calculate current distance from spawn
        float currentDistanceFromSpawn = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(spawnPosition.x, spawnPosition.z)
        );
        
        // Only apply correction in extreme cases (5% beyond max radius)
        if (currentDistanceFromSpawn > maxWanderRadius * 1.05f)
        {
            // Calculate direction from spawn to current position
            Vector3 directionFromSpawn = (transform.position - spawnPosition).normalized;
            directionFromSpawn.y = 0;
            
            // Calculate new position exactly at the boundary
            Vector3 correctedPosition = spawnPosition + directionFromSpawn * maxWanderRadius;
            correctedPosition.y = spawnPosition.y; // Maintain Y position
            
            // Immediately set position to the corrected position
            transform.position = correctedPosition;
            
            // Log the correction
            Debug.LogWarning($"{gameObject.name}: Emergency boundary correction applied");
        }
    }

    // Add this method to check if a position is too close to any border
    private bool IsPositionTooCloseToMapBorder(Vector3 position)
    {
        // Check for borders using a sphere overlap
        Collider[] borderColliders = Physics.OverlapSphere(position, borderAvoidanceDistance, borderLayer);
        return borderColliders.Length > 0;
    }
}