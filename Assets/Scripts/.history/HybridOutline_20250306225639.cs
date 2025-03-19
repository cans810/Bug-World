using UnityEngine;

public class HybridOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;
    [SerializeField] private bool useCustomFallback = true;
    
    [Header("References")]
    [SerializeField] private GameObject entity;
    
    private EntityOutline primaryOutline;
    private CustomOutline fallbackOutline;
    
    private void Start()
    {
        if (entity == null)
            entity = gameObject;
            
        // Try primary outline first
        primaryOutline = entity.AddComponent<EntityOutline>();
        primaryOutline.SetOutlineColor(outlineColor);
        
        // If fallback is enabled, check after a delay if primary is working
        if (useCustomFallback)
        {
            Invoke("CheckAndApplyFallback", 1.0f);
        }
    }
    
    private void CheckAndApplyFallback()
    {
        // Check if the primary outline appears to be working
        bool primaryWorking = false;
        
        Renderer[] renderers = entity.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.name.Contains("OutlineFill"))
                {
                    primaryWorking = true;
                    break;
                }
            }
            if (primaryWorking) break;
        }
        
        // If primary isn't working, use fallback
        if (!primaryWorking)
        {
            Debug.Log($"Primary outline not working for {entity.name}, using fallback");
            
            // Disable primary outline
            if (primaryOutline != null)
            {
                Destroy(primaryOutline);
            }
            
            // Add fallback outline
            fallbackOutline = entity.AddComponent<CustomOutline>();
        }
    }
    
    public void SetOutlineColor(Color newColor)
    {
        outlineColor = newColor;
        
        if (primaryOutline != null)
            primaryOutline.SetOutlineColor(newColor);
    }
} 