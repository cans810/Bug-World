using UnityEngine;

public class SimplePlayerOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 0.05f;
    
    private Material outlineMaterial;
    private Renderer[] renderers;
    private GameObject[] outlineObjects;
    
    void Start()
    {
        // Create outline material
        outlineMaterial = new Material(Shader.Find("Standard"));
        outlineMaterial.SetFloat("_Mode", 3); // Transparent mode
        outlineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        outlineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        outlineMaterial.SetInt("_ZWrite", 0);
        outlineMaterial.DisableKeyword("_ALPHATEST_ON");
        outlineMaterial.EnableKeyword("_ALPHABLEND_ON");
        outlineMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        outlineMaterial.renderQueue = 3000;
        outlineMaterial.color = outlineColor;
        
        // Get all renderers in this object and children
        renderers = GetComponentsInChildren<Renderer>();
        
        // Create outline objects
        CreateOutlineObjects();
    }
    
    void CreateOutlineObjects()
    {
        outlineObjects = new GameObject[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer meshRenderer = renderers[i] as MeshRenderer;
            SkinnedMeshRenderer skinnedMeshRenderer = renderers[i] as SkinnedMeshRenderer;
            
            if (meshRenderer != null && meshRenderer.gameObject.GetComponent<MeshFilter>())
            {
                GameObject outlineObject = new GameObject(renderers[i].gameObject.name + "_Outline");
                outlineObjects[i] = outlineObject;
                
                outlineObject.transform.position = renderers[i].transform.position;
                outlineObject.transform.rotation = renderers[i].transform.rotation;
                outlineObject.transform.localScale = renderers[i].transform.localScale * (1 + outlineWidth);
                outlineObject.transform.parent = renderers[i].transform;
                
                MeshFilter meshFilter = outlineObject.AddComponent<MeshFilter>();
                meshFilter.mesh = renderers[i].gameObject.GetComponent<MeshFilter>().mesh;
                
                MeshRenderer renderer = outlineObject.AddComponent<MeshRenderer>();
                renderer.material = outlineMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else if (skinnedMeshRenderer != null)
            {
                GameObject outlineObject = new GameObject(renderers[i].gameObject.name + "_Outline");
                outlineObjects[i] = outlineObject;
                
                outlineObject.transform.position = renderers[i].transform.position;
                outlineObject.transform.rotation = renderers[i].transform.rotation;
                outlineObject.transform.localScale = renderers[i].transform.localScale * (1 + outlineWidth);
                outlineObject.transform.parent = renderers[i].transform;
                
                SkinnedMeshRenderer renderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = skinnedMeshRenderer.sharedMesh;
                renderer.bones = skinnedMeshRenderer.bones;
                renderer.material = outlineMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up outline objects when this is destroyed
        if (outlineObjects != null)
        {
            foreach (GameObject obj in outlineObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
        }
    }
} 