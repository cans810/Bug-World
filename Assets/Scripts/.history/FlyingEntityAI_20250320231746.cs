using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEntityAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotationSpeed = 5.0f;
    [SerializeField] private float fixedHeight = 2.0f;
    [SerializeField] private float straightLineDuration = 1.5f; // How long to move in a straight line
    
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
    private bool isBouncing = false;
    private Coroutine movementCoroutine;
    
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
        
        // Start simple movement pattern
        StartMovement();
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
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
            return;
        }
        
        // Always enforce fixed height
        SetFixedHeight();
        
        // Check for border collision - this is still done every frame
        if (restrictToSpawnArea && !isBouncing)
        {
            CheckBorderAndBounce();
        }
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
    
    // Choose a completely random direction
    private Vector3 GetRandomDirection()
    {
        // Random 2D direction (XZ plane only)
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        return new Vector3(randomDir.x, 0, randomDir.y);
    }
    
    // Start the movement pattern
    public void StartMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        
        movementCoroutine = StartCoroutine(SimpleMovementPattern());
    }
    
    // The simplified movement pattern coroutine
    private IEnumerator SimpleMovementPattern()
    {
        while (true)
        {
            // Skip if we're currently handling a bounce
            if (isBouncing)
            {
                yield return null;
                continue;
            }
            
            // Choose a random direction to move
            currentDirection = GetRandomDirection();
            
            // Rotate to face that direction
            yield return StartCoroutine(RotateToDirection(currentDirection));
            
            // Move in a straight line for a fixed duration
            float timer = 0f;
            while (timer < straightLineDuration && !isBouncing)
            {
                // Move forward
                MoveInDirection(currentDirection);
                
                timer += Time.deltaTime;
                yield return null;
            }
            
            // Small pause before changing direction
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    // Check if we hit the border and bounce 
    private void CheckBorderAndBounce()
    {
        // Get horizontal distance from spawn
        Vector2 currentPosFlat = new Vector2(transform.position.x, transform.position.z);
        Vector2 spawnPosFlat = new Vector2(spawnPosition.x, spawnPosition.z);
        float distanceFromSpawn = Vector2.Distance(currentPosFlat, spawnPosFlat);
        
        // If beyond boundary and not already bouncing, bounce
        if (distanceFromSpawn >= maxWanderRadius && !isBouncing)
        {
            StartCoroutine(SimpleBounce());
        }
    }
    
    // Simple bounce behavior that picks a direction toward the center
    private IEnumerator SimpleBounce()
    {
        isBouncing = true;
        
        // Direction toward center
        Vector3 directionToCenter = (new Vector3(spawnPosition.x, fixedHeight, spawnPosition.z) - transform.position).normalized;
        
        // Move slightly inward to ensure we're not stuck on the border
        transform.position += directionToCenter * 0.5f;
        
        // Rotate to face toward center
        yield return StartCoroutine(RotateToDirection(directionToCenter));
        
        // Move toward center for a short time
        float centerMoveDuration = 0.5f;
        float timer = 0f;
        
        while (timer < centerMoveDuration)
        {
            MoveInDirection(directionToCenter);
            timer += Time.deltaTime;
            yield return null;
        }
        
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
    
    // Force a new direction immediately
    public void ForceNewDirection()
    {
        if (isBouncing) return;
        
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        
        StartMovement();
    }
    
    // When destroyed, clean up all coroutines
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
