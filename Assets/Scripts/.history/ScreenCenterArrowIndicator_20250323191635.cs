using UnityEngine;
using System.Collections;

public class ScreenCenterArrowIndicator : MonoBehaviour
{
    [Header("Arrow Appearance")]
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float arrowSize = 0.1f;
    [SerializeField] private float distanceFromCamera = 0.5f;
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private float hideDistance = 2f;
    
    // References
    private GameObject arrowObject;
    private MeshRenderer arrowRenderer;
    private bool isShowing = false;
    private Transform playerTransform;
    private Camera mainCamera;
    
    private void Awake()
    {
        // Find the player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("ScreenCenterArrowIndicator: Player not found! Make sure the player has the 'Player' tag.");
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("ScreenCenterArrowIndicator: Main camera not found!");
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
            if (distanceToTarget <= hideDistance)
            {
                arrowObject.SetActive(false);
                
                // If we've reached the target and should destroy, clean up
                if (distanceToTarget <= hideDistance / 2f)
                {
                    HideArrow();
                }
            }
            else
            {
                arrowObject.SetActive(true);
            }
        }
    }
    
    private void CreateArrowMesh()
    {
        // Create a new game object for the arrow
        arrowObject = new GameObject("ScreenCenterArrow");
        arrowObject.transform.parent = transform;
        
        // Add mesh filter
        MeshFilter meshFilter = arrowObject.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateArrowMesh(0.2f, 0.4f, 0.1f); // width, length, head size
        
        // Add mesh renderer and set material
        arrowRenderer = arrowObject.AddComponent<MeshRenderer>();
        Material arrowMaterial = new Material(Shader.Find("Standard"));
        arrowMaterial.color = arrowColor;
        arrowRenderer.material = arrowMaterial;
        
        // Position in front of camera
        PositionArrowInView();
        
        // Initially hide the arrow
        arrowObject.SetActive(false);
    }
    
    private Mesh CreateArrowMesh(float width, float length, float headSize)
    {
        Mesh mesh = new Mesh();
        
        // Create vertices for a simple arrow shape
        Vector3[] vertices = new Vector3[]
        {
            // Arrow shaft
            new Vector3(-width/2, 0, 0),                  // 0
            new Vector3(width/2, 0, 0),                   // 1
            new Vector3(width/2, 0, length - headSize),   // 2
            new Vector3(-width/2, 0, length - headSize),  // 3
            
            // Arrow head
            new Vector3(-width, 0, length - headSize),    // 4
            new Vector3(width, 0, length - headSize),     // 5
            new Vector3(0, 0, length)                     // 6 (tip)
        };
        
        // Create triangles
        int[] triangles = new int[]
        {
            // Shaft quad (2 triangles)
            0, 2, 1,
            0, 3, 2,
            
            // Head triangles
            4, 6, 3,
            3, 6, 2,
            2, 6, 5,
            
            // Back side of shaft
            1, 2, 0,
            2, 3, 0,
            
            // Back side of head
            3, 6, 4,
            2, 6, 3,
            5, 6, 2
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
    
    private void PositionArrowInView()
    {
        if (mainCamera == null || arrowObject == null) return;
        
        // Position the arrow in the center of the screen, slightly in front of the camera
        arrowObject.transform.position = mainCamera.transform.position + mainCamera.transform.forward * distanceFromCamera;
        
        // Scale the arrow based on arrowSize
        arrowObject.transform.localScale = Vector3.one * arrowSize;
    }
    
    private void UpdateArrowPosition()
    {
        // Keep the arrow at a fixed distance in front of the camera
        arrowObject.transform.position = mainCamera.transform.position + mainCamera.transform.forward * distanceFromCamera;
        
        // Apply pulsing effect to scale
        float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
        arrowObject.transform.localScale = Vector3.one * arrowSize * pulse;
    }
    
    private void UpdateArrowRotation()
    {
        if (target == null) return;
        
        // Calculate direction to target in world space
        Vector3 directionToTarget = target.position - playerTransform.position;
        
        // Project the direction onto the camera's plane
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        Vector3 cameraUp = mainCamera.transform.up;
        
        // Get the angle between the direction to target and camera forward in the XZ plane (yaw)
        Vector3 directionXZ = new Vector3(directionToTarget.x, 0, directionToTarget.z).normalized;
        Vector3 cameraForwardXZ = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
        
        // Calculate the signed angle between the camera's forward and the direction to target
        float angleY = Vector3.SignedAngle(cameraForwardXZ, directionXZ, Vector3.up);
        
        // Calculate pitch angle for up/down pointing
        Vector3 directionXY = new Vector3(directionToTarget.x, directionToTarget.y, directionToTarget.z).normalized;
        float angleX = Vector3.SignedAngle(directionXZ, directionXY, Vector3.Cross(directionXZ, Vector3.up));
        
        // Create rotation from angles
        Quaternion targetRotation = Quaternion.Euler(angleX, angleY, 0);
        
        // Apply rotation relative to the camera
        arrowObject.transform.rotation = mainCamera.transform.rotation * targetRotation;
        
        // Rotate arrow to point in the right direction (arrow points along Z)
        arrowObject.transform.Rotate(90, 0, 0);
    }
    
    public void ShowArrow()
    {
        if (target == null)
        {
            Debug.LogError("ScreenCenterArrowIndicator: Cannot show arrow - target is null!");
            return;
        }
        
        arrowObject.SetActive(true);
        isShowing = true;
        
        Debug.Log($"Screen center arrow now visible and pointing to {target.name}");
    }
    
    public void HideArrow()
    {
        if (arrowObject != null)
        {
            arrowObject.SetActive(false);
        }
        isShowing = false;
        
        Debug.Log("Screen center arrow is now hidden");
    }
    
    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogError("ScreenCenterArrowIndicator: Cannot set null target!");
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