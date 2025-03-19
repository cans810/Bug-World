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
        // Handle zoom input
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            currentZoom -= scrollInput * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minDistance, maxDistance);
        }
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
}