using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Creates a visible representation of a SphereCollider as a semi-transparent gray sphere.
/// Attach this to any GameObject that has a SphereCollider component to visualize the boundary.
/// </summary>
public class BorderVisualizer : MonoBehaviour
{
    [Header("Border Settings")]
    [SerializeField] private bool showDebugVisuals = true;
    [SerializeField] private Color borderColor = Color.red;
    [SerializeField] private Color safeAreaColor = Color.green;
    [SerializeField] private int rayCount = 36; // Rays around the circle (every 10 degrees)
    [SerializeField] private float rayLength = 20f; // How far the rays extend
    [SerializeField] private float safetyMargin = 1f; // How far inside the border is considered "safe"
    
    [Header("Visualization Settings")]
    [SerializeField] private Color sphereColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Semi-transparent gray
    [SerializeField] private bool showInGame = true;
    [SerializeField] private Material visualizerMaterial;
    
    // For object pooling
    private static List<BorderVisualizer> allBorders = new List<BorderVisualizer>();
    
    // Cached components
    private SphereCollider sphereCollider;
    private BoxCollider boxCollider;
    private Collider[] otherColliders;
    
    private GameObject visualSphere;
    private Transform visualTransform;
    private Material instanceMaterial;
    
    private void Awake()
    {
        // Cache collider references
        sphereCollider = GetComponent<SphereCollider>();
        boxCollider = GetComponent<BoxCollider>();
        otherColliders = GetComponents<Collider>();
        
        // Add to static list for easy access
        allBorders.Add(this);
    }
    
    private void Start()
    {
        if (showInGame)
        {
            CreateVisualSphere();
        }
    }
    
    private void OnDestroy()
    {
        // Remove from list
        allBorders.Remove(this);
        
        // Clean up the generated material when this component is destroyed
        if (instanceMaterial != null)
        {
            Destroy(instanceMaterial);
        }
        
        // Also destroy the visual sphere if it exists
        if (visualSphere != null)
        {
            Destroy(visualSphere);
        }
    }
    
    // This runs in the editor to visualize the border
    private void OnDrawGizmos()
    {
        if (!showDebugVisuals) return;
        
        // Get the collider if we don't have it yet
        if (sphereCollider == null) sphereCollider = GetComponent<SphereCollider>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();
        
        if (sphereCollider != null)
        {
            DrawSphereGizmo();
        }
        else if (boxCollider != null)
        {
            DrawBoxGizmo();
        }
    }
    
    private void DrawSphereGizmo()
    {
        // Draw sphere border
        Vector3 center = transform.TransformPoint(sphereCollider.center);
        float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, 
                                                       Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
        
        // Draw the border
        Gizmos.color = borderColor;
        Gizmos.DrawWireSphere(center, radius);
        
        // Draw the safe zone
        Gizmos.color = safeAreaColor;
        Gizmos.DrawWireSphere(center, radius - safetyMargin);
        
        // Draw rays
        for (int i = 0; i < rayCount; i++)
        {
            float angle = (i / (float)rayCount) * 360f;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            // Draw ray
            Vector3 rayOrigin = center;
            Gizmos.color = borderColor;
            Gizmos.DrawRay(rayOrigin, direction * rayLength);
        }
    }
    
    private void DrawBoxGizmo()
    {
        // Draw box border
        Vector3 center = transform.TransformPoint(boxCollider.center);
        Vector3 size = Vector3.Scale(boxCollider.size, transform.lossyScale);
        
        // Draw the border
        Gizmos.color = borderColor;
        Gizmos.DrawWireCube(center, size);
        
        // Draw the safe zone
        Gizmos.color = safeAreaColor;
        Gizmos.DrawWireCube(center, size - Vector3.one * 2 * safetyMargin);
    }
    
    // Public methods that other scripts can use
    
    // Add this to the BorderVisualizer class to help debug issues
    public static bool DebugMode = true;
    
    // Modify the IsPositionSafe method to include debugging
    public static bool IsPositionSafe(Vector3 position)
    {
        // Check for no borders registered
        if (allBorders.Count == 0)
        {
            if (DebugMode)
            {
                Debug.LogError("BorderVisualizer: No border objects found! Did you add the BorderVisualizer component to your MapBorder objects?");
            }
            return true; // Default to safe if no borders found (prevent breaking gameplay)
        }
        
        foreach (BorderVisualizer border in allBorders)
        {
            if (!border.IsPointInsideBorder(position))
            {
                if (DebugMode)
                {
                    Debug.Log($"BorderVisualizer: Position {position} is UNSAFE - outside border {border.gameObject.name}");
                    // Draw a red sphere at the unsafe position for 5 seconds
                    Debug.DrawRay(position, Vector3.up * 3f, Color.red, 5f);
                }
                return false; // Position is outside at least one border
            }
        }
        
        if (DebugMode)
        {
            // Draw a green sphere at the safe position
            Debug.DrawRay(position, Vector3.up * 1f, Color.green, 0.5f);
        }
        
        return true; // Position is inside all borders
    }
    
    // Get the closest safe position to the target position
    public static Vector3 GetClosestSafePosition(Vector3 targetPosition, Vector3 originalPosition)
    {
        // If the target position is already safe, return it
        if (IsPositionSafe(targetPosition))
        {
            return targetPosition;
        }
        
        // Otherwise, find a safe position along the line from original to target
        return FindClosestSafePoint(originalPosition, targetPosition);
    }
    
    // Static helper to check if a waypoint would be valid
    public static bool IsWaypointValid(Vector3 currentPosition, Vector3 targetWaypoint)
    {
        // First check if the target is beyond the border
        if (!IsPositionSafe(targetWaypoint))
        {
            return false; // Waypoint is outside border
        }
        
        // Then check if the path between current and target crosses the border
        float dist = Vector3.Distance(currentPosition, targetWaypoint);
        Vector3 dir = (targetWaypoint - currentPosition).normalized;
        
        // Check points along the path
        for (float t = 0; t < dist; t += 0.5f)
        {
            Vector3 pointToCheck = currentPosition + dir * t;
            if (!IsPositionSafe(pointToCheck))
            {
                return false; // Path crosses outside the border
            }
        }
        
        return true; // Waypoint is valid and path stays inside
    }
    
    // Find the closest safe point along a line
    private static Vector3 FindClosestSafePoint(Vector3 start, Vector3 end)
    {
        // If start isn't safe, return start (shouldn't happen)
        if (!IsPositionSafe(start))
        {
            Debug.LogWarning("Start position is not safe!");
            return start;
        }
        
        // Binary search to find closest safe point
        float distance = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;
        
        float minDist = 0;
        float maxDist = distance;
        float currentDist = distance * 0.5f;
        
        for (int i = 0; i < 10; i++) // 10 iterations should be enough
        {
            Vector3 testPoint = start + direction * currentDist;
            
            if (IsPositionSafe(testPoint))
            {
                // We can go farther
                minDist = currentDist;
            }
            else
            {
                // We went too far
                maxDist = currentDist;
            }
            
            // Update distance
            currentDist = (minDist + maxDist) * 0.5f;
        }
        
        // Return the safe point
        return start + direction * minDist;
    }
    
    // Instance method to check if a point is inside this specific border
    private bool IsPointInsideBorder(Vector3 point)
    {
        if (sphereCollider != null)
        {
            // For sphere collider
            Vector3 center = transform.TransformPoint(sphereCollider.center);
            float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, 
                                                          Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
            
            // Distance from point to center
            float distance = Vector3.Distance(point, center);
            
            // Point is safe if it's inside the sphere minus safety margin
            return distance < (radius - safetyMargin);
        }
        else if (boxCollider != null)
        {
            // For box collider
            Vector3 localPoint = transform.InverseTransformPoint(point);
            Vector3 halfSize = boxCollider.size * 0.5f;
            Vector3 safeHalfSize = halfSize - Vector3.one * safetyMargin;
            
            // Point is safe if it's inside the box minus safety margin
            return Mathf.Abs(localPoint.x) < safeHalfSize.x &&
                   Mathf.Abs(localPoint.y) < safeHalfSize.y &&
                   Mathf.Abs(localPoint.z) < safeHalfSize.z;
        }
        else if (otherColliders != null && otherColliders.Length > 0)
        {
            // For other collider types - use closest point
            foreach (Collider col in otherColliders)
            {
                if (col != null)
                {
                    Vector3 closestPoint = col.ClosestPoint(point);
                    float distance = Vector3.Distance(point, closestPoint);
                    if (distance < safetyMargin)
                    {
                        return false; // Too close to border
                    }
                }
            }
            return true;
        }
        
        // Default to safe if no collider (shouldn't happen)
        return true;
    }

    private void CreateVisualSphere()
    {
        // Create a sphere to represent the collider
        visualSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualSphere.name = "BorderVisual_" + gameObject.name;
        
        // Remove the collider from the visual sphere to prevent physics interactions
        Collider visualCollider = visualSphere.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }
        
        // Set the visual sphere as a child of this object
        visualTransform = visualSphere.transform;
        visualTransform.SetParent(transform, false);
        
        // Set position to match the collider's center
        visualTransform.localPosition = sphereCollider.center;
        
        // Set scale to match the collider's radius
        float diameter = sphereCollider.radius * 2;
        visualTransform.localScale = new Vector3(diameter, diameter, diameter);
        
        // Create a material for the visualization
        if (visualizerMaterial == null)
        {
            // Create a new material with mobile-compatible settings
            instanceMaterial = new Material(Shader.Find("Transparent/Diffuse"));
            
            // For mobile compatibility, use simpler transparency settings
            instanceMaterial.SetFloat("_Mode", 3); // Set to transparent mode
            instanceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            instanceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            instanceMaterial.SetInt("_ZWrite", 0);
            instanceMaterial.DisableKeyword("_ALPHATEST_ON");
            instanceMaterial.EnableKeyword("_ALPHABLEND_ON");
            instanceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            instanceMaterial.renderQueue = 3000;
        }
        else
        {
            // Create an instance of the provided material
            instanceMaterial = new Material(visualizerMaterial);
        }
        
        // Set the color with more transparency for mobile
        instanceMaterial.color = new Color(sphereColor.r, sphereColor.g, sphereColor.b, 0.15f);
        
        // Apply the material to the sphere
        Renderer renderer = visualSphere.GetComponent<Renderer>();
        renderer.material = instanceMaterial;
        
        // Disable shadow casting for the visual sphere
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void Update()
    {
        // If the collider's properties change, update the visual sphere
        if (visualSphere != null && sphereCollider != null)
        {
            // Update position if needed
            if (visualTransform.localPosition != sphereCollider.center)
            {
                visualTransform.localPosition = sphereCollider.center;
            }
            
            // Update scale if needed
            float diameter = sphereCollider.radius * 2;
            Vector3 targetScale = new Vector3(diameter, diameter, diameter);
            if (visualTransform.localScale != targetScale)
            {
                visualTransform.localScale = targetScale;
            }
        }
    }

    // Allow toggling the visualization at runtime
    public void ToggleVisibility(bool visible)
    {
        if (visualSphere != null)
        {
            visualSphere.SetActive(visible);
        }
        else if (visible && showInGame)
        {
            CreateVisualSphere();
        }
    }
} 