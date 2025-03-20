using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEntityAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotationSpeed = 3.0f;
    [SerializeField] private float fixedHeight = 2.0f;
    
    [Header("Wandering Settings")]
    [SerializeField] private float minWanderDistance = 3f;
    [SerializeField] private float maxWanderDistance = 8f;
    [SerializeField] private float waypointReachedDistance = 1.0f;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    [Header("Spawn Area Restriction")]
    [SerializeField] private bool restrictToSpawnArea = true;
    [SerializeField] private float maxWanderRadius = 10f;
    [SerializeField] private bool showSpawnAreaGizmo = true;
    
    // Internal variables
    private Vector3 currentWaypoint;
    private Vector3 spawnPosition;
    private bool isWandering = false;
    private bool isWaiting = false;
    private Coroutine wanderingCoroutine;
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
        
        // Store spawn position
        spawnPosition = transform.position;
        
        // Set initial height and ensure we're using walk animation
        SetFixedHeight();
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
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            
#if UNITY_EDITOR
            // Draw the wander area
            UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.1f);
            UnityEditor.Handles.DrawSolidDisc(center, Vector3.up, maxWanderRadius);
            Gizmos.color = Color.cyan;
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(center, Vector3.up, maxWanderRadius);
#else
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
        SetFixedHeight();
    }
    
    // Start wandering behavior
    public void StartWandering()
    {
        if (wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
        }
        
        // Get initial waypoint
        currentWaypoint = GetRandomWaypoint();
        
        // Start wandering routine
        wanderingCoroutine = StartCoroutine(WanderingRoutine());
    }
    
    // Core wandering routine - similar to AIWandering
    private IEnumerator WanderingRoutine()
    {
        while (true)
        {
            // Wait phase
            isWaiting = true;
            isWandering = false;
            
            // Always ensure walking animation is active
            if (animController != null)
            {
                animController.SetWalking(true);
            }
            
            // Wait at current position
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            
            // Movement phase
            isWaiting = false;
            isWandering = true;
            
            // Get a new waypoint
            currentWaypoint = GetRandomWaypoint();
            
            // Calculate distance to waypoint
            float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
            
            // First rotate to face the waypoint
            Vector3 directionToWaypoint = (currentWaypoint - transform.position).normalized;
            float rotationTime = 0f;
            float maxRotationTime = 1.0f;
            
            Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
            Quaternion startRotation = transform.rotation;
            
            while (rotationTime < maxRotationTime)
            {
                rotationTime += Time.deltaTime;
                float rotationProgress = rotationTime / maxRotationTime;
                
                // Smoothly rotate toward target
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, rotationProgress);
                
                // If we're close enough to the target rotation, break early
                if (Quaternion.Angle(transform.rotation, targetRotation) < 5f)
                    break;
                    
                yield return null;
            }
            
            // Now move toward the waypoint
            while (isWandering)
            {
                // Calculate remaining distance
                float remainingDistance = Vector3.Distance(transform.position, currentWaypoint);
                
                // Check if we've reached the waypoint
                if (remainingDistance <= waypointReachedDistance)
                {
                    break;
                }
                
                // Move toward waypoint
                if (livingEntity != null)
                {
                    livingEntity.MoveInDirection(transform.forward, 1.0f);
                }
                else
                {
                    // Fallback movement
                    transform.position += transform.forward * moveSpeed * Time.deltaTime;
                }
                
                // Ensure fixed height
                SetFixedHeight();
                
                // Check if we hit the boundary
                if (restrictToSpawnArea)
                {
                    CheckAndHandleBoundary();
                }
                
                yield return null;
            }
        }
    }
    
    // Get a random waypoint within allowed area
    private Vector3 GetRandomWaypoint()
    {
        // Random direction in XZ plane
        Vector2 randomDir2D = Random.insideUnitCircle.normalized;
        Vector3 randomDirection = new Vector3(randomDir2D.x, 0, randomDir2D.y);
        
        // Random distance
        float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
        
        // Calculate potential waypoint
        Vector3 potentialWaypoint = transform.position + randomDirection * randomDistance;
        potentialWaypoint.y = fixedHeight;
        
        // If we're restricting to spawn area, check if waypoint is valid
        if (restrictToSpawnArea)
        {
            // Get distance from spawn to waypoint
            float distanceFromSpawn = Vector2.Distance(
                new Vector2(potentialWaypoint.x, potentialWaypoint.z),
                new Vector2(spawnPosition.x, spawnPosition.z)
            );
            
            // If beyond allowed radius, adjust the waypoint
            if (distanceFromSpawn > maxWanderRadius * 0.8f)
            {
                // Get direction from spawn to waypoint
                Vector2 spawnToWaypoint = new Vector2(
                    potentialWaypoint.x - spawnPosition.x,
                    potentialWaypoint.z - spawnPosition.z
                ).normalized;
                
                // Adjust waypoint to be within bounds
                potentialWaypoint.x = spawnPosition.x + spawnToWaypoint.x * (maxWanderRadius * 0.7f);
                potentialWaypoint.z = spawnPosition.z + spawnToWaypoint.y * (maxWanderRadius * 0.7f);
            }
        }
        
        return potentialWaypoint;
    }
    
    // Check if we hit the boundary and handle if needed
    private void CheckAndHandleBoundary()
    {
        // Get distance from spawn
        float distanceFromSpawn = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(spawnPosition.x, spawnPosition.z)
        );
        
        // If we're beyond the boundary
        if (distanceFromSpawn >= maxWanderRadius)
        {
            // Stop current wandering
            isWandering = false;
            
            // Calculate direction to spawn
            Vector3 directionToCenter = spawnPosition - transform.position;
            directionToCenter.y = 0;
            directionToCenter.Normalize();
            
            // Move slightly inward
            transform.position += directionToCenter * 0.5f;
            
            // Stop current coroutine and start a new one that will handle the bounce
            if (wanderingCoroutine != null)
            {
                StopCoroutine(wanderingCoroutine);
            }
            wanderingCoroutine = StartCoroutine(BounceFromBorder());
        }
    }
    
    // New coroutine for handling border bounces with a pause
    private IEnumerator BounceFromBorder()
    {
        // Pause at the border
        float pauseDuration = 0.5f;
        yield return new WaitForSeconds(pauseDuration);
        
        // Get direction to center
        Vector3 directionToCenter = spawnPosition - transform.position;
        directionToCenter.y = 0;
        directionToCenter.Normalize();
        
        // Rotate to face slightly inward
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(directionToCenter);
        
        float rotationTime = 0f;
        float maxRotationTime = 0.8f;
        
        while (rotationTime < maxRotationTime)
        {
            rotationTime += Time.deltaTime;
            float rotationProgress = rotationTime / maxRotationTime;
            
            // Smoothly rotate toward target
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, rotationProgress);
            
            yield return null;
        }
        
        // Brief pause after rotating
        yield return new WaitForSeconds(0.2f);
        
        // Calculate a new waypoint that's inward but at an angle
        float angle = Random.Range(-70f, 70f); // Random angle offset from center direction
        Vector3 randomizedDirection = Quaternion.Euler(0, angle, 0) * directionToCenter;
        
        // Choose a safe distance for the new waypoint
        float safeDistance = maxWanderRadius * 0.6f;
        currentWaypoint = spawnPosition + randomizedDirection * safeDistance;
        currentWaypoint.y = fixedHeight;
        
        // Start normal wandering again
        wanderingCoroutine = StartCoroutine(WanderingRoutine());
    }
    
    // Set entity to fixed height
    private void SetFixedHeight()
    {
        Vector3 pos = transform.position;
        if (pos.y != fixedHeight)
        {
            pos.y = fixedHeight;
            transform.position = pos;
        }
    }
    
    // Method to manually trigger a new waypoint
    public void ForceNewWaypoint()
    {
        if (wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
        }
        
        currentWaypoint = GetRandomWaypoint();
        wanderingCoroutine = StartCoroutine(WanderingRoutine());
    }
    
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
