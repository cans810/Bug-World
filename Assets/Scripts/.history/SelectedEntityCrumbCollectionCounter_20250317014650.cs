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
    
    private int currentCrumbs = 0;
    private int requiredCrumbs = 0;
    
    // Add variables for pulsate effect
    [Header("Pulsate Effect")]
    [SerializeField] private float pulsateScale = 1.2f;
    [SerializeField] private float pulsateDuration = 0.3f;
    
    // Add this flag to prevent multiple overlapping pulsate animations
    private bool isPulsating = false;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Initialize as hidden
        canvasGroup.alpha = 0f;
    }
    
    public void Initialize(int required)
    {
        requiredCrumbs = required;
        currentCrumbs = 0;
        UpdateDisplay();
        
        // Show the counter
        FadeIn();
    }
    
    public void UpdateCrumbCount(int count)
    {
        currentCrumbs = count;
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        // Update the text
        if (crumbCountText != null)
        {
            crumbCountText.text = string.Format(crumbCountFormat, currentCrumbs, requiredCrumbs);
        }
        
        // Update the fill image
        if (fillImage != null)
        {
            fillImage.fillAmount = requiredCrumbs > 0 ? (float)currentCrumbs / requiredCrumbs : 0f;
        }
    }
    
    public void FadeIn()
    {
        // Start fade in animation
        StartCoroutine(FadeRoutine(0f, 1f, 0.5f));
    }
    
    public void FadeOut()
    {
        // Start fade out animation
        StartCoroutine(FadeRoutine(1f, 0f, 0.5f));
    }
    
    private System.Collections.IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
        
        // If fading out completely, destroy the gameObject
        if (endAlpha == 0f)
        {
            Destroy(gameObject);
        }
    }
    
    // Method to check if entity has enough crumbs
    public bool HasEnoughCrumbs()
    {
        return currentCrumbs >= requiredCrumbs;
    }
    
    // Add this method to create a pulsate effect when crumbs are received
    public void Pulsate()
    {
        // Only start a new pulsate if not already pulsating
        if (!isPulsating)
        {
            StartCoroutine(PulsateRoutine());
        }
    }
    
    private IEnumerator PulsateRoutine()
    {
        isPulsating = true;
        
        Transform contentTransform = transform;
        if (contentTransform == null) 
        {
            isPulsating = false;
            yield break;
        }
        
        // Store original scale at the start of the animation
        Vector3 originalScale = contentTransform.localScale;
        Vector3 targetScale = originalScale * pulsateScale;
        
        Debug.Log($"Pulsate starting. Original scale: {originalScale}, Target scale: {targetScale}");
        
        // Scale up
        float elapsed = 0f;
        float halfDuration = pulsateDuration / 2f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            contentTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        // Ensure we reach the maximum scale
        contentTransform.localScale = targetScale;
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            contentTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        // Explicitly set the final scale to ensure it's correct
        contentTransform.localScale = originalScale;
        
        Debug.Log($"Pulsate complete. Final scale: {contentTransform.localScale}");
        
        isPulsating = false;
    }
} 