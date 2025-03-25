using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 3.93f, -4f);  // Default Y offset
    
    [Header("Camera Settings")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.5f, 0f);
    
    [Header("Camera Distance")]
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float currentZoom = 3.09f;  // Default zoom
    
    [Header("Metamorphosis Camera Settings")]
    private readonly float[] metamorphosisLevels = new float[] { 11f, 22f, 33f, 44f, 55f };
    private readonly float[] zoomSettings = new float[] { 3.69f, 4.27f, 4.92f, 5.77f, 6.2f };
    private readonly float[] offsetYSettings = new float[] { 4.75f, 5.4f, 6.21f, 7.14f, 8.53f };
    
    // Fixed camera orientation
    [Header("Fixed Orientation")]
    [SerializeField] private bool useFixedOrientation = true;
    [SerializeField] private Vector3 fixedRotation = new Vector3(30f, 0f, 0f); // X = pitch, Y = yaw, Z = roll
    
    private void Start()
    {
        // Find player if target not assigned
        if (target == null && GameObject.FindGameObjectWithTag("Player"))
        {
            target = GameObject.FindGameObjectWithTag("Player").transform;
            
            // Get the player's current level and apply appropriate camera settings
            PlayerInventory playerInventory = target.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                UpdateCameraSettingsForLevel(playerInventory.CurrentLevel);
            }
        }
            
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

    // Add this method to update camera settings based on level
    public void UpdateCameraSettingsForLevel(int level)
    {
        // Default settings (before level 11)
        float newZoom = 3.09f;
        float newOffsetY = 3.93f;

        // Check each metamorphosis level
        for (int i = metamorphosisLevels.Length - 1; i >= 0; i--)
        {
            if (level >= metamorphosisLevels[i])
            {
                newZoom = zoomSettings[i];
                newOffsetY = offsetYSettings[i];
                break;
            }
        }

        // Smoothly transition to new settings
        StartCoroutine(SmoothCameraTransition(newZoom, newOffsetY));
    }

    private IEnumerator SmoothCameraTransition(float targetZoom, float targetOffsetY)
    {
        float startZoom = currentZoom;
        float startOffsetY = offset.y;
        float elapsedTime = 0f;
        float transitionDuration = 1f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            // Use smooth step for more natural transition
            t = t * t * (3f - 2f * t);
            
            // Interpolate values
            currentZoom = Mathf.Lerp(startZoom, targetZoom, t);
            Vector3 newOffset = offset;
            newOffset.y = Mathf.Lerp(startOffsetY, targetOffsetY, t);
            offset = newOffset;

            yield return null;
        }

        // Ensure we reach the exact target values
        currentZoom = targetZoom;
        Vector3 finalOffset = offset;
        finalOffset.y = targetOffsetY;
        offset = finalOffset;
        
        Debug.Log($"Camera transition complete - Zoom: {currentZoom}, Offset Y: {offset.y}");
    }
}