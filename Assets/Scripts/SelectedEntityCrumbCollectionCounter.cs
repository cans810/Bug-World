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
    [SerializeField] private Image entityIconImage;
    
    // Internal variables
    private CanvasGroup canvasGroup;
    
    // Track crumb collection progress
    private int currentCrumbs = 0;
    private int requiredCrumbs = 10;
    private string entityType;
    
    // Add event for completion
    public delegate void CrumbCollectionCompleteHandler(string entityType);
    public event CrumbCollectionCompleteHandler OnCrumbCollectionComplete;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Initialize with invisible state
        canvasGroup.alpha = 0f;
        
        // Find the entity icon image if it wasn't set
        if (entityIconImage == null)
        {
            // Look for a child named "Icon" with an Image component
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
            {
                entityIconImage = iconTransform.GetComponent<Image>();
            }
        }
    }
    
    public void Initialize(string entityType, int requiredCrumbs, Sprite entityIcon = null)
    {
        this.entityType = entityType;
        this.requiredCrumbs = requiredCrumbs;
        this.currentCrumbs = 0;
        
        // Set the entity icon if provided
        if (entityIcon != null && entityIconImage != null)
        {
            entityIconImage.sprite = entityIcon;
            entityIconImage.enabled = true;
        }
        else if (entityIconImage != null)
        {
            Debug.LogWarning($"No icon provided for entity type: {entityType}");
            // Keep the image enabled but use whatever default sprite is assigned
        }
        
        // Make this the first child in the parent's hierarchy
        transform.SetSiblingIndex(0);
        
        // Update UI
        UpdateUI();
        
        // Show the counter with a fade-in effect
        StartCoroutine(FadeIn(0.5f));
    }
    
    public void UpdateCrumbCount(int count)
    {
        int previousCrumbs = currentCrumbs;
        currentCrumbs = count;
        if (currentCrumbs > requiredCrumbs)
        {
            currentCrumbs = requiredCrumbs;
        }
        
        UpdateUI();
        
        // Check if collection is complete (just reached the requirement)
        if (currentCrumbs >= requiredCrumbs && previousCrumbs < requiredCrumbs)
        {
            // Notify subscribers that collection is complete
            OnCrumbCollectionComplete?.Invoke(entityType);
            
            // Hide this counter after a short delay
            StartCoroutine(HideAfterDelay(1.5f));
        }
    }
    
    public void IncrementCrumbCount()
    {
        int previousCrumbs = currentCrumbs;
        currentCrumbs++;
        if (currentCrumbs > requiredCrumbs)
        {
            currentCrumbs = requiredCrumbs;
        }
        
        UpdateUI();
        
        // Check if collection is complete (just reached the requirement)
        if (currentCrumbs >= requiredCrumbs && previousCrumbs < requiredCrumbs)
        {
            // Notify subscribers that collection is complete
            OnCrumbCollectionComplete?.Invoke(entityType);
            
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

    public int CurrentCrumbs
    {
        get { return currentCrumbs; }
    }
} 