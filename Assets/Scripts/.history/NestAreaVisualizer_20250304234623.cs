using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class NestAreaVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private Color circleColor = new Color(0f, 1f, 0f, 0.5f); // Semi-transparent green
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private int segments = 60; // Higher number = smoother circle
    [SerializeField] private LayerMask groundLayerMask; // Set this to your ground layer
    [SerializeField] private float yOffset = 0.05f; // Small offset to prevent z-fighting
    
    [Header("Debug")]
    [SerializeField] private bool showAreaInEditor = true;
    [SerializeField] private bool showAreaInGame = true;
    
    private SphereCollider sphereCollider;
    private LineRenderer lineRenderer;
    
    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        
        // Create the line renderer if it doesn't exist
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // Configure the line renderer
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = segments + 1;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = circleColor;
        lineRenderer.endColor = circleColor;
    }
    
    private void Start()
    {
        DrawAreaCircle();
    }
    
    private void OnValidate()
    {
        // Update the visualization when values change in editor
        if (showAreaInEditor && Application.isEditor && !Application.isPlaying)
        {
            // Ensure we have the required components
            sphereCollider = GetComponent<SphereCollider>();
            
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = true;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.positionCount = segments + 1;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            lineRenderer.startColor = circleColor;
            lineRenderer.endColor = circleColor;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            
            DrawAreaCircle();
        }
    }
    
    private void DrawAreaCircle()
    {
        if (!showAreaInGame && Application.isPlaying)
        {
            lineRenderer.enabled = false;
            return;
        }
        
        lineRenderer.enabled = true;
        
        // Get world center and radius of the sphere collider
        Vector3 center = transform.position + sphereCollider.center;
        float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, 
                                                         Mathf.Max(transform.lossyScale.y, 
                                                                 transform.lossyScale.z));
        
        // Raycast downward to find the ground
        if (Physics.Raycast(center, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
        {
            // Calculate circle radius at ground intersection point
            float distanceToGround = hit.distance;
            float circleRadius;
            
            if (distanceToGround <= radius)
            {
                // Calculate the radius of the circle at the intersection
                // Using the Pythagorean theorem: r² = R² - d²
                // Where r is the circle radius, R is the sphere radius, d is the distance from center to plane
                circleRadius = Mathf.Sqrt(radius * radius - distanceToGround * distanceToGround);
                
                // Draw the circle
                Vector3 circleCenter = hit.point + new Vector3(0, yOffset, 0); // Small y offset to prevent z-fighting
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (float)i / segments * 2 * Mathf.PI;
                    Vector3 pos = new Vector3(
                        circleCenter.x + Mathf.Cos(angle) * circleRadius,
                        circleCenter.y,
                        circleCenter.z + Mathf.Sin(angle) * circleRadius
                    );
                    lineRenderer.SetPosition(i, pos);
                }
            }
            else
            {
                // The sphere doesn't intersect with the ground
                lineRenderer.enabled = false;
                Debug.LogWarning("Sphere doesn't intersect with the ground. Increase sphere radius or move closer to ground.");
            }
        }
        else
        {
            // No ground was found
            lineRenderer.enabled = false;
            Debug.LogWarning("No ground found under the sphere collider. Make sure ground layer is set correctly.");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (showAreaInEditor && !Application.isPlaying)
        {
            // Debug visualization in the scene view
            Gizmos.color = circleColor;
            
            if (sphereCollider == null)
                sphereCollider = GetComponent<SphereCollider>();
                
            if (sphereCollider != null)
            {
                Vector3 center = transform.position + sphereCollider.center;
                Gizmos.DrawWireSphere(center, sphereCollider.radius * Mathf.Max(transform.lossyScale.x, 
                                                                             Mathf.Max(transform.lossyScale.y, 
                                                                                     transform.lossyScale.z)));
                
                // Draw ray down to show where we're checking for ground
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(center, Vector3.down * 20f);
            }
        }
    }
} 