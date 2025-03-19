using UnityEngine;

public class SimplePlayerOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 0.05f;
    [SerializeField] private bool updateInRealtime = true;
    
    private Material outlineMaterial;
    private Renderer[] renderers;
    private GameObject[] outlineObjects;
    private Color lastColor;
    
    void Start()
    {
        CreateOutlineMaterial();
        
        // Get all renderers in this object and children
        renderers = GetComponentsInChildren<Renderer>();
        
        // Create outline objects
        CreateOutlineObjects();
        
        // Store initial color
        lastColor = outlineColor;
    }
    
    void Update()
    {
        // Check if the color has changed
        if (updateInRealtime && outlineColor != lastColor)
        {
            UpdateOutlineColor();
        }
    }
    
    void CreateOutlineMaterial()
    {
        // Create outline material using an unlit shader for better color control
        outlineMaterial = new Material(Shader.Find("Unlit/Color"));
        
        // If Unlit/Color isn't available, try standard transparent
        if (outlineMaterial.shader == null)
        {
            outlineMaterial = new Material(Shader.Find("Standard"));
            outlineMaterial.SetFloat("_Mode", 3); // Transparent mode
            outlineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            outlineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            outlineMaterial.SetInt("_ZWrite", 0);
            outlineMaterial.DisableKeyword("_ALPHATEST_ON");
            outlineMaterial.EnableKeyword("_ALPHABLEND_ON");
            outlineMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            outlineMaterial.renderQueue = 3000;
        }
        
        // Force color application - this is important
        outlineMaterial.color = outlineColor;
        outlineMaterial.SetColor("_EmissionColor", outlineColor);
        outlineMaterial.EnableKeyword("_EMISSION");
    }
    
    void UpdateOutlineColor()
    {
        if (outlineMaterial != null)
        {
            outlineMaterial.color = outlineColor;
            outlineMaterial.SetColor("_EmissionColor", outlineColor);
            lastColor = outlineColor;
            
            // Force material update on all renderers
            if (outlineObjects != null)
            {
                foreach (GameObject obj in outlineObjects)
                {
                    if (obj != null)
                    {
                        Renderer renderer = obj.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material.color = outlineColor;
                            renderer.material.SetColor("_EmissionColor", outlineColor);
                        }
                    }
                }
            }
        }
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
                
                // Create a new instance of the material for each renderer
                renderer.material = new Material(outlineMaterial);
                renderer.material.color = outlineColor;  // Force color application
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
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
                
                // Create a new instance of the material for each renderer
                renderer.material = new Material(outlineMaterial);
                renderer.material.color = outlineColor;  // Force color application
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }
    
    // Method that can be called from other scripts to change outline color
    public void SetOutlineColor(Color newColor)
    {
        outlineColor = newColor;
        UpdateOutlineColor();
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