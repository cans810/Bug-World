using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIVisualEffectController : MonoBehaviour
{
    public GameObject attributeSymbolPrefab;
    public GameObject chitinSymbolPrefab;
    public GameObject crumbSymbolPrefab;
    public GameObject xpSymbolPrefab;
    public GameObject coinSymbolPrefab;

    public GameObject coinPanel;
    public GameObject xpPanel;
    public GameObject chitinPanel;
    public GameObject crumbPanel;

    [Header("Animation Settings")]
    [SerializeField] private float flyDuration = 1.0f;
    [SerializeField] private float xpFlyDuration = 0.8f;
    [SerializeField] private int maxSymbolsPerDeposit = 10;
    [SerializeField] private float spawnDelay = 0.1f;

    // Called when chitin is deposited - SIMPLIFIED VERSION
    public void PlayChitinDepositEffect(int chitinAmount, int xpAmount)
    {
        Debug.Log($"PlayChitinDepositEffect called with {chitinAmount} chitin");
        
        // Limit the number of symbols
        int symbolCount = Mathf.Min(chitinAmount, maxSymbolsPerDeposit);
        
        // Simple version - just create the symbols and move them
        StartCoroutine(SimpleChitinToCenter(symbolCount, xpAmount));
    }
    
    private IEnumerator SimpleChitinToCenter(int count, int xpAmount)
    {
        // Get screen center
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        
        // Get chitin panel position
        if (chitinPanel == null)
        {
            Debug.LogError("Chitin panel is null!");
            yield break;
        }
        
        RectTransform chitinRectTransform = chitinPanel.GetComponent<RectTransform>();
        if (chitinRectTransform == null)
        {
            Debug.LogError("Chitin panel has no RectTransform!");
            yield break;
        }
        
        // Get panel position in screen space
        Vector2 panelPos = RectTransformUtility.WorldToScreenPoint(null, chitinRectTransform.position);
        Debug.Log($"Chitin panel position: {panelPos}, Screen center: {screenCenter}");
        
        for (int i = 0; i < count; i++)
        {
            // Create chitin symbol
            if (chitinSymbolPrefab == null)
            {
                Debug.LogError("Chitin symbol prefab is null!");
                yield break;
            }
            
            // Create the symbol as a direct child of the canvas
            GameObject symbol = Instantiate(chitinSymbolPrefab, transform);
            
            // Explicitly set up the RectTransform
            RectTransform rt = symbol.GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogError("Created symbol has no RectTransform!");
                Destroy(symbol);
                continue;
            }
            
            // Set position to chitin panel
            rt.position = chitinRectTransform.position;
            
            // Set size explicitly
            rt.sizeDelta = new Vector2(50, 50);
            
            // Make sure it's visible - set alpha explicitly
            Image img = symbol.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.white; // Fully opaque
            }
            else
            {
                Debug.LogError("Symbol has no Image component!");
            }
            
            Debug.Log($"Created chitin symbol {i+1} at position {rt.position}");
            
            // Start movement immediately
            StartCoroutine(MoveToCenter(symbol, rt.position, screenCenter));
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // Wait for animations to complete before spawning XP
        yield return new WaitForSeconds(flyDuration + 0.2f);
        
        // Spawn XP symbols
        if (xpAmount > 0)
        {
            SpawnXPSymbols(screenCenter, xpAmount);
        }
    }
    
    private IEnumerator MoveToCenter(GameObject symbol, Vector3 startPos, Vector2 centerPos)
    {
        float startTime = Time.time;
        float duration = flyDuration;
        RectTransform rt = symbol.GetComponent<RectTransform>();
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            rt.position = Vector3.Lerp(startPos, centerPos, t);
            yield return null;
        }
        
        // Ensure final position
        rt.position = centerPos;
        Debug.Log($"Symbol reached center position: {rt.position}");
        
        // Destroy the symbol
        Destroy(symbol);
    }
    
    private void SpawnXPSymbols(Vector2 centerPos, int amount)
    {
        Debug.Log($"Spawning {amount} XP symbols at {centerPos}");
        
        if (xpSymbolPrefab == null)
        {
            Debug.LogError("XP symbol prefab is null!");
            return;
        }
        
        if (xpPanel == null)
        {
            Debug.LogError("XP panel is null!");
            return;
        }
        
        StartCoroutine(CreateXPSymbols(centerPos, amount));
    }
    
    private IEnumerator CreateXPSymbols(Vector2 centerPos, int count)
    {
        // Get XP panel position
        RectTransform xpRectTransform = xpPanel.GetComponent<RectTransform>();
        Vector2 xpPanelPos = xpRectTransform.position;
        
        for (int i = 0; i < count; i++)
        {
            // Create XP symbol
            GameObject symbol = Instantiate(xpSymbolPrefab, transform);
            
            // Set position
            RectTransform rt = symbol.GetComponent<RectTransform>();
            rt.position = centerPos;
            
            // Set size
            rt.sizeDelta = new Vector2(40, 40);
            
            // Make sure it's visible
            Image img = symbol.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.white;
            }
            
            // Move to XP panel
            StartCoroutine(MoveToXP(symbol, centerPos, xpPanelPos));
            
            yield return new WaitForSeconds(0.05f);
        }
        
        // Play sound
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("XPGained");
        }
    }
    
    private IEnumerator MoveToXP(GameObject symbol, Vector3 startPos, Vector2 endPos)
    {
        float startTime = Time.time;
        float duration = xpFlyDuration;
        RectTransform rt = symbol.GetComponent<RectTransform>();
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            
            // Add slight arc to movement
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * 50f; // Arc height
            
            rt.position = pos;
            yield return null;
        }
        
        // Final position
        rt.position = endPos;
        
        // Flash XP panel
        Image panelImage = xpPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            StartCoroutine(FlashPanel(panelImage));
        }
        
        // Destroy symbol
        Destroy(symbol);
    }
    
    private IEnumerator FlashPanel(Image panelImage)
    {
        Color originalColor = panelImage.color;
        Color flashColor = new Color(1f, 1f, 0.5f, originalColor.a);
        
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            panelImage.color = Color.Lerp(flashColor, originalColor, t);
            yield return null;
        }
        
        panelImage.color = originalColor;
    }
}
