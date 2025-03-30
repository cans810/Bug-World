using UnityEngine;
using System.Collections;
using CandyCoded.HapticFeedback;

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
        
        // CRITICAL: Check inventory capacity directly - BUT ONLY STOP COLLECTION IF FULL
        // This allows multiple chitins to be collected rapidly up to the limit
        if (playerInventory.ChitinCount >= playerInventory.MaxChitinCapacity)
        {
            // Inventory is already full, don't start the animation
            Debug.Log($"Cannot collect {gameObject.name}: chitin inventory full ({playerInventory.ChitinCount}/{playerInventory.MaxChitinCapacity})");
            
            // Player inventory is full - play different sound and/or show message occasionally
            if (fullInventorySound != null && Time.time % 1.0f < 0.1f)
            {
                AudioSource.PlayClipAtPoint(fullInventorySound, transform.position);
            }
            
            return false;
        }
        
        // Mark as being collected immediately to prevent multiple collection attempts
        isBeingCollected = true;
        
        // Add a lock to the name to prevent other scripts from destroying it
        gameObject.name = gameObject.name + "_" + ANIMATION_LOCK_KEY;
        
        // Play pickup sound if available
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Add haptic feedback for collection
        HapticFeedback.HeavyFeedback();
        
        Debug.Log($"Starting collection animation for {gameObject.name}");
        
        // Start collection animation - but make it faster for smooth multiple item collection
        StartCoroutine(CollectionAnimation());
        return true;
    }
    
    private IEnumerator CollectionAnimation()
    {
        // Make the animation much faster for multiple item collection
        float animationSpeedMultiplier = 2.0f; // Double speed for smoother collection
        
        isAnimationRunning = true;
        isBeingCollected = true;
        isCollectable = false;
        
        // Disable collider to prevent duplicate collection attempts
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Disable physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }
        
        // Store initial position and scale
        Vector3 startPosition = transform.position;
        Vector3 highPoint = startPosition + Vector3.up * floatHeight;
        Vector3 originalScale = transform.localScale;
        
        // Get current player position immediately for faster animation
        Vector3 targetPosition = playerTransform?.position ?? transform.position;
        
        // Skip float-up for faster collection when multiple items
        if (playerInventory != null && playerInventory.ChitinCount < playerInventory.MaxChitinCapacity - 5)
        {
            // Main animation - directly to player
            float elapsedTime = 0;
            while (elapsedTime < 0.5f && playerTransform != null) // Shorter animation
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime * 2.0f);
                transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.2f, elapsedTime * 2.0f);
                
                elapsedTime += Time.deltaTime * animationSpeedMultiplier;
                yield return null;
            }
        }
        else
        {
            // Normal animation but faster - for when approaching capacity
            // Float up animation
            float elapsedTime = 0;
            while (elapsedTime < 0.5f) // Half the duration
            {
                transform.position = Vector3.Lerp(startPosition, highPoint, elapsedTime * 2.0f);
                elapsedTime += Time.deltaTime * floatSpeed * animationSpeedMultiplier;
                yield return null;
            }
            
            // Drop toward player
            elapsedTime = 0;
            while (elapsedTime < 0.5f && playerTransform != null)
            {
                transform.position = Vector3.Lerp(highPoint, targetPosition, elapsedTime * 2.0f);
                transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.2f, elapsedTime * 2.0f);
                
                elapsedTime += Time.deltaTime * dropSpeed * animationSpeedMultiplier;
                yield return null;
            }
        }
        
        // FINAL CHECK: Add to player inventory only if there's still room
        if (playerInventory != null && playerInventory.ChitinCount < playerInventory.MaxChitinCapacity)
        {
            playerInventory.AddChitin(1);
            Destroy(gameObject);
        }
        else
        {
            // Return object to its original state if inventory became full
            transform.position = startPosition;
            transform.localScale = originalScale;
            
            if (collider != null) collider.enabled = true;
            if (rb != null) {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
            }
            
            gameObject.name = gameObject.name.Replace("_" + ANIMATION_LOCK_KEY, "");
            isBeingCollected = false;
            isCollectable = true;
        }
        
        isAnimationRunning = false;
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