using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Creates a visible representation of a Collider as a semi-transparent visualization.
/// Now supports SphereCollider, BoxCollider, and CapsuleCollider types.
/// </summary>
public class BorderVisualizer : MonoBehaviour
{
    
    [Header("Level-Based Visibility")]
    [SerializeField] private int requiredPlayerLevel = 0; // Level required to enter this area
    private bool hasBeenDisabled = false;
    
    // Cached components
    public SphereCollider sphereCollider;
    public CapsuleCollider capsuleCollider; // Add reference for capsule collider
    
    
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

    // Add this method to help debug visualization problems
    public void DebugVisualization()
    {
        Debug.Log($"--- Border Visualization Debug for {gameObject.name} ---");
        Debug.Log($"Required Level: {requiredPlayerLevel}");
        Debug.Log($"Show Radius: {showRadius}, Fill Circle: {fillCircle}");
        Debug.Log($"Sphere Collider: {(sphereCollider != null ? "Present" : "Missing")}");
        Debug.Log($"Capsule Collider: {(capsuleCollider != null ? "Present" : "Missing")}");
        
        if (visualSphere != null)
        {
            Debug.Log($"Visualization Object: Active");
            
            // Check for renderers
            Renderer[] renderers = visualSphere.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"Found {renderers.Length} renderers");
            
            foreach (var renderer in renderers)
            {
                Debug.Log($"Renderer: {renderer.gameObject.name}, Material: {renderer.material.name}, Color: {renderer.material.color}");
                Debug.Log($"Is visible: {renderer.isVisible}, Enabled: {renderer.enabled}");
            }
            
            // Check for mesh filters
            MeshFilter[] meshFilters = visualSphere.GetComponentsInChildren<MeshFilter>(true);
            Debug.Log($"Found {meshFilters.Length} mesh filters");
            
            // Add a temporary visual marker at the position for testing
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "DEBUG_MARKER_" + gameObject.name;
            marker.transform.position = transform.position + Vector3.up * 2f;
            marker.transform.localScale = Vector3.one * 0.5f;
            marker.GetComponent<Renderer>().material.color = Color.magenta;
            Debug.Log($"Created debug marker at {marker.transform.position}");
            GameObject.Destroy(marker, 5f); // Clean up after 5 seconds
        }
        else
        {
            Debug.Log("Visualization Object: NULL");
        }
        
        // Force recreation of visualization
        if (visualSphere != null)
        {
            Debug.Log("Destroying and recreating visualization...");
            Destroy(visualSphere);
            visualSphere = null;
        }
        
        CreateVisualSphere();
        Debug.Log("--- End Debug ---");
    }
} 