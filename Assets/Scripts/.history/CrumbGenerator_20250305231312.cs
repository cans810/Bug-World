using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrumbGenerator : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private bool enableSpawning = true;
    [SerializeField] private int desiredCrumbCount = 10; // Always maintain this many crumbs
    [SerializeField] private float checkInterval = 1f; // How often to check for missing crumbs
    [SerializeField] private LayerMask spawnableLayers; // Layers where crumbs can spawn
    [SerializeField] private GameObject crumbPrefab; // The crumb prefab to spawn
    
    [Header("Spawn Area")]
    [SerializeField] private bool useCustomSpawnArea = false;
    [SerializeField] private Vector3 spawnAreaCenter; // Custom center point
    [SerializeField] private float spawnRadius = 40f; // How far from center to spawn
    [SerializeField] private float minDistanceFromPlayer = 5f; // Minimum distance from player
    [SerializeField] private float minDistanceBetweenCrumbs = 2f; // Minimum distance between crumbs
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.7f, 0f, 0.2f);
    
    private Transform playerTransform;
    private float nextCheckTime;
    private List<GameObject> activeCrumbs = new List<GameObject>();
    
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
            Debug.LogWarning("CrumbGenerator: Player not found! Make sure player has the 'Player' tag.");
        }
        
        // Schedule first check
        nextCheckTime = Time.time + checkInterval;
        
        // Initial spawn to fill the scene with the desired number of crumbs
        SpawnInitialCrumbs();
    }
    
    private void SpawnInitialCrumbs()
    {
        for (int i = 0; i < desiredCrumbCount; i++)
        {
            TrySpawnCrumb();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"CrumbGenerator: Initial spawn complete. {activeCrumbs.Count} crumbs active.");
        }
    }
    
    private void Update()
    {
        if (!enableSpawning) return;
        
        // Check for missing crumbs at regular intervals
        if (Time.time >= nextCheckTime)
        {
            ReplenishCrumbs();
            nextCheckTime = Time.time + checkInterval;
        }
    }
    
    private void ReplenishCrumbs()
    {
        // Clean up null references first
        activeCrumbs.RemoveAll(crumb => crumb == null);
        
        // Calculate how many crumbs we need to spawn
        int crumbsNeeded = desiredCrumbCount - activeCrumbs.Count;
        
        if (crumbsNeeded > 0 && showDebugInfo)
        {
            Debug.Log($"CrumbGenerator: Spawning {crumbsNeeded} new crumbs to maintain {desiredCrumbCount}");
        }
        
        // Spawn the needed crumbs
        for (int i = 0; i < crumbsNeeded; i++)
        {
            TrySpawnCrumb();
        }
    }
    
    private void TrySpawnCrumb()
    {
        // Try to find a valid spawn position
        Vector3 spawnPosition;
        if (TryGetSpawnPosition(out spawnPosition))
        {
            // Spawn the crumb
            GameObject newCrumb = Instantiate(crumbPrefab, spawnPosition, Quaternion.Euler(0, Random.Range(0, 360), 0));
            
            // Verify the component exists
            if (newCrumb.GetComponent<CrumbCollectible>() == null)
            {
                Debug.LogWarning("CrumbCollectible component missing from spawned crumb! Adding it now.");
                newCrumb.AddComponent<CrumbCollectible>();
            }
            
            // Make sure the name contains "crumb" for easy identification
            if (!newCrumb.name.ToLower().Contains("crumb"))
            {
                newCrumb.name = "Crumb_" + newCrumb.name;
            }
            
            activeCrumbs.Add(newCrumb);
            
            if (showDebugInfo)
                Debug.Log($"CrumbGenerator: Spawned crumb at {spawnPosition}");
        }
        else
        {
            if (showDebugInfo)
                Debug.Log("CrumbGenerator: Failed to find valid spawn position");
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
            
            // Check if too close to other crumbs
            bool tooCloseToOthers = false;
            foreach (GameObject crumb in activeCrumbs)
            {
                if (crumb == null) continue;
                
                float distanceToCrumb = Vector3.Distance(
                    new Vector3(randomPoint.x, 0, randomPoint.z),
                    new Vector3(crumb.transform.position.x, 0, crumb.transform.position.z)
                );
                
                if (distanceToCrumb < minDistanceBetweenCrumbs)
                {
                    tooCloseToOthers = true;
                    break;
                }
            }
            
            if (tooCloseToOthers)
                continue;
            
            // Raycast to find ground
            RaycastHit hit;
            if (Physics.Raycast(randomPoint + Vector3.up * 50f, Vector3.down, out hit, 100f, spawnableLayers))
            {
                // Set exact Y position to 0.02800143, while keeping X and Z from hit point
                position = new Vector3(hit.point.x, 0.02800143f, hit.point.z);
                return true;
            }
        }
        
        return false;
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