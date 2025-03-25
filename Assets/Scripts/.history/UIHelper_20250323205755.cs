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
    public GameObject attributesPanel;

    [Header("Nest Market Panel")]
    public GameObject nestMarketController;
    public GameObject nestMarketPanel;


    [Header("Icon Prefabs")]
    [SerializeField] private GameObject chitinIconPrefab;
    [SerializeField] private Transform posToMoveIconTransform;
    [SerializeField] private Transform nestTransform;

    
    // Add a field to cache the PlayerAttributes reference
    public PlayerAttributes cachedPlayerAttributes;

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
    public Image xpbarImage;
    

    [Header("Selected Entity Crumb Collection Counter")]
    [SerializeField] public GameObject selectedEntityCrumbCollectionCounterPrefab;

    [Header("Scale Reset Settings")]
    [SerializeField] private bool enablePeriodicScaleReset = true;
    [SerializeField] private float scaleResetInterval = 5f;
    private float scaleResetTimer = 0f;

    public Image crumbbarImage;
    public Image chitinbarImage;
    public Image eggbarImage;

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
        
        // Get reference to CameraAnimations
        cameraAnimations = FindObjectOfType<CameraAnimations>();
        
        // Only subscribe to animation events if AttributeDisplay is not handling it
        AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
        if (cameraAnimations != null)
        {
            if (attributeDisplay == null)
            {
                // Only subscribe if AttributeDisplay doesn't exist
                cameraAnimations.OnCameraAnimationCompleted += ShowAttributesPanelAfterAnimation;
                Debug.Log("UIHelper subscribing to camera animation events (AttributeDisplay not found)");
            }
            else
            {
                Debug.Log("UIHelper NOT subscribing to camera animation (using AttributeDisplay instead)");
            }
        }

        // Initialize cachedPlayerAttributes if not set
        if (cachedPlayerAttributes == null)
        {
            cachedPlayerAttributes = FindObjectOfType<PlayerAttributes>();
            Debug.Log("UIHelper: Cached PlayerAttributes reference");
        }
    }
    
    private void OnDestroy()
    {
        // Reset all scales before destroying
        ResetAllUIScales();
        
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
    
    public void UpdateChitinDisplay(int chitin)
    {
        if (chitinCountText != null && playerInventory != null)
        {
            chitinCountText.text = string.Format(countFormat, chitin, playerInventory.MaxChitinCapacity);
            
            if (animateOnChange)
            {
                StartCoroutine(AnimateTextScale(countTextRect, originalScale, animationDuration));
            }
        }
        
        if (chitinbarImage != null && playerInventory != null)
        {
            float fillAmount = (float)chitin / playerInventory.MaxChitinCapacity;
            fillAmount = Mathf.Clamp01(fillAmount);
            
            chitinbarImage.fillAmount = fillAmount;
        }
    }
    
    public void UpdateCrumbDisplay(int crumbs)
    {
        if (crumbCountText != null && playerInventory != null)
        {
            crumbCountText.text = string.Format(countFormat, crumbs, playerInventory.MaxCrumbCapacity);
            
            if (animateOnChange)
            {
                StartCoroutine(AnimateTextScale(crumbTextRect, originalCrumbScale, animationDuration));
            }
        }
        
        if (crumbbarImage != null && playerInventory != null)
        {
            float fillAmount = (float)crumbs / playerInventory.MaxCrumbCapacity;
            fillAmount = Mathf.Clamp01(fillAmount);
            
            crumbbarImage.fillAmount = fillAmount;
        }
    }
    
    // Add this overload to match the delegate signature
    private void UpdateExperienceDisplay(int experienceValue)
    {
        // Call the new version with default animation setting
        UpdateExperienceDisplay(experienceValue, true);
    }

    // Update the UpdateExperienceDisplay method to fill the XP bar
    public void UpdateExperienceDisplay(int experience, bool animate = true)
    {
        // Handle text display (existing code)
        if (experienceCountText != null && playerInventory != null)
        {
            int nextLevelXP = playerInventory.ExperienceForNextLevel;
            int currentLevel = playerInventory.CurrentLevel;
            bool isMaxLevel = playerInventory.IsMaxLevel;
            
            // If max level, show a different format
            if (isMaxLevel)
            {
                experienceCountText.text = string.Format("MAX LEVEL");
            }
            else
            {
                experienceCountText.text = string.Format(xpFormat, experience, nextLevelXP);
            }
            
            // Start XP counter animation if enabled
            if (animateXPCounter && !isMaxLevel)
            {
                if (xpAnimationCoroutine != null)
                    StopCoroutine(xpAnimationCoroutine);
                    
                xpAnimationCoroutine = StartCoroutine(AnimateXPCounter(experience));
            }
            
            // Animate text scale if enabled
            if (animateOnChange)
            {
                StartCoroutine(AnimateTextScale(xpTextRect, originalXpScale, animationDuration));
            }
        }
        
        // NEW CODE: Update XP bar fill amount
        if (xpbarImage != null && playerInventory != null)
        {
            int currentLevel = playerInventory.CurrentLevel;
            bool isMaxLevel = playerInventory.IsMaxLevel;
            
            // If max level, fill the bar completely
            if (isMaxLevel)
            {
                xpbarImage.fillAmount = 1f;
                return;
            }
            
            // Calculate current level XP threshold
            int currentLevelXP = 0;
            if (currentLevel > 1 && currentLevel <= playerInventory.GetMaxLevel())
            {
                currentLevelXP = playerInventory.GetXPRequirementForLevel(currentLevel - 1);
            }
            
            // Calculate next level XP threshold
            int nextLevelXP = playerInventory.ExperienceForNextLevel;
            
            // Calculate XP needed for this level
            int xpForThisLevel = nextLevelXP - currentLevelXP;
            
            // Calculate current progress within this level
            int progressInThisLevel = experience - currentLevelXP;
            
            // Calculate fill percentage (0 to 1)
            float fillAmount = (float)progressInThisLevel / xpForThisLevel;
            fillAmount = Mathf.Clamp01(fillAmount); // Ensure it's between 0 and 1
            
            // Set the fill amount
            xpbarImage.fillAmount = fillAmount;
            
            Debug.Log($"XP Bar updated: {fillAmount:P2} filled. Level {currentLevel}: " +
                      $"{progressInThisLevel}/{xpForThisLevel} XP toward next level.");
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
        // Don't show level-up effects if we're loading data
        if (playerInventory != null && playerInventory.IsLoadingData)
        {
            // Just update the text without animations or panels
            if (levelText != null)
            {
                levelText.text = string.Format(levelFormat, newLevel);
            }
            return;  // Skip all the level-up effects
        }

        // Regular level-up code (show effects, panel, etc.)
        if (levelText != null)
        {
            levelText.text = string.Format(levelFormat, newLevel);
        }
        
        // Show level up effect
        if (levelUpEffect != null)
        {
            levelUpEffect.SetActive(true);
        }
        
        // Check if there's an unlocked area
        LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
        bool hasUnlockedArea = false;
        
        if (arrowManager != null)
        {
            // Access the levelAreas field using reflection since it's private
            var areasField = typeof(LevelAreaArrowManager).GetField("levelAreas", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            if (areasField != null)
            {
                var areas = areasField.GetValue(arrowManager) as LevelAreaArrowManager.LevelAreaTarget[];
                if (areas != null)
                {
                    foreach (var area in areas)
                    {
                        if (area.requiredLevel == newLevel && !area.hasBeenVisited && area.areaTarget != null)
                        {
                            hasUnlockedArea = true;
                            break;
                        }
                    }
                }
            }
        }
        
        // If there's no unlocked area, show the attributes panel immediately
        if (!hasUnlockedArea && attributesPanel != null)
        {
            attributesPanel.SetActive(true);
            Animator panelAnimator = attributesPanel.GetComponent<Animator>();
            if (panelAnimator != null)
            {
                panelAnimator.SetBool("ShowUp", true);
                panelAnimator.SetBool("Hide", false);
            }
        }
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
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        MarketManager marketManager = FindObjectOfType<MarketManager>();
        if (marketManager == null)
        {
            Debug.LogError("MarketManager not found!");
            return;
        }

        // Initialize purchasing if not already initialized
        if (!marketManager.IsInitialized())
        {
            marketManager.InitializePurchasing();
        }

        MarketPanel.GetComponent<Animator>().SetBool("ShowUp", true);
        MarketPanel.GetComponent<Animator>().SetBool("Hide", false);

        if(attributesPanel.activeSelf)
        {
            attributesPanel.GetComponent<Animator>().SetBool("ShowUp", false);
            attributesPanel.GetComponent<Animator>().SetBool("Hide", true);
        }
    }

    public void ShowPanel()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        // Check if camera animation is in progress
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null && cameraAnimations.IsAnimationInProgress())
        {
            Debug.Log("UIHelper: Skipping panel display during camera animation");
            shouldShowAttributesPanel = true;
            return;
        }

        if (attributesPanel != null)
        {
            attributesPanel.SetActive(true);
            
            Animator panelAnimator = attributesPanel.GetComponent<Animator>();
            if (panelAnimator != null)
            {
                panelAnimator.SetBool("ShowUp", true);
                panelAnimator.SetBool("Hide", false);
            }
        }
    }

    public void ShowDiscoveredEntitiesPanel()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
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
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        DiscoveredEntitiesPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        DiscoveredEntitiesPanel.GetComponent<Animator>().SetBool("Hide", true);
    }

    public void ClosePanel()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        attributesPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        attributesPanel.GetComponent<Animator>().SetBool("Hide", true);
    }

    public void CloseMarket()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        MarketPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        MarketPanel.GetComponent<Animator>().SetBool("Hide", true);
    }

    // Coroutine for chitin count animation (bounce effect)
    private System.Collections.IEnumerator AnimateChitinCount()
    {
        if (countTextRect == null) yield break;
        
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
        
        // Ensure we reach the maximum scale
        countTextRect.localScale = targetScale;
        
        // Scale back down
        timer = 0;
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            countTextRect.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }
        
        // Always force the final scale to be exactly the original
        countTextRect.localScale = originalScale;
    }
    
    // New coroutine for crumb count animation (bounce effect)
    private System.Collections.IEnumerator AnimateCrumbCount()
    {
        if (crumbTextRect == null) yield break;
        
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
        
        // Ensure we reach the maximum scale
        crumbTextRect.localScale = targetScale;
        
        // Scale back down
        timer = 0;
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            crumbTextRect.localScale = Vector3.Lerp(targetScale, originalCrumbScale, progress);
            yield return null;
        }
        
        // Always force the final scale to be exactly the original
        crumbTextRect.localScale = originalCrumbScale;
    }
    
    // Improved XP counter animation that completes properly
    private IEnumerator AnimateXPCounter(int xpRequiredForNextLevel)
    {
        // If we're already at the target, no need to animate
        if (currentDisplayedXP == targetXP)
            yield break;
        
        // Calculate increment size based on total change
        int difference = targetXP - currentDisplayedXP;
        int increment = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(difference) / 20f)); // At most 20 steps
        
        // Ensure increment direction matches the change
        if (difference < 0)
            increment = -increment;
        
        // Animation loop
        while (currentDisplayedXP != targetXP)
        {
            // Move current value toward target
            currentDisplayedXP = difference > 0
                ? Mathf.Min(currentDisplayedXP + increment, targetXP)
                : Mathf.Max(currentDisplayedXP + increment, targetXP);
            
            // Update the UI text
            experienceCountText.text = string.Format(xpFormat, currentDisplayedXP, xpRequiredForNextLevel);
            
            // Update any progress bar if you have one
            UpdateExperienceBar(currentDisplayedXP, xpRequiredForNextLevel);
            
            // Pause between updates
            yield return new WaitForSeconds(xpCounterSpeed);
        }
        
        // Ensure we end at exactly the target value
        currentDisplayedXP = targetXP;
        experienceCountText.text = string.Format(xpFormat, targetXP, xpRequiredForNextLevel);
        UpdateExperienceBar(targetXP, xpRequiredForNextLevel);
        
        xpAnimationCoroutine = null;
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
            // Use the safer method instead of starting the coroutine directly
            SafeStopAndStartCoroutine("AnimateXPText");
            
            // Failsafe reset
            StartCoroutine(ResetScaleAfterDelay(xpTextRect, originalXpScale, animationDuration + 0.1f));
        }
    }

    // Add this method to animate the XP text
    private System.Collections.IEnumerator AnimateXPText()
    {
        if (xpTextRect == null) yield break;
        
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
        
        // Ensure we reach the maximum scale
        xpTextRect.localScale = targetScale;
        
        // Scale back down
        timer = 0;
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            xpTextRect.localScale = Vector3.Lerp(targetScale, originalXpScale, progress);
            yield return null;
        }
        
        // Always force the final scale to be exactly the original
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
        
        // Periodically reset all text scales if enabled
        if (enablePeriodicScaleReset)
        {
            scaleResetTimer += Time.deltaTime;
            if (scaleResetTimer >= scaleResetInterval)
            {
                ResetAllUIScales();
                scaleResetTimer = 0f;
            }
        }

        // Check if we should show the attributes panel after animation
        if (shouldShowAttributesPanel && cameraAnimations != null && !cameraAnimations.IsAnimationInProgress())
        {
            Debug.Log("UIHelper: Animation ended, now showing attributes panel");
            shouldShowAttributesPanel = false;
            ShowPanel();
        }
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

    // Update the egg display method to use our safer animation approach
    public void UpdateEggDisplay(int eggCount)
    {
        if (eggCountText != null && playerInventory != null)
        {
            eggCountText.text = string.Format(countFormat, eggCount, playerInventory.MaxEggCapacity);
            
            if (animateOnChange)
            {
                StartCoroutine(AnimateTextScale(eggTextRect, originalEggScale, animationDuration));
            }
        }
        
        if (eggbarImage != null && playerInventory != null)
        {
            float fillAmount = (float)eggCount / playerInventory.MaxEggCapacity;
            fillAmount = Mathf.Clamp01(fillAmount);
            
            eggbarImage.fillAmount = fillAmount;
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
        
        // Ensure we reach the maximum scale
        eggTextRect.localScale = targetScale;
        
        // Scale back down
        timer = 0;
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            eggTextRect.localScale = Vector3.Lerp(targetScale, originalEggScale, progress);
            yield return null;
        }
        
        // Always force the final scale to be exactly the original
        eggTextRect.localScale = originalEggScale;
    }

    // Add a method to refresh the egg display when incubation changes
    private void RefreshEggDisplay()
    {
        // Find the current egg count from InsectIncubator
        InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
        int currentCount = incubator != null ? incubator.GetCurrentEggCount() : 0;
        
        // Update the display
        UpdateEggDisplay(currentCount);
        Debug.Log("UIHelper: Refreshed egg display after incubation change");
    }
 
    public void DebugAttributePoint(){
        playerInventory.AddCoins(1000);

        // Check if we have a valid reference
        if (cachedPlayerAttributes == null)
        {
            cachedPlayerAttributes = FindObjectOfType<PlayerAttributes>();
            if (cachedPlayerAttributes == null)
            {
                Debug.LogError("UIHelper: Could not find PlayerAttributes component!");
                return;
            }
        }
        
        // Add points using the proper method
        cachedPlayerAttributes.AddAttributePoints(25);
        
        // Make sure the attributes panel is visible
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(true);
            Animator panelAnimator = attributesPanel.GetComponent<Animator>();
            if (panelAnimator != null)
            {
                panelAnimator.SetBool("ShowUp", true);
                panelAnimator.SetBool("Hide", false);
            }
        }
        
        Debug.Log("UIHelper: Debug attribute points added successfully");
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
            int actualMaxLevel = playerInventory != null ? playerInventory.GetMaxLevel() : 55;
            if (data.currentLevel >= actualMaxLevel)
            {
                experienceCountText.text = "MAX LEVEL";
                Debug.Log($"Showing MAX LEVEL text because currentLevel ({data.currentLevel}) >= actualMaxLevel ({actualMaxLevel})");
            }
            else
            {
                Debug.Log($"Not at max level yet: currentLevel ({data.currentLevel}) < actualMaxLevel ({actualMaxLevel})");
                int xpForNextLevel = playerInventory != null ? playerInventory.ExperienceForNextLevel : CalculateXPRequiredForLevel(data.currentLevel + 1);
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
            // Show saving notification
            ShowInformText("Saving game...");
            
            // Save the game
            gameManager.SaveGame();
            
            // Show success message after a brief delay
            StartCoroutine(ShowSaveFeedbackDelayed());
        }
        else
        {
            Debug.LogError("Could not find GameManager to save the game!");
            ShowInformText("Error: Could not save game!");
        }
    }

    private IEnumerator ShowSaveFeedbackDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        ShowInformText("Game saved successfully!");
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
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        Debug.Log("Discovered Entities button clicked!");
        ShowDiscoveredEntitiesPanel();
    }

    public void OnNestMarketButtonClicked()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        Debug.Log("Nest Market button clicked!");
        ShowNestMarketPanel();
    }

    public void ShowNestMarketPanel()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        nestMarketController.GetComponent<Animator>().SetBool("ShowUp", true);
        nestMarketController.GetComponent<Animator>().SetBool("Hide", false);
    }

    public void CloseNestMarketPanel()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        nestMarketController.GetComponent<Animator>().SetBool("ShowUp", false);
        nestMarketController.GetComponent<Animator>().SetBool("Hide", true);
    }

    // Update the coin display method to use the safer approach
    public void UpdateCoinDisplay(int count)
    {
        if (coinCountText != null)
        {
            coinCountText.text = string.Format(coinFormat, count);
            
            if (animateOnChange && coinTextRect != null)
            {
                // Use the safer method
                SafeStopAndStartCoroutine("AnimateCoinCount");
                
                // Failsafe reset
                StartCoroutine(ResetScaleAfterDelay(coinTextRect, originalCoinScale, animationDuration + 0.1f));
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
        
        // Ensure we reach the maximum scale
        coinTextRect.localScale = targetScale;
        
        // Scale back down
        timer = 0;
        while (timer < animationDuration / 2)
        {
            timer += Time.deltaTime;
            float progress = timer / (animationDuration / 2);
            coinTextRect.localScale = Vector3.Lerp(targetScale, originalCoinScale, progress);
            yield return null;
        }
        
        // Always force the final scale to be exactly the original
        coinTextRect.localScale = originalCoinScale;
    }

    // Add this method to the UIHelper class
    private void SafeStopAndStartCoroutine(string coroutineName)
    {
        // Try to stop any running instance of this coroutine
        try {
            StopCoroutine(coroutineName);
        } 
        catch (System.Exception) {
            // Ignore errors if coroutine wasn't running
        }
        
        // Start new coroutine
        StartCoroutine(coroutineName);
    }

    // Improve the reset scale after delay to be more robust
    private IEnumerator ResetScaleAfterDelay(RectTransform targetRect, Vector3 originalScale, float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);
        
        // Force reset the scale to original after the delay no matter what
        if (targetRect != null)
        {
            targetRect.localScale = originalScale;
            Debug.Log($"Force reset scale for {targetRect.name} to {originalScale}");
        }
    }

    // Add a more robust animation system for UI elements
    private IEnumerator AnimateTextScale(RectTransform targetRect, Vector3 originalScale, float duration)
    {
        if (targetRect == null) yield break;
        
        // Animation parameters
        float pulseSize = 1.3f;
        float elapsed = 0f;
        
        // Animation phase 1: Grow
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration * 0.5f);
            float easedProgress = Mathf.SmoothStep(0, 1, progress);
            targetRect.localScale = Vector3.Lerp(originalScale, originalScale * pulseSize, easedProgress);
            yield return null;
        }
        
        // Animation phase 2: Shrink back
        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration * 0.5f);
            float easedProgress = Mathf.SmoothStep(0, 1, progress);
            targetRect.localScale = Vector3.Lerp(originalScale * pulseSize, originalScale, easedProgress);
            yield return null;
        }
        
        // Ensure we end at exactly the original scale
        targetRect.localScale = originalScale;
        
        // We can't use try-catch with yield, so we'll use this failsafe instead
        StartCoroutine(ResetScaleAfterDelay(targetRect, originalScale, 0.1f));
    }

    // Add this helper method to reset all UI scales
    public void ResetAllUIScales()
    {
        if (countTextRect != null) countTextRect.localScale = originalScale;
        if (crumbTextRect != null) crumbTextRect.localScale = originalCrumbScale;
        if (coinTextRect != null) coinTextRect.localScale = originalCoinScale;
        
        Debug.Log("Reset all UI text scales to original values");
    }

    // Add this method to update the experience bar
    private void UpdateExperienceBar(int currentXP, int xpRequiredForNextLevel)
    {
        // Implementation of UpdateExperienceBar method
    }

    // Add this method to UIHelper.cs - can be connected to a debug button in inspector
    public void TriggerBarrierRemovalDebug()
    {
        Debug.Log("<color=yellow>DEBUG: Manually triggering barrier removal animation</color>");
        
        // Find player inventory and trigger the barrier removal
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.DebugTriggerBarrierRemoval();
        }
        else
        {
            Debug.LogError("Could not find PlayerInventory component");
        }
    }

    // Modify the OnAttributeButtonClicked method to use a more direct approach
    public void OnAttributeButtonClicked()
    {
        Debug.Log("UIHelper: Attribute button clicked");
        
        // Find the AttributeDisplay component
        AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
        
        if (attributeDisplay != null)
        {
            // Call the direct, force show method
            attributeDisplay.ForceShowAttributesPanel();
        }
        else
        {
            Debug.LogError("CRITICAL ERROR: AttributeDisplay not found! Cannot show panel.");
            
            // Try to find and activate the panel directly if we can
            GameObject attributesPanel = GameObject.Find("AttributesPanel");
            if (attributesPanel != null)
            {
                Debug.Log("Found AttributesPanel directly - attempting to activate");
                attributesPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("Could not find AttributesPanel in the scene");
            }
        }
    }
} 
