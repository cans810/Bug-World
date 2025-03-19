using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIHelper : MonoBehaviour
{
    // Singleton pattern for easy access
    public static UIHelper Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI chitinCountText;
    [SerializeField] private TextMeshProUGUI crumbCountText;
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
    
    private PlayerInventory playerInventory;
    private RectTransform countTextRect;
    private RectTransform crumbTextRect;
    private RectTransform xpTextRect;
    private Vector3 originalScale;
    private Vector3 originalCrumbScale;
    private Vector3 originalXpScale;

    public GameObject attributesPanel;

    [Header("AllyAnts UI Components")]
    public bool isAllyAntsPanelOpen = false;
    [SerializeField] private GameObject allyAntsPanel;
    [SerializeField] private GameObject allyAntsOrdersPanel;
    
    [Header("Inform Text")]
    [SerializeField] private TextMeshProUGUI informText;
    [SerializeField] private float informTextDuration = 3f;
    [SerializeField] private Animator informTextAnimator;
    
    // Animation parameter names
    private readonly string appearParam = "Appear";
    private readonly string disappearParam = "Disappear";
    
    // Track if text is currently showing
    private bool isInformTextShowing = false;
    private Coroutine informTextCoroutine;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    
    }
    
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

    public void ShowPanel()
    {
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(true);
        }
    }

    public void ClosePanel()
    {
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(false);
        }
    }

    public void OnAllyAntsPanelClicked()
    {
        isAllyAntsPanelOpen = !isAllyAntsPanelOpen;
        allyAntsPanel.SetActive(isAllyAntsPanelOpen);
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
            }
        }
    }

    private void Update()
    {
        // Update HP display every frame to keep it current
        UpdateHPDisplay();
    }

    // Show inform text with animation
    public void ShowInformText(string message, float duration = -1)
    {
        if (informText == null || informTextAnimator == null)
        {
            Debug.LogWarning("Inform text components not set up properly");
            return;
        }
        
        // Set the text
        informText.text = message;
        
        // If already showing, stop the current hide coroutine
        if (isInformTextShowing && informTextCoroutine != null)
        {
            StopCoroutine(informTextCoroutine);
        }
        
        // Set the appear parameter to true (as a bool, not a trigger)
        informTextAnimator.SetBool(appearParam, true);
        informTextAnimator.SetBool(disappearParam, false);
        
        // Mark as showing
        isInformTextShowing = true;
        
        // Start the hide coroutine
        float actualDuration = duration > 0 ? duration : informTextDuration;
        informTextCoroutine = StartCoroutine(HideInformTextAfterDelay(actualDuration));
    }
    
    // Hide inform text with animation
    public void HideInformText()
    {
        if (informTextAnimator == null || !isInformTextShowing)
            return;
        
        // Trigger the disappear animation
        informTextAnimator.SetBool(appearParam, false);
        informTextAnimator.SetBool(disappearParam, true);
        
        // Start coroutine to disable the panel after animation completes
        StartCoroutine(DisableInformTextAfterAnimation());
    }
    
    // Coroutine to hide the inform text after a delay
    private IEnumerator HideInformTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideInformText();
    }
    
    // Coroutine to disable the panel after animation completes
    private IEnumerator DisableInformTextAfterAnimation()
    {
        // Get the length of the disappear animation
        float animationLength = 0;
        AnimatorClipInfo[] clipInfo = informTextAnimator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            animationLength = clipInfo[0].clip.length;
        }
        else
        {
            animationLength = 0.5f; // Default fallback if we can't get the actual length
        }
        
        // Wait for the animation to complete
        yield return new WaitForSeconds(animationLength);
        
        // Disable the panel and mark as not showing
        informTextPanel.SetActive(false);
        isInformTextShowing = false;
    }
} 
