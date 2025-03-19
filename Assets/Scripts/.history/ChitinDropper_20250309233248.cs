using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [Header("Chitin Drop Settings")]
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private float scatterRadius = 5f;
    [SerializeField] private float dropHeight = 0.1f; 
    
    [Header("Physics Forces")]
    [SerializeField] private float upwardForceMin = 0.2f; 
    [SerializeField] private float upwardForceMax = 0.5f;  
    [SerializeField] private float horizontalForce = 0.3f; 
    [SerializeField] private float explosionRadius = 0.2f; 
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Reference to the LivingEntity
    private LivingEntity livingEntity;
    
    private void Awake()
    {
        // Get the LivingEntity component
        livingEntity = GetComponent<LivingEntity>();
        
        // Subscribe to the death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(DropChitinOnDeath);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(DropChitinOnDeath);
        }
    }
    
    private void DropChitinOnDeath()
    {
        if (livingEntity == null || chitinPrefab == null)
            return;
            
        int amountToDrop = livingEntity.chitinAmount;
        
        if (showDebugLogs)
            Debug.Log($"Dropping {amountToDrop} chitin pieces from {gameObject.name}");
        
        // Start the coroutine to spawn pieces one by one
        StartCoroutine(SpawnChitinPieces(amountToDrop));
    }
    
    private IEnumerator SpawnChitinPieces(int amount)
    {
        // Safety check
        if (amount <= 0 || chitinPrefab == null)
            yield break;
            
        // Check if the prefab has the required component
        if (!chitinPrefab.GetComponent<ChitinCollectible>())
        {
            Debug.LogError($"Chitin prefab is missing ChitinCollectible component!");
            yield break;
        }
        
        // Spawn one piece at a time with a tiny delay
        for (int i = 0; i < amount; i++)
        {
            // Create the chitin piece
            Vector3 dropPosition = livingEntity.transform.position + new Vector3(0, dropHeight, 0);
            GameObject chitinObject = Instantiate(chitinPrefab, dropPosition, Quaternion.Euler(0, Random.Range(0, 360), 0));
            
            // Verify the spawned object has the required component
            if (!chitinObject.GetComponent<ChitinCollectible>())
            {
                Debug.LogError("Spawned chitin is missing ChitinCollectible component! Adding it now.");
                chitinObject.AddComponent<ChitinCollectible>();
            }
            
            // Set initial layer to NonpickableLoot
            chitinObject.layer = LayerMask.NameToLayer("NonpickableLoot");
            
            // Use a coroutine for a simple animation instead of physics
            StartCoroutine(SimpleChitinAnimation(chitinObject));
            
            // Small delay between spawns
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator SimpleChitinAnimation(GameObject chitinObject)
    {
        // Early exit if object is null
        if (chitinObject == null) yield break;
        
        // Get the collectible component
        ChitinCollectible collectible = chitinObject.GetComponent<ChitinCollectible>();
        if (collectible == null)
        {
            Debug.LogError("ChitinCollectible component missing on spawned chitin!");
            collectible = chitinObject.AddComponent<ChitinCollectible>();
        }
        
        // Disable collection until animation completes
        collectible.enabled = false;
        
        // Get the rigidbody but disable physics
        Rigidbody rb = chitinObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Starting position
        Vector3 startPos = chitinObject.transform.position;
        
        // Calculate a random landing position with spread
        float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * Random.Range(0.2f, 0.8f),
            0,
            Mathf.Sin(angle) * Random.Range(0.2f, 0.8f)
        );
        
        // End position - on the ground with random offset
        Vector3 endPos = new Vector3(startPos.x + offset.x, 0.05f, startPos.z + offset.z);
        
        // Calculate peak position with higher arc
        float peakHeight = Random.Range(0.3f, 0.6f);
        Vector3 peakPos = (startPos + endPos) * 0.5f + Vector3.up * peakHeight;
        
        // Animate over time
        float duration = 0.5f;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            // Check if the object has been destroyed
            if (chitinObject == null) yield break;
            
            float t = elapsed / duration;
            
            // Quadratic Bezier curve for a nice arc
            Vector3 m1 = Vector3.Lerp(startPos, peakPos, t);
            Vector3 m2 = Vector3.Lerp(peakPos, endPos, t);
            chitinObject.transform.position = Vector3.Lerp(m1, m2, t);
            
            // Rotation for visual interest
            chitinObject.transform.Rotate(Vector3.up, 120 * Time.deltaTime);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Check if the object has been destroyed
        if (chitinObject == null) yield break;
        
        // Ensure we end exactly at the end position
        chitinObject.transform.position = endPos;
        
        // NOW set the layer to Loot so it can be collected
        chitinObject.gameObject.layer = LayerMask.NameToLayer("Loot");
        
        // Enable the collectible component
        collectible.enabled = true;
        
        // Check if player is nearby for immediate collection
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && chitinObject != null) // Add null check here
        {
            float distanceToPlayer = Vector3.Distance(chitinObject.transform.position, player.transform.position);
            if (distanceToPlayer <= collectible.GetCollectRadius())
            {
                Debug.Log($"Player is nearby ({distanceToPlayer}), forcing collection");
                collectible.ForceCollect();
            }
        }
    }
} 