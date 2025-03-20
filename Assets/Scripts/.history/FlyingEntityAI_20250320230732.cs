using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEntityAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotationSpeed = 3.0f;
    [SerializeField] private float fixedHeight = 2.0f; // Fixed height for flying
    
    [Header("Wandering Settings")]
    [SerializeField] private float minWanderDistance = 3f;
    [SerializeField] private float maxWanderDistance = 8f;
    [SerializeField] private float waypointReachedDistance = 1.0f;
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;
    [SerializeField] private float boundaryRotationDuration = 0.8f; // Longer duration for smoother rotation
    [SerializeField] private float boundaryPauseDuration = 0.5f; // Time to pause at boundary before moving again
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    [Header("Spawn Area Restriction")]
    [SerializeField] private bool restrictToSpawnArea = true;
    [SerializeField] private float maxWanderRadius = 10f;
    [SerializeField] private bool showSpawnAreaGizmo = true;
    
    // Internal variables
    private bool isWandering = false;
    private bool isWaiting = false;
    private Vector3 currentWaypoint;
    private Vector3 spawnPosition;
    private Coroutine wanderingCoroutine;
    private bool isHandlingBoundary = false;
    
    // Add these variables for smoother boundary handling
    private Vector3 currentVelocity;
    private float boundaryHandlingDelayTimer = 0f;
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
        
        // Store spawn position
        spawnPosition = transform.position;
        
        // Set initial height to fixed value
        Vector3 pos = transform.position;
        pos.y = fixedHeight;
        transform.position = pos;
        
        // Always set flying animation from the start
        if (animController != null)
        {
            animController.SetWalking(true);
        }
        
        // Start wandering
        StartWandering();
    }
    
    private void OnDrawGizmosSelected()
    {
        if (showSpawnAreaGizmo && restrictToSpawnArea)
        {
            // Draw the spawn area in cyan
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Transparent cyan
            
            // In editor, use current position; in play mode, use stored spawn position
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            
#if UNITY_EDITOR
            // Draw a flat circle for the wander area
            UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.1f);
            UnityEditor.Handles.DrawSolidDisc(center, Vector3.up, maxWanderRadius);
            
            // Draw the outline
            Gizmos.color = Color.cyan;
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(center, Vector3.up, maxWanderRadius);
            
            // Draw height line at fixed height
            Vector3 fixedHeightPos = center + Vector3.up * fixedHeight;
            UnityEditor.Handles.DrawDottedLine(center, fixedHeightPos, 2f);
            UnityEditor.Handles.DrawWireDisc(fixedHeightPos, Vector3.up, 1f);
#else
            // Fallback for builds - just draw a wire sphere
            Gizmos.DrawWireSphere(center, maxWanderRadius);
#endif
        }
    }
    
    private void Update()
    {
        // Skip if entity is dead
        if (livingEntity != null && livingEntity.IsDead)
        {
            if (wanderingCoroutine != null)
            {
                StopCoroutine(wanderingCoroutine);
                wanderingCoroutine = null;
            }
            return;
        }
        
        // Always enforce fixed height
        MaintainFixedHeight();
        
        // Always enforce boundary limits (if not already handling a boundary)
        if (!isHandlingBoundary)
        {
            EnforceBoundaryLimits();
        }
    }
    
    private void MaintainFixedHeight()
    {
        // Get current position
        Vector3 currentPos = transform.position;
        
        // If not at fixed height, correct it
        if (currentPos.y != fixedHeight)
        {
            currentPos.y = fixedHeight;
            transform.position = currentPos;
        }
    }
    
    private void EnforceBoundaryLimits()
    {
        if (!restrictToSpawnArea || isHandlingBoundary) return;
        
        // Don't check again too soon after handling a boundary
        if (boundaryHandlingDelayTimer > 0f)
        {
            boundaryHandlingDelayTimer -= Time.deltaTime;
            return;
        }
        
        // Get horizontal distance from spawn (ignoring Y)
        Vector2 currentPosFlat = new Vector2(transform.position.x, transform.position.z);
        Vector2 spawnPosFlat = new Vector2(spawnPosition.x, spawnPosition.z);
        float distanceFromSpawn = Vector2.Distance(currentPosFlat, spawnPosFlat);
        
        // If beyond boundary, handle it more smoothly
        if (distanceFromSpawn > maxWanderRadius * 0.95f)
        {
            // Calculate direction from spawn to current position (outward direction)
            Vector2 directionFromSpawn = (currentPosFlat - spawnPosFlat).normalized;
            
            // Gently move back inside boundary instead of teleporting
            Vector2 newPosFlat = spawnPosFlat + directionFromSpawn * (maxWanderRadius * 0.9f);
            Vector3 newPosition = new Vector3(newPosFlat.x, fixedHeight, newPosFlat.y);
            
            // Transition to the boundary position over a few frames
            transform.position = Vector3.Lerp(transform.position, newPosition, 0.4f);
            
            // Only start boundary handling if not already in progress
            if (!isHandlingBoundary && wanderingCoroutine != null)
            {
                Debug.Log("Entity hit boundary - starting smooth handling");
                StopCoroutine(wanderingCoroutine);
                wanderingCoroutine = null;
                StartCoroutine(SmoothBoundaryCollisionHandling(directionFromSpawn));
            }
        }
    }
    
    // Improved boundary collision handling
    private IEnumerator SmoothBoundaryCollisionHandling(Vector2 outwardDirection)
    {
        isHandlingBoundary = true;
        
        // First, ensure we're completely stopped
        float stopDuration = 0.3f;
        float stopTimer = 0f;
        
        // Record position at start of stop
        Vector3 positionAtStop = transform.position;
        
        // Complete stop phase
        while (stopTimer < stopDuration)
        {
            stopTimer += Time.deltaTime;
            
            // Hold position exactly during stop
            transform.position = positionAtStop;
            
            yield return null;
        }
        
        // Short pause before rotation
        yield return new WaitForSeconds(0.2f);
        
        // Calculate the inward direction (reverse of outward)
        Vector3 inwardDirection = new Vector3(-outwardDirection.x, 0, -outwardDirection.y);
        
        // Get current rotation
        Quaternion startRotation = transform.rotation;
        
        // Calculate target rotation to face away from the boundary
        Quaternion targetRotation = Quaternion.LookRotation(inwardDirection);
        
        // Rotate to face away from boundary (slowly)
        float elapsed = 0f;
        while (elapsed < boundaryRotationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / boundaryRotationDuration;
            
            // Use smooth step for more natural rotation
            float easedProgress = Mathf.SmoothStep(0, 1, progress);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedProgress);
            
            // Hold position during rotation
            transform.position = positionAtStop;
            
            yield return null;
        }
        
        // Ensure exact final rotation
        transform.rotation = targetRotation;
        
        // Pause briefly after rotation before moving
        yield return new WaitForSeconds(boundaryPauseDuration);
        
        // Generate a new waypoint in the direction we're now facing
        float safeDistance = Mathf.Min(minWanderDistance, maxWanderRadius * 0.5f);
        float newDistance = Random.Range(safeDistance, maxWanderDistance * 0.7f);
        currentWaypoint = transform.position + transform.forward * newDistance;
        currentWaypoint.y = fixedHeight;
        
        // Debug visualization for the new waypoint
        Debug.DrawLine(transform.position, currentWaypoint, Color.green, 3f);
        
        // Set a delay before we check boundaries again
        boundaryHandlingDelayTimer = 1.5f;
        
        // Resume normal wandering
        isHandlingBoundary = false;
        wanderingCoroutine = StartCoroutine(WanderingRoutine(false)); // Resume from movement phase
    }
    
    public void StartWandering()
    {
        if (wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
        }
        
        wanderingCoroutine = StartCoroutine(WanderingRoutine(true)); // Start from beginning
    }
    
    private IEnumerator WanderingRoutine(bool startFromBeginning = true)
    {
        while (true)
        {
            // Skip if entity is dead
            if (livingEntity != null && livingEntity.IsDead)
            {
                isWandering = false;
                isWaiting = false;
                yield break;
            }
            
            // --- WAITING PHASE ---
            if (startFromBeginning)
            {
                isWaiting = true;
                isWandering = false;
                
                // Always maintain the walking animation even when waiting
                if (animController != null)
                {
                    animController.SetWalking(true);
                }
                
                // Maintain fixed height
                MaintainFixedHeight();
                
                float waitTime = Random.Range(minWaitTime, maxWaitTime);
                yield return new WaitForSeconds(waitTime);
                
                // Skip if entity died during waiting
                if (livingEntity != null && livingEntity.IsDead)
                    continue;
                
                // --- WAYPOINT SELECTION PHASE ---
                Vector3 newWaypoint = GetRandomWaypoint();
                currentWaypoint = newWaypoint;
                
                // --- TURNING PHASE ---
                Vector3 directionToWaypoint = (currentWaypoint - transform.position).normalized;
                
                float turnDuration = 0.5f;
                float turnTimer = 0;
                Quaternion startRotation = transform.rotation;
                Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
                
                while (turnTimer < turnDuration)
                {
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer / turnDuration);
                    turnTimer += Time.deltaTime;
                    
                    // Maintain fixed height during turning
                    MaintainFixedHeight();
                    
                    yield return null;
                }
                
                // Ensure final rotation
                transform.rotation = targetRotation;
            }
            
            // --- MOVEMENT PHASE ---
            isWaiting = false;
            isWandering = true;
            startFromBeginning = true; // Reset for next iteration
            
            while (isWandering && !isHandlingBoundary)
            {
                // Calculate distance to waypoint (using XZ only)
                float distanceToWaypoint = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(currentWaypoint.x, currentWaypoint.z)
                );
                
                // Check if reached waypoint
                if (distanceToWaypoint < waypointReachedDistance)
                {
                    break; // Exit movement loop to get a new waypoint
                }
                
                // Move toward waypoint
                MoveTowardWaypoint();
                
                // Maintain fixed height during movement
                MaintainFixedHeight();
                
                // Check if we've been flying for too long
                if (Random.value < 0.002f) // Small chance each frame to change waypoint
                {
                    break; // Exit movement loop to get a new waypoint
                }
                
                yield return null;
            }
            
            // If we're handling a boundary collision, exit this coroutine
            if (isHandlingBoundary)
            {
                yield break;
            }
        }
    }
    
    private void MoveTowardWaypoint()
    {
        // Skip movement if handling boundary
        if (isHandlingBoundary) return;
        
        // Direction to waypoint (XZ plane only)
        Vector3 targetPos = new Vector3(currentWaypoint.x, transform.position.y, currentWaypoint.z);
        Vector3 direction = (targetPos - transform.position).normalized;
        
        // Rotate toward waypoint (smoother rotation)
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        
        // Apply movement based on current facing direction
        if (livingEntity != null)
        {
            // Use the LivingEntity for movement
            livingEntity.MoveInDirection(transform.forward, 1.0f);
            
            // Ensure we maintain fixed height after movement
            Vector3 pos = transform.position;
            pos.y = fixedHeight;
            transform.position = pos;
        }
        else
        {
            // Fallback movement with smoother acceleration
            Vector3 targetVelocity = transform.forward * moveSpeed;
            Vector3 newPos = transform.position + targetVelocity * Time.deltaTime;
            newPos.y = fixedHeight; // Ensure fixed height
            transform.position = newPos;
        }
    }
    
    private Vector3 GetRandomWaypoint()
    {
        // Start with random direction in 2D (XZ plane)
        Vector2 randomDirection2D = Random.insideUnitCircle.normalized;
        Vector3 randomDirection = new Vector3(randomDirection2D.x, 0, randomDirection2D.y);
        
        // Generate random distance
        float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Calculate base position (at fixed height)
        Vector3 baseWaypoint = transform.position + randomDirection * randomDistance;
        baseWaypoint.y = fixedHeight; // Always use fixed height
        
        // If restricting to spawn area, ensure waypoint is within radius
        if (restrictToSpawnArea)
        {
            // Check horizontal distance from spawn
            Vector2 waypointFlat = new Vector2(baseWaypoint.x, baseWaypoint.z);
            Vector2 spawnFlat = new Vector2(spawnPosition.x, spawnPosition.z);
            float distanceFromSpawn = Vector2.Distance(waypointFlat, spawnFlat);
            
            // If beyond boundary, adjust the position
            if (distanceFromSpawn > maxWanderRadius * 0.8f) // 80% of radius to stay away from edge
            {
                // Direction from spawn to waypoint (2D)
                Vector2 directionFromSpawn = (waypointFlat - spawnFlat).normalized;
                
                // New 2D position within boundary
                Vector2 newWaypointFlat = spawnFlat + directionFromSpawn * (maxWanderRadius * 0.7f);
                
                // Update the waypoint with corrected X,Z coordinates
                baseWaypoint.x = newWaypointFlat.x;
                baseWaypoint.z = newWaypointFlat.y;
            }
        }
        
        return baseWaypoint;
    }
    
    // Public methods to control the flying entity
    
    public void StopWandering()
    {
        if (wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
            wanderingCoroutine = null;
        }
        
        isWandering = false;
        isWaiting = false;
        isHandlingBoundary = false;
        
        // Still keep the walking animation
        if (animController != null)
        {
            animController.SetWalking(true);
        }
    }
    
    public void ForceNewWaypoint()
    {
        // Stop current movement
        if (wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
        }
        
        isHandlingBoundary = false;
        
        // Get new waypoint and restart wandering
        currentWaypoint = GetRandomWaypoint();
        wanderingCoroutine = StartCoroutine(WanderingRoutine(true));
    }
    
    public void SetWanderingEnabled(bool enabled)
    {
        if (enabled && wanderingCoroutine == null)
        {
            isHandlingBoundary = false;
            wanderingCoroutine = StartCoroutine(WanderingRoutine(true));
        }
        else if (!enabled && wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
            wanderingCoroutine = null;
            isWandering = false;
            isWaiting = false;
            isHandlingBoundary = false;
            
            // Always maintain walking animation
            if (animController != null)
            {
                animController.SetWalking(true);
            }
        }
    }
    
    private void OnDestroy()
    {
        // Clean up all coroutines when destroyed
        StopAllCoroutines();
    }
}
