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
} 