using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [Header("Chitin Drop Settings")]
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private int minChitinToDrop = 1;
    [SerializeField] private int maxChitinToDrop = 3;
    [SerializeField] private float dropHeight = 0.3f;
    
    [Header("Physics Forces")]
    [SerializeField] private float upwardForceMin = 2.0f;
    [SerializeField] private float upwardForceMax = 4.0f;
    [SerializeField] private float horizontalForceMin = 1.0f;
    [SerializeField] private float horizontalForceMax = 3.0f;
    [SerializeField] private float torqueForce = 0.3f;
    
    [Header("Collection Delay")]
    [SerializeField] private float collectionDelay = 0.8f; // Delay before chitins become collectible
    
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
            Debug.Log($"Dropping {amountToDrop} chitin pieces from {gameObject.name} using physics");
        
        // Drop all chitin pieces at once
        for (int i = 0; i < amountToDrop; i++)
        {
            // Spawn and apply physics
            SpawnChitinWithPhysics();
            
            // Small delay between spawns to prevent explosion effect
            StartCoroutine(DelayBetweenSpawns(0.12f)); // Increased from 0.05f to 0.12f
        }
    }
    
    private IEnumerator DelayBetweenSpawns(float delay)
    {
        yield return new WaitForSeconds(delay);
    }
    
    private void SpawnChitinWithPhysics()
    {
        // Spawn position directly above the enemy with no horizontal offset
        Vector3 spawnPosition = transform.position + new Vector3(0, dropHeight, 0);
        
        // Create chitin with random rotation
        GameObject chitin = Instantiate(chitinPrefab, spawnPosition, Quaternion.Euler(
            Random.Range(0, 360),
            Random.Range(0, 360),
            Random.Range(0, 360)
        ));
        
        // Verify it has the ChitinCollectible component
        ChitinCollectible collectible = chitin.GetComponent<ChitinCollectible>();
        if (collectible == null)
        {
            collectible = chitin.AddComponent<ChitinCollectible>();
            Debug.LogWarning("Had to add ChitinCollectible component to spawned chitin");
        }
        
        // Set to non-collectible layer initially
        chitin.layer = LayerMask.NameToLayer("NonpickableLoot");
        
        // Get or add rigidbody
        Rigidbody rb = chitin.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = chitin.AddComponent<Rigidbody>();
            Debug.LogWarning("Had to add Rigidbody to chitin");
        }
        
        // Configure the Rigidbody for better physics
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = 0.5f;               // Lower mass means more responsive to forces
        rb.drag = 0.2f;              // Low drag for longer air time
        rb.angularDrag = 0.05f;       // Low angular drag for more rotation
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smoother movement
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
        
        // Apply more moderate upward force for a natural arch
        float upwardForce = Random.Range(2.5f, 4.0f); // Reduced from 5.0-8.0 to 2.5-4.0
        
        // Apply very minimal horizontal force for slight spread
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector3 horizontalForce = new Vector3(
            randomDirection.x,
            0,
            randomDirection.y
        ) * Random.Range(0.2f, 0.6f); // Reduced horizontal force significantly
        
        // Apply the combined force - more vertical than horizontal
        rb.AddForce(new Vector3(horizontalForce.x, upwardForce, horizontalForce.z), ForceMode.Impulse);
        
        // Apply very gentle torque for subtle spinning
        rb.AddTorque(new Vector3(
            Random.Range(-0.1f, 0.1f), 
            Random.Range(-0.1f, 0.1f),
            Random.Range(-0.1f, 0.1f)
        ), ForceMode.Impulse);
        
        // Get collider
        Collider collider = chitin.GetComponent<Collider>();
        if (collider != null)
        {
            // Make sure it's not a trigger
            collider.isTrigger = false;
            
            // Make sure the collider is not too large (can prevent proper physics)
            if (collider is BoxCollider boxCollider)
            {
                // Ensure the box collider size is reasonable
                if (boxCollider.size.magnitude > 5f)
                {
                    boxCollider.size = Vector3.one * 0.5f;
                    Debug.LogWarning("Adjusted oversized box collider");
                }
            }
        }
        else
        {
            Debug.LogError("Chitin has no collider!");
        }
        
        // Start coroutine to make collectible after delay
        StartCoroutine(MakeCollectibleAfterDelay(chitin, collectionDelay));
    }
    
    private IEnumerator MakeCollectibleAfterDelay(GameObject chitin, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Only proceed if the object still exists
        if (chitin != null)
        {
            chitin.layer = LayerMask.NameToLayer("Loot");
            
            // Enable the collectible component
            ChitinCollectible collectible = chitin.GetComponent<ChitinCollectible>();
            if (collectible != null)
            {
                collectible.enabled = true;
            }
        }
    }
} 