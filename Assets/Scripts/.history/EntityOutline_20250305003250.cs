using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class EntityOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.red;
    [SerializeField, Range(0.001f, 0.05f)] private float outlineWidth = 0.005f;
    [SerializeField] private bool visibleThroughWalls = true;
    [SerializeField] private bool outlineEnabled = true;

    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Material[] outlineMaterials;
    
    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        
        // Store all original materials and create outline materials
        originalMaterials = new Material[renderers.Length];
        outlineMaterials = new Material[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            // Store original material
            originalMaterials[i] = renderers[i].material;
            
            // Create outline material
            outlineMaterials[i] = new Material(Shader.Find("Custom/Outline"));
            outlineMaterials[i].SetColor("_Color", originalMaterials[i].color);
            outlineMaterials[i].SetTexture("_MainTex", originalMaterials[i].mainTexture);
            outlineMaterials[i].SetColor("_OutlineColor", outlineColor);
            outlineMaterials[i].SetFloat("_OutlineWidth", outlineWidth);
            outlineMaterials[i].SetFloat("_OutlineVisibleThroughWalls", visibleThroughWalls ? 8 : 4); // 8=Always, 4=LEqual
        }
        
        // Apply initial state
        SetOutlineEnabled(outlineEnabled);
    }
    
    public void SetOutlineEnabled(bool enabled)
    {
        outlineEnabled = enabled;
        
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = enabled ? outlineMaterials[i] : originalMaterials[i];
        }
    }
    
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
        for (int i = 0; i < outlineMaterials.Length; i++)
        {
            outlineMaterials[i].SetColor("_OutlineColor", color);
        }
        
        // Re-apply materials if outline is enabled
        if (outlineEnabled)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = outlineMaterials[i];
            }
        }
    }
    
    public void SetOutlineWidth(float width)
    {
        outlineWidth = Mathf.Clamp(width, 0.001f, 0.05f);
        for (int i = 0; i < outlineMaterials.Length; i++)
        {
            outlineMaterials[i].SetFloat("_OutlineWidth", outlineWidth);
        }
    }
    
    // Toggle outline visibility through walls
    public void SetVisibleThroughWalls(bool visible)
    {
        visibleThroughWalls = visible;
        for (int i = 0; i < outlineMaterials.Length; i++)
        {
            outlineMaterials[i].SetFloat("_OutlineVisibleThroughWalls", visible ? 8 : 4);
        }
    }
    
    // Cleanup
    private void OnDestroy()
    {
        // Destroy created materials to prevent memory leaks
        for (int i = 0; i < outlineMaterials.Length; i++)
        {
            Destroy(outlineMaterials[i]);
        }
    }
} 