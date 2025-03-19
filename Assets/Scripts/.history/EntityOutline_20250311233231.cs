using UnityEngine;
using System.Collections;

public class EntityOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineAll;
    
    [Header("References")]
    [SerializeField] private GameObject entity; // Assign in inspector if not on same GameObject
    
    [Header("Recovery Settings")]
    [SerializeField] private bool monitorOutline = true;
    [SerializeField] private float checkInterval = 0.2f;
    [SerializeField] private float recoveryDelay = 0.05f; // Delay before recovering outline after damage
    
    private Outline outline;
    private float nextCheckTime;
    
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
        
        // Fix color immediately and after a short delay
        FixOutlineColor();
        Invoke("FixOutlineColor", 0.1f);
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
    }

    // Add this public method to the PlayerOutline class to allow other scripts to change the outline color
    public Color OutlineColor
    {
        get { return outlineColor; }
    }

    public void SetOutlineColor(Color newColor)
    {
        outlineColor = newColor;
        
        if (outline != null)
        {
            outline.OutlineColor = newColor;
            FixOutlineColor();
        }
    }

    // Add this method to force an immediate update of the outline
    public void ForceUpdate()
    {
        if (outline != null)
        {
            // Disable and re-enable to force a refresh
            bool wasEnabled = outline.enabled;
            outline.enabled = false;
            outline.enabled = true;
            
            // Set color again
            outline.OutlineColor = outlineColor;
            FixOutlineColor();
            
            Debug.Log($"Forced outline update on {gameObject.name}. Color: {outlineColor}");
        }
        else
        {
            // If outline doesn't exist, set it up
            SetupOutline();
            Debug.Log($"Created new outline on {gameObject.name} during force update. Color: {outlineColor}");
        }
    }
} 