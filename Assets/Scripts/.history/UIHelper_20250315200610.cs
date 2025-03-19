using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIHelper : MonoBehaviour
{
    // Singleton instance
    public static UIHelper Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI chitinCountText;
    [SerializeField] private TextMeshProUGUI crumbCountText;
    [SerializeField] private TextMeshProUGUI eggCountText;
    [SerializeField] private TextMeshProUGUI experienceCountText;
    [SerializeField] private string countFormat = "{0}/{1}";
    [SerializeField] private string xpFormat = "{0}/{1}";
    [SerializeField] private string levelFormat = "LVL {0}";
    [SerializeField] private string hpFormat = "{0}/{1}";

    [SerializeField] private TextMeshProUGUI currentHPText;
    
    [Header("Level Display")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject levelUpEffect;
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateOnChange = true;
    [SerializeField] private float animationDuration = 0.25f;

    [Header("Inform Player Text")]
    [SerializeField] public TextMeshProUGUI informPlayerText;
    [SerializeField] public bool isShowingInformText = false;
    
    private PlayerInventory playerInventory;
    private RectTransform countTextRect;
    private RectTransform crumbTextRect;
    private RectTransform eggTextRect;
    private RectTransform xpTextRect;
    private Vector3 originalScale;
    private Vector3 originalCrumbScale;
    private Vector3 originalXpScale;
    private Vector3 originalEggScale;


    [Header("UI Panel References")]
    [SerializeField] private GameObject allyAntsPanel;
    public GameObject attributesPanel;

    [Header("Icon Prefabs")]
    [SerializeField] private GameObject chitinIconPrefab;
    [SerializeField] private Transform posToMoveIconTransform;
    [SerializeField] private Transform nestTransform;

    
    // Add a field to cache the PlayerAttributes reference
    private PlayerAttributes cachedPlayerAttributes;

    [Header("Health Bar")]
    [SerializeField] private Image healthFillImage;

    public GameObject AttackButton;

    private UIMessageQueue messageQueue;

    // Add these fields to UIHelper class
    private CameraAnimations cameraAnimations;
    private bool shouldShowAttributesPanel = false;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Optional: Uncomment if you want this object to persist between scenes
        // DontDestroyOnLoad(gameObject);

        // Find or add the message queue component
        messageQueue = GetComponent<UIMessageQueue>();
        if (messageQueue == null)
        {
            messageQueue = gameObject.AddComponent<UIMessageQueue>();
        }
        
        // Assign the text component to the queue
        if (messageQueue != null && informPlayerText != null)
        {
            // Use reflection to set the private field (or add a public setter)
            var field = typeof(UIMessageQueue).GetField("messageText", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            if (field != null)
            {
                field.SetValue(messageQueue, informPlayerText);
            }
        }

        // Find camera animations
        cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null)
        {
            cameraAnimations.OnCameraAnimationCompleted += ShowAttributesPanelAfterAnimation;
        }
    }
    
    private void Start()
    {
        // Find player inventory
        playerInventory = FindObjectOfType<PlayerInventory>();
        
        // Cache PlayerAttributes reference
        cachedPlayerAttributes = FindObjectOfType<PlayerAttributes>();
        if (cachedPlayerAttributes != null)
        {
            // Subscribe to incubation changes
            cachedPlayerAttributes.OnIncubationChanged += RefreshEggDisplay;
            Debug.Log("UIHelper: Subscribed to incubation changes");
        }
        
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
        
        if (eggCountText != null)
        {
            eggTextRect = eggCountText.GetComponent<RectTransform>();
            originalEggScale = eggTextRect.localScale;
        }
        
        // Initialize displays
        UpdateChitinDisplay(playerInventory.ChitinCount);
        UpdateCrumbDisplay(playerInventory.CrumbCount);
        UpdateExperienceDisplay(playerInventory.TotalExperience);
        UpdateLevelDisplay(playerInventory.CurrentLevel);
        
        // Add HP display update
        UpdateHPDisplay();
        
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
        }
        
        // Unsubscribe from PlayerAttributes events
        if (cachedPlayerAttributes != null)
        {
            cachedPlayerAttributes.OnIncubationChanged -= RefreshEggDisplay;
        }

        // Unsubscribe from camera animation events
        if (cameraAnimations != null)
        {
            cameraAnimations.OnCameraAnimationCompleted -= ShowAttributesPanelAfterAnimation;
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
    
    private void UpdateExperienceDisplay(int experienceValue)
    {
        if (experienceCountText != null)
        {
            // Get player level info
            int currentLevel = playerInventory.CurrentLevel;
            int totalXP = playerInventory.TotalExperience;
            int xpForNextLevel = playerInventory.ExperienceForNextLevel;
            
            // Format XP text based on level status
            if (playerInventory.IsMaxLevel)
            {
                experienceCountText.text = $"MAX LEVEL";
            }
            else
            {
                experienceCountText.text = string.Format(xpFormat, totalXP, xpForNextLevel);
            }
            
            // Update level text if available
            if (levelText != null)
            {
                levelText.text = string.Format(levelFormat, currentLevel);
            }
            
            // Animate XP text on change
            if (animateOnChange && xpTextRect != null)
            {
                StartCoroutine(AnimateXPCount());
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
        // Show level up effect
        if (levelUpEffect != null)
        {
            levelUpEffect.SetActive(true);
            StartCoroutine(DisableLevelUpEffect(2f));
        }
        
        // Update the UI
        UpdateLevelDisplay(newLevel);
        
        // Don't immediately show the attributes panel
        // Instead, flag that we should show it after animation
        shouldShowAttributesPanel = true;
        
        // We'll let the camera animation completion handle this
        // attributesPanel.SetActive(true);
    }
    
    private IEnumerator DisableLevelUpEffect(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLevelUpEffect();
    }

    private void HideLevelUpEffect()
    {
        if (levelUpEffect != null)
        {
            levelUpEffect.SetActive(false);
        }
    }

    public void ShowPanel()
    {
        // Only show attributes panel if ally ants panel is not active
        if (!allyAntsPanel.activeSelf)
        {
            attributesPanel.SetActive(true);
            attributesPanel.GetComponent<Animator>().SetBool("ShowUp", true);
            attributesPanel.GetComponent<Animator>().SetBool("Hide", false);
        }
    }

    public void ClosePanel()
    {
        attributesPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        attributesPanel.GetComponent<Animator>().SetBool("Hide", true);
    }

    public void OnAllyAntsPanelClicked()
    {
        allyAntsPanel.SetActive(true);
        allyAntsPanel.GetComponent<Animator>().SetBool("ShowUp", true);
        allyAntsPanel.GetComponent<Animator>().SetBool("Hide", false);
    }

    public void OnAllyAntsPanelClosed()
    {
        allyAntsPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        allyAntsPanel.GetComponent<Animator>().SetBool("Hide", true);
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

    // Add this method to update the HP display
    private void UpdateHPDisplay()
    {
        if (currentHPText != null)
        {
            // Find player's LivingEntity component
            LivingEntity playerEntity = FindObjectOfType<PlayerController>()?.GetComponent<LivingEntity>();
            
            if (playerEntity != null)
            {
                // Format the HP text as currentHP/maxHP
                currentHPText.text = string.Format(hpFormat, 
                    Mathf.RoundToInt(playerEntity.CurrentHealth), 
                    Mathf.RoundToInt(playerEntity.MaxHealth));
                    
                // Update the health fill image if it exists
                if (healthFillImage != null)
                {
                    healthFillImage.fillAmount = playerEntity.HealthPercentage;
                }
            }
        }
    }

    private void Update()
    {
        // Update HP display every frame to keep it current
        UpdateHPDisplay();
    }

    public void ShowInformText(string message)
    {
        if (messageQueue != null)
        {
            messageQueue.EnqueueMessage(message);
        }
        else
        {
            // Fallback to original behavior
            StartCoroutine(ShowInformTextCoroutine(message));
        }
    }

    private System.Collections.IEnumerator ShowInformTextCoroutine(string message)
    {
        // Cancel any existing hide coroutines
        StopAllCoroutines();
        
        if (informPlayerText == null)
        {
            Debug.LogWarning("Inform player text is null!");
            yield break;
        }
        
        // Set text first
        informPlayerText.text = message;
        
        // Get animator reference once
        Animator textAnimator = informPlayerText.GetComponent<Animator>();
        if (textAnimator == null)
        {
            Debug.LogWarning("No Animator component found on informPlayerText!");
            yield break;
        }
        
        // Reset the animator completely to clear any stuck states
        textAnimator.Rebind();
        textAnimator.Update(0f);
        
        // Set animation parameter
        textAnimator.SetBool("Appear", true);
        
        // Set flag
        isShowingInformText = true;
        
        // Automatically hide the text after 4.5 seconds
        StartCoroutine(HideInformTextAfterDelay(4.5f));
    }

    private System.Collections.IEnumerator HideInformTextAfterDelay(float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);
        
        // Call HideInformText directly instead of duplicating code
        HideInformText();
    }

    public void HideInformText()
    {
        // Don't do anything if we're not showing text or the text component is missing
        if (!isShowingInformText || informPlayerText == null)
        {
            return;
        }
        
        Debug.Log("Hiding inform text now");
        
        Animator textAnimator = informPlayerText.GetComponent<Animator>();
        if (textAnimator != null)
        {
            // Reset the animator first to clear any stuck states
            textAnimator.Rebind();
            textAnimator.Update(0f);
            
            // Set animation parameter
            textAnimator.SetBool("Appear", false);
            
            // Clear the text to ensure it's not visible even if animation fails
            informPlayerText.text = "";
        }
        else
        {
            Debug.LogWarning("Cannot hide inform text - animator component is null");
            // Still clear the text even if animator is missing
            informPlayerText.text = "";
        }
        
        isShowingInformText = false;
    }

    // Update the egg display method to use the dynamic max capacity from PlayerAttributes
    public void UpdateEggDisplay(int count)
    {
        if (eggCountText != null)
        {
            // Get the max egg capacity from PlayerAttributes
            int maxEggCapacity = 1; // Default fallback value
            
            // Find PlayerAttributes if we need it
            PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
            if (playerAttributes != null)
            {
                maxEggCapacity = playerAttributes.MaxEggCapacity;
                Debug.Log($"UIHelper: Getting max egg capacity: {maxEggCapacity}");
            }
            
            // Use the dynamic max capacity instead of hardcoded 1
            eggCountText.text = string.Format(countFormat, count, maxEggCapacity);
            
            // Optionally animate the text similar to other counters
            if (animateOnChange && eggTextRect != null)
            {
                StopCoroutine("AnimateEggCount");
                StartCoroutine("AnimateEggCount");
            }
        }
    }

    // Add this coroutine for egg count animation
    private System.Collections.IEnumerator AnimateEggCount()
    {
        if (eggTextRect == null) yield break;
        
        // Scale up
        float timer = 0;
        Vector3 targetScale = originalEggScale * 1.4f;
        
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            eggTextRect.localScale = Vector3.Lerp(originalEggScale, targetScale, progress);
            yield return null;
        }
        
        // Scale back down
        timer = 0;
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            eggTextRect.localScale = Vector3.Lerp(targetScale, originalEggScale, progress);
            yield return null;
        }
        
        // Ensure final scale is correct
        eggTextRect.localScale = originalEggScale;
    }

    // Add a method to refresh the egg display when incubation changes
    private void RefreshEggDisplay()
    {
        // Find the current egg count from AntIncubator
        AntIncubator incubator = FindObjectOfType<AntIncubator>();
        int currentCount = incubator != null ? incubator.GetCurrentEggCount() : 0;
        
        // Update the display
        UpdateEggDisplay(currentCount);
        Debug.Log("UIHelper: Refreshed egg display after incubation change");
    }

    // New method to show the attributes panel after camera animation completes
    private void ShowAttributesPanelAfterAnimation(GameObject areaTarget, int level, string areaName)
    {
        if (shouldShowAttributesPanel && attributesPanel != null)
        {
            // Small delay to let the arrow appear first
            StartCoroutine(ShowAttributesPanelWithDelay(0.5f));
            shouldShowAttributesPanel = false; // Reset the flag
        }
    }

    // Helper method to show panel with delay
    private IEnumerator ShowAttributesPanelWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(true);
            
            // Optional: If you have an animator
            Animator panelAnimator = attributesPanel.GetComponent<Animator>();
            if (panelAnimator != null)
            {
                panelAnimator.SetBool("ShowUp", true);
                panelAnimator.SetBool("Hide", false);
            }
        }
    }
} 
