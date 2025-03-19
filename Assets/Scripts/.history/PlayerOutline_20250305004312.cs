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
        
        // Force the color value
        outline.OutlineColor = outlineColor;
        
        // Force color through material
        ForceOutlineColor();
        
        // Debug the outline to verify setup
        Debug.Log($"Outline component created with color: {outline.OutlineColor}");
    }
    
    private void Update()
    {
        // Check if color needs updating
        if (outline != null && outline.OutlineColor != outlineColor)
        {
            outline.OutlineColor = outlineColor;
            ForceOutlineColor();
            Debug.Log($"Updated outline color to: {outlineColor}");
        }
    }
    
    // Direct material manipulation to force color
    private void ForceOutlineColor()
    {
        if (outline == null) return;
        
        // Access the material directly using reflection
        System.Reflection.FieldInfo fieldInfo = outline.GetType().GetField("outlineMaterial", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (fieldInfo != null)
        {
            Material outlineMaterial = fieldInfo.GetValue(outline) as Material;
            if (outlineMaterial != null)
            {
                // Apply color directly to the material
                outlineMaterial.SetColor("_OutlineColor", outlineColor);
                
                // Also try setting these common color properties
                outlineMaterial.SetColor("_Color", outlineColor);
                outlineMaterial.SetColor("_EmissionColor", outlineColor);
                outlineMaterial.EnableKeyword("_EMISSION");
                
                Debug.Log("Applied color directly to outline material");
            }
            else
            {
                Debug.LogWarning("Couldn't access outline material");
            }
        }
        else
        {
            // Alternative approach: find outline materials in the renderer
            Renderer renderer = playerAnt.GetComponent<Renderer>();
            if (renderer != null)
            {
                foreach (Material mat in renderer.materials)
                {
                    if (mat.shader.name.Contains("Outline"))
                    {
                        mat.SetColor("_OutlineColor", outlineColor);
                        mat.SetColor("_Color", outlineColor);
                        Debug.Log("Applied color to renderer material: " + mat.name);
                    }
                }
            }
        }
    }
} 