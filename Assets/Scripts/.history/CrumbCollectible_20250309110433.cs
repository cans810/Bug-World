using UnityEngine;
using System.Collections;

public class CrumbCollectible : MonoBehaviour
{
    [SerializeField] private float collectRadius = 1.5f; // Distance at which player can collect
    [SerializeField] private float lifetimeSeconds = 60f; // How long before the crumb disappears
    [SerializeField] private AudioClip pickupSound; // Sound when collected
    [SerializeField] private AudioClip fullInventorySound; // Sound when inventory is full
    [SerializeField] private bool useProximityCollection = false; // Whether to use proximity-based collection
    
    [Header("Collection Animation")]
    [SerializeField] private float floatHeight = 1f; // How high the item floats
    [SerializeField] private float floatSpeed = 4.0f; // Speed of floating animation
    [SerializeField] private float dropSpeed = 6.0f; // Speed of dropping onto player
    [SerializeField] private float collectionDelay = 0.3f; // Delay before actually adding to inventory
    
    private Transform playerTransform;
    private PlayerInventory playerInventory;
    private float spawnTime;
    private bool isCollectable = true;
    private bool isBeingCollected = false;
    
    private void Start()
    {
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerInventory = player.GetComponent<PlayerInventory>();
        }
        
        spawnTime = Time.time;
        
        // Destroy after lifetime
        Destroy(gameObject, lifetimeSeconds);
        
        // Log for debugging
        Debug.Log($"CrumbCollectible initialized on {gameObject.name}");
    }
    
    private void Update()
    {
        // If being collected, don't check for proximity collection
        if (isBeingCollected)
            return;
            
        // Only process proximity collection if enabled
        if (!useProximityCollection || !isCollectable)
            return;
            
        if (playerTransform == null || playerInventory == null)
            return;
            
        // Get distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // If close enough to collect
        if (distanceToPlayer <= collectRadius)
        {
            TryCollect();
        }
    }
    
    // Make collection logic accessible for both proximity and trigger methods
    public bool TryCollect()
    {
        if (!isCollectable || isBeingCollected || playerInventory == null)
            return false;
            
        // Check if player's crumb inventory is full
        if (!playerInventory.IsCrumbFull)
        {
            // Play pickup sound if available
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            
            // Start collection animation
            StartCoroutine(CollectionAnimation());
            return true;
        }
        else
        {
            // Player inventory is full - play different sound and/or show message
            if (fullInventorySound != null)
            {
                // Only play this sound occasionally to avoid spamming
                if (Time.time % 1.0f < 0.1f) // Play roughly every second
                {
                    AudioSource.PlayClipAtPoint(fullInventorySound, transform.position);
                }
            }
        }
        
        return false;
    }
    
    private IEnumerator CollectionAnimation()
    {
        isBeingCollected = true;
        isCollectable = false;
        
        // Disable any colliders to prevent further interactions
        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
            
        // Disable any rigidbody physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }
        
        // Store initial position
        Vector3 startPosition = transform.position;
        Vector3 highPoint = startPosition + Vector3.up * floatHeight;
        
        // Float up animation
        float elapsedTime = 0;
        while (elapsedTime < 1.0f)
        {
            transform.position = Vector3.Lerp(startPosition, highPoint, elapsedTime);
            elapsedTime += Time.deltaTime * floatSpeed;
            yield return null;
        }
        
        // Ensure we reach the high point
        transform.position = highPoint;
        
        // Small pause at the top
        yield return new WaitForSeconds(0.1f);
        
        // Drop onto player animation
        elapsedTime = 0;
        while (elapsedTime < 1.0f && playerTransform != null)
        {
            // Get current player position (with slight offset upward)
            Vector3 targetPosition = playerTransform.position + Vector3.up * 0.5f;
            
            // Move toward player
            transform.position = Vector3.Lerp(highPoint, targetPosition, elapsedTime);
            
            elapsedTime += Time.deltaTime * dropSpeed;
            yield return null;
        }
        
        // Add to player inventory
        if (playerInventory != null)
        {
            playerInventory.AddCrumb(1);
            Debug.Log($"Crumb collected from {gameObject.name}, added to crumb inventory");
        }
        
        // Destroy the object
        Destroy(gameObject);
    }
} 