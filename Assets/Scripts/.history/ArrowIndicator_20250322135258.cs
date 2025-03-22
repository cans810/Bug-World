using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab; // Assign your sprite-based arrow prefab in the inspector
    [SerializeField] private float arrowSize = 0.18f; // Reduced size
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseAmount = 0.15f;
    
    [Header("Target Settings")]
    [SerializeField] private GameObject target;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private bool destroyWhenReached = false;
    [SerializeField] private float reachDistance = 2f;
    
    [Header("Positioning")]
    [SerializeField] private Vector2 screenPosition = new Vector2(0.5f, 0.5f); // Center of screen by default
    [SerializeField] private float distanceFromCenter = 0.15f; // Distance from center of screen (as percentage)
    
    [Header("Smoothing Settings")]
    [SerializeField] private float rotationSmoothTime = 0.1f; // Smoothing time for rotation
    
    private GameObject arrowObject;
    private bool isShowing = false;
    private Transform playerTransform;
    private Camera mainCamera;
    
    // Variables for smooth movement
    private float currentRotationVelocity = 0f;
    private float targetRotation;
    
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
            CalculateArrowDirection();
            UpdateArrowTransform();
        }
    }
    
    // This method calculates the direction the arrow should point
    private void CalculateArrowDirection()
    {
        // Get target position in screen space
        Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(target.transform.position);
        
        // Calculate screen center
        Vector2 screenCenter = new Vector2(Screen.width * screenPosition.x, Screen.height * screenPosition.y);
        
        // Check if target is behind camera
        bool isBehind = targetScreenPos.z < 0;
        
        // If behind, flip the position
        if (isBehind)
        {
            targetScreenPos.x = Screen.width - targetScreenPos.x;
            targetScreenPos.y = Screen.height - targetScreenPos.y;
        }
        
        // Calculate direction from screen center to target
        Vector2 direction = new Vector2(targetScreenPos.x - screenCenter.x, targetScreenPos.y - screenCenter.y);
        
        // Calculate the angle to point toward the target
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Adjust the angle to point correctly in Unity's 2D space
        targetRotation = angle - 90f; // -90 because the arrow sprite typically points up
        
        // Check if target is on screen
        bool isOnScreen = IsOnScreen(targetScreenPos);
        
        // Calculate distance to target for visibility
        float distance = Vector3.Distance(playerTransform.position, target.transform.position);
        
        // Show/hide arrow based on target visibility and distance
        if (!isOnScreen && distance > reachDistance)
        {
            arrowObject.SetActive(true);
        }
        else if (distance <= reachDistance && destroyWhenReached)
        {
            HideArrow();
        }
        else if (isOnScreen)
        {
            arrowObject.SetActive(false);
        }
    }
    
    // This method updates the arrow's transform
    private void UpdateArrowTransform()
    {
        if (!arrowObject.activeSelf) return;
        
        // Position the arrow at the fixed screen position
        Vector3 arrowWorldPos = mainCamera.ViewportToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, 1f));
        arrowObject.transform.position = arrowWorldPos;
        
        // Apply the Z rotation smoothly for directional pointing
        Vector3 currentRotation = arrowObject.transform.eulerAngles;
        float smoothedZRotation = Mathf.SmoothDampAngle(
            currentRotation.z, 
            targetRotation, 
            ref currentRotationVelocity, 
            rotationSmoothTime
        );
        
        // Set the rotation
        arrowObject.transform.eulerAngles = new Vector3(0, 0, smoothedZRotation);
        
        // Apply pulsing effect to scale
        float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
        float baseScale = arrowSize * pulse;
        
        // Smoothly update scale
        arrowObject.transform.localScale = Vector3.Lerp(
            arrowObject.transform.localScale,
            new Vector3(baseScale, baseScale, baseScale),
            Time.deltaTime / 0.1f
        );
    }
    
    public void ShowArrow()
    {
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
            // Instantiate directly in the scene
            arrowObject = Instantiate(arrowPrefab);
            
            // Set the color
            SpriteRenderer renderer = arrowObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = arrowColor;
                renderer.sortingOrder = 1000; // Ensure it's drawn on top
            }
            else
            {
                Debug.LogWarning("Arrow prefab does not have a SpriteRenderer component!");
            }
            
            // Set initial scale
            arrowObject.transform.localScale = Vector3.one * arrowSize;
            
            // Position it initially in the center of the screen
            Vector3 initialPos = mainCamera.ViewportToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, 1f));
            arrowObject.transform.position = initialPos;
        }
        
        arrowObject.SetActive(true);
        isShowing = true;
    }
    
    public void HideArrow()
    {
        if (arrowObject != null)
        {
            arrowObject.SetActive(false);
        }
        isShowing = false;
    }
    
    public void SetTarget(GameObject newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogError("ArrowIndicator: Cannot set null target!");
            return;
        }
        
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
        return screenPos.x > 0 && 
               screenPos.x < Screen.width && 
               screenPos.y > 0 && 
               screenPos.y < Screen.height && 
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