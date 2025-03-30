using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CandyCoded.HapticFeedback;

public class VisualEffectManager : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private GameObject healingSymbolPrefab;
    [SerializeField] private GameObject levelupSymbolPrefab;
    [SerializeField] private GameObject shineSymbolPrefab;
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
    [SerializeField] private Transform nestTransform1;
    [SerializeField] private Transform nestTransform2;
    [SerializeField] private Transform nestTransform3;

    [SerializeField] private GameObject actualNestGameObject1;
    [SerializeField] private GameObject actualNestGameObject2;
    [SerializeField] private GameObject actualNestGameObject3;
    [SerializeField] private float chitinFlyDuration = 1.5f;
    [SerializeField] private float chitinSpawnDelay = 0.2f;
    [SerializeField] private AnimationCurve chitinFlyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float spreadRadius = 80f;

    [Header("XP Symbol Effects")]
    [SerializeField] private float xpFlyDuration = 1.3f;
    [SerializeField] private float xpSpreadRadius = 50f;
    [SerializeField] private string xpGainedSoundName = "XPGained";
    [SerializeField] private float targetOffsetY = 175f;

    [Header("XP Counter Animation")]
    [SerializeField] private bool incrementXPOneByOne = true;
    [SerializeField] private int xpPerSymbol = 1; // How much XP each symbol represents

    private Dictionary<string, Queue<GameObject>> symbolPools;
    private Transform cameraTransform;

    // Add this variable to track which nest is active
    private Transform activeNestTransform;
    private GameObject activeNestGameObject;

    public LevelUpPanelHelper levelUpPanelHelper;

    public float ChitinFlyDuration => chitinFlyDuration;

    private void Awake()
    {
        symbolPools = new Dictionary<string, Queue<GameObject>>();
        InitializePool("heal", healingSymbolPrefab);
        // Remove chitin, crumb, xp, and coin pools
        
        // Use a larger pool size for level up symbols
        InitializePoolWithSize("levelup", levelupSymbolPrefab, 40); // Increased from default pool size
        
        cameraTransform = Camera.main.transform;
    }

    private void InitializePool(string type, GameObject prefab)
    {
        if (prefab == null) return;

        // Create a container with forced scale of 1
        GameObject poolContainer = new GameObject($"{type}Pool");
        poolContainer.transform.SetParent(null);
        poolContainer.transform.localScale = Vector3.one;
        poolContainer.transform.SetParent(transform, true);

        Queue<GameObject> pool = new Queue<GameObject>();
        
        // Use default pool size for all remaining symbols
        int poolSize = poolSizePerType;
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject symbol = Instantiate(prefab);

            // Set all symbols to base scale
            symbol.transform.localScale = baseScale;

            // Get the sprite renderer
            SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Set a very high sorting order to ensure it renders on top
                spriteRenderer.sortingOrder = 1000;
                
                // Optionally set the sorting layer to "UI" or "Overlay" if you have one
                spriteRenderer.sortingLayerName = "UI";
            }

            symbol.transform.SetParent(poolContainer.transform, true);
            symbol.SetActive(false);
            pool.Enqueue(symbol);
        }
        symbolPools[type] = pool;
    }

    private void InitializePoolWithSize(string type, GameObject prefab, int size)
    {
        if (prefab == null) return;

        // Create a container with forced scale of 1
        GameObject poolContainer = new GameObject($"{type}Pool");
        poolContainer.transform.SetParent(null);
        poolContainer.transform.localScale = Vector3.one;
        poolContainer.transform.SetParent(transform, true);

        Queue<GameObject> pool = new Queue<GameObject>();
        
        for (int i = 0; i < size; i++)
        {
            GameObject symbol = Instantiate(prefab);
            symbol.transform.localScale = baseScale;
            symbol.transform.SetParent(poolContainer.transform, true);
            symbol.SetActive(false);
            pool.Enqueue(symbol);
        }
        symbolPools[type] = pool;
    }

    private void Start()
    {
        // Initialize the active nest
        FindActiveNest();
    }

    private void FindActiveNest()
    {
        // Default to the first nest
        activeNestTransform = nestTransform1;
        activeNestGameObject = actualNestGameObject1;
        
        // Try to find which nest the player is in
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            // Get all base interactions
            BaseInteraction[] bases = FindObjectsOfType<BaseInteraction>();
            
            foreach (BaseInteraction baseInteraction in bases)
            {
                // Check if this base contains the player
                Collider baseCollider = baseInteraction.GetComponent<Collider>();
                Collider playerCollider = player.GetComponent<Collider>();
                
                if (baseCollider != null && playerCollider != null)
                {
                    // Get the bounds of both colliders
                    Bounds baseBounds = baseCollider.bounds;
                    Bounds playerBounds = playerCollider.bounds;
                    
                    // Check if the player is inside this base's collider
                    if (baseBounds.Intersects(playerBounds))
                    {
                        // Get the layer name of this base
                        string layerName = LayerMask.LayerToName(baseInteraction.gameObject.layer);
                        
                        // Set the active nest based on the layer name
                        if (layerName == "PlayerNest1")
                        {
                            activeNestTransform = nestTransform1;
                            activeNestGameObject = actualNestGameObject1;
                            Debug.Log("Player is in Nest 1");
                        }
                        else if (layerName == "PlayerNest2")
                        {
                            activeNestTransform = nestTransform2;
                            activeNestGameObject = actualNestGameObject2;
                            Debug.Log("Player is in Nest 2");
                        }
                        else if (layerName == "PlayerNest3")
                        {
                            activeNestTransform = nestTransform3;
                            activeNestGameObject = actualNestGameObject3;
                            Debug.Log("Player is in Nest 3");
                        }
                        
                        break;
                    }
                }
            }
        }
        
        // Fallback - if no active nest was found, use nest 1
        if (activeNestTransform == null)
        {
            Debug.LogWarning("No active nest found, using Nest 1 as default");
            activeNestTransform = nestTransform1;
            activeNestGameObject = actualNestGameObject1;
        }
    }

    public void SpawnEffect(string effectType, Vector3 position, int count = 1)
    {
        // Find which nest is active first
        if (effectType == "chitin" || effectType == "crumb")
        {
            FindActiveNest();
        }

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
        if (activeNestTransform == null) yield break;

        Vector2 screenTopRight = new Vector2(Screen.width - 200f, Screen.height - 150f);

        for (int i = 0; i < count; i++)
        {
            if (symbolPools["chitin"].Count == 0) yield break;

            GameObject symbol = symbolPools["chitin"].Dequeue();
            
            // Create random offset around top right corner
            Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;
            Vector3 spawnScreenPos = screenTopRight + randomOffset;

            // Convert screen position to world position
            Ray ray = Camera.main.ScreenPointToRay(spawnScreenPos);
            float distanceToCamera = 10f;
            Vector3 worldPos = ray.GetPoint(distanceToCamera);
            
            symbol.transform.position = worldPos;
            
            symbol.SetActive(true);
            StartCoroutine(AnimateResourceCollection(symbol, "chitin"));

            yield return new WaitForSeconds(chitinSpawnDelay);
        }
    }

    public IEnumerator SpawnCrumbCollectEffect(int count)
    {
        if (activeNestTransform == null) yield break;

        Vector2 screenTopLeft = new Vector2(Screen.width + crumbSpawnOffsetX, Screen.height - spawnOffsetY);

        for (int i = 0; i < count; i++)
        {
            if (symbolPools["crumb"].Count == 0) yield break;

            GameObject symbol = symbolPools["crumb"].Dequeue();
            
            // Create random offset around top left corner
            Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;
            Vector3 spawnScreenPos = screenTopLeft + randomOffset;

            // Convert screen position to world position
            Ray ray = Camera.main.ScreenPointToRay(spawnScreenPos);
            float distanceToCamera = 10f;
            Vector3 worldPos = ray.GetPoint(distanceToCamera);
            
            symbol.transform.position = worldPos;
            
            symbol.SetActive(true);
            StartCoroutine(AnimateResourceCollection(symbol, "crumb"));

            yield return new WaitForSeconds(chitinSpawnDelay);
        }
    }

    private IEnumerator AnimateResourceCollection(GameObject symbol, string type)
    {
        Vector3 startPos = symbol.transform.position;
        Vector3 endPos = activeNestTransform.position;
        
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
                SoundEffectManager.Instance.PlaySound("SymbolToNest", activeNestTransform.position, true);
            }

            yield return null;
        }

        // Pulsate the nest when resource reaches it
        if (activeNestGameObject != null)
        {
            StartCoroutine(PulsateNest(activeNestGameObject));
            
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

    private IEnumerator PulsateNest(GameObject nestObject)
    {
        if (nestObject == null) yield break;
        
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
            nestObject.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            nestObject.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        // Ensure we end at exactly scale 1
        nestObject.transform.localScale = originalScale;
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

    public void PlayLevelUpEffect(Vector3 position)
    {
        // Make sure we have the level up symbol in the pool
        if (levelupSymbolPrefab != null && !symbolPools.ContainsKey("levelup"))
        {
            InitializePool("levelup", levelupSymbolPrefab);
        }
        
        if (symbolPools.ContainsKey("levelup") && symbolPools["levelup"].Count > 0)
        {
            // Get level up symbols from pool
            GameObject levelupSymbol = symbolPools["levelup"].Dequeue();
            
            // Position and activate
            levelupSymbol.transform.position = position;
            levelupSymbol.transform.rotation = cameraTransform.rotation;
            levelupSymbol.SetActive(true);
            
            // Start animation coroutine
            StartCoroutine(AnimateLevelUpSymbol(levelupSymbol));
        }
    }
    
    private IEnumerator AnimateLevelUpSymbol(GameObject symbol)
    {
        float lifetime = symbolDuration;
        float elapsed = 0f;
        
        // Store initial position and scale
        Vector3 startPosition = symbol.transform.position;
        Vector3 startScale = symbol.transform.localScale;
        Vector3 targetScale = startScale * 1.5f;
        
        // Get sprite renderer
        SpriteRenderer renderer = symbol.GetComponent<SpriteRenderer>();
        
        // Animation loop
        while (elapsed < lifetime)
        {
            // Update time
            elapsed += Time.deltaTime;
            float progress = elapsed / lifetime;
            
            // Scale up slowly then back down
            float scaleProgress = Mathf.PingPong(progress * 2f, 1f);
            symbol.transform.localScale = Vector3.Lerp(startScale, targetScale, scaleProgress);
            
            // Move upward
            symbol.transform.position = startPosition + Vector3.up * progress * 2f;
            
            // Rotate if enabled
            if (useRotation)
            {
                symbol.transform.Rotate(Vector3.forward, 50f * Time.deltaTime);
            }
            
            // Fade out in the last half
            if (progress > 0.5f && renderer != null)
            {
                Color color = renderer.color;
                color.a = Mathf.Lerp(1f, 0f, (progress - 0.5f) * 2f);
                renderer.color = color;
            }
            
            yield return null;
        }
        
        // Reset and return to pool
        if (renderer != null)
        {
            Color color = renderer.color;
            color.a = 1f;
            renderer.color = color;
        }
        
        symbol.transform.localScale = startScale;
        symbol.SetActive(false);
        symbolPools["levelup"].Enqueue(symbol);
    }

    public void PlayCoinRewardEffect(Vector3 panelPosition, int coinAmount)
    {
        if (coinSymbolPrefab == null)
        {
            Debug.LogError("Coin Symbol prefab not assigned!");
            return;
        }
        
        // Create a pool for coin symbols if it doesn't exist
        if (!symbolPools.ContainsKey("coin"))
        {
            InitializePool("coin", coinSymbolPrefab);
        }
        
        // Always spawn exactly 7 coins regardless of reward amount
        int symbolCount = 7;
        
        // Start spawning symbols from the panel position
        StartCoroutine(SpawnCoinSymbols(panelPosition, symbolCount, coinAmount));
    }

    private IEnumerator SpawnCoinSymbols(Vector3 panelPosition, int count, int totalCoinAmount)
    {
        // Define target position for coins
        Vector2 targetScreenPos;
        
        // Check if coin panel is assigned
        if (coinPanel == null)
        {
            Debug.LogWarning("Coin panel not assigned! Using fallback screen position.");
            // Fallback to a fixed screen position if panel not assigned
            targetScreenPos = new Vector2(Screen.width - 100f, Screen.height - 450f);
        }
        else
        {
            // Get the RectTransform of the coin panel
            RectTransform coinPanelRect = coinPanel.GetComponent<RectTransform>();
            if (coinPanelRect != null)
            {
                // For Screen Space - Overlay, we can get the screen position directly
                // Get the corners of the rect in screen space
                Vector3[] corners = new Vector3[4];
                coinPanelRect.GetWorldCorners(corners);
                
                // Calculate center of the panel in screen space
                Vector3 centerWorld = (corners[0] + corners[2]) / 2f;
                
                // Convert to screen space
                Canvas canvas = coinPanel.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // For overlay canvas, the world corners are already in screen space
                    targetScreenPos = new Vector2(centerWorld.x, centerWorld.y);
                    Debug.Log($"Using Screen Space Overlay mode. Target position: {targetScreenPos}");
                }
                else
                {
                    // For other canvas modes, convert world to screen
                    Camera cam = canvas != null && canvas.worldCamera != null ? 
                        canvas.worldCamera : Camera.main;
                    targetScreenPos = cam.WorldToScreenPoint(centerWorld);
                    Debug.Log($"Using regular world-to-screen conversion. Target position: {targetScreenPos}");
                }
            }
            else
            {
                Debug.LogWarning("Coin panel has no RectTransform! Using fallback.");
                targetScreenPos = new Vector2(Screen.width - 100f, Screen.height - 450f);
            }
        }
        
        // Start the actual coin spawning with the calculated target position
        StartCoroutine(SpawnCoinSymbolsWithTarget(panelPosition, count, totalCoinAmount, targetScreenPos));
        
        // Play coin sound after a slight delay
        yield return new WaitForSeconds(0.3f);
        if (SoundEffectManager.Instance != null)
        {
            // Use Pickup3 sound instead of CoinCollect
            SoundEffectManager.Instance.PlaySound("Pickup3", Vector3.zero, false);
        }
    }

    // New method to handle the actual spawning with a target
    private IEnumerator SpawnCoinSymbolsWithTarget(Vector3 panelPosition, int count, int totalCoinAmount, Vector2 targetScreenPos)
    {
        // Debug log to confirm the method is being called
        Debug.Log($"Spawning {count} coin symbols from position {panelPosition} to target {targetScreenPos}");
        
        for (int i = 0; i < count; i++)
        {
            if (!symbolPools.ContainsKey("coin") || symbolPools["coin"].Count == 0)
            {
                Debug.LogError("Coin symbol pool is empty or not found!");
                yield break;
            }
            
            GameObject symbol = symbolPools["coin"].Dequeue();
            
            // Ensure the symbol is active and visible
            symbol.SetActive(true);
            
            // Set scale for coin symbols
            Vector3 coinScale = new Vector3(0.4f, 0.4f, 0.4f);
            symbol.transform.localScale = coinScale;
            
            // Start at the panel position with a small random offset
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            symbol.transform.position = panelPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
            
            // Make sure it faces the camera and is in front of everything
            symbol.transform.rotation = cameraTransform.rotation;
            
            // Get a sprite renderer
            SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Set a bright gold color with full opacity
                spriteRenderer.color = new Color(1f, 0.84f, 0f, 1f);
                // Ensure the sprite renders on top of everything
                spriteRenderer.sortingOrder = 1000;
            }
            
            // Start the animation to the UI position
            StartCoroutine(AnimateCoinSymbolToScreen(symbol, targetScreenPos));
            
            // Stagger the spawns
            yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator AnimateCoinSymbolToScreen(GameObject symbol, Vector2 targetScreenPos)
    {
        // Get the starting position (at the mission panel)
        Vector3 startPos = symbol.transform.position;
        
        SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        Color startColor = spriteRenderer.color;
        float elapsed = 0f;
        
        // Store initial scale for scaling animation
        Vector3 initialScale = symbol.transform.localScale;
        Vector3 finalScale = initialScale * 0.7f; // End at 70% of original size
        
        // Animation duration - DECREASED for faster movement
        float duration = 1.2f; // Reduced from 1.8f to 1.2f for faster movement
        
        // Track if we've triggered the UI animation
        bool hasBumpedUI = false;
        
        // Convert the target screen position to world position ONCE (at the start)
        Ray initialRay = Camera.main.ScreenPointToRay(targetScreenPos);
        float distanceToCamera = 10f;
        Vector3 targetWorldPos = initialRay.GetPoint(distanceToCamera);
        
        // Calculate a FIXED midpoint for the arc (not random per frame)
        // Generate a random height ONCE that stays constant through the animation
        float arcHeight = Random.Range(1.0f, 2.0f);
        Vector3 fixedMidPoint = Vector3.Lerp(startPos, targetWorldPos, 0.5f) + Vector3.up * arcHeight;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            float curveValue = chitinFlyCurve.Evaluate(normalizedTime);
            
            // Use the fixed midpoint for bezier calculation
            Vector3 a = Vector3.Lerp(startPos, fixedMidPoint, curveValue);
            Vector3 b = Vector3.Lerp(fixedMidPoint, targetWorldPos, curveValue);
            symbol.transform.position = Vector3.Lerp(a, b, curveValue);
            
            // Always face the camera
            symbol.transform.rotation = cameraTransform.rotation;
            
            // Scale animation - grow slightly then shrink
            float scaleProgress = normalizedTime < 0.3f ? 
                Mathf.Lerp(1f, 1.3f, normalizedTime / 0.3f) : 
                Mathf.Lerp(1.3f, 0.7f, (normalizedTime - 0.3f) / 0.7f);
                
            symbol.transform.localScale = initialScale * scaleProgress;
            
            // Fade out at the very end
            float alpha = normalizedTime > 0.85f ? Mathf.Lerp(1f, 0f, (normalizedTime - 0.85f) / 0.15f) : 1f;
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            // When coin is close to destination, animate the UI text
            if (!hasBumpedUI && normalizedTime > 0.75f)
            {
                // Find UIHelper to animate coin text
                UIHelper uiHelper = FindObjectOfType<UIHelper>();
                if (uiHelper != null)
                {
                    // Get current coin count from player inventory
                    PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
                    if (playerInventory != null)
                    {
                        // Fix: Use currentCoinCount instead of CoinAmount
                        uiHelper.UpdateCoinDisplay(playerInventory.currentCoinCount);
                    }
                }
                
                hasBumpedUI = true;
            }
            
            yield return null;
        }
        
        // Reset and return to pool
        spriteRenderer.color = startColor;
        symbol.transform.localScale = initialScale;
        symbol.SetActive(false);
        symbolPools["coin"].Enqueue(symbol);
    }

    // Method to get screen position from a UI panel
    private Vector2 GetScreenPositionFromPanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("Panel not assigned! Using fallback screen position.");
            return new Vector2(Screen.width - 100f, Screen.height - 450f);
        }
        
        // Get the RectTransform of the panel
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // Get the corners of the rect in screen space
            Vector3[] corners = new Vector3[4];
            panelRect.GetWorldCorners(corners);
            
            // Calculate center of the panel in screen space
            Vector3 centerWorld = (corners[0] + corners[2]) / 2f;
            
            // Convert to screen space
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // For overlay canvas, the world corners are already in screen space
                return new Vector2(centerWorld.x, centerWorld.y);
            }
            else
            {
                // For other canvas modes, convert world to screen
                Camera cam = canvas != null && canvas.worldCamera != null ? 
                    canvas.worldCamera : Camera.main;
                return cam.WorldToScreenPoint(centerWorld);
            }
        }
        
        Debug.LogWarning("Panel has no RectTransform! Using fallback.");
        return new Vector2(Screen.width - 100f, Screen.height - 450f);
    }

    // Add this method to ensure symbols always face the camera
    private void LateUpdate()
    {
        // Make all active symbols face the camera
        foreach (var pool in symbolPools.Values)
        {
            foreach (var symbol in pool)
            {
                if (symbol.activeInHierarchy)
                {
                    symbol.transform.rotation = cameraTransform.rotation;
                    
                    // Optional: Ensure Z position is slightly in front of other objects
                    Vector3 pos = symbol.transform.position;
                    pos.z = -1f; // Adjust this value as needed
                    symbol.transform.position = pos;
                }
            }
        }
    }
} 