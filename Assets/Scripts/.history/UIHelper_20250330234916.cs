using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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
    [SerializeField] public TextMeshProUGUI attackCoolDownText;
    [SerializeField] public TextMeshProUGUI sizeUpText;
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

    [Header("Attack Cooldown Settings")]
    [SerializeField] private Vector2 cooldownTextOffset = new Vector2(50, 0); // Changed from (0, 10) to (50, 0) for right-side positioning
    [SerializeField] private bool followPlayer = true; // Can be toggled in inspector

    [Header("Revival Panel")]
    [SerializeField] public GameObject revivalPanel;

    public GameObject LevelUpPanel;

    // Add these methods to UIHelper.cs

    // Variable to track if we're showing a persistent message
    private bool hasPersistentMessage = false;
    private Coroutine persistentMessageCoroutine;

    // Add this field to store the default color
    private Color defaultInformTextColor = Color.white;

    // Keep track of all active coroutines
    private Dictionary<string, Coroutine> activeTextCoroutines = new Dictionary<string, Coroutine>();
    private bool isMessageLocked = false;

    // Add these fields to track message timing directly
    private float currentMessageEndTime = 0f;
    private string currentMessageId = "";

    public GameObject mapPanel;

    // Add this variable to track the map button
    [SerializeField] private Button mapButton;

    public GameObject thisIsYourNestPanel;

    public GameObject attributePanelButtonPointingPulsatingImage;
    public GameObject discoveredEntitiesButtonPointingPulsatingImage;
    public GameObject nestMarketButtonPointingPulsatingImage;



    // Main method to show regular informational text
    public void ShowInformText(string message, float duration = 5f)
    {
        // Don't show new messages if we're locked
        if (isMessageLocked) return;

        // Skip if there's no text component
        if (informPlayerText == null) return;
        
        // Generate a unique ID for this message
        string id = "inform_" + Time.time.ToString("F3");
        
        // Cancel any existing text coroutines
        CancelAllTextCoroutines();
        
        // Set up the text
        informPlayerText.text = message;
        informPlayerText.color = defaultInformTextColor;
        informPlayerText.gameObject.SetActive(true);
        isShowingInformText = true;
        
        // Start timed hide with the unique ID
        Coroutine newCoroutine = StartCoroutine(HideTextAfterDelay(id, duration));
        activeTextCoroutines[id] = newCoroutine;
        currentMessageId = id;
        
        Debug.Log($"<color=yellow>ShowInformText: '{message}' for {duration}s (ID: {id})</color>");
    }

    // Method for persistent messages that don't auto-hide
    public void ShowPersistentInformText(string message)
    {
        // Skip if there's no text component
        if (informPlayerText == null) return;
        
        // Cancel all existing text coroutines
        CancelAllTextCoroutines();
        
        // Lock the message system
        isMessageLocked = true;
        
        // Set up the text
        informPlayerText.text = message;
        informPlayerText.color = defaultInformTextColor;
        informPlayerText.gameObject.SetActive(true);
        isShowingInformText = true;
        
        Debug.Log($"<color=orange>ShowPersistentInformText: '{message}' (stays until cleared)</color>");
    }

    // Method for warnings (red text)
    public void ShowWarningText(string message, float duration = 6f)
    {
        // Don't show new messages if we're locked
        if (isMessageLocked) return;
        
        // Skip if there's no text component
        if (informPlayerText == null) return;
        
        // Generate a unique ID for this message
        string id = "warning_" + Time.time.ToString("F3");
        
        // Cancel any existing text coroutines
        CancelAllTextCoroutines();
        
        // Set up the text
        informPlayerText.text = message;
        informPlayerText.color = Color.red;
        informPlayerText.gameObject.SetActive(true);
        isShowingInformText = true;
        
        // Start timed hide with the unique ID
        Coroutine newCoroutine = StartCoroutine(HideTextAfterDelay(id, duration));
        activeTextCoroutines[id] = newCoroutine;
        currentMessageId = id;
        
        Debug.Log($"<color=red>ShowWarningText: '{message}' for {duration}s (ID: {id})</color>");
    }

    // Method to clear persistent messages
    public void ClearPersistentInformText()
    {
        // Skip if there's no text component
        if (informPlayerText == null) return;
        
        // Reset locked state
        isMessageLocked = false;
        
        // Hide the text
        informPlayerText.gameObject.SetActive(false);
        isShowingInformText = false;
        
        // Reset color to default
        informPlayerText.color = defaultInformTextColor;
        
        Debug.Log($"<color=green>ClearPersistentInformText: Message cleared</color>");
    }

    // Helper to cancel all text coroutines
    private void CancelAllTextCoroutines()
    {
        foreach (var kvp in activeTextCoroutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        activeTextCoroutines.Clear();
    }

    // Single coroutine for timed text hiding
    private IEnumerator HideTextAfterDelay(string id, float duration)
    {
        // Set the end time for this message
        currentMessageId = id;
        currentMessageEndTime = Time.time + duration;
        
        Debug.Log($"<color=magenta>Message '{id}' will hide at time {currentMessageEndTime}</color>");
        
        // Wait for the specified duration
        yield return new WaitForSeconds(duration);
        
        // Only hide if this is still the active message and not locked
        if (!isMessageLocked && currentMessageId == id && informPlayerText != null)
        {
            // Reset text state
            informPlayerText.gameObject.SetActive(false);
            isShowingInformText = false;
            informPlayerText.color = defaultInformTextColor;
            
            // Log actual duration for debugging
            Debug.Log($"<color=cyan>Text '{id}' hidden after {duration:F2}s as scheduled</color>");
        }
        
        // Remove from active coroutines
        if (activeTextCoroutines.ContainsKey(id))
        {
            activeTextCoroutines.Remove(id);
        }
    }

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
        
        // Store the default text color
        if (informPlayerText != null)
        {
            defaultInformTextColor = informPlayerText.color;
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
        
        // Add this subscription
        playerInventory.OnChitinMaxed += HandleChitinMaxed;
        
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

        // Initialize UI
        InitializeUI();

        // Find camera animations
        cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations == null)
        {
            Debug.LogWarning("CameraAnimations not found - map camera effects won't work");
        }

        // Add to Start method to find the map button automatically
        if (mapButton == null)
        {
            // Try to find the map button using common naming conventions
            mapButton = GameObject.FindWithTag("MapButton")?.GetComponent<Button>();
            
            // If not found by tag, try finding by name
            if (mapButton == null)
            {
                Transform buttonTransform = transform.Find("MapButton") ?? 
                                        GameObject.Find("MapButton")?.transform;
                
                if (buttonTransform != null)
                {
                    mapButton = buttonTransform.GetComponent<Button>();
                }
                else
                {
                    Debug.LogWarning("Map button not found! Button interactivity won't be managed.");
                }
            }
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
        if (chitinCountText != null)
        {
            chitinCountText.text = string.Format(countFormat, chitin, playerInventory.MaxChitinCapacity);
            
            // No effect animation
        }
    }
    
    public void UpdateCrumbDisplay(int crumbs)
    {
        if (crumbCountText != null)
        {
            crumbCountText.text = string.Format(countFormat, crumbs, playerInventory.MaxCrumbCapacity);
            
            // No effect animation
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
        
        // Hide attack cooldown text when player levels up
        if (attackCoolDownText != null && attackCoolDownText.gameObject.activeInHierarchy)
        {
            attackCoolDownText.gameObject.SetActive(false);
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

        // Create visual effect
        VisualEffectManager effectManager = FindObjectOfType<VisualEffectManager>();
        if (effectManager != null)
        {
            // Get player position
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Play the level up effect at the player's position
                effectManager.PlayLevelUpEffect(player.transform.position);
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
        attributePanelButtonPointingPulsatingImage.SetActive(false);

        if (attributesPanel != null)
        {
            // Get the AttributeDisplay component
            AttributeDisplay attributeDisplay = attributesPanel.GetComponent<AttributeDisplay>();
            
            // Show the panel
            attributesPanel.SetActive(true);
            
            // Force update the display
            if (attributeDisplay != null)
            {
                attributeDisplay.UpdateDisplay();
                Debug.Log("Forced AttributeDisplay update when showing panel");
            }
            
            // Get the animator if it exists
            Animator animator = attributesPanel.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("ShowUp", true);
                animator.SetBool("Hide", false);
            }
        }
        else
        {
            Debug.LogWarning("Attributes panel reference is missing!");
        }
    }

    public void ShowDiscoveredEntitiesPanel()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        discoveredEntitiesButtonPointingPulsatingImage.SetActive(false);

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
                        
            // Pause between updates
            yield return new WaitForSeconds(xpCounterSpeed);
        }
        
        // Ensure we end at exactly the target value
        currentDisplayedXP = targetXP;
        experienceCountText.text = string.Format(xpFormat, targetXP, xpRequiredForNextLevel);
        
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
        
        // Update the position of the cooldown text if it's active
        if (followPlayer && attackCoolDownText != null && attackCoolDownText.gameObject.activeInHierarchy)
        {
            UpdateCooldownTextPosition();
        }
        
        // Check if we have an active message that should still be showing
        if (isShowingInformText && !isMessageLocked && Time.time < currentMessageEndTime)
        {
            // If the text object isn't active but should be, reactivate it
            if (informPlayerText != null && !informPlayerText.gameObject.activeInHierarchy)
            {
                informPlayerText.gameObject.SetActive(true);
                Debug.Log($"<color=yellow>⚠️ Reactivated hidden message, still has {currentMessageEndTime - Time.time:F2}s remaining</color>");
            }
        }
    }

    // Add diagnostic logging to UpdateEggDisplay
    public void UpdateEggDisplay(int currentEggCount)
    {
        // Find InsectIncubator to get max capacity and verify egg count
        InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
        
        if (incubator != null)
        {
            int maxCapacity = incubator.GetMaxEggCapacity();
            int actualCount = incubator.GetCurrentEggCount();
            
            // Log if there's a mismatch between reported and actual count
            if (actualCount != currentEggCount)
            {
                Debug.LogWarning($"⚠️ Egg count mismatch! Passed count: {currentEggCount}, Actual count: {actualCount}");
                // Use the actual count from the incubator
                currentEggCount = actualCount;
            }
            
            // Update egg count text
            if (eggCountText != null)
            {
                eggCountText.text = string.Format(countFormat, currentEggCount, maxCapacity);
                Debug.Log($"UI Egg display updated: {currentEggCount}/{maxCapacity}");
            }
            
            // Update the egg fill bar
            UpdateEggBar(currentEggCount, maxCapacity);
            
            Debug.Log($"Egg UI fully updated: Count={currentEggCount}, Max={maxCapacity}");
        }
        else
        {
            Debug.LogError("Unable to find InsectIncubator in UpdateEggDisplay!");
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
        nestMarketButtonPointingPulsatingImage.SetActive(false);

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
        //nestMarketButtonPointingPulsatingImage.SetActive(true);

        nestMarketController.GetComponent<Animator>().SetBool("ShowUp", false);
        nestMarketController.GetComponent<Animator>().SetBool("Hide", true);
    }

    // Update the coin display method to use the safer approach
    public void UpdateCoinDisplay(int count)
    {
        if (coinCountText != null)
        {
            coinCountText.text = string.Format(coinFormat, count);
            
            // No effect animation
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

    // Modify the position method to place text at bottom of player and fix flickering
    private void UpdateCooldownTextPosition()
    {
        if (attackCoolDownText == null) return;
        
        // Find the player object
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        // Convert player's world position to screen position
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        // Get position at the side of player (changed from -0.3f Y offset to +0.5f X offset)
        Vector3 playerPos = player.transform.position + new Vector3(0.5f, 0, 0);
        Vector3 screenPos = mainCamera.WorldToScreenPoint(playerPos);
        
        // Check if player is behind the camera
        if (screenPos.z < 0) return;
        
        // Set the position of the text
        RectTransform textRect = attackCoolDownText.rectTransform;
        if (textRect != null)
        {
            // Apply offset for right side positioning
            screenPos.x += cooldownTextOffset.x;
            screenPos.y += cooldownTextOffset.y;
            
            // Ensure text stays within screen bounds
            screenPos.x = Mathf.Clamp(screenPos.x, textRect.rect.width/2, Screen.width - textRect.rect.width/2);
            screenPos.y = Mathf.Clamp(screenPos.y, textRect.rect.height/2, Screen.height - textRect.rect.height/2);
            
            // Use extremely strong smoothing to prevent any flickering
            textRect.position = Vector3.Lerp(textRect.position, new Vector3(screenPos.x, screenPos.y, 0), Time.deltaTime * 50f);
        }
    }

    // Modify the show method to prevent multiple coroutines from conflicting
    public void ShowAttackCooldownText(string message)
    {
        if (attackCoolDownText != null)
        {
            // Only update the text if it's new or the object is inactive
            if (!attackCoolDownText.gameObject.activeInHierarchy || attackCoolDownText.text != message)
            {
                attackCoolDownText.text = message;
                attackCoolDownText.gameObject.SetActive(true);
                
                // Update position immediately when shown
                if (followPlayer)
                {
                    UpdateCooldownTextPosition();
                }
                
                // Stop any existing hide coroutines to prevent conflicts
                StopCoroutine("HideAttackCooldownText");
                
                // Start coroutine to auto-hide after the cooldown completes
                StartCoroutine(HideAttackCooldownText(message));
            }
        }
    }

    // Fix the hiding coroutine to prevent rapid activation/deactivation
    private IEnumerator HideAttackCooldownText(string originalMessage)
    {
        // Try to parse the cooldown time from the message
        float cooldownTime = 0f;
        try
        {
            // Extract number from text like "Cooldown X.Xs"
            string numericPart = Regex.Match(originalMessage, @"[\d\.]+").Value;
            if (!string.IsNullOrEmpty(numericPart))
            {
                cooldownTime = float.Parse(numericPart);
                // Add a larger buffer to ensure we don't hide prematurely (0.5s instead of 0.2s)
                cooldownTime += 0.5f;
            }
            else
            {
                cooldownTime = 2f; // Default if no number found
            }
        }
        catch (System.Exception)
        {
            cooldownTime = 2f; // Default fallback if parsing fails
        }
        
        // Wait for the cooldown to finish
        yield return new WaitForSeconds(cooldownTime);
        
        // Add a small delay before hiding to prevent rapid toggling
        yield return new WaitForSeconds(0.1f);
        
        // Hide the text only if it's still showing this message (prevents race conditions)
        if (attackCoolDownText != null && attackCoolDownText.text == originalMessage)
        {
            attackCoolDownText.gameObject.SetActive(false);
        }
    }

    // Add this method to initialize the UI in Start
    private void InitializeUI()
    {
        // Initialize attack cooldown text
        if (attackCoolDownText != null)
        {
            attackCoolDownText.gameObject.SetActive(false);
        }
        
        // Other UI initialization code...
    }

    // Watch rewarded ad for free revival
    public void WatchRewardedAdButtonForRevivalClicked()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        
        // Use AdManager instead of directly accessing RewardedAdExample
        if (AdManager.Instance != null && AdManager.Instance.IsInitialized)
        {
            // Register for the ad completion callback first
            AdManager.Instance.OnRewardedAdComplete += HandleRewardedAdCompleted;
            
            // Show the rewarded ad
            AdManager.Instance.ShowRewardedAd();
            
            // Show a loading message
            ShowInformText("Loading advertisement...");
        }
        else
        {
            Debug.LogError("AdManager not initialized!");
            ShowInformText("Error loading advertisement. Please try another option.");
        }
    }

    // Callback method for when rewarded ad completes
    private void HandleRewardedAdCompleted()
    {
        // Unregister from the event to prevent multiple calls
        if (AdManager.Instance != null)
        {
            AdManager.Instance.OnRewardedAdComplete -= HandleRewardedAdCompleted;
        }
        
        // Find the player controller to revive
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            // Hide the revival panel
            if (revivalPanel != null)
            {
                ShowRevivalPanel(false);
            }
            
            // Revive the player - importantly, we don't modify chitin or crumbs here
            // so they keep all their resources
            RevivePlayer(playerController, "You have been revived by watching an ad!");
        }
    }

    // Fix the coin payment revival option
    public void Give50CoinsForRevivalClicked()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        
        // Find the player inventory to check for coins
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            // Check if player has enough coins
            if (playerInventory.CoinCount >= 50)
            {
                // Directly remove coins using the PlayerInventory method
                playerInventory.RemoveCoins(50);
                
                // Update coin display
                if (coinCountText != null)
                {
                    coinCountText.text = string.Format(coinFormat, playerInventory.CoinCount);
                }
                
                // Find the player controller to revive
                PlayerController playerController = FindObjectOfType<PlayerController>();
                if (playerController != null)
                {
                    // Hide the revival panel
                    if (revivalPanel != null)
                    {
                        ShowRevivalPanel(false);
                    }
                    
                    // Revive player - keep their chitin and crumbs
                    RevivePlayer(playerController, "You have been revived by spending 50 coins!");
                }
            }
            else
            {
                // Not enough coins
                ShowInformText("Not enough coins! You need 50 coins to revive.");
                
                // Play error sound
                if (SoundEffectManager.Instance != null)
                {
                    SoundEffectManager.Instance.PlaySound("Error");
                }
            }
        }
    }

    // Fix the close button to set chitin and crumbs to zero
    public void CloseRevivalAndLooseChitinAndCrumbsClicked()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");

        // Find the player controller to revive
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            // Hide the revival panel
            if (revivalPanel != null)
            {
                ShowRevivalPanel(false);
            }
            
            // Revive the player - importantly, we don't modify chitin or crumbs here
            // so they keep all their resources
            RevivePlayer(playerController, "You lost all your chitin and crumbs but have been revived!");

            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();

            // Reset chitin and crumbs to zero
            playerInventory.SetChitinCount(0);
            playerInventory.SetCrumbCount(0);
            
            // Update UI with reviving still true
            playerInventory.ForceUpdateUIUpdateChitinCapacity();
            playerInventory.ForceUpdateUIUpdateCrumbCapacity();
        }
    }

    // Common method to handle player revival
    private void RevivePlayer(PlayerController playerController, string message)
    {
        if (playerController != null)
        {
            // Get the LivingEntity component
            LivingEntity livingEntity = playerController.GetComponent<LivingEntity>();
            // Get player inventory to temporarily disable level up events
            PlayerInventory playerInventory = playerController.GetComponent<PlayerInventory>();
            
            // Set a revival flag to prevent level up effects if inventory exists
            if (playerInventory != null)
            {
                playerInventory.IsReviving = true;
            }
            
            if (livingEntity != null)
            {
                // Revive the player
                livingEntity.Revive();
                
                // Reset player to spawn position if available
                if (playerController.spawnPoint != null)
                {
                    playerController.transform.position = playerController.spawnPoint.position;
                    playerController.transform.rotation = playerController.spawnPoint.rotation;
                }
                
                // Re-enable player controls
                playerController.enabled = true;
                
                // Reset animation state if needed
                AnimationController animController = playerController.GetComponent<AnimationController>();
                if (animController != null)
                {
                    animController.SyncWithLivingEntity();
                    animController.SetIdle();
                }
            }
            
            // Play revival sound effect
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound("Revived");
            }
            
            // Show success message
            ShowInformText(message);
            
            // Reset the revival flag
            if (playerInventory != null)
            {
                playerInventory.IsReviving = false;
            }
        }
    }

    // Modify the method to use animator parameters
    public void ShowRevivalPanel(bool show)
    {
        if (revivalPanel != null)
        {
            // Get the animator component
            Animator animator = revivalPanel.GetComponent<Animator>();
            if (animator != null)
            {
                revivalPanel.SetActive(show);
                // Set the appropriate animation parameters
                animator.SetBool("ShowUp", show);
                animator.SetBool("Hide", !show);
                
                // Play sound effect if showing the panel
                if (show && SoundEffectManager.Instance != null)
                {
                    SoundEffectManager.Instance.PlaySound("YouDied");
                }
            }
            else
            {
                Debug.LogWarning("Animator component missing on revival panel!");
                // Fallback to direct activation if no animator is found
                revivalPanel.SetActive(show);
            }
        }
        else
        {
            Debug.LogWarning("Revival panel reference is missing in UIHelper!");
        }
    }

    // Then add this method to handle the chitin maxed event
    private void HandleChitinMaxed()
    {
        // Show warning text
        ShowWarningText("Chitin Capacity Reached, Go To Your Nest To deposit.", 6f);
        
        // Show arrow to the nest
        ChitinDepositArrowManager arrowManager = FindObjectOfType<ChitinDepositArrowManager>();
        if (arrowManager != null)
        {
            arrowManager.ForceShowArrow();
        }
    }

    // Add this method to hide inform text (used by PlayerController)
    public void HideInformText()
    {
        // Skip if there's no text component
        if (informPlayerText == null) return;
        
        // Only hide if not showing a persistent/locked message
        if (!isMessageLocked)
        {
            // Cancel any existing text coroutines
            CancelAllTextCoroutines();
            
            // Reset text state
            informPlayerText.gameObject.SetActive(false);
            isShowingInformText = false;
            informPlayerText.color = defaultInformTextColor;
            
            Debug.Log("HideInformText: Text hidden manually");
        }
    }

    // Modify ShowMapPanel method to also show entity icons
    public void ShowMapPanel()
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        
        // Disable the map button to prevent double-clicking
        if (mapButton != null)
        {
            mapButton.interactable = false;
        }
        
        // Show entity icons for locked areas
        ToggleAllEntityIcons(true);
        
        // Show the map panel UI
        mapPanel.SetActive(true);
        mapPanel.transform.parent.GetComponent<Animator>().SetBool("ShowUp", true);
        mapPanel.transform.parent.GetComponent<Animator>().SetBool("Hide", false);
        
        // Animate camera to map view if available
        if (cameraAnimations != null)
        {
            cameraAnimations.AnimateToMapView();
        }
        
        // Disable player controls while in map view
        DisablePlayerControlsForMap(true);
    }

    // Modify HideMapPanel method to also hide entity icons
    public void HideMapPanel()  
    {
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        
        // Re-enable the map button
        if (mapButton != null)
        {
            mapButton.interactable = true;
        }
        
        // Hide all entity icons
        ToggleAllEntityIcons(false);
        
        // Hide the map panel UI
        mapPanel.transform.parent.GetComponent<Animator>().SetBool("ShowUp", false);
        mapPanel.transform.parent.GetComponent<Animator>().SetBool("Hide", true);
        
        // Return camera from map view if available
        if (cameraAnimations != null)
        {
            cameraAnimations.ReturnFromMapView();
        }
        
        // Re-enable player controls
        DisablePlayerControlsForMap(false);
    }

    // Helper method to disable player controls during map view
    private void DisablePlayerControlsForMap(bool disable)
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.SetControlsEnabled(!disable);
        }
    }

    // Add this method to UIHelper class
    private void ToggleAllEntityIcons(bool showIcons)
    {
        // Find all border visualizers
        BorderVisualizer[] allBorders = FindObjectsOfType<BorderVisualizer>();
        
        foreach (var border in allBorders)
        {
            if (showIcons)
            {
                // Only show icons for borders that haven't been unlocked yet
                if (!border.IsSpawningEnabled())
                {
                    border.ActivateEntityIcons();
                }
            }
            else
            {
                // Hide all icons when map view is closed
                border.DeactivateEntityIcons();
            }
        }
        
        Debug.Log($"Toggled all entity icons: {(showIcons ? "Visible" : "Hidden")} for map view");
    }

    // Add these methods to the UIHelper class to update the resource bars

    // Method to update the chitin bar fill amount
    public void UpdateChitinBar(int currentAmount, int maxAmount)
    {
        if (chitinbarImage != null)
        {
            // Calculate fill amount (0.0 to 1.0)
            float fillAmount = (float)currentAmount / maxAmount;
            
            // Ensure fill amount is within valid range
            fillAmount = Mathf.Clamp01(fillAmount);
            
            // Update the fill amount
            chitinbarImage.fillAmount = fillAmount;
            
            // Optionally add a visual effect when the bar changes
            StartCoroutine(PulseEffect(chitinbarImage));
        }
    }

    // Method to update the crumb bar fill amount
    public void UpdateCrumbBar(int currentAmount, int maxAmount)
    {
        if (crumbbarImage != null)
        {
            // Calculate fill amount (0.0 to 1.0)
            float fillAmount = (float)currentAmount / maxAmount;
            
            // Ensure fill amount is within valid range
            fillAmount = Mathf.Clamp01(fillAmount);
            
            // Update the fill amount
            crumbbarImage.fillAmount = fillAmount;
            
            // Optionally add a visual effect when the bar changes
            StartCoroutine(PulseEffect(crumbbarImage));
        }
    }

    // Method to update the egg bar fill amount
    public void UpdateEggBar(int currentAmount, int maxAmount)
    {
        if (eggbarImage != null)
        {
            // Calculate fill amount (0.0 to 1.0)
            float fillAmount = (float)currentAmount / maxAmount;
            
            // Ensure fill amount is within valid range
            fillAmount = Mathf.Clamp01(fillAmount);
            
            // Update the fill amount
            eggbarImage.fillAmount = fillAmount;
            
            // Optionally add a visual effect when the bar changes
            StartCoroutine(PulseEffect(eggbarImage));
        }
    }

    // Modified pulse effect that only changes fill amount temporarily
    private IEnumerator PulseEffect(Image barImage)
    {
        // Store original fill amount
        float originalFill = barImage.fillAmount;
        
        // Add a small amount to the fill (capped at 1.0)
        float pulseFill = Mathf.Min(originalFill + 0.15f, 1.0f);
        
        // Duration of the pulse effect
        float duration = 0.3f;
        float halfDuration = duration / 2f;
        float elapsed = 0f;
        
        // Increase fill
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            // Lerp from original to pulse fill
            barImage.fillAmount = Mathf.Lerp(originalFill, pulseFill, t);
            
            yield return null;
        }
        
        // Decrease fill back to original
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            // Lerp from pulse back to original fill
            barImage.fillAmount = Mathf.Lerp(pulseFill, originalFill, t);
            
            yield return null;
        }
        
        // Ensure we return to original fill
        barImage.fillAmount = originalFill;
    }

    public void CloseThisIsYourNestPanel(){
        SoundEffectManager.Instance.PlaySound("ButtonClicked");
        nestMarketButtonPointingPulsatingImage.SetActive(true);

        thisIsYourNestPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        thisIsYourNestPanel.GetComponent<Animator>().SetBool("Hide", true);
    }
} 
