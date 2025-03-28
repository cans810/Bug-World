using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Creates a visible representation of a Collider as a semi-transparent visualization.
/// Now supports SphereCollider, BoxCollider, and CapsuleCollider types.
/// </summary>
public class BorderVisualizer : MonoBehaviour
{
    [Header("Border Settings")]
    [SerializeField] private bool showDebugVisuals = true;
    [SerializeField] private Color borderColor = Color.red;
    [SerializeField] private Color safeAreaColor = Color.green;
    [SerializeField] private int rayCount = 36; // Rays around the circle (every 10 degrees)
    [SerializeField] private float rayLength = 20f; // How far the rays extend
    [SerializeField] private float safetyMargin = 1f; // How far inside the border is considered "safe"
    [SerializeField] private bool showRays = false; // Added control to toggle rays visibility
    
    [Header("Visualization Settings")]
    [SerializeField] private Color sphereColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Semi-transparent gray
    [SerializeField] private bool showInGame = true;
    [SerializeField] private Material visualizerMaterial;
    [SerializeField] private bool showRadius = true; // Whether to show the radius instead of the sphere
    [SerializeField] private Color radiusColor = Color.yellow; // Color of the radius line/disc
    [SerializeField] private float radiusWidth = 0.1f; // Width of the radius line
    [SerializeField] private bool fillCircle = true; // Whether to fill the circle or just show the radius line
    
    [Header("Level-Based Visibility")]
    [SerializeField] private int requiredPlayerLevel = 0; // Level required to enter this area
    [SerializeField] private float fadeOutDuration = 1.5f; // How long the fade out animation takes
    private bool isFading = false;
    private bool hasBeenDisabled = false;
    
    // For object pooling
    private static List<BorderVisualizer> allBorders = new List<BorderVisualizer>();
    
    // Cached components
    public SphereCollider sphereCollider;
    public CapsuleCollider capsuleCollider; // Add reference for capsule collider
    public Collider[] otherColliders;
    
    private GameObject visualSphere;
    private Transform visualTransform;
    private Material instanceMaterial;
    private LineRenderer radiusLine; // Line renderer for showing the radius
    
    private void Awake()
    {
        // Cache collider references
        sphereCollider = GetComponent<SphereCollider>();
        capsuleCollider = GetComponent<CapsuleCollider>(); // Get capsule collider reference
        otherColliders = GetComponents<Collider>();
        
        // Add to static list for easy access
        allBorders.Add(this);
    }
    
    private void Start()
    {
        if (showInGame)
        {
            CreateVisualSphere();
        }
    }
    
    private void OnDestroy()
    {
        // Remove from list
        allBorders.Remove(this);
        
        // Clean up the generated material when this component is destroyed
        if (instanceMaterial != null)
        {
            Destroy(instanceMaterial);
        }
        
        // Also destroy the visual sphere if it exists
        if (visualSphere != null)
        {
            Destroy(visualSphere);
        }
    }

    
    // Get the closest safe position to the target position
    public static Vector3 GetClosestSafePosition(Vector3 targetPosition, Vector3 originalPosition)
    {
        // If the target position is already safe, return it
        if (IsPositionSafe(targetPosition))
        {
            return targetPosition;
        }
        
        // Otherwise, find a safe position along the line from original to target
        return FindClosestSafePoint(originalPosition, targetPosition);
    }
    
    // Static helper to check if a waypoint would be valid
    public static bool IsWaypointValid(Vector3 currentPosition, Vector3 targetWaypoint)
    {
        // First check if the target is beyond the border
        if (!IsPositionSafe(targetWaypoint))
        {
            return false; // Waypoint is outside border
        }
        
        // Then check if the path between current and target crosses the border
        float dist = Vector3.Distance(currentPosition, targetWaypoint);
        Vector3 dir = (targetWaypoint - currentPosition).normalized;
        
        // Check points along the path
        for (float t = 0; t < dist; t += 0.5f)
        {
            Vector3 pointToCheck = currentPosition + dir * t;
            if (!IsPositionSafe(pointToCheck))
            {
                return false; // Path crosses outside the border
            }
        }
        
        return true; // Waypoint is valid and path stays inside
    }
    
