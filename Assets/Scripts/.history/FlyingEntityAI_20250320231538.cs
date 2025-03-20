using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEntityAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotationSpeed = 5.0f;
    [SerializeField] private float fixedHeight = 2.0f;
    
    [Header("Wandering Settings")]
    [SerializeField] private float minChangeDirectionTime = 3f;
    [SerializeField] private float maxChangeDirectionTime = 8f;
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    [Header("Spawn Area Restriction")]
    [SerializeField] private bool restrictToSpawnArea = true;
    [SerializeField] private float maxWanderRadius = 10f;
    [SerializeField] private bool showSpawnAreaGizmo = true;
    
    // Internal variables
    private Vector3 currentDirection;
    private Vector3 spawnPosition;
    private float nextDirectionChangeTime;
    private bool isBouncing = false;
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
        
        // Store spawn position
        spawnPosition = transform.position;
        
        // Set initial height
        SetFixedHeight();
        
        // Always set flying animation
        if (animController != null)
        {
            animController.SetWalking(true);
        }
        
        // Choose initial random direction
        ChooseNewRandomDirection();
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
            return;
        }
        
        // Always enforce fixed height
        SetFixedHeight();
        
        // If not bouncing, check if it's time for a direction change
        if (!isBouncing && Time.time > nextDirectionChangeTime)
        {
            ChooseNewRandomDirection();
        }
        
        // Check for border collision and bounce if needed
        if (restrictToSpawnArea)
        {
            CheckBorderAndBounce();
        }
        
        // Move in current direction
        MoveInDirection(currentDirection);
    }
    
    // Set the entity to fixed height
    private void SetFixedHeight()
    {
        Vector3 pos = transform.position;
        if (pos.y != fixedHeight)
        {
            pos.y = fixedHeight;
            transform.position = pos;
        }
    }
    
    // Choose a new random movement direction
    private void ChooseNewRandomDirection()
    {
        // Random 2D direction (XZ plane)
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        currentDirection = new Vector3(randomDir.x, 0, randomDir.y);
        
        // Schedule next direction change
        nextDirectionChangeTime = Time.time + Random.Range(minChangeDirectionTime, maxChangeDirectionTime);
        
        // Rotate to face movement direction
        StartCoroutine(RotateToDirection(currentDirection));
    }
    
    // Check if we hit the border and bounce with a simplified approach
    private void CheckBorderAndBounce()
    {
        // Get horizontal distance from spawn
        Vector2 currentPosFlat = new Vector2(transform.position.x, transform.position.z);
        Vector2 spawnPosFlat = new Vector2(spawnPosition.x, spawnPosition.z);
        float distanceFromSpawn = Vector2.Distance(currentPosFlat, spawnPosFlat);
        
        // If beyond boundary and not already bouncing, bounce
        if (distanceFromSpawn >= maxWanderRadius && !isBouncing)
        {
            // Start bounce sequence
            StartCoroutine(SimpleBounce());
        }
    }
    
    // Simple bounce behavior that just turns toward center
    private IEnumerator SimpleBounce()
    {
        isBouncing = true;
        
        // Direction to center
        Vector3 directionToCenter = (new Vector3(spawnPosition.x, fixedHeight, spawnPosition.z) - transform.position).normalized;
        
        // Move slightly inward to make sure we're not stuck at the edge
        transform.position += directionToCenter * 0.5f;
        
        // Start with a clearer rotation toward center
        StartCoroutine(RotateToDirection(directionToCenter));
        
        // Wait for rotation to complete
        yield return new WaitForSeconds(0.5f);
        
        // After rotating toward center, choose a random direction in the semicircle facing inward
        Vector3 perpendicularDir = Vector3.Cross(Vector3.up, directionToCenter).normalized;
        float angle = Random.Range(-90f, 90f); // Only directions within 180 degrees of center direction
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
        Vector3 newDirection = rotation * directionToCenter;
        
        // Set this as our new direction
        currentDirection = newDirection;
        
        // Rotate to face this new direction
        StartCoroutine(RotateToDirection(currentDirection));
        
        // Wait a bit before allowing another bounce
        yield return new WaitForSeconds(0.5f);
        
        // Schedule next direction change
        nextDirectionChangeTime = Time.time + Random.Range(minChangeDirectionTime, maxChangeDirectionTime);
        
        // End bounce state
        isBouncing = false;
    }
    
    // Move in the specified direction
    private void MoveInDirection(Vector3 direction)
    {
        if (livingEntity != null)
        {
            livingEntity.MoveInDirection(direction, 1.0f);
            
            // Ensure fixed height after movement
            SetFixedHeight();
        }
        else
        {
            // Fallback direct movement
            transform.position += direction * moveSpeed * Time.deltaTime;
            SetFixedHeight();
        }
    }
    
    // Smoothly rotate to face a direction
    private IEnumerator RotateToDirection(Vector3 direction)
    {
        // Skip if direction is zero
        if (direction.magnitude < 0.1f)
            yield break;
            
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        float rotationDuration = 0.5f; // Slightly longer for smoother turns
        float elapsedTime = 0;
        
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / rotationDuration);
            
            // Use smooth step for nicer rotation
            float smoothT = Mathf.SmoothStep(0, 1, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
            
            yield return null;
        }
        
        // Ensure final rotation is exact
        transform.rotation = targetRotation;
    }
    
    // Start wandering behavior
    public void StartWandering()
    {
        // Choose initial random direction
        ChooseNewRandomDirection();
        
        // Make sure the walking animation is on
        if (animController != null)
        {
            animController.SetWalking(true);
        }
    }
    
    // Force a new direction
    public void ForceNewDirection()
    {
        isBouncing = false;
        ChooseNewRandomDirection();
    }
    
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
