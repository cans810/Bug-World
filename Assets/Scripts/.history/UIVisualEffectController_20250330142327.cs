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
    [SerializeField] private float flyDuration = 0.6f;
    [SerializeField] private float xpFlyDuration = 0.5f;
    [SerializeField] private float symbolSpacing = 90f;
    [SerializeField] private int maxSymbolsPerDeposit = 50;
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

    // Add this at the top of the class where your other private variables are
    private bool isAnimationInProgress = false;
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    // Add this field near the top of the class
    [SerializeField] private bool keepAnimationsWhenLeavingBase = true;

    // Add this reference to maintain a persistent connection
    private static UIVisualEffectController _instance;

    private void Awake()
    {
        // Get the canvas reference
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogWarning("UIVisualEffectController should be a child of a Canvas!");
            parentCanvas = FindObjectOfType<Canvas>();
        }
        
        // Set the instance reference to ensure persistence
        _instance = this;
    }

    // Called when chitin is deposited - optimize to start animations faster
    public void PlayChitinDepositEffect(int chitinAmount, int xpAmount, PlayerInventory inventory)
    {
        if (chitinSymbolPrefab == null || xpSymbolPrefab == null)
        {
            Debug.LogError("Symbol prefabs not assigned!");
            return;
        }

        // Store the inventory reference and prevent nulls later
        if (inventory != null)
        {
            // Immediately clear any existing animations to prevent conflicts
            StopAllChitinAnimations();
            
            // Create a static reference to ensure animations continue after leaving the base
            playerInventory = inventory;
            pendingXpAmount = xpAmount;
            
            // Set flag that animation is in progress
            isAnimationInProgress = true;
            
            // Reset tracking variables
            xpSymbolsReachedTarget = 0;
            totalXpSymbols = 0;
            soundPlayed = false;
            xpAddedThisSession = false;
            
            // Limit the number of symbols to avoid overwhelming the screen
            int symbolCount = Mathf.Min(chitinAmount, maxSymbolsPerDeposit);
            
            Debug.Log($"Starting chitin deposit animation for {chitinAmount} chitin and {xpAmount} XP");
            
            // Start the coroutine immediately - do this FIRST before any other coroutines
            Coroutine spawnCoroutine = StartCoroutine(SpawnChitinSymbolsImmediate(symbolCount, xpAmount));
            activeCoroutines.Add(spawnCoroutine);
            
            // Delay the safety check to avoid interference
            StartCoroutine(DelayedSafetyCheck());
        }
        else
        {
            Debug.LogError("Cannot play chitin deposit effect: inventory reference is null");
        }
    }

    // Optimized version that starts spawning immediately with multi-row support
    private IEnumerator SpawnChitinSymbolsImmediate(int count, int xpAmount)
    {
        if (chitinPanel == null)
        {
            Debug.LogWarning("Chitin panel not assigned!");
            yield break;
        }

        // Pre-calculate common values to avoid delays
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

        // Calculate how many symbols can fit in a row
        // Leave some margin on both sides of the screen
        float screenWidth = Screen.width - 200f; // 100px margin on each side
        int symbolsPerRow = Mathf.Max(1, Mathf.FloorToInt(screenWidth / symbolSpacing));
        
        // Calculate number of rows needed
        int numRows = Mathf.CeilToInt((float)count / symbolsPerRow);
        
        // Vertical spacing between rows
        float rowSpacing = 80f;
        
        // Create a list to store references to the chitin symbols
        List<GameObject> chitinSymbols = new List<GameObject>(count);
        
        // Create all symbols at once and start animations immediately
        for (int i = 0; i < count; i++)
        {
            // Calculate row and position within row
            int row = i / symbolsPerRow;
            int posInRow = i % symbolsPerRow;
            
            // Calculate symbols in this row (last row might have fewer)
            int symbolsInThisRow = (row == numRows - 1) ? 
                count - (row * symbolsPerRow) : symbolsPerRow;
            
            // Create chitin symbol 
            GameObject chitinSymbol = Instantiate(chitinSymbolPrefab, transform);
            RectTransform symbolRT = chitinSymbol.GetComponent<RectTransform>();
            
            // Apply scale immediately
            symbolRT.localScale = chitinSymbolScale;
            
            // Position near chitin panel with slight randomization
            symbolRT.position = panelCenter + new Vector2(
                Random.Range(-20f, 20f),
                Random.Range(-20f, 20f)
            );

            // Add to list of symbols
            chitinSymbols.Add(chitinSymbol);
            
            // Calculate the line width for this row based on number of symbols in it
            float lineWidth = symbolSpacing * symbolsInThisRow;
            
            // Calculate starting X position for this row (centered)
            float startX = screenCenter.x - (lineWidth / 2) + (symbolSpacing / 2);
            
            // Calculate target position
            Vector2 targetPos = new Vector2(
                startX + (posInRow * symbolSpacing),
                screenCenter.y + 150f - (row * rowSpacing) // Lower rows are positioned below
            );
            
            // Start animation immediately
            StartCoroutine(AnimateChitinToLineup(chitinSymbol, targetPos, i, panelCenter));
            
            // Minimal delay between spawns
            yield return new WaitForSeconds(0.03f);
        }
        
        // Wait briefly for chitins to get moving before starting XP transformation
        yield return new WaitForSeconds(0.1f);
        
        // Start transforming chitins to XP symbols sequentially
        StartCoroutine(TransformChitinsToXP(chitinSymbols, xpAmount));
    }

    // Simplified version that avoids redundant calculations
    private IEnumerator AnimateChitinToLineup(GameObject chitinSymbol, Vector2 targetPos, int index, Vector2 startPos)
    {
        RectTransform symbolRT = chitinSymbol.GetComponent<RectTransform>();
        if (symbolRT == null) yield break;
        
        // Store position and initial scale
        Vector3 baseScale = symbolRT.localScale;
        
        // Create control points for bezier curve - make each path unique but faster
        Vector2 controlPoint1 = Vector2.Lerp(startPos, targetPos, 0.3f);
        controlPoint1 += new Vector2(
            Random.Range(-60f, 60f), 
            Random.Range(-30f, 60f)
        );
        
        Vector2 controlPoint2 = Vector2.Lerp(startPos, targetPos, 0.7f);
        controlPoint2 += new Vector2(
            Random.Range(-60f, 60f), 
            Random.Range(-30f, 60f)
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
    }

    private IEnumerator TransformChitinsToXP(List<GameObject> chitinSymbols, int totalXPAmount)
    {
        // Get position of XP panel
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
        
        // Calculate XP per chitin
        int chitinCount = chitinSymbols.Count;
        int xpPerChitin = Mathf.CeilToInt((float)totalXPAmount / chitinCount);
        
        // Initialize XP tracking for safety
        totalXpSymbols = Mathf.Min(totalXPAmount, maxSymbolsPerDeposit);

        // Transform each chitin to XP, one by one but with minimal delay
        for (int i = 0; i < chitinSymbols.Count; i++)
        {
            GameObject chitinSymbol = chitinSymbols[i];
            if (chitinSymbol == null) continue;
            
            Vector2 chitinPosition = chitinSymbol.GetComponent<RectTransform>().position;
            
            // XP to spawn from this chitin
            int xpToSpawn = (i < chitinCount - 1) ? 
                            xpPerChitin : 
                            totalXPAmount - (xpPerChitin * (chitinCount - 1)); // Last chitin gets remaining XP
            
            // Spawn the XP symbol at the chitin position
            StartCoroutine(TransformChitinToXPWithEffect(chitinSymbol, xpToSpawn, panelCenter));
            
            // Minimal delay between transforming the next chitin
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator TransformChitinToXPWithEffect(GameObject chitinSymbol, int xpToSpawn, Vector2 targetPosition)
    {
        if (chitinSymbol == null) yield break;

        // Get chitin position
        RectTransform chitinRT = chitinSymbol.GetComponent<RectTransform>();
        Vector2 chitinPosition = chitinRT.position;
        
        // Flash effect on chitin before transformation
        Image chitinImage = chitinSymbol.GetComponent<Image>();
        if (chitinImage != null)
        {
            Color originalColor = chitinImage.color;
            Color flashColor = Color.white;
            
            float flashDuration = 0.2f;
            float elapsed = 0f;
            
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                chitinImage.color = Color.Lerp(originalColor, flashColor, elapsed / flashDuration);
                yield return null;
            }
        }
        
        // Play the transformation sound effect
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("Pickup3");
        }
        
        // Destroy the chitin
        Destroy(chitinSymbol);
        
        // Limit XP symbols for performance
        int symbolsToSpawn = Mathf.Min(xpToSpawn, 5);  // Limit max XP symbols per chitin
        
        // Spawn XP symbols in a small burst with almost no delay
        for (int i = 0; i < symbolsToSpawn; i++)
        {
            // Create XP symbol at chitin position
            GameObject xpSymbol = Instantiate(xpSymbolPrefab, transform);
            RectTransform xpRT = xpSymbol.GetComponent<RectTransform>();
            
            // Apply scale
            xpRT.localScale = xpSymbolScale;
            
            // Position with slight variation from chitin position
            xpRT.position = chitinPosition + new Vector2(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f)
            );
            
            // Animate to XP panel
            StartCoroutine(AnimateXPToPanel(xpSymbol, targetPosition));
            
            // Extremely minimal delay between each XP symbol
            yield return new WaitForSeconds(0.02f);
        }
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

        // Calculate XP amount for this symbol
        float xpPerSymbol = 0;
        if (totalXpSymbols > 0 && pendingXpAmount > 0)
        {
            xpPerSymbol = (float)pendingXpAmount / totalXpSymbols;
        }
        
        // Add XP incrementally as each symbol reaches the target
        if (playerInventory != null && pendingXpAmount > 0)
        {
            // Add the XP portion
            int xpToAdd = Mathf.CeilToInt(xpPerSymbol);
            
            // Make sure we don't add more than the total pending amount
            xpToAdd = Mathf.Min(xpToAdd, pendingXpAmount);
            
            // Add the XP to player inventory
            playerInventory.AddXP(xpToAdd);
            
            // Reduce pending amount
            pendingXpAmount -= xpToAdd;
            
            Debug.Log($"Added {xpToAdd} XP from symbol. Remaining pending XP: {pendingXpAmount}");
            
            // Update UI to reflect changes
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.UpdateExperienceDisplay(playerInventory.TotalExperience, true);
            }
            
            // Mark session as having added XP
            xpAddedThisSession = true;
        }
        
        // Play sound for EACH XP symbol that reaches the target
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("XPGained");
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

    // Add a method to ensure animations complete regardless of player location
    public void EnsureChitinDepositCompletes()
    {
        // Called from BaseInteraction when player exits the trigger
        Debug.Log("EnsureChitinDepositCompletes called - ensuring animations will continue");
        
        // Set the flag to keep animations when leaving base
        keepAnimationsWhenLeavingBase = true;
        
        // Create a delayed safety check
        StartCoroutine(DelayedDepositCompletionCheck());
    }

    // Update DelayedDepositCompletionCheck to prevent premature XP symbols
    private IEnumerator DelayedDepositCompletionCheck()
    {
        // Wait longer for any potential state changes (giving animations more time)
        yield return new WaitForSeconds(2f);
        
        // Check if animations are still needed and we haven't created any XP symbols yet
        if (isAnimationInProgress && pendingXpAmount > 0 && playerInventory != null && totalXpSymbols == 0)
        {
            Debug.Log("Delayed check found incomplete animations - ensuring they finish");
            
            // If there are no active symbols, create a single one to ensure XP is granted
            if (xpSymbolsReachedTarget == 0)
            {
                Debug.Log("No symbols found, creating a single XP symbol to ensure completion");
                
                // Get position of XP panel
                Vector2 panelPosition = xpPanel.GetComponent<RectTransform>().position;
                
                // Create a single XP symbol
                GameObject xpSymbol = Instantiate(xpSymbolPrefab, transform);
                
                // Set it near the target position already
                xpSymbol.GetComponent<RectTransform>().position = panelPosition + new Vector2(Random.Range(-50, 50), Random.Range(-50, 50));
                
                // Send it to complete the animation
                StartCoroutine(AnimateXPToPanel(xpSymbol, panelPosition));
            }
        }
        
        // Continue checking for 5 seconds total to ensure completion
        yield return new WaitForSeconds(3f);
        
        // Final safety check only if we still have pending XP
        if (pendingXpAmount > 0 && playerInventory != null)
        {
            Debug.Log("Final safety check: animations didn't complete, adding remaining XP directly: " + pendingXpAmount);
            playerInventory.AddXP(pendingXpAmount);
            pendingXpAmount = 0;
            xpAddedThisSession = true;
        }
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

    // Add a new method to delay the safety mechanism
    private IEnumerator DelayedSafetyCheck()
    {
        // Wait to give the main animation time to start properly
        yield return new WaitForSeconds(1.5f);
        
        // Only run the safety check if animation is still in progress
        if (isAnimationInProgress && pendingXpAmount > 0)
        {
            StartCoroutine(EnsureXPIsAdded());
        }
    }

    // Modify EnsureXPIsAdded method to be more robust
    private IEnumerator EnsureXPIsAdded()
    {
        // Wait for a reasonably long time (longer than the expected animation duration)
        float timeLimit = 5f;
        float elapsed = 0f;
        
        while (elapsed < timeLimit && pendingXpAmount > 0) // Check pendingXpAmount instead of xpAddedThisSession
        {
            elapsed += Time.deltaTime;
            
            // If XP hasn't been fully added yet, check if we should add it now
            if (pendingXpAmount > 0 && playerInventory != null)
            {
                // If half the time has passed and no XP has been added yet,
                // or if the animation was interrupted (check if any symbols were spawned)
                if ((elapsed > timeLimit/2 && totalXpSymbols == 0) || 
                    (totalXpSymbols > 0 && xpSymbolsReachedTarget == 0 && elapsed > 1.0f))
                {
                    Debug.Log("Animation may have been interrupted - ensuring remaining XP is added via safety coroutine");
                    playerInventory.AddXP(pendingXpAmount);
                    
                    // Update UI
                    UIHelper uiHelper = FindObjectOfType<UIHelper>();
                    if (uiHelper != null)
                    {
                        uiHelper.UpdateExperienceDisplay(playerInventory.TotalExperience, true);
                    }
                    
                    // Set remaining pending XP to zero
                    pendingXpAmount = 0;
                    xpAddedThisSession = true;
                }
            }
            
            yield return null;
        }
        
        // Final check - if any XP still hasn't been added, add it now
        if (pendingXpAmount > 0 && playerInventory != null)
        {
            Debug.Log("Final safety check - adding remaining XP: " + pendingXpAmount);
            playerInventory.AddXP(pendingXpAmount);
                    
            // Update UI
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.UpdateExperienceDisplay(playerInventory.TotalExperience, true);
            }
            
            // Set remaining pending XP to zero
            pendingXpAmount = 0;
            xpAddedThisSession = true;
        }
        
        // Animation is no longer in progress
        isAnimationInProgress = false;
    }

    // Add this method to stop all existing chitin animations
    private void StopAllChitinAnimations()
    {
        foreach (Coroutine coroutine in activeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        activeCoroutines.Clear();
    }
}
