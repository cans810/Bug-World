using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    [SerializeField] private GameObject allyAntsOrdersPanel;
    [SerializeField] private GameObject AllallyAntsOrdersPanel;
    public GameObject attributesPanel;

    [Header("Icon Prefabs")]
    [SerializeField] private GameObject chitinIconPrefab;
    [SerializeField] private Transform posToMoveIconTransform;

    
    // Add a field to cache the PlayerAttributes reference
    private PlayerAttributes cachedPlayerAttributes;

    // Add a reference to the ChitinPanel
    [Header("UI Panels")]
    [SerializeField] private Transform chitinPanel;

    // Add these fields to track animation state
    private bool isAnimatingChitin = false;
    private Coroutine currentChitinAnimation = null;

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
        
        // Validate the chitin icon prefab
        ValidateChitinIconPrefab();
        
        // Create test button for debugging
        CreateTestButton();
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
        AllallyAntsOrdersPanel.SetActive(true);
        AllallyAntsOrdersPanel.GetComponent<Animator>().SetBool("ShowUp", true);
        AllallyAntsOrdersPanel.GetComponent<Animator>().SetBool("Hide", false);
    }

    public void OnAllyAntsPanelClosed()
    {
        allyAntsPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        allyAntsPanel.GetComponent<Animator>().SetBool("Hide", true);
        AllallyAntsOrdersPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        AllallyAntsOrdersPanel.GetComponent<Animator>().SetBool("Hide", true);
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

    public void ShowInformText(string text)
    {
        // Cancel any existing hide coroutines
        StopAllCoroutines();
        
        if (informPlayerText == null)
        {
            Debug.LogWarning("Inform player text is null!");
            return;
        }
        
        // Set text first
        informPlayerText.text = text;
        
        // Get animator reference once
        Animator textAnimator = informPlayerText.GetComponent<Animator>();
        if (textAnimator == null)
        {
            Debug.LogWarning("No Animator component found on informPlayerText!");
            return;
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

    // Improved debug method with better coroutine management
    public void DebugAnimateChitins(int count)
    {
        Debug.Log($"[DEBUG] Directly testing chitin animation with count: {count}");
        
        // If already animating, stop the current animation
        if (isAnimatingChitin && currentChitinAnimation != null)
        {
            StopCoroutine(currentChitinAnimation);
            Debug.Log("[DEBUG] Stopped existing animation");
        }
        
        // Reset the animation state
        isAnimatingChitin = true;
        
        // Start a new animation and store the reference
        currentChitinAnimation = StartCoroutine(SpawnAndAnimateChitinIcons(count));
    }

    // Improved animation method with better error handling and state management
    private IEnumerator SpawnAndAnimateChitinIcons(int chitinCount)
    {
        Debug.Log($"[CRITICAL] Animation started with count: {chitinCount}");
        
        try
        {
            // Ensure we have a valid count
            if (chitinCount <= 0)
            {
                Debug.LogError("[CRITICAL] Invalid chitin count: " + chitinCount);
                yield break;
            }
            
            // Get starting position - use chitin count text if available
            Vector3 startPos;
            if (chitinCountText != null)
            {
                // Use the chitin count text position
                startPos = chitinCountText.transform.position;
                Debug.Log($"[CRITICAL] Using chitinCountText position: {startPos}");
            }
            else
            {
                // Use top-right corner as fallback
                startPos = new Vector3(Screen.width * 0.9f, Screen.height * 0.9f, 0);
                Debug.Log($"[CRITICAL] Using fallback position: {startPos}");
            }
            
            // Find nest or use a fixed position
            Vector3 targetPos;
            GameObject nestObject = GameObject.Find("Player Nest 1");
            if (nestObject != null)
            {
                targetPos = Camera.main.WorldToScreenPoint(nestObject.transform.position);
                targetPos.z = 0;
                Debug.Log($"[CRITICAL] Using nest position as target: {targetPos}");
            }
            else
            {
                // Use center-bottom as fallback target
                targetPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.1f, 0);
                Debug.Log($"[CRITICAL] Using fallback target position: {targetPos}");
            }
            
            // Create all icons at once
            List<GameObject> allIcons = new List<GameObject>();
            for (int i = 0; i < chitinCount; i++)
            {
                if (chitinIconPrefab == null)
                {
                    Debug.LogError("[CRITICAL] Chitin icon prefab is null!");
                    continue; // Skip this icon but try to continue with others
                }
                
                // Create icon as a direct child of the canvas
                GameObject icon = Instantiate(chitinIconPrefab, transform);
                icon.name = $"ChitinIcon_{i}"; // Give it a unique name for debugging
                
                // Force the icon to be visible with a bright color
                Image iconImage = icon.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.color = Color.red; // Use a very visible color for debugging
                    iconImage.raycastTarget = false;
                }
                
                // Set position explicitly
                RectTransform iconRect = icon.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    // Make it large for visibility during testing
                    iconRect.sizeDelta = new Vector2(100, 100);
                    
                    // Position it at the start position with a slight offset for each icon
                    Vector3 offset = new Vector3(i * 20, i * 20, 0);
                    icon.transform.position = startPos + offset;
                    
                    Debug.Log($"[CRITICAL] Positioned icon {i+1} at {icon.transform.position}");
                }
                
                // Make sure it's active
                icon.SetActive(true);
                
                allIcons.Add(icon);
            }
            
            Debug.Log($"[CRITICAL] Created {allIcons.Count} icons");
        }
        catch (System.Exception e)
        {
            // Log any errors that occur during setup
            Debug.LogError($"[CRITICAL] Error in chitin animation setup: {e.Message}\n{e.StackTrace}");
            isAnimatingChitin = false;
            currentChitinAnimation = null;
            yield break;
        }
        
        // Wait a frame to ensure icons are initialized - outside try/catch
        yield return null;
        
        // Now animate each icon sequentially
        List<GameObject> icons = GameObject.FindGameObjectsWithTag("ChitinIcon")
            .Where(obj => obj.name.StartsWith("ChitinIcon_"))
            .ToList();
        
        if (icons.Count == 0)
        {
            Debug.LogWarning("[CRITICAL] No chitin icons found to animate");
            isAnimatingChitin = false;
            currentChitinAnimation = null;
            yield break;
        }
        
        for (int i = 0; i < icons.Count; i++)
        {
            GameObject icon = icons[i];
            if (icon == null) continue;
            
            try
            {
                // Wait a moment before starting each icon's animation
                yield return new WaitForSeconds(0.1f);
                
                Debug.Log($"[CRITICAL] Starting animation for icon {i+1}");
                
                // Use a simpler animation approach
                float duration = 1.5f;
                float elapsedTime = 0f;
                Vector3 startPosition = icon.transform.position;
                Vector3 originalScale = icon.transform.localScale;
                
                // Get target position again in case it changed
                Vector3 targetPos;
                GameObject nestObject = GameObject.Find("Player Nest 1");
                if (nestObject != null)
                {
                    targetPos = Camera.main.WorldToScreenPoint(nestObject.transform.position);
                    targetPos.z = 0;
                }
                else
                {
                    // Use center-bottom as fallback target
                    targetPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.1f, 0);
                }
                
                // Run animation in a separate coroutine to avoid yield in try/catch
                yield return AnimateIcon(icon, startPosition, targetPos, originalScale, duration);
                
                // Final cleanup
                if (icon != null)
                {
                    Debug.Log($"[CRITICAL] Destroying icon {i+1}");
                    Destroy(icon);
                }
            }
            catch (System.Exception e)
            {
                // Log any errors that occur during animation
                Debug.LogError($"[CRITICAL] Error animating icon {i+1}: {e.Message}");
                if (icon != null) Destroy(icon);
            }
        }
        
        Debug.Log("[CRITICAL] All animations completed");
        
        // Always reset the animation state when done
        isAnimatingChitin = false;
        currentChitinAnimation = null;
    }

    // Helper method to animate a single icon
    private IEnumerator AnimateIcon(GameObject icon, Vector3 startPosition, Vector3 targetPos, Vector3 originalScale, float duration)
    {
        if (icon == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && icon != null)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            
            // Use a simple arc motion
            float arcHeight = 200f * Mathf.Sin(normalizedTime * Mathf.PI);
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPos, normalizedTime);
            currentPos.y += arcHeight;
            
            // Update position and scale
            if (icon != null) // Double-check icon still exists
            {
                icon.transform.position = currentPos;
                
                // Scale down gradually
                icon.transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.3f, normalizedTime);
                
                // Log position periodically
                if (Mathf.Approximately(normalizedTime * 4, Mathf.Floor(normalizedTime * 4)))
                {
                    Debug.Log($"[CRITICAL] Icon at position {currentPos}, progress: {normalizedTime:F2}");
                }
            }
            
            yield return null;
        }
    }

    // Add this method to validate the chitin icon prefab
    private void ValidateChitinIconPrefab()
    {
        if (chitinIconPrefab == null)
        {
            Debug.LogError("Chitin icon prefab is not assigned!");
            return;
        }
        
        // Check if the prefab has the required components
        Image iconImage = chitinIconPrefab.GetComponent<Image>();
        if (iconImage == null)
        {
            Debug.LogWarning("Chitin icon prefab is missing an Image component!");
        }
        
        RectTransform rectTransform = chitinIconPrefab.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning("Chitin icon prefab is missing a RectTransform component!");
        }
    }

    // Add this method to create a test button in the scene
    private void CreateTestButton()
    {
        // Check if a test button already exists
        if (GameObject.Find("ChitinAnimationTestButton") != null)
            return;
        
        // Create a button GameObject
        GameObject buttonObj = new GameObject("ChitinAnimationTestButton");
        buttonObj.transform.SetParent(transform, false);
        
        // Add RectTransform
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(100, 100);
        rectTransform.sizeDelta = new Vector2(200, 50);
        
        // Add Image component
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.8f);
        
        // Add Button component
        Button button = buttonObj.AddComponent<Button>();
        
        // Add Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        // Add RectTransform to text
        RectTransform textRectTransform = textObj.AddComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.offsetMin = Vector2.zero;
        textRectTransform.offsetMax = Vector2.zero;
        
        // Add Text component
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Test Chitin Animation";
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24;
        
        // Add click listener
        button.onClick.AddListener(() => {
            Debug.Log("[TEST] Test button clicked, running animation with 3 chitins");
            DebugAnimateChitins(3);
        });
    }
} 
