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
    
    private void Awake()
    {
        // Cache collider references
        sphereCollider = GetComponent<SphereCollider>();
        capsuleCollider = GetComponent<CapsuleCollider>(); // Get capsule collider reference
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