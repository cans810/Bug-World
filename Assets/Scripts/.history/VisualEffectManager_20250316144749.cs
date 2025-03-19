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

    public float ChitinFlyDuration => chitinFlyDuration;

    private void Awake()
    {
        symbolPools = new Dictionary<string, Queue<GameObject>>();
        InitializePool("heal", healingSymbolPrefab);
        InitializePool("chitin", chitinSymbolPrefab);
        InitializePool("crumb", crumbSymbolPrefab);
        InitializePool("xp", xpSymbolPrefab);
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
        for (int i = 0; i < poolSizePerType; i++)
        {
            GameObject symbol = Instantiate(prefab);

            // Set all symbols to base scale (0.05)
            symbol.transform.localScale = baseScale;

            symbol.transform.SetParent(poolContainer.transform, true);

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

    private IEnumerator SpawnCrumbCollectEffect(int count)
    {
        if (nestTransform == null) yield break;

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

    public void PlayXPGainEffect(Vector3 nestPosition, int xpAmount)
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
        
        // Calculate number of symbols - 5 per chitin (xpAmount / 5 = number of chitins)
        int chitinCount = xpAmount / 5;
        int symbolCount = chitinCount * 5;
        
        // Start spawning symbols - use the nest position as the starting point
        StartCoroutine(SpawnXPSymbols(nestPosition, symbolCount, xpAmount));
    }

    private IEnumerator SpawnXPSymbols(Vector3 nestPosition, int count, int totalXPAmount)
    {
        // Define the target screen position (top left for XP counter)
        // Use the same Y position as chitin symbols for consistency
        Vector2 screenTopLeft = new Vector2(100f, Screen.height - spawnOffsetY);
        
        // Calculate XP per symbol for incremental counting
        int xpPerSymbol = incrementXPOneByOne ? 1 : totalXPAmount / count;
        if (xpPerSymbol < 1) xpPerSymbol = 1;
        
        for (int i = 0; i < count; i++)
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
            
            // Store the XP value this symbol represents
            symbol.name = $"XPSymbol_{xpPerSymbol}";
            
            // Start the animation to the UI position
            StartCoroutine(AnimateXPSymbolToScreen(symbol, screenTopLeft, xpPerSymbol));
            
            // Stagger the spawns
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator AnimateXPSymbolToScreen(GameObject symbol, Vector2 targetScreenPos, int xpValue)
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
        
        // Use the same duration as chitin for consistency
        float duration = chitinFlyDuration;
        
        // Track if we've played the sound yet
        bool hasSoundPlayed = false;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            float curveValue = chitinFlyCurve.Evaluate(normalizedTime);
            
            // IMPORTANT: Always recalculate the target position each frame
            // This ensures the symbol always goes to the top-left of the screen
            // regardless of player movement
            Vector2 currentTargetScreenPos = new Vector2(100f, Screen.height - targetOffsetY);
            Ray ray = Camera.main.ScreenPointToRay(currentTargetScreenPos);
            float distanceToCamera = 10f;
            Vector3 targetPos = ray.GetPoint(distanceToCamera);
            
            // Adjust the Y position down a bit
            targetPos.y -= 0.5f; // Lower the target position by 0.5 units
            
            // Create a curved path with midpoint above
            Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
            midPoint += Vector3.up * Random.Range(0.5f, 1.5f);
            
            // Use the same bezier curve movement as chitin
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
            
            // Play sound halfway through the animation
            if (!hasSoundPlayed && normalizedTime >= 0.5f)
            {
                PlayXPSound();
                hasSoundPlayed = true;
            }
            
            yield return null;
        }
        
        // Play sound at the end if it hasn't played yet (shouldn't happen, but just in case)
        if (!hasSoundPlayed)
        {
            PlayXPSound();
        }
        
        // When the symbol reaches its destination, increment the XP counter
        if (incrementXPOneByOne)
        {
            // Find the player inventory and add XP incrementally
            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
            {
                // Add XP one by one
                playerInventory.AddXPIncremental(xpValue);
            }
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
} 