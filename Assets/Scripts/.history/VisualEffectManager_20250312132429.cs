using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VisualEffectManager : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private GameObject healingSymbolPrefab;
    [SerializeField] private GameObject chitinSymbolPrefab;
    [SerializeField] private int poolSizePerType = 10;
    [SerializeField] private float symbolDuration = 1.5f;
    [SerializeField] private float minSpawnRadius = 0.3f;
    [SerializeField] private float maxSpawnRadius = 0.8f;
    [SerializeField] private float floatSpeed = 3f;
    [SerializeField] private float fadeSpeed = 1.5f;

    [Header("Additional Effects")]
    [SerializeField] private bool useRotation = true;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private Vector3 baseScale = new Vector3(0.03f, 0.03f, 0.03f);
    [SerializeField] private Vector2 scaleMultiplierRange = new Vector2(0.5f, 0.7f);

    [Header("Chitin Effects")]
    [SerializeField] private Transform chitinPanel;
    [SerializeField] private Transform nestTransform;
    [SerializeField] private float chitinFlyDuration = 1f;
    [SerializeField] private float chitinSpawnDelay = 0.1f;
    [SerializeField] private AnimationCurve chitinFlyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float spreadRadius = 50f;

    private Dictionary<string, Queue<GameObject>> symbolPools;
    private Transform cameraTransform;

    private void Awake()
    {
        symbolPools = new Dictionary<string, Queue<GameObject>>();
        InitializePool("heal", healingSymbolPrefab);
        InitializePool("chitin", chitinSymbolPrefab);
        cameraTransform = Camera.main.transform;
    }

    private void InitializePool(string type, GameObject prefab)
    {
        if (prefab == null) return;

        Queue<GameObject> pool = new Queue<GameObject>();
        for (int i = 0; i < poolSizePerType; i++)
        {
            GameObject symbol = Instantiate(prefab, transform);
            symbol.transform.localScale = baseScale;
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
        if (chitinPanel == null || nestTransform == null) yield break;

        for (int i = 0; i < count; i++)
        {
            if (symbolPools["chitin"].Count == 0) yield break;

            GameObject symbol = symbolPools["chitin"].Dequeue();
            
            // Start at the chitin panel position
            Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;
            Vector3 screenPos = chitinPanel.position + new Vector3(randomOffset.x, randomOffset.y, 0);
            
            symbol.transform.position = screenPos;
            symbol.transform.localScale = baseScale;
            symbol.SetActive(true);

            // Start the collection animation
            StartCoroutine(AnimateChitinCollection(symbol));

            // Add delay between spawns
            yield return new WaitForSeconds(chitinSpawnDelay);
        }
    }

    private IEnumerator AnimateChitinCollection(GameObject symbol)
    {
        Vector3 startPos = symbol.transform.position;
        Vector3 endPos = nestTransform.position;
        SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
        Color startColor = spriteRenderer.color;
        float elapsed = 0f;

        // Initial random rotation
        float randomRotation = Random.Range(0f, 360f);
        float rotationSpeed = Random.Range(-180f, 180f);

        while (elapsed < chitinFlyDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / chitinFlyDuration;
            float curveValue = chitinFlyCurve.Evaluate(normalizedTime);

            // Position
            symbol.transform.position = Vector3.Lerp(startPos, endPos, curveValue);

            // Add some swirling motion
            if (useRotation)
            {
                randomRotation += rotationSpeed * Time.deltaTime;
                symbol.transform.rotation = Quaternion.Euler(0, 0, randomRotation);
            }

            // Scale pulse effect
            float scale = 1f + Mathf.Sin(normalizedTime * Mathf.PI * 2) * 0.2f;
            symbol.transform.localScale = baseScale * scale;

            yield return null;
        }

        // Final fade out at the nest
        float fadeDuration = 0.2f;
        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // Reset and return to pool
        spriteRenderer.color = startColor;
        symbol.transform.localScale = baseScale;
        symbol.SetActive(false);
        symbolPools["chitin"].Enqueue(symbol);
    }

    private System.Collections.IEnumerator AnimateSymbol(GameObject symbol, string effectType)
    {
        float elapsed = 0f;
        Vector3 startPos = symbol.transform.position;
        SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
        Color startColor = spriteRenderer.color;
        
        // Random scale multiplier
        float randomScaleMultiplier = Random.Range(scaleMultiplierRange.x, scaleMultiplierRange.y);
        symbol.transform.localScale = baseScale * randomScaleMultiplier;

        while (elapsed < symbolDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / symbolDuration;

            // Move upward
            symbol.transform.position = startPos + Vector3.up * (floatSpeed * normalizedTime);

            // Rotate if enabled
            if (useRotation)
            {
                float randomRotation = Random.Range(0f, 360f);
                symbol.transform.rotation = cameraTransform.rotation * Quaternion.Euler(0, 0, randomRotation);
            }

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
} 