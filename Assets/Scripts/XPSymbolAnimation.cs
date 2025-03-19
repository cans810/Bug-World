using UnityEngine;
using System.Collections;

public class XPSymbolAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float scaleSpeed = 1f;
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float acceleration = 5f;
    
    [Header("Visual Effects")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem particles;
    
    // Target position to move towards (usually the XP counter in UI)
    private Vector3 targetPosition;
    private Vector3 initialPosition;
    private Vector3 initialScale;
    private bool isMoving = false;
    private float currentSpeed;
    private Camera mainCamera;
    
    private void Start()
    {
        // Store initial position and scale
        initialPosition = transform.position;
        initialScale = transform.localScale;
        currentSpeed = moveSpeed;
        
        // Get main camera for converting world to screen positions
        mainCamera = Camera.main;
        
        // Start with small scale
        transform.localScale = Vector3.zero;
        
        // Start animation sequence
        StartCoroutine(AnimationSequence());
    }
    
    // Method to set the target position (called by VisualEffectsManager)
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
    }
    
    private IEnumerator AnimationSequence()
    {
        // Initial appearance animation
        float timer = 0f;
        while (timer < initialDelay)
        {
            timer += Time.deltaTime;
            float progress = timer / initialDelay;
            
            // Scale up
            transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, progress);
            
            // Random rotation
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        // Ensure we reached final scale
        transform.localScale = initialScale;
        
        // Start moving toward target
        isMoving = true;
        
        // Move for up to 5 seconds (safety timeout)
        float moveDuration = 0f;
        while (moveDuration < 5f)
        {
            moveDuration += Time.deltaTime;
            
            // If we have a target position, move toward it
            if (targetPosition != Vector3.zero)
            {
                // Accelerate over time
                currentSpeed += acceleration * Time.deltaTime;
                
                // Move toward target
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    currentSpeed * Time.deltaTime
                );
                
                // If we're very close to target, destroy the object
                if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
                {
                    // Trigger any "collection" effects here if needed
                    break;
                }
            }
            
            // Continue rotation during movement
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        // Scale down and destroy
        timer = 0f;
        float destroyDelay = 0.2f;
        Vector3 finalScale = transform.localScale;
        
        while (timer < destroyDelay)
        {
            timer += Time.deltaTime;
            float progress = timer / destroyDelay;
            
            // Scale down
            transform.localScale = Vector3.Lerp(finalScale, Vector3.zero, progress);
            
            yield return null;
        }
        
        // Destroy the object
        Destroy(gameObject);
    }
    
    // Optional: Add a method to handle what happens when XP reaches the counter
    private void OnReachedTarget()
    {
        // Play a sound effect
        // Trigger a particle burst
        // Add a flash effect to the XP counter text
    }
} 