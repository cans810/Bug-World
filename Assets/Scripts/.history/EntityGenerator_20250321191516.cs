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
    [SerializeField] private float spawnRadius = 30f; // How far from center to spawn (outer radius)
    
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
        else
        {
        }
        
        // Try to find the nest if needed
        if (findNestAutomatically || playerNestObject == null)
        {
            GameObject nestObj = GameObject.Find("PlayerNest1");
            if (nestObj != null)
            {
                playerNestObject = nestObj;
                nestTransform = nestObj.transform;
                
            }
            else
            {
            }
        }
        else if (playerNestObject != null)
        {
            nestTransform = playerNestObject.transform;
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
            
            // Get random point - either in full circle or in ring
            Vector3 randomPoint;
            
            // Original circle spawning
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            
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
        
        // Draw the original circle visualization
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(center, spawnRadius);
        
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
            // Try to find nest in editor for visualization
            GameObject nestObj = GameObject.Find("PlayerNest1");
            if (nestObj != null)
            {
                Gizmos.color = nestExclusionColor;
                Gizmos.DrawSphere(nestObj.transform.position, minDistanceFromNest);
            }
        }
    }
} 