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
    private bool initialSetupComplete = false;
    
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
        
        // Give it time to initialize
        Invoke("EnsureOutlineIsWorking", 0.2f);
    }
    
    private void EnsureOutlineIsWorking()
    {
        // Double-check the outline component
        if (outline == null)
        {
            outline = playerAnt.GetComponent<Outline>();
            if (outline == null)
            {
                outline = playerAnt.AddComponent<Outline>();
            }
        }
        
        // Re-apply settings
        outline.OutlineMode = outlineMode;
        outline.OutlineWidth = outlineWidth;
        outline.OutlineColor = outlineColor;
        
        initialSetupComplete = true;
        
        Debug.Log("Outline setup complete. Width: " + outline.OutlineWidth);
    }
    
    private void Update()
    {
        if (!initialSetupComplete) return;
        
        // Check if outline still exists (it might have been destroyed)
        if (outline == null)
        {
            outline = playerAnt.GetComponent<Outline>();
            if (outline == null)
            {
                Debug.LogWarning("Outline component missing! Re-adding...");
                outline = playerAnt.AddComponent<Outline>();
                outline.OutlineMode = outlineMode;
                outline.OutlineWidth = outlineWidth;
                outline.OutlineColor = outlineColor;
            }
        }
        
        // Check if width is set properly
        if (outline.OutlineWidth != outlineWidth)
        {
            outline.OutlineWidth = outlineWidth;
        }
        
        // Check if color is set properly
        if (outline.OutlineColor != outlineColor)
        {
            outline.OutlineColor = outlineColor;
        }
    }
    
    // For debugging - can be called from UI buttons if needed
    public void ToggleOutline()
    {
        if (outline != null)
        {
            if (outline.OutlineWidth > 0)
            {
                outline.OutlineWidth = 0;
            }
            else
            {
                outline.OutlineWidth = outlineWidth;
            }
        }
    }
} 