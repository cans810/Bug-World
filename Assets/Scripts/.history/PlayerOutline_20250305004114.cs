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
        
        // Debug the outline to verify setup
        Debug.Log($"Outline component created with color: {outline.OutlineColor}");
    }
    
    private void Update()
    {
        // Continuously update color to ensure it takes effect
        // (This is just for debugging - you can remove this later)
        if (outline != null && outline.OutlineColor != outlineColor)
        {
            outline.OutlineColor = outlineColor;
            Debug.Log($"Updated outline color to: {outlineColor}");
        }
    }
} 