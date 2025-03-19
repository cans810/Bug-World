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
    public bool texturedSides = true;
    
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
        
        // Get the renderer and mesh
        Renderer renderer = model.GetComponent<Renderer>();
        Mesh mesh = model.GetComponent<MeshFilter>().mesh;
        
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
            
            // Apply texture to top, bottom, and optionally sides
            if (i == 1 || i == 0 || (texturedSides && !transparentSides))  
            {
                materials[i].mainTexture = myTexture;
                
                // For sides, we need to adjust UVs to use the edge of the texture
                if (texturedSides && i > 1)
                {
                    // This will be handled in the AdjustUVsForSides method
                }
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
        
        // Adjust UVs for sides if using textured sides
        if (texturedSides && !transparentSides)
        {
            AdjustUVsForSides(mesh);
        }
        
        // Name the object appropriately
        model.name = "TexturedModel";
    }
    
    private void AdjustUVsForSides(Mesh mesh)
    {
        // Get the UVs
        Vector2[] uvs = mesh.uv;
        
        // A cube has 24 vertices (4 for each face)
        // Faces are ordered: +X, -X, +Y, -Y, +Z, -Z
        // We want to adjust UVs for sides: +X(0-3), -X(4-7), +Z(16-19), -Z(20-23)
        
        // Right side (+X) - use right edge of texture
        for (int i = 0; i < 4; i++)
        {
            uvs[i].x = 1;
        }
        
        // Left side (-X) - use left edge of texture
        for (int i = 4; i < 8; i++)
        {
            uvs[i].x = 0;
        }
        
        // Front side (+Z) - use bottom edge of texture
        for (int i = 16; i < 20; i++)
        {
            uvs[i].y = 0;
        }
        
        // Back side (-Z) - use top edge of texture
        for (int i = 20; i < 24; i++)
        {
            uvs[i].y = 1;
        }
        
        // Apply the modified UVs
        mesh.uv = uvs;
    }
} 