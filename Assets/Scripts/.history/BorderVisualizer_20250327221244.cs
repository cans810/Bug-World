using UnityEngine;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Creates a visible representation of a Collider as a semi-transparent visualization.
/// Now supports SphereCollider, BoxCollider, and CapsuleCollider types.
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
    public CapsuleCollider capsuleCollider; // Add reference for capsule collider
    
    [Header("Scene View Visualization")]
    [SerializeField] private bool showInSceneView = true;
    [SerializeField] private Color restrictedAreaColor = new Color(1f, 0.5f, 0, 0.7f); // Orange
    [SerializeField] private Color standardAreaColor = new Color(0, 0.7f, 1f, 0.5f);   // Cyan
    [SerializeField] private bool showLabels = true;
    
    // Line renderer for runtime visualization
    private LineRenderer perimeterLineRenderer;
    
    private void Awake()
    {
        // Cache collider references
        sphereCollider = GetComponent<SphereCollider>();
        capsuleCollider = GetComponent<CapsuleCollider>(); // Get capsule collider reference
    }
    
    private void Start()
    {
        if (showRuntimePerimeter)
        {
            CreatePerimeterVisual();
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
        else if (capsuleCollider != null)
        {
            DrawCapsulePerimeter();
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
    
    private void DrawCapsulePerimeter()
    {
        if (perimeterLineRenderer == null) return;
        
        // Get capsule properties
        Vector3 center = capsuleCollider.center;
        float radius = capsuleCollider.radius;
        float height = capsuleCollider.height;
        int direction = capsuleCollider.direction;
        
        // We just want to draw the outline of the capsule when viewed from above
        // This means we need to create an oval or stadium shape
        
        // Calculate dimensions for the perimeter
        Vector3 dimensions = Vector3.zero;
        
        if (direction == 0) // X axis
        {
            // X-aligned capsule, length along X
            dimensions.x = height;
            dimensions.z = radius * 2;
        }
        else if (direction == 1) // Y axis
        {
            // Y-aligned capsule, equal X and Z (circle from above)
            dimensions.x = radius * 2;
            dimensions.z = radius * 2;
        }
        else // Z axis
        {
            // Z-aligned capsule, length along Z
            dimensions.x = radius * 2;
            dimensions.z = height;
        }
        
        // Create the stadium shape
        int segments = 60;
        perimeterLineRenderer.positionCount = segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            // Add 90 degrees (π/2) to rotate the entire shape
            angle += Mathf.PI/2;
            Vector3 point = Vector3.zero;
            
            // For Y-aligned capsules, just draw a circle
            if (direction == 1)
            {
                point.x = center.x + radius * Mathf.Cos(angle);
                point.z = center.z + radius * Mathf.Sin(angle);
            }
            // For X or Z aligned capsules, draw a stadium shape (rectangle with semicircle ends)
            else
            {
                float halfLength, halfWidth;
                
                if (direction == 0) // X axis
                {
                    halfLength = height / 2 - radius;
                    halfWidth = radius;
                    
                    // Rotated semicircle logic - now checking top and bottom semicircles
                    if (angle > Mathf.PI/2 && angle < 3*Mathf.PI/2)
                    {
                        // Bottom semicircle
                        point.x = center.x - halfLength + halfWidth * Mathf.Sin(angle);
                        point.z = center.z - halfWidth * Mathf.Cos(angle);
                    }
                    else
                    {
                        // Top semicircle
                        point.x = center.x + halfLength + halfWidth * Mathf.Sin(angle);
                        point.z = center.z - halfWidth * Mathf.Cos(angle);
                    }
                }
                else // Z axis
                {
                    halfLength = height / 2 - radius;
                    halfWidth = radius;
                    
                    // Rotated semicircle logic - now checking left and right semicircles
                    if (angle > 0 && angle < Mathf.PI)
                    {
                        // Right semicircle
                        point.x = center.x + halfWidth * Mathf.Cos(angle);
                        point.z = center.z + halfLength + halfWidth * Mathf.Sin(angle);
                    }
                    else
                    {
                        // Left semicircle
                        point.x = center.x + halfWidth * Mathf.Cos(angle);
                        point.z = center.z - halfLength + halfWidth * Mathf.Sin(angle);
                    }
                }
            }
            
            // Set the height to the perimeter height
            point.y = perimeterHeight;
            
            // Add the point to the line renderer
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
        else if (capsuleCollider != null)
        {
            DrawCapsuleGizmo();
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
    
    private void DrawCapsuleGizmo()
    {
        // Get capsule properties
        Vector3 center = transform.TransformPoint(capsuleCollider.center);
        float radius = capsuleCollider.radius * Mathf.Max(transform.lossyScale.x, 
                                                    Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
        
        // Height needs to be scaled based on direction
        float height = capsuleCollider.height;
        Vector3 direction = Vector3.up; // Default Y axis
        
        if (capsuleCollider.direction == 0) // X axis
        {
            height *= transform.lossyScale.x;
            direction = transform.right;
        }
        else if (capsuleCollider.direction == 1) // Y axis
        {
            height *= transform.lossyScale.y;
            direction = transform.up;
        }
        else if (capsuleCollider.direction == 2) // Z axis
        {
            height *= transform.lossyScale.z;
            direction = transform.forward;
        }
        
        // Calculate end points
        float halfHeight = (height - 2 * radius) * 0.5f;
        Vector3 point1 = center + direction * halfHeight;
        Vector3 point2 = center - direction * halfHeight;
        
        // Draw the end cap spheres
        Gizmos.DrawWireSphere(point1, radius);
        Gizmos.DrawWireSphere(point2, radius);
        
        // Draw connecting lines
        Vector3 right = Vector3.Cross(point2 - point1, Vector3.up).normalized;
        if (right == Vector3.zero) right = Vector3.right;
        
        Vector3 forward = Vector3.Cross(right, (point2 - point1).normalized).normalized;
        
        Gizmos.DrawLine(point1 + right * radius, point2 + right * radius);
        Gizmos.DrawLine(point1 - right * radius, point2 - right * radius);
        Gizmos.DrawLine(point1 + forward * radius, point2 + forward * radius);
        Gizmos.DrawLine(point1 - forward * radius, point2 - forward * radius);
        
        // Draw ground projection
        DrawGroundCapsule(point1, point2, radius);
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
    
    private void DrawGroundCapsule(Vector3 point1, Vector3 point2, float radius)
    {
#if UNITY_EDITOR
        // Project points to ground plane
        Vector3 ground1 = new Vector3(point1.x, 0.05f, point1.z);
        Vector3 ground2 = new Vector3(point2.x, 0.05f, point2.z);
        
        // Use different color if this is a restricted area
        Color groundColor = (requiredPlayerLevel > 0) ? 
            restrictedAreaColor :  // Orange for restricted
            standardAreaColor;     // Blue for standard
        
        Handles.color = groundColor;
        
        // Draw end discs
        Handles.DrawWireDisc(ground1, Vector3.up, radius);
        Handles.DrawWireDisc(ground2, Vector3.up, radius);
        
        // Draw connecting lines
        Vector3 right = Vector3.Cross(ground2 - ground1, Vector3.up).normalized;
        if (right == Vector3.zero) right = Vector3.right;
        
        Handles.DrawLine(ground1 + right * radius, ground2 + right * radius);
        Handles.DrawLine(ground1 - right * radius, ground2 - right * radius);
        
        // Draw level requirement in center
        if (requiredPlayerLevel > 0)
        {
            Vector3 midPoint = (ground1 + ground2) * 0.5f;
            Handles.Label(midPoint, $"Level {requiredPlayerLevel}+");
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
        else if (capsuleCollider != null)
        {
            Vector3 center = transform.TransformPoint(capsuleCollider.center);
            labelPosition = center + Vector3.up * (capsuleCollider.height * 0.5f + 1f);
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
} 