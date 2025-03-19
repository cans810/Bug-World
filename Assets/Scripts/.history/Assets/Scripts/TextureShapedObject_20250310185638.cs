using UnityEngine;
using System.Collections.Generic;

public class TextureShapedObject : MonoBehaviour
{
    public Texture2D myTexture;
    [Range(0.001f, 1f)]
    public float thickness = 0.1f;
    [Range(0.01f, 10f)]
    public float width = 1f;
    [Range(0.01f, 10f)]
    public float height = 1f;
    [Range(0, 255)]
    public byte alphaThreshold = 128;
    public bool addCollider = true;
    public bool useTransparentMaterial = true;
    
    void Start()
    {
        CreateShapedObject();
    }
    
    public void CreateShapedObject()
    {
        if (myTexture == null)
        {
            Debug.LogError("Texture is not assigned!");
            return;
        }
        
        // Create a new game object for our mesh
        GameObject shapedObject = new GameObject("ShapedObject");
        shapedObject.transform.SetParent(transform);
        shapedObject.transform.localPosition = Vector3.zero;
        
        // Add mesh components
        MeshFilter meshFilter = shapedObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = shapedObject.AddComponent<MeshRenderer>();
        
        // Create the mesh based on texture transparency
        Mesh mesh = GenerateExtrudedMeshFromTexture();
        meshFilter.mesh = mesh;
        
        // Create material with the texture
        Material material;
        if (useTransparentMaterial)
        {
            material = new Material(Shader.Find("Standard"));
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        else
        {
            material = new Material(Shader.Find("Standard"));
        }
        
        material.mainTexture = myTexture;
        meshRenderer.material = material;
        
        // Add collider if requested
        if (addCollider)
        {
            MeshCollider meshCollider = this.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }
    }
    
    private Mesh GenerateExtrudedMeshFromTexture()
    {
        // Make sure we can read the texture data
        if (!myTexture.isReadable)
        {
            Debug.LogError("Texture is not readable! Please enable 'Read/Write Enabled' in the texture import settings.");
            return CreateSimpleCubeMesh();
        }
        
        Color[] pixels = myTexture.GetPixels();
        int texWidth = myTexture.width;
        int texHeight = myTexture.height;
        
        // Create a binary representation of the texture (transparent vs non-transparent)
        bool[,] transparencyMap = new bool[texWidth, texHeight];
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                Color pixel = pixels[y * texWidth + x];
                transparencyMap[x, y] = pixel.a >= alphaThreshold / 255f;
            }
        }
        
        // Find the contours of the shape
        List<Vector2> contourPoints = FindContourPoints(transparencyMap, texWidth, texHeight);
        
        // Create the mesh
        return CreateExtrudedMesh(contourPoints, texWidth, texHeight);
    }
    
    private List<Vector2> FindContourPoints(bool[,] transparencyMap, int texWidth, int texHeight)
    {
        List<Vector2> contourPoints = new List<Vector2>();
        
        // Scan the texture for edges
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                if (!transparencyMap[x, y]) continue;
                
                // Check if this pixel is on an edge
                bool isEdge = false;
                
                // Check neighboring pixels (4-connected)
                if (x > 0 && !transparencyMap[x-1, y]) isEdge = true;
                else if (x < texWidth-1 && !transparencyMap[x+1, y]) isEdge = true;
                else if (y > 0 && !transparencyMap[x, y-1]) isEdge = true;
                else if (y < texHeight-1 && !transparencyMap[x, y+1]) isEdge = true;
                
                // Also check diagonals (8-connected)
                else if (x > 0 && y > 0 && !transparencyMap[x-1, y-1]) isEdge = true;
                else if (x < texWidth-1 && y > 0 && !transparencyMap[x+1, y-1]) isEdge = true;
                else if (x > 0 && y < texHeight-1 && !transparencyMap[x-1, y+1]) isEdge = true;
                else if (x < texWidth-1 && y < texHeight-1 && !transparencyMap[x+1, y+1]) isEdge = true;
                
                if (isEdge)
                {
                    contourPoints.Add(new Vector2(x, y));
                }
            }
        }
        
        return contourPoints;
    }
    
    private Mesh CreateExtrudedMesh(List<Vector2> contourPoints, int texWidth, int texHeight)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Scale factors to map texture coordinates to world space
        float scaleX = this.width / texWidth;
        float scaleZ = this.height / texHeight;
        float halfThickness = thickness / 2f;
        
        // Create top and bottom vertices for each contour point
        foreach (Vector2 point in contourPoints)
        {
            float worldX = (point.x - texWidth / 2f) * scaleX;
            float worldZ = (point.y - texHeight / 2f) * scaleZ;
            
            // Add top vertex
            vertices.Add(new Vector3(worldX, halfThickness, worldZ));
            uvs.Add(new Vector2(point.x / texWidth, point.y / texHeight));
            
            // Add bottom vertex
            vertices.Add(new Vector3(worldX, -halfThickness, worldZ));
            uvs.Add(new Vector2(point.x / texWidth, point.y / texHeight));
        }
        
        // Create triangles for the sides
        for (int i = 0; i < contourPoints.Count; i++)
        {
            int nextI = (i + 1) % contourPoints.Count;
            
            int topCurrent = i * 2;
            int bottomCurrent = i * 2 + 1;
            int topNext = nextI * 2;
            int bottomNext = nextI * 2 + 1;
            
            // First triangle of the quad
            triangles.Add(topCurrent);
            triangles.Add(bottomCurrent);
            triangles.Add(topNext);
            
            // Second triangle of the quad
            triangles.Add(bottomCurrent);
            triangles.Add(bottomNext);
            triangles.Add(topNext);
        }
        
        // Create triangles for the top and bottom faces
        // This is more complex and would require triangulation of the contour
        // For simplicity, we'll use a basic approach that works for convex shapes
        
        // Top face
        for (int i = 2; i < contourPoints.Count; i++)
        {
            triangles.Add(0);
            triangles.Add((i-1) * 2);
            triangles.Add(i * 2);
        }
        
        // Bottom face
        for (int i = 2; i < contourPoints.Count; i++)
        {
            triangles.Add(1);
            triangles.Add(i * 2 + 1);
            triangles.Add((i-1) * 2 + 1);
        }
        
        // Create and return the mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    private Mesh CreateSimpleCubeMesh()
    {
        // Fallback if texture isn't readable
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cubeMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tempCube);
        return cubeMesh;
    }
} 