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

    // Reference to the Canvas
    private Canvas parentCanvas;

    // Track how many XP symbols have reached the target
    private int xpSymbolsReachedTarget = 0;
    private int totalXpSymbols = 0;
    private bool soundPlayed = false;

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
    public void PlayChitinDepositEffect(int chitinAmount, int xpAmount)
    {
        if (chitinSymbolPrefab == null || xpSymbolPrefab == null)
        {
            Debug.LogError("Symbol prefabs not assigned!");
            return;
        }

        // Reset tracking variables
        xpSymbolsReachedTarget = 0;
        totalXpSymbols = 0;
        soundPlayed = false;
        
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
        
        // Animation
        float elapsed = 0f;
        
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            
            // Use animation curve for smoother motion
            float curvedT = flyCurve.Evaluate(t);
            
            // Lerp position
            symbolRT.position = Vector2.Lerp(startPos, targetPos, curvedT);
            
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
        
        // Randomize path slightly
        Vector2 controlPoint = Vector2.Lerp(startPos, targetPosition, 0.5f);
        controlPoint += new Vector2(Random.Range(-50f, 50f), Random.Range(20f, 80f));
        
        // Animation
        float elapsed = 0f;
        
        while (elapsed < xpFlyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / xpFlyDuration;
            
            // Use bezier curve for arcing motion
            float oneMinusT = 1f - t;
            Vector2 position = 
                oneMinusT * oneMinusT * startPos + 
                2f * oneMinusT * t * controlPoint + 
                t * t * targetPosition;
            
            symbolRT.position = position;
            
            // Rotation for more dynamic movement
            symbolRT.Rotate(0, 0, Time.deltaTime * 180f);
            
            yield return null;
        }
        
        // Ensure we hit the target
        symbolRT.position = targetPosition;
        
        // Flash the XP panel
        Image panelImage = xpPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            StartCoroutine(FlashPanel(panelImage));
        }
        
        // Track XP symbols that have reached the target
        xpSymbolsReachedTarget++;
        
        SoundEffectManager.Instance.PlaySound("XPGained");
        
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
}
