using UnityEngine;

public class PlayerOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineAll;
    
    [Header("References")]
    [SerializeField] private GameObject playerAnt; // Assign in inspector if not on same GameObject
    
    // Track if we need to check outline recovery
    [SerializeField] private bool monitorOutline = true;
    [SerializeField] private float checkInterval = 0.2f;
    
    private Outline outline;
    private float nextCheckTime;
    
    private void Start()
    {
        // If player reference is not set, use this GameObject
        if (playerAnt == null)
            playerAnt = gameObject;
        
        // Configure the outline
        SetupOutline();
        
        // Subscribe to damage events if possible
        LivingEntity livingEntity = playerAnt.GetComponent<LivingEntity>();
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
        
        // Periodically check if outline is missing and needs to be restored
        if (monitorOutline && Time.time > nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            
            // Check if we need to restore outline
            if (outline == null || !outline.enabled)
            {
                Debug.Log("Outline missing or disabled - restoring");
                SetupOutline();
            }
            else
            {
                // Check if outline materials need to be fixed
                FixOutlineColor();
            }
        }
    }
    
    private void SetupOutline()
    {
        // Add outline component if it doesn't exist
        outline = playerAnt.GetComponent<Outline>();
        if (outline == null)
            outline = playerAnt.AddComponent<Outline>();
        
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
        // Force outline to be visible after damage
        Invoke("SetupOutline", 0.01f); // Very short delay to ensure it runs after any damage effects
    }
    
    // This modifies the actual material on the GameObject after the Outline component has created it
    private void FixOutlineColor()
    {
        if (outline == null) return;
        
        // Find all renderers that might have the outline material
        Renderer[] renderers = playerAnt.GetComponentsInChildren<Renderer>();
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
        LivingEntity livingEntity = playerAnt?.GetComponent<LivingEntity>();
        if (livingEntity != null)
        {
            livingEntity.OnDamaged.RemoveListener(OnEntityDamaged);
        }
    }
} 