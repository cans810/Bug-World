using UnityEngine;
using System.Collections;

public class PlayerFollowingArrowIndicator : MonoBehaviour
{
    [Header("Arrow Appearance")]
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float arrowSize = 0.5f;
    [SerializeField] private float floatHeight = 2.0f;
    [SerializeField] private float followOffset = 0.5f;
    [SerializeField] private float bobSpeed = 1.0f;
    [SerializeField] private float bobAmount = 0.2f;
    [SerializeField] private float rotationSpeed = 5.0f;
    
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private float hideDistance = 2f;
    
    // References
    private GameObject arrowObject;
    private MeshRenderer arrowRenderer;
    private bool isShowing = false;
    private Transform playerTransform;
    
    private void Awake()
    {
        // Force hide distance to always be 2
        hideDistance = 2f;
        
        // Find the player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("PlayerFollowingArrowIndicator: Player not found! Make sure the player has the 'Player' tag.");
        }
        
        // Create the 3D arrow
        CreateArrowMesh();
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
            UpdateArrowRotation();
            
            // Check if we should hide the arrow based on distance
            float distanceToTarget = Vector3.Distance(playerTransform.position, target.position);
            
            // Debug the distance to help troubleshoot
            if (Time.frameCount % 60 == 0) // Only log every 60 frames to avoid spam
            {
                Debug.Log($"Distance to target: {distanceToTarget}, hideDistance: {hideDistance}");
            }
            
