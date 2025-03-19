using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);
    
    [Header("Camera Settings")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f); // Look at player's head
    
    private void Start()
    {
        // Find player if target not assigned
        if (target == null && GameObject.FindGameObjectWithTag("Player"))
            target = GameObject.FindGameObjectWithTag("Player").transform;
            
        // Set initial position
        if (target != null)
            transform.position = target.position + offset;
    }
    
    private void LateUpdate()
    {
        if (target == null)
            return;
            
        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly move camera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
        
        // Look at target if enabled
        if (lookAtTarget)
            transform.LookAt(target.position + targetOffset);
    }
}