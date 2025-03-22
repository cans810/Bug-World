using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowScale = 0.7f; // Reduced from default (likely 1.0)
    [SerializeField] private float smoothSpeed = 5f; // Higher = faster smoothing
    [SerializeField] private float minDistanceToShow = 5f; // Min distance to show arrow
    [SerializeField] private float offsetFromPlayer = 3f; // Distance from player
    
    [Header("Appearance")]
    [SerializeField] private Color arrowColor = Color.white;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseAmount = 0.1f;
    
    // References
    private GameObject targetObject;
    private GameObject arrowInstance;
    private Transform playerTransform;
    private SpriteRenderer arrowRenderer;
    
    // Smoothing variables
    private Vector3 smoothVelocity = Vector3.zero;
    private Vector3 currentPosition;
    private bool isInitialized = false;
    
    private void Start()
    {
        // Find player reference
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("ArrowIndicator: Player not found!");
            enabled = false;
            return;
        }
    }
    
    private void Update()
    {
        if (arrowInstance != null && targetObject != null && playerTransform != null)
        {
            // Calculate desired position
            Vector3 directionToTarget = (targetObject.transform.position - playerTransform.position).normalized;
            Vector3 desiredPosition = playerTransform.position + directionToTarget * offsetFromPlayer;
            
            // Apply y offset to keep arrow at consistent height
            desiredPosition.y = playerTransform.position.y + 1.5f;
            
            // Smooth the position using SmoothDamp instead of direct assignment
            if (!isInitialized)
            {
                // Initialize on first update to avoid jarring movement
                currentPosition = desiredPosition;
                isInitialized = true;
            }
            
            // Smooth movement using SmoothDamp
            currentPosition = Vector3.SmoothDamp(
                currentPosition, 
                desiredPosition, 
                ref smoothVelocity, 
                1f / smoothSpeed
            );
            
            // Apply the smoothed position
            arrowInstance.transform.position = currentPosition;
            
            // Make arrow point to target
            float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            arrowInstance.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Animate pulse effect
            if (arrowRenderer != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                arrowInstance.transform.localScale = new Vector3(
                    arrowScale * pulse,
                    arrowScale * pulse,
                    arrowScale * pulse
                );
            }
            
            // Hide arrow if too close to target
            float distanceToTarget = Vector3.Distance(playerTransform.position, targetObject.transform.position);
            arrowInstance.SetActive(distanceToTarget > minDistanceToShow);
        }
    }
    
    public void SetArrowPrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            arrowPrefab = prefab;
        }
    }
    
    public void SetTarget(GameObject target)
    {
        targetObject = target;
    }
    
    public void SetArrowColor(Color color)
    {
        arrowColor = color;
        
        if (arrowRenderer != null)
        {
            arrowRenderer.color = arrowColor;
        }
    }
    
    public void ShowArrow()
    {
        if (arrowInstance == null && arrowPrefab != null)
        {
            // Create arrow instance
            arrowInstance = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            
            // Set as child of this transform
            arrowInstance.transform.SetParent(transform);
            
            // Set smaller initial scale
            arrowInstance.transform.localScale = new Vector3(arrowScale, arrowScale, arrowScale);
            
            // Get renderer and set color
            arrowRenderer = arrowInstance.GetComponent<SpriteRenderer>();
            if (arrowRenderer != null)
            {
                arrowRenderer.color = arrowColor;
            }
            
            // Reset initialization
            isInitialized = false;
        }
        
        if (arrowInstance != null)
        {
            arrowInstance.SetActive(true);
        }
    }
    
    public void HideArrow()
    {
        if (arrowInstance != null)
        {
            arrowInstance.SetActive(false);
        }
    }
    
    public void SetOffsetFromPlayer(float offset)
    {
        offsetFromPlayer = offset;
    }
    
    public void SetArrowScale(float scale)
    {
        arrowScale = scale;
        
        // Apply to existing arrow if present
        if (arrowInstance != null)
        {
            arrowInstance.transform.localScale = new Vector3(arrowScale, arrowScale, arrowScale);
        }
    }
    
    private void OnDestroy()
    {
        if (arrowInstance != null)
        {
            Destroy(arrowInstance);
        }
    }
} 