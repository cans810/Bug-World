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
    
    [Header("Smoothing Settings")]
    [SerializeField] private float positionSmoothTime = 0.2f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    
    private GameObject arrowObject;
    private bool isShowing = false;
    private Transform playerTransform;
    private Camera mainCamera;
    
    // Variables for smoothing
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 targetPosition;
    private float currentAngle = 0f;
    private float targetAngle = 0f;
    private float angleVelocity = 0f;
    
    // Screen edge padding - increased to keep arrow more centered
    private float screenEdgePadding = 100f;
    
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
        // Get target and player positions in screen space
        Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(target.transform.position);
        Vector3 playerScreenPos = mainCamera.WorldToScreenPoint(playerTransform.position);
        
        // Check if target is behind camera
        bool isBehind = targetScreenPos.z < 0;
        if (isBehind)
        {
            // If behind, invert the direction
            targetScreenPos.x = Screen.width - targetScreenPos.x;
            targetScreenPos.y = Screen.height - targetScreenPos.y;
        }
        
        // Calculate direction from player to target in screen space (not target to player)
        Vector2 directionToTarget = new Vector2(
            targetScreenPos.x - playerScreenPos.x,
            targetScreenPos.y - playerScreenPos.y
        );
        
        // Normalize the direction
        if (directionToTarget.magnitude > 0)
        {
            directionToTarget.Normalize();
        }
        
        // Calculate the angle to point arrow toward target
        targetAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
        
        // Check if target is on screen
        bool isOnScreen = IsOnScreen(targetScreenPos);
        
        // Calculate distance to target
        float distance = Vector3.Distance(playerTransform.position, target.transform.position);
        
        // If target is off screen or far enough away, show the arrow
        if (!isOnScreen || (distance > reachDistance))
        {
            // Make arrow visible
            arrowObject.SetActive(true);
            
            if (!isOnScreen)
            {
                // Calculate screen center
                Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                
                // Calculate screen bounds with padding
                float minX = screenEdgePadding;
                float maxX = Screen.width - screenEdgePadding;
                float minY = screenEdgePadding;
                float maxY = Screen.height - screenEdgePadding;
                
                // Calculate arrow position at screen edge
                Vector2 arrowScreenPos = ClampToScreenEdge(playerScreenPos, directionToTarget, minX, maxX, minY, maxY);
                
                // Convert to world position for the arrow
                Vector3 newArrowWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(arrowScreenPos.x, arrowScreenPos.y, 10f));
                
                // Apply smooth position transition
                targetPosition = newArrowWorldPos;
                arrowObject.transform.position = Vector3.SmoothDamp(
                    arrowObject.transform.position,
                    targetPosition,
                    ref currentVelocity,
                    positionSmoothTime
                );
            }
            else
            {
                // If on screen but far away, position directly at target position
                Vector3 newArrowWorldPos = target.transform.position;
                targetPosition = newArrowWorldPos;
                arrowObject.transform.position = Vector3.SmoothDamp(
                    arrowObject.transform.position,
                    targetPosition,
                    ref currentVelocity,
                    positionSmoothTime
                );
            }
            
            // Apply billboard effect - always face camera
            arrowObject.transform.LookAt(
                arrowObject.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up
            );
            
            // Smooth the rotation angle
            currentAngle = Mathf.SmoothDampAngle(
                currentAngle,
                targetAngle,
                ref angleVelocity,
                rotationSmoothTime
            );
            
            // Apply the rotation (after billboarding)
            arrowObject.transform.Rotate(0, 0, currentAngle, Space.Self);
            
            // Apply pulsing effect
            float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
            arrowObject.transform.localScale = Vector3.one * arrowSize * pulse;
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
    
    // Calculate position at screen edge where ray from player to target intersects
    private Vector2 ClampToScreenEdge(Vector2 playerPos, Vector2 direction, float minX, float maxX, float minY, float maxY)
    {
        // Calculate screen bounds
        Rect screenRect = new Rect(minX, minY, maxX - minX, maxY - minY);
        
        // Calculate intersection points with each edge
        float distToTop = float.MaxValue;
        float distToBottom = float.MaxValue;
        float distToRight = float.MaxValue;
        float distToLeft = float.MaxValue;
        Vector2 topIntersect = Vector2.zero;
        Vector2 bottomIntersect = Vector2.zero;
        Vector2 rightIntersect = Vector2.zero;
        Vector2 leftIntersect = Vector2.zero;
        bool foundIntersection = false;
        
        // Check top edge
        if (direction.y > 0)
        {
            float t = (screenRect.yMax - playerPos.y) / direction.y;
            Vector2 intersect = playerPos + direction * t;
            if (intersect.x >= screenRect.xMin && intersect.x <= screenRect.xMax)
            {
                distToTop = t;
                topIntersect = intersect;
                foundIntersection = true;
            }
        }
        
        // Check bottom edge
        if (direction.y < 0)
        {
            float t = (screenRect.yMin - playerPos.y) / direction.y;
            Vector2 intersect = playerPos + direction * t;
            if (intersect.x >= screenRect.xMin && intersect.x <= screenRect.xMax)
            {
                distToBottom = t;
                bottomIntersect = intersect;
                foundIntersection = true;
            }
        }
        
        // Check right edge
        if (direction.x > 0)
        {
            float t = (screenRect.xMax - playerPos.x) / direction.x;
            Vector2 intersect = playerPos + direction * t;
            if (intersect.y >= screenRect.yMin && intersect.y <= screenRect.yMax)
            {
                distToRight = t;
                rightIntersect = intersect;
                foundIntersection = true;
            }
        }
        
        // Check left edge
        if (direction.x < 0)
        {
            float t = (screenRect.xMin - playerPos.x) / direction.x;
            Vector2 intersect = playerPos + direction * t;
            if (intersect.y >= screenRect.yMin && intersect.y <= screenRect.yMax)
            {
                distToLeft = t;
                leftIntersect = intersect;
                foundIntersection = true;
            }
        }
        
        // Choose the closest valid intersection
        Vector2 result = playerPos;
        float minDist = Mathf.Min(distToTop, distToBottom, distToRight, distToLeft);
        
        if (foundIntersection)
        {
            if (minDist == distToTop) result = topIntersect;
            else if (minDist == distToBottom) result = bottomIntersect;
            else if (minDist == distToRight) result = rightIntersect;
            else result = leftIntersect;
        }
        
        return result;
    }
    
    // Other methods remain mostly unchanged
    // ...

    // Check if a screen position is within the visible screen
    private bool IsOnScreen(Vector3 screenPos)
    {
        return screenPos.x > screenEdgePadding && 
               screenPos.x < Screen.width - screenEdgePadding && 
               screenPos.y > screenEdgePadding && 
               screenPos.y < Screen.height - screenEdgePadding && 
               screenPos.z > 0;
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
            arrowObject = Instantiate(arrowPrefab);
            
            // Set the color
            SpriteRenderer renderer = arrowObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = arrowColor;
                renderer.sortingOrder = 1000; // Ensure it's drawn on top
            }
            
            // Set initial scale
            arrowObject.transform.localScale = Vector3.one * arrowSize;
            
            // Position it initially in front of the camera
            Vector3 initialPos = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
            arrowObject.transform.position = initialPos;
            targetPosition = initialPos;
            
            // Initialize rotation values
            currentAngle = 0f;
            targetAngle = 0f;
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
    
    private void OnDestroy()
    {
        // Clean up arrow when this component is destroyed
        if (arrowObject != null)
        {
            Destroy(arrowObject);
        }
    }
} 