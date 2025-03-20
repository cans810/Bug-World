using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEntityAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotationSpeed = 3.0f;
    [SerializeField] private float minHeight = 1.5f; // Minimum hover height
    [SerializeField] private float maxHeight = 2.5f; // Maximum hover height
    [SerializeField] private float heightVariationSpeed = 0.5f; // How fast vertical position changes
    
    [Header("Wandering Settings")]
    [SerializeField] private float minWanderDistance = 3f;
    [SerializeField] private float maxWanderDistance = 8f;
    [SerializeField] private float waypointReachedDistance = 1.0f;
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;
    
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
    private float targetHeight;
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
        
        // Store spawn position
        spawnPosition = transform.position;
        
        // Set initial target height between min and max
        targetHeight = Random.Range(minHeight, maxHeight);
        
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
            // Draw a sphere for the 3D wander area
            UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.1f);
            UnityEditor.Handles.DrawSolidDisc(center, Vector3.up, maxWanderRadius);
            
            // Draw the outline
            Gizmos.color = Color.cyan;
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(center, Vector3.up, maxWanderRadius);
            
            // Draw height bounds
            Vector3 minHeightPos = center + Vector3.up * minHeight;
            Vector3 maxHeightPos = center + Vector3.up * maxHeight;
            UnityEditor.Handles.DrawDottedLine(center, minHeightPos, 2f);
            UnityEditor.Handles.DrawDottedLine(center, maxHeightPos, 2f);
            UnityEditor.Handles.DrawWireDisc(minHeightPos, Vector3.up, 1f);
            UnityEditor.Handles.DrawWireDisc(maxHeightPos, Vector3.up, 1f);
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
            if (isWandering)
            {
                isWandering = false;
                UpdateAnimationState(false);
                if (wanderingCoroutine != null)
                {
                    StopCoroutine(wanderingCoroutine);
                    wanderingCoroutine = null;
                }
            }
            return;
        }
        
        // Gradually adjust height to target
        AdjustHeight();
        
        // Always enforce boundary limits
        EnforceBoundaryLimits();
    }
    
    private void AdjustHeight()
    {
        // Get current position
        Vector3 currentPos = transform.position;
        
        // Interpolate towards target height
        float newHeight = Mathf.Lerp(currentPos.y, targetHeight, Time.deltaTime * heightVariationSpeed);
        
        // Apply new height
        currentPos.y = newHeight;
        transform.position = currentPos;
        
        // Periodically change target height
        if (Random.value < 0.005f) // Small chance each frame
        {
            targetHeight = Random.Range(minHeight, maxHeight);
        }
    }
    
    private void EnforceBoundaryLimits()
    {
        if (!restrictToSpawnArea) return;
        
        // Get horizontal distance from spawn (ignoring Y)
        Vector2 currentPosFlat = new Vector2(transform.position.x, transform.position.z);
        Vector2 spawnPosFlat = new Vector2(spawnPosition.x, spawnPosition.z);
        float distanceFromSpawn = Vector2.Distance(currentPosFlat, spawnPosFlat);
        
        // If beyond boundary, move back inside
        if (distanceFromSpawn > maxWanderRadius)
        {
            // Calculate direction back to spawn (2D)
            Vector2 directionToSpawn = (spawnPosFlat - currentPosFlat).normalized;
            
            // Calculate position just inside boundary
            Vector2 newPosFlat = spawnPosFlat + directionToSpawn * -maxWanderRadius * 0.9f;
            
            // Apply new position, keeping current height
            transform.position = new Vector3(newPosFlat.x, transform.position.y, newPosFlat.y);
            
            // Face back toward center
            Vector3 lookDir = new Vector3(directionToSpawn.x, 0, directionToSpawn.y);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDir),
                Time.deltaTime * rotationSpeed * 2f
            );
            
            // Force to pick a new waypoint
            if (wanderingCoroutine != null)
            {
                StopCoroutine(wanderingCoroutine);
            }
            wanderingCoroutine = StartCoroutine(WanderingRoutine());
        }
    }
    
    public void StartWandering()
    {
        if (wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
        }
        
        wanderingCoroutine = StartCoroutine(WanderingRoutine());
    }
    
    private IEnumerator WanderingRoutine()
    {
        while (true)
        {
            // Skip if entity is dead
            if (livingEntity != null && livingEntity.IsDead)
            {
                isWandering = false;
                isWaiting = false;
                UpdateAnimationState(false);
                yield break;
            }
            
            // --- WAITING PHASE ---
            isWaiting = true;
            isWandering = false;
            UpdateAnimationState(false); // Idle animation
            
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
                yield return null;
            }
            
            // Ensure final rotation
            transform.rotation = targetRotation;
            
            // --- MOVEMENT PHASE ---
            isWaiting = false;
            isWandering = true;
            UpdateAnimationState(true); // Flying animation
            
            while (isWandering)
            {
                // Calculate distance to waypoint (3D)
                float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
                
                // Check if reached waypoint
                if (distanceToWaypoint < waypointReachedDistance)
                {
                    break; // Exit movement loop to get a new waypoint
                }
                
                // Move toward waypoint
                MoveTowardWaypoint();
                
                // Check if we've been flying for too long or hit a boundary
                if (Random.value < 0.002f) // Small chance each frame to change waypoint
                {
                    break; // Exit movement loop to get a new waypoint
                }
                
                yield return null;
            }
        }
    }
    
    private void MoveTowardWaypoint()
    {
        // Direction to waypoint
        Vector3 direction = (currentWaypoint - transform.position).normalized;
        
        // Rotate toward waypoint
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        
        // Apply movement based on current facing direction
        if (livingEntity != null)
        {
            livingEntity.MoveInDirection(transform.forward, 1.0f);
        }
        else
        {
            // Fallback movement if LivingEntity not available
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
    }
    
    private Vector3 GetRandomWaypoint()
    {
        // Start with random direction in 3D
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-0.3f, 0.3f), // Small vertical component
            Random.Range(-1f, 1f)
        ).normalized;
        
        // Generate random distance
        float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Calculate base position
        Vector3 baseWaypoint = transform.position + randomDirection * randomDistance;
        
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
        
        // Ensure Y is within height range
        baseWaypoint.y = Mathf.Clamp(baseWaypoint.y, minHeight, maxHeight);
        
        return baseWaypoint;
    }
    
    private void UpdateAnimationState(bool isFlying)
    {
        // Note: We use "Walk" parameter for flying animation as specified
        if (animController != null)
        {
            if (isFlying)
            {
                animController.SetWalking(true); // Use "Walk" for flying
            }
            else
            {
                animController.SetIdle();
            }
        }
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
        UpdateAnimationState(false);
    }
    
    public void SetNewTargetHeight(float height)
    {
        targetHeight = Mathf.Clamp(height, minHeight, maxHeight);
    }
    
    public void ForceNewWaypoint()
    {
        // Stop current movement
        if (wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
        }
        
        // Get new waypoint and restart wandering
        currentWaypoint = GetRandomWaypoint();
        wanderingCoroutine = StartCoroutine(WanderingRoutine());
    }
    
    public void SetWanderingEnabled(bool enabled)
    {
        if (enabled && wanderingCoroutine == null)
        {
            wanderingCoroutine = StartCoroutine(WanderingRoutine());
        }
        else if (!enabled && wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
            wanderingCoroutine = null;
            isWandering = false;
            isWaiting = false;
            UpdateAnimationState(false);
        }
    }
}
