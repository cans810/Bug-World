using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIHelper : MonoBehaviour
{
    // Singleton instance
    public static UIHelper Instance { get; private set; }

    public GameObject SafeArea;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI chitinCountText;
    [SerializeField] private TextMeshProUGUI crumbCountText;
    [SerializeField] private TextMeshProUGUI eggCountText;
    [SerializeField] private TextMeshProUGUI experienceCountText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private string countFormat = "{0}/{1}";
    [SerializeField] private string xpFormat = "{0}/{1}";
    [SerializeField] private string levelFormat = "LVL {0}";
    [SerializeField] private string hpFormat = "{0}/{1}";
    [SerializeField] private string coinFormat = "{0}";  // New format for coins (no max capacity)

    [SerializeField] private TextMeshProUGUI currentHPText;
    
    [Header("Level Display")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject levelUpEffect;
    
    [Header("Animation Settings")]
    [SerializeField] private bool animateOnChange = true;
    [SerializeField] private float animationDuration = 0.25f;

    [Header("Inform Player Text")]
    [SerializeField] public TextMeshProUGUI informPlayerText;
    [SerializeField] public TextMeshProUGUI onLevelUpThingsGainedText;
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
    private Vector3 originalCoinScale;
    private RectTransform coinTextRect;


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

    // Add these fields to UIHelper class
    private CameraAnimations cameraAnimations;
    private bool shouldShowAttributesPanel = false;

    public GameObject ItemInformPanel;

    public GameObject DiscoveredEntitiesPanel;

    public GameObject MarketPanel;

    [Header("XP Animation Settings")]
    [SerializeField] private bool animateXPCounter = true;
    [SerializeField] private float xpCounterSpeed = 0.05f; // Time between count increments
    private int currentDisplayedXP;
    private int targetXP;
    private Coroutine xpAnimationCoroutine;

    [Header("Selected Entity Crumb Collection Counter")]
    [SerializeField] public GameObject selectedEntityCrumbCollectionCounterPrefab;

    private void Awake()
    {
        // Set up the singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Hide the level up rewards text initially
        if (onLevelUpThingsGainedText != null)
        {
            onLevelUpThingsGainedText.gameObject.SetActive(false);
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
        // Find the player inventory
        playerInventory = FindObjectOfType<PlayerInventory>();
        
        if (playerInventory == null)
        {
            Debug.LogError("UIHelper: PlayerInventory not found!");
            return;
        }
        
        // Subscribe to events
        playerInventory.OnChitinCountChanged += UpdateChitinDisplay;
        playerInventory.OnCrumbCountChanged += UpdateCrumbDisplay;
        playerInventory.OnExperienceChanged += UpdateExperienceDisplay;
        playerInventory.OnLevelUp += OnPlayerLevelUp;
        playerInventory.OnCoinCountChanged += UpdateCoinDisplay;
        
        // Store original scales for animations
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
        
        if (coinCountText != null)
        {
            coinTextRect = coinCountText.GetComponent<RectTransform>();
            originalCoinScale = coinTextRect.localScale;
        }
        
        // Initialize the current displayed XP
        if (playerInventory != null)
        {
            currentDisplayedXP = playerInventory.TotalExperience;
        }
        
        // Make sure the inform text is ready to use
        if (informPlayerText != null)
        {
            // Ensure it's initially hidden
            informPlayerText.gameObject.SetActive(false);
            informPlayerText.text = "";
            isShowingInformText = false;
        }
        
        // Make sure the level up rewards text is hidden initially
        if (onLevelUpThingsGainedText != null)
        {
            onLevelUpThingsGainedText.gameObject.SetActive(false);
        }
        
        // Find camera animations
        cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null)
        {
            cameraAnimations.OnCameraAnimationCompleted += ShowAttributesPanelAfterAnimation;
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
            playerInventory.OnCoinCountChanged -= UpdateCoinDisplay;
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
    
    // Add this overload to match the delegate signature
    private void UpdateExperienceDisplay(int experienceValue)
    {
        // Call the new version with default animation setting
        UpdateExperienceDisplay(experienceValue, true);
    }

    // Keep the existing method with the bool parameter
    public void UpdateExperienceDisplay(int xpAmount, bool animate = true)
    {
        if (experienceCountText == null) return;
        
        // Get the player inventory for XP requirements
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory == null) return;
        }
        
        // Store the target XP
        targetXP = xpAmount;
        
        // If animation is disabled or we're loading data, update immediately
        if (!animateXPCounter || !animate || playerInventory.IsLoadingData)
        {
            currentDisplayedXP = targetXP;
            UpdateXPText();
            return;
        }
        
        // If we're already animating, stop the current animation
        if (xpAnimationCoroutine != null)
        {
            StopCoroutine(xpAnimationCoroutine);
        }
        
        // Start a new animation
        xpAnimationCoroutine = StartCoroutine(AnimateXPCounter());
    }
    
    private void UpdateLevelDisplay(int level)
    {
        if (levelText != null)
        {
            levelText.text = string.Format(levelFormat, level);
        }
    }
    
    private void OnPlayerLevelUp(int level)
    {
        // Don't show level-up effects if we're loading data
        if (playerInventory != null && playerInventory.IsLoadingData)
        {
            // Just update the text without animations or panels
            if (levelText != null)
            {
                levelText.text = string.Format(levelFormat, level);
            }
            return;  // Skip all the level-up effects
        }

        // Regular level-up code (show effects, panel, etc.)
        if (levelText != null)
        {
            levelText.text = string.Format(levelFormat, level);
        }
        
        // Show level up effect
        if (levelUpEffect != null)
        {
            levelUpEffect.SetActive(true);
        }
        
        // Let the camera animation system handle showing the attributes panel through the event
        if (cameraAnimations != null)
        {
            // Just set the flag - don't try to call an animation method directly
            shouldShowAttributesPanel = true;
            
            // Then show the level up UI element or play a sound here
            if (levelUpEffect != null)
            {
                levelUpEffect.SetActive(true);
            }
            
            // Let the camera animation system handle showing the attributes panel through the event
        }
        else if (attributesPanel != null)
        {
            // If no camera animations, show the panel directly
            attributesPanel.SetActive(true);
            Animator panelAnimator = attributesPanel.GetComponent<Animator>();
            if (panelAnimator != null)
            {
                panelAnimator.SetBool("ShowUp", true);
                panelAnimator.SetBool("Hide", false);
            }
        }
        
        // Play sound effects if needed
    }
    
    private IEnumerator HideLevelUpEffectAfterDelay(float delay)
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

    public void ShowMarket()
    {
        MarketPanel.SetActive(true);
        //MarketPanel.GetComponent<Animator>().SetBool("ShowUp", true);
        //MarketPanel.GetComponent<Animator>().SetBool("Hide", false);

        //if(MarketPanel.activeSelf)
        //{
        //    MarketPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        //    MarketPanel.GetComponent<Animator>().SetBool("Hide", true);
        //}
    }

    public void ShowPanel()
    {
        attributesPanel.SetActive(true);
        attributesPanel.GetComponent<Animator>().SetBool("ShowUp", true);
        attributesPanel.GetComponent<Animator>().SetBool("Hide", false);

        if(DiscoveredEntitiesPanel.activeSelf)
        {
            DiscoveredEntitiesPanel.GetComponent<Animator>().SetBool("ShowUp", false);
            DiscoveredEntitiesPanel.GetComponent<Animator>().SetBool("Hide", true);
        }
    }

    public void ShowDiscoveredEntitiesPanel()
    {
        if (DiscoveredEntitiesPanel != null)
        {
            if(attributesPanel.activeSelf)
            {
                attributesPanel.GetComponent<Animator>().SetBool("ShowUp", false);
                attributesPanel.GetComponent<Animator>().SetBool("Hide", true);
            }

            Debug.Log("Showing Discovered Entities Panel");
            DiscoveredEntitiesPanel.SetActive(true);
            
            // If it has an animator, use it
            Animator panelAnimator = DiscoveredEntitiesPanel.GetComponent<Animator>();
            if (panelAnimator != null)
            {
                panelAnimator.SetBool("ShowUp", true);
                panelAnimator.SetBool("Hide", false);
                Debug.Log("Setting animator parameters for Discovered Entities Panel");
            }
        }
        else
        {
            Debug.LogError("DiscoveredEntitiesPanel is null in UIHelper");
        }
    }

    public void CloseDiscoveredEntitiesPanel()
    {
        DiscoveredEntitiesPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        DiscoveredEntitiesPanel.GetComponent<Animator>().SetBool("Hide", true);
    }

    public void ClosePanel()
    {
        attributesPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        attributesPanel.GetComponent<Animator>().SetBool("Hide", true);
    }

    public void CloseMarket()

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
    
    // Add this method to animate the XP counter
    private IEnumerator AnimateXPCounter()
    {
        // If we're already at or past the target, just update
        if (currentDisplayedXP >= targetXP)
        {
            currentDisplayedXP = targetXP;
            UpdateXPText();
            yield break;
        }
        
        // Animate the counter up to the target
        while (currentDisplayedXP < targetXP)
        {
            // Increment by 1
            currentDisplayedXP++;
            
            // Update the text
            UpdateXPText();
            
            // Wait a short time between increments
            yield return new WaitForSeconds(xpCounterSpeed);
        }
        
        // Ensure we end exactly at the target
        currentDisplayedXP = targetXP;
        UpdateXPText();
    }

    // Helper method to update the XP text
    private void UpdateXPText()
    {
        if (experienceCountText == null || playerInventory == null) return;
        
        // Format XP text based on level status
        if (playerInventory.CurrentLevel >= 20) // Assuming max level is 20
        {
            experienceCountText.text = "MAX LEVEL";
        }
        else
        {
            int xpForNextLevel = playerInventory.ExperienceForNextLevel;
            experienceCountText.text = string.Format(xpFormat, currentDisplayedXP, xpForNextLevel);
        }
        
        // Animate the text scale if enabled
        if (animateOnChange && xpTextRect != null)
        {
            StartCoroutine(AnimateXPText());
        }
    }

    // Add this method to animate the XP text
    private System.Collections.IEnumerator AnimateXPText()
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
        // Skip showing messages if we're loading data
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null && playerInventory.IsLoadingData)
        {
            return;
        }

        // Skip the message queue and animation system entirely
        if (informPlayerText != null)
        {
            // Simply set the text and make it visible
            informPlayerText.gameObject.SetActive(true);
            informPlayerText.text = message;
            isShowingInformText = true;
            
            // Start a coroutine to hide the text after a delay
            StopAllCoroutines(); // Stop any existing hide coroutines
            StartCoroutine(HideInformTextAfterDelay(4.5f));
        }
        else
        {
            Debug.LogWarning("Inform player text is null!");
        }
    }

    private IEnumerator HideInformTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideInformText();
    }

    public void HideInformText()
    {
        if (informPlayerText != null)
        {
            // Simply hide the text
            informPlayerText.gameObject.SetActive(false);
            informPlayerText.text = "";
            isShowingInformText = false;
        }
    }

    // Update the egg display method to use the dynamic max capacity from PlayerAttributes
    public void UpdateEggDisplay(int count)
    {
        if (eggCountText != null)
        {
            // Get the max egg capacity from PlayerInventory first (which should be synced with PlayerAttributes)
            int maxEggCapacity = 1; // Default fallback value
            
            // Try to get from PlayerInventory first
            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
            {
                maxEggCapacity = playerInventory.MaxEggCapacity;
                Debug.Log($"UIHelper: Getting max egg capacity from PlayerInventory: {maxEggCapacity}");
            }
            // If not available, try PlayerAttributes
            else
            {
                PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
                if (playerAttributes != null)
                {
                    maxEggCapacity = playerAttributes.MaxEggCapacity;
                    Debug.Log($"UIHelper: Getting max egg capacity from PlayerAttributes: {maxEggCapacity}");
                }
            }
            
            // Use the dynamic max capacity
            eggCountText.text = string.Format(countFormat, count, maxEggCapacity);
            Debug.Log($"Updated egg display: {count}/{maxEggCapacity}");
            
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

    // Add this method to UIHelper class
    public void LoadGameData(GameData data)
    {
        if (data == null) return;
        
        // Update UI displays with the loaded data
        if (chitinCountText != null)
        {
            chitinCountText.text = string.Format(countFormat, data.currentChitin, playerInventory?.MaxChitinCapacity ?? 100);
        }
        
        if (crumbCountText != null)
        {
            crumbCountText.text = string.Format(countFormat, data.currentCrumb, playerInventory?.MaxCrumbCapacity ?? 100);
        }
        
        if (experienceCountText != null)
        {
            // Format XP text based on level status
            if (data.currentLevel >= 20) // Assuming max level is 20
            {
                experienceCountText.text = "MAX LEVEL";
            }
            else
            {
                // Fix: Get the actual next level XP target from PlayerInventory if possible
                int xpForNextLevel = 0;
                
                if (playerInventory != null)
                {
                    // Try to get the actual next level requirement directly
                    xpForNextLevel = playerInventory.ExperienceForNextLevel;
                }
                else
                {
                    // Fallback to our calculation
                    xpForNextLevel = CalculateXPRequiredForLevel(data.currentLevel + 1);
                }
                
                experienceCountText.text = string.Format(xpFormat, data.currentXP, xpForNextLevel);
                Debug.Log($"Setting XP display: {data.currentXP}/{xpForNextLevel}");
            }
        }
        
        if (levelText != null)
        {
            levelText.text = string.Format(levelFormat, data.currentLevel);
        }
        
        if (currentHPText != null)
        {
            currentHPText.text = string.Format(hpFormat, data.currentHP, data.currentMaxHP);
        }
        
        if (healthFillImage != null)
        {
            // Update health bar fill amount
            float fillAmount = (float)data.currentHP / data.currentMaxHP;
            healthFillImage.fillAmount = Mathf.Clamp01(fillAmount);
        }
        
        if (coinCountText != null)
        {
            coinCountText.text = string.Format(coinFormat, data.currentCoin);
        }
        
        // Log the load
        Debug.Log("UI updated with loaded game data. Level: " + data.currentLevel);
    }

    // Add this helper method to calculate XP requirements
    private int CalculateXPRequiredForLevel(int level)
    {
        // Common formula for XP progression:
        // Base amount + (level * multiplier)^exponent
        int baseXP = 100;
        float multiplier = 1.5f;
        float exponent = 1.2f;
        
        return Mathf.RoundToInt(baseXP + Mathf.Pow(level * multiplier, exponent) * 10);
    }

    // Add this method to UIHelper class
    public void SaveGame()
    {
        // Find the GameManager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SaveGame();
            
            // Show a message to the player
            ShowInformText("Game saved successfully!");
            
            Debug.Log("Game saved via UI button");
        }
        else
        {
            Debug.LogError("Could not find GameManager to save the game!");
            ShowInformText("Error: Could not save game!");
        }
    }

    // Optional: Add this method to create a visual feedback when saving
    private IEnumerator ShowSaveFeedback()
    {
        // Here you could show a save icon animation, play a sound, etc.
        yield return new WaitForSeconds(1.5f);
        // Hide the save feedback after delay
    }

    // Add this to UIHelper
    private void AutoSave()
    {
        // Only auto-save every few minutes or at important moments
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SaveGame();
            Debug.Log("Game auto-saved");
        }
    }

    // Make sure this method is connected to your button
    public void OnDiscoveredEntitiesButtonClicked()
    {
        Debug.Log("Discovered Entities button clicked!");
        ShowDiscoveredEntitiesPanel();
    }

    // Add this method to update coin display
    public void UpdateCoinDisplay(int count)
    {
        if (coinCountText != null)
        {
            coinCountText.text = string.Format(coinFormat, count);
            
            if (animateOnChange && coinTextRect != null)
            {
                // Stop any running animations
                StopCoroutine("AnimateCoinCount");
                StartCoroutine("AnimateCoinCount");
            }
        }
    }

    // Add this coroutine for coin count animation
    private System.Collections.IEnumerator AnimateCoinCount()
    {
        if (coinTextRect == null) yield break;
        
        // Scale up
        float timer = 0;
        Vector3 targetScale = originalCoinScale * 1.4f;
        
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            coinTextRect.localScale = Vector3.Lerp(originalCoinScale, targetScale, progress);
            yield return null;
        }
        
        // Scale back down
        timer = 0;
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            coinTextRect.localScale = Vector3.Lerp(targetScale, originalCoinScale, progress);
            yield return null;
        }
        
        // Ensure final scale is correct
        coinTextRect.localScale = originalCoinScale;
    }
} 
