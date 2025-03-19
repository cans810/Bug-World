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
    private bool isCollectable = false;
    private bool isBeingCollected = false;
    
    private void Awake()
    {
        // Get reference to the ChitinBehavior component
        chitinBehavior = GetComponent<ChitinBehavior>();
        
        // Initially not collectable - will be set collectable once stuck
        isCollectable = false;
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
        
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;
        
        // Subscribe to the chitin stuck event
        if (chitinBehavior != null)
        {
            chitinBehavior.OnChitinStuck += OnChitinStuck;
        }
        else
        {
            // If no behavior script, make it immediately collectable (fallback)
            isCollectable = true;
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
        Debug.Log("Chitin stuck event received - making collectable");
        isCollectable = true;
        
        // Make sure we're on the correct layer
        gameObject.layer = LayerMask.NameToLayer("Loot");
    }
    
    private void Update()
    {
        // Don't process collection logic if not collectable yet or already being collected
        if (!isCollectable || isBeingCollected)
            return;
            
        if (playerTransform == null || playerInventory == null)
            return;
            
        // Get distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // If close enough to collect
        if (distanceToPlayer <= collectRadius)
        {
            // Directly use the IsChitinFull property
            if (!playerInventory.IsChitinFull)
            {
                // Play pickup sound if available
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
                
                // Start collection animation
                StartCoroutine(CollectionAnimation());
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
                
                // Show feedback to player that inventory is full
                // You could add a UI pop-up here or other visual indicator
            }
        }
    }
    
    private IEnumerator CollectionAnimation()
    {
        Debug.Log($"Starting collection animation for {gameObject.name}");
        isBeingCollected = true;
        isCollectable = false;
        
        // Disable any colliders to prevent further interactions
        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
            
        // Disable any rigidbody physics
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
            playerInventory.AddChitin(1);
        }
        
        // Destroy the object
        Destroy(gameObject);
    }

    // Add a public method to force collection for testing
    public void ForceCollect()
    {
        if (!isBeingCollected && playerInventory != null)
        {
            isCollectable = true;
            StartCoroutine(CollectionAnimation());
        }
    }

    // Add a method to check if the collection animation is working
    private void OnTriggerEnter(Collider other)
    {
        // If player enters the trigger and we're collectable but not being collected
        if (other.CompareTag("Player") && isCollectable && !isBeingCollected)
        {
            Debug.Log("Player entered chitin trigger - attempting collection");
            
            // Check if we're within collection radius
            float distanceToPlayer = Vector3.Distance(transform.position, other.transform.position);
            if (distanceToPlayer <= collectRadius)
            {
                // Try to collect
                if (!playerInventory.IsChitinFull)
                {
                    Debug.Log("Starting chitin collection from trigger");
                    StartCoroutine(CollectionAnimation());
                }
            }
        }
    }
} 