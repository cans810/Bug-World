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
    
    private GameObject arrowObject;
    private bool isShowing = false;
    private Transform playerTransform;
    private Camera mainCamera;
    
    // Screen edge padding - increased to keep arrow more centered
    private float screenEdgePadding = 150f;
    
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
        
        // Check if target is behind camera
        bool isBehind = targetScreenPos.z < 0;
        
        // If behind, flip the position
        if (isBehind)
        {
            targetScreenPos.x = Screen.width - targetScreenPos.x;
            targetScreenPos.y = Screen.height - targetScreenPos.y;
        }
        
        // Calculate screen center
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        // Calculate direction from screen center to target
        Vector2 direction = new Vector2(targetScreenPos.x - screenCenter.x, targetScreenPos.y - screenCenter.y);
        
        // Check if target is on screen
        bool isOnScreen = IsOnScreen(targetScreenPos);
        
        // Calculate distance to target for fading
        float distance = Vector3.Distance(playerTransform.position, target.transform.position);
        
        // If target is off screen or far enough away, show the arrow
        if (!isOnScreen || (distance > reachDistance))
        {
            // Make arrow visible
            arrowObject.SetActive(true);
            
            // If off screen, position at edge
            if (!isOnScreen)
            {
                // Normalize direction
                if (direction.magnitude > 0)
                {
                    direction.Normalize();
                }
                
                // Calculate screen bounds with padding
                float minX = screenEdgePadding;
                float maxX = Screen.width - screenEdgePadding;
                float minY = screenEdgePadding;
                float maxY = Screen.height - screenEdgePadding;
                
                // Calculate arrow position at screen edge
                Vector2 arrowScreenPos = ClampToScreenEdge(screenCenter, direction, minX, maxX, minY, maxY);
                
                // Convert to world position for the arrow (fixed Z distance from camera)
                Vector3 arrowWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(arrowScreenPos.x, arrowScreenPos.y, 1f));
                arrowObject.transform.position = arrowWorldPos;
                
                // Rotate arrow to point toward target - add 180 degrees to flip direction
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;
                arrowObject.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                // If on screen but far away, position near the target
                Vector3 arrowWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(targetScreenPos.x, targetScreenPos.y, 1f));
                arrowObject.transform.position = arrowWorldPos;
                
                // For on-screen targets, we still want to point from player to target
                Vector2 playerScreenPos = mainCamera.WorldToScreenPoint(playerTransform.position);
                Vector2 directionFromPlayer = new Vector2(targetScreenPos.x - playerScreenPos.x, 
                                                         targetScreenPos.y - playerScreenPos.y);
                
                if (directionFromPlayer.magnitude > 0)
                {
                    directionFromPlayer.Normalize();
                    // Add 180 degrees to flip the direction
                    float angle = Mathf.Atan2(directionFromPlayer.y, directionFromPlayer.x) * Mathf.Rad2Deg + 180f;
                    arrowObject.transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
            
            // Apply pulsing effect
            float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
            arrowObject.transform.localScale = Vector3.one * arrowSize * pulse;
            
            // Make sure arrow faces the camera (but don't change its rotation around z-axis)
            Vector3 currentRotation = arrowObject.transform.rotation.eulerAngles;
            arrowObject.transform.LookAt(arrowObject.transform.position + mainCamera.transform.forward);
            arrowObject.transform.rotation = Quaternion.Euler(0, 0, currentRotation.z);
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
            Vector3 initialPos = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 1f));
            arrowObject.transform.position = initialPos;
            
            // Make sure it faces the camera
            arrowObject.transform.forward = mainCamera.transform.forward;
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
    
    // Calculate position at screen edge
    private Vector2 ClampToScreenEdge(Vector2 center, Vector2 direction, float minX, float maxX, float minY, float maxY)
    {
        // Calculate screen bounds
        Rect screenRect = new Rect(minX, minY, maxX - minX, maxY - minY);
        
        // Calculate the point where the direction vector intersects the screen edge
        Vector2 screenEdgePoint = center;
        
        // Calculate the max distance to the edge in the direction
        float maxDistance = 0f;
        
        // Check intersection with top edge
        if (direction.y > 0)
        {
            float d = (screenRect.yMax - center.y) / direction.y;
            Vector2 intersection = center + direction * d;
            if (intersection.x >= screenRect.xMin && intersection.x <= screenRect.xMax && d > 0)
            {
                maxDistance = Mathf.Max(maxDistance, d);
            }
        }
        
        // Check intersection with bottom edge
        if (direction.y < 0)
        {
            float d = (screenRect.yMin - center.y) / direction.y;
            Vector2 intersection = center + direction * d;
            if (intersection.x >= screenRect.xMin && intersection.x <= screenRect.xMax && d > 0)
            {
                maxDistance = Mathf.Max(maxDistance, d);
            }
        }
        
        // Check intersection with right edge
        if (direction.x > 0)
        {
            float d = (screenRect.xMax - center.x) / direction.x;
            Vector2 intersection = center + direction * d;
            if (intersection.y >= screenRect.yMin && intersection.y <= screenRect.yMax && d > 0)
            {
                maxDistance = Mathf.Max(maxDistance, d);
            }
        }
        
        // Check intersection with left edge
        if (direction.x < 0)
        {
            float d = (screenRect.xMin - center.x) / direction.x;
            Vector2 intersection = center + direction * d;
            if (intersection.y >= screenRect.yMin && intersection.y <= screenRect.yMax && d > 0)
            {
                maxDistance = Mathf.Max(maxDistance, d);
            }
        }
        
        // Calculate the edge point
        if (maxDistance > 0)
        {
            screenEdgePoint = center + direction * maxDistance;
        }
        
        return screenEdgePoint;
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