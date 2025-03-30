using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VisualEffectManager : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private GameObject healingSymbolPrefab;
    [SerializeField] private GameObject shineSymbolPrefab;
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
    [SerializeField] private Vector2 scaleMultiplierRange = new Vector2(0.5f, 0.7f);

    private Dictionary<string, Queue<GameObject>> symbolPools;
    private Transform cameraTransform;

    public LevelUpPanelHelper levelUpPanelHelper;

    private void Awake()
    {
        symbolPools = new Dictionary<string, Queue<GameObject>>();
        InitializePool("heal", healingSymbolPrefab);
        InitializePool("shine", shineSymbolPrefab);
        
        // Use a larger pool size for level up symbols
        InitializePoolWithSize("levelup", levelupSymbolPrefab, 40);
        
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
    
    private void InitializePoolWithSize(string type, GameObject prefab, int customPoolSize)
    {
        if (prefab == null) return;

        // Create a container
        GameObject poolContainer = new GameObject($"{type}Pool");
        poolContainer.transform.SetParent(transform);

        Queue<GameObject> pool = new Queue<GameObject>();
        for (int i = 0; i < customPoolSize; i++)
        {
            GameObject symbol = Instantiate(prefab);
            
            // Set all symbols to base scale
            symbol.transform.localScale = baseScale;
            
            symbol.transform.SetParent(poolContainer.transform);
            symbol.SetActive(false);
            pool.Enqueue(symbol);
        }
        symbolPools[type] = pool;
    }

    public void SpawnEffect(string effectType, Vector3 position, int count = 1)
    {
        if (!symbolPools.ContainsKey(effectType)) return;

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

    private Vector3 GetRandomPositionAround(Vector3 center)
    {
        Vector2 randomCircle = Random.insideUnitCircle;
        float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
        return center + new Vector3(randomCircle.x * radius, 0.5f, randomCircle.y * radius);
    }

    private IEnumerator AnimateSymbol(GameObject symbol, string type)
    {
        float elapsed = 0f;
        Vector3 startPos = symbol.transform.position;
        Vector3 startScale = symbol.transform.localScale;
        
        // Get a random scale multiplier to vary the size
        float scaleMultiplier = Random.Range(scaleMultiplierRange.x, scaleMultiplierRange.y);
        symbol.transform.localScale = startScale * scaleMultiplier;
        
        SpriteRenderer renderer = symbol.GetComponent<SpriteRenderer>();
        Color startColor = Color.white;
        if (renderer != null)
        {
            startColor = renderer.color;
        }
        
        // Rotation variables
        float rotSpeed = useRotation ? Random.Range(30f, 60f) : 0f;
        float rotDirection = Random.value > 0.5f ? 1f : -1f;
        
        while (elapsed < symbolDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / symbolDuration;
            
            // Move upward
            float newY = startPos.y + floatSpeed * normalizedTime;
            symbol.transform.position = new Vector3(startPos.x, newY, startPos.z);
            
            // Fade out
            if (renderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, normalizedTime);
                renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            
            // Optional rotation
            if (useRotation)
            {
                symbol.transform.Rotate(Vector3.forward, rotSpeed * rotDirection * Time.deltaTime);
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
        symbolPools[type].Enqueue(symbol);
    }

    public void PlayLevelUpEffect(Vector3 position)
    {
        Debug.Log("Playing level up effect at " + position);
        if (levelupSymbolPrefab == null) return;

        // Spawn multiple level up symbols
        int symbolCount = 15;
        StartCoroutine(SpawnLevelUpSymbols(position, symbolCount));
        
        // Show the level up panel after a delay
        if (levelUpPanelHelper != null)
        {
            StartCoroutine(ShowLevelUpPanelAfterDelay(0.5f));
        }
    }

    private IEnumerator SpawnLevelUpSymbols(Vector3 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (symbolPools["levelup"].Count == 0) yield break;

            GameObject symbol = symbolPools["levelup"].Dequeue();
            symbol.transform.position = GetRandomPositionAround(position);
            symbol.SetActive(true);
            
            StartCoroutine(AnimateLevelUpSymbol(symbol));
            
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator AnimateLevelUpSymbol(GameObject symbol)
    {
        float duration = 2f;
        float elapsed = 0f;
        
        Vector3 startPos = symbol.transform.position;
        Vector3 startScale = symbol.transform.localScale;
        Vector3 targetScale = startScale * 1.5f;
        
        SpriteRenderer renderer = symbol.GetComponent<SpriteRenderer>();
        Color startColor = Color.white;
        if (renderer != null)
        {
            startColor = renderer.color;
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            
            // Move upward
            float newY = startPos.y + (2.5f * normalizedTime);
            symbol.transform.position = new Vector3(startPos.x, newY, startPos.z);
            
            // Scale up then down
            if (normalizedTime < 0.5f)
            {
                symbol.transform.localScale = Vector3.Lerp(startScale, targetScale, normalizedTime * 2);
            }
            else
            {
                symbol.transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, (normalizedTime - 0.5f) * 2);
            }
            
            // Fade out at the end
            if (renderer != null && normalizedTime > 0.75f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (normalizedTime - 0.75f) * 4);
                renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
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

    private IEnumerator ShowLevelUpPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (levelUpPanelHelper != null)
        {
            // Just show the panel, don't worry about level number
            levelUpPanelHelper.ShowUpperPanel();
            levelUpPanelHelper.InstantiateRewards();
        }
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