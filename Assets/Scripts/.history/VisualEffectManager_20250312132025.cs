using UnityEngine;
using System.Collections;

public class VisualEffectManager : MonoBehaviour
{
    // ... existing fields ...

    [Header("Chitin Effects")]
    [SerializeField] private Transform chitinPanel;
    [SerializeField] private Transform nestTransform;
    [SerializeField] private float chitinFlyDuration = 1f;
    [SerializeField] private float chitinSpawnDelay = 0.1f; // Delay between each chitin spawn
    [SerializeField] private AnimationCurve chitinFlyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float spreadRadius = 50f; // How far chitins spread in UI space before flying

    public void SpawnEffect(string effectType, Vector3 position, int count = 1)
    {
        if (!symbolPools.ContainsKey(effectType)) return;

        if (effectType == "chitin")
        {
            StartCoroutine(SpawnChitinCollectEffect(count));
        }
        else
        {
            // Original healing effect logic
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

    // ... existing methods ...
} 