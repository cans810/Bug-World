using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab; // Assign your sprite-based arrow prefab in the inspector
    [SerializeField] private float arrowSize = 0.25f;
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    [Header("Target Settings")]
    [SerializeField] private GameObject target;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private bool destroyWhenReached = false;
    [SerializeField] private float reachDistance = 2f;
    
    // Increase padding to keep arrow more centered
    [Header("Screen Positioning")]
    [SerializeField] private float screenEdgePadding = 250f; // Increased from 150f to 250f
    [SerializeField] private bool useBillboardEffect = true; // Make arrow always face camera
    
    private GameObject arrowObject;
    private bool isShowing = false;
    private Transform playerTransform;
    private Camera mainCamera;
    
    private void Awake()
    {
        // Find the player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("ArrowIndicator: Player not found! Make sure the player has the 'Player' tag.");
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("ArrowIndicator: Main camera not found!");
        }
    }
    
    private void Start()
    {
        if (showOnStart && target != null)
        {
            ShowArrow();
        }
    }
    
    private void Update()
    {
        if (isShowing && arrowObject != null && target != null && playerTransform != null)
        {
            UpdateArrowPosition();
        }
    }
    
    private void UpdateArrowPosition()
    {
        // Get target position in screen space
        Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(target.transform.position);
        
        // Calculate screen center
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        // Check if target is behind camera
        bool isBehind = targetScreenPos.z < 0;
        
        // If behind, flip the position
        if (isBehind)
        {
            targetScreenPos.x = Screen.width - targetScreenPos.x;
            targetScreenPos.y = Screen.height - targetScreenPos.y;
        }
        
        // Calculate direction from screen center to target
        Vector2 directionFromCenter = new Vector2(targetScreenPos.x - screenCenter.x, targetScreenPos.y - screenCenter.y);
        
        // Check if target is on screen
        bool isOnScreen = IsOnScreen(targetScreenPos);
        
        // Calculate distance to target for fade/hide logic
        float distance = Vector3.Distance(playerTransform.position, target.transform.position);
        
        // If target is off screen or far enough away, show the arrow
        if (!isOnScreen || (distance > reachDistance))
        {
            // Make arrow visible
            arrowObject.SetActive(true);
            
            Vector3 arrowWorldPos;
            float angle;
            
            if (!isOnScreen)
            {
                // Normalize direction for positioning
                Vector2 normalizedDirection = directionFromCenter.normalized;
                
                // Calculate offset in based on screen dimensions for more centered position
                float offsetMultiplier = 0.5f; // Controls how centered the arrow is (0.5 = halfway from center to edge)
                
                // Calculate screen bounds with padding
                float minX = screenEdgePadding;
                float maxX = Screen.width - screenEdgePadding;
                float minY = screenEdgePadding;
                float maxY = Screen.height - screenEdgePadding;
                
                // Get width/height of the visible area
                float visibleWidth = maxX - minX;
                float visibleHeight = maxY - minY;
                
                // Calculate position more toward center (using offsetMultiplier)
                Vector2 arrowScreenPos = screenCenter + normalizedDirection * Mathf.Min(
                    visibleWidth * offsetMultiplier,
                    visibleHeight * offsetMultiplier
                );
                
                // Ensure position is within the padded screen area
                arrowScreenPos.x = Mathf.Clamp(arrowScreenPos.x, minX, maxX);
                arrowScreenPos.y = Mathf.Clamp(arrowScreenPos.y, minY, maxY);
                
                // Convert to world position
                arrowWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(arrowScreenPos.x, arrowScreenPos.y, 10f));
                
                // Calculate angle - point in direction of target
                angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;
            }
            else
            {
                // If on screen but far away, position near the target
                arrowWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(targetScreenPos.x, targetScreenPos.y, 10f));
                
                // Calculate angle - point toward the player in this case
                Vector2 dirToPlayer = new Vector2(
                    screenCenter.x - targetScreenPos.x,
                    screenCenter.y - targetScreenPos.y
                ).normalized;
                
                angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
            }
            
            // Position the arrow
            arrowObject.transform.position = arrowWorldPos;
            
            // Fix warping by using proper rotation approach
            if (useBillboardEffect)
            {
                // First make sure the arrow is facing the camera
                arrowObject.transform.forward = mainCamera.transform.forward;
                
                // Then apply rotation around the local Z axis (which is now aligned with camera's forward)
                arrowObject.transform.Rotate(0, 0, angle, Space.Self);
            }
            else
            {
                // Simple Z-rotation for 2D arrows
                arrowObject.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            
            // Apply pulsing effect to scale
            float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
            float baseScale = arrowSize * pulse;
            arrowObject.transform.localScale = new Vector3(baseScale, baseScale, baseScale);
        }
        else
        {
            // If target is on screen and close enough, hide the arrow
            arrowObject.SetActive(false);
            
            // If we've reached the target, hide the arrow completely
            if (distance <= reachDistance && destroyWhenReached)
            {
                HideArrow();
                Debug.Log("Player reached target, hiding arrow");
            }
        }
    }
    
    public void ShowArrow()
    {
        Debug.Log("ShowArrow called");
        
        if (target == null)
        {
            Debug.LogError("ArrowIndicator: Cannot show arrow - target is null!");
            return;
        }
        
        if (arrowPrefab == null)
        {
            Debug.LogError("ArrowIndicator: Arrow prefab is null!");
            return;
        }
        
        // Create the arrow if it doesn't exist
        if (arrowObject == null)
        {
            Debug.Log($"Creating new arrow from prefab: {arrowPrefab.name}");
            // Instantiate directly in the scene, not as a child
            arrowObject = Instantiate(arrowPrefab);
            
            // Set the color
            SpriteRenderer renderer = arrowObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = arrowColor;
                Debug.Log($"Set arrow color to: {arrowColor}");
                
                // Make sure the sprite faces the camera
                renderer.sortingOrder = 1000; // Ensure it's drawn on top
            }
            else
            {
                Debug.LogWarning("Arrow prefab does not have a SpriteRenderer component!");
            }
            
            // Set initial scale
            arrowObject.transform.localScale = Vector3.one * arrowSize;
            
            // Position it initially in front of the camera
            Vector3 initialPos = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
            arrowObject.transform.position = initialPos;
        }
        
        arrowObject.SetActive(true);
        isShowing = true;
        Debug.Log($"Arrow is now visible and pointing to {target.name}");
    }
    
    public void HideArrow()
    {
        if (arrowObject != null)
        {
            arrowObject.SetActive(false);
        }
        isShowing = false;
        Debug.Log("Arrow is now hidden");
    }
    
    public void SetTarget(GameObject newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogError("ArrowIndicator: Cannot set null target!");
            return;
        }
        
        Debug.Log($"Setting arrow target to: {newTarget.name}");
        target = newTarget;
        
        // If arrow is already showing, update it
        if (isShowing)
        {
            HideArrow();
            ShowArrow();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up arrow when this component is destroyed
        if (arrowObject != null)
        {
            Destroy(arrowObject);
        }
    }
    
    // Check if a screen position is within the visible screen
    private bool IsOnScreen(Vector3 screenPos)
    {
        return screenPos.x > screenEdgePadding && 
               screenPos.x < Screen.width - screenEdgePadding && 
               screenPos.y > screenEdgePadding && 
               screenPos.y < Screen.height - screenEdgePadding && 
               screenPos.z > 0;
    }
    
    public void SetArrowPrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            arrowPrefab = prefab;
            
            // If we already have an arrow, destroy it so it will be recreated with the new prefab
            if (arrowObject != null)
            {
                Destroy(arrowObject);
                arrowObject = null;
            }
        }
    }
    
    public void SetArrowColor(Color color)
    {
        arrowColor = color;
        
        // Update the color of the existing arrow if it exists
        if (arrowObject != null)
        {
            SpriteRenderer renderer = arrowObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = arrowColor;
            }
        }
    }
} 