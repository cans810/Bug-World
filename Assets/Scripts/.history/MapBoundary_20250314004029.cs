using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    [Header("Boundary Settings")]
    [SerializeField] private float boundaryRadius = 50f; // Radius of the spherical boundary
    [SerializeField] private float wallHeight = 10f; // For visualization purposes
    [SerializeField] private bool createPhysicalBoundaries = true;
    
    [Header("Visual Settings")]
    [SerializeField] private bool showVisualBoundaries = true;
    [SerializeField] private Color boundaryColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private int sphereSegments = 32; // Segments for the sphere visualization
    
    private GameObject boundaryVisualization;
    
    private void Awake()
    {
        if (createPhysicalBoundaries)
        {
            CreateSphericalBoundary();
        }
    }
    
    private void CreateSphericalBoundary()
    {
        // Create a parent for the boundary
        Transform boundaryParent = new GameObject("Spherical_Boundary").transform;
        boundaryParent.position = transform.position;
        
        // Create a spherical collider
        GameObject sphereCollider = new GameObject("Boundary_Collider");
        sphereCollider.transform.SetParent(boundaryParent);
        sphereCollider.transform.position = transform.position;
        
        // Add a sphere collider
        SphereCollider collider = sphereCollider.AddComponent<SphereCollider>();
        collider.radius = boundaryRadius;
        collider.isTrigger = false; // Make it a physical boundary
        
        // Make the collider face inward (entities inside can't get out)
        // This is done by setting the material's physics properties
        PhysicMaterial physicsMaterial = new PhysicMaterial("BoundaryMaterial");
        physicsMaterial.bounciness = 0.1f;
        physicsMaterial.dynamicFriction = 0.6f;
        physicsMaterial.staticFriction = 0.6f;
        collider.material = physicsMaterial;
        
        // Add visualization if needed
        if (showVisualBoundaries)
        {
            boundaryVisualization = CreateSphereVisualization(boundaryRadius);
            boundaryVisualization.transform.SetParent(boundaryParent);
            boundaryVisualization.transform.position = transform.position;
        }
    }
    
    private GameObject CreateSphereVisualization(float radius)
    {
        GameObject sphere = new GameObject("Boundary_Visualization");
        
        // Create a mesh for the sphere
        MeshFilter meshFilter = sphere.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateSphereMesh(radius, sphereSegments);
        
        // Add a renderer with a transparent material
        MeshRenderer renderer = sphere.AddComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Transparent/Diffuse"));
        material.color = boundaryColor;
        renderer.material = material;
        
        return sphere;
    }
    
    private Mesh CreateSphereMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        
        // Calculate vertices
        int rings = segments / 2;
        int sectors = segments;
        
        float R = 1f / (float)(rings - 1);
        float S = 1f / (float)(sectors - 1);
        
        Vector3[] vertices = new Vector3[(rings) * (sectors)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[(rings - 1) * (sectors - 1) * 6];
        
        int vi = 0, ti = 0;
        
        // Generate vertices
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sectors; s++)
            {
                float y = Mathf.Sin(Mathf.PI * r * R - Mathf.PI * 0.5f);
                float x = Mathf.Cos(Mathf.PI * r * R - Mathf.PI * 0.5f) * Mathf.Cos(2 * Mathf.PI * s * S);
                float z = Mathf.Cos(Mathf.PI * r * R - Mathf.PI * 0.5f) * Mathf.Sin(2 * Mathf.PI * s * S);
                
                vertices[vi] = new Vector3(x, y, z) * radius;
                uv[vi] = new Vector2(s * S, r * R);
                vi++;
            }
        }
        
        // Generate triangles
        for (int r = 0; r < rings - 1; r++)
        {
            for (int s = 0; s < sectors - 1; s++)
            {
                triangles[ti++] = r * sectors + s;
                triangles[ti++] = r * sectors + (s + 1);
                triangles[ti++] = (r + 1) * sectors + (s + 1);
                
                triangles[ti++] = r * sectors + s;
                triangles[ti++] = (r + 1) * sectors + (s + 1);
                triangles[ti++] = (r + 1) * sectors + s;
            }
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showVisualBoundaries) return;
        
        Gizmos.color = boundaryColor;
        Gizmos.DrawWireSphere(transform.position, boundaryRadius);
    }
    
    // Utility method to check if a position is within the spherical boundary
    public bool IsWithinBounds(Vector3 position)
    {
        return Vector3.Distance(position, transform.position) < boundaryRadius;
    }
    
    // Method to get the nearest point inside the spherical boundary
    public Vector3 GetNearestPointInBounds(Vector3 position)
    {
        Vector3 direction = (position - transform.position).normalized;
        float distance = Vector3.Distance(position, transform.position);
        
        if (distance >= boundaryRadius)
        {
            // Position is outside the boundary, clamp to the boundary
            return transform.position + direction * (boundaryRadius * 0.95f); // Slightly inside
        }
        
        // Already inside the boundary
        return position;
    }

    // Check if a point is outside this boundary
    public bool IsPointOutside(Vector3 point)
    {
        return Vector3.Distance(point, transform.position) > boundaryRadius;
    }

    // Visualize the boundary in the editor
    private void OnDrawGizmos()
    {
        if (!showVisualBoundaries)
            return;
            
        Gizmos.color = boundaryColor;
        
        // Draw a wireframe sphere representing the boundary
        Gizmos.DrawWireSphere(transform.position, boundaryRadius);
        
        // Draw a semi-transparent sphere
        // Unity can't draw solid spheres in gizmos, so we approximate with a mesh in the game view
    }
    
    // Method to access the radius
    public float GetBoundaryRadius()
    {
        return boundaryRadius;
    }
} 