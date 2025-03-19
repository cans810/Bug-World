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
    [SerializeField] private GameObject allyAntsOrdersPanel;
    [SerializeField] private GameObject AllallyAntsOrdersPanel;
    public GameObject attributesPanel;

    [Header("Icon Prefabs")]
    [SerializeField] private GameObject chitinIconPrefab;
    [SerializeField] private Transform posToMoveIconTransform;
    [SerializeField] private Transform posToDropIcons;

    
    // Add a field to cache the PlayerAttributes reference
    private PlayerAttributes cachedPlayerAttributes;

    // Add a reference to the ChitinPanel
    [Header("UI Panels")]
    [SerializeField] private Transform chitinPanel;

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

    // Update the AnimateChitinCollection method to check for ChitinPanel
    public void AnimateChitinCollection(int chitinCount, Vector3 startPosition)
    {
        // Find ChitinPanel if not assigned
        if (chitinPanel == null)
        {
            chitinPanel = transform.Find("ChitinPanel");
            
            // If still not found, try to find it in children recursively
            if (chitinPanel == null)
            {
                chitinPanel = FindChildRecursive(transform, "ChitinPanel");
            }
            
            if (chitinPanel == null)
            {
                Debug.LogWarning("Cannot find ChitinPanel in hierarchy");
            }
        }

        if (chitinIconPrefab == null || posToMoveIconTransform == null || nestTransform == null || chitinPanel == null)
        {
            Debug.LogWarning("Cannot animate chitin collection: missing references");
            return;
        }

        // Start the coroutine to spawn and animate chitin icons
        StartCoroutine(SpawnAndAnimateChitinIcons(chitinCount));
    }

    // Helper method to find a child by name recursively
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        
        return null;
    }

    private IEnumerator SpawnAndAnimateChitinIcons(int chitinCount)
    {
        // Limit the number of icons to prevent overwhelming visuals
        int maxIconsToShow = Mathf.Min(chitinCount, 10);
        
        for (int i = 0; i < maxIconsToShow; i++)
        {
            // Instantiate the chitin icon as a child of the GameObject holding UIHelper
            GameObject chitinIcon = Instantiate(chitinIconPrefab, transform);
            
            // Get the position of the ChitinPanel in screen space
            Vector2 chitinPanelScreenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, chitinPanel.position);
            
            // Convert screen position to world position for the icon
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                transform.GetComponent<RectTransform>(),
                chitinPanelScreenPos,
                Camera.main,
                out Vector3 worldPos);
                
            // Set the position
            chitinIcon.transform.position = worldPos;
            
            // Add slight random offset to prevent all icons from moving in the exact same path
            Vector2 randomOffset = new Vector2(
                Random.Range(-20f, 20f),
                Random.Range(-20f, 20f)
            );
            
            // Move to the intermediate position with animation
            StartCoroutine(MoveChitinIconUI(chitinIcon, 
                chitinPanel, 
                posToMoveIconTransform, 
                randomOffset, 
                0.5f));
            
            // Add slight delay between spawning each icon
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator MoveChitinIconUI(GameObject icon, Transform startTransform, Transform endTransform, Vector3 offset, float duration)
    {
        float startTime = Time.time;
        float endTime = startTime + duration;
        
        // Get the starting position in screen space (returns Vector2)
        Vector2 startPos = RectTransformUtility.WorldToScreenPoint(Camera.main, startTransform.position);
        
        // Get the ending position in screen space (returns Vector2)
        Vector2 endPos = RectTransformUtility.WorldToScreenPoint(Camera.main, endTransform.position);
        
        // Convert offset to Vector2 for adding to screen positions
        Vector2 offset2D = new Vector2(offset.x, offset.y);
        
        // Add the offset to the end position
        endPos += offset2D;
        
        // Move from start to intermediate position
        while (Time.time < endTime)
        {
            float t = (Time.time - startTime) / duration;
            
            // Lerp in screen space
            Vector2 currentScreenPos = Vector2.Lerp(startPos, endPos, t);
            
            // Convert back to world space and set position
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                icon.GetComponent<RectTransform>(),
                currentScreenPos,
                Camera.main,
                out Vector3 worldPos);
                
            icon.transform.position = worldPos;
            yield return null;
        }
        
        // Ensure it reaches the exact position (we'll need to calculate this differently)
        // Instead of directly setting to endTransform.position + offset, we'll use the screen-to-world conversion
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            icon.GetComponent<RectTransform>(),
            endPos,
            Camera.main,
            out Vector3 finalWorldPos);
        
        icon.transform.position = finalWorldPos;
        
        // Wait at the intermediate position
        yield return new WaitForSeconds(1f);
        
        // Now move to the nest
        StartCoroutine(MoveToNestAndDestroyUI(icon, endTransform, nestTransform, 0.7f));
    }

    private IEnumerator MoveToNestAndDestroyUI(GameObject icon, Transform startTransform, Transform endTransform, float duration)
    {
        float startTime = Time.time;
        float endTime = startTime + duration;
        
        // Get the starting position in screen space
        Vector2 startPos = RectTransformUtility.WorldToScreenPoint(Camera.main, startTransform.position);
        
        // Get the ending position in screen space
        Vector2 endPos = RectTransformUtility.WorldToScreenPoint(Camera.main, endTransform.position);
        
        // Move from intermediate position to nest
        while (Time.time < endTime)
        {
            float t = (Time.time - startTime) / duration;
            
            // Lerp in screen space with easing
            float easedT = Mathf.SmoothStep(0, 1, t);
            Vector2 currentScreenPos = Vector2.Lerp(startPos, endPos, easedT);
            
            // Add a slight arc to the movement (only affects y component)
            currentScreenPos.y += 50f * Mathf.Sin(t * Mathf.PI);
            
            // Convert back to world space and set position
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                icon.GetComponent<RectTransform>(),
                currentScreenPos,
                Camera.main,
                out Vector3 worldPos);
                
            icon.transform.position = worldPos;
            
            // Scale down as it approaches the nest
            icon.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t * 0.8f);
            
            yield return null;
        }
        
        // Destroy the icon when it reaches the nest
        Destroy(icon);
    }

    // Update the AnimateChitinDeposit method to not use player position
    public void AnimateChitinDeposit(int chitinCount)
    {
        if (chitinCount > 0)
        {
            AnimateChitinCollection(chitinCount, Vector3.zero); // Position is ignored now
        }
    }
} 
