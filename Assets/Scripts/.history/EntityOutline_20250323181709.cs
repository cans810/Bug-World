using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EntityOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] public Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineAll;
    
    [Header("References")]
    [SerializeField] private GameObject entity; // Assign in inspector if not on same GameObject
    
    [Header("Recovery Settings")]
    [SerializeField] private bool monitorOutline = true;
    [SerializeField] private float checkInterval = 0.2f;
    [SerializeField] private float recoveryDelay = 0.05f; // Delay before recovering outline after damage
    
    [Header("Layer Exclusions")]
    [SerializeField] private bool excludeCarriedLoot = true;
    [SerializeField] private string[] excludedLayers = new string[] { "CarriedLoot", "Loot" };
    
    private Outline outline;
    private float nextCheckTime;
    
    // Track excluded renderers so we can re-enable them
    private List<Renderer> disabledRenderers = new List<Renderer>();
    
    private void Start()
    {
        // If player reference is not set, use this GameObject
        if (entity == null)
            entity = gameObject;
        
        // Configure the outline
        SetupOutline();
        
        // Subscribe to damage events if possible
        LivingEntity livingEntity = entity.GetComponent<LivingEntity>();
        if (livingEntity != null)
        {
            livingEntity.OnDamaged.AddListener(OnEntityDamaged);
        }
        
        // Schedule removal of outlines from excluded objects
        if (excludeCarriedLoot)
        {
            InvokeRepeating("RemoveOutlinesFromExcludedObjects", 0.1f, 0.5f);
        }
    }
    
    private void Update()
    {
        // Monitor for color changes
        if (outline != null && outline.OutlineColor != outlineColor)
        {
            outline.OutlineColor = outlineColor;
            FixOutlineColor();
        }
        
        // Periodically check if outline is missing or needs to be restored
        if (monitorOutline && Time.time > nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            
            // Check if outline needs restoration
            CheckAndRestoreOutline();
        }
    }
    
    private void CheckAndRestoreOutline()
    {
        // Check if outline exists and has proper materials
        bool needsRestore = false;
        
        if (outline == null || !outline.enabled)
        {
            needsRestore = true;
        }
        else
        {
            // Check if materials are properly set
            Renderer[] renderers = entity.GetComponentsInChildren<Renderer>();
            bool foundOutlineMaterial = false;
            
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                foreach (Material mat in materials)
                {
                    if (mat != null && mat.name.Contains("OutlineFill"))
                    {
                        foundOutlineMaterial = true;
                        break;
                    }
                }
                
                if (foundOutlineMaterial) break;
            }
            
            if (!foundOutlineMaterial)
            {
                needsRestore = true;
            }
        }
        
        if (needsRestore)
        {
            Debug.Log("Outline needs restoration - performing full reset");
            PerformFullOutlineReset();
        }
    }
    
    private void SetupOutline()
    {
        // Add outline component if it doesn't exist
        outline = entity.GetComponent<Outline>();
        if (outline == null)
            outline = entity.AddComponent<Outline>();
        
        // Make sure it's enabled
        outline.enabled = true;
        
        // Configure outline
        outline.OutlineMode = outlineMode;
        outline.OutlineWidth = outlineWidth;
        outline.OutlineColor = outlineColor;
        
        // Handle layer exclusions by disabling renderers temporarily
        if (excludeCarriedLoot)
        {
            DisableExcludedRenderers();
        }
        
        // Fix color immediately and after a short delay
        FixOutlineColor();
        Invoke("FixOutlineColor", 0.1f);
        
        // Add an additional delayed fix to ensure color is applied
        Invoke("FixOutlineColor", 0.5f);
    }
    
    // Disable renderers on excluded layers before outline processing
    private void DisableExcludedRenderers()
    {
        // Clear the list first
        disabledRenderers.Clear();
        
        // Get all renderers in children
        Renderer[] allRenderers = entity.GetComponentsInChildren<Renderer>(true);
        
        foreach (Renderer renderer in allRenderers)
        {
            // Check if this renderer is on an excluded layer
            if (IsOnExcludedLayer(renderer.gameObject))
            {
                // If the renderer is enabled, disable it and add to our list
                if (renderer.enabled)
                {
                    renderer.enabled = false;
                    disabledRenderers.Add(renderer);
                    Debug.Log($"Temporarily disabled renderer on {renderer.gameObject.name} to exclude from outline (layer: {LayerMask.LayerToName(renderer.gameObject.layer)})");
                }
            }
        }
        
        // Schedule re-enabling of renderers
        Invoke("ReEnableDisabledRenderers", 0.05f);
    }
    
    // Re-enable renderers that we disabled
    private void ReEnableDisabledRenderers()
    {
        foreach (Renderer renderer in disabledRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
        disabledRenderers.Clear();
    }
    
    // Called when entity takes damage
    private void OnEntityDamaged()
    {
        // Force outline to be visible after damage with a full reset
        Invoke("PerformFullOutlineReset", recoveryDelay);
    }
    
    // This performs a full enable/disable cycle of the outline component to force recreation
    private void PerformFullOutlineReset()
    {
        if (outline != null)
        {
            // Store current settings
            Color currentColor = outline.OutlineColor;
            float currentWidth = outline.OutlineWidth;
            Outline.Mode currentMode = outline.OutlineMode;
            
            // Disable the outline component
            outline.enabled = false;
            
            // Wait a frame to ensure it's fully disabled
            StartCoroutine(EnableAfterDisable(currentColor, currentWidth, currentMode));
        }
        else
        {
            // If outline doesn't exist, just set it up
            SetupOutline();
        }
    }
    
    // Coroutine to wait a frame before re-enabling
    private System.Collections.IEnumerator EnableAfterDisable(Color color, float width, Outline.Mode mode)
    {
        // Wait for end of frame to ensure disable has taken effect
        yield return new WaitForEndOfFrame();
        
        // Re-enable with saved settings
        if (outline != null)
        {
            outline.enabled = true;
            outline.OutlineColor = color;
            outline.OutlineWidth = width;
            outline.OutlineMode = mode;
            
            // Fix materials after re-enabling
            FixOutlineColor();
            
            // Schedule another material fix for good measure
            Invoke("FixOutlineColor", 0.1f);
        }
    }
    
    // This modifies the actual material on the GameObject after the Outline component has created it
    private void FixOutlineColor()
    {
        if (outline == null) return;
        
        // Find all renderers that might have the outline material
        Renderer[] renderers = entity.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // Look through all materials on this renderer
            Material[] materials = renderer.materials; // Note: this returns a copy we can modify
            bool materialChanged = false;
            
            for (int i = 0; i < materials.Length; i++)
            {
                // Check if this is an outline material by name
                if (materials[i] != null && materials[i].name.Contains("OutlineFill"))
                {
                    // Set color directly on the material instance
                    materials[i].SetColor("_OutlineColor", outlineColor);
                    materialChanged = true;
                }
            }
            
            // If we modified any materials, apply them back to the renderer
            if (materialChanged)
            {
                renderer.materials = materials;
            }
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events if we were subscribed
        LivingEntity livingEntity = entity?.GetComponent<LivingEntity>();
        if (livingEntity != null)
        {
            livingEntity.OnDamaged.RemoveListener(OnEntityDamaged);
        }
        
        // Cancel any pending invokes
        CancelInvoke("ReEnableDisabledRenderers");
        CancelInvoke("RemoveOutlinesFromExcludedObjects");
    }

    // Add this public method to the PlayerOutline class to allow other scripts to change the outline color
    public Color OutlineColor
    {
        get { return outlineColor; }
    }

    // Add this property to fix the compilation errors
    public Color CurrentOutlineColor
    {
        get { return outlineColor; }
    }

    public void SetOutlineColor(Color newColor)
    {
        outlineColor = newColor;
        
        if (outline != null)
        {
            outline.OutlineColor = newColor;
            
            // Apply color immediately
            FixOutlineColor();
            
            // Schedule additional fixes to ensure color is applied
            CancelInvoke("FixOutlineColor");
            Invoke("FixOutlineColor", 0.1f);
            Invoke("FixOutlineColor", 0.5f);
            
            Debug.Log($"Set outline color to {newColor} on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Tried to set outline color on {gameObject.name} but outline component is null");
            // Try to recreate the outline
            SetupOutline();
        }
    }

    // Helper method to check if a GameObject is on an excluded layer
    private bool IsOnExcludedLayer(GameObject obj)
    {
        string layerName = LayerMask.LayerToName(obj.layer);
        
        foreach (string excludedLayer in excludedLayers)
        {
            if (layerName == excludedLayer)
                return true;
        }
        
        return false;
    }

    // Update the ForceUpdate method
    public void ForceUpdate()
    {
        if (outline != null)
        {
            // Store current settings
            bool wasEnabled = outline.enabled;
            Color currentColor = outline.OutlineColor;
            
            // Disable and re-enable to force a refresh
            outline.enabled = false;
            
            // Handle layer exclusions
            if (excludeCarriedLoot)
            {
                DisableExcludedRenderers();
            }
            
            // Wait a frame before re-enabling
            StartCoroutine(DelayedReEnable(currentColor));
            
            Debug.Log($"Forced outline update on {gameObject.name}. Color: {outlineColor}");
        }
        else
        {
            // If outline doesn't exist, set it up
            SetupOutline();
            Debug.Log($"Created new outline on {gameObject.name} during force update. Color: {outlineColor}");
        }
    }

    // Add a new coroutine to delay re-enabling the outline
    private IEnumerator DelayedReEnable(Color targetColor)
    {
        yield return new WaitForEndOfFrame();
        
        if (outline != null)
        {
            outline.enabled = true;
            outline.OutlineColor = targetColor;
            FixOutlineColor();
            
            // Schedule additional fixes
            Invoke("FixOutlineColor", 0.1f);
            Invoke("FixOutlineColor", 0.3f);
        }
    }

    // Add this method to the EntityOutline class to completely remove outlines from excluded objects
    private void RemoveOutlinesFromExcludedObjects()
    {
        if (!excludeCarriedLoot) return;
        
        // Find all child objects on excluded layers
        Transform[] allChildren = entity.GetComponentsInChildren<Transform>(true);
        
        foreach (Transform child in allChildren)
        {
            // Skip the entity itself
            if (child.gameObject == entity) continue;
            
            // Check if this object is on an excluded layer
            if (IsOnExcludedLayer(child.gameObject))
            {
                // Instead of destroying the Outline component, disable it
                Outline childOutline = child.GetComponent<Outline>();
                if (childOutline != null && childOutline.enabled)
                {
                    childOutline.enabled = false;
                    Debug.Log($"Disabled outline on excluded object: {child.name}");
                }
                
                // Add a LootOutlineBlocker component if it doesn't have one
                LootOutlineBlocker blocker = child.GetComponent<LootOutlineBlocker>();
                if (blocker == null)
                {
                    blocker = child.gameObject.AddComponent<LootOutlineBlocker>();
                    blocker.Initialize();
                    Debug.Log($"Added LootOutlineBlocker to {child.name}");
                }
            }
        }
    }
} 