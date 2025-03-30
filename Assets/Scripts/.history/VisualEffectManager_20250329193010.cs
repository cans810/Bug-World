using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CandyCoded.HapticFeedback;

public class VisualEffectManager : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private GameObject healingSymbolPrefab;
    [SerializeField] private GameObject chitinSymbolPrefab;
    [SerializeField] private GameObject crumbSymbolPrefab;
    [SerializeField] private GameObject levelupSymbolPrefab;
    [SerializeField] private GameObject xpSymbolPrefab;
    [SerializeField] private GameObject coinSymbolPrefab;
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

    [Header("Resource Collection Effects")]
    [SerializeField] private float chitinSpawnOffsetX = -200f;
    [SerializeField] private float crumbSpawnOffsetX = -400f;
    [SerializeField] private float spawnOffsetY = 150f;

    [Header("Nest Pulsate Effect")]
    [SerializeField] private float pulsateScale = 1.2f;
    [SerializeField] private float pulsateDuration = 0.3f;

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

    [Header("Panels")]
    [SerializeField] private GameObject coinPanel;
    [SerializeField] private GameObject xpPanel;
    [SerializeField] private GameObject crumbPanel;
    [SerializeField] private GameObject chitinPanel;

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

    public void PlayLevelUpEffect(Vector3 playerPosition, System.Action onCompleteCallback = null)
    {
        // Skip level up effects if player is being revived
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null && playerInventory.IsReviving)
        {
            Debug.Log("Skipping level up effects during revival");
            onCompleteCallback?.Invoke(); // Still invoke callback if provided
            return;
        }

        int symbolCount = 20;
        int completedAnimations = 0;
        
        // Get player reference to track movement
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) {
            playerObj = new GameObject("TempPlayerPosition");
            playerObj.transform.position = playerPosition;
            Destroy(playerObj, 3f);
        }
        
        // Play haptic feedback for level up
        HapticFeedback.HeavyFeedback();
        
        // Check if we need to create the pool
        if (!symbolPools.ContainsKey("levelup"))
        {
            InitializePoolWithSize("levelup", levelupSymbolPrefab, 40);
        }
        
        // Create shine effect at the player's feet
        if (shineSymbolPrefab != null)
        {
            // Initialize pool if needed
            if (!symbolPools.ContainsKey("shine"))
            {
                InitializePool("shine", shineSymbolPrefab);
            }
            
            // Get a shine symbol
            GameObject shineSymbol = GetSymbolFromPool("shine", shineSymbolPrefab);
            
            // Position it under the player at y = 0.005
            Vector3 shinePosition = playerObj.transform.position;
            shinePosition.y = 0.005f; // Set very close to the ground
            shineSymbol.transform.position = shinePosition;
            
            // Rotate to face upward (not at camera)
            shineSymbol.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // X rotation of 90 degrees makes it face up
            
            // Set scale
            Vector3 originalShineScale = baseScale * 1.0f;
            shineSymbol.transform.localScale = originalShineScale;
            
            // Activate the shine
            shineSymbol.SetActive(true);
            
            // Animate the shine with player transform reference for following
            StartCoroutine(AnimateShineSymbol(shineSymbol, originalShineScale, playerObj.transform));
        }
        
        // Store a list of all active coroutines for this level up effect
        List<Coroutine> activeCoroutines = new List<Coroutine>();
        
        for (int i = 0; i < symbolCount; i++)
        {
            // Get a symbol with our new helper method that creates one if needed
            GameObject symbol = GetSymbolFromPool("levelup", levelupSymbolPrefab);
            
            // Start BELOW the player with vertical staggering
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.25f, 0.25f),
                -0.5f + (0.15f * i),  // Start 0.5 units BELOW the player, then stagger upward
                Random.Range(-0.25f, 0.25f)
            );
            
            // Set position and rotation
            symbol.transform.position = playerObj.transform.position + randomOffset;
            symbol.transform.rotation = cameraTransform.rotation;
            
            // IMPORTANT: Set initial scale before activating
            Vector3 originalScale = symbol.transform.localScale;
            symbol.transform.localScale = originalScale * 0.1f;
            
            // CRITICAL: Actually activate the symbol!
            symbol.SetActive(true);
            
            // Start the animation without a delay
            Coroutine animationCoroutine = StartCoroutine(AnimateLevelUpSymbolInRay(symbol, playerObj.transform, Vector3.up, i * 0.01f, originalScale, () => {
                completedAnimations++;
                if (completedAnimations >= symbolCount && onCompleteCallback != null)
                {
                    onCompleteCallback();
                }
            }));
            
            // Track this coroutine
            activeCoroutines.Add(animationCoroutine);
        }
        
        // Play level up sound
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("LevelUp", playerPosition, false);
        }

        levelUpPanelHelper.ShowUpperPanel();
        levelUpPanelHelper.InstantiateRewards();
    }

    // Update the shine effect creation to pass the player transform
    private IEnumerator AnimateShineSymbol(GameObject shineSymbol, Vector3 originalScale, Transform playerTransform)
    {
        if (shineSymbol == null || playerTransform == null) yield break;
        
        SpriteRenderer spriteRenderer = shineSymbol.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = shineSymbol.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null) yield break;
        }
        
        Color startColor = spriteRenderer.color;
        
        // Make sure it starts fully visible
        spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1.0f);
        
        // Duration of shine effect
        float duration = 3.0f;
        float elapsed = 0f;
        
        // Expand and fade
        while (elapsed < duration)
        {
            // Update position to match player, but keep fixed Y coordinate
            Vector3 newPosition = playerTransform.position;
            newPosition.y = 0.005f; // Keep fixed height
            shineSymbol.transform.position = newPosition;
            
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            
            // Grow gradually
            float growMultiplier = Mathf.Lerp(1.0f, 2.0f, normalizedTime);
            shineSymbol.transform.localScale = originalScale * growMultiplier;
            
            // Fade out in the second half of the animation
            float alpha = normalizedTime < 0.5f ? 
                1.0f : 
                Mathf.Lerp(1.0f, 0.0f, (normalizedTime - 0.5f) / 0.5f);
                
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            yield return null;
        }
        
        // Reset and return to pool
        spriteRenderer.color = startColor;
        shineSymbol.transform.localScale = originalScale;
        shineSymbol.SetActive(false);
        symbolPools["shine"].Enqueue(shineSymbol);
    }

    // Modified to receive the original scale as a parameter
    private IEnumerator AnimateLevelUpSymbolInRay(GameObject symbol, Transform playerTransform, Vector3 rayDirection, float delay, Vector3 originalScale, System.Action onComplete)
    {
        // Minimal delay
        yield return new WaitForSeconds(delay * 0.2f);
        
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
        
        // Store starting position
        Vector3 startPosition = symbol.transform.position;
        
        // Animation parameters
        float animDuration = 3.5f;  // Increased from 2.0f to 3.5f for slower movement
        float maxHeight = 9.0f;
        float elapsed = 0f;
        
        // Wider curved path
        Vector3 rayOffset = new Vector3(
            Random.Range(-0.15f, 0.15f),  // Increased from -0.05f/0.05f to -0.15f/0.15f
            0,
            Random.Range(-0.15f, 0.15f)   // Increased Z spread as well
        ).normalized * 0.3f;  // Increased from 0.2f to 0.3f for more pronounced curve
        
        float rayVelocityMultiplier = 1.0f + (delay * 1.0f);
        bool hasReachedPeak = false;
        
        while (elapsed < animDuration && playerTransform != null && !hasReachedPeak)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / animDuration;
            
            // Use a more gradual progression curve for slower initial movement
            float verticalProgress = Mathf.Pow(normalizedTime, 0.95f);
            
            // Growth animation - reach full size much faster (30% of animation instead of 50%)
            float baseGrowth = normalizedTime < 0.3f ? 
                Mathf.Lerp(0.1f, 1.0f, normalizedTime / 0.3f) : 
                1.0f;
                
            float pulse = 1.0f + 0.05f * Mathf.Sin(normalizedTime * 12f);
            
            // Apply scale
            symbol.transform.localScale = originalScale * baseGrowth * pulse;
            
            // Vertical movement code
            float currentHeight = Mathf.Lerp(0, maxHeight * rayVelocityMultiplier, verticalProgress);
            
            // Allow more horizontal drift for a wider ray
            float rayIntegrity = 0.95f;  // Decreased from 0.98f to 0.95f to allow more drift
            float horizontalDrift = Mathf.Sin(normalizedTime * 2f) * 0.1f * (1f - rayIntegrity);  // Increased from 0.05f to 0.1f
            
            Vector3 newPosition = startPosition + new Vector3(
                horizontalDrift + rayOffset.x * currentHeight * 0.1f,  // Increased from 0.05f to 0.1f
                currentHeight,
                rayOffset.z * currentHeight * 0.1f                    // Increased from 0.05f to 0.1f
            );
            
            symbol.transform.position = newPosition;
            symbol.transform.rotation = cameraTransform.rotation;
            
            // Alpha code (only fade in, no fade out)
            float alpha = normalizedTime < 0.1f ? 
                Mathf.Lerp(0f, 1f, normalizedTime / 0.1f) : 
                1f;
            
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            // Check if we've reached peak height (at 95% of animation)
            if (normalizedTime >= 0.95f)
            {
                hasReachedPeak = true;
            }
            
            yield return null;
        }
        
        // Reset and return to pool immediately when peak is reached
        spriteRenderer.color = startColor;
        symbol.transform.localScale = originalScale;
        symbol.SetActive(false);
        symbolPools["levelup"].Enqueue(symbol);
        
        onComplete?.Invoke();
    }

    public void PlayXPGainEffect(Vector3 nestPosition, int xpAmount, int startXP, int targetXP)
    {
        if (xpSymbolPrefab == null)
        {
            Debug.LogError("XP Symbol prefab not assigned!");
            return;
        }
        
        // Add debug logging to track calls
        Debug.Log($"PlayXPGainEffect called with xpAmount: {xpAmount}, startXP: {startXP}, targetXP: {targetXP}");
        
        // Create a pool for XP symbols if it doesn't exist
        if (!symbolPools.ContainsKey("xp"))
        {
            InitializePool("xp", xpSymbolPrefab);
        }
        
        // Determine number of XP symbols based on XP amount
        int symbolCount = 5; // Default for < 20
        if (xpAmount >= 20 && xpAmount < 60)
        {
            symbolCount = 10;
        }
        else if (xpAmount >= 60 && xpAmount < 100)
        {
            symbolCount = 20;
        }
        else if (xpAmount >= 100)
        {
            symbolCount = 30;
        }
        
        // Log the calculated values
        Debug.Log($"Creating {symbolCount} XP symbols for {xpAmount} XP");
        
        // Start spawning symbols - use the nest position as the starting point
        StartCoroutine(SpawnXPSymbols(nestPosition, symbolCount, xpAmount, startXP, targetXP));
    }

    private IEnumerator SpawnXPSymbols(Vector3 nestPosition, int symbolCount, int totalXPAmount, int startXP, int targetXP)
    {
        // Define the target screen position (top left for XP counter)
        Vector2 screenTopLeft = new Vector2(100f, Screen.height - spawnOffsetY);
        
        // Calculate XP per symbol
        int xpPerSymbol = totalXPAmount / symbolCount;
        if (xpPerSymbol < 1) xpPerSymbol = 1;
        
        // Track how much XP we've added so far
        int xpAdded = 0;
        
        // Get reference to player inventory
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null) 
        {
            Debug.LogError("Could not find PlayerInventory!");
            yield break;
        }
        
        // Ensure player's XP is at the starting value (in case something changed it)
        if (playerInventory.TotalExperience != startXP)
        {
            Debug.LogWarning($"XP mismatch! Expected {startXP}, but found {playerInventory.TotalExperience}");
            startXP = playerInventory.TotalExperience;
            targetXP = startXP + totalXPAmount;
        }
        
        for (int i = 0; i < symbolCount; i++)
        {
            if (!symbolPools.ContainsKey("xp") || symbolPools["xp"].Count == 0) yield break;
            
            GameObject symbol = symbolPools["xp"].Dequeue();
            
            // Set scale to exactly 0.03 (hardcoded)
            symbol.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            
            // Start at the nest position with a small random offset
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            symbol.transform.position = nestPosition + new Vector3(randomOffset.x, 0.5f, randomOffset.y);
            
            // Make sure it faces the camera
            symbol.transform.rotation = cameraTransform.rotation;
            symbol.SetActive(true);
            
            // Get a sprite renderer
            SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Set a bright gold color
                spriteRenderer.color = new Color(1f, 0.8f, 0.2f, 1f);
            }
            
            // Calculate how much XP this symbol represents
            // Last symbol gets any leftover XP to ensure total adds up exactly
            int thisSymbolXP = (i == symbolCount - 1) 
                ? (totalXPAmount - xpAdded) 
                : xpPerSymbol;
            
            // Store the XP value this symbol represents
            symbol.name = $"XPSymbol_{thisSymbolXP}";
            
            // Start the animation to the UI position
            StartCoroutine(AnimateXPSymbolToScreen(symbol, screenTopLeft, thisSymbolXP, startXP + xpAdded));
            
            // Track XP added
            xpAdded += thisSymbolXP;
            
            // Stagger the spawns
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator AnimateXPSymbolToScreen(GameObject symbol, Vector2 targetScreenPos, int xpValue, int currentXP)
    {
        // Get the starting position (at the nest)
        Vector3 startPos = symbol.transform.position;
        
        SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        Color startColor = spriteRenderer.color;
        float elapsed = 0f;
        
        // Store initial scale for scaling animation
        Vector3 initialScale = symbol.transform.localScale;
        Vector3 finalScale = initialScale * 0.3f; // End at 30% of original size
        
        // Use a consistent duration
        float duration = chitinFlyDuration;
        
        // Track if we've added XP yet
        bool hasAddedXP = false;
        
        // Track if we've played the sound yet
        bool hasSoundPlayed = false;
        
        // Get reference to player inventory
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogError("Could not find PlayerInventory!");
            yield break;
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            float curveValue = chitinFlyCurve.Evaluate(normalizedTime);
            
            // Always recalculate the target position each frame
            Vector2 currentTargetScreenPos = new Vector2(100f, Screen.height - targetOffsetY);
            Ray ray = Camera.main.ScreenPointToRay(currentTargetScreenPos);
            float distanceToCamera = 10f;
            Vector3 targetPos = ray.GetPoint(distanceToCamera);
            
            // Adjust the Y position down a bit
            targetPos.y -= 0.5f;
            
            // Create a curved path with midpoint above
            Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
            midPoint += Vector3.up * Random.Range(0.5f, 1.5f);
            
            // Use bezier curve movement
            Vector3 a = Vector3.Lerp(startPos, midPoint, curveValue);
            Vector3 b = Vector3.Lerp(midPoint, targetPos, curveValue);
            symbol.transform.position = Vector3.Lerp(a, b, curveValue);
            
            // Always face the camera
            symbol.transform.rotation = cameraTransform.rotation;
            
            // Scale down as it approaches destination
            if (normalizedTime > 0.5f)
            {
                float scaleT = (normalizedTime - 0.5f) / 0.5f;
                symbol.transform.localScale = Vector3.Lerp(initialScale, finalScale, scaleT);
            }
            
            // Play sound when halfway to destination
            if (!hasSoundPlayed && normalizedTime >= 0.5f)
            {
                PlayXPSound();
                hasSoundPlayed = true;
            }
            
            // When the symbol is 90% of the way to the destination, add XP
            if (!hasAddedXP && normalizedTime >= 0.9f)
            {
                // Add XP for this symbol
                int newXP = currentXP + xpValue;
                playerInventory.AddXP(xpValue);
                
                // Log that we're adding XP
                Debug.Log($"Adding {xpValue} XP from symbol. Current: {currentXP}, New: {newXP}");
                
                hasAddedXP = true;
            }
            
            yield return null;
        }
        
        // If we somehow missed adding the XP during animation, add it now
        if (!hasAddedXP && playerInventory != null)
        {
            playerInventory.AddXP(xpValue);
            Debug.Log($"Adding {xpValue} XP from symbol at end of animation");
        }
        
        // Play sound at the end if it hasn't played yet
        if (!hasSoundPlayed)
        {
            PlayXPSound();
        }
        
        // Reset and return to pool
        spriteRenderer.color = startColor;
        symbol.transform.localScale = initialScale;
        symbol.SetActive(false);
        symbolPools["xp"].Enqueue(symbol);
    }

    // Modify this helper method to play the XP sound without changing volume
    private void PlayXPSound()
    {
        // Use SoundEffectManager with original volume settings
        if (SoundEffectManager.Instance != null)
        {
            AudioSource source = SoundEffectManager.Instance.PlaySound(xpGainedSoundName, Camera.main.transform.position, false);
            if (source != null)
            {
                // Only set priority, don't change volume
                source.priority = 0;   // Highest priority
                Debug.Log($"Playing XP sound: {xpGainedSoundName}");
            }
            else
            {
                // Fallback: Use a direct AudioSource.PlayClipAtPoint
                AudioClip clip = Resources.Load<AudioClip>("Sounds/" + xpGainedSoundName);
                if (clip != null)
                {
                    // Use default volume (0.5f)
                    AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
                    Debug.Log("Playing XP sound via PlayClipAtPoint");
                }
                else
                {
                    Debug.LogWarning($"Failed to play XP sound - could not find sound {xpGainedSoundName}");
                }
            }
        }
        else
        {
            Debug.LogError("Cannot play XP sound - SoundEffectManager not found");
        }
    }

    public void PlayXPGainEffectVisualOnly(Vector3 position, int xpAmount)
    {
        if (xpSymbolPrefab == null)
        {
            Debug.LogError("XP Symbol prefab not assigned!");
            return;
        }
        
        Debug.Log($"Playing visual-only XP effect for {xpAmount} XP (no XP will be added)");
        
        // Create a pool for XP symbols if it doesn't exist
        if (!symbolPools.ContainsKey("xp"))
        {
            InitializePool("xp", xpSymbolPrefab);
        }
        
        // Determine number of XP symbols based on XP amount
        int symbolCount = 5; // Default for < 20
        if (xpAmount >= 20 && xpAmount < 60)
        {
            symbolCount = 10;
        }
        else if (xpAmount >= 60 && xpAmount < 100)
        {
            symbolCount = 20;
        }
        else if (xpAmount >= 100)
        {
            symbolCount = 30;
        }
        
        // Start spawning symbols - use just the visual effect without adding XP
        StartCoroutine(SpawnXPSymbolsVisualOnly(position, symbolCount));
    }

    // Modified version that doesn't add actual XP
    private IEnumerator SpawnXPSymbolsVisualOnly(Vector3 position, int symbolCount)
    {
        // Define the target screen position (XP counter location)
        Vector2 targetScreenPos = new Vector2(100f, Screen.height - targetOffsetY);
        
        for (int i = 0; i < symbolCount; i++)
        {
            if (!symbolPools.ContainsKey("xp") || symbolPools["xp"].Count == 0) yield break;
            
            GameObject symbol = symbolPools["xp"].Dequeue();
            
            // Set scale appropriately
            symbol.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            
            // Start at the position with a small random offset
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            symbol.transform.position = position + new Vector3(randomOffset.x, 0.5f, randomOffset.y);
            
            // Make sure it faces the camera
            symbol.transform.rotation = cameraTransform.rotation;
            symbol.SetActive(true);
            
            // Get a sprite renderer
            SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Set a bright gold color
                spriteRenderer.color = new Color(1f, 0.8f, 0.2f, 1f);
            }
            
            // Start the animation to the UI position
            StartCoroutine(AnimateXPSymbolVisualOnly(symbol, targetScreenPos));
            
            // Stagger the spawns
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Animation coroutine without adding XP
    private IEnumerator AnimateXPSymbolVisualOnly(GameObject symbol, Vector2 targetScreenPos)
    {
        // Get the starting position
        Vector3 startPos = symbol.transform.position;
        
        SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        Color startColor = spriteRenderer.color;
        float elapsed = 0f;
        
        // Store initial scale for scaling animation
        Vector3 initialScale = symbol.transform.localScale;
        Vector3 finalScale = initialScale * 0.3f; // End at 30% of original size
        
        // Use a consistent duration
        float duration = xpFlyDuration;
        
        // Track if we've played the sound yet
        bool hasSoundPlayed = false;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            float curveValue = chitinFlyCurve.Evaluate(normalizedTime);
            
            // Always recalculate the target position each frame
            Vector2 currentTargetScreenPos = new Vector2(100f, Screen.height - targetOffsetY);
            Ray ray = Camera.main.ScreenPointToRay(currentTargetScreenPos);
            float distanceToCamera = 10f;
            Vector3 targetPos = ray.GetPoint(distanceToCamera);
            
            // Create a curved path with midpoint above
            Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
            midPoint += Vector3.up * Random.Range(0.5f, 1.5f);
            
            // Use bezier curve movement
            Vector3 a = Vector3.Lerp(startPos, midPoint, curveValue);
            Vector3 b = Vector3.Lerp(midPoint, targetPos, curveValue);
            symbol.transform.position = Vector3.Lerp(a, b, curveValue);
            
            // Always face the camera
            symbol.transform.rotation = cameraTransform.rotation;
            
            // Scale down as it approaches destination
            if (normalizedTime > 0.5f)
            {
                float scaleT = (normalizedTime - 0.5f) / 0.5f;
                symbol.transform.localScale = Vector3.Lerp(initialScale, finalScale, scaleT);
            }
            
            // Play sound when halfway to destination
            if (!hasSoundPlayed && normalizedTime >= 0.5f)
            {
                PlayXPSound();
                hasSoundPlayed = true;
            }
            
            yield return null;
        }
        
        // Play sound at the end if it hasn't played yet
        if (!hasSoundPlayed)
        {
            PlayXPSound();
        }
        
        // Reset and return to pool
        spriteRenderer.color = startColor;
        symbol.transform.localScale = initialScale;
        symbol.SetActive(false);
        symbolPools["xp"].Enqueue(symbol);
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

    // Update the XP symbol animation to target the XP panel
    private IEnumerator FlyXPSymbolToCounter(GameObject symbol, Vector3 startPos)
    {
        SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        Color startColor = spriteRenderer.color;
        float elapsed = 0f;
        Vector3 initialScale = symbol.transform.localScale;
        Vector3 finalScale = initialScale * 0.3f; // End at 30% of original size
        
        // Get the target screen position from XP panel
        Vector2 targetScreenPos = GetScreenPositionFromPanel(xpPanel);
        
        // Convert target screen position to world position ONCE
        Ray initialRay = Camera.main.ScreenPointToRay(targetScreenPos);
        float distanceToCamera = 10f;
        Vector3 targetPos = initialRay.GetPoint(distanceToCamera);
        
        // Adjust the Y position down a bit
        targetPos.y -= 0.5f;
        
        // Use a consistent speed
        float duration = xpFlyDuration;
        
        // Calculate a fixed midpoint (not random per frame)
        float arcHeight = Random.Range(0.8f, 1.8f);
        Vector3 fixedMidPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + Vector3.up * arcHeight;
        
        // Track if we've played the sound yet
        bool hasSoundPlayed = false;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            float curveValue = chitinFlyCurve.Evaluate(normalizedTime);
            
            // Use the fixed midpoint for bezier calculation
            Vector3 a = Vector3.Lerp(startPos, fixedMidPoint, curveValue);
            Vector3 b = Vector3.Lerp(fixedMidPoint, targetPos, curveValue);
            symbol.transform.position = Vector3.Lerp(a, b, curveValue);
            
            // Always face the camera
            symbol.transform.rotation = cameraTransform.rotation;
            
            // Scale down as it approaches destination
            if (normalizedTime > 0.5f)
            {
                float scaleT = (normalizedTime - 0.5f) / 0.5f;
                symbol.transform.localScale = Vector3.Lerp(initialScale, finalScale, scaleT);
            }
            
            // Play sound halfway through the animation
            if (!hasSoundPlayed && normalizedTime >= 0.5f)
            {
                PlayXPSound();
                hasSoundPlayed = true;
            }
            
            yield return null;
        }
        
        // Play sound at the end if it hasn't played yet
        if (!hasSoundPlayed)
        {
            PlayXPSound();
        }
        
        // Reset and return to pool
        spriteRenderer.color = startColor;
        symbol.transform.localScale = initialScale;
        symbol.SetActive(false);
        symbolPools["xp"].Enqueue(symbol);
    }

    // Update Chitin fly animation to target chitin panel
    private IEnumerator FlyChitinToNest(GameObject chitinSymbol, Vector3 startPos, int amount = 1)
    {
        float elapsed = 0;
        SpriteRenderer spriteRenderer = chitinSymbol.GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer.color;
        Vector3 originalScale = chitinSymbol.transform.localScale;
        
        // Get the target screen position from chitin panel
        Vector2 targetScreenPos = GetScreenPositionFromPanel(chitinPanel);
        
        // Convert target screen position to world position ONCE
        Ray initialRay = Camera.main.ScreenPointToRay(targetScreenPos);
        float distanceToCamera = 10f;
        Vector3 targetPos = initialRay.GetPoint(distanceToCamera);
        
        // Create a fixed midpoint for the arc
        float arcHeight = Random.Range(1.0f, 2.5f);
        Vector3 fixedMidPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + Vector3.up * arcHeight;
        
        // Special effects when the chitin reaches the nest
        bool effectsTriggered = false;
        
        while (elapsed < chitinFlyDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / chitinFlyDuration;
            float curveValue = chitinFlyCurve.Evaluate(normalizedTime);
            
            // Use bezier curve with fixed midpoint
            Vector3 a = Vector3.Lerp(startPos, fixedMidPoint, curveValue);
            Vector3 b = Vector3.Lerp(fixedMidPoint, targetPos, curveValue);
            chitinSymbol.transform.position = Vector3.Lerp(a, b, curveValue);
            
            // Always face the camera
            chitinSymbol.transform.rotation = cameraTransform.rotation;
            
            // Trigger effects near the end of the animation (for chitin)
            if (!effectsTriggered && normalizedTime > 0.9f)
            {
                // Get the UIHelper
                UIHelper uiHelper = FindObjectOfType<UIHelper>();
                if (uiHelper != null)
                {
                    // Use a general approach - try to pulse the UI panel itself rather than the text
                    // This will work without needing access to the private text fields
                    if (chitinPanel != null)
                    {
                        RectTransform panelRect = chitinPanel.GetComponent<RectTransform>();
                        if (panelRect != null)
                        {
                            StartCoroutine(PulseUIElement(panelRect));
                        }
                    }
                }
                effectsTriggered = true;
            }
            
            // Fade out at the end
            if (normalizedTime > 0.8f)
            {
                float alpha = 1f - ((normalizedTime - 0.8f) / 0.2f);
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
            
            yield return null;
        }
        
        // Reset and return to pool
        chitinSymbol.SetActive(false);
        spriteRenderer.color = originalColor;
        chitinSymbol.transform.localScale = originalScale;
        symbolPools["chitin"].Enqueue(chitinSymbol);
    }

    // Update crumb fly animation to target crumb panel
    private IEnumerator FlyCrumbToInventory(GameObject crumbSymbol, Vector3 startPos, int amount = 1)
    {
        float elapsed = 0;
        SpriteRenderer spriteRenderer = crumbSymbol.GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer.color;
        Vector3 originalScale = crumbSymbol.transform.localScale;
        
        // Get the target screen position from crumb panel
        Vector2 targetScreenPos = GetScreenPositionFromPanel(crumbPanel);
        
        // Convert target screen position to world position ONCE
        Ray initialRay = Camera.main.ScreenPointToRay(targetScreenPos);
        float distanceToCamera = 10f;
        Vector3 targetPos = initialRay.GetPoint(distanceToCamera);
        
        // Create a fixed midpoint for the arc
        float arcHeight = Random.Range(1.0f, 2.0f);
        Vector3 fixedMidPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + Vector3.up * arcHeight;
        
        // Animation effects for when the crumb reaches the counter
        bool effectsTriggered = false;
        
        while (elapsed < chitinFlyDuration)  // Using same duration as chitin for consistency
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / chitinFlyDuration;
            float curveValue = chitinFlyCurve.Evaluate(normalizedTime);
            
            // Use bezier curve with fixed midpoint
            Vector3 a = Vector3.Lerp(startPos, fixedMidPoint, curveValue);
            Vector3 b = Vector3.Lerp(fixedMidPoint, targetPos, curveValue);
            crumbSymbol.transform.position = Vector3.Lerp(a, b, curveValue);
            
            // Always face the camera
            crumbSymbol.transform.rotation = cameraTransform.rotation;
            
            // Trigger effects near the end of the animation (for crumb)
            if (!effectsTriggered && normalizedTime > 0.9f)
            {
                // Get the UIHelper
                UIHelper uiHelper = FindObjectOfType<UIHelper>();
                if (uiHelper != null)
                {
                    // Use a general approach - try to pulse the UI panel itself rather than the text
                    // This will work without needing access to the private text fields
                    if (crumbPanel != null)
                    {
                        RectTransform panelRect = crumbPanel.GetComponent<RectTransform>();
                        if (panelRect != null)
                        {
                            StartCoroutine(PulseUIElement(panelRect));
                        }
                    }
                }
                effectsTriggered = true;
            }
            
            // Fade out at the end
            if (normalizedTime > 0.8f)
            {
                float alpha = 1f - ((normalizedTime - 0.8f) / 0.2f);
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
            
            yield return null;
        }
        
        // Reset and return to pool
        crumbSymbol.SetActive(false);
        spriteRenderer.color = originalColor;
        crumbSymbol.transform.localScale = originalScale;
        symbolPools["crumb"].Enqueue(crumbSymbol);
    }

    // Add this helper method to pulse UI elements
    private IEnumerator PulseUIElement(RectTransform rectTransform)
    {
        if (rectTransform == null) yield break;
        
        // Save original scale
        Vector3 originalScale = rectTransform.localScale;
        float pulseDuration = 0.3f;
        
        // Pulse animation: Grow
        float elapsed = 0f;
        while (elapsed < pulseDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (pulseDuration * 0.5f);
            float easedProgress = Mathf.SmoothStep(0, 1, progress);
            rectTransform.localScale = Vector3.Lerp(originalScale, originalScale * 1.3f, easedProgress);
            yield return null;
        }
        
        // Pulse animation: Shrink back
        elapsed = 0f;
        while (elapsed < pulseDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (pulseDuration * 0.5f);
            float easedProgress = Mathf.SmoothStep(0, 1, progress);
            rectTransform.localScale = Vector3.Lerp(originalScale * 1.3f, originalScale, easedProgress);
            yield return null;
        }
        
        // Ensure we end at exactly the original scale
        rectTransform.localScale = originalScale;
    }

    // Add this method to support XP rewards from missions
    public void PlayXPRewardEffect(Vector3 panelPosition, int xpAmount)
    {
        if (xpSymbolPrefab == null)
        {
            Debug.LogError("XP Symbol prefab not assigned!");
            return;
        }
        
        // Create a pool for XP symbols if it doesn't exist
        if (!symbolPools.ContainsKey("xp"))
        {
            InitializePool("xp", xpSymbolPrefab);
        }
        
        // Always spawn exactly 5 XP symbols regardless of reward amount
        int symbolCount = 5;
        
        Debug.Log($"Playing XP reward effect: {xpAmount} XP from {panelPosition}");
        
        // Spawn and animate XP symbols from the panel position
        StartCoroutine(SpawnXPSymbolsFromPosition(panelPosition, symbolCount, xpAmount));
    }

    // Method to spawn XP symbols from a specific position
    private IEnumerator SpawnXPSymbolsFromPosition(Vector3 panelPosition, int count, int totalXPAmount)
    {
        // Stagger the spawn of each XP symbol
        for (int i = 0; i < count; i++)
        {
            if (!symbolPools.ContainsKey("xp") || symbolPools["xp"].Count == 0)
            {
                Debug.LogError("XP symbol pool is empty or not found!");
                yield break;
            }
            
            GameObject symbol = symbolPools["xp"].Dequeue();
            
            // Ensure the symbol is active and visible
            symbol.SetActive(true);
            
            // Set scale for XP symbols
            Vector3 xpScale = new Vector3(0.4f, 0.4f, 0.4f);
            symbol.transform.localScale = xpScale;
            
            // Start at the panel position with a small random offset
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            symbol.transform.position = panelPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
            
            // Make sure it faces the camera
            symbol.transform.rotation = cameraTransform.rotation;
            
            // Setup the sprite renderer
            SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Set a bright blue color for XP
                spriteRenderer.color = new Color(0.2f, 0.6f, 1f, 1f);
                // Ensure the sprite renders on top of everything
                spriteRenderer.sortingOrder = 1000;
            }
            
            // Start the animation to the XP counter using our existing method
            StartCoroutine(FlyXPSymbolToCounter(symbol, symbol.transform.position));
            
            // Stagger the spawns
            yield return new WaitForSeconds(0.08f);
        }
    }

    // Method to dynamically create new symbols if the pool is depleted
    private GameObject GetSymbolFromPool(string type, GameObject prefab)
    {
        if (!symbolPools.ContainsKey(type))
        {
            Debug.LogWarning($"Pool for {type} doesn't exist. Creating it.");
            InitializePool(type, prefab);
        }
        
        if (symbolPools[type].Count == 0)
        {
            // Pool is empty, create a new symbol dynamically
            Debug.Log($"Pool for {type} is empty. Creating additional symbol.");
            GameObject poolContainer = transform.Find($"{type}Pool").gameObject;
            
            GameObject symbol = Instantiate(prefab);
            symbol.transform.localScale = baseScale;
            symbol.transform.SetParent(poolContainer.transform, true);
            symbol.SetActive(false);
            
            return symbol;
        }
        
        return symbolPools[type].Dequeue();
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

    // Updated coin effect method based on chitin symbol patterns
    public void PlayCoinEffectFromWorldPosition(Vector3 startPosition, int coinAmount = 5)
    {
        Debug.Log($"Playing coin effect with {coinAmount} coins at {startPosition}");
        
        if (coinSymbolPrefab == null)
        {
            Debug.LogError("Coin symbol prefab not assigned!");
            return;
        }
        
        // Find the player to use as reference point for collection
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found! Cannot play coin effect.");
            return;
        }
        
        // Find player inventory for XP gain
        PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
        
        // Initialize the pool if needed
        if (!symbolPools.ContainsKey("coin"))
        {
            InitializePool("coin", coinSymbolPrefab);
        }
        
        // Spawn multiple coins with staggered timing
        StartCoroutine(SpawnWorldCoinSymbols(startPosition, coinAmount, player));
        
        // Play sound effect
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("CoinCollect");
        }
    }

    // Create coins in world space similar to chitin symbols
    private IEnumerator SpawnWorldCoinSymbols(Vector3 startPosition, int count, GameObject player)
    {
        // Get reference to the camera
        Camera mainCamera = Camera.main;
        
        for (int i = 0; i < count; i++)
        {
            if (!symbolPools.ContainsKey("coin") || symbolPools["coin"].Count == 0)
            {
                Debug.LogError("Coin symbol pool is empty!");
                yield break;
            }
            
            // Get coin from pool
            GameObject coinSymbol = symbolPools["coin"].Dequeue();
            
            // Reset and activate
            coinSymbol.SetActive(true);
            coinSymbol.transform.SetParent(null); // Ensure it's in world space
            
            // Random position around the starting point
            Vector3 randomOffset = Random.insideUnitSphere * 0.5f;
            coinSymbol.transform.position = startPosition + randomOffset;
            
            // Set larger scale for visibility in world
            coinSymbol.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // Make sure the sprite renderer is visible
            SpriteRenderer renderer = coinSymbol.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
                renderer.sortingOrder = 100;
            }
            
            // Calculate target position (coin counter in screen space)
            Vector3 targetPos = Vector3.zero;
            if (coinPanel != null)
            {
                // Convert UI position to world space
                RectTransform rt = coinPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Use a screen position approach
                    Vector3 screenPoint = mainCamera.WorldToScreenPoint(rt.position);
                    targetPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 10f));
                }
                else
                {
                    // Fallback - use fixed position in camera's forward direction
                    targetPos = mainCamera.transform.position + mainCamera.transform.forward * 5f;
                    targetPos.y += 1f; // Slightly above center
                    targetPos.x += 2f; // Right side
                }
            }
            else
            {
                // Use top-right of screen as fallback
                targetPos = mainCamera.ViewportToWorldPoint(new Vector3(0.9f, 0.9f, 10f));
            }
            
            // Start animation coroutine
            StartCoroutine(AnimateCoinToCounter(coinSymbol, targetPos, player));
            
            // Slight delay between spawns
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Animate a coin symbol to the counter
    private IEnumerator AnimateCoinToCounter(GameObject coinSymbol, Vector3 targetPos, GameObject player)
    {
        if (coinSymbol == null) yield break;
        
        Vector3 startPos = coinSymbol.transform.position;
        float duration = 1.5f;
        float elapsed = 0f;
        
        // Animation - move in arc toward player then to counter
        while (elapsed < duration && coinSymbol != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // First half of animation - move toward player with arc
            if (t < 0.5f)
            {
                float normalizedT = t * 2f; // rescale to 0-1 for first half
                
                // Calculate arc height
                float arcHeight = Mathf.Sin(normalizedT * Mathf.PI) * 1.5f;
                
                // Lerp position with arc
                Vector3 lerpPos = Vector3.Lerp(startPos, player.transform.position, normalizedT);
                lerpPos.y += arcHeight;
                
                coinSymbol.transform.position = lerpPos;
            }
            // Second half - move to counter
            else
            {
                float normalizedT = (t - 0.5f) * 2f; // rescale to 0-1 for second half
                
                // Start position is player position
                Vector3 secondStartPos = player.transform.position;
                
                // Calculate arc height
                float arcHeight = Mathf.Sin(normalizedT * Mathf.PI) * 1.5f;
                
                // Lerp position with arc
                Vector3 lerpPos = Vector3.Lerp(secondStartPos, targetPos, normalizedT);
                lerpPos.y += arcHeight;
                
                coinSymbol.transform.position = lerpPos;
            }
            
            // Make sure it's always facing the camera
            coinSymbol.transform.rotation = cameraTransform.rotation;
            
            yield return null;
        }
        
        // Make sure the coin counter is updated
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            // Play the collection sound
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound("CoinCollect");
            }
        }
        
        // Return to pool
        if (coinSymbol != null)
        {
            coinSymbol.SetActive(false);
            symbolPools["coin"].Enqueue(coinSymbol);
        }
    }
} 