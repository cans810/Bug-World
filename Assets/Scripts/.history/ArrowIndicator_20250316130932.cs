using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab; // Assign this in the inspector
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
        
        // Create the arrow object if it doesn't exist
        if (arrowPrefab != null)
        {
            // Use the prefab if provided
            arrowObject = Instantiate(arrowPrefab, transform);
        }
        else
        {
            // Create a simple arrow from primitives
            CreateSimpleArrow();
        }
        
        // Initially hide the arrow
        if (arrowObject != null)
        {
            arrowObject.SetActive(false);
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
    
    private void CreateSimpleArrow()
    {
        // Create a new empty GameObject for the arrow
        arrowObject = new GameObject("DirectionArrow");
        arrowObject.transform.SetParent(transform);
        
        // Create the arrow head using a cube
        GameObject arrowHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrowHead.transform.SetParent(arrowObject.transform);
        arrowHead.transform.localPosition = Vector3.forward * 0.5f;
        arrowHead.transform.localScale = new Vector3(0.5f, 0.5f, 0.8f);
        
        // Create left wing of arrow head
        GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWing.transform.SetParent(arrowObject.transform);
        leftWing.transform.localPosition = new Vector3(-0.3f, 0f, 0.3f);
        leftWing.transform.localRotation = Quaternion.Euler(0, 45, 0);
        leftWing.transform.localScale = new Vector3(0.3f, 0.3f, 0.6f);
        
        // Create right wing of arrow head
        GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWing.transform.SetParent(arrowObject.transform);
        rightWing.transform.localPosition = new Vector3(0.3f, 0f, 0.3f);
        rightWing.transform.localRotation = Quaternion.Euler(0, -45, 0);
        rightWing.transform.localScale = new Vector3(0.3f, 0.3f, 0.6f);
        
        // Create the arrow shaft
        GameObject arrowShaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrowShaft.transform.SetParent(arrowObject.transform);
        arrowShaft.transform.localPosition = Vector3.back * 0.2f;
        arrowShaft.transform.localRotation = Quaternion.Euler(90, 0, 0);
        arrowShaft.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
        
        // Set the color of all parts
        Renderer[] renderers = arrowObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = arrowColor;
            // Make it glow/emissive
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", arrowColor * 0.8f);
        }
    }
    
    public void ShowArrow()
    {
        if (target == null)
        {
            Debug.LogError("ArrowIndicator: Cannot show arrow - target is null!");
            return;
        }
        
        if (arrowObject == null)
        {
            Debug.LogError("ArrowIndicator: Arrow object is null! Creating a new one.");
            CreateSimpleArrow();
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