            if (distanceToTarget <= hideDistance)
            {
                // Only hide the visual, don't completely deactivate the system
                arrowObject.SetActive(false);
                
                // If we're extremely close, then we can completely hide the arrow
                if (distanceToTarget <= hideDistance / 2f)
                {
                    HideArrow();
                }
            }
            else
            {
                // Make sure arrow is visible
                arrowObject.SetActive(true);
            }
        }
    }
    
    private void CreateArrowMesh()
    {
        // Create a new game object for the arrow
        arrowObject = new GameObject("PlayerFollowingArrow");
        arrowObject.transform.parent = transform;
        
        // Add mesh filter with better proportions
        MeshFilter meshFilter = arrowObject.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateArrowMesh(0.3f, 1.0f, 0.35f); // Better width-to-length ratio
        
        // Add mesh renderer and set material
        arrowRenderer = arrowObject.AddComponent<MeshRenderer>();
        Material arrowMaterial = new Material(Shader.Find("Standard"));
        arrowMaterial.color = arrowColor;
        arrowMaterial.EnableKeyword("_EMISSION");
        arrowMaterial.SetColor("_EmissionColor", arrowColor * 0.7f); // Reduced emission brightness
        
        // Make the material matte by reducing glossiness and metallic values
        arrowMaterial.SetFloat("_Glossiness", 0.1f); // Changed from 0.8f to 0.1f for matte appearance
        arrowMaterial.SetFloat("_Metallic", 0.0f); // Changed from 0.5f to 0.0f for non-metallic appearance
        
        arrowRenderer.material = arrowMaterial;
        
        // Set initial position
        if (playerTransform != null)
        {
            arrowObject.transform.position = playerTransform.position + Vector3.up * floatHeight;
        }
        
        // Set initial scale
        arrowObject.transform.localScale = Vector3.one * arrowSize;
        
        // Initially hide the arrow
        arrowObject.SetActive(false);
    }
    
    private Mesh CreateArrowMesh(float width, float length, float headSize)
    {
        Mesh mesh = new Mesh();
        
        // Simplify the arrow to have cleaner edges
        Vector3[] vertices = new Vector3[]
        {
            // Base of the arrow (back)
            new Vector3(-width/4, -width/4, 0),             // 0
            new Vector3(width/4, -width/4, 0),              // 1
            new Vector3(width/4, width/4, 0),               // 2
            new Vector3(-width/4, width/4, 0),              // 3
            
            // Middle of the arrow (where head connects)
            new Vector3(-width/4, -width/4, length - headSize),  // 4
            new Vector3(width/4, -width/4, length - headSize),   // 5
            new Vector3(width/4, width/4, length - headSize),    // 6
            new Vector3(-width/4, width/4, length - headSize),   // 7
            
            // Outer points of the arrowhead
            new Vector3(-width*0.7f, -width/4, length - headSize),  // 8
            new Vector3(width*0.7f, -width/4, length - headSize),   // 9
            new Vector3(width*0.7f, width/4, length - headSize),    // 10
            new Vector3(-width*0.7f, width/4, length - headSize),   // 11
            
            // Tip of the arrow
            new Vector3(0, 0, length*1.2f)                     // 12
        };
        
        // Define triangles with proper winding order
        int[] triangles = new int[]
        {
            // Back face
            0, 2, 1, 0, 3, 2,
            
            // Shaft sides
            0, 1, 5, 0, 5, 4, // Bottom
            1, 2, 6, 1, 6, 5, // Right
            2, 3, 7, 2, 7, 6, // Top
            3, 0, 4, 3, 4, 7, // Left
            
            // Arrow head sides (from shaft to outer edge)
            4, 5, 9, 4, 9, 8, // Bottom expansion
            5, 6, 10, 5, 10, 9, // Right expansion
            6, 7, 11, 6, 11, 10, // Top expansion
            7, 4, 8, 7, 8, 11, // Left expansion
            
            // Arrow head point triangles
            8, 9, 12, // Bottom point
            9, 10, 12, // Right point
            10, 11, 12, // Top point
            11, 8, 12, // Left point
        };
        
        // Create simple UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x + 0.5f, vertices[i].z / length);
        }
        
        // Apply to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        // Calculate normals
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    private void UpdateArrowPosition()
    {
        if (playerTransform == null) return;
        
        // Calculate a position relative to the player
        Vector3 targetPosition;
        
        // Handle negative float height (arrows below player)
        if (floatHeight < 0)
        {
            // Position below player
            targetPosition = playerTransform.position + Vector3.up * floatHeight;
            // Add offset behind the player
            targetPosition += -playerTransform.forward * followOffset;
        }
        else
        {
            // Standard position above player
            targetPosition = playerTransform.position + Vector3.up * floatHeight;
            // Add offset in front of player
            targetPosition += playerTransform.forward * followOffset;
        }
        
        // Add bobbing motion
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        targetPosition.y += bobOffset;
        
        // Smoothly move to target position
        arrowObject.transform.position = Vector3.Lerp(
            arrowObject.transform.position, 
            targetPosition, 
            Time.deltaTime * 5f);
    }
    
    private void UpdateArrowRotation()
    {
        if (target == null) return;
        
        // Get direction to target, ignoring Y difference
        Vector3 targetPosition = target.position;
        Vector3 arrowPosition = arrowObject.transform.position;
        
        // Create direction vector on XZ plane (horizontal only)
        Vector3 direction = new Vector3(
            targetPosition.x - arrowPosition.x,
            0f,
            targetPosition.z - arrowPosition.z).normalized;
        
        // Calculate rotation to face target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        // Smoothly rotate towards target
        arrowObject.transform.rotation = Quaternion.Slerp(
            arrowObject.transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed);
    }
    
    public void ShowArrow()
    {
        if (target == null)
        {
            Debug.LogError("PlayerFollowingArrowIndicator: Cannot show arrow - target is null!");
            return;
        }
        
        // Make sure the arrow object exists
        if (arrowObject == null)
        {
            Debug.LogWarning("Arrow object was null when ShowArrow called - recreating arrow");
            CreateArrowMesh();
        }
        
        arrowObject.SetActive(true);
        isShowing = true;
        
        // Force initial position and rotation
        if (playerTransform != null)
        {
            arrowObject.transform.position = playerTransform.position + Vector3.up * floatHeight;
            UpdateArrowRotation();
        }
        
        Debug.Log($"Player following arrow now visible and pointing to {target.name}");
    }
    
    public void HideArrow()
    {
        if (arrowObject != null)
        {
            arrowObject.SetActive(false);
        }
        isShowing = false;
        
        Debug.Log("Player following arrow is now hidden");
    }
    
    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogError("PlayerFollowingArrowIndicator: Cannot set null target!");
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
    
    public void SetArrowColor(Color color)
    {
        arrowColor = color;
        
        // Update the color of the existing arrow if it exists
        if (arrowRenderer != null)
        {
            arrowRenderer.material.color = arrowColor;
            arrowRenderer.material.SetColor("_EmissionColor", arrowColor);
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