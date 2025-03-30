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
        
        int segments = 60;
        perimeterLineRenderer.positionCount = segments;
        
        // Calculate the half-length of the capsule's "cylinder" part
        float halfLength = (height - 2 * radius) * 0.5f;
        
        // Draw the perimeter based on capsule orientation
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * 2f * Mathf.PI;
            Vector3 point = Vector3.zero;
            
            // Determine position based on the angle
            if (direction == 0) // X-axis
            {
                // First half - right side
                if (angle <= Mathf.PI)
                {
                    if (angle <= Mathf.PI / 2) // First quarter - top curve
                    {
                        // Right end cap - top half
                        float capAngle = angle;
                        point.x = center.x + halfLength + radius * Mathf.Cos(capAngle);
                        point.z = center.z + radius * Mathf.Sin(capAngle);
                    }
                    else // Second quarter - bottom curve
                    {
                        // Right end cap - bottom half
                        float capAngle = angle;
                        point.x = center.x + halfLength + radius * Mathf.Cos(capAngle);
                        point.z = center.z + radius * Mathf.Sin(capAngle);
                    }
                }
                // Second half - left side
                else
                {
                    if (angle <= 3 * Mathf.PI / 2) // Third quarter - bottom curve
                    {
                        // Left end cap - bottom half
                        float capAngle = angle;
                        point.x = center.x - halfLength + radius * Mathf.Cos(capAngle);
                        point.z = center.z + radius * Mathf.Sin(capAngle);
                    }
                    else // Fourth quarter - top curve
                    {
                        // Left end cap - top half
                        float capAngle = angle;
                        point.x = center.x - halfLength + radius * Mathf.Cos(capAngle);
                        point.z = center.z + radius * Mathf.Sin(capAngle);
                    }
                }
            }
            else if (direction == 2) // Z-axis
            {
                // First half - front side
                if (angle <= Mathf.PI)
                {
                    if (angle <= Mathf.PI / 2) // First quarter - right curve
                    {
                        // Front end cap - right half
                        float capAngle = angle;
                        point.x = center.x + radius * Mathf.Sin(capAngle);
                        point.z = center.z + halfLength + radius * Mathf.Cos(capAngle);
                    }
                    else // Second quarter - left curve
                    {
                        // Front end cap - left half
                        float capAngle = angle;
                        point.x = center.x + radius * Mathf.Sin(capAngle);
                        point.z = center.z + halfLength + radius * Mathf.Cos(capAngle);
                    }
                }
                // Second half - back side
                else
                {
                    if (angle <= 3 * Mathf.PI / 2) // Third quarter - left curve
                    {
                        // Back end cap - left half
                        float capAngle = angle;
                        point.x = center.x + radius * Mathf.Sin(capAngle);
                        point.z = center.z - halfLength + radius * Mathf.Cos(capAngle);
                    }
                    else // Fourth quarter - right curve
                    {
                        // Back end cap - right half
                        float capAngle = angle;
                        point.x = center.x + radius * Mathf.Sin(capAngle);
                        point.z = center.z - halfLength + radius * Mathf.Cos(capAngle);
                    }
                }
            }
            else // Y-axis (direction == 1)
            {
                // For Y-axis, just draw a circle
                point.x = center.x + radius * Mathf.Cos(angle);
                point.z = center.z + radius * Mathf.Sin(angle);
            }
            
            // Set the point at the configured height
            point.y = perimeterHeight;
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

    // Modify this method to keep colliders enabled while disabling only visuals
    public void DisableBorderVisualization()
    {
        // Mark this border as disabled
        hasBeenDisabled = true;
        
        // DO NOT disable the collider - comment out or remove these lines
        // Collider borderCollider = GetComponent<Collider>();
        // if (borderCollider != null)
        // {
        //     borderCollider.enabled = false;
        // }
        
        // Only disable the visual elements
        if (perimeterLineRenderer != null)
        {
            perimeterLineRenderer.enabled = false;
        }
        
        // Log the border visualization change
        Debug.Log($"Border visualization (visuals only) disabled for {gameObject.name} - collider remains active");
    }
} 