    // Find the closest safe point along a line
    private static Vector3 FindClosestSafePoint(Vector3 start, Vector3 end)
    {
        // If start isn't safe, return start (shouldn't happen)
        if (!IsPositionSafe(start))
        {
            Debug.LogWarning("Start position is not safe!");
            return start;
        }
        
        // Binary search to find closest safe point
        float distance = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;
        
        float minDist = 0;
        float maxDist = distance;
        float currentDist = distance * 0.5f;
        
        for (int i = 0; i < 10; i++) // 10 iterations should be enough
        {
            Vector3 testPoint = start + direction * currentDist;
            
            if (IsPositionSafe(testPoint))
            {
                // We can go farther
                minDist = currentDist;
            }
            else
            {
                // We went too far
                maxDist = currentDist;
            }
            
            // Update distance
            currentDist = (minDist + maxDist) * 0.5f;
        }
        
        // Return the safe point
        return start + direction * minDist;
    }
    
    // Instance method to check if a point is inside this specific border
    private bool IsPointInsideBorder(Vector3 point)
    {
        if (sphereCollider != null)
        {
            // For sphere collider
            Vector3 center = transform.TransformPoint(sphereCollider.center);
            float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, 
                                                          Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
            
            // Distance from point to center
            float distance = Vector3.Distance(point, center);
            
            // Point is safe if it's inside the sphere minus safety margin
            return distance < (radius - safetyMargin);
        }
        else if (capsuleCollider != null)
        {
            // For capsule collider
            Vector3 center = transform.TransformPoint(capsuleCollider.center);
            float radius = capsuleCollider.radius * Mathf.Max(transform.lossyScale.x, 
                                                         Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
            
            // Get the axis direction
            Vector3 direction = Vector3.up;
            float height = capsuleCollider.height;
            
            if (capsuleCollider.direction == 0) // X axis
            {
                direction = transform.right;
                height *= transform.lossyScale.x;
            }
            else if (capsuleCollider.direction == 1) // Y axis
            {
                direction = transform.up;
                height *= transform.lossyScale.y;
            }
            else if (capsuleCollider.direction == 2) // Z axis
            {
                direction = transform.forward;
                height *= transform.lossyScale.z;
            }
            
            // Calculate the segment endpoints
            float halfHeight = (height - 2 * radius) * 0.5f;
            Vector3 point1 = center + direction * halfHeight;
            Vector3 point2 = center - direction * halfHeight;
            
            // Find the closest point on the segment
            Vector3 closestPointOnSegment = ClosestPointOnSegment(point, point1, point2);
            
            // Find the distance from the closest point on the segment to the test point
            float distance = Vector3.Distance(point, closestPointOnSegment);
            
            // Point is safe if it's inside the capsule minus safety margin
            return distance < (radius - safetyMargin);
        }
        else if (otherColliders != null && otherColliders.Length > 0)
        {
            // For other collider types - use closest point
            foreach (Collider col in otherColliders)
            {
                if (col != null)
                {
                    Vector3 closestPoint = col.ClosestPoint(point);
                    float distance = Vector3.Distance(point, closestPoint);
                    if (distance < safetyMargin)
                    {
                        return false; // Too close to border
                    }
                }
            }
            return true;
        }
        
        // Default to safe if no collider (shouldn't happen)
        return true;
    }

    // Helper method to find the closest point on a line segment
    private Vector3 ClosestPointOnSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;
        lineDirection.Normalize();
        
        Vector3 pointToLineStart = point - lineStart;
        float dotProduct = Vector3.Dot(pointToLineStart, lineDirection);
        
        if (dotProduct <= 0)
            return lineStart;
        if (dotProduct >= lineLength)
            return lineEnd;
        
        return lineStart + lineDirection * dotProduct;
    }

