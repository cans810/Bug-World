using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrumbGenerator : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private bool enableSpawning = true;
    [SerializeField] private float spawnInterval = 5f; // Time between spawn attempts
    [SerializeField] private int maxTotalEntities = 20; // Maximum entities across all types
    [SerializeField] private LayerMask spawnableLayers; // Layers where entities can spawn
    
    [Header("Spawn Area")]
    [SerializeField] private bool useCustomSpawnArea = false;
    [SerializeField] private Vector3 spawnAreaCenter; // Custom center point
    [SerializeField] private float spawnRadius = 40f; // How far from center to spawn
    [SerializeField] private float minDistanceFromPlayer = 5f; // Minimum distance from player
    [SerializeField] private float minDistanceFromOtherEntities = 5f; // Minimum distance between entities
    [SerializeField] private float spawnHeight = 0.5f; // Height above ground to spawn
    
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
            
            // Get random point in circle
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            
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
        if (!useCustomSpawnArea)
        {
            // Draw spawn area centered on this object
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, spawnRadius);
            
            // Draw player exclusion zone if player exists
            if (playerTransform != null)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
                Gizmos.DrawSphere(playerTransform.position, minDistanceFromPlayer);
            }
        }
        else
        {
            // Draw custom spawn area
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(spawnAreaCenter, spawnRadius);
        }
    }
} 