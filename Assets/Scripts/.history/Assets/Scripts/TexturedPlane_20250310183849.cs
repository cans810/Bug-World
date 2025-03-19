using UnityEngine;

public class TexturedPlane : MonoBehaviour
{
    public Texture2D myTexture;
    [Range(0.01f, 10f)]
    public float width = 1f;
    [Range(0.01f, 10f)]
    public float height = 1f;
    [Range(0.001f, 1f)]
    public float thickness = 0.01f;
    public Color sideColor = Color.gray;
    public bool transparentSides = false;
    
    void Start()
    {
        CreateTexturedModel();
    }
    
    public void CreateTexturedModel()
    {
        // Create a cube as our base object
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.transform.SetParent(this.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localScale = new Vector3(width, thickness, height);
        
        // Get the renderer
        Renderer renderer = model.GetComponent<Renderer>();
        
        // Create materials for each face of the cube
        Material[] materials = new Material[6];
        
        for (int i = 0; i < 6; i++)
        {
            // Create material with transparency support
            materials[i] = new Material(Shader.Find("Standard"));
            materials[i].SetFloat("_Mode", 3); // Transparent mode
            materials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            materials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            materials[i].SetInt("_ZWrite", 0);
            materials[i].DisableKeyword("_ALPHATEST_ON");
            materials[i].EnableKeyword("_ALPHABLEND_ON");
            materials[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
            materials[i].renderQueue = 3000;
            
            // Apply texture only to top (index 1) and bottom (index 0) faces
            if (i == 1 || i == 0)  // Unity's cube material indices: 0=bottom, 1=top
            {
                materials[i].mainTexture = myTexture;
            }
            else
            {
                if (transparentSides)
                {
                    // Make sides completely transparent
                    materials[i].color = new Color(0, 0, 0, 0);
                }
                else
                {
                    // Use the specified side color
                    materials[i].color = sideColor;
                }
            }
        }
        
        // Apply all materials to the renderer
        renderer.materials = materials;
        
        // Name the object appropriately
        model.name = "TexturedModel";
    }
} 