using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CandyCoded.HapticFeedback;
using UnityEngine.UI;

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

    [Header("UI References")]
    [SerializeField] private RectTransform safeAreaTransform; // Reference to UIHelper's SafeArea

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

    public float ChitinFlyDuration => chitinFlyDuration;

    private void Awake()
    {
        symbolPools = new Dictionary<string, Queue<GameObject>>();
        InitializePool("heal", healingSymbolPrefab);
        InitializePool("chitin", chitinSymbolPrefab);
        InitializePool("crumb", crumbSymbolPrefab);
        InitializePool("xp", xpSymbolPrefab);
        InitializePool("coin", coinSymbolPrefab);
        
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
        
        // Use larger pool size for XP symbols
        int poolSize = type == "xp" ? 50 : poolSizePerType;
        
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
        
        // Get reference to SafeArea from UIHelper if not set
        if (safeAreaTransform == null)
        {
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null && uiHelper.SafeArea != null)
            {
                safeAreaTransform = uiHelper.SafeArea.GetComponent<RectTransform>();
                Debug.Log("Found SafeArea reference from UIHelper");
            }
            else
            {
                Debug.LogWarning("Could not find SafeArea reference - UI animations may not display correctly");
            }
        }
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
        
        // Get reference to UI Helper and Canvas if not already set
        if (safeAreaTransform == null)
        {
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null && uiHelper.SafeArea != null)
            {
                safeAreaTransform = uiHelper.SafeArea.GetComponent<RectTransform>();
            }
        }
        
        if (safeAreaTransform == null)
        {
            Debug.LogError("Cannot spawn chitin effect - SafeArea reference is missing");
            yield break;
        }
        
        // Get canvas reference
        Canvas canvas = safeAreaTransform.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Cannot spawn chitin effect - Canvas not found");
            yield break;
        }
        
        // Use screen position for chitin panel
        Vector2 screenTopRight = new Vector2(Screen.width - 200f, Screen.height - 150f);
        
        for (int i = 0; i < count; i++)
        {
            if (symbolPools["chitin"].Count == 0) yield break;
            
            GameObject symbol = symbolPools["chitin"].Dequeue();
            
            // Parent to the SafeArea in the Canvas
            symbol.transform.SetParent(safeAreaTransform, false);
            
            // Create random offset around top right corner
            Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;
            Vector2 spawnScreenPos = screenTopRight + randomOffset;
            
            // Set UI position
            RectTransform symbolRect = symbol.GetComponent<RectTransform>();
            if (symbolRect == null)
            {
                symbolRect = symbol.AddComponent<RectTransform>();
            }
            
            // Convert screen position to canvas position
            Vector2 anchoredPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                safeAreaTransform, 
                spawnScreenPos, 
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out anchoredPos
            );
            
            symbolRect.anchoredPosition = anchoredPos;
            
            // Set active and scale appropriately for UI
            symbol.SetActive(true);
            
            // Adjust scale for canvas space (may need tweaking)
            symbolRect.localScale = new Vector3(1f, 1f, 1f);
            
            // Start animated collection
            StartCoroutine(AnimateResourceCollectionInUI(symbol, "chitin"));
            
            yield return new WaitForSeconds(chitinSpawnDelay);
        }
    }

    // New method for UI-based animation
    private IEnumerator AnimateResourceCollectionInUI(GameObject symbol, string type)
    {
        if (safeAreaTransform == null || symbol == null)
        {
            Debug.LogError("Cannot animate resource - SafeArea or symbol is null");
            yield break;
        }
        
        // Get canvas
        Canvas canvas = safeAreaTransform.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Cannot animate resource - Canvas not found");
            yield break;
        }
        
        // Get the RectTransform
        RectTransform symbolRect = symbol.GetComponent<RectTransform>();
        if (symbolRect == null)
        {
            Debug.LogError("Symbol does not have a RectTransform component");
            yield break;
        }
        
        // Get the starting anchored position
        Vector2 startPos = symbolRect.anchoredPosition;
        
        // Get the target panel based on resource type
        GameObject targetPanel = null;
        if (type == "chitin")
        {
            targetPanel = chitinPanel;
        }
        else if (type == "crumb")
        {
            targetPanel = crumbPanel;
        }
        
        if (targetPanel == null)
        {
            Debug.LogError($"Target panel for {type} is not set");
            yield break;
        }
        
        // Get target position (center of the panel in canvas space)
        RectTransform targetRect = targetPanel.GetComponent<RectTransform>();
        Vector2 targetPos;
        
        // Convert panel position to canvas space
        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        Vector3 worldPos = (corners[0] + corners[2]) / 2f; // Center of panel
        
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, 
            worldPos
        );
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            safeAreaTransform,
            screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out targetPos
        );
        
        SpriteRenderer spriteRenderer = symbol.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            // For UI, we might use Image instead of SpriteRenderer
            Image image = symbol.GetComponent<Image>();
            if (image == null && symbol.transform.childCount > 0)
            {
                image = symbol.GetComponentInChildren<Image>();
            }
            
            if (image == null)
            {
                Debug.LogError("No renderer found on symbol");
                yield break;
            }
            
            Color startColor = image.color;
            float elapsed = 0f;
            
            // Create a curved path in canvas space
            Vector2 midPoint = Vector2.Lerp(startPos, targetPos, 0.5f);
            midPoint += Vector2.up * 100f; // Arc height in UI space
            
            while (elapsed < chitinFlyDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / chitinFlyDuration;
                float curveValue = chitinFlyCurve.Evaluate(normalizedTime);
                
                // Use bezier curve movement in canvas space
                Vector2 a = Vector2.Lerp(startPos, midPoint, curveValue);
                Vector2 b = Vector2.Lerp(midPoint, targetPos, curveValue);
                symbolRect.anchoredPosition = Vector2.Lerp(a, b, curveValue);
                
                // Fade out at the end
                if (normalizedTime > 0.8f)
                {
                    float alpha = 1f - ((normalizedTime - 0.8f) / 0.2f);
                    image.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                }
                
                if (normalizedTime > 0.9f && normalizedTime < 0.92f)
                {
                    SoundEffectManager.Instance.PlaySound("SymbolToNest", Camera.main.transform.position, true);
                    
                    // Pulse the UI panel
                    if (targetPanel != null)
                    {
                        StartCoroutine(PulseUIElement(targetRect));
                    }
                }
                
                yield return null;
            }
            
            // Reset and return to pool
            symbol.SetActive(false);
            image.color = startColor;
            symbol.transform.SetParent(transform, false); // Return to original parent
            symbolPools[type].Enqueue(symbol);
        }
        else
        {
            // Code path for SpriteRenderer objects
            Color startColor = spriteRenderer.color;
            float elapsed = 0f;
            
            // Create a curved path in canvas space
            Vector2 midPoint = Vector2.Lerp(startPos, targetPos, 0.5f);
            midPoint += Vector2.up * 100f; // Arc height in UI space
            
            while (elapsed < chitinFlyDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / chitinFlyDuration;
                float curveValue = chitinFlyCurve.Evaluate(normalizedTime);
                
                // Use bezier curve movement in canvas space
                Vector2 a = Vector2.Lerp(startPos, midPoint, curveValue);
                Vector2 b = Vector2.Lerp(midPoint, targetPos, curveValue);
                symbolRect.anchoredPosition = Vector2.Lerp(a, b, curveValue);
                
                // Fade out at the end
                if (normalizedTime > 0.8f)
                {
                    float alpha = 1f - ((normalizedTime - 0.8f) / 0.2f);
                    spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                }
                
                if (normalizedTime > 0.9f && normalizedTime < 0.92f)
                {
                    SoundEffectManager.Instance.PlaySound("SymbolToNest", Camera.main.transform.position, true);
                    
                    // Pulse the UI panel
                    if (targetPanel != null)
                    {
                        StartCoroutine(PulseUIElement(targetRect));
                    }
                }
                
                yield return null;
            }
            
            // Reset and return to pool
            symbol.SetActive(false);
            spriteRenderer.color = startColor;
            symbol.transform.SetParent(transform, false); // Return to original parent
            symbolPools[type].Enqueue(symbol);
        }
        
        // Add haptic feedback
        HapticFeedback.LightFeedback();
    }

    public IEnumerator SpawnCrumbCollectEffect(int count)
    {
        if (activeNestTransform == null) yield break;
        
        // Get reference to UI Helper and Canvas if not already set
        if (safeAreaTransform == null)
        {
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null && uiHelper.SafeArea != null)
            {
                safeAreaTransform = uiHelper.SafeArea.GetComponent<RectTransform>();
            }
        }
        
        if (safeAreaTransform == null)
        {
            Debug.LogError("Cannot spawn crumb effect - SafeArea reference is missing");
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
} 