using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -4f);
    
    [Header("Camera Settings")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.5f, 0f);
    
    [Header("Camera Distance")]
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float currentZoom = 4f;
    
    // Fixed camera orientation
    [Header("Fixed Orientation")]
    [SerializeField] private bool useFixedOrientation = true;
    [SerializeField] private Vector3 fixedRotation = new Vector3(30f, 0f, 0f); // X = pitch, Y = yaw, Z = roll
    
    // Transition parameters
    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 1.5f; // Duration in seconds for smooth transitions
    
    // Keep track of any active transition coroutine
    private Coroutine transitionCoroutine;
    
    // Add this method to CameraController for camera shake during metamorphosis
    public void ShakeCamera(float intensity = 0.3f)
    {
        // Stop any existing shake
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        // Start a new shake
        shakeCoroutine = StartCoroutine(DoCameraShake(intensity));
    }
    
    private Coroutine shakeCoroutine;
    
    private IEnumerator DoCameraShake(float intensity)
    {
        // Store original position
        Vector3 originalPosition = transform.localPosition;
        
        // Shake for 0.5 seconds
        float elapsed = 0.0f;
        float duration = 0.5f;
        
        while (elapsed < duration)
        {
            // Calculate diminishing intensity as the shake progresses
            float currentIntensity = intensity * (1.0f - (elapsed / duration));
            
            // Apply random offset
            Vector3 shakeOffset = Random.insideUnitSphere * currentIntensity;
            transform.localPosition = originalPosition + shakeOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Return to original position
        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }
    
    private void Start()
    {
        // Find player if target not assigned
        if (target == null && GameObject.FindGameObjectWithTag("Player"))
            target = GameObject.FindGameObjectWithTag("Player").transform;
            
        // Set initial position
        if (target != null)
            transform.position = target.position + GetCameraOffset();
            
        // Set fixed rotation if enabled
        if (useFixedOrientation)
            transform.rotation = Quaternion.Euler(fixedRotation);
    }
    
    private void Update()
    {

    }
    
    private void LateUpdate()
    {
        if (target == null)
            return;
            
        // Calculate desired position with current zoom level
        Vector3 desiredPosition = target.position + GetCameraOffset();
        
        // Smoothly move camera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
        
        // Handle camera orientation
        if (useFixedOrientation)
        {
            // Maintain fixed rotation
            transform.rotation = Quaternion.Euler(fixedRotation);
        }
        else if (lookAtTarget)
        {
            // Look at target if fixed orientation is disabled
            transform.LookAt(target.position + targetOffset);
        }
    }
    
    private Vector3 GetCameraOffset()
    {
        // Calculate position behind the player
        Vector3 backDirection = -Vector3.forward; // Always use world forward
        Vector3 rightDirection = Vector3.right;   // Always use world right
        Vector3 upDirection = Vector3.up;         // Always use world up
        
        // Calculate offset based on fixed world directions
        Vector3 calculatedOffset = 
            backDirection * currentZoom + 
            upDirection * offset.y + 
            rightDirection * offset.x;
            
        return calculatedOffset;
    }

    // Updated method to set camera parameters with smooth transition
    public void SetCameraParameters(float zoomLevel, float heightOffset)
    {
        // Stop any existing transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        // Start a new transition coroutine
        transitionCoroutine = StartCoroutine(TransitionCameraParameters(zoomLevel, heightOffset));
        
        Debug.Log($"Starting camera transition to: Zoom = {zoomLevel}, Height Offset = {heightOffset}");
    }
    
    // New coroutine to smoothly transition camera parameters
    private IEnumerator TransitionCameraParameters(float targetZoom, float targetHeight)
    {
        // Store the starting values
        float startZoom = currentZoom;
        float startHeight = offset.y;
        
        // Transition over time
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionDuration)
        {
            // Calculate the interpolation factor (0 to 1)
            float t = elapsedTime / transitionDuration;
            
            // Use an easing function for smoother transitions
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            // Interpolate the values
            currentZoom = Mathf.Lerp(startZoom, targetZoom, smoothT);
            offset.y = Mathf.Lerp(startHeight, targetHeight, smoothT);
            
            // Increment the elapsed time
            elapsedTime += Time.deltaTime;
            
            yield return null;
        }
        
        // Ensure final values are exactly what was requested
        currentZoom = targetZoom;
        offset.y = targetHeight;
        
        Debug.Log($"Camera transition complete: Zoom = {currentZoom}, Height = {offset.y}");
        
        // Clear the coroutine reference
        transitionCoroutine = null;
    }
    
    // Optional method to immediately set camera parameters without transition
    public void SetCameraParametersImmediate(float zoomLevel, float heightOffset)
    {
        // Stop any existing transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        
        // Update the camera parameters immediately
        currentZoom = zoomLevel;
        offset.y = heightOffset;
        
        Debug.Log($"Camera parameters updated immediately: Zoom = {zoomLevel}, Height Offset = {heightOffset}");
        
        // Force immediate camera update
        if (target != null)
        {
            Vector3 desiredPosition = target.position + GetCameraOffset();
            transform.position = desiredPosition;
        }
    }
}