    private void CreateVisualSphere()
    {
        // For sphere colliders
        if (sphereCollider != null)
        {
            // Create a GameObject for the visualization
            GameObject radiusObj = new GameObject("BorderRadius_" + gameObject.name);
            radiusObj.transform.SetParent(transform, false);
            radiusObj.transform.localPosition = sphereCollider.center;
            
            // Create ground circle visualization
            if (showRadius)
            {
                if (fillCircle)
                {
                    // Create a circle mesh for ground painting
                    GameObject circle = new GameObject("FilledCircle");
                    circle.transform.SetParent(radiusObj.transform, false);
                    
                    // Add mesh components
                    MeshFilter meshFilter = circle.AddComponent<MeshFilter>();
                    MeshRenderer meshRenderer = circle.AddComponent<MeshRenderer>();
                    
                    // Create circular mesh for X-Z plane
                    float radius = sphereCollider.radius;
                    meshFilter.mesh = CreateCircleMesh(radius, 36); // 36 segments for a smooth circle
                    
                    // Position the circle slightly above ground to prevent z-fighting
                    circle.transform.localPosition = new Vector3(0, 0.02f, 0);
                    
                    // Create a material for the circle
                    Material circleMaterial = new Material(Shader.Find("Sprites/Default"));
                    
                    // Use different colors based on whether the area is enterable
                    if (requiredPlayerLevel > 0)
                    {
                        // Area has level restriction - use warning color
                        circleMaterial.color = new Color(radiusColor.r, radiusColor.g, radiusColor.b, 0.6f);
                    }
                    else
                    {
                        // Standard area - use normal color
                        circleMaterial.color = new Color(radiusColor.r, radiusColor.g, radiusColor.b, 0.3f);
                    }
                    
                    // Set rendering properties
                    circleMaterial.renderQueue = 3000;
                    circleMaterial.SetInt("_ZWrite", 0);
                    circleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    circleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    
                    // Apply the material
                    meshRenderer.material = circleMaterial;
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    meshRenderer.receiveShadows = false;
                }
                else
                {
                    // Use line renderer for radius visualization
                    radiusLine = radiusObj.AddComponent<LineRenderer>();
                    radiusLine.useWorldSpace = false;
                    radiusLine.startWidth = radiusWidth;
                    radiusLine.endWidth = radiusWidth;
                    radiusLine.material = new Material(Shader.Find("Sprites/Default"));
                    radiusLine.startColor = radiusColor;
                    radiusLine.endColor = radiusColor;
                    radiusLine.positionCount = 2;
                    
                    // Set the line positions - from center to edge of sphere
                    radiusLine.SetPosition(0, Vector3.zero); // Center of the sphere in local space
                    radiusLine.SetPosition(1, Vector3.forward * sphereCollider.radius); // Radius along the forward direction
                }
            }
            
            // Create the 3D sphere visualization
            visualSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualSphere.name = "BorderVisual_" + gameObject.name;
            
            // Remove the collider to prevent physics interactions
            Collider visualCollider = visualSphere.GetComponent<Collider>();
            if (visualCollider != null) Destroy(visualCollider);
            
            visualTransform = visualSphere.transform;
            visualTransform.SetParent(transform, false);
            visualTransform.localPosition = sphereCollider.center;
            
            float diameter = sphereCollider.radius * 2;
            visualTransform.localScale = new Vector3(diameter, diameter, diameter);
            
            // Create and apply material
            CreateAndApplyMaterial(visualSphere);
        }
        else if (capsuleCollider != null)
        {
            // Create a GameObject for the radius visualization
            GameObject radiusObj = new GameObject("BorderRadius_" + gameObject.name);
            radiusObj.transform.SetParent(transform, false);
            radiusObj.transform.localPosition = capsuleCollider.center;
            
            if (fillCircle)
            {
                // Create a stadium shape (capsule projection on the ground)
                GameObject stadium = new GameObject("FilledStadium");
                stadium.transform.SetParent(radiusObj.transform, false);
                
                // Add mesh components
                MeshFilter meshFilter = stadium.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = stadium.AddComponent<MeshRenderer>();
                
                // Create stadium-shaped mesh based on capsule dimensions
                float radius = capsuleCollider.radius;
                float height = capsuleCollider.height;
                
                // Determine the orientation and create the appropriate stadium shape
                if (capsuleCollider.direction == 1) // Y-axis (vertical)
                {
                    // For vertical capsules, just show circular footprint
                    meshFilter.mesh = CreateCircleMesh(radius, 36);
                }
                else 
                {
                    // For horizontal capsules (X or Z axis), create a stadium shape
                    meshFilter.mesh = CreateStadiumMesh(radius, height, 
                        capsuleCollider.direction == 0); // true if X-axis
                }
                
                // Keep it flat on the ground
                stadium.transform.localRotation = Quaternion.identity;
                
                // Create material with transparency settings
                Material stadiumMaterial = new Material(Shader.Find("Sprites/Default"));
                stadiumMaterial.color = radiusColor;
                
                // Prevent overlapping areas from becoming darker
                stadiumMaterial.renderQueue = 3000;
                stadiumMaterial.SetInt("_ZWrite", 0);
                stadiumMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                stadiumMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                
                // Apply the material
                meshRenderer.material = stadiumMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }
            else
            {
                // Line renderer approach for showing capsule radius
                radiusLine = radiusObj.AddComponent<LineRenderer>();
                radiusLine.useWorldSpace = false;
                radiusLine.startWidth = radiusWidth;
                radiusLine.endWidth = radiusWidth;
                radiusLine.material = new Material(Shader.Find("Sprites/Default"));
                radiusLine.startColor = radiusColor;
                radiusLine.endColor = radiusColor;
                radiusLine.positionCount = 2;
                
                // Set the line positions - from center to edge of capsule
                radiusLine.SetPosition(0, Vector3.zero);
                radiusLine.SetPosition(1, Vector3.forward * capsuleCollider.radius);
            }
            
            visualSphere = radiusObj;
        }
        // Add box support if needed...
    }

