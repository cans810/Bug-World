using UnityEngine;
using System.Collections;

public class PlayerFollowingArrowIndicator : MonoBehaviour
{
    [Header("Arrow Appearance")]
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float arrowSize = 1.0f;
    [SerializeField] private float floatHeight = 2.0f;
    [SerializeField] private float followOffset = 0.5f;
    [SerializeField] private float bobSpeed = 1.0f;
    [SerializeField] private float bobAmount = 0.2f;
    [SerializeField] private float rotationSpeed = 5.0f;
    
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private float hideDistance = 5f;
    
    // References
    private GameObject arrowObject;
    private MeshRenderer arrowRenderer;
    private bool isShowing = false;
    private Transform playerTransform;
    
    private void Awake()
    {
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
        meshFilter.mesh = CreateArrowMesh(0.25f, 0.8f, 0.3f); // width, length, head size - better proportions
        
        // Add mesh renderer and set material
        arrowRenderer = arrowObject.AddComponent<MeshRenderer>();
        Material arrowMaterial = new Material(Shader.Find("Standard"));
        arrowMaterial.color = arrowColor;
        arrowMaterial.EnableKeyword("_EMISSION");
        arrowMaterial.SetColor("_EmissionColor", arrowColor * 0.8f); // Make it glow
        
        // Make the material slightly transparent for a better look
        arrowMaterial.SetFloat("_Glossiness", 0.8f); // More glossy
        
        arrowRenderer.material = arrowMaterial;
        
        // Set initial position
        if (playerTransform != null)
        {
            arrowObject.transform.position = playerTransform.position + Vector3.up * floatHeight;
        }
        
        // Initially hide the arrow
        arrowObject.SetActive(false);
    }
    
    private Mesh CreateArrowMesh(float width, float length, float headSize)
    {
        Mesh mesh = new Mesh();
        
        // Create vertices for a more pointy 3D arrow shape
        Vector3[] vertices = new Vector3[]
        {
            // Arrow shaft (front) - made thinner
            new Vector3(-width/4, 0, 0),                  // 0
            new Vector3(width/4, 0, 0),                   // 1
            new Vector3(width/4, 0, length - headSize),   // 2
            new Vector3(-width/4, 0, length - headSize),  // 3
            
            // Arrow head (front) - made wider and more pronounced
            new Vector3(-width*0.7f, 0, length - headSize),    // 4
            new Vector3(width*0.7f, 0, length - headSize),     // 5
            new Vector3(0, 0, length*1.2f),                    // 6 (tip - extended forward)
            
            // Arrow shaft (top/bottom edges for 3D)
            new Vector3(-width/4, width/4, 0),              // 7
            new Vector3(width/4, width/4, 0),               // 8
            new Vector3(width/4, width/4, length - headSize), // 9
            new Vector3(-width/4, width/4, length - headSize), // 10
            
            new Vector3(-width/4, -width/4, 0),              // 11
            new Vector3(width/4, -width/4, 0),               // 12
            new Vector3(width/4, -width/4, length - headSize), // 13
            new Vector3(-width/4, -width/4, length - headSize), // 14
            
            // Arrow head (sides for 3D)
            new Vector3(-width*0.7f, width/4, length - headSize), // 15
            new Vector3(width*0.7f, width/4, length - headSize),  // 16
            new Vector3(0, width/4, length*1.2f),                 // 17
            
            new Vector3(-width*0.7f, -width/4, length - headSize), // 18
            new Vector3(width*0.7f, -width/4, length - headSize),  // 19
            new Vector3(0, -width/4, length*1.2f)                  // 20
        };
        
        // Create triangles - this is simplified, would need all faces for a complete 3D mesh
        int[] triangles = new int[]
        {
            // Front face
            0, 2, 1, 0, 3, 2,  // shaft
            4, 6, 3, 3, 6, 2, 2, 6, 5,  // head
            
            // Back face
            1, 2, 0, 2, 3, 0,
            3, 6, 4, 2, 6, 3, 5, 6, 2,
            
            // Top face
            7, 9, 8, 7, 10, 9,
            10, 17, 9, 9, 17, 16, 15, 17, 10,
            
            // Bottom face
            12, 13, 11, 11, 13, 14,
            14, 18, 11, 13, 19, 14, 14, 19, 18,
            
            // Side faces
            0, 1, 8, 0, 8, 7,  // back
            1, 5, 16, 1, 16, 8,  // right
            5, 6, 17, 5, 17, 16,  // right-front
            6, 4, 15, 6, 15, 17,  // left-front
            4, 0, 7, 4, 7, 15,  // left
            
            // Bottom side faces
            0, 11, 1, 1, 11, 12,
            1, 12, 5, 5, 12, 19,
            5, 19, 6, 6, 19, 20,
            6, 20, 4, 4, 20, 18,
            4, 18, 0, 0, 18, 11
        };
        
        // Create simple UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x + 0.5f, vertices[i].z);
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
        
        // Calculate a position above the player with some offset
        Vector3 targetPosition = playerTransform.position + Vector3.up * floatHeight;
        
        // Add some offset in front of the player
        targetPosition += playerTransform.forward * followOffset;
        
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
            arrowRenderer.material.SetColor("_EmissionColor", arrowColor * 0.8f);
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