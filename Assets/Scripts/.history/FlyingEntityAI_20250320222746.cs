using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEntityAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float rotationSpeed = 3.0f;
    [SerializeField] private float fixedHeight = 2.0f; // Fixed height for flying
    
    [Header("Circular Movement Settings")]
    [SerializeField] private float circleRadius = 5.0f; // Radius of the circle
    [SerializeField] private bool clockwiseRotation = true; // Direction of rotation
    [SerializeField] private float circleCompletionTime = 10.0f; // Time to complete one circle
    [SerializeField] private float bankAngle = 15f; // How much to tilt during turns
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal variables
    private Vector3 circleCenter;
    private float circleProgress = 0f;
    private Coroutine circlingCoroutine;
    private Vector3 currentVelocity = Vector3.zero; // For smooth movement
    private bool isInitialized = false;
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
        
        // Store the circle center (spawn position)
        circleCenter = transform.position;
        
        // Set initial height
        Vector3 pos = transform.position;
        pos.y = fixedHeight;
        transform.position = pos;
        
        // Always set flying animation from the start
        if (animController != null)
        {
            animController.SetWalking(true);
        }
        
        // Start circular movement
        StartCircularMovement();
    }
    
    private void OnDrawGizmosSelected()
    {
        // Show the circular path in the editor
        Vector3 center = Application.isPlaying ? circleCenter : transform.position;
        center.y = fixedHeight;
        
#if UNITY_EDITOR
        // Draw the circle path
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, circleRadius);
        
        // Draw the center point
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(center, 0.2f);
        
        // Draw current position on circle (in play mode)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Vector3 currentTarget = GetPositionOnCircle(circleProgress);
            Gizmos.DrawLine(center, currentTarget);
            Gizmos.DrawSphere(currentTarget, 0.1f);
        }
#endif
    }
    
    private void Update()
    {
        // Skip if entity is dead
        if (livingEntity != null && livingEntity.IsDead)
        {
            if (circlingCoroutine != null)
            {
                StopCoroutine(circlingCoroutine);
                circlingCoroutine = null;
            }
            return;
        }
        
        // Gently maintain fixed height
        MaintainFixedHeight();
    }
    
    private void MaintainFixedHeight()
    {
        // Get current position
        Vector3 currentPos = transform.position;
        
        // If not at fixed height, gradually correct it
        if (Mathf.Abs(currentPos.y - fixedHeight) > 0.01f)
        {
            currentPos.y = Mathf.Lerp(currentPos.y, fixedHeight, Time.deltaTime * 2f);
            transform.position = currentPos;
        }
    }
    
    public void StartCircularMovement()
    {
        // Stop any existing movement
        if (circlingCoroutine != null)
        {
            StopCoroutine(circlingCoroutine);
        }
        
        // Start circular movement
        circlingCoroutine = StartCoroutine(CircularMovementRoutine());
    }
    
    private IEnumerator CircularMovementRoutine()
    {
        // Initialize at a random point on the circle
        circleProgress = Random.Range(0f, 1f);
        
        // Allow a short initialization time before starting movement
        yield return new WaitForSeconds(0.5f);
        isInitialized = true;
        
        while (true)
        {
            // Skip if entity is dead
            if (livingEntity != null && livingEntity.IsDead)
            {
                yield break;
            }
            
            // Calculate delta for smooth movement based on time
            float deltaProgress = (Time.deltaTime / circleCompletionTime);
            
            // Update progress along the circle
            if (clockwiseRotation)
                circleProgress = (circleProgress + deltaProgress) % 1f;
            else
                circleProgress = (circleProgress - deltaProgress + 1f) % 1f;
                
            // Get next position on the circle
            Vector3 targetPosition = GetPositionOnCircle(circleProgress);
            
            // Direction to next position
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            // Calculate the tangent to the circle (for smooth rotation)
            Vector3 tangent = clockwiseRotation ? 
                new Vector3(-direction.z, 0, direction.x) : 
                new Vector3(direction.z, 0, -direction.x);
            
            // Calculate the angle for tilting (banking) 
            // The entity should tilt toward the center of the circle
            Vector3 directionToCenter = (circleCenter - transform.position).normalized;
            directionToCenter.y = 0;
            
            // Create a rotation that combines forward direction and banking
            Quaternion forwardRotation = Quaternion.LookRotation(tangent);
            
            // Add banking by rotating around the forward axis
            float bankAmount = clockwiseRotation ? -bankAngle : bankAngle;
            Quaternion bankRotation = Quaternion.AngleAxis(bankAmount, Vector3.forward);
            
            // Combine rotations
            Quaternion targetRotation = forwardRotation * bankRotation;
            
            // Apply rotation smoothly
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
            
            // Move forward at a constant speed
            if (livingEntity != null)
            {
                // Use the entity's forward direction for consistent speed
                livingEntity.MoveInDirection(transform.forward, 1.0f);
                
                // Apply a small correction to stay on the circle path
                // Make this very subtle to avoid teleporting
                Vector3 currentPos = transform.position;
                Vector3 correctedPos = Vector3.SmoothDamp(
                    currentPos,
                    new Vector3(targetPosition.x, currentPos.y, targetPosition.z),
                    ref currentVelocity,
                    0.5f  // Longer smoothing time means more gradual corrections
                );
                
                // Apply the correction but maintain current height
                correctedPos.y = currentPos.y;
                transform.position = correctedPos;
            }
            
            yield return null;
        }
    }
    
    private Vector3 GetPositionOnCircle(float progress)
    {
        // Convert progress (0-1) to angle (0-360 degrees)
        float angle = progress * 2 * Mathf.PI;
        
        // Calculate position on circle
        float x = circleCenter.x + circleRadius * Mathf.Sin(angle);
        float z = circleCenter.z + circleRadius * Mathf.Cos(angle);
        
        // Return point on circle at fixed height
        return new Vector3(x, fixedHeight, z);
    }
    
    // Public methods for external control
    
    public void SetCircleRadius(float radius)
    {
        circleRadius = Mathf.Max(1f, radius);
    }
    
    public void SetRotationDirection(bool clockwise)
    {
        clockwiseRotation = clockwise;
    }
    
    public void SetCircleCompletionTime(float seconds)
    {
        circleCompletionTime = Mathf.Max(1f, seconds);
    }
    
    public void SetBankAngle(float angle)
    {
        bankAngle = Mathf.Clamp(angle, 0f, 45f);
    }
    
    private void OnDestroy()
    {
        // Clean up any coroutines when destroyed
        if (circlingCoroutine != null)
        {
            StopCoroutine(circlingCoroutine);
        }
    }
}
