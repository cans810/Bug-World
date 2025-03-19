using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIHelper : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI chitinCountText;
    [SerializeField] private TextMeshProUGUI crumbCountText;
    [SerializeField] private TextMeshProUGUI experienceCountText;
    [SerializeField] private string countFormat = "{0}/{1}";
    [SerializeField] private string xpFormat = "{0}/{1}";
    [SerializeField] private string levelFormat = "LVL {0}";
    
    [Header("Level Display")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject levelUpEffect;
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateOnChange = true;
    [SerializeField] private float animationDuration = 0.25f;

    [Header("Inform Player Text")]
    [SerializeField] public TextMeshProUGUI informPlayerText;
    
    private PlayerInventory playerInventory;
    private RectTransform countTextRect;
    private RectTransform crumbTextRect;
    private RectTransform xpTextRect;
    private Vector3 originalScale;
    private Vector3 originalCrumbScale;
    private Vector3 originalXpScale;

    [Header("Panel Management")]
    [SerializeField] private GameObject attributesPanel;
    [SerializeField] private GameObject alliesPanel;
    
    // Panel state tracking
    private bool isAttributesPanelQueued = false;
    private bool isAlliesPanelActive = false;

    private void Start()
    {
        // Find player inventory
        playerInventory = FindObjectOfType<PlayerInventory>();
        
        // Log the actual capacity for debugging
        Debug.Log($"Player max chitin capacity: {playerInventory.MaxChitinCapacity}");
        Debug.Log($"Player max crumb capacity: {playerInventory.MaxCrumbCapacity}");
        
        if (playerInventory == null)
        {
            Debug.LogError("InventoryUI: No PlayerInventory found in scene!");
            return;
        }
        
        // Subscribe to inventory change events
        playerInventory.OnChitinCountChanged += UpdateChitinDisplay;
        playerInventory.OnCrumbCountChanged += UpdateCrumbDisplay;
        playerInventory.OnExperienceChanged += UpdateExperienceDisplay;
        playerInventory.OnLevelUp += OnPlayerLevelUp;
        playerInventory.OnAllyPointsAdded += OnAllyPointsAdded;
        
        // Cache components
        if (chitinCountText != null)
        {
            countTextRect = chitinCountText.GetComponent<RectTransform>();
            originalScale = countTextRect.localScale;
        }
        
        if (crumbCountText != null)
        {
            crumbTextRect = crumbCountText.GetComponent<RectTransform>();
            originalCrumbScale = crumbTextRect.localScale;
        }
        
        if (experienceCountText != null)
        {
            xpTextRect = experienceCountText.GetComponent<RectTransform>();
            originalXpScale = xpTextRect.localScale;
        }
        
        // Initialize displays
        UpdateChitinDisplay(playerInventory.ChitinCount);
        UpdateCrumbDisplay(playerInventory.CrumbCount);
        UpdateExperienceDisplay(playerInventory.Experience);
        UpdateLevelDisplay(playerInventory.CurrentLevel);
        
        // Hide level up effect initially
        if (levelUpEffect != null)
        {
            levelUpEffect.SetActive(false);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerInventory != null)
        {
            playerInventory.OnChitinCountChanged -= UpdateChitinDisplay;
            playerInventory.OnCrumbCountChanged -= UpdateCrumbDisplay;
            playerInventory.OnExperienceChanged -= UpdateExperienceDisplay;
            playerInventory.OnLevelUp -= OnPlayerLevelUp;
            playerInventory.OnAllyPointsAdded -= OnAllyPointsAdded;
        }
    }
    
    private void UpdateChitinDisplay(int count)
    {
        if (chitinCountText != null)
        {
            chitinCountText.text = string.Format(countFormat, count, playerInventory.MaxChitinCapacity);
            
            if (animateOnChange && countTextRect != null)
            {
                // Stop any running animations
                StopCoroutine("AnimateChitinCount");
                StartCoroutine("AnimateChitinCount");
            }
        }
    }
    
    private void UpdateCrumbDisplay(int count)
    {
        if (crumbCountText != null)
        {
            crumbCountText.text = string.Format(countFormat, count, playerInventory.MaxCrumbCapacity);
            
            if (animateOnChange && crumbTextRect != null)
            {
                // Stop any running animations
                StopCoroutine("AnimateCrumbCount");
                StartCoroutine("AnimateCrumbCount");
            }
        }
    }
    
    private void UpdateExperienceDisplay(int experience)
    {
        if (experienceCountText != null)
        {
            if (playerInventory.IsMaxLevel)
            {
                // If at max level, show "MAX"
                experienceCountText.text = "MAX";
            }
            else
            {
                // Show current XP in level and XP required for next level
                experienceCountText.text = string.Format(xpFormat, 
                    playerInventory.CurrentLevelExperience, 
                    playerInventory.ExperienceForNextLevel);
            }
            
            if (animateOnChange && xpTextRect != null)
            {
                // Stop any running animations
                StopCoroutine("AnimateXPCount");
                StartCoroutine("AnimateXPCount");
            }
        }
    }
    
    private void UpdateLevelDisplay(int level)
    {
        if (levelText != null)
        {
            levelText.text = string.Format(levelFormat, level);
        }
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        // Update level display
        UpdateLevelDisplay(newLevel);
        
        // Play level up effect
        if (levelUpEffect != null)
        {
            levelUpEffect.SetActive(true);
            
            // Hide effect after a delay
            Invoke("HideLevelUpEffect", 2.0f);
        }
    }
    
    private void HideLevelUpEffect()
    {
        if (levelUpEffect != null)
        {
            levelUpEffect.SetActive(false);
        }
    }

    public void ShowAttributesPanel()
    {
        // If allies panel is active, queue attributes panel to show later
        if (isAlliesPanelActive)
        {
            isAttributesPanelQueued = true;
            return;
        }
        
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(true);
        }
    }
    
    public void ShowAlliesPanel()
    {
        if (alliesPanel != null)
        {
            // Hide attributes panel if it's active
            if (attributesPanel != null && attributesPanel.activeSelf)
            {
                attributesPanel.SetActive(false);
                isAttributesPanelQueued = true;  // Queue it to reappear later
            }
            
            alliesPanel.SetActive(true);
            isAlliesPanelActive = true;
        }
    }
    
    public void CloseAttributesPanel()
    {
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(false);
            isAttributesPanelQueued = false;  // Clear the queue
        }
    }
    
    public void CloseAlliesPanel()
    {
        if (alliesPanel != null)
        {
            alliesPanel.SetActive(false);
            isAlliesPanelActive = false;
            
            // If attributes panel was queued, show it now
            if (isAttributesPanelQueued)
            {
                StartCoroutine(ShowAttributesPanelDelayed());
                isAttributesPanelQueued = false;
            }
        }
    }
    
    // Add a small delay before showing the attributes panel to allow for transition
    private IEnumerator ShowAttributesPanelDelayed()
    {
        yield return new WaitForSeconds(0.1f);  // Short delay
        
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(true);
        }
    }
    
    // Coroutine for chitin count animation (bounce effect)
    private System.Collections.IEnumerator AnimateChitinCount()
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
    
    // New coroutine for crumb count animation (bounce effect)
    private System.Collections.IEnumerator AnimateCrumbCount()
    {
        // Scale up
        float timer = 0;
        Vector3 targetScale = originalCrumbScale * 1.4f;
        
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            crumbTextRect.localScale = Vector3.Lerp(originalCrumbScale, targetScale, progress);
            yield return null;
        }
        
        // Scale back down
        timer = 0;
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            crumbTextRect.localScale = Vector3.Lerp(targetScale, originalCrumbScale, progress);
            yield return null;
        }
        
        // Ensure final scale is correct
        crumbTextRect.localScale = originalCrumbScale;
    }
    
    // Coroutine for XP count animation (bounce effect)
    private System.Collections.IEnumerator AnimateXPCount()
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

    private void OnAllyPointsAdded(int points)
    {
        // Show a message about earning ally points
        if (informPlayerText != null)
        {
            informPlayerText.text = $"Earned {points} ally point{(points > 1 ? "s" : "")}!";
            StartCoroutine(ClearMessageAfterDelay(3f));
        }
    }

    private System.Collections.IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (informPlayerText != null)
        {
            informPlayerText.text = "";
        }
    }
} 