    // Helper method to create and apply the material to visualizations
    private void CreateAndApplyMaterial(GameObject visualObj)
    {
        if (visualizerMaterial == null)
        {
            // Create a new material with mobile-compatible settings
            instanceMaterial = new Material(Shader.Find("Transparent/Diffuse"));
            
            // For mobile compatibility, use simpler transparency settings
            instanceMaterial.SetFloat("_Mode", 3); // Set to transparent mode
            instanceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            instanceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            instanceMaterial.SetInt("_ZWrite", 0);
            instanceMaterial.DisableKeyword("_ALPHATEST_ON");
            instanceMaterial.EnableKeyword("_ALPHABLEND_ON");
            instanceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            instanceMaterial.renderQueue = 3000;
        }
        else
        {
            // Create an instance of the provided material
            instanceMaterial = new Material(visualizerMaterial);
        }
        
        // Set the color with transparency
        instanceMaterial.color = new Color(sphereColor.r, sphereColor.g, sphereColor.b, 0.15f);
        
        // Apply the material
        Renderer renderer = visualObj.GetComponent<Renderer>();
        renderer.material = instanceMaterial;
        
        // Disable shadow casting
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // Helper method to create a circular mesh for X-Z plane (horizontal circle)
    private Mesh CreateCircleMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        
        // Create vertices
        Vector3[] vertices = new Vector3[segments + 1];
        vertices[0] = Vector3.zero; // Center vertex
        
        float angleStep = 2f * Mathf.PI / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices[i + 1] = new Vector3(x, 0, z); // Using x,z for horizontal plane (ground)
        }
        
