using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSize = 0.25f;
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    [Header("Target Settings")]
    [SerializeField] private GameObject target;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private bool destroyWhenReached = false;
    [SerializeField] private float reachDistance = 2f;
    
    [Header("Position Settings")]
    [SerializeField] private Vector2 screenPosition = new Vector2(0.5f, 0.9f); // Default to top-middle (x=0.5, y=0.9)
    
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
        
        // Get player position in screen space
        Vector2 playerScreenPos = mainCamera.WorldToScreenPoint(playerTransform.position);
        
        // Calculate direction from screen position to target
        Vector2 fixedScreenPos = new Vector2(Screen.width * screenPosition.x, Screen.height * screenPosition.y);
        Vector2 directionToTarget = new Vector2(targetScreenPos.x - fixedScreenPos.x, 
                                                targetScreenPos.y - fixedScreenPos.y);
        
        // Normalize the direction
        if (directionToTarget.magnitude > 0)
        {
            directionToTarget.Normalize();
        }
        
        // Calculate the angle for rotation, adding 180 degrees to point in opposite direction
        float angle = (Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg) + 180f;
        
        // Check if target is behind camera
        bool isBehind = targetScreenPos.z < 0;
        
        // If behind, flip the angle (add another 180 degrees)
        if (isBehind)
        {
            angle += 180f;
        }
        
        // Calculate distance to target for visibility check
        float distance = Vector3.Distance(playerTransform.position, target.transform.position);
        
        // Only show arrow if target is far enough away
        if (distance > reachDistance)
        {
            // Make arrow visible
            arrowObject.SetActive(true);
            
            // Position the arrow at the fixed position
            Vector3 arrowWorldPos = mainCamera.ScreenToWorldPoint(
                new Vector3(fixedScreenPos.x, fixedScreenPos.y, 1f));
            arrowObject.transform.position = arrowWorldPos;
            
            // Ensure the arrow faces the camera (billboard effect)
            arrowObject.transform.LookAt(
                arrowObject.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
                                    
            // Apply the rotation for direction
            arrowObject.transform.Rotate(0, 0, angle, Space.Self);
            
            // Apply pulsing effect to scale
            float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
            float baseScale = arrowSize * pulse;
            arrowObject.transform.localScale = new Vector3(baseScale, baseScale, baseScale);
        }
        else
        {
            // If target is close enough, hide the arrow
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
            arrowObject = Instantiate(arrowPrefab);
            
            // Set the color
            SpriteRenderer renderer = arrowObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = arrowColor;
                Debug.Log($"Set arrow color to: {arrowColor}");
                renderer.sortingOrder = 1000; // Ensure it's drawn on top
            }
            else
            {
                Debug.LogWarning("Arrow prefab does not have a SpriteRenderer component!");
            }
            
            // Set initial scale
            arrowObject.transform.localScale = Vector3.one * arrowSize;
            
            // Position at the fixed screen position
            Vector3 fixedScreenPos = new Vector3(
                Screen.width * screenPosition.x, 
                Screen.height * screenPosition.y, 
                1f);
            Vector3 initialPos = mainCamera.ScreenToWorldPoint(fixedScreenPos);
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
    
    // New method to set the screen position of the arrow
    public void SetScreenPosition(Vector2 newPosition)
    {
        screenPosition = newPosition;
        
        // Update position if arrow exists
        if (arrowObject != null && isShowing)
        {
            Vector3 fixedScreenPos = new Vector3(
                Screen.width * screenPosition.x, 
                Screen.height * screenPosition.y, 
                1f);
            Vector3 newPos = mainCamera.ScreenToWorldPoint(fixedScreenPos);
            arrowObject.transform.position = newPos;
        }
    }
} 