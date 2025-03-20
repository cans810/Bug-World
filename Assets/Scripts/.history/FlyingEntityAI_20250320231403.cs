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
    private Coroutine wanderingCoroutine;
    private float nextDirectionChangeTime;
    
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
        
        // Check if we need to change direction
        if (Time.time > nextDirectionChangeTime)
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
    
    // Check if we hit the border and bounce
    private void CheckBorderAndBounce()
    {
        // Get horizontal distance from spawn
        Vector2 currentPosFlat = new Vector2(transform.position.x, transform.position.z);
        Vector2 spawnPosFlat = new Vector2(spawnPosition.x, spawnPosition.z);
        float distanceFromSpawn = Vector2.Distance(currentPosFlat, spawnPosFlat);
        
        // If beyond boundary, bounce
        if (distanceFromSpawn >= maxWanderRadius)
        {
            // Calculate normal vector (pointing inward from boundary)
            Vector2 normalVector = (spawnPosFlat - currentPosFlat).normalized;
            
            // Get current direction as 2D vector
            Vector2 currentDir2D = new Vector2(currentDirection.x, currentDirection.z);
            
            // Reflect direction around normal (bounce)
            Vector2 reflectedDir = Vector2.Reflect(currentDir2D, normalVector);
            
            // Convert back to 3D and set as new direction
            currentDirection = new Vector3(reflectedDir.x, 0, reflectedDir.y);
            
            // Rotate to face new direction
            StartCoroutine(RotateToDirection(currentDirection));
            
            // Schedule next normal direction change a bit later
            nextDirectionChangeTime = Time.time + Random.Range(minChangeDirectionTime, maxChangeDirectionTime);
            
            // Debug visualization
            Debug.DrawRay(transform.position, currentDirection * 2f, Color.red, 1f);
        }
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
        
        float rotationDuration = 0.3f;
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
        if (wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
        }
        
        // Choose initial random direction
        ChooseNewRandomDirection();
        
        // Make sure the walking animation is on
        if (animController != null)
        {
            animController.SetWalking(true);
        }
    }
    
    // Stop wandering
    public void StopWandering()
    {
        if (wanderingCoroutine != null)
        {
            StopCoroutine(wanderingCoroutine);
            wanderingCoroutine = null;
        }
        
        // Always keep walking animation
        if (animController != null)
        {
            animController.SetWalking(true);
        }
    }
    
    // Force a new direction
    public void ForceNewDirection()
    {
        ChooseNewRandomDirection();
    }
    
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
