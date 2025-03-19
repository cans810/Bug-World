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

    private Dictionary<string, Queue<GameObject>> symbolPools;
    private Transform cameraTransform;

    private void Awake()
    {
        symbolPools = new Dictionary<string, Queue<GameObject>>();
        InitializePool("heal", healingSymbolPrefab);
        InitializePool("chitin", chitinSymbolPrefab);
        InitializePool("crumb", crumbSymbolPrefab);
        cameraTransform = Camera.main.transform;
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
            
            symbol.transform.position = playerPosition + randomOffset;
            symbol.SetActive(true);
            
            // Face the symbol toward the camera
            symbol.transform.rotation = cameraTransform.rotation;
            
            // Start a specialized animation for level up
            StartCoroutine(AnimateLevelUpSymbol(symbol, i * 0.1f, () => {
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

    private IEnumerator AnimateLevelUpSymbol(GameObject symbol, float delay, System.Action onComplete)
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
        Vector3 startPos = symbol.transform.position;
        Vector3 startScale = symbol.transform.localScale;
        
        // Set initial scale (slightly smaller)
        symbol.transform.localScale = startScale * 0.5f;
        
        // Rise up and scale animation
        float animDuration = 1.8f;
        float elapsed = 0f;
        
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / animDuration;
            
            // Move directly upward with increasing speed
            float heightGain = floatSpeed * normalizedTime * normalizedTime * 3f; // Increased height gain
            symbol.transform.position = startPos + Vector3.up * heightGain;
            
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
} 