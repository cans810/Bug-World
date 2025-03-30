using UnityEngine;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Creates a visible representation of a Collider as a semi-transparent visualization.
/// Now supports SphereCollider and BoxCollider types.
/// </summary>
public class BorderVisualizer : MonoBehaviour
{
    [Header("Border Settings")]
    [SerializeField] private bool showDebugVisuals = true;
    [SerializeField] private Color borderColor = Color.red;
    [SerializeField] private Color safeAreaColor = Color.green;
    [SerializeField] private float safetyMargin = 1f; // How far inside the border is considered "safe"
    
    [Header("Runtime Visualization")]
    [SerializeField] private bool showRuntimePerimeter = true;
    [SerializeField] private float perimeterWidth = 0.05f;  // Reduced from 0.2f to make it thinner
    [SerializeField] private Material perimeterMaterial;
    [SerializeField] private float perimeterHeight = 1f;    // Reduced from 2f to place it at half-height
    
    [Header("Level-Based Visibility")]
    [SerializeField] private int requiredPlayerLevel = 0; // Level required to enter this area
    [SerializeField] private bool hasBeenDisabled = false;
    
    // Cached components
    public SphereCollider sphereCollider;
    public BoxCollider boxCollider; // Reference for box collider
    
    [Header("Scene View Visualization")]
    [SerializeField] private bool showInSceneView = true;
    [SerializeField] private Color restrictedAreaColor = new Color(1f, 0.5f, 0, 0.7f); // Orange
    [SerializeField] private Color standardAreaColor = new Color(0, 0.7f, 1f, 0.5f);   // Cyan
    [SerializeField] private bool showLabels = true;
    
    // Line renderer for runtime visualization
    private LineRenderer perimeterLineRenderer;

    public EntityGenerator entityGenerator;
    
    [Header("Entity Spawning")]
    [SerializeField] private bool controlsEntitySpawning = true; // Whether this border controls entity spawning
    [SerializeField] private bool spawningEnabled = false; // Default to disabled

    public GameObject entityContainedIcons;
    
    private void Awake()
    {
        // Cache collider references
        sphereCollider = GetComponent<SphereCollider>();
        boxCollider = GetComponent<BoxCollider>(); // Get box collider reference
    }
    
    private void Start()
    {
        if (showRuntimePerimeter)
        {
            CreatePerimeterVisual();
        }
        
        // Ensure entity icons are initially hidden
        if (entityContainedIcons != null)
        {
            entityContainedIcons.SetActive(false);
        }
    }
    
    private void CreatePerimeterVisual()
    {
        // Create a new GameObject for the perimeter
        GameObject perimeterObj = new GameObject("PerimeterVisual");
        perimeterObj.transform.parent = transform;
        perimeterObj.transform.localPosition = Vector3.zero;
        
        // Add LineRenderer component
        perimeterLineRenderer = perimeterObj.AddComponent<LineRenderer>();
        perimeterLineRenderer.startWidth = perimeterWidth;
        perimeterLineRenderer.endWidth = perimeterWidth;
        perimeterLineRenderer.useWorldSpace = false;
        perimeterLineRenderer.loop = true;
        
        // Set material
        if (perimeterMaterial != null)
        {
            perimeterLineRenderer.material = perimeterMaterial;
        }
        else
        {
            // Create a default material if none provided
            perimeterLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            perimeterLineRenderer.material.color = borderColor;
        }
        
        // Draw the appropriate perimeter based on collider type
        if (sphereCollider != null)
        {
            DrawSpherePerimeter();
        }
        else if (boxCollider != null)
        {
            DrawBoxPerimeter();
        }
    }
    
