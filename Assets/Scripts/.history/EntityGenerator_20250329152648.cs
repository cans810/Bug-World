using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class EntityGenerator : MonoBehaviour
{
    [System.Serializable]
    public class EntityType
    {
        public GameObject entityPrefab;
        public string entityName;
        [Range(0f, 100f)] public float spawnWeight = 25f; // Relative chance of spawning this entity
        [Min(0)] public int maxInstances = 5; // Maximum number of this type
        public bool randomizeDrops = false;
        
        // Runtime tracking
        [HideInInspector] public List<GameObject> activeInstances = new List<GameObject>();
        
        public bool CanSpawn => activeInstances.Count < maxInstances;
        
        public void CleanupDeadInstances()
        {
            activeInstances.RemoveAll(instance => instance == null);
        }
    }
    
    [Header("Spawn Settings")]
    [SerializeField] private bool enableSpawning = true;
    [SerializeField] private float spawnInterval = 5f; // Time between spawn attempts
    [SerializeField] private int maxTotalEntities = 20; // Maximum entities across all types
    [SerializeField] private LayerMask spawnableLayers; // Layers where entities can spawn
    
    [Header("Spawn Area Shape")]
    [SerializeField] private bool useBoxShape = false; // Toggle between sphere and box shape
    [SerializeField] private float spawnRadius = 30f; // How far from center to spawn (for sphere)
    [SerializeField] private Vector3 boxDimensions = new Vector3(60f, 1f, 60f); // Dimensions for box shape
    
    [Header("Spawn Area")]
    [SerializeField] private bool useCustomSpawnArea = false;
    [SerializeField] private Vector3 spawnAreaCenter; // Custom center point
    [SerializeField] private float minDistanceFromPlayer = 15f; // Minimum distance from player
    [SerializeField] private float minDistanceFromNest = 20f; // Minimum distance from PlayerNest1
    [SerializeField] private float minDistanceFromOtherEntities = 5f; // Minimum distance between entities
    [SerializeField] private float spawnHeight = 0.2f; // Reduced from 0.5f
    
    [Header("Protected Objects")]
    [SerializeField] private bool findNestAutomatically = true; // Auto-find the PlayerNest1 object
    [SerializeField] private GameObject playerNestObject; // Reference to PlayerNest1
    
    [Header("Entity Prefabs")]
    [SerializeField] private EntityType[] entityTypes;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.2f);
    [SerializeField] private Color nestExclusionColor = new Color(0f, 1f, 0.5f, 0.2f); // Color for nest exclusion zone
    
    private Transform playerTransform;
    private Transform nestTransform;
    private float nextSpawnTime;
    private List<GameObject> allSpawnedEntities = new List<GameObject>();
    
    private void Start()
    {
        // Try to find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        
        // Start spawning
        nextSpawnTime = Time.time + spawnInterval;
    }
    
    private void Update()
    {
        if (!enableSpawning) return;
        
        // Time to spawn?
        if (Time.time >= nextSpawnTime)
        {
            TrySpawnEntity();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    private void TrySpawnEntity()
    {
        // Check if we're at the total entity limit
        int totalEntities = CountTotalEntities();
        if (totalEntities >= maxTotalEntities)
        {
            if (showDebugInfo)
            return;
        }
        
        // Get valid entity types that aren't at their maximum limit
        List<EntityType> validTypes = new List<EntityType>();
        float totalWeight = 0f;
        
        foreach (EntityType entityType in entityTypes)
        {
            if (entityType.CanSpawn && entityType.entityPrefab != null)
            {
                validTypes.Add(entityType);
                totalWeight += entityType.spawnWeight;
            }
        }
        
        if (validTypes.Count == 0)
        {
            if (showDebugInfo)
            return;
        }
        
        // Choose a random entity type based on weights
        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;
        EntityType selectedType = validTypes[0];
        
        foreach (EntityType entityType in validTypes)
        {
            cumulativeWeight += entityType.spawnWeight;
            if (randomValue <= cumulativeWeight)
            {
                selectedType = entityType;
                break;
            }
        }
        
        // Try to find a valid spawn position
        Vector3 spawnPosition;
        if (TryGetSpawnPosition(out spawnPosition))
        {
            // Spawn the entity
            GameObject newEntity = Instantiate(selectedType.entityPrefab, spawnPosition, Quaternion.Euler(0, Random.Range(0, 360), 0));
            selectedType.activeInstances.Add(newEntity);
            allSpawnedEntities.Add(newEntity);
            
            // Get the border object name from the first child of the EntityGenerator
            string borderName = "";
            if (transform.childCount > 0)
            {
                borderName = transform.GetChild(0).name;
            }
            
            // Set the border object name in the EnemyAI component
            EnemyAI enemyAI = newEntity.GetComponent<EnemyAI>();
            if (enemyAI != null && !string.IsNullOrEmpty(borderName))
            {
                enemyAI.borderObjectName = borderName;
                
                if (showDebugInfo)
                {
                    Debug.Log($"Set border object '{borderName}' for entity {newEntity.name}");
                }
            }
            
            // Add debugging - just log the chitin amount without changing it
            LivingEntity livingEntity = newEntity.GetComponent<LivingEntity>();
            if (livingEntity != null)
            {
            }
        }
    }
    
    private bool TryGetSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;
        int maxAttempts = 30;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            // Get spawn center (either this object or custom)
            Vector3 center = useCustomSpawnArea ? spawnAreaCenter : transform.position;
            
            // Get random point - either in sphere or box
            Vector3 randomPoint;
            
            if (useBoxShape)
            {
                // Box shape spawning - account for rotation
                // Generate random point in local space
                Vector3 localRandomPoint = new Vector3(
                    Random.Range(-boxDimensions.x * 0.5f, boxDimensions.x * 0.5f),
                    0,
                    Random.Range(-boxDimensions.z * 0.5f, boxDimensions.z * 0.5f)
                );
                
                // Transform to world space using the generator's rotation
                if (useCustomSpawnArea) {
                    // For custom area, we just offset from center without rotation
                    randomPoint = center + localRandomPoint;
                } else {
                    // Apply the rotation of the generator for proper orientation
                    randomPoint = center + transform.rotation * localRandomPoint;
                }
            }
            else
            {
                // Original sphere spawning
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            }
            
            // Check if too close to player
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(
                    new Vector3(randomPoint.x, 0, randomPoint.z),
                    new Vector3(playerTransform.position.x, 0, playerTransform.position.z)
                );
                
                if (distanceToPlayer < minDistanceFromPlayer)
                    continue;
            }
            
            // Check if too close to the player's nest
            if (nestTransform != null)
            {
                float distanceToNest = Vector3.Distance(
                    new Vector3(randomPoint.x, 0, randomPoint.z),
                    new Vector3(nestTransform.position.x, 0, nestTransform.position.z)
                );
                
                if (distanceToNest < minDistanceFromNest)
                    continue;
            }
            
            // Check if too close to other entities
            bool tooCloseToOthers = false;
            foreach (EntityType entityType in entityTypes)
            {
                foreach (GameObject instance in entityType.activeInstances)
                {
                    if (instance == null) continue;
                    
                    float distanceToEntity = Vector3.Distance(
                        new Vector3(randomPoint.x, 0, randomPoint.z),
                        new Vector3(instance.transform.position.x, 0, instance.transform.position.z)
                    );
                    
                    if (distanceToEntity < minDistanceFromOtherEntities)
                    {
                        tooCloseToOthers = true;
                        break;
                    }
                }
                
                if (tooCloseToOthers) break;
            }
            
            if (tooCloseToOthers)
                continue;
            
            // Raycast to find ground
            RaycastHit hit;
            if (Physics.Raycast(randomPoint + Vector3.up * 50f, Vector3.down, out hit, 100f, spawnableLayers))
            {
                // Get the potential spawn position
                position = hit.point + Vector3.up * spawnHeight;
                
                // For box shape, verify the final position is still within bounds after raycast
                if (useBoxShape)
                {
                    // Transform the position back to local space
                    Vector3 relativePos = position - center;
                    Vector3 localPos;
                    
                    if (useCustomSpawnArea) {
                        // For custom area without rotation
                        localPos = relativePos;
                    } else {
                        // Apply inverse rotation
                        localPos = Quaternion.Inverse(transform.rotation) * relativePos;
                    }
                    
                    // Check if still within box bounds (allowing for height difference)
                    if (Mathf.Abs(localPos.x) > boxDimensions.x * 0.5f ||
                        Mathf.Abs(localPos.z) > boxDimensions.z * 0.5f)
                    {
                        // Outside the box bounds after raycast, try again
                        continue;
                    }
                }
                
                return true;
            }
        }
        
        return false;
    }
    
    private int CountTotalEntities()
    {
        int count = 0;
        foreach (EntityType entityType in entityTypes)
        {
            entityType.CleanupDeadInstances();
            count += entityType.activeInstances.Count;
        }
        return count;
    }
    
    private void OnDrawGizmosSelected()
    {
        Vector3 center = useCustomSpawnArea ? spawnAreaCenter : transform.position;
        
        if (useBoxShape)
        {
            // Draw the box area visualization with improved visibility
            Gizmos.color = gizmoColor;
            
            // Store original matrix
            Matrix4x4 originalMatrix = Gizmos.matrix;
            
            // Apply rotation and position of the entity generator
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;
            
            // Draw solid box with semi-transparency
            Gizmos.DrawCube(Vector3.zero, boxDimensions);
            
            // Draw wireframe with more opaque color
            Color wireColor = gizmoColor;
            wireColor.a = 0.8f;
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(Vector3.zero, boxDimensions);
            
            // Reset matrix
            Gizmos.matrix = originalMatrix;
            
            // Draw ground-level boundaries for clarity
            DrawBoxGroundBoundary(center, boxDimensions);
        }
        else
        {
            // Draw the original sphere visualization
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(center, spawnRadius);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.8f);
            Gizmos.DrawWireSphere(center, spawnRadius);
        }
        
        // Draw player exclusion zone if player exists
        if (playerTransform != null)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
            Gizmos.DrawSphere(playerTransform.position, minDistanceFromPlayer);
        }
        
        // Draw nest exclusion zone if nest exists
        if (nestTransform != null || playerNestObject != null)
        {
            Vector3 nestPosition;
            
            if (nestTransform != null)
                nestPosition = nestTransform.position;
            else
                nestPosition = playerNestObject.transform.position;
                
            Gizmos.color = nestExclusionColor;
            Gizmos.DrawSphere(nestPosition, minDistanceFromNest);
        }
        else if (Application.isEditor)
        {
            Gizmos.color = nestExclusionColor;
            Gizmos.DrawSphere(nestTransform.transform.position, minDistanceFromNest);
        }
    }
    
    // Helper method to draw clear ground-level boundaries for the box
    private void DrawBoxGroundBoundary(Vector3 center, Vector3 dimensions)
    {
        // Draw ground-level boundaries (slightly above ground to prevent z-fighting)
        float groundY = 0.05f;
        Vector3 groundCenter = new Vector3(center.x, groundY, center.z);
        
        // Half extents
        float halfWidth = dimensions.x * 0.5f;
        float halfLength = dimensions.z * 0.5f;
        
        // Get rotated corners
        Vector3 forward = transform.forward * halfLength;
        Vector3 right = transform.right * halfWidth;
        
        // Calculate corners
        Vector3 corner1 = groundCenter - right - forward;
        Vector3 corner2 = groundCenter + right - forward;
        Vector3 corner3 = groundCenter + right + forward;
        Vector3 corner4 = groundCenter - right + forward;
        
        // Use brighter color for ground outline
        Color brightOutline = new Color(1f, 0.7f, 0.2f, 1f);
        Gizmos.color = brightOutline;
        
        // Draw the ground rectangle
        Gizmos.DrawLine(corner1, corner2);
        Gizmos.DrawLine(corner2, corner3);
        Gizmos.DrawLine(corner3, corner4);
        Gizmos.DrawLine(corner4, corner1);
    }

    public void EnableSpawning(bool enable)
    {
        // Add debugging info with fully qualified names
        UnityEngine.Debug.Log($"<color=yellow>EntityGenerator.EnableSpawning called for {gameObject.name}: {enable}</color>");
        UnityEngine.Debug.Log($"<color=yellow>Call stack: {System.Environment.StackTrace}</color>");
        
        // Set the spawning state
        this.enableSpawning = enable;
        
        // Log the change
        UnityEngine.Debug.Log($"Entity spawning for {gameObject.name} set to {enable}");
    }
} 