        // Create triangles
        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0; // Center vertex
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1; // Wrap around to the first vertex for the last triangle
        }
        
        // Create UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        uvs[0] = new Vector2(0.5f, 0.5f); // Center UV
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float u = Mathf.Cos(angle) * 0.5f + 0.5f;
            float v = Mathf.Sin(angle) * 0.5f + 0.5f;
            uvs[i + 1] = new Vector2(u, v);
        }
        
        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        
        return mesh;
    }

    // Improved stadium mesh generator that avoids overlapping triangles
    private Mesh CreateStadiumMesh(float radius, float height, bool isXAxis)
    {
        Mesh mesh = new Mesh();
        
        // Calculate the length of the straight section
        float straightLength = height - 2 * radius;
        
        // We'll create a stadium with:
        // - Two semicircles (one at each end)
        // - A rectangle connecting them
        int circleSegments = 18; // Segments per semicircle
        
        // Calculate vertices needed (no center vertex needed in improved approach)
        int vertexCount = circleSegments * 2 + 4; // Two semicircles + 4 corners
        Vector3[] vertices = new Vector3[vertexCount];
        
        // Calculate center points of the two semicircles
        Vector3 semicircle1Center = isXAxis ? 
            new Vector3(straightLength/2, 0, 0) : 
            new Vector3(0, 0, straightLength/2);
        
        Vector3 semicircle2Center = isXAxis ? 
            new Vector3(-straightLength/2, 0, 0) : 
            new Vector3(0, 0, -straightLength/2);
        
        // Create the perimeter vertices in sequence (going around the shape clockwise)
        int vertIndex = 0;
        
        // First semicircle
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = Mathf.PI * (-0.5f + i / (float)(circleSegments - 1));
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            vertices[vertIndex++] = semicircle1Center + new Vector3(x, 0, z);
        }
        
        // Top rectangle edge
        if (isXAxis)
        {
            vertices[vertIndex++] = new Vector3(-straightLength/2, 0, radius);
        }
        else
        {
            vertices[vertIndex++] = new Vector3(radius, 0, -straightLength/2);
        }
        
        // Second semicircle
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = Mathf.PI * (0.5f + i / (float)(circleSegments - 1));
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            vertices[vertIndex++] = semicircle2Center + new Vector3(x, 0, z);
        }
        
        // Bottom rectangle edge
        if (isXAxis)
        {
            vertices[vertIndex++] = new Vector3(straightLength/2, 0, -radius);
        }
        else
        {
            vertices[vertIndex++] = new Vector3(-radius, 0, straightLength/2);
        }
        
        // Create triangles using triangle strips or fans from the perimeter
        List<int> trianglesList = new List<int>();
        
        // Create triangles by connecting to center point
        Vector3 centerPoint = Vector3.zero;
        
        // Add center vertex at the end
        System.Array.Resize(ref vertices, vertices.Length + 1);
        vertices[vertices.Length - 1] = centerPoint;
        int centerIndex = vertices.Length - 1;
        
        // Create triangles - proper fan pattern without overlaps
        for (int i = 0; i < vertexCount; i++)
        {
            trianglesList.Add(centerIndex);
            trianglesList.Add(i);
            trianglesList.Add((i + 1) % vertexCount);
        }
        
        // Create UVs (simple mapping)
        Vector2[] uvs = new Vector2[vertices.Length];
        
        // Set UVs for perimeter vertices
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 vertex = vertices[i];
            // Normalize to 0-1 range based on overall dimensions
            float maxDim = Mathf.Max(height, radius * 2);
            float u = vertex.x / maxDim + 0.5f;
            float v = vertex.z / maxDim + 0.5f;
            uvs[i] = new Vector2(u, v);
        }
        
        // Center UV
        uvs[centerIndex] = new Vector2(0.5f, 0.5f);
        
        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = trianglesList.ToArray();
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        
        return mesh;
    }

    private void Update()
    {
        // If the collider's properties change, update the visual
        if (visualSphere != null)
        {
            if (showRadius)
            {
                if (fillCircle)
                {
                    // Update circle size if needed
                    float radius = sphereCollider.radius;
                    foreach (Transform child in visualSphere.transform)
                    {
                        MeshFilter meshFilter = child.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.mesh != null)
                        {
                            if (Mathf.Abs(meshFilter.mesh.bounds.extents.x - radius) > 0.01f)
                            {
                                // Recreate the mesh if radius changed
                                meshFilter.mesh = CreateCircleMesh(radius, 36);
                            }
                        }
                    }
                    
                    // Update position if needed
                    if (visualSphere.transform.localPosition != sphereCollider.center)
                    {
                        visualSphere.transform.localPosition = sphereCollider.center;
                    }
                }
                else if (radiusLine != null)
                {
                    // Update the radius line if needed
                    radiusLine.SetPosition(0, sphereCollider.center);
                    radiusLine.SetPosition(1, sphereCollider.center + Vector3.forward * sphereCollider.radius);
                }
            }
            else if (!showRadius)
            {
                if (sphereCollider != null)
                {
                    // Update sphere visualization...
                    // ...existing sphere update code...
                }
                else if (capsuleCollider != null)
                {
                    // Update capsule visualization
                    // Update position if needed
                    if (visualTransform.localPosition != capsuleCollider.center)
                    {
                        visualTransform.localPosition = capsuleCollider.center;
                    }
                    
                    // Update scale if needed
                    float diameter = capsuleCollider.radius * 2;
                    float length = capsuleCollider.height;
                    Vector3 targetScale = Vector3.one;
                    
                    if (capsuleCollider.direction == 0) // X axis
                        targetScale = new Vector3(length, diameter, diameter);
                    else if (capsuleCollider.direction == 1) // Y axis
                        targetScale = new Vector3(diameter, length, diameter);
                    else // Z axis
                        targetScale = new Vector3(diameter, diameter, length);
                    
                    if (visualTransform.localScale != targetScale)
                    {
                        visualTransform.localScale = targetScale;
                    }
                }
            }
        }
    }

    // Allow toggling between visualizations at runtime
    public void ToggleVisualization(bool showRadiusOnly, bool fillTheCircle = true)
    {
        // Only proceed if there's a change
        if (showRadiusOnly != showRadius || fillTheCircle != fillCircle)
        {
            showRadius = showRadiusOnly;
            fillCircle = fillTheCircle;
            
            // Destroy current visualization
            if (visualSphere != null)
            {
                Destroy(visualSphere);
                visualSphere = null;
                radiusLine = null;
            }
            
            // Create new visualization
            if (showInGame)
            {
                CreateVisualSphere();
            }
        }
    }

    // Add this method to fade out and disable the visualization
    public void FadeOutAndDisable()
    {
        // Only fade if not already fading or disabled
        if (!isFading && !hasBeenDisabled)
        {
            Debug.Log($"Starting fade out for border: {gameObject.name}");
            StartCoroutine(FadeOutVisualization());
        }
        else
        {
            Debug.Log($"Border already fading or disabled: {gameObject.name}, isFading: {isFading}, hasBeenDisabled: {hasBeenDisabled}");
            
            // Force disable if needed
            if (!hasBeenDisabled && visualSphere != null)
            {
                visualSphere.SetActive(false);
                hasBeenDisabled = true;
                Debug.Log($"Force disabled border visualization: {gameObject.name}");
            }
        }
    }

    // Add a new method to check if the border should be visible based on player level
    public void CheckVisibilityForPlayerLevel(int playerLevel)
    {
        // If player level is high enough and border is still visible, fade it out
        if (requiredPlayerLevel > 0 && playerLevel >= requiredPlayerLevel && !hasBeenDisabled)
        {
            FadeOutAndDisable();
        }
    }

    // Method to retrieve the required level for this border
    public int GetRequiredLevel()
    {
        return requiredPlayerLevel;
    }

    // Method to set the required level for this border
    public void SetRequiredLevel(int level)
    {
        requiredPlayerLevel = level;
    }

    // Add this to handle visualization state for loading
    public void SetVisualizationState(bool enabled)
    {
        if (!enabled && visualSphere != null)
        {
            visualSphere.SetActive(false);
            hasBeenDisabled = true;
        }
    }

    // Add this method to help debug visualization problems
    public void DebugVisualization()
    {
        Debug.Log($"--- Border Visualization Debug for {gameObject.name} ---");
        Debug.Log($"Required Level: {requiredPlayerLevel}");
        Debug.Log($"Show Radius: {showRadius}, Fill Circle: {fillCircle}");
        Debug.Log($"Sphere Collider: {(sphereCollider != null ? "Present" : "Missing")}");
        Debug.Log($"Capsule Collider: {(capsuleCollider != null ? "Present" : "Missing")}");
        
        if (visualSphere != null)
        {
            Debug.Log($"Visualization Object: Active");
            
            // Check for renderers
            Renderer[] renderers = visualSphere.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"Found {renderers.Length} renderers");
            
            foreach (var renderer in renderers)
            {
                Debug.Log($"Renderer: {renderer.gameObject.name}, Material: {renderer.material.name}, Color: {renderer.material.color}");
                Debug.Log($"Is visible: {renderer.isVisible}, Enabled: {renderer.enabled}");
            }
            
            // Check for mesh filters
            MeshFilter[] meshFilters = visualSphere.GetComponentsInChildren<MeshFilter>(true);
            Debug.Log($"Found {meshFilters.Length} mesh filters");
            
            // Add a temporary visual marker at the position for testing
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "DEBUG_MARKER_" + gameObject.name;
            marker.transform.position = transform.position + Vector3.up * 2f;
            marker.transform.localScale = Vector3.one * 0.5f;
            marker.GetComponent<Renderer>().material.color = Color.magenta;
            Debug.Log($"Created debug marker at {marker.transform.position}");
            GameObject.Destroy(marker, 5f); // Clean up after 5 seconds
        }
        else
        {
            Debug.Log("Visualization Object: NULL");
        }
        
        // Force recreation of visualization
        if (visualSphere != null)
        {
            Debug.Log("Destroying and recreating visualization...");
            Destroy(visualSphere);
            visualSphere = null;
        }
        
        CreateVisualSphere();
        Debug.Log("--- End Debug ---");
    }
} 