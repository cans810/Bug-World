using UnityEngine;
using System.Collections;

public class ChitinCollectible : MonoBehaviour
{
    [SerializeField] private float collectRadius = 1.5f; // Distance at which player can collect
    [SerializeField] private float lifetimeSeconds = 60f; // How long before the crumb disappears
    [SerializeField] private AudioClip pickupSound; // Sound when collected
    [SerializeField] private AudioClip fullInventorySound; // Sound when inventory is full
    [SerializeField] private bool useProximityCollection = true; // Whether to use proximity-based collection
    
    [Header("Collection Animation")]
    [SerializeField] private float floatHeight = 0.7f; // How high the item floats
    [SerializeField] private float floatSpeed = 2.0f; // Speed of floating animation
    [SerializeField] private float dropSpeed = 6.0f; // Speed of dropping onto player
    [SerializeField] private float collectionDelay = 0.3f; // Delay before actually adding to inventory
    
    private Transform playerTransform;
    private PlayerInventory playerInventory;
    private float spawnTime;
    private bool isCollectable = true;
    private bool isBeingCollected = false;
    private bool isAnimationRunning = false;
    
    private static readonly string ANIMATION_LOCK_KEY = "CHITIN_ANIMATION_LOCK";
    
    private void Start()
    {
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerInventory = player.GetComponent<PlayerInventory>();
            
            // Check if we should collect immediately
            if (useProximityCollection && Vector3.Distance(transform.position, playerTransform.position) <= collectRadius)
            {
                TryCollect();
            }
        }
        
        spawnTime = Time.time;
        
        // Destroy after lifetime
        Destroy(gameObject, lifetimeSeconds);
        
        // Make sure we're on the correct layer
        if (gameObject.layer != LayerMask.NameToLayer("Loot") && 
            gameObject.layer != LayerMask.NameToLayer("NonpickableLoot"))
        {
            gameObject.layer = LayerMask.NameToLayer("Loot");
        }
        
        // Ensure physics behavior
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        // Make sure collider is not a trigger
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false;
        }
        
        Debug.Log($"ChitinCollectible initialized on {gameObject.name} with physics");
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
        // Don't collect if already being collected or not collectable
        if (!isCollectable || isBeingCollected || playerInventory == null)
        {
            Debug.Log($"Cannot collect {gameObject.name}: isCollectable={isCollectable}, isBeingCollected={isBeingCollected}, playerInventory={playerInventory != null}");
            return false;
        }
        
        // Check if this object has a lock on it (being animated by another script)
        if (gameObject.GetInstanceID().ToString().Contains(ANIMATION_LOCK_KEY))
        {
            Debug.Log($"Cannot collect {gameObject.name}: animation lock is active");
            return false;
        }
        
        // Mark as being collected immediately to prevent multiple collection attempts
        isBeingCollected = true;
        
        // Add a lock to the name to prevent other scripts from destroying it
        gameObject.name = gameObject.name + "_" + ANIMATION_LOCK_KEY;
        
        // Check if player's CHITIN inventory is full
        if (!playerInventory.IsChitinFull)
        {
            // Play pickup sound if available
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            
            Debug.Log($"Starting collection animation for {gameObject.name}");
            
            // Start collection animation
            StartCoroutine(CollectionAnimation());
            return true;
        }
        else
        {
            // Reset the being collected flag since we're not actually collecting
            isBeingCollected = false;
            
            // Remove the lock
            gameObject.name = gameObject.name.Replace("_" + ANIMATION_LOCK_KEY, "");
            
            Debug.Log($"Cannot collect {gameObject.name}: chitin inventory is full");
            
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
        isAnimationRunning = true;
        
        Debug.Log($"Collection animation started for {gameObject.name}");
        isBeingCollected = true;
        isCollectable = false;
        
        // Disable any colliders to prevent further interactions
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Debug.Log("Disabled collider for collection animation");
        }
            
        // Disable any rigidbody physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            Debug.Log("Set rigidbody to kinematic for collection animation");
        }
        
        // Store initial position and scale
        Vector3 startPosition = transform.position;
        Vector3 highPoint = startPosition + Vector3.up * floatHeight;
        Vector3 originalScale = transform.localScale;
        
        Debug.Log($"Animation parameters: Start pos {startPosition}, high point {highPoint}");
        
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
        
        // Drop onto player animation with shrinking effect
        elapsedTime = 0;
        while (elapsedTime < 1.0f && playerTransform != null)
        {
            // Get current player position (exactly at player position, not offset)
            Vector3 targetPosition = playerTransform.position;
            
            // Calculate progress - ensure it's clamped
            float progress = Mathf.Clamp01(elapsedTime);
            
            // Move toward player
            transform.position = Vector3.Lerp(highPoint, targetPosition, progress);
            
            // Shrink as it moves down
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.2f, progress);
            
            elapsedTime += Time.deltaTime * dropSpeed;
            yield return null;
        }
        
        // Ensure it reaches exactly the player position
        if (playerTransform != null)
        {
            transform.position = playerTransform.position;
            transform.localScale = originalScale * 0.2f;
        }
        
        Debug.Log("Completed drop animation, adding to inventory");
        
        // Add to player inventory
        if (playerInventory != null)
        {
            playerInventory.AddChitin(1);
            Debug.Log($"Chitin collected from {gameObject.name}, added to chitin inventory");
        }
        else
        {
            Debug.LogError("Player inventory is null at collection time!");
        }
        
        isAnimationRunning = false;
        Debug.Log($"Destroying {gameObject.name} after collection");
        Destroy(gameObject);
    }

    // Public getter for collect radius
    public float GetCollectRadius() => collectRadius;

    // Force collection method for compatibility
    public void ForceCollect()
    {
        Debug.Log($"ForceCollect called on {gameObject.name}");
        TryCollect();
    }

    private void Awake()
    {
        // Make sure we're on the right layer
        if (gameObject.layer != LayerMask.NameToLayer("Loot"))
        {
            Debug.Log($"Setting {gameObject.name} to Loot layer");
            gameObject.layer = LayerMask.NameToLayer("Loot");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Skip if already being collected or not collectable
        if (!isCollectable || isBeingCollected || isAnimationRunning)
        {
            Debug.Log($"Skipping collision collection for {gameObject.name}: already being processed");
            return;
        }
        
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"Player collided with {gameObject.name}, attempting collection");
            TryCollect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Skip if we have a non-trigger collider (we'll use OnCollisionEnter instead)
        Collider ownCollider = GetComponent<Collider>();
        if (ownCollider != null && !ownCollider.isTrigger)
        {
            return;
        }
        
        // Skip if already being collected or not collectable
        if (!isCollectable || isBeingCollected || isAnimationRunning)
        {
            Debug.Log($"Skipping trigger collection for {gameObject.name}: already being processed");
            return;
        }
        
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered trigger for {gameObject.name}, attempting collection");
            TryCollect();
        }
    }

    private void OnDestroy()
    {
        if (isAnimationRunning)
        {
            // Get the stack trace to see what's destroying the object
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            Debug.LogWarning($"Object {gameObject.name} was destroyed while animation was running!\nStack trace: {stackTrace}");
        }
    }
} 