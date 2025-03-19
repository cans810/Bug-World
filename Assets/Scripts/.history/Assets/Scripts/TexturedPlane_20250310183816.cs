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
            materials[i] = new Material(Shader.Find("Standard"));
            
            // Apply texture only to top (index 1) and bottom (index 0) faces
            if (i == 1 || i == 0)  // Unity's cube material indices: 0=bottom, 1=top
            {
                materials[i].mainTexture = myTexture;
            }
            else
            {
                materials[i].color = sideColor;
            }
        }
        
        // Apply all materials to the renderer
        renderer.materials = materials;
        
        // Name the object appropriately
        model.name = "TexturedModel";
    }
} 