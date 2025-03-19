using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    [Header("Boundary Settings")]
    [SerializeField] private Vector2 mapSize = new Vector2(100f, 100f); // Width x Length of the map
    [SerializeField] private float wallHeight = 10f; // Height of invisible walls
    [SerializeField] private bool createPhysicalBoundaries = true;
    
    [Header("Visual Settings")]
    [SerializeField] private bool showVisualBoundaries = true;
    [SerializeField] private Color boundaryColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private bool alignWithTerrain = true;
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private float castHeight = 50f; // Height to raycast from
    
    private GameObject[] walls = new GameObject[4];
    private Transform boundaryParent;
    
    private void Awake()
    {
        if (createPhysicalBoundaries)
        {
            CreateBoundaries();
        }
    }
    
    private void CreateBoundaries()
    {
        // Create a parent for all boundaries
        boundaryParent = new GameObject("Map_Boundaries").transform;
        boundaryParent.position = transform.position;
        
        // Create the four walls
        CreateWall("North_Wall", Vector3.forward * mapSize.y/2, new Vector3(mapSize.x, wallHeight, 1));
        CreateWall("South_Wall", Vector3.back * mapSize.y/2, new Vector3(mapSize.x, wallHeight, 1));
        CreateWall("East_Wall", Vector3.right * mapSize.x/2, new Vector3(1, wallHeight, mapSize.y));
        CreateWall("West_Wall", Vector3.left * mapSize.x/2, new Vector3(1, wallHeight, mapSize.y));
        
        // If desired, align walls with terrain height
        if (alignWithTerrain)
        {
            AlignWallsWithTerrain();
        }
    }
    
    private void CreateWall(string name, Vector3 position, Vector3 size)
    {
        // Create wall object
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(boundaryParent);
        
        // Position relative to map center
        wall.transform.position = transform.position + position;
        
        // Add collider
        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
        
        // Make the walls invisible but physical
        MeshRenderer renderer = wall.AddComponent<MeshRenderer>();
        renderer.enabled = showVisualBoundaries;
        
        if (showVisualBoundaries)
        {
            // Create a simple material for visualization
            Material boundaryMaterial = new Material(Shader.Find("Transparent/Diffuse"));
            boundaryMaterial.color = boundaryColor;
            renderer.material = boundaryMaterial;
            
            // Add mesh filter with cube mesh for visualization
            MeshFilter meshFilter = wall.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateCubeMesh(size);
        }
        
        // Store reference to wall
        int index = -1;
        if (name.Contains("North")) index = 0;
        else if (name.Contains("South")) index = 1;
        else if (name.Contains("East")) index = 2;
        else if (name.Contains("West")) index = 3;
        
        if (index >= 0 && index < walls.Length)
        {
            walls[index] = wall;
        }
    }
    
    private void AlignWallsWithTerrain()
    {
        // For each wall, raycast to find terrain height
        foreach (GameObject wall in walls)
        {
            if (wall == null) continue;
            
            // Sample multiple points along the wall
            BoxCollider collider = wall.GetComponent<BoxCollider>();
            Vector3 size = collider.size;
            Vector3 center = wall.transform.position;
            
            // Determine if this is a N/S or E/W wall
            bool isNorthSouth = wall.name.Contains("North") || wall.name.Contains("South");
            
            // Sample points to determine average terrain height
            float totalHeight = 0f;
            int validSamples = 0;
            int sampleCount = 5; // Number of samples along the wall length
            
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3 samplePoint = center;
                
                if (isNorthSouth)
                {
                    // Sample along X axis for North/South walls
                    float offset = (i / (float)(sampleCount - 1) - 0.5f) * size.x;
                    samplePoint += Vector3.right * offset;
                }
                else
                {
                    // Sample along Z axis for East/West walls
                    float offset = (i / (float)(sampleCount - 1) - 0.5f) * size.z;
                    samplePoint += Vector3.forward * offset;
                }
                
                // Raycast down to find terrain
                RaycastHit hit;
                if (Physics.Raycast(samplePoint + Vector3.up * castHeight, Vector3.down, out hit, castHeight * 2, terrainLayer))
                {
                    totalHeight += hit.point.y;
                    validSamples++;
                }
            }
            
            // If we got valid samples, adjust the wall height
            if (validSamples > 0)
            {
                float avgHeight = totalHeight / validSamples;
                Vector3 pos = wall.transform.position;
                pos.y = avgHeight + (wallHeight / 2); // Center the wall vertically at terrain height
                wall.transform.position = pos;
            }
        }
    }
    
    private Mesh CreateCubeMesh(Vector3 size)
    {
        Mesh mesh = new Mesh();
        
        // Vertices (corners of a cube)
        Vector3[] vertices = new Vector3[8];
        vertices[0] = new Vector3(-size.x/2, -size.y/2, -size.z/2);
        vertices[1] = new Vector3(size.x/2, -size.y/2, -size.z/2);
        vertices[2] = new Vector3(size.x/2, -size.y/2, size.z/2);
        vertices[3] = new Vector3(-size.x/2, -size.y/2, size.z/2);
        vertices[4] = new Vector3(-size.x/2, size.y/2, -size.z/2);
        vertices[5] = new Vector3(size.x/2, size.y/2, -size.z/2);
        vertices[6] = new Vector3(size.x/2, size.y/2, size.z/2);
        vertices[7] = new Vector3(-size.x/2, size.y/2, size.z/2);
        
        // Triangles (6 faces, 2 triangles each = 12 triangles with 3 indices each = 36 indices)
        int[] triangles = new int[36];
        
        // Bottom face
        triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
        triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
        
        // Top face
        triangles[6] = 4; triangles[7] = 5; triangles[8] = 6;
        triangles[9] = 4; triangles[10] = 6; triangles[11] = 7;
        
        // Front face
        triangles[12] = 0; triangles[13] = 1; triangles[14] = 5;
        triangles[15] = 0; triangles[16] = 5; triangles[17] = 4;
        
        // Back face
        triangles[18] = 2; triangles[19] = 3; triangles[20] = 7;
        triangles[21] = 2; triangles[22] = 7; triangles[23] = 6;
        
        // Left face
        triangles[24] = 0; triangles[25] = 4; triangles[26] = 7;
        triangles[27] = 0; triangles[28] = 7; triangles[29] = 3;
        
        // Right face
        triangles[30] = 1; triangles[31] = 2; triangles[32] = 6;
        triangles[33] = 1; triangles[34] = 6; triangles[35] = 5;
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showVisualBoundaries) return;
        
        Gizmos.color = boundaryColor;
        Vector3 center = transform.position;
        Vector3 size = new Vector3(mapSize.x, wallHeight, mapSize.y);
        
        // Draw wireframe box
        Gizmos.DrawWireCube(center, size);
    }
    
    // Utility method to check if a position is within bounds (can be used by other scripts)
    public bool IsWithinBounds(Vector3 position)
    {
        Vector3 localPos = position - transform.position;
        return Mathf.Abs(localPos.x) < mapSize.x/2 && Mathf.Abs(localPos.z) < mapSize.y/2;
    }
    
    // Method to get the nearest point inside bounds (useful for steering entities back into bounds)
    public Vector3 GetNearestPointInBounds(Vector3 position)
    {
        Vector3 localPos = position - transform.position;
        localPos.x = Mathf.Clamp(localPos.x, -mapSize.x/2, mapSize.x/2);
        localPos.z = Mathf.Clamp(localPos.z, -mapSize.y/2, mapSize.y/2);
        return transform.position + localPos;
    }
} 