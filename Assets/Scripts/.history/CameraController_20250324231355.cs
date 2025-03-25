using UnityEngine;

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
    
    [Header("Scale-based Adjustments")]
    [SerializeField] private float[] scaleMultipliers = new float[] { 1f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f }; // Match with PlayerController
    [SerializeField] private float[] zoomMultipliers = new float[] { 1f, 1.2f, 1.4f, 1.6f, 1.8f, 2.0f }; // Camera zoom for each scale
    [SerializeField] private float[] fovMultipliers = new float[] { 1f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f }; // FOV adjustments

    private Vector3 baseOffset = new Vector3(0f, 2f, -4f); // Store the base offset
    private float baseZoom = 4f; // Store the base zoom level
    private float baseFOV = 60f; // Store the base field of view

    private Vector3 offset;
    private float targetZoom;
    private float targetFOV;
    private Camera mainCamera;

    private void Start()
    {
        // Find player if target not assigned
        if (target == null && GameObject.FindGameObjectWithTag("Player"))
            target = GameObject.FindGameObjectWithTag("Player").transform;
            
        mainCamera = Camera.main;
        
        // Initialize with base values
        offset = baseOffset;
        currentZoom = baseZoom;
        targetZoom = baseZoom;
        targetFOV = baseFOV;
        
        if (mainCamera != null)
            mainCamera.fieldOfView = baseFOV;
            
        // Set initial position
        if (target != null)
        {
            // Get initial player scale and adjust camera
            float playerScale = target.localScale.x;
            AdjustCameraForPlayerScale(playerScale);
            transform.position = target.position + GetCameraOffset();
        }
        
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
            
        // Smoothly adjust FOV
        if (mainCamera != null && !Mathf.Approximately(mainCamera.fieldOfView, targetFOV))
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, smoothSpeed * Time.deltaTime);
        }
        
        // Smoothly adjust zoom
        if (!Mathf.Approximately(currentZoom, targetZoom))
        {
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, smoothSpeed * Time.deltaTime);
        }
        
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

    // Add this method to adjust camera based on player scale
    public void AdjustCameraForPlayerScale(float playerScale)
    {
        // Find which scale multiplier we're using
        int scaleIndex = 0;
        for (int i = scaleMultipliers.Length - 1; i >= 0; i--)
        {
            if (Mathf.Approximately(playerScale / scaleMultipliers[0], scaleMultipliers[i]))
            {
                scaleIndex = i;
                break;
            }
        }
        
        // Adjust zoom
        targetZoom = baseZoom * zoomMultipliers[scaleIndex];
        
        // Adjust FOV
        targetFOV = baseFOV * fovMultipliers[scaleIndex];
        
        // Adjust offset
        offset = baseOffset * zoomMultipliers[scaleIndex];
        
        Debug.Log($"Adjusted camera for scale {playerScale}: Zoom={targetZoom}, FOV={targetFOV}");
    }
}