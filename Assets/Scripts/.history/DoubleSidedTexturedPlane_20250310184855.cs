using UnityEngine;

public class DoubleSidedTexturedPlane : MonoBehaviour
{
    public Texture2D myTexture;
    [Range(0.01f, 10f)]
    public float width = 1f;
    [Range(0.01f, 10f)]
    public float height = 1f;
    [Range(0.001f, 0.1f)]
    public float spacing = 0.01f;
    public bool createSides = true;
    public Color sideColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public bool addCollider = true;
    
    void Start()
    {
        CreateDoubleSidedPlane();
    }
    
    public void CreateDoubleSidedPlane()
    {
        // Create parent object to hold all parts
        GameObject planeHolder = new GameObject("TexturedPlaneHolder");
        planeHolder.transform.SetParent(this.transform);
        planeHolder.transform.localPosition = Vector3.zero;
        
        // Create and apply material with texture and transparency
        Material textureMaterial = new Material(Shader.Find("Standard"));
        textureMaterial.SetFloat("_Mode", 3); // Transparent mode
        textureMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        textureMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        textureMaterial.SetInt("_ZWrite", 0);
        textureMaterial.DisableKeyword("_ALPHATEST_ON");
        textureMaterial.EnableKeyword("_ALPHABLEND_ON");
        textureMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        textureMaterial.renderQueue = 3000;
        textureMaterial.mainTexture = myTexture;
        
        // Create top plane
        GameObject topPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        topPlane.transform.SetParent(planeHolder.transform);
        topPlane.transform.localPosition = new Vector3(0, spacing/2, 0);
        topPlane.transform.localScale = new Vector3(width/10, 1, height/10); // Plane is 10x10 units by default
        topPlane.transform.localRotation = Quaternion.identity;
        topPlane.name = "TopPlane";
        topPlane.GetComponent<Renderer>().material = textureMaterial;
        
        // Create bottom plane (flipped)
        GameObject bottomPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        bottomPlane.transform.SetParent(planeHolder.transform);
        bottomPlane.transform.localPosition = new Vector3(0, -spacing/2, 0);
        bottomPlane.transform.localScale = new Vector3(width/10, 1, height/10);
        bottomPlane.transform.localRotation = Quaternion.Euler(180, 0, 0); // Flip it
        bottomPlane.name = "BottomPlane";
        bottomPlane.GetComponent<Renderer>().material = textureMaterial;
        
        // Create sides if requested
        if (createSides)
        {
            // Create material for sides
            Material sideMaterial = new Material(Shader.Find("Standard"));
            sideMaterial.SetFloat("_Mode", 3); // Transparent mode
            sideMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sideMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sideMaterial.SetInt("_ZWrite", 0);
            sideMaterial.DisableKeyword("_ALPHATEST_ON");
            sideMaterial.EnableKeyword("_ALPHABLEND_ON");
            sideMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            sideMaterial.renderQueue = 3000;
            sideMaterial.color = sideColor;
            
            // Create four sides
            CreateSide(planeHolder.transform, "LeftSide", new Vector3(-width/2, 0, 0), 
                       new Vector3(0, 0, 90), new Vector3(spacing/10, 1, height/10), sideMaterial);
            
            CreateSide(planeHolder.transform, "RightSide", new Vector3(width/2, 0, 0), 
                       new Vector3(0, 0, -90), new Vector3(spacing/10, 1, height/10), sideMaterial);
            
            CreateSide(planeHolder.transform, "FrontSide", new Vector3(0, 0, height/2), 
                       new Vector3(90, 0, 0), new Vector3(width/10, 1, spacing/10), sideMaterial);
            
            CreateSide(planeHolder.transform, "BackSide", new Vector3(0, 0, -height/2), 
                       new Vector3(-90, 0, 0), new Vector3(width/10, 1, spacing/10), sideMaterial);
        }
        
        // Add box collider to the parent object (this gameObject)
        if (addCollider)
        {
            BoxCollider boxCollider = this.gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(width, spacing, height);
            boxCollider.center = Vector3.zero;
        }
    }
    
    private void CreateSide(Transform parent, string name, Vector3 position, 
                           Vector3 rotation, Vector3 scale, Material material)
    {
        GameObject side = GameObject.CreatePrimitive(PrimitiveType.Plane);
        side.transform.SetParent(parent);
        side.transform.localPosition = position;
        side.transform.localRotation = Quaternion.Euler(rotation);
        side.transform.localScale = scale;
        side.name = name;
        side.GetComponent<Renderer>().material = material;
    }
} 