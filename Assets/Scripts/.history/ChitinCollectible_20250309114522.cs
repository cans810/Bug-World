using UnityEngine;
using System.Collections;

public class ChitinCollectible : MonoBehaviour
{
    [SerializeField] private float collectRadius = 1.5f; // Distance at which player can collect
    [SerializeField] private float lifetimeSeconds = 60f; // How long before the chitin disappears
    [SerializeField] private AudioClip pickupSound; // Sound when collected
    [SerializeField] private AudioClip fullInventorySound; // Sound when inventory is full
    
    [Header("Collection Animation")]
    [SerializeField] private float floatHeight = 0.7f; // How high the item floats
    [SerializeField] private float floatSpeed = 4.0f; // Speed of floating animation
    [SerializeField] private float dropSpeed = 8.0f; // Speed of dropping onto player
    [SerializeField] private float collectionDelay = 0.3f; // Delay before actually adding to inventory
    
    private Transform playerTransform;
    private PlayerInventory playerInventory;
    private Rigidbody rb;
    private float spawnTime;
    private ChitinBehavior chitinBehavior;
    private bool isBeingCollected = false;
    

    // Public getter for collect radius
    public float GetCollectRadius() => collectRadius;
    
    private void Awake()
    {
        // Get reference to the ChitinBehavior component
        chitinBehavior = GetComponent<ChitinBehavior>();
        
        // Get rigidbody reference
        rb = GetComponent<Rigidbody>();
    }
    
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
        
        // Subscribe to the chitin stuck event
        if (chitinBehavior != null)
        {
            chitinBehavior.OnChitinStuck += OnChitinStuck;
        }
        
        // Destroy after lifetime
        Destroy(gameObject, lifetimeSeconds);
        
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from event to prevent memory leaks
        if (chitinBehavior != null)
        {
            chitinBehavior.OnChitinStuck -= OnChitinStuck;
        }
    }
    
    // Called when the chitin sticks to the ground
    private void OnChitinStuck()
    {
        // ! change the layer to loot to make sure it can be collected
        gameObject.layer = LayerMask.NameToLayer("Loot");
    }
    
    private void Update()
    {
        // Don't process collection logic if not collectable yet or already being collected
        if (isBeingCollected)
            return;
            
        if (playerTransform == null || playerInventory == null)
        {
            // Try to find player again if references are missing
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerInventory = player.GetComponent<PlayerInventory>();
            }
            else
            {
                return;
            }
        }
            
        // Get distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // If close enough to collect
        if (distanceToPlayer <= collectRadius)
        {
            Debug.Log($"Player in range ({distanceToPlayer} <= {collectRadius}), attempting collection");
            TryCollect();
        }
    }
    
    // Centralized collection logic
    public bool TryCollect()
    {
        Debug.Log($"TryCollect called on {gameObject.name}, isCollectable: {isCollectable}, isBeingCollected: {isBeingCollected}");
        
        // Safety checks
        if (isBeingCollected || playerInventory == null)
        {
            Debug.LogWarning($"TryCollect failed: isCollectable={isCollectable}, isBeingCollected={isBeingCollected}, playerInventory={playerInventory != null}");
            return false;
        }
        
        // Check if player's chitin inventory is full
        if (playerInventory.IsChitinFull)
        {
            Debug.Log("Player chitin inventory is full");
            
            // Player inventory is full - play different sound and/or show message
            if (fullInventorySound != null)
            {
                // Only play this sound occasionally to avoid spamming
                if (Time.time % 1.0f < 0.1f) // Play roughly every second
                {
                    AudioSource.PlayClipAtPoint(fullInventorySound, transform.position);
                }
            }
            return false;
        }
        
        // Play pickup sound if available
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            Debug.Log("Playing pickup sound");
        }
        
        // Start collection animation
        Debug.Log("Starting collection animation coroutine");
        StartCoroutine(CollectionAnimation());
        return true;
    }
    
    private IEnumerator CollectionAnimation()
    {
        Debug.Log($"Starting collection animation for {gameObject.name}");
        isBeingCollected = true;
        isCollectable = false;
        
        // Disable any colliders to prevent further interactions
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Debug.Log("Disabled collider during collection");
        }
            
        // Disable any rigidbody physics
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            Debug.Log("Set rigidbody to kinematic during collection");
        }
        
        // Store initial position
        Vector3 startPosition = transform.position;
        Vector3 highPoint = startPosition + Vector3.up * floatHeight;
        
        Debug.Log($"Collection animation: Start pos {startPosition}, high point {highPoint}");
        
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
        Debug.Log("Reached high point in collection animation");
        
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
        
        Debug.Log("Completed drop animation, adding to inventory");
        
        // Add to player inventory
        if (playerInventory != null)
        {
            playerInventory.AddChitin(1);
            Debug.Log("Chitin added to inventory successfully");
        }
        else
        {
            Debug.LogError("Player inventory is null at collection time!");
        }
        
        // Destroy the object
        Destroy(gameObject);
    }

    // Improved force collect method
    public void ForceCollect()
    {
        Debug.Log($"ForceCollect called on {gameObject.name}");
        
        // Make sure we have a reference to the player
        if (playerTransform == null || playerInventory == null)
        {
            Debug.Log("Player reference missing, attempting to find player");
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerInventory = player.GetComponent<PlayerInventory>();
                Debug.Log($"Found player: {player.name}, has inventory: {playerInventory != null}");
            }
            else
            {
                Debug.LogError("Could not find player with tag 'Player'");
                return;
            }
        }
        
        // Only proceed if we have the player and aren't already being collected
        if (isBeingCollected)
        {
            Debug.Log("Already being collected, ignoring ForceCollect");
            return;
        }
        
        if (playerInventory == null)
        {
            Debug.LogError("Player inventory is null, cannot collect");
            return;
        }
        
        Debug.Log("Starting forced chitin collection");
        isCollectable = true;
        bool success = TryCollect();
        Debug.Log($"TryCollect result: {success}");
    }

    // Add a method to check if the collection animation is working
    private void OnTriggerEnter(Collider other)
    {
        // If player enters the trigger and we're collectable but not being collected
        if (other.CompareTag("Player") && isCollectable && !isBeingCollected)
        {
            Debug.Log("Player entered chitin trigger - attempting collection");
            TryCollect();
        }
    }

    private void OnEnable()
    {
        Debug.Log($"ChitinCollectible.OnEnable called on {gameObject.name}");
        
        // When enabled, make sure we're collectable if the chitin has already stuck
        if (chitinBehavior != null && chitinBehavior.HasStuck)
        {
            Debug.Log("Chitin was already stuck when collectible was enabled");
            isCollectable = true;
        }
        
        // Check if player is already nearby when enabled
        if (isCollectable)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= collectRadius)
                {
                    Debug.Log($"Player is already within collection radius ({distance}) when enabled");
                    ForceCollect();
                }
            }
        }
    }

    // Add this method to help with debugging
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Collision with {collision.gameObject.name}, layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Collision with player detected");
            if (isCollectable && !isBeingCollected)
            {
                Debug.Log("Attempting collection from collision");
                TryCollect();
            }
        }
    }
} 