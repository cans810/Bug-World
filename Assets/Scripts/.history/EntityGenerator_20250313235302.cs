using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Spawn Area")]
    [SerializeField] private bool useCustomSpawnArea = false;
    [SerializeField] private Vector3 spawnAreaCenter; // Custom center point
    [SerializeField] private float spawnRadius = 30f; // How far from center to spawn (outer radius)
    [SerializeField] private bool useRingSpawning = false; // Whether to spawn in a ring
    [SerializeField] private float innerRadius = 20f; // Inner radius for ring spawning
    [SerializeField] private float minDistanceFromPlayer = 15f; // Minimum distance from player
    [SerializeField] private float minDistanceFromOtherEntities = 5f; // Minimum distance between entities
    [SerializeField] private float spawnHeight = 0.5f; // Height above ground to spawn
    
    [Header("Entity Prefabs")]
    [SerializeField] private EntityType[] entityTypes;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.2f);
    
    private Transform playerTransform;
    private float nextSpawnTime;
    
    private void Start()
    {
        // Try to find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("EntityGenerator: Player not found! Make sure player has the 'Player' tag.");
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
                Debug.Log($"EntityGenerator: Max total entities reached ({totalEntities}/{maxTotalEntities})");
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
                Debug.Log("EntityGenerator: No valid entity types to spawn");
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
            
            // Add debugging - just log the chitin amount without changing it
            LivingEntity livingEntity = newEntity.GetComponent<LivingEntity>();
            if (livingEntity != null)
            {
                Debug.Log($"Entity {selectedType.entityName} spawned with chitin amount: {livingEntity.chitinAmount}");
            }
            
            if (showDebugInfo)
                Debug.Log($"EntityGenerator: Spawned {selectedType.entityName} at {spawnPosition}");
        }
        else
        {
            if (showDebugInfo)
                Debug.Log("EntityGenerator: Failed to find valid spawn position");
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
            
            // Get random point - either in full circle or in ring
            Vector3 randomPoint;
            
            if (useRingSpawning)
            {
                // Generate point in a ring (between innerRadius and spawnRadius)
                float randomRadius = Mathf.Sqrt(
                    Random.Range(innerRadius * innerRadius, spawnRadius * spawnRadius)
                );
                float randomAngle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 randomCircle = new Vector2(
                    Mathf.Cos(randomAngle) * randomRadius, 
                    Mathf.Sin(randomAngle) * randomRadius
                );
                randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            }
            else
            {
                // Original circle spawning
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
                // Set position slightly above ground
                position = hit.point + Vector3.up * spawnHeight;
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
        
        if (useRingSpawning)
        {
            // Draw outer circle
            Gizmos.color = gizmoColor;
            DrawWireCircle(center, spawnRadius, 32);
            
            // Draw inner circle
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.5f);
            DrawWireCircle(center, innerRadius, 32);
            
            // Draw filled ring
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.2f);
            for (int i = 0; i < 32; i++)
            {
                float angle1 = i / 32f * Mathf.PI * 2f;
                float angle2 = (i + 1) / 32f * Mathf.PI * 2f;
                
                Vector3 innerPoint1 = center + new Vector3(Mathf.Cos(angle1) * innerRadius, 0, Mathf.Sin(angle1) * innerRadius);
                Vector3 innerPoint2 = center + new Vector3(Mathf.Cos(angle2) * innerRadius, 0, Mathf.Sin(angle2) * innerRadius);
                Vector3 outerPoint1 = center + new Vector3(Mathf.Cos(angle1) * spawnRadius, 0, Mathf.Sin(angle1) * spawnRadius);
                Vector3 outerPoint2 = center + new Vector3(Mathf.Cos(angle2) * spawnRadius, 0, Mathf.Sin(angle2) * spawnRadius);
                
                // Draw the triangles that make up the ring segment
                Gizmos.DrawLine(innerPoint1, innerPoint2);
                Gizmos.DrawLine(outerPoint1, outerPoint2);
                Gizmos.DrawLine(innerPoint1, outerPoint1);
                Gizmos.DrawLine(innerPoint2, outerPoint2);
            }
        }
        else
        {
            // Draw the original circle visualization
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(center, spawnRadius);
        }
        
        // Draw player exclusion zone if player exists
        if (playerTransform != null)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
            Gizmos.DrawSphere(playerTransform.position, minDistanceFromPlayer);
        }
    }
    
    // Helper method to draw a wire circle in the editor
    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float angle = 0f;
        float angleStep = 2f * Mathf.PI / segments;
        Vector3 prevPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        
        for (int i = 0; i < segments; i++)
        {
            angle += angleStep;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
} 