using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI chitinCountText;
    [SerializeField] private string countFormat = "{0}";
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateOnChange = true;
    [SerializeField] private float animationDuration = 0.25f;
    
    private PlayerInventory playerInventory;
    private RectTransform countTextRect;
    private Vector3 originalScale;
    
    private void Start()
    {
        // Find player inventory
        playerInventory = FindObjectOfType<PlayerInventory>();
        
        if (playerInventory == null)
        {
            Debug.LogError("InventoryUI: No PlayerInventory found in scene!");
            return;
        }
        
        // Subscribe to inventory change events
        playerInventory.OnChitinCountChanged += UpdateChitinDisplay;
        
        // Get the rect transform for animations
        if (chitinCountText != null)
        {
            countTextRect = chitinCountText.GetComponent<RectTransform>();
            originalScale = countTextRect.localScale;
            
            // Initial update
            UpdateChitinDisplay(playerInventory.ChitinCount);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscription
        if (playerInventory != null)
        {
            playerInventory.OnChitinCountChanged -= UpdateChitinDisplay;
        }
    }
    
    private void UpdateChitinDisplay(int count)
    {
        if (chitinCountText != null)
        {
            // Update the text
            chitinCountText.text = string.Format(countFormat, count);
            
            // Play animation if enabled
            if (animateOnChange && countTextRect != null)
            {
                StopAllCoroutines();
                StartCoroutine(PulseAnimation());
            }
        }
    }
    
    private System.Collections.IEnumerator PulseAnimation()
    {
        // Scale up
        float timer = 0;
        Vector3 targetScale = originalScale * 1.2f;
        
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            countTextRect.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }
        
        // Scale back down
        timer = 0;
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            countTextRect.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }
        
        // Ensure final scale is correct
        countTextRect.localScale = originalScale;
    }
} 