    private void DrawSpherePerimeter()
    {
        if (perimeterLineRenderer == null) return;
        
        // Get sphere properties
        Vector3 center = sphereCollider.center;
        float radius = sphereCollider.radius;
        
        // Create points for a circle at specified height
        int segments = 50;
        perimeterLineRenderer.positionCount = segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            float x = center.x + radius * Mathf.Cos(angle);
            float z = center.z + radius * Mathf.Sin(angle);
            perimeterLineRenderer.SetPosition(i, new Vector3(x, perimeterHeight, z));
        }
    }
    
    private void DrawBoxPerimeter()
    {
        if (perimeterLineRenderer == null) return;
        
        // Get box properties
        Vector3 center = boxCollider.center;
        Vector3 size = boxCollider.size;
        
        // Create points for a rectangle at the specified height
        int segments = 5; // 4 corners + 1 to close the loop
        perimeterLineRenderer.positionCount = segments;
        
        // Calculate the half extents of the box
        float halfWidth = size.x / 2;
        float halfLength = size.z / 2;
        
        // Important: Since we'll use worldspace for the linerenderer
        perimeterLineRenderer.useWorldSpace = true;
        
        // Set the four corners of the box at the perimeter height
        Vector3[] corners = new Vector3[segments];
        corners[0] = new Vector3(-halfWidth, 0, -halfLength);
        corners[1] = new Vector3(halfWidth, 0, -halfLength);
        corners[2] = new Vector3(halfWidth, 0, halfLength);
        corners[3] = new Vector3(-halfWidth, 0, halfLength);
        corners[4] = corners[0]; // Close the loop
        
        // Apply transforms to each point and set the positions
        for (int i = 0; i < segments; i++)
        {
            // 1. Apply center offset but keep Y at 0
            Vector3 point = new Vector3(
                corners[i].x + center.x,
                0,
                corners[i].z + center.z
            );
            
            // 2. Transform to world space to get correct X/Z rotation
            point = transform.TransformPoint(point);
            
            // 3. Now set the Y position to the exact world-space perimeter height
            point.y = transform.position.y + perimeterHeight;
            
            // Set the position in world space
            perimeterLineRenderer.SetPosition(i, point);
        }
    }

    // Method to retrieve the required level for this border
    public int GetRequiredLevel()
    {
        return requiredPlayerLevel;
    }

    // Method to set the required level for this border
    public void SetRequiredLevel(int level)
    {
        requiredPlayerLevel = level;
    }

    // Add the OnDrawGizmos method to visualize in scene view
    private void OnDrawGizmos()
    {
        if (!showInSceneView) return;
        
        // Choose color based on level requirement
        Color gizmoColor = (requiredPlayerLevel > 0) ? restrictedAreaColor : standardAreaColor;
        Gizmos.color = gizmoColor;
        
        // Draw the appropriate collider type
        if (sphereCollider != null)
        {
            DrawSphereGizmo();
        }
        else if (boxCollider != null)
        {
            DrawBoxGizmo();
        }
        
        // Draw label if needed
        if (showLabels)
        {
            DrawLabel();
        }
    }
    
    private void DrawSphereGizmo()
    {
        // Get sphere properties
        Vector3 center = transform.TransformPoint(sphereCollider.center);
        float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, 
                                                   Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
        
        // Draw wireframe
        Gizmos.DrawWireSphere(center, radius);
        
        // Draw ground disc
        DrawGroundDisc(center, radius);
    }
    
    private void DrawBoxGizmo()
    {
        // Get box properties
        Vector3 center = transform.TransformPoint(boxCollider.center);
        Vector3 size = Vector3.Scale(boxCollider.size, transform.lossyScale);
        
        // Create a matrix for the box rotation and position
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        
        // Draw wireframe
        Gizmos.DrawWireCube(Vector3.zero, size);
        
        // Reset matrix before drawing ground projection
        Gizmos.matrix = Matrix4x4.identity;
        
        // Draw ground projection
        DrawGroundBox(center, size);
    }
    
    private void DrawGroundDisc(Vector3 center, float radius)
    {
#if UNITY_EDITOR
        // Draw a disc on the ground plane
        Vector3 groundCenter = new Vector3(center.x, 0.05f, center.z);
        
        // Use different color if this is a restricted area
        Color groundColor = (requiredPlayerLevel > 0) ? 
            restrictedAreaColor :  // Orange for restricted
            standardAreaColor;     // Blue for standard
        
        Handles.color = groundColor;
        Handles.DrawWireDisc(groundCenter, Vector3.up, radius);
        
        // Draw level requirement in center
        if (requiredPlayerLevel > 0)
        {
            Handles.Label(groundCenter, $"Level {requiredPlayerLevel}+");
        }
#endif
    }
    
    private void DrawGroundBox(Vector3 center, Vector3 size)
    {
#if UNITY_EDITOR
        // Project box to ground plane
        Vector3 groundCenter = new Vector3(center.x, 0.05f, center.z);
        
        // Use different color if this is a restricted area
        Color groundColor = (requiredPlayerLevel > 0) ? 
            restrictedAreaColor :  // Orange for restricted
            standardAreaColor;     // Blue for standard
        
        Handles.color = groundColor;
        
        // Create rotated ground rectangle
        Vector3 forward = transform.forward * size.z * 0.5f;
        Vector3 right = transform.right * size.x * 0.5f;
        
        Vector3 corner1 = groundCenter - right - forward;
        Vector3 corner2 = groundCenter + right - forward;
        Vector3 corner3 = groundCenter + right + forward;
        Vector3 corner4 = groundCenter - right + forward;
        
        // Draw rectangle on the ground
        Handles.DrawLine(corner1, corner2);
        Handles.DrawLine(corner2, corner3);
        Handles.DrawLine(corner3, corner4);
        Handles.DrawLine(corner4, corner1);
        
        // Draw level requirement in center
        if (requiredPlayerLevel > 0)
        {
            Handles.Label(groundCenter, $"Level {requiredPlayerLevel}+");
        }
#endif
    }
    
    private void DrawLabel()
    {
#if UNITY_EDITOR
        Vector3 labelPosition;
        
        if (sphereCollider != null)
        {
            Vector3 center = transform.TransformPoint(sphereCollider.center);
            labelPosition = center + Vector3.up * (sphereCollider.radius + 1f);
        }
        else if (boxCollider != null)
        {
            Vector3 center = transform.TransformPoint(boxCollider.center);
            labelPosition = center + Vector3.up * (boxCollider.size.y * 0.5f + 1f);
        }
        else
        {
            labelPosition = transform.position + Vector3.up * 2f;
        }
        
        string labelText = gameObject.name;
        if (requiredPlayerLevel > 0)
        {
            labelText += $" (Level {requiredPlayerLevel}+)";
        }
        
        Handles.Label(labelPosition, labelText);
#endif
    }

    // Add this method to BorderVisualizer.cs to handle deactivating the icons
    public void DeactivateEntityIcons()
    {
        if (entityContainedIcons != null)
        {
            entityContainedIcons.SetActive(false);
            Debug.Log($"Deactivated entity icons for {gameObject.name}");
        }
    }

    // Add this method to BorderVisualizer.cs to activate the icons
    public void ActivateEntityIcons()
    {
        // Only activate if this area is still locked (hasn't been disabled)
        if (!hasBeenDisabled && entityContainedIcons != null)
        {
            entityContainedIcons.SetActive(true);
            Debug.Log($"Activated entity icons for {gameObject.name}");
        }
    }

    // Update the DisableBorderVisualization method to also deactivate icons
    public void DisableBorderVisualization()
    {
        // Mark this border as disabled
        hasBeenDisabled = true;
        
        // Only disable the visual elements
        if (perimeterLineRenderer != null)
        {
            perimeterLineRenderer.enabled = false;
        }
        
        // Deactivate entity icons
        DeactivateEntityIcons();
        
        // Enable entity spawning when border is disabled
        if (controlsEntitySpawning && entityGenerator != null)
        {
            entityGenerator.EnableSpawning(true);
            spawningEnabled = true;
            Debug.Log($"Border {gameObject.name} disabled and spawning enabled for its area");
        }
        
        // Log the border visualization change
        Debug.Log($"Border visualization (visuals only) disabled for {gameObject.name} - collider remains active");
    }

    // Add a method to directly enable/disable spawning
    public void SetSpawningEnabled(bool enabled)
    {
        if (controlsEntitySpawning && entityGenerator != null)
        {
            entityGenerator.EnableSpawning(enabled);
            spawningEnabled = enabled;
            Debug.Log($"Spawning for {gameObject.name} area set to {enabled}");
        }
    }

    // Add method to check if this border has spawning enabled
    public bool IsSpawningEnabled()
    {
        return spawningEnabled;
    }
} 