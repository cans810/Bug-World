using UnityEngine;

public class PlayerOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineAll;
    
    [Header("References")]
    [SerializeField] private GameObject playerAnt; // Assign in inspector if not on same GameObject
    
    private Outline outline;
    
    private void Start()
    {
        // If player reference is not set, use this GameObject
        if (playerAnt == null)
            playerAnt = gameObject;
        
        // Add outline component if it doesn't exist
        outline = playerAnt.GetComponent<Outline>();
        if (outline == null)
            outline = playerAnt.AddComponent<Outline>();
        
        // Configure outline
        outline.OutlineMode = outlineMode;
        outline.OutlineWidth = outlineWidth;
        outline.OutlineColor = outlineColor;
        
        // Delay the color update to make sure materials are initialized
        Invoke("FixOutlineColor", 0.1f);
    }
    
    private void Update()
    {
        // Monitor for color changes
        if (outline != null && outline.OutlineColor != outlineColor)
        {
            outline.OutlineColor = outlineColor;
            FixOutlineColor();
        }
    }
    
    // This modifies the actual material on the GameObject after the Outline component has created it
    private void FixOutlineColor()
    {
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
                if (materials[i].name.Contains("OutlineFill"))
                {
                    // Set color directly on the material instance
                    materials[i].SetColor("_OutlineColor", outlineColor);
                    materialChanged = true;
                    Debug.Log($"Fixed outline color on material {materials[i].name}");
                }
            }
            
            // If we modified any materials, apply them back to the renderer
            if (materialChanged)
            {
                renderer.materials = materials;
            }
        }
    }
} 