using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -4f); // Reduced height and distance
    
    [Header("Camera Settings")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.5f, 0f); // Lowered look target
    
    [Header("Camera Distance")]
    [SerializeField] private float minDistance = 2f; // Minimum zoom distance
    [SerializeField] private float maxDistance = 8f; // Maximum zoom distance
    [SerializeField] private float zoomSpeed = 2f; // How fast to zoom with scroll wheel
    [SerializeField] private float currentZoom = 4f; // Starting zoom level
    
    private void Start()
    {
        // Find player if target not assigned
        if (target == null && GameObject.FindGameObjectWithTag("Player"))
            target = GameObject.FindGameObjectWithTag("Player").transform;
            
        // Set initial position
        if (target != null)
            transform.position = target.position + GetZoomedOffset();
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
        Vector3 desiredPosition = target.position + GetZoomedOffset();
        
        // Smoothly move camera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
        
        // Look at target if enabled
        if (lookAtTarget)
            transform.LookAt(target.position + targetOffset);
    }
    
    private Vector3 GetZoomedOffset()
    {
        // Calculate zoom direction (normalized offset)
        Vector3 zoomDirection = new Vector3(offset.x, offset.y, offset.z).normalized;
        
        // Apply zoom to the z component (distance from player)
        float zoomZ = -currentZoom;
        
        // Scale the height based on distance
        float heightRatio = Mathf.Abs(offset.y / offset.z);
        float zoomY = Mathf.Abs(zoomZ * heightRatio);
        
        return new Vector3(offset.x, zoomY, zoomZ);
    }
}