using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class CustomOutline : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 0.05f;
    
    private Renderer originalRenderer;
    private GameObject outlineObject;
    
    void Start()
    {
        originalRenderer = GetComponent<Renderer>();
        CreateOutlineObject();
    }
    
    void Update()
    {
        // Update color if changed in inspector
        if (outlineObject != null)
        {
            Renderer outlineRenderer = outlineObject.GetComponent<Renderer>();
            if (outlineRenderer != null && outlineRenderer.material.color != outlineColor)
            {
                outlineRenderer.material.color = outlineColor;
            }
        }
    }
    
    void CreateOutlineObject()
    {
        // Create outline object
        outlineObject = new GameObject(gameObject.name + "_Outline");
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * (1 + outlineWidth);
        
        // Copy mesh
        MeshFilter originalMeshFilter = GetComponent<MeshFilter>();
        if (originalMeshFilter != null && originalMeshFilter.sharedMesh != null)
        {
            MeshFilter outlineMeshFilter = outlineObject.AddComponent<MeshFilter>();
            outlineMeshFilter.sharedMesh = originalMeshFilter.sharedMesh;
            
            // Add renderer
            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            
            // Create colored material
            Material outlineMaterial = new Material(Shader.Find("Unlit/Color"));
            outlineMaterial.color = outlineColor;
            
            outlineRenderer.material = outlineMaterial;
            outlineRenderer.enabled = true;
            
            // Set to render before original
            outlineRenderer.sortingOrder = originalRenderer.sortingOrder - 1;
        }
        else
        {
            // Handle skinned mesh
            SkinnedMeshRenderer originalSkinned = GetComponent<SkinnedMeshRenderer>();
            if (originalSkinned != null && originalSkinned.sharedMesh != null)
            {
                SkinnedMeshRenderer outlineSkinned = outlineObject.AddComponent<SkinnedMeshRenderer>();
                outlineSkinned.sharedMesh = originalSkinned.sharedMesh;
                outlineSkinned.bones = originalSkinned.bones;
                outlineSkinned.rootBone = originalSkinned.rootBone;
                
                // Create colored material
                Material outlineMaterial = new Material(Shader.Find("Unlit/Color"));
                outlineMaterial.color = outlineColor;
                
                outlineSkinned.material = outlineMaterial;
                outlineSkinned.enabled = true;
            }
        }
    }
    
    void OnDestroy()
    {
        if (outlineObject != null)
            Destroy(outlineObject);
    }
} 