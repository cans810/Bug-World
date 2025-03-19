using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowIndicatorManager : MonoBehaviour
{
    // Singleton instance
    public static ArrowIndicatorManager Instance { get; private set; }
    
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowDistance = 100f; // Distance from screen center
    [SerializeField] private float arrowScale = 1f;
    [SerializeField] private Color arrowColor = Color.white;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    [Header("Behavior")]
    [SerializeField] private bool fadeWhenClose = true;
    [SerializeField] private float fadeStartDistance = 5f;
    [SerializeField] private float fadeEndDistance = 1f;
    
    // Dictionary to track active arrows and their targets
    private Dictionary<GameObject, GameObject> activeArrows = new Dictionary<GameObject, GameObject>();
    private Camera mainCamera;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found for arrow indicators!");
        }
    }
    
    private void Update()
    {
        UpdateArrows();
    }
    
    // Create a new arrow pointing to a target
    public GameObject CreateArrowToTarget(GameObject target, Color? customColor = null)
    {
        if (arrowPrefab == null || target == null)
        {
            Debug.LogError("Arrow prefab or target is null!");
            return null;
        }
        
        // Check if we already have an arrow for this target
        if (activeArrows.ContainsValue(target))
        {
            Debug.Log($"Arrow already exists for target {target.name}");
            return GetArrowForTarget(target);
        }
        
        // Create arrow as child of canvas
        GameObject arrow = Instantiate(arrowPrefab, transform);
        
        // Set color if provided
        if (customColor.HasValue)
        {
            SpriteRenderer renderer = arrow.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = customColor.Value;
            }
            else
            {
                UnityEngine.UI.Image image = arrow.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.color = customColor.Value;
                }
            }
        }
        
        // Set initial scale
        arrow.transform.localScale = Vector3.one * arrowScale;
        
        // Add to tracking dictionary
        activeArrows.Add(arrow, target);
        
        return arrow;
    }
    
    // Remove an arrow
    public void RemoveArrow(GameObject target)
    {
        GameObject arrowToRemove = GetArrowForTarget(target);
        
        if (arrowToRemove != null)
        {
            activeArrows.Remove(arrowToRemove);
            Destroy(arrowToRemove);
        }
    }
    
    // Get the arrow pointing to a specific target
    private GameObject GetArrowForTarget(GameObject target)
    {
        foreach (var pair in activeArrows)
        {
            if (pair.Value == target)
            {
                return pair.Key;
            }
        }
        return null;
    }
    
    // Update all arrows to point to their targets
    private void UpdateArrows()
    {
        // Create a list to track arrows to remove (can't modify dictionary during iteration)
        List<GameObject> arrowsToRemove = new List<GameObject>();
        
        foreach (var pair in activeArrows)
        {
            GameObject arrow = pair.Key;
            GameObject target = pair.Value;
            
            // If target is destroyed, mark arrow for removal
            if (target == null)
            {
                arrowsToRemove.Add(arrow);
                continue;
            }
            
            // Update arrow position and rotation
            UpdateArrowTransform(arrow, target);
        }
        
        // Remove any arrows whose targets are gone
        foreach (GameObject arrow in arrowsToRemove)
        {
            activeArrows.Remove(arrow);
            Destroy(arrow);
        }
    }
    
    // Update a single arrow's position and rotation
    private void UpdateArrowTransform(GameObject arrow, GameObject target)
    {
        if (mainCamera == null || arrow == null || target == null)
            return;
            
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
        
        // Calculate direction from screen center to target
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 direction = new Vector2(targetScreenPos.x - screenCenter.x, targetScreenPos.y - screenCenter.y);
        
        // Check if target is on screen
        bool isOnScreen = IsOnScreen(targetScreenPos);
        
        // If target is on screen and close enough, fade the arrow
        float distance = Vector3.Distance(mainCamera.transform.position, target.transform.position);
        float alpha = 1f;
        
        if (fadeWhenClose && isOnScreen)
        {
            if (distance < fadeEndDistance)
            {
                alpha = 0f;
            }
            else if (distance < fadeStartDistance)
            {
                alpha = Mathf.InverseLerp(fadeEndDistance, fadeStartDistance, distance);
            }
        }
        
        // Apply alpha
        SetArrowAlpha(arrow, alpha);
        
        // If target is off screen, position arrow at screen edge
        if (!isOnScreen)
        {
            // Normalize direction and set arrow position at screen edge
            direction.Normalize();
            
            // Calculate screen bounds with some padding
            float padding = 100f; // Increased padding to move arrow closer to center
            float minX = padding;
            float maxX = Screen.width - padding;
            float minY = padding;
            float maxY = Screen.height - padding;
            
            // Calculate arrow position at screen edge
            Vector2 arrowPos = ClampToScreenEdge(screenCenter, direction, minX, maxX, minY, maxY);
            
            // Convert to world position for the arrow
            arrow.transform.position = arrowPos;
            
            // Rotate arrow to point toward target
            // Assuming the arrow sprite points to the right (0 degrees) by default
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // If your arrow points left by default, use this:
            arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Make arrow visible
            arrow.SetActive(true);
            
            // Apply pulsing effect
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            arrow.transform.localScale = Vector3.one * arrowScale * pulse;
        }
        else
        {
            // If target is on screen, hide arrow or make it follow the target
            if (alpha <= 0.01f)
            {
                arrow.SetActive(false);
            }
            else
            {
                arrow.SetActive(true);
                arrow.transform.position = targetScreenPos;
                
                // Calculate direction from arrow to screen center
                Vector2 toCenter = screenCenter - new Vector2(targetScreenPos.x, targetScreenPos.y);
                toCenter.Normalize();
                
                // Rotate to point away from center (toward target)
                float angle = Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle + 180); // +180 to point away from center
            }
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
    
    // Set the alpha of an arrow
    private void SetArrowAlpha(GameObject arrow, float alpha)
    {
        // Try to get sprite renderer
        SpriteRenderer renderer = arrow.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
            return;
        }
        
        // Try to get UI image
        UnityEngine.UI.Image image = arrow.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
} 