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

    // Reference to the Canvas
    private Canvas parentCanvas;

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
        Debug.Log($"PlayChitinDepositEffect called: chitinAmount={chitinAmount}, xpAmount={xpAmount}");
        
        if (chitinSymbolPrefab == null)
        {
            Debug.LogError("Chitin symbol prefab is null!");
            return;
        }
        
        if (xpSymbolPrefab == null)
        {
            Debug.LogError("XP symbol prefab is null!");
            return;
        }

        // Limit the number of symbols to avoid overwhelming the screen
        int symbolCount = Mathf.Min(chitinAmount, maxSymbolsPerDeposit);
        Debug.Log($"Will spawn {symbolCount} chitin symbols");
        
        // Start the coroutine for spawning symbols
        StartCoroutine(SpawnChitinSymbols(symbolCount, xpAmount));
    }

    private IEnumerator SpawnChitinSymbols(int count, int xpAmount)
    {
        Debug.Log($"SpawnChitinSymbols started: count={count}");
        
        if (chitinPanel == null)
        {
            Debug.LogWarning("Chitin panel not assigned!");
            yield break;
        }

        // Calculate center of screen
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Debug.Log($"Screen center: {screenCenter}");
        
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
        Debug.Log($"Chitin panel corners: {corners[0]}, {corners[1]}, {corners[2]}, {corners[3]}");
        
        // Calculate panel center
        Vector2 panelCenter = new Vector2(
            (corners[0].x + corners[2].x) / 2f,
            (corners[0].y + corners[2].y) / 2f
        );
        Debug.Log($"Panel center: {panelCenter}");

        // Spawn chitin symbols with delay
        for (int i = 0; i < count; i++)
        {
            Debug.Log($"Creating chitin symbol {i+1}/{count}");
            
            // Create chitin symbol
            GameObject chitinSymbol = Instantiate(chitinSymbolPrefab, transform);
            
            // Ensure the symbol is active and visible
            chitinSymbol.SetActive(true);
            
            // Set a name for easier debugging
            chitinSymbol.name = $"ChitinSymbol_{i}";
            
            RectTransform symbolRT = chitinSymbol.GetComponent<RectTransform>();
            if (symbolRT == null)
            {
                Debug.LogError("Chitin symbol prefab doesn't have a RectTransform component!");
                Destroy(chitinSymbol);
                continue;
            }
            
            // Make sure the symbol has proper scale
            symbolRT.localScale = Vector3.one;
            
            // Position near chitin panel with slight randomization
            Vector2 spawnPos = panelCenter + new Vector2(
                Random.Range(-20f, 20f),
                Random.Range(-20f, 20f)
            );
            symbolRT.position = spawnPos;
            Debug.Log($"Positioned chitin symbol at {spawnPos}");
            
            // Ensure any Image component is visible
            Image image = chitinSymbol.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = 1f;
                image.color = color;
                Debug.Log("Found and reset Image component");
            }
            
            // Or if using a SpriteRenderer
            SpriteRenderer renderer = chitinSymbol.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 1f;
                renderer.color = color;
                renderer.sortingOrder = 1000; // Ensure it's drawn on top
                Debug.Log("Found and reset SpriteRenderer component");
            }

            // Start animation coroutine
            StartCoroutine(AnimateChitinToCenter(chitinSymbol, screenCenter, i));
            
            // Wait before spawning next symbol
            yield return new WaitForSeconds(spawnDelay);
        }
        
        Debug.Log("Finished spawning all chitin symbols");
        
        // Wait until all chitin symbols have reached center, then play XP effect
        float totalDelay = count * spawnDelay + flyDuration + 0.1f;
        Debug.Log($"Waiting {totalDelay} seconds before spawning XP symbols");
        yield return new WaitForSeconds(totalDelay);
        
        // Play XP gain effect
        if (xpAmount > 0)
        {
            Debug.Log($"Now playing XP effect with {xpAmount} XP");
            PlayXPGainEffect(screenCenter, xpAmount);
        }
    }

    private IEnumerator AnimateChitinToCenter(GameObject chitinSymbol, Vector2 screenCenter, int index)
    {
        RectTransform symbolRT = chitinSymbol.GetComponent<RectTransform>();
        if (symbolRT == null) yield break;
        
        // Store start position
        Vector2 startPos = symbolRT.position;
        
        // Add slight offset to screen center based on index to avoid all symbols overlapping
        float angle = (index * 36f) % 360f; // Distribute in a circle
        float radius = 20f; // Small radius around center
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
            
            // Optional: Scale up slightly during flight
            float scale = Mathf.Lerp(0.8f, 1.2f, Mathf.Sin(t * Mathf.PI));
            symbolRT.localScale = new Vector3(scale, scale, 1f);
            
            yield return null;
        }
        
        // Ensure final position is reached
        symbolRT.position = targetPos;
        
        // Optional: Small pop animation at the end
        StartCoroutine(PopAnimation(chitinSymbol));
        
        // Destroy after a short delay
        Destroy(chitinSymbol, 0.2f);
    }
    
    private IEnumerator PopAnimation(GameObject symbol)
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
            
            // Pop curve: quick scale up then down
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.4f;
            rt.localScale = new Vector3(scale, scale, 1f);
            
            yield return null;
        }
        
        // Reset scale
        rt.localScale = Vector3.one;
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
        
        // Play XP sound
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("XPGained");
        }
    }
    
    private IEnumerator AnimateXPToPanel(GameObject xpSymbol, Vector2 targetPosition)
    {
        RectTransform symbolRT = xpSymbol.GetComponent<RectTransform>();
        if (symbolRT == null) yield break;
        
        // Store start position
        Vector2 startPos = symbolRT.position;
        
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
