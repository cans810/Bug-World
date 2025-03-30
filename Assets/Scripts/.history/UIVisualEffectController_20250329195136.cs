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

    // Add this test function to directly trigger the effect
    public void TestChitinEffect()
    {
        Debug.Log("======= TESTING CHITIN EFFECT =======");
        
        // Check if prefabs are assigned
        if (chitinSymbolPrefab == null)
        {
            Debug.LogError("CRITICAL ERROR: Chitin symbol prefab is not assigned in the Inspector!");
            
            // Create a primitive cube as fallback for testing
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(0, 0, 0);
            cube.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            cube.GetComponent<Renderer>().material.color = Color.green;
            Debug.Log("Created a test cube since prefab is missing");
            
            return;
        }
        
        // Test parameters
        int testAmount = 5;
        int testXP = 10;
        
        // Try direct instantiation first
        GameObject directTest = Instantiate(chitinSymbolPrefab, transform);
        directTest.name = "DirectTestChitin";
        directTest.transform.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        directTest.SetActive(true);
        
        Debug.Log($"Directly instantiated a test chitin symbol at screen center");
        
        // Now call the normal effect
        PlayChitinDepositEffect(testAmount, testXP);
    }

    // Modify PlayChitinDepositEffect to ensure it catches errors
    public void PlayChitinDepositEffect(int chitinAmount, int xpAmount)
    {
        try
        {
            Debug.Log($"PlayChitinDepositEffect called: chitinAmount={chitinAmount}, xpAmount={xpAmount}");
            
            if (chitinSymbolPrefab == null)
            {
                Debug.LogError("CRITICAL ERROR: Chitin symbol prefab is null!");
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
            
            // Special test case - always ensure at least 3 symbols for testing
            if (symbolCount == 0 && chitinAmount > 0)
            {
                symbolCount = 3;
                Debug.Log("Using minimum 3 symbols for testing");
            }
            
            // Start the coroutine for spawning symbols
            StartCoroutine(SpawnChitinSymbols(symbolCount, xpAmount));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in PlayChitinDepositEffect: {e.Message}\n{e.StackTrace}");
        }
    }

    // Modify SpawnChitinSymbols to be more error-resistant
    private IEnumerator SpawnChitinSymbols(int count, int xpAmount)
    {
        try
        {
            Debug.Log($"SpawnChitinSymbols started: count={count}");
            
            // Use a default screen position if chitinPanel is missing
            Vector2 panelCenter;
            
            if (chitinPanel == null)
            {
                Debug.LogWarning("Chitin panel not assigned! Using screen center instead.");
                panelCenter = new Vector2(Screen.width / 2f, Screen.height - 100);
            }
            else
            {
                // Get position of chitin panel
                RectTransform chitinRT = chitinPanel.GetComponent<RectTransform>();
                if (chitinRT == null)
                {
                    Debug.LogWarning("Chitin panel has no RectTransform! Using screen center instead.");
                    panelCenter = new Vector2(Screen.width / 2f, Screen.height - 100);
                }
                else
                {
                    // Get the world corners of the panel
                    Vector3[] corners = new Vector3[4];
                    chitinRT.GetWorldCorners(corners);
                    Debug.Log($"Chitin panel corners: {corners[0]}, {corners[1]}, {corners[2]}, {corners[3]}");
                    
                    // Calculate panel center
                    panelCenter = new Vector2(
                        (corners[0].x + corners[2].x) / 2f,
                        (corners[0].y + corners[2].y) / 2f
                    );
                }
            }

            // Calculate center of screen for target position
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Debug.Log($"Screen center: {screenCenter}, Panel center: {panelCenter}");

            // Spawn chitin symbols with delay
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForSeconds(0.1f); // Small initial delay
                
                Debug.Log($"Creating chitin symbol {i+1}/{count}");
                
                // Create chitin symbol - use the safest approach
                GameObject chitinSymbol = null;
                
                try
                {
                    chitinSymbol = Instantiate(chitinSymbolPrefab);
                    chitinSymbol.transform.SetParent(transform, false);
                    
                    // Ensure the symbol is active and visible
                    chitinSymbol.SetActive(true);
                    
                    // Set a name for easier debugging
                    chitinSymbol.name = $"ChitinSymbol_{i}";
                    
                    Debug.Log($"Successfully instantiated {chitinSymbol.name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to instantiate chitin symbol: {e.Message}");
                    continue;
                }
                
                // Skip if instantiation failed
                if (chitinSymbol == null)
                {
                    Debug.LogError("Failed to instantiate chitin symbol - resulted in null object");
                    continue;
                }
                
                // Set position directly based on screen coordinates
                RectTransform symbolRT = chitinSymbol.GetComponent<RectTransform>();
                if (symbolRT == null)
                {
                    Debug.LogError("Chitin symbol prefab doesn't have a RectTransform component!");
                    
                    // Last resort - try to add one
                    symbolRT = chitinSymbol.AddComponent<RectTransform>();
                    if (symbolRT == null)
                    {
                        Destroy(chitinSymbol);
                        continue;
                    }
                }
                
                // Position near chitin panel with slight randomization
                Vector2 spawnPos = panelCenter + new Vector2(
                    Random.Range(-50f, 50f),
                    Random.Range(-50f, 50f)
                );
                
                // Set position directly
                symbolRT.position = spawnPos;
                
                // Make sure the scale is appropriate
                symbolRT.localScale = Vector3.one;
                
                Debug.Log($"Positioned chitin symbol at {spawnPos}");
                
                // Try to find any renderer component and make it visible
                Image image = chitinSymbol.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
                    Debug.Log("Found and set Image component to fully visible");
                }
                
                SpriteRenderer renderer = chitinSymbol.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 1f);
                    renderer.sortingOrder = 1000;
                    Debug.Log("Found and set SpriteRenderer to fully visible with high sorting order");
                }
                
                // Start a simplified animation directly in this coroutine for testing
                StartCoroutine(SimpleMoveToCenter(chitinSymbol, screenCenter));
                
                // Wait before spawning next symbol
                yield return new WaitForSeconds(spawnDelay);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in SpawnChitinSymbols: {e.Message}\n{e.StackTrace}");
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

    // Add a simplified movement animation for testing
    private IEnumerator SimpleMoveToCenter(GameObject symbol, Vector2 targetPos)
    {
        if (symbol == null) yield break;
        
        RectTransform rt = symbol.GetComponent<RectTransform>();
        if (rt == null) yield break;
        
        Vector2 startPos = rt.position;
        float duration = 1.0f;
        float elapsed = 0f;
        
        while (elapsed < duration && symbol != null && rt != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Simple linear movement for testing
            rt.position = Vector2.Lerp(startPos, targetPos, t);
            
            yield return null;
        }
        
        // Destroy after reaching the center
        if (symbol != null)
        {
            Debug.Log($"Symbol {symbol.name} reached center, destroying it");
            Destroy(symbol, 0.2f);
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
