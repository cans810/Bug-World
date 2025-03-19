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
            // Position the arrow above the player
            arrowObject.transform.position = playerTransform.position + Vector3.up * arrowHeight;
            
            // Make the arrow point toward the target
            Vector3 directionToTarget = (target.transform.position - playerTransform.position).normalized;
            directionToTarget.y = 0; // Keep the arrow level horizontally
            
            if (directionToTarget != Vector3.zero)
            {
                // Point the arrow in the direction of the target
                arrowObject.transform.rotation = Quaternion.LookRotation(directionToTarget);
                
                // Add some rotation to make it more visible
                arrowObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
            
            // Pulse the arrow size for visibility
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * pulseSpeed);
            arrowObject.transform.localScale = Vector3.one * arrowSize * pulse;
            
            // Check if player has reached the target
            if (destroyWhenReached)
            {
                float distance = Vector3.Distance(playerTransform.position, target.transform.position);
                if (distance <= reachDistance)
                {
                    HideArrow();
                    Debug.Log("Player reached target, hiding arrow");
                }
            }
        }
    }
    
    public void ShowArrow()
    {
        if (target == null)
        {
            Debug.LogError("ArrowIndicator: Cannot show arrow - target is null!");
            return;
        }
        
        // Use ArrowIndicatorManager to create the arrow
        if (ArrowIndicatorManager.Instance != null)
        {
            // If we already have an arrow, remove it first
            if (arrowObject != null)
            {
                ArrowIndicatorManager.Instance.RemoveArrow(target);
            }
            
            // Create a new arrow using the manager
            arrowObject = ArrowIndicatorManager.Instance.CreateArrowToTarget(target, arrowColor);
            isShowing = true;
            Debug.Log($"Arrow is now visible and pointing to {target.name}");
        }
        else
        {
            Debug.LogError("ArrowIndicator: ArrowIndicatorManager instance not found!");
        }
    }
    
    public void HideArrow()
    {
        if (isShowing && ArrowIndicatorManager.Instance != null)
        {
            ArrowIndicatorManager.Instance.RemoveArrow(target);
            arrowObject = null;
            isShowing = false;
            Debug.Log("Arrow is now hidden");
        }
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
        if (isShowing && ArrowIndicatorManager.Instance != null)
        {
            ArrowIndicatorManager.Instance.RemoveArrow(target);
        }
    }
} 