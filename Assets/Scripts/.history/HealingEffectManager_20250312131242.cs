using UnityEngine;
using System.Collections.Generic;

public class HealingEffectManager : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private GameObject healingSymbolPrefab; // Assign a prefab with your + symbol sprite
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float symbolDuration = 1f;
    [SerializeField] private float minSpawnRadius = 0.3f;
    [SerializeField] private float maxSpawnRadius = 0.8f;
    [SerializeField] private float floatSpeed = 3f;
    [SerializeField] private float fadeSpeed = 1.5f;

    [Header("Additional Effects")]
    [SerializeField] private bool useRotation = true;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private Vector3 baseScale = new Vector3(0.03f, 0.03f, 0.03f);
    [SerializeField] private Vector2 scaleMultiplierRange = new Vector2(0.8f, 1f);

    private Queue<GameObject> symbolPool;
    private Transform cameraTransform;

    private void Awake()
    {
        // Initialize object pool
        symbolPool = new Queue<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject symbol = Instantiate(healingSymbolPrefab, transform);
            symbol.transform.localScale = baseScale; // Set initial scale
            symbol.SetActive(false);
            symbolPool.Enqueue(symbol);
        }

        cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        // Make all active symbols face the camera
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                child.rotation = cameraTransform.rotation;
            }
        }
    }

    public void SpawnHealingEffect(Vector3 position)
    {
        if (symbolPool.Count == 0) return;

        GameObject symbol = symbolPool.Dequeue();
        symbol.transform.position = GetRandomPositionAround(position);
        symbol.SetActive(true);

        // Initial rotation (LateUpdate will handle the rest)
        symbol.transform.rotation = cameraTransform.rotation;

        StartCoroutine(AnimateSymbol(symbol));
    }

    private Vector3 GetRandomPositionAround(Vector3 center)
    {
        Vector2 randomCircle = Random.insideUnitCircle;
        float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
        return center + new Vector3(randomCircle.x * radius, 0.5f, randomCircle.y * radius);
    }

    private System.Collections.IEnumerator AnimateSymbol(GameObject symbol)
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
        symbol.transform.localScale = baseScale; // Reset to base scale
        symbol.SetActive(false);
        symbolPool.Enqueue(symbol);
    }
} 