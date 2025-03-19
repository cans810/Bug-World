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
        Mesh mesh = GenerateMeshFromTexture();
        meshFilter.mesh = mesh;
        
        // Create material with the texture
        Material material = new Material(Shader.Find("Standard"));
        material.mainTexture = myTexture;
        meshRenderer.material = material;
        
        // Add collider if requested
        if (addCollider)
        {
            MeshCollider meshCollider = this.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            
            // Also add a box collider for simpler physics
            BoxCollider boxCollider = this.gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(width, thickness, height);
            boxCollider.center = new Vector3(0, 0, 0);
        }
    }
    
    private Mesh GenerateMeshFromTexture()
    {
        // Make sure we can read the texture data
        if (!myTexture.isReadable)
        {
            Debug.LogError("Texture is not readable! Please enable 'Read/Write Enabled' in the texture import settings.");
            return CreateSimpleCubeMesh();
        }
        
        Color[] pixels = myTexture.GetPixels();
        int width = myTexture.width;
        int height = myTexture.height;
        
        // Create lists to hold mesh data
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Scale factors to map texture coordinates to world space
        float scaleX = this.width / width;
        float scaleZ = this.height / height;
        
        // Create top and bottom faces based on non-transparent pixels
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[y * width + x];
                
                // Skip transparent pixels
                if (pixel.a < alphaThreshold / 255f)
                    continue;
                
                // Check if neighboring pixels are transparent to determine if we need to create a face
                bool createTop = true;
                bool createBottom = true;
                bool createLeft = x == 0 || pixels[y * width + (x - 1)].a < alphaThreshold / 255f;
                bool createRight = x == width - 1 || pixels[y * width + (x + 1)].a < alphaThreshold / 255f;
                bool createFront = y == 0 || pixels[(y - 1) * width + x].a < alphaThreshold / 255f;
                bool createBack = y == height - 1 || pixels[(y + 1) * width + x].a < alphaThreshold / 255f;
                
                // Calculate world space coordinates
                float worldX = (x - width / 2f) * scaleX;
                float worldZ = (y - height / 2f) * scaleZ;
                
                // Create a cube for this pixel
                AddCube(vertices, triangles, uvs, worldX, worldZ, scaleX, scaleZ, 
                        createTop, createBottom, createLeft, createRight, createFront, createBack);
            }
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
    
    private void AddCube(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, 
                         float x, float z, float sizeX, float sizeZ,
                         bool top, bool bottom, bool left, bool right, bool front, bool back)
    {
        float halfSizeX = sizeX * 0.5f;
        float halfSizeZ = sizeZ * 0.5f;
        float halfThickness = thickness * 0.5f;
        
        // Define the 8 corners of the cube
        Vector3 p0 = new Vector3(x - halfSizeX, -halfThickness, z - halfSizeZ);
        Vector3 p1 = new Vector3(x + halfSizeX, -halfThickness, z - halfSizeZ);
        Vector3 p2 = new Vector3(x + halfSizeX, -halfThickness, z + halfSizeZ);
        Vector3 p3 = new Vector3(x - halfSizeX, -halfThickness, z + halfSizeZ);
        Vector3 p4 = new Vector3(x - halfSizeX, halfThickness, z - halfSizeZ);
        Vector3 p5 = new Vector3(x + halfSizeX, halfThickness, z - halfSizeZ);
        Vector3 p6 = new Vector3(x + halfSizeX, halfThickness, z + halfSizeZ);
        Vector3 p7 = new Vector3(x - halfSizeX, halfThickness, z + halfSizeZ);
        
        int baseIndex = vertices.Count;
        
        // Add vertices
        vertices.Add(p0); vertices.Add(p1); vertices.Add(p2); vertices.Add(p3); // Bottom
        vertices.Add(p4); vertices.Add(p5); vertices.Add(p6); vertices.Add(p7); // Top
        
        // Add UVs (simple mapping)
        float u = (x / this.width) + 0.5f;
        float v = (z / this.height) + 0.5f;
        
        for (int i = 0; i < 8; i++)
        {
            uvs.Add(new Vector2(u, v));
        }
        
        // Add faces (triangles)
        if (bottom)
        {
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
            
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 2);
        }
        
        if (top)
        {
            triangles.Add(baseIndex + 4);
            triangles.Add(baseIndex + 5);
            triangles.Add(baseIndex + 6);
            
            triangles.Add(baseIndex + 4);
            triangles.Add(baseIndex + 6);
            triangles.Add(baseIndex + 7);
        }
        
        if (left)
        {
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 4);
            triangles.Add(baseIndex + 7);
            
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 7);
            triangles.Add(baseIndex + 3);
        }
        
        if (right)
        {
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 6);
            
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 6);
            triangles.Add(baseIndex + 5);
        }
        
        if (front)
        {
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 5);
            
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 5);
            triangles.Add(baseIndex + 4);
        }
        
        if (back)
        {
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 7);
            
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 7);
            triangles.Add(baseIndex + 6);
        }
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