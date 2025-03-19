using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    
    private void Start()
    {
        // If no camera is assigned, try to find the main camera
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
            
        // Optional: Lock cursor for first-person view
        // Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void Update()
    {
        // Get input axes
        float horizontal = Input.GetAxis("Horizontal"); // A and D keys
        float vertical = Input.GetAxis("Vertical");     // W and S keys
        
        // Calculate movement direction
        Vector3 movement = new Vector3(horizontal, 0f, vertical);
        
        // Normalize movement vector to prevent faster diagonal movement
        if (movement.magnitude > 1f)
            movement.Normalize();
            
        // Move the player
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.Self);
        
        // Optional: Mouse look rotation
        if (Input.GetMouseButton(1)) // Right mouse button held
        {
            float mouseX = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime);
        }
    }
}