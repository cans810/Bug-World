using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Underwater Environment")]
    [SerializeField] private Material waterMaterial;
    [SerializeField] private Material groundMaterial;
    [SerializeField] private Color waterColor = new Color(0.2f, 0.5f, 0.8f, 0.6f);
    [SerializeField] private Color groundColor = new Color(0.3f, 0.2f, 0.1f, 1f);
    [SerializeField] private float waterSurfaceHeight = 10f;
    [SerializeField] private float seaFloorDepth = -10f;
    [SerializeField] private float seaSize = 50f;
    
    private GameObject waterSurface;
    private GameObject seaFloor;
    
    // Start is called before the first frame update
    void Start()
    {
        CreateWaterEnvironment();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void CreateWaterEnvironment()
    {
        // Create water surface
        waterSurface = CreatePlane("WaterSurface", seaSize, waterSurfaceHeight, waterMaterial);
        if (waterMaterial == null)
        {
            Renderer waterRenderer = waterSurface.GetComponent<Renderer>();
            waterRenderer.material = new Material(Shader.Find("Standard"));
            waterRenderer.material.color = waterColor;
            waterRenderer.material.SetFloat("_Glossiness", 0.8f);
            waterRenderer.material.SetFloat("_Metallic", 0.2f);
            waterRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterRenderer.material.EnableKeyword("_ALPHABLEND_ON");
            waterRenderer.material.renderQueue = 3000;
            waterRenderer.material.SetInt("_ZWrite", 0);
        }
        
        // Create sea floor
        seaFloor = CreatePlane("SeaFloor", seaSize, seaFloorDepth, groundMaterial);
        if (groundMaterial == null)
        {
            Renderer groundRenderer = seaFloor.GetComponent<Renderer>();
            groundRenderer.material = new Material(Shader.Find("Standard"));
            groundRenderer.material.color = groundColor;
            groundRenderer.material.SetFloat("_Glossiness", 0.1f);
        }
        
        // Add some random terrain variation to the sea floor
        AddTerrainVariation(seaFloor);
    }
    
    private GameObject CreatePlane(string name, float size, float height, Material material)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = name;
        plane.transform.localScale = new Vector3(size/10f, 1f, size/10f); // Plane is 10x10 units by default
        plane.transform.position = new Vector3(0f, height, 0f);
        
        return plane;
    }
    
    private void AddTerrainVariation(GameObject terrain)
    {
        // Get the mesh to modify
        Mesh mesh = terrain.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        
        // Add some random height variation to create a low-poly look
        for (int i = 0; i < vertices.Length; i++)
        {
            // Skip the edges to keep the boundaries flat
            if (IsEdgeVertex(i, mesh))
                continue;
                
            // Add random height variation
            float noise = Mathf.PerlinNoise(vertices[i].x * 0.1f, vertices[i].z * 0.1f);
            vertices[i].y += noise * 2f - 1f;
        }
        
        // Update the mesh
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // Add mesh collider for physics interactions
        MeshCollider collider = terrain.GetComponent<MeshCollider>();
        if (collider == null)
            terrain.AddComponent<MeshCollider>();
        else
            collider.sharedMesh = mesh;
    }
    
    private bool IsEdgeVertex(int index, Mesh mesh)
    {
        // A simple check to determine if a vertex is on the edge of the plane
        Vector3[] vertices = mesh.vertices;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        
        // Find the bounds
        foreach (Vector3 v in vertices)
        {
            minX = Mathf.Min(minX, v.x);
            maxX = Mathf.Max(maxX, v.x);
            minZ = Mathf.Min(minZ, v.z);
            maxZ = Mathf.Max(maxZ, v.z);
        }
        
        // Check if the vertex is on the edge
        Vector3 v = vertices[index];
        float tolerance = 0.01f;
        return Mathf.Abs(v.x - minX) < tolerance || Mathf.Abs(v.x - maxX) < tolerance ||
               Mathf.Abs(v.z - minZ) < tolerance || Mathf.Abs(v.z - maxZ) < tolerance;
    }
}
