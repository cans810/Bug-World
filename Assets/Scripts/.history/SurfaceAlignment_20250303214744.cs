using UnityEngine;

public class SurfaceAlignment : MonoBehaviour
{
    [Header("Surface Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayLength = 1.0f;
    [SerializeField] private int rayCount = 4;
    [SerializeField] private float raySpread = 0.5f;
    
    [Header("Alignment Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float maxSlopeAngle = 75f; // Maximum slope angle the ant can climb
    [SerializeField] private bool debugRays = true;
    
    private Transform modelTransform; // The visual model that will rotate
    private Rigidbody rb;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Find the model transform - adjust this to match your hierarchy
        modelTransform = transform.Find("AntModel");
        if (modelTransform == null)
            modelTransform = transform; // Use this transform if no separate model
    }
    
    private void FixedUpdate()
    {
        AlignToSurface();
    }
    
    private void AlignToSurface()
    {
        Vector3 averageNormal = Vector3.zero;
        int hitCount = 0;
        
        // Cast multiple rays in a pattern around the character
        for (int i = 0; i < rayCount; i++)
        {
            float angle = (i / (float)rayCount) * 360f;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * transform.forward * raySpread;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f; // Slightly above character
            
            Ray ray = new Ray(rayStart + rayDirection, Vector3.down);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, rayLength, groundLayer))
            {
                // Check if slope is climbable
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle <= maxSlopeAngle)
                {
                    averageNormal += hit.normal;
                    hitCount++;
                }
                
                if (debugRays)
                    Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
            }
            else if (debugRays)
            {
                Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red);
            }
        }
        
        // If we hit any surfaces, align to the average normal
        if (hitCount > 0)
        {
            averageNormal /= hitCount;
            AlignModelToNormal(averageNormal);
        }
        else
        {
            // If not on any surface, gradually return to normal up orientation
            AlignModelToNormal(Vector3.up);
        }
    }
    
    private void AlignModelToNormal(Vector3 normal)
    {
        // Create rotation that aligns up direction with surface normal
        Quaternion targetRotation = Quaternion.FromToRotation(modelTransform.up, normal) * modelTransform.rotation;
        
        // Keep the forward direction aligned with movement when possible
        if (rb.velocity.magnitude > 0.1f)
        {
            Vector3 forwardDirection = Vector3.ProjectOnPlane(rb.velocity.normalized, normal);
            if (forwardDirection.magnitude > 0.1f)
            {
                Quaternion forwardRotation = Quaternion.LookRotation(forwardDirection, normal);
                targetRotation = forwardRotation;
            }
        }
        
        // Smoothly rotate to target orientation
        modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}