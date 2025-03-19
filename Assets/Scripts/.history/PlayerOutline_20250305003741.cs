using UnityEngine;

// Try different namespace options
//using QuickOutline; // Original namespace that's causing the error

public class PlayerOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;
    
    // Use direct enum reference instead of depending on namespace
    [SerializeField] private int outlineMode = 2; // 0=OutlineAll, 1=OutlineVisible, 2=OutlineHidden, 3=OutlineAndSilhouette
    
    [Header("References")]
    [SerializeField] private GameObject playerAnt; // Assign in inspector if not on same GameObject
    
    private void Awake()
    {
        // If player reference is not set, use this GameObject
        if (playerAnt == null)
            playerAnt = gameObject;
        
        // Check if Outline component exists
        Component outline = playerAnt.GetComponent("Outline");
        
        if (outline == null)
        {
            // If Outline can't be added because the class doesn't exist, log an error
            Debug.LogError("QuickOutline package is not properly imported. Please make sure the package is in your project.");
            return;
        }
        
        // Access properties through reflection to avoid direct references to the class
        // This is a workaround for the namespace issue
        var outlineModeProperty = outline.GetType().GetProperty("OutlineMode");
        var outlineColorProperty = outline.GetType().GetProperty("OutlineColor");
        var outlineWidthProperty = outline.GetType().GetProperty("OutlineWidth");
        
        if (outlineModeProperty != null) 
            outlineModeProperty.SetValue(outline, outlineMode);
        
        if (outlineColorProperty != null) 
            outlineColorProperty.SetValue(outline, outlineColor);
        
        if (outlineWidthProperty != null) 
            outlineWidthProperty.SetValue(outline, outlineWidth);
    }
} 