using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Draws Gizmos for border visualization in the Unity Editor scene view.
/// Attach this to objects with SphereCollider or CapsuleCollider components
/// that represent level-based border areas.
/// </summary>
public class BorderGizmoDrawer : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color standardBorderColor = new Color(0.5f, 0.5f, 1f, 0.4f);  // Light blue
    [SerializeField] private Color restrictedBorderColor = new Color(1f, 0.5f, 0.5f, 0.4f); // Light red
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private Color safeAreaColor = new Color(0.5f, 1f, 0.5f, 0.2f); // Light green
    [SerializeField] private float safetyMargin = 1f; // How far inside the border is considered "safe"

    [Header("Label Settings")]
    [SerializeField] private bool showLabels = true;
    [SerializeField] private float labelOffset = 2f; // Height above the border
    
    // Cache references to colliders
    private SphereCollider sphereCollider;
    private CapsuleCollider capsuleCollider;
    private BorderVisualizer borderVisualizer;
    
    private void OnEnable()
    {
        // Cache collider references
        sphereCollider = GetComponent<SphereCollider>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        borderVisualizer = GetComponent<BorderVisualizer>();
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Get required level from BorderVisualizer if it exists
        int requiredLevel = 0;
        if (borderVisualizer != null)
        {
            requiredLevel = borderVisualizer.GetRequiredLevel();
        }
        
        // Choose color based on level requirement
        Gizmos.color = (requiredLevel > 0) ? restrictedBorderColor : standardBorderColor;
        
        // Draw appropriate collider visualization
        if (sphereCollider != null)
        {
            DrawSphereGizmo(requiredLevel);
        }
        else if (capsuleCollider != null)
        {
            DrawCapsuleGizmo(requiredLevel);
        }
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw additional info when selected
        if (!showGizmos) return;
        
        // Get required level from BorderVisualizer if it exists
        int requiredLevel = 0;
        if (borderVisualizer != null)
        {
            requiredLevel = borderVisualizer.GetRequiredLevel();
        }
        
        // Draw labels if enabled
        if (showLabels)
        {
            DrawLabel(requiredLevel);
        }
    }
#endif
    
    private void DrawSphereGizmo(int requiredLevel)
    {
        // Draw sphere border
        Vector3 center = transform.TransformPoint(sphereCollider.center);
        float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, 
                                                     Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
        
        // Draw the border
        Gizmos.DrawWireSphere(center, radius);
        
        // Draw the safe zone
        Gizmos.color = safeAreaColor;
        Gizmos.DrawWireSphere(center, radius - safetyMargin);
        
        // Draw solid sphere with low alpha for visual clarity
        Color solidColor = Gizmos.color;
        solidColor.a *= 0.2f;
        Gizmos.color = solidColor;
        Gizmos.DrawSphere(center, radius);
        
        // Draw ground disc for clarity
        DrawGroundDisc(center, radius, requiredLevel);
    }
    
    private void DrawCapsuleGizmo(int requiredLevel)
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
        
        // Calculate end points - accounting for the actual capsule shape
        float halfHeight = (height - 2 * radius) * 0.5f;
        Vector3 point1 = center + direction * halfHeight;
        Vector3 point2 = center - direction * halfHeight;
        
        // Draw the end cap spheres
        Gizmos.DrawWireSphere(point1, radius);
        Gizmos.DrawWireSphere(point2, radius);
        
        // Draw the connecting lines
        DrawCapsuleConnectingLines(point1, point2, radius);
        
        // Draw ground projection
        DrawGroundCapsule(point1, point2, radius, requiredLevel);
    }
    
    private void DrawCapsuleConnectingLines(Vector3 point1, Vector3 point2, float radius)
    {
        // Draw lines connecting the two spheres of the capsule
        Vector3 right = Vector3.Cross(point2 - point1, Vector3.up).normalized;
        if (right == Vector3.zero) right = Vector3.right;
        
        Vector3 forward = Vector3.Cross(right, (point2 - point1).normalized).normalized;
        
        // Draw 4 connecting lines
        Gizmos.DrawLine(point1 + right * radius, point2 + right * radius);
        Gizmos.DrawLine(point1 - right * radius, point2 - right * radius);
        Gizmos.DrawLine(point1 + forward * radius, point2 + forward * radius);
        Gizmos.DrawLine(point1 - forward * radius, point2 - forward * radius);
    }
    
    private void DrawGroundDisc(Vector3 center, float radius, int requiredLevel)
    {
        // Draw a disc on the ground plane (Y = 0)
        Vector3 groundCenter = new Vector3(center.x, 0.05f, center.z); // Slightly above ground to prevent z-fighting
        
        // Use a dashed circle for the ground projection
#if UNITY_EDITOR
        Color groundColor = (requiredLevel > 0) ? 
            new Color(1f, 0.5f, 0, 0.7f) : // Orange for restricted areas
            new Color(0, 0.7f, 1f, 0.5f);  // Cyan for normal areas
        
        Handles.color = groundColor;
        Handles.DrawWireDisc(groundCenter, Vector3.up, radius);
        
        // Draw filled disc with very low alpha
        Color filledColor = groundColor;
        filledColor.a = 0.2f;
        Handles.color = filledColor;
        Handles.DrawSolidDisc(groundCenter, Vector3.up, radius);
        
        // Draw label if needed
        if (requiredLevel > 0)
        {
            Handles.Label(groundCenter, $"Level {requiredLevel}+");
        }
#endif
    }
    
    private void DrawGroundCapsule(Vector3 point1, Vector3 point2, float radius, int requiredLevel)
    {
        // Project points to ground plane
        Vector3 ground1 = new Vector3(point1.x, 0.05f, point1.z);
        Vector3 ground2 = new Vector3(point2.x, 0.05f, point2.z);
        
#if UNITY_EDITOR
        Color groundColor = (requiredLevel > 0) ? 
            new Color(1f, 0.5f, 0, 0.7f) : // Orange for restricted areas
            new Color(0, 0.7f, 1f, 0.5f);  // Cyan for normal areas
        
        Handles.color = groundColor;
        
        // Draw end discs
        Handles.DrawWireDisc(ground1, Vector3.up, radius);
        Handles.DrawWireDisc(ground2, Vector3.up, radius);
        
        // Draw connecting lines for the capsule
        Vector3 right = Vector3.Cross(ground2 - ground1, Vector3.up).normalized;
        if (right == Vector3.zero) right = Vector3.right;
        
        Handles.DrawLine(ground1 + right * radius, ground2 + right * radius);
        Handles.DrawLine(ground1 - right * radius, ground2 - right * radius);
        
        // Draw filled area with very low alpha
        Color filledColor = groundColor;
        filledColor.a = 0.15f;
        Handles.color = filledColor;
        
        // Draw center label if needed
        if (requiredLevel > 0)
        {
            Vector3 midPoint = (ground1 + ground2) * 0.5f;
            Handles.Label(midPoint, $"Level {requiredLevel}+");
        }
#endif
    }
    
    private void DrawLabel(int requiredLevel)
    {
#if UNITY_EDITOR
        if (!showLabels) return;
        
        Vector3 labelPosition;
        if (sphereCollider != null)
        {
            Vector3 center = transform.TransformPoint(sphereCollider.center);
            labelPosition = center + Vector3.up * (sphereCollider.radius + labelOffset);
        }
        else if (capsuleCollider != null)
        {
            Vector3 center = transform.TransformPoint(capsuleCollider.center);
            labelPosition = center + Vector3.up * (capsuleCollider.radius + labelOffset);
        }
        else
        {
            labelPosition = transform.position + Vector3.up * labelOffset;
        }
        
        string labelText = gameObject.name;
        if (requiredLevel > 0)
        {
            labelText += $" (Level {requiredLevel}+)";
        }
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = labelColor;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        
        Handles.Label(labelPosition, labelText, style);
#endif
    }
} 