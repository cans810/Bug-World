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
    private bool isFading = false;
    private bool hasBeenDisabled = false;
    
    // For object pooling
    private static List<BorderVisualizer> allBorders = new List<BorderVisualizer>();
    
    // Cached components
    public SphereCollider sphereCollider;
    public CapsuleCollider capsuleCollider; // Add reference for capsule collider
    public Collider[] otherColliders;
    
    private GameObject visualSphere;
    private Transform visualTransform;
    private Material instanceMaterial;
    private LineRenderer radiusLine; // Line renderer for showing the radius
    
    private void Awake()
    {
        // Cache collider references
        sphereCollider = GetComponent<SphereCollider>();
        capsuleCollider = GetComponent<CapsuleCollider>(); // Get capsule collider reference
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

    // Add a new method to check if the border should be visible based on player level
    public void CheckVisibilityForPlayerLevel(int playerLevel)
    {
        // If player level is high enough and border is still visible, fade it out
        if (requiredPlayerLevel > 0 && playerLevel >= requiredPlayerLevel && !hasBeenDisabled)
        {
            FadeOutAndDisable();
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

    // Add this to handle visualization state for loading
    public void SetVisualizationState(bool enabled)
    {
        if (!enabled && visualSphere != null)
        {
            visualSphere.SetActive(false);
            hasBeenDisabled = true;
        }
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