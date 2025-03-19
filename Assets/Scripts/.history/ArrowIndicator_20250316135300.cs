using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab; // Assign your sprite-based arrow prefab in the inspector
    [SerializeField] private float arrowHeight = 2.0f; // Height above the player
    [SerializeField] private float arrowSize = 1.0f;
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float rotationSpeed = 50.0f;
    
    [Header("Target Settings")]
    [SerializeField] private GameObject target;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private bool destroyWhenReached = false;
    [SerializeField] private float reachDistance = 2f;
    
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
        
        // If target is off screen, position arrow at screen edge
        if (!isOnScreen)
        {
            // Normalize direction and set arrow position at screen edge
            direction.Normalize();
            
            // Calculate screen bounds with some padding
            float padding = 100f; // Padding to move arrow closer to center
            float minX = padding;
            float maxX = Screen.width - padding;
            float minY = padding;
            float maxY = Screen.height - padding;
            
            // Calculate arrow position at screen edge
            Vector2 arrowPos = ClampToScreenEdge(screenCenter, direction, minX, maxX, minY, maxY);
            
            // Convert to world position for the arrow
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(arrowPos.x, arrowPos.y, 10f));
            arrowObject.transform.position = worldPos;
            
            // Rotate arrow to point toward target
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            arrowObject.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Make arrow visible
            arrowObject.SetActive(true);
            
            // Apply pulsing effect
            float pulse = 1f + 0.2f * Mathf.Sin(Time.time * pulseSpeed);
            arrowObject.transform.localScale = Vector3.one * arrowSize * pulse;
        }
        else
        {
            // If target is on screen, check if we should hide the arrow
            float distance = Vector3.Distance(playerTransform.position, target.transform.position);
            if (distance <= reachDistance && destroyWhenReached)
            {
                HideArrow();
                Debug.Log("Player reached target, hiding arrow");
            }
            else
            {
                // Hide the arrow when on screen
                arrowObject.SetActive(false);
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
            }
            else
            {
                Debug.LogWarning("Arrow prefab does not have a SpriteRenderer component!");
            }
            
            // Set initial scale
            arrowObject.transform.localScale = Vector3.one * arrowSize;
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
        return screenPos.x > 0 && screenPos.x < Screen.width && 
               screenPos.y > 0 && screenPos.y < Screen.height && 
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