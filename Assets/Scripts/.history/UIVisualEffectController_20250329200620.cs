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
        
        // Force a larger scale to make it more visible
        symbolRT.localScale = new Vector3(2.0f, 2.0f, 1f);
        
        // Store start position
        Vector2 startPos = symbolRT.position;
        
        // Add slight offset to screen center based on index to avoid all symbols overlapping
        float angle = (index * 36f) % 360f; // Distribute in a circle
        float radius = 20f; // Small radius around center
        Vector2 targetPos = screenCenter + new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
            Mathf.Sin(angle * Mathf.Deg2Rad) * radius
        );
        
        // Add debug text component to see it in the scene
        if (!chitinSymbol.GetComponent<TextMesh>())
        {
            TextMesh debugText = chitinSymbol.AddComponent<TextMesh>();
            debugText.text = "Chitin";
            debugText.fontSize = 14;
            debugText.color = Color.red;
        }
        
        // SLOW DOWN ANIMATION FOR DEBUGGING - increase this to see symbols for longer
        float slowedDuration = flyDuration * 3.0f; // Triple the duration
        float elapsed = 0f;
        
        Debug.Log($"Starting chitin animation from {startPos} to {targetPos}");
        
        // Animation loop
        while (elapsed < slowedDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slowedDuration;
            float curvedT = flyCurve.Evaluate(t);
            
            // Lerp position
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, curvedT);
            symbolRT.position = currentPos;
            
            Debug.Log($"Chitin {index} position: {currentPos}, t={t:F2}");
            
            // Make it pulse to be more visible
            float pulse = 1.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 8);
            symbolRT.localScale = new Vector3(pulse, pulse, 1f);
            
            yield return null;
        }
        
        // Ensure final position is reached
        symbolRT.position = targetPos;
        Debug.Log($"Chitin {index} reached target position: {targetPos}");
        
        // Hold at the target for a moment to see it clearly
        yield return new WaitForSeconds(1.0f);
        
        // Optional: Small pop animation at the end
        StartCoroutine(PopAnimation(chitinSymbol));
        
        // Destroy after a longer delay to see it clearly
        Destroy(chitinSymbol, 2.0f);
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

    // Modify the prefab setup method to check for common issues
    private void SetupSymbolPrefab(GameObject symbol, string debugName)
    {
        // Add this at the start of SpawnChitinSymbols for each symbol
        
        // Check for Image component
        Image img = symbol.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(1f, 0.5f, 0f, 1f); // Bright orange
            Debug.Log($"{debugName}: Set Image color to bright orange");
        }
        else
        {
            // Try to add an Image component if none exists
            img = symbol.AddComponent<Image>();
            img.color = new Color(1f, 0.5f, 0f, 1f);
            Debug.Log($"{debugName}: Added Image component with bright orange color");
        }
        
        // Check for Sprite Renderer
        SpriteRenderer sr = symbol.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1f, 0.5f, 0f, 1f); // Bright orange
            sr.sortingOrder = 9999; // Very high to be on top
            Debug.Log($"{debugName}: Set SpriteRenderer to bright orange and highest order");
        }
        
        // Ensure it's active
        symbol.SetActive(true);
        
        // Force layout update if needed
        LayoutRebuilder.ForceRebuildLayoutImmediate(symbol.transform as RectTransform);
    }

    // Add this new method for a simpler, more reliable effect
    public void ShowSimpleChitinEffect(int chitinCount, int xpAmount)
    {
        Debug.Log($"Simple chitin effect: {chitinCount} chitin, {xpAmount} XP");
        
        // Get the center of the screen
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        // Get chitin panel position
        Vector2 startPos = Vector2.zero;
        if (chitinPanel != null)
        {
            RectTransform panelRT = chitinPanel.GetComponent<RectTransform>();
            if (panelRT != null)
            {
                Vector3[] corners = new Vector3[4];
                panelRT.GetWorldCorners(corners);
                startPos = new Vector2(
                    (corners[0].x + corners[2].x) / 2f,
                    (corners[0].y + corners[2].y) / 2f
                );
            }
        }
        else
        {
            // Fallback position if no panel
            startPos = new Vector2(100, 100);
        }
        
        // Create the container for our effects
        GameObject container = new GameObject("ChitinEffectContainer");
        container.transform.SetParent(transform, false);
        
        // Start coroutine for sequential creation
        StartCoroutine(CreateChitinSymbolsSequentially(
            container, 
            startPos, 
            screenCenter, 
            Mathf.Min(chitinCount, maxSymbolsPerDeposit), 
            xpAmount
        ));
    }

    private IEnumerator CreateChitinSymbolsSequentially(
        GameObject container, 
        Vector2 startPos, 
        Vector2 centerPos, 
        int count, 
        int xpAmount)
    {
        for (int i = 0; i < count; i++)
        {
            // Create a simple circular image
            GameObject symbol = new GameObject($"SimpleChitin_{i}");
            symbol.transform.SetParent(container.transform, false);
            
            // Add image component
            Image img = symbol.AddComponent<Image>();
            img.color = new Color(1f, 0.7f, 0.2f, 1f); // Amber color for chitin
            
            // Make it circular
            img.sprite = Resources.Load<Sprite>("UI/CircleSprite");
            if (img.sprite == null)
            {
                // If no sprite is found, use a basic shape
                Debug.Log("No sprite found, using default white texture");
            }
            
            // Get RectTransform and set size
            RectTransform rt = symbol.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(30, 30); // 30x30 pixel size
            
            // Position with random offset near start position
            Vector2 randomOffset = new Vector2(
                Random.Range(-20f, 20f),
                Random.Range(-20f, 20f)
            );
            rt.position = startPos + randomOffset;
            
            // Add animation
            StartCoroutine(AnimateBasicSymbol(rt, startPos + randomOffset, centerPos, i));
            
            // Wait before next
            yield return new WaitForSeconds(0.1f);
        }
        
        // Wait until chitin animations should be complete
        yield return new WaitForSeconds(1.5f);
        
        // Now show XP symbols
        if (xpAmount > 0)
        {
            PlayXPGainEffect(centerPos, xpAmount);
        }
        
        // Clean up the container after all effects are done
        Destroy(container, 5f);
    }

    private IEnumerator AnimateBasicSymbol(RectTransform rt, Vector2 startPos, Vector2 endPos, int index)
    {
        // Calculate slight variation in target position
        float angle = (index * 36f) % 360f;
        float radius = 40f;
        Vector2 targetPos = endPos + new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
            Mathf.Sin(angle * Mathf.Deg2Rad) * radius
        );
        
        float duration = 1.0f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Add a slight arc
            float yOffset = Mathf.Sin(t * Mathf.PI) * 50f;
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, t);
            currentPos.y += yOffset;
            
            // Apply position
            rt.position = currentPos;
            
            // Pulse scale for visibility
            float scale = 1f + 0.2f * Mathf.Sin(t * Mathf.PI * 4f);
            rt.localScale = new Vector3(scale, scale, 1f);
            
            yield return null;
        }
        
        // Make sure we reach the exact target
        rt.position = targetPos;
        
        // Wait a moment at the destination
        yield return new WaitForSeconds(0.2f);
        
        // Optional: add a small pop effect at the end
        StartCoroutine(PopAndDestroy(rt.gameObject));
    }

    private IEnumerator PopAndDestroy(GameObject obj)
    {
        // Get transform
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) yield break;
        
        // Pop animation
        float duration = 0.2f;
        float elapsed = 0f;
        
        // Starting scale
        Vector3 startScale = rt.localScale;
        Vector3 endScale = startScale * 1.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Scale up quickly
            rt.localScale = Vector3.Lerp(startScale, endScale, t);
            
            // Fade out
            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                Color color = img.color;
                color.a = 1f - t;
                img.color = color;
            }
            
            yield return null;
        }
        
        // Destroy at end of animation
        Destroy(obj);
    }
}
