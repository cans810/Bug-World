using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SelectedEntityCrumbCollectionCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI crumbCountText;
    [SerializeField] private string crumbCountFormat = "{0}/{1}";
    
    // Internal variables
    private CanvasGroup canvasGroup;
    
    // Track crumb collection progress
    private int currentCrumbs = 0;
    private int requiredCrumbs = 10;
    private string entityType;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Initialize with invisible state
        canvasGroup.alpha = 0f;
    }
    
    public void Initialize(string entityType, int requiredCrumbs)
    {
        this.entityType = entityType;
        this.requiredCrumbs = requiredCrumbs;
        this.currentCrumbs = 0;
        
        // Update UI
        UpdateUI();
        
        // Show the counter with a fade-in effect
        StartCoroutine(FadeIn(0.5f));
    }
    
    public void UpdateCrumbCount(int count)
    {
        currentCrumbs = count;
        if (currentCrumbs > requiredCrumbs)
        {
            currentCrumbs = requiredCrumbs;
        }
        
        UpdateUI();
        
        // Check if collection is complete
        if (currentCrumbs >= requiredCrumbs)
        {
            // Hide this counter after a short delay
            StartCoroutine(HideAfterDelay(1.5f));
        }
    }
    
    public void IncrementCrumbCount()
    {
        currentCrumbs++;
        if (currentCrumbs > requiredCrumbs)
        {
            currentCrumbs = requiredCrumbs;
        }
        
        UpdateUI();
        
        // Check if collection is complete
        if (currentCrumbs >= requiredCrumbs)
        {
            // Hide this counter after a short delay
            StartCoroutine(HideAfterDelay(1.5f));
        }
    }
    
    private void UpdateUI()
    {
        // Update the text
        if (crumbCountText != null)
        {
            crumbCountText.text = string.Format(crumbCountFormat, currentCrumbs, requiredCrumbs);
        }
        
        // Update the fill image
        if (fillImage != null)
        {
            fillImage.fillAmount = (float)currentCrumbs / requiredCrumbs;
        }
    }
    
    private System.Collections.IEnumerator FadeIn(float duration)
    {
        float startTime = Time.time;
        float startAlpha = canvasGroup.alpha;
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    private System.Collections.IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(FadeOut(0.5f));
    }
    
    private System.Collections.IEnumerator FadeOut(float duration)
    {
        float startTime = Time.time;
        float startAlpha = canvasGroup.alpha;
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        
        // Optionally destroy the game object after fading out
        Destroy(gameObject);
    }
} 