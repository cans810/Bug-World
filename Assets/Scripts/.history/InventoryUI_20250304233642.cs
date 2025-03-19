using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI chitinCountText;
    [SerializeField] private TextMeshProUGUI experienceCountText;
    [SerializeField] private string countFormat = "{0}/{1}";
    [SerializeField] private string xpFormat = "XP: {0}";
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateOnChange = true;
    [SerializeField] private float animationDuration = 0.25f;
    
    private PlayerInventory playerInventory;
    private RectTransform countTextRect;
    private RectTransform xpTextRect;
    private Vector3 originalScale;
    private Vector3 originalXpScale;
    
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
        playerInventory.OnExperienceChanged += UpdateExperienceDisplay;
        
        // Get the rect transform for animations
        if (chitinCountText != null)
        {
            countTextRect = chitinCountText.GetComponent<RectTransform>();
            originalScale = countTextRect.localScale;
            
            // Initial update
            UpdateChitinDisplay(playerInventory.ChitinCount);
        }
        
        // Get the rect transform for XP animations
        if (experienceCountText != null)
        {
            xpTextRect = experienceCountText.GetComponent<RectTransform>();
            originalXpScale = xpTextRect.localScale;
            
            // Initial update
            UpdateExperienceDisplay(playerInventory.Experience);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscription
        if (playerInventory != null)
        {
            playerInventory.OnChitinCountChanged -= UpdateChitinDisplay;
            playerInventory.OnExperienceChanged -= UpdateExperienceDisplay;
        }
    }
    
    private void UpdateChitinDisplay(int count)
    {
        if (chitinCountText != null)
        {
            // Get the max capacity from the player inventory
            int capacity = playerInventory != null ? 
                playerInventory.MaxChitinCapacity : 5; // Fallback to 5 if can't get value
            
            // Update the text with current/capacity format
            chitinCountText.text = string.Format(countFormat, count, capacity);
            
            // Play animation if enabled
            if (animateOnChange && countTextRect != null)
            {
                StopCoroutine(nameof(PulseChitinAnimation));
                StartCoroutine(PulseChitinAnimation());
            }
        }
    }
    
    private void UpdateExperienceDisplay(int xp)
    {
        if (experienceCountText != null)
        {
            // Update the text
            experienceCountText.text = string.Format(xpFormat, xp);
            
            // Play animation if enabled
            if (animateOnChange && xpTextRect != null)
            {
                StopCoroutine(nameof(PulseXpAnimation));
                StartCoroutine(PulseXpAnimation());
            }
        }
    }
    
    private System.Collections.IEnumerator PulseChitinAnimation()
    {
        // Scale up
        float timer = 0;
        Vector3 targetScale = originalScale * 1.4f;
        
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
    
    private System.Collections.IEnumerator PulseXpAnimation()
    {
        // Scale up
        float timer = 0;
        Vector3 targetScale = originalXpScale * 1.4f;
        
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            xpTextRect.localScale = Vector3.Lerp(originalXpScale, targetScale, progress);
            yield return null;
        }
        
        // Scale back down
        timer = 0;
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            xpTextRect.localScale = Vector3.Lerp(targetScale, originalXpScale, progress);
            yield return null;
        }
        
        // Ensure final scale is correct
        xpTextRect.localScale = originalXpScale;
    }
} 