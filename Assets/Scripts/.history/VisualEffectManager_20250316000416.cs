using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CandyCoded.HapticFeedback;
using TMPro;
using UnityEngine.UI;

public class VisualEffectManager : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private GameObject healingSymbolPrefab;
    [SerializeField] private GameObject chitinSymbolPrefab;
    [SerializeField] private GameObject crumbSymbolPrefab;
    [SerializeField] private GameObject levelupSymbolPrefab;
    [SerializeField] private GameObject xpSymbolPrefab;
    [SerializeField] private int poolSizePerType = 10;
    [SerializeField] private float symbolDuration = 1.5f;
    [SerializeField] private float minSpawnRadius = 0.3f;
    [SerializeField] private float maxSpawnRadius = 0.8f;
    [SerializeField] private float floatSpeed = 3f;
    [SerializeField] private float fadeSpeed = 1.5f;

    [Header("Additional Effects")]
    [SerializeField] private bool useRotation = false;
    [SerializeField] private Vector3 baseScale = new Vector3(0.03f, 0.03f, 0.03f);
    [SerializeField] private Vector3 chitinScale = new Vector3(0.05f, 0.05f, 0.05f);
    [SerializeField] private Vector3 crumbScale = new Vector3(0.05f, 0.05f, 0.05f);
    [SerializeField] private Vector2 scaleMultiplierRange = new Vector2(0.5f, 0.7f);

    [Header("Chitin Effects")]
    [SerializeField] private Transform nestTransform;
    [SerializeField] private GameObject actualNestGameObject;
    [SerializeField] private float chitinFlyDuration = 1.5f;
    [SerializeField] private float chitinSpawnDelay = 0.2f;
    [SerializeField] private AnimationCurve chitinFlyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float spreadRadius = 80f;

    [Header("Resource Collection Effects")]
    [SerializeField] private float chitinSpawnOffsetX = -200f;
    [SerializeField] private float crumbSpawnOffsetX = -400f;
    [SerializeField] private float spawnOffsetY = 150f;

    [Header("Nest Pulsate Effect")]
    [SerializeField] private float pulsateScale = 1.2f;
    [SerializeField] private float pulsateDuration = 0.3f;

    [Header("XP Gain Effect")]
    [SerializeField] private int numberOfSymbols = 5;
    [SerializeField] private float minSymbolSpeed = 1.0f;
    [SerializeField] private float maxSymbolSpeed = 3.0f;
    [SerializeField] private float symbolLifetime = 2.0f;
    [SerializeField] private float symbolScale = 0.5f;
    [SerializeField] private RectTransform xpTargetTransform; // UI XP counter location
    
    [Header("Symbol Settings")]
    [SerializeField] private Sprite[] xpSymbolSprites; // Different XP symbol variations
    [SerializeField] private Color xpSymbolColor = new Color(1f, 0.8f, 0.2f); // Yellow-gold color

    private Dictionary<string, Queue<GameObject>> symbolPools;
    private Transform cameraTransform;
    private List<GameObject> xpSymbolPool = new List<GameObject>();
    private Canvas mainCanvas;
    private RectTransform canvasRectTransform;

    public float ChitinFlyDuration => chitinFlyDuration;

    // Singleton instance
    public static VisualEffectManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        symbolPools = new Dictionary<string, Queue<GameObject>>();
        InitializePool("heal", healingSymbolPrefab);
        InitializePool("chitin", chitinSymbolPrefab);
        InitializePool("crumb", crumbSymbolPrefab);
        cameraTransform = Camera.main.transform;

        // Find the main canvas if it exists
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas != null)
        {
            canvasRectTransform = mainCanvas.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("No Canvas found in the scene for VisualEffectManager");
        }
        
        // Pre-create XP symbols for the object pool
        InitializeXPSymbolPool();
    }

    private void InitializePool(string type, GameObject prefab)
    {
        if (prefab == null) return;

        Debug.Log($"Initializing pool for {type}. Prefab scale: {prefab.transform.localScale}");
        Debug.Log($"VisualEffectManager scale: {transform.localScale}");

        // Create a container with forced scale of 1
        GameObject poolContainer = new GameObject($"{type}Pool");
        poolContainer.transform.SetParent(null);
        poolContainer.transform.localScale = Vector3.one;
        poolContainer.transform.SetParent(transform, true);

        Debug.Log($"Pool container scale after parenting: {poolContainer.transform.localScale}");

        Queue<GameObject> pool = new Queue<GameObject>();
        for (int i = 0; i < poolSizePerType; i++)
        {
            GameObject symbol = Instantiate(prefab);
            Debug.Log($"Symbol {i} initial scale: {symbol.transform.localScale}");

            // Set all symbols to base scale (0.05)
            symbol.transform.localScale = baseScale;
            Debug.Log($"Symbol {i} scale after setting: {symbol.transform.localScale}");

            symbol.transform.SetParent(poolContainer.transform, true);
            Debug.Log($"Symbol {i} final scale after parenting: {symbol.transform.localScale}");

            symbol.SetActive(false);
            pool.Enqueue(symbol);
        }
        symbolPools[type] = pool;
    }

    public void SpawnEffect(string effectType, Vector3 position, int count = 1)
    {
        if (!symbolPools.ContainsKey(effectType)) return;

        if (effectType == "chitin")
        {
            StartCoroutine(SpawnChitinCollectEffect(count));
        }
        else if (effectType == "crumb")
        {
            StartCoroutine(SpawnCrumbCollectEffect(count));
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                if (symbolPools[effectType].Count == 0) return;

                GameObject symbol = symbolPools[effectType].Dequeue();
                symbol.transform.position = GetRandomPositionAround(position);
                symbol.SetActive(true);
                symbol.transform.rotation = cameraTransform.rotation;

                StartCoroutine(AnimateSymbol(symbol, effectType));
            }
        }
    }

    private Vector3 GetRandomPositionAround(Vector3 center)
    {
        Vector2 randomCircle = Random.insideUnitCircle;
        float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
        return center + new Vector3(randomCircle.x * radius, 0.5f, randomCircle.y * radius);
    }

    private IEnumerator SpawnChitinCollectEffect(int count)
    {
        if (nestTransform == null) yield break;

        Vector2 screenTopRight = new Vector2(Screen.width - 200f, Screen.height - 150f);

        for (int i = 0; i < count; i++)
        {
            if (symbolPools["chitin"].Count == 0) yield break;

            GameObject symbol = symbolPools["chitin"].Dequeue();
            Debug.Log($"Chitin symbol scale before setting position: {symbol.transform.localScale}");
            
            // Create random offset around top right corner
            Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;
            Vector3 spawnScreenPos = screenTopRight + randomOffset;

            // Convert screen position to world position
            Ray ray = Camera.main.ScreenPointToRay(spawnScreenPos);
            float distanceToCamera = 10f;
            Vector3 worldPos = ray.GetPoint(distanceToCamera);
            
            symbol.transform.position = worldPos;
            Debug.Log($"Chitin symbol scale after setting: {symbol.transform.localScale}");
            
            symbol.SetActive(true);
            StartCoroutine(AnimateResourceCollection(symbol, "chitin"));

            yield return new WaitForSeconds(chitinSpawnDelay);
        }
    }

    private IEnumerator SpawnCrumbCollectEffect(int count)
    {
        if (nestTransform == null) yield break;

        Vector2 screenTopLeft = new Vector2(Screen.width + crumbSpawnOffsetX, Screen.height - spawnOffsetY);

        for (int i = 0; i < count; i++)
        {
            if (symbolPools["crumb"].Count == 0) yield break;

            GameObject symbol = symbolPools["crumb"].Dequeue();
            Debug.Log($"Crumb symbol scale before setting position: {symbol.transform.localScale}");
            
            // Create random offset around top left corner
            Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;
            Vector3 spawnScreenPos = screenTopLeft + randomOffset;

            // Convert screen position to world position
            Ray ray = Camera.main.ScreenPointToRay(spawnScreenPos);
            float distanceToCamera = 10f;
            Vector3 worldPos = ray.GetPoint(distanceToCamera);
            
            symbol.transform.position = worldPos;
            Debug.Log($"Crumb symbol scale after setting: {symbol.transform.localScale}");
            
            symbol.SetActive(true);
            StartCoroutine(AnimateResourceCollection(symbol, "crumb"));

            yield return new WaitForSeconds(chitinSpawnDelay);
        }
    }

    private IEnumerator AnimateResourceCollection(GameObject symbol, string type)
    {
        Vector3 startPos = symbol.transform.position;
        Vector3 endPos = nestTransform.position;
        
        SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
        Color startColor = spriteRenderer.color;
        float elapsed = 0f;

        Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.5f);
        midPoint += Vector3.up * Random.Range(2f, 4f);

        // Always use baseScale (0.03) for both types
        Vector3 targetScale = baseScale;

        while (elapsed < chitinFlyDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / chitinFlyDuration;
            float curveValue = chitinFlyCurve.Evaluate(normalizedTime);

            Vector3 a = Vector3.Lerp(startPos, midPoint, curveValue);
            Vector3 b = Vector3.Lerp(midPoint, endPos, curveValue);
            symbol.transform.position = Vector3.Lerp(a, b, curveValue);

            // Remove the pulse scaling effect
            symbol.transform.localScale = targetScale;

            if (normalizedTime > 0.9f && normalizedTime < 0.92f)
            {
                SoundEffectManager.Instance.PlaySound("SymbolToNest", nestTransform.position, true);
            }

            yield return null;
        }

        // Pulsate the nest when resource reaches it
        if (actualNestGameObject != null)
        {
            StartCoroutine(PulsateNest());
            
            // Add haptic feedback when resources reach the nest
            HapticFeedback.LightFeedback();
        }

        // Fade out and reset
        float fadeDuration = 0.2f;
        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        spriteRenderer.color = startColor;
        symbol.transform.localScale = targetScale;
        symbol.SetActive(false);
        symbolPools[type].Enqueue(symbol);
    }

    private IEnumerator PulsateNest()
    {
        if (actualNestGameObject == null) yield break;
        
        // Always use Vector3.one as the original scale
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * pulsateScale;
        
        // Scale up
        float elapsed = 0f;
        float halfDuration = pulsateDuration / 2f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            actualNestGameObject.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            actualNestGameObject.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        // Ensure we end at exactly scale 1
        actualNestGameObject.transform.localScale = originalScale;
    }

    private System.Collections.IEnumerator AnimateSymbol(GameObject symbol, string effectType)
    {
        float elapsed = 0f;
        Vector3 startPos = symbol.transform.position;
        SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
        Color startColor = spriteRenderer.color;
        
        // Remove the random scale multiplier and use baseScale directly
        symbol.transform.localScale = baseScale;

        while (elapsed < symbolDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / symbolDuration;

            // Move upward
            symbol.transform.position = startPos + Vector3.up * (floatSpeed * normalizedTime);

            // Fade out
            spriteRenderer.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                Mathf.Lerp(1f, 0f, normalizedTime * fadeSpeed)
            );

            yield return null;
        }

        // Reset and return to pool
        spriteRenderer.color = startColor;
        symbol.transform.localScale = baseScale;
        symbol.SetActive(false);
        symbolPools[effectType].Enqueue(symbol);
    }

    private Vector3 GetWorldPositionFromUI(RectTransform rectTransform)
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform,
            RectTransformUtility.WorldToScreenPoint(Camera.main, rectTransform.position),
            Camera.main,
            out Vector3 worldPos
        );
        return worldPos;
    }

    public void PlayLevelUpEffect(Vector3 playerPosition, System.Action onCompleteCallback = null)
    {
        int symbolCount = 8; // Number of symbols to spawn
        int completedAnimations = 0;
        
        // Get player reference to track movement
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) {
            // Fall back to static position if player not found
            playerObj = new GameObject("TempPlayerPosition");
            playerObj.transform.position = playerPosition;
            Destroy(playerObj, 3f); // Clean up after animation
        }
        
        // Initialize the levelup pool if it doesn't exist
        if (!symbolPools.ContainsKey("levelup"))
        {
            InitializePool("levelup", levelupSymbolPrefab);
        }
        
        for (int i = 0; i < symbolCount; i++)
        {
            if (symbolPools["levelup"].Count == 0) break;
            
            GameObject symbol = symbolPools["levelup"].Dequeue();
            
            // Create a tighter clustering of symbols on the player, slightly above them
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.3f, 0.3f),  // Much tighter horizontal spread
                0.5f,                       // Start slightly above the player
                Random.Range(-0.3f, 0.3f)   // Much tighter depth spread
            );
            
            // Set initial position
            symbol.transform.position = playerObj.transform.position + randomOffset;
            symbol.SetActive(true);
            
            // Face the symbol toward the camera
            symbol.transform.rotation = cameraTransform.rotation;
            
            // Start a specialized animation for level up that tracks player movement
            StartCoroutine(AnimateLevelUpSymbolWithTracking(symbol, playerObj.transform, randomOffset, i * 0.1f, () => {
                completedAnimations++;
                if (completedAnimations >= symbolCount && onCompleteCallback != null)
                {
                    onCompleteCallback();
                }
            }));
        }
        
        // Play level up sound
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("LevelUp", playerPosition, false);
        }
    }

    private IEnumerator AnimateLevelUpSymbolWithTracking(GameObject symbol, Transform playerTransform, Vector3 initialOffset, float delay, System.Action onComplete)
    {
        // Initial delay for staggered effect
        yield return new WaitForSeconds(delay);
        
        SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) 
        {
            spriteRenderer = symbol.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                onComplete?.Invoke();
                yield break;
            }
        }
        
        Color startColor = spriteRenderer.color;
        Vector3 startScale = symbol.transform.localScale;
        
        // Set initial scale (slightly smaller)
        symbol.transform.localScale = startScale * 0.5f;
        
        // Rise up and scale animation
        float animDuration = 1.8f;
        float elapsed = 0f;
        
        // Store original horizontal position offset from player
        Vector3 horizontalOffset = new Vector3(initialOffset.x, 0, initialOffset.z);
        
        // Path parameters
        float pathRadius = 0.2f;
        float pathSpeed = 3f;
        float initialVerticalOffset = initialOffset.y;
        
        while (elapsed < animDuration && playerTransform != null)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / animDuration;
            
            // Calculate current height gain
            float heightGain = floatSpeed * normalizedTime * normalizedTime * 3f;
            
            // Create a slight spiral/curved path around the original offset
            float angle = normalizedTime * pathSpeed * Mathf.PI * 2;
            float spiralRadius = pathRadius * (1 - normalizedTime); // Spiral gets tighter as it rises
            
            Vector3 spiralOffset = new Vector3(
                Mathf.Cos(angle) * spiralRadius,
                0,
                Mathf.Sin(angle) * spiralRadius
            );
            
            // Position relative to the player with spiral offset and height gain
            Vector3 newPosition = playerTransform.position + horizontalOffset + spiralOffset;
            newPosition.y = playerTransform.position.y + initialVerticalOffset + heightGain;
            
            // Update position
            symbol.transform.position = newPosition;
            
            // Always face the camera
            symbol.transform.rotation = cameraTransform.rotation;
            
            // Scale up then down
            float scaleProgress = normalizedTime < 0.3f ? normalizedTime / 0.3f : 1f - ((normalizedTime - 0.3f) / 0.7f);
            symbol.transform.localScale = startScale * (0.5f + scaleProgress * 0.7f);
            
            // Fade in then out
            float alpha = normalizedTime < 0.2f ? 
                Mathf.Lerp(0f, 1f, normalizedTime / 0.2f) : 
                Mathf.Lerp(1f, 0f, (normalizedTime - 0.2f) / 0.8f);
                
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            yield return null;
        }
        
        // Reset and return to pool
        spriteRenderer.color = startColor;
        symbol.transform.localScale = startScale;
        symbol.SetActive(false);
        symbolPools["levelup"].Enqueue(symbol);
        
        // Notify completion
        onComplete?.Invoke();
    }

    private void InitializeXPSymbolPool()
    {
        if (xpSymbolPrefab == null)
        {
            Debug.LogError("XP Symbol Prefab not assigned to VisualEffectManager");
            return;
        }
        
        // Create pool parent
        GameObject poolParent = new GameObject("XP_Symbol_Pool");
        poolParent.transform.SetParent(transform);
        
        // Pre-instantiate symbols
        for (int i = 0; i < numberOfSymbols * 2; i++) // Create double the number for buffer
        {
            GameObject symbol = Instantiate(xpSymbolPrefab, poolParent.transform);
            symbol.SetActive(false);
            
            // Scale the symbol
            symbol.transform.localScale = new Vector3(symbolScale, symbolScale, symbolScale);
            
            // Set random sprite if available
            if (xpSymbolSprites != null && xpSymbolSprites.Length > 0)
            {
                Image symbolImage = symbol.GetComponent<Image>();
                if (symbolImage != null)
                {
                    symbolImage.sprite = xpSymbolSprites[Random.Range(0, xpSymbolSprites.Length)];
                    symbolImage.color = xpSymbolColor;
                }
            }
            
            xpSymbolPool.Add(symbol);
        }
    }
    
    // Play XP gain effect when depositing chitin at the nest
    public void PlayXPGainEffect(Vector3 startWorldPosition, int xpAmount)
    {
        if (mainCanvas == null || xpTargetTransform == null)
        {
            Debug.LogWarning("Cannot play XP gain effect: Canvas or XP Target not set");
            return;
        }
        
        // Convert world position to canvas position
        Vector2 startCanvasPosition = WorldToCanvasPosition(startWorldPosition);
        
        // Get target position (XP counter in UI)
        Vector2 targetPosition = new Vector2(xpTargetTransform.position.x, xpTargetTransform.position.y);
        
        // Number of symbols to spawn based on XP amount
        int symbolsToSpawn = Mathf.Min(numberOfSymbols, Mathf.Max(1, xpAmount / 5));
        
        // Spawn XP symbols
        for (int i = 0; i < symbolsToSpawn; i++)
        {
            GameObject symbol = GetPooledXPSymbol();
            if (symbol != null)
            {
                StartCoroutine(AnimateXPSymbol(symbol, startCanvasPosition, targetPosition, xpAmount));
            }
        }
        
        // Optional: Play a sound effect
        // SoundEffectManager.Instance?.PlaySound("XPGain", startWorldPosition);
    }
    
    // Play XP gain effect from the nest to the XP counter
    public void PlayXPGainEffectFromNest(int xpAmount)
    {
        // Find the nest transform
        Transform nestTransform = GameObject.FindGameObjectWithTag("Nest")?.transform;
        if (nestTransform == null)
        {
            Debug.LogWarning("Nest transform not found for XP gain effect");
            return;
        }
        
        PlayXPGainEffect(nestTransform.position, xpAmount);
    }
    
    // Get an inactive XP symbol from the pool
    private GameObject GetPooledXPSymbol()
    {
        foreach (GameObject symbol in xpSymbolPool)
        {
            if (!symbol.activeInHierarchy)
            {
                return symbol;
            }
        }
        
        // If all symbols are in use, expand the pool by creating a new one
        if (xpSymbolPrefab != null)
        {
            GameObject newSymbol = Instantiate(xpSymbolPrefab, transform);
            newSymbol.SetActive(false);
            
            // Scale the symbol
            newSymbol.transform.localScale = new Vector3(symbolScale, symbolScale, symbolScale);
            
            // Set random sprite if available
            if (xpSymbolSprites != null && xpSymbolSprites.Length > 0)
            {
                Image symbolImage = newSymbol.GetComponent<Image>();
                if (symbolImage != null)
                {
                    symbolImage.sprite = xpSymbolSprites[Random.Range(0, xpSymbolSprites.Length)];
                    symbolImage.color = xpSymbolColor;
                }
            }
            
            xpSymbolPool.Add(newSymbol);
            return newSymbol;
        }
        
        return null;
    }
    
    // Animate a single XP symbol
    private IEnumerator AnimateXPSymbol(GameObject symbol, Vector2 startPosition, Vector2 targetPosition, int xpAmount)
    {
        if (symbol == null) yield break;
        
        // Set symbol active and position
        symbol.SetActive(true);
        symbol.transform.position = startPosition;
        
        // Add random offset to start position for visual variety
        Vector2 randomOffset = new Vector2(
            Random.Range(-50f, 50f),
            Random.Range(-50f, 50f)
        );
        startPosition += randomOffset;
        
        // Generate a curved path using bezier curve
        Vector2 controlPoint = new Vector2(
            startPosition.x + Random.Range(-100f, 100f),
            startPosition.y + Random.Range(50f, 150f)
        );
        
        // Randomize speed for each symbol
        float speed = Random.Range(minSymbolSpeed, maxSymbolSpeed);
        float timeElapsed = 0;
        float duration = Vector2.Distance(startPosition, targetPosition) / (100f * speed);
        duration = Mathf.Clamp(duration, 0.5f, symbolLifetime);
        
        // Optional: Add text showing XP value
        TextMeshProUGUI xpText = symbol.GetComponentInChildren<TextMeshProUGUI>();
        if (xpText != null)
        {
            xpText.text = "+" + xpAmount.ToString();
        }
        
        // Animate along the bezier curve
        while (timeElapsed < duration)
        {
            // Calculate position along bezier curve
            float t = timeElapsed / duration;
            Vector2 position = QuadraticBezier(startPosition, controlPoint, targetPosition, t);
            
            // Update position
            symbol.transform.position = position;
            
            // Fade in at the beginning, fade out at the end
            Image symbolImage = symbol.GetComponent<Image>();
            if (symbolImage != null)
            {
                float alpha = 1.0f;
                if (t < 0.2f) alpha = t / 0.2f; // Fade in
                else if (t > 0.8f) alpha = (1 - t) / 0.2f; // Fade out
                
                Color color = symbolImage.color;
                symbolImage.color = new Color(color.r, color.g, color.b, alpha);
                
                // Also fade text if it exists
                if (xpText != null)
                {
                    Color textColor = xpText.color;
                    xpText.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
                }
            }
            
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Set symbol inactive when animation completes
        symbol.SetActive(false);
    }
    
    // Calculate a point along a quadratic bezier curve
    private Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }
    
    // Convert world position to canvas position
    private Vector2 WorldToCanvasPosition(Vector3 worldPosition)
    {
        Vector2 viewportPosition = Camera.main.WorldToViewportPoint(worldPosition);
        
        // Convert viewport position to canvas position
        return new Vector2(
            viewportPosition.x * canvasRectTransform.sizeDelta.x,
            viewportPosition.y * canvasRectTransform.sizeDelta.y
        );
    }

    public void SpawnXPGainEffect(int xpAmount)
    {
        // Debug log to verify this method is being called
        Debug.Log($"XP EFFECT: Spawning XP effect with {xpAmount} XP");
        
        // Start the coroutine for the XP gain effect
        StartCoroutine(SpawnXPGainEffectCoroutine(xpAmount));
    }

    private IEnumerator SpawnXPGainEffectCoroutine(int xpAmount)
    {
        // Calculate how many symbols to spawn based on XP amount (minimum 1, maximum based on value)
        int symbolsToSpawn = Mathf.Clamp(xpAmount / 5, 1, 7);
        
        // Create a target for the XP symbols to fly to (usually your XP counter in top-left UI)
        Vector2 targetPosition = new Vector2(200, Screen.height - 100); // Default position if no target set
        
        // If we have an explicit target transform, use it instead
        if (xpTargetTransform != null)
        {
            targetPosition = new Vector2(xpTargetTransform.position.x, xpTargetTransform.position.y);
        }
        
        // Find the nest position
        Transform nestTransform = GameObject.FindGameObjectWithTag("Nest")?.transform;
        if (nestTransform == null)
        {
            Debug.LogWarning("Cannot find nest for XP effect, using center of screen");
            nestTransform = Camera.main.transform;
        }
        
        // Convert nest world position to screen position
        Vector2 screenPos = Camera.main.WorldToScreenPoint(nestTransform.position);
        
        // Spawn XP symbols in sequence
        for (int i = 0; i < symbolsToSpawn; i++)
        {
            // Create XP symbol
            GameObject xpSymbol = null;
            
            // Try to get from pool first
            if (xpSymbolPrefab != null)
            {
                xpSymbol = Instantiate(xpSymbolPrefab, mainCanvas.transform);
                
                // Set initial position to nest screen position with random offset
                xpSymbol.transform.position = screenPos + new Vector2(
                    UnityEngine.Random.Range(-50f, 50f), 
                    UnityEngine.Random.Range(-50f, 50f)
                );
                
                // Set scale
                xpSymbol.transform.localScale = new Vector3(symbolScale, symbolScale, symbolScale);
                
                // Get the image component and set color
                Image symbolImage = xpSymbol.GetComponent<Image>();
                if (symbolImage != null)
                {
                    symbolImage.color = xpSymbolColor;
                    
                    // Set random sprite if available
                    if (xpSymbolSprites != null && xpSymbolSprites.Length > 0)
                    {
                        symbolImage.sprite = xpSymbolSprites[UnityEngine.Random.Range(0, xpSymbolSprites.Length)];
                    }
                }
                
                // Add text showing XP value if the prefab has a TextMeshProUGUI component
                TextMeshProUGUI xpText = xpSymbol.GetComponentInChildren<TextMeshProUGUI>();
                if (xpText != null)
                {
                    xpText.text = "+" + xpAmount.ToString();
                    xpText.color = xpSymbolColor;
                }
                
                // Animate the symbol to the target position
                StartCoroutine(AnimateXPSymbol(xpSymbol, xpSymbol.transform.position, targetPosition));
            }
            else
            {
                Debug.LogError("XP Symbol Prefab not assigned!");
            }
            
            // Short delay between spawning multiple symbols
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator AnimateXPSymbol(GameObject symbol, Vector2 startPosition, Vector2 targetPosition)
    {
        if (symbol == null) yield break;
        
        // Calculate a curved path
        Vector2 controlPoint = new Vector2(
            (startPosition.x + targetPosition.x) / 2 + UnityEngine.Random.Range(-100f, 100f),
            (startPosition.y + targetPosition.y) / 2 + UnityEngine.Random.Range(-50f, 100f)
        );
        
        // Animation duration
        float duration = UnityEngine.Random.Range(0.8f, 1.5f);
        float timeElapsed = 0;
        
        // Animate along the bezier curve
        while (timeElapsed < duration)
        {
            if (symbol == null) yield break;
            
            // Calculate position along bezier curve
            float t = timeElapsed / duration;
            Vector2 position = QuadraticBezier(startPosition, controlPoint, targetPosition, t);
            
            // Apply easing for smooth movement
            float easedT = Mathf.SmoothStep(0, 1, t);
            
            // Update position
            symbol.transform.position = Vector2.Lerp(
                Vector2.Lerp(startPosition, controlPoint, easedT),
                Vector2.Lerp(controlPoint, targetPosition, easedT),
                easedT
            );
            
            // Scale effect
            float scale = Mathf.Lerp(symbolScale, symbolScale * 0.7f, t);
            symbol.transform.localScale = new Vector3(scale, scale, scale);
            
            // Fade in at start, fade out at end
            Image symbolImage = symbol.GetComponent<Image>();
            if (symbolImage != null)
            {
                float alpha = 1.0f;
                if (t < 0.2f) alpha = t / 0.2f; // Fade in
                else if (t > 0.8f) alpha = (1 - t) / 0.2f; // Fade out
                
                Color color = symbolImage.color;
                symbolImage.color = new Color(color.r, color.g, color.b, alpha);
                
                // Also fade text if it exists
                TextMeshProUGUI xpText = symbol.GetComponentInChildren<TextMeshProUGUI>();
                if (xpText != null)
                {
                    Color textColor = xpText.color;
                    xpText.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
                }
            }
            
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Destroy the symbol when done
        Destroy(symbol);
    }
} 