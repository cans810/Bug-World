using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIVisualEffectController : MonoBehaviour
{
    public GameObject attributeSymbolPrefab;
    public GameObject chitinSymbolPrefab;
    public GameObject crumbSymbolPrefab;
    public GameObject xpSymbolPrefab;
    public GameObject coinSymbolPrefab;

    public GameObject coinPanel;
    public GameObject xpPanel;
    public GameObject chitinPanel;
    public GameObject crumbPanel;

    [Header("Animation Settings")]
    [SerializeField] private float flyDuration = 1.0f;
    [SerializeField] private float xpFlyDuration = 0.8f;
    [SerializeField] private float symbolSpacing = 30f;
    [SerializeField] private int maxSymbolsPerDeposit = 10;
    [SerializeField] private float spawnDelay = 0.1f;
    [SerializeField] private AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Symbol Scaling")]
    [SerializeField] private Vector3 chitinSymbolScale = new Vector3(0.2f, 0.2f, 0.2f); // Much smaller scale
    [SerializeField] private Vector3 xpSymbolScale = new Vector3(0.3f, 0.3f, 0.3f);     // Adjust as needed
    [SerializeField] private Vector3 coinSymbolScale = new Vector3(0.25f, 0.25f, 0.25f); // Adjustable coin scale

    // Reference to the Canvas
    private Canvas parentCanvas;

    // Track how many XP symbols have reached the target
    private int xpSymbolsReachedTarget = 0;
    private int totalXpSymbols = 0;
    private bool soundPlayed = false;

    // Add these variables at class level
    private PlayerInventory playerInventory;
    private bool xpAddedThisSession = false;
    private int pendingXpAmount = 0;

    private void Awake()
    {
        // Get the canvas reference
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogWarning("UIVisualEffectController should be a child of a Canvas!");
            parentCanvas = FindObjectOfType<Canvas>();
        }
    }

    // Called when chitin is deposited
    public void PlayChitinDepositEffect(int chitinAmount, int xpAmount, PlayerInventory inventory)
    {
        if (chitinSymbolPrefab == null || xpSymbolPrefab == null)
        {
            Debug.LogError("Symbol prefabs not assigned!");
            return;
        }

        // Store the inventory reference
        playerInventory = inventory;
        pendingXpAmount = xpAmount;
        
        // Reset tracking variables
        xpSymbolsReachedTarget = 0;
        totalXpSymbols = 0;
        soundPlayed = false;
        xpAddedThisSession = false;
        
        // Limit the number of symbols to avoid overwhelming the screen
        int symbolCount = Mathf.Min(chitinAmount, maxSymbolsPerDeposit);
        
        // Start the coroutine for spawning symbols
        StartCoroutine(SpawnChitinSymbols(symbolCount, xpAmount));
    }

    private IEnumerator SpawnChitinSymbols(int count, int xpAmount)
    {
        if (chitinPanel == null)
        {
            Debug.LogWarning("Chitin panel not assigned!");
            yield break;
        }

        // Calculate center of screen
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        // Get position of chitin panel
        RectTransform chitinRT = chitinPanel.GetComponent<RectTransform>();
        if (chitinRT == null)
        {
            Debug.LogWarning("Chitin panel has no RectTransform!");
            yield break;
        }
        
        // Get the world corners of the panel
        Vector3[] corners = new Vector3[4];
        chitinRT.GetWorldCorners(corners);
        
        // Calculate panel center
        Vector2 panelCenter = new Vector2(
            (corners[0].x + corners[2].x) / 2f,
            (corners[0].y + corners[2].y) / 2f
        );

        // Calculate how many XP symbols to spawn per chitin
        int xpSymbolsPerChitin = Mathf.CeilToInt((float)xpAmount / count);
        totalXpSymbols = Mathf.Min(xpAmount, maxSymbolsPerDeposit);
        
        // Spawn chitin symbols with delay
        for (int i = 0; i < count; i++)
        {
            // Create chitin symbol
            GameObject chitinSymbol = Instantiate(chitinSymbolPrefab, transform);
            RectTransform symbolRT = chitinSymbol.GetComponent<RectTransform>();
            
            // Apply the smaller scale
            symbolRT.localScale = chitinSymbolScale;
            
            // Position near chitin panel with slight randomization
            symbolRT.position = panelCenter + new Vector2(
                Random.Range(-20f, 20f),
                Random.Range(-20f, 20f)
            );

            // Calculate XP symbols for this chitin
            int xpForThisChitin = Mathf.Min(xpSymbolsPerChitin, xpAmount - (i * xpSymbolsPerChitin));
            xpForThisChitin = Mathf.Max(0, xpForThisChitin); // Ensure not negative
            
            // Start animation coroutine - pass XP for this chitin
            StartCoroutine(AnimateChitinToCenter(chitinSymbol, screenCenter, i, xpForThisChitin));
            
            // Wait before spawning next symbol
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private IEnumerator AnimateChitinToCenter(GameObject chitinSymbol, Vector2 screenCenter, int index, int xpToSpawn)
    {
        RectTransform symbolRT = chitinSymbol.GetComponent<RectTransform>();
        if (symbolRT == null) yield break;
        
        // Store start position and initial scale
        Vector2 startPos = symbolRT.position;
        Vector3 baseScale = symbolRT.localScale;
        
        // Add slight offset to screen center based on index to avoid all symbols overlapping
        float angle = (index * 36f) % 360f;
        float radius = 20f;
        Vector2 targetPos = screenCenter + new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
            Mathf.Sin(angle * Mathf.Deg2Rad) * radius
        );
        
        // Create control points for bezier curve - make each path unique
        Vector2 controlPoint1 = Vector2.Lerp(startPos, targetPos, 0.3f);
        controlPoint1 += new Vector2(
            Random.Range(-80f, 80f), 
            Random.Range(-40f, 80f)
        );
        
        Vector2 controlPoint2 = Vector2.Lerp(startPos, targetPos, 0.7f);
        controlPoint2 += new Vector2(
            Random.Range(-80f, 80f), 
            Random.Range(-40f, 80f)
        );
        
        // Animation
        float elapsed = 0f;
        
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            
            // Use animation curve for smoother motion
            float curvedT = flyCurve.Evaluate(t);
            
            // Use cubic bezier curve for more natural arcing motion
            symbolRT.position = CubicBezier(startPos, controlPoint1, controlPoint2, targetPos, curvedT);
            
            // Rotation for more dynamic movement
            symbolRT.Rotate(0, 0, Time.deltaTime * 180f * (1 + index % 3));
            
            // Scale up slightly during flight but maintain the aspect ratio
            float scaleMultiplier = Mathf.Lerp(0.8f, 1.2f, Mathf.Sin(t * Mathf.PI));
            symbolRT.localScale = baseScale * scaleMultiplier;
            
            yield return null;
        }
        
        // Ensure final position is reached
        symbolRT.position = targetPos;
        
        // Pop animation at the end
        StartCoroutine(PopAnimation(chitinSymbol, baseScale));
        
        // Immediately spawn XP symbols from this position
        if (xpToSpawn > 0)
        {
            // Spawn XP symbols right away
            PlayXPGainEffect(targetPos, xpToSpawn);
        }
        
        // Destroy after a short delay
        Destroy(chitinSymbol, 0.2f);
    }
    
    private IEnumerator PopAnimation(GameObject symbol, Vector3 baseScale)
    {
        RectTransform rt = symbol.GetComponent<RectTransform>();
        if (rt == null) yield break;
        
        // Quick scale up and down
        float duration = 0.15f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Pop curve: quick scale up then down, but preserve original scale ratio
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI) * 0.4f;
            rt.localScale = baseScale * scaleMultiplier;
            
            yield return null;
        }
        
        // Reset to base scale, not Vector3.one
        rt.localScale = baseScale;
    }

    public void PlayXPGainEffect(Vector2 spawnPosition, int xpAmount)
    {
        if (xpSymbolPrefab == null || xpPanel == null)
        {
            Debug.LogWarning("XP symbol prefab or XP panel not assigned!");
            return;
        }
        
        // Limit the number of symbols
        int symbolCount = Mathf.Min(xpAmount, maxSymbolsPerDeposit);
        
        // Start spawning
        StartCoroutine(SpawnXPSymbols(spawnPosition, symbolCount));
    }
    
    private IEnumerator SpawnXPSymbols(Vector2 spawnPosition, int count)
    {
        // Get XP panel position
        RectTransform xpRT = xpPanel.GetComponent<RectTransform>();
        if (xpRT == null)
        {
            Debug.LogWarning("XP panel has no RectTransform!");
            yield break;
        }
        
        // Get the world corners of the panel
        Vector3[] corners = new Vector3[4];
        xpRT.GetWorldCorners(corners);
        
        // Calculate panel center
        Vector2 panelCenter = new Vector2(
            (corners[0].x + corners[2].x) / 2f,
            (corners[0].y + corners[2].y) / 2f
        );
        
        // Spawn XP symbols
        for (int i = 0; i < count; i++)
        {
            // Create XP symbol
            GameObject xpSymbol = Instantiate(xpSymbolPrefab, transform);
            RectTransform symbolRT = xpSymbol.GetComponent<RectTransform>();
            
            // Apply the smaller scale
            symbolRT.localScale = xpSymbolScale;
            
            // Position near spawn point with variation
            symbolRT.position = spawnPosition + new Vector2(
                Random.Range(-20f, 20f),
                Random.Range(-20f, 20f)
            );
            
            // Start animation
            StartCoroutine(AnimateXPToPanel(xpSymbol, panelCenter));
            
            // Wait before spawning next
            yield return new WaitForSeconds(spawnDelay * 0.5f); // Faster spawn for XP
        }
    }
    
    private IEnumerator AnimateXPToPanel(GameObject xpSymbol, Vector2 targetPosition)
    {
        RectTransform symbolRT = xpSymbol.GetComponent<RectTransform>();
        if (symbolRT == null) yield break;
        
        // Store start position and initial scale
        Vector2 startPos = symbolRT.position;
        Vector3 baseScale = symbolRT.localScale;
        
        // Create multiple control points for more dynamic path
        Vector2 controlPoint1 = Vector2.Lerp(startPos, targetPosition, 0.25f);
        controlPoint1 += new Vector2(
            Random.Range(-100f, 100f),
            Random.Range(30f, 150f)
        );
        
        Vector2 controlPoint2 = Vector2.Lerp(startPos, targetPosition, 0.75f);
        controlPoint2 += new Vector2(
            Random.Range(-100f, 100f),
            Random.Range(-30f, 70f)
        );
        
        // Animation
        float elapsed = 0f;
        
        while (elapsed < xpFlyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / xpFlyDuration;
            
            // Use cubic bezier curve for more varied, swooping arcs
            Vector2 position = CubicBezier(startPos, controlPoint1, controlPoint2, targetPosition, t);
            
            symbolRT.position = position;
            
            // Rotation for more dynamic movement - vary rotation speed
            symbolRT.Rotate(0, 0, Time.deltaTime * (180f + Random.Range(-90f, 90f)));
            
            // Scale pulse for more life
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.2f;
            symbolRT.localScale = baseScale * scaleMultiplier;
            
            yield return null;
        }
        
        // Ensure we hit the target
        symbolRT.position = targetPosition;
        
        // Add XP if this is the first symbol to reach the target and XP hasn't been added yet
        if (!xpAddedThisSession && playerInventory != null && pendingXpAmount > 0)
        {
            // Store current level before adding XP
            int currentLevelBefore = playerInventory.CurrentLevel;
            
            // Add XP to player inventory
            playerInventory.AddXP(pendingXpAmount);
            Debug.Log($"*** Added {pendingXpAmount} XP to player inventory when symbol reached panel ***");
            
            // Update UI to reflect changes
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.UpdateExperienceDisplay(playerInventory.TotalExperience, true);
                
                // Check if player leveled up
                if (playerInventory.CurrentLevel > currentLevelBefore)
                {
                    // Show level up panel
                    Debug.Log($"Level up detected! Level {currentLevelBefore} → {playerInventory.CurrentLevel}");
                    
                    // Trigger level-up panel after a short delay to let XP animation finish
                    StartCoroutine(ShowLevelUpPanelAfterDelay(0.5f, playerInventory.CurrentLevel));
                }
            }
            
            // Mark as added so we don't add XP multiple times
            xpAddedThisSession = true;
        }
        
        // Play sound when first XP symbol reaches target (only once)
        if (!soundPlayed && SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("XPGained");
            soundPlayed = true;
        }
        
        // Flash the XP panel
        Image panelImage = xpPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            StartCoroutine(FlashPanel(panelImage));
        }
        
        // Track XP symbols that have reached the target
        xpSymbolsReachedTarget++;
        
        // Destroy the symbol
        Destroy(xpSymbol);
    }
    
    private IEnumerator FlashPanel(Image panelImage)
    {
        // Store original color
        Color originalColor = panelImage.color;
        Color flashColor = new Color(1f, 1f, 0.5f, originalColor.a);
        
        // Flash quickly
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Flashing effect
            panelImage.color = Color.Lerp(flashColor, originalColor, t);
            
            yield return null;
        }
        
        // Reset to original
        panelImage.color = originalColor;
    }

    // New method to play coin reward animation
    public void PlayCoinRewardEffect(GameObject missionPanel, int coinAmount)
    {
        if (coinSymbolPrefab == null || coinPanel == null || missionPanel == null)
        {
            Debug.LogError("Coin prefabs or panels not assigned!");
            return;
        }

        // Get the mission panel position
        RectTransform missionPanelRT = missionPanel.GetComponent<RectTransform>();
        if (missionPanelRT == null)
        {
            Debug.LogWarning("Mission panel has no RectTransform!");
            return;
        }

        // Get panel position in screen space
        Vector3[] corners = new Vector3[4];
        missionPanelRT.GetWorldCorners(corners);
        
        // Use the center of the panel
        Vector2 panelCenter = new Vector2(
            (corners[0].x + corners[2].x) / 2f,
            (corners[0].y + corners[2].y) / 2f
        );

        // Limit the number of coins to avoid overwhelming the screen
        int symbolCount = Mathf.Min(coinAmount, maxSymbolsPerDeposit);
        
        // Start the coroutine for spawning coins
        StartCoroutine(SpawnCoinSymbols(panelCenter, symbolCount));
    }

    private IEnumerator SpawnCoinSymbols(Vector2 spawnPosition, int count)
    {
        if (coinPanel == null)
        {
            Debug.LogWarning("Coin panel not assigned!");
            yield break;
        }

        // Get position of coin panel
        RectTransform coinRT = coinPanel.GetComponent<RectTransform>();
        if (coinRT == null)
        {
            Debug.LogWarning("Coin panel has no RectTransform!");
            yield break;
        }
        
        // Get the world corners of the panel
        Vector3[] corners = new Vector3[4];
        coinRT.GetWorldCorners(corners);
        
        // Calculate panel center
        Vector2 panelCenter = new Vector2(
            (corners[0].x + corners[2].x) / 2f,
            (corners[0].y + corners[2].y) / 2f
        );
        
        // Spawn coin symbols with delay
        for (int i = 0; i < count; i++)
        {
            // Create coin symbol
            GameObject coinSymbol = Instantiate(coinSymbolPrefab, transform);
            RectTransform symbolRT = coinSymbol.GetComponent<RectTransform>();
            
            // Set scale using the serialized field
            symbolRT.localScale = coinSymbolScale;
            
            // Position near spawn point with slight randomization
            symbolRT.position = spawnPosition + new Vector2(
                Random.Range(-20f, 20f),
                Random.Range(-20f, 20f)
            );

            // Start animation coroutine
            StartCoroutine(AnimateCoinToPanel(coinSymbol, panelCenter, coinSymbolScale));
            
            // Wait before spawning next symbol
            yield return new WaitForSeconds(spawnDelay * 0.3f);
        }
        
        // Play coin sound after all coins have been spawned
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("CoinReward");
        }
    }

    private IEnumerator AnimateCoinToPanel(GameObject coinSymbol, Vector2 targetPosition, Vector3 baseScale)
    {
        RectTransform symbolRT = coinSymbol.GetComponent<RectTransform>();
        if (symbolRT == null) yield break;
        
        // Store start position
        Vector2 startPos = symbolRT.position;
        
        // Randomize path with curve for more natural motion
        Vector2 controlPoint = Vector2.Lerp(startPos, targetPosition, 0.5f);
        controlPoint += new Vector2(Random.Range(-70f, 70f), Random.Range(30f, 100f));
        
        // Animation
        float flyTime = 0.7f; // Slightly faster than chitin
        float elapsed = 0f;
        
        while (elapsed < flyTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyTime;
            
            // Use bezier curve for arcing motion
            float oneMinusT = 1f - t;
            Vector2 position = 
                oneMinusT * oneMinusT * startPos + 
                2f * oneMinusT * t * controlPoint + 
                t * t * targetPosition;
            
            symbolRT.position = position;
            
            // Rotation for spinning coin effect
            symbolRT.Rotate(0, 0, Time.deltaTime * 360f);
            
            // Scale pulse for more dynamic animation
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.2f;
            symbolRT.localScale = baseScale * scaleMultiplier;
            
            yield return null;
        }
        
        // Ensure we hit the target
        symbolRT.position = targetPosition;
        
        // Play the pickup sound for each coin that reaches the target
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("Pickup3");
        }
        
        // Flash the coin panel
        Image panelImage = coinPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            StartCoroutine(FlashCoinPanel(panelImage));
        }
        
        // Destroy the coin
        Destroy(coinSymbol);
    }

    private IEnumerator FlashCoinPanel(Image panelImage)
    {
        // Store original color
        Color originalColor = panelImage.color;
        Color flashColor = new Color(1f, 0.9f, 0.2f, originalColor.a); // Golden flash for coins
        
        // Flash quickly
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Flashing effect
            panelImage.color = Color.Lerp(flashColor, originalColor, t);
            
            yield return null;
        }
        
        // Reset to original
        panelImage.color = originalColor;
    }

    // Helper method for cubic bezier curves - add this to your class
    private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        
        Vector2 p = uuu * p0; // (1-t)³ * P0
        p += 3f * uu * t * p1; // 3(1-t)² * t * P1
        p += 3f * u * tt * p2; // 3(1-t) * t² * P2
        p += ttt * p3; // t³ * P3
        
        return p;
    }

    // Add this coroutine to UIVisualEffectController.cs
    private IEnumerator ShowLevelUpPanelAfterDelay(float delay, int newLevel)
    {
        yield return new WaitForSeconds(delay);
        
        // Find level up panel helper
        LevelUpPanelHelper levelUpHelper = FindObjectOfType<LevelUpPanelHelper>();
        if (levelUpHelper != null)
        {
            // Call the ShowUpperPanel method first
            levelUpHelper.ShowUpperPanel();
            
            // Call the SetLevel method to ensure appropriate rewards are shown
            // This is likely what's missing - setting the level before showing rewards
            if (newLevel % 5 == 0)
            {
                // Every 5th level - show attribute points reward
                Instantiate(levelUpHelper.attributePointRewardPrefab, levelUpHelper.LowerPanelRewards.transform);
            }
            
            if (newLevel % 3 == 0)
            {
                // Every 3rd level - show chitin capacity reward
                Instantiate(levelUpHelper.chitinCapacityRewardPrefab, levelUpHelper.LowerPanelRewards.transform);
            }
            
            // Log the level up
            Debug.Log($"Showing level up panel for level {newLevel} with appropriate rewards");
            
            // Play the level up sound effect
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound("LevelUp");
            }
        }
        else
        {
            Debug.LogWarning("LevelUpPanelHelper not found - can't show level up panel");
            
            // Fallback notification
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowInformText($"Level Up! You are now level {newLevel}!", 3f);
            }
        }
    }
}
