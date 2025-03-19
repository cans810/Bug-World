using UnityEngine;

public class LOLCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -10f);
    
    [Header("Camera Controls")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float panSpeed = 20f;
    [SerializeField] private float panBorderThickness = 10f;
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 15f;
    [SerializeField] private bool edgePanning = true;
    
    [Header("Camera Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float boundX = 50f;
    [SerializeField] private float boundZ = 50f;
    
    private float currentZoom;
    private Vector3 currentOffset;
    private Vector3 panMovement;
    private bool isFollowingTarget = true;
    
    private void Start()
    {
        if (target == null && GameObject.FindGameObjectWithTag("Player"))
            target = GameObject.FindGameObjectWithTag("Player").transform;
            
        currentZoom = Mathf.Abs(offset.y);
        currentOffset = offset;
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    private void LateUpdate()
    {
        if (target == null)
            return;
            
        // Calculate desired position
        Vector3 desiredPosition;
        
        if (isFollowingTarget)
        {
            // Follow target with current offset
            desiredPosition = target.position + currentOffset;
        }
        else
        {
            // Free camera movement with panning
            desiredPosition = transform.position + panMovement * Time.deltaTime;
        }
        
        // Apply bounds if enabled
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, -boundX, boundX);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, -boundZ, boundZ);
        }
        
        // Smoothly move camera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
        
        // Always look at the ground below the target
        Vector3 lookAtPosition = target.position;
        lookAtPosition.y = target.position.y - 1f; // Look slightly below the character
        transform.LookAt(lookAtPosition);
    }
    
    private void HandleInput()
    {
        // Reset pan movement
        panMovement = Vector3.zero;
        
        // Toggle between follow mode and free camera mode
        if (Input.GetKeyDown(KeyCode.Space))
            isFollowingTarget = !isFollowingTarget;
            
        // Camera zoom with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentZoom = Mathf.Clamp(currentZoom - scroll * scrollSpeed, minZoom, maxZoom);
        currentOffset.y = currentZoom;
        currentOffset.z = -currentZoom;
        
        // Only handle panning in free camera mode
        if (!isFollowingTarget)
        {
            // Edge panning
            if (edgePanning)
            {
                if (Input.mousePosition.y >= Screen.height - panBorderThickness)
                    panMovement.z += panSpeed;
                if (Input.mousePosition.y <= panBorderThickness)
                    panMovement.z -= panSpeed;
                if (Input.mousePosition.x >= Screen.width - panBorderThickness)
                    panMovement.x += panSpeed;
                if (Input.mousePosition.x <= panBorderThickness)
                    panMovement.x -= panSpeed;
            }
            
            // Keyboard panning
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                panMovement.z += panSpeed;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                panMovement.z -= panSpeed;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                panMovement.x += panSpeed;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                panMovement.x -= panSpeed;
        }
        
        // Center camera on target with Y key (common in MOBAs)
        if (Input.GetKeyDown(KeyCode.Y) && target != null)
            isFollowingTarget = true;
    }
}