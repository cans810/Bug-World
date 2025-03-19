using UnityEngine;
using System.Collections.Generic;

public class HealingEffectManager : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private GameObject healingSymbolPrefab; // Assign a prefab with your + symbol sprite
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float symbolDuration = 1f;
    [SerializeField] private float minSpawnRadius = 0.5f;
    [SerializeField] private float maxSpawnRadius = 1.5f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float fadeSpeed = 1f;

    private Queue<GameObject> symbolPool;
    private Transform cameraTransform;

    private void Awake()
    {
        // Initialize object pool
        symbolPool = new Queue<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject symbol = Instantiate(healingSymbolPrefab, transform);
            symbol.SetActive(false);
            symbolPool.Enqueue(symbol);
        }

        cameraTransform = Camera.main.transform;
    }

    public void SpawnHealingEffect(Vector3 position)
    {
        if (symbolPool.Count == 0) return;

        GameObject symbol = symbolPool.Dequeue();
        symbol.transform.position = GetRandomPositionAround(position);
        symbol.SetActive(true);

        // Make the symbol face the camera
        symbol.transform.rotation = Quaternion.LookRotation(cameraTransform.forward);

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

            // Always face camera
            symbol.transform.rotation = Quaternion.LookRotation(cameraTransform.forward);

            yield return null;
        }

        // Reset and return to pool
        spriteRenderer.color = startColor;
        symbol.SetActive(false);
        symbolPool.Enqueue(symbol);
    }
} 