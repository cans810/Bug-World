using UnityEngine;
using System.Collections;

public class ChitinCollectible : MonoBehaviour
{
    [SerializeField] private float collectRadius = 1.5f; // Distance at which player can collect
    [SerializeField] private float lifetimeSeconds = 60f; // How long before the crumb disappears
    [SerializeField] private AudioClip pickupSound; // Sound when collected
    [SerializeField] private AudioClip fullInventorySound; // Sound when inventory is full
    [SerializeField] private bool useProximityCollection = false; // Whether to use proximity-based collection
    
    [Header("Collection Animation")]
    [SerializeField] private float floatHeight = 1.2f; // Increased from 0.7f - make it float higher
    [SerializeField] private float floatSpeed = 2.0f; // Decreased from 4.0f - make it float slower
    [SerializeField] private float dropSpeed = 4.0f; // Decreased from 8.0f - make it drop slower
    [SerializeField] private float collectionDelay = 0.5f; // Increased from 0.3f - longer pause at top
    
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
        }
        
        spawnTime = Time.time;
        
        // Destroy after lifetime
        Destroy(gameObject, lifetimeSeconds);
        
        // Log for debugging
        Debug.Log($"ChitinCollectible initialized on {gameObject.name}");
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
        Color originalColor = GetComponent<Renderer>()?.material.color ?? Color.white;
        
        Debug.Log($"Animation parameters: Start pos {startPosition}, high point {highPoint}");
        
        // Add a visual effect - change color or add glow
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Make it glow by brightening the color
            Color glowColor = new Color(
                Mathf.Min(originalColor.r * 1.5f, 1f),
                Mathf.Min(originalColor.g * 1.5f, 1f),
                Mathf.Min(originalColor.b * 1.5f, 1f),
                originalColor.a
            );
            renderer.material.color = glowColor;
        }
        
        // Make sure the object is visible
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = true;
        }
        
        // Float up animation
        float elapsedTime = 0;
        while (elapsedTime < 1.0f)
        {
            // Add rotation during float up
            transform.Rotate(Vector3.up, 180 * Time.deltaTime);
            
            transform.position = Vector3.Lerp(startPosition, highPoint, elapsedTime);
            elapsedTime += Time.deltaTime * floatSpeed;
            yield return null;
        }
        
        // Ensure we reach the high point
        transform.position = highPoint;
        Debug.Log("Reached high point in collection animation");
        
        // Small pause at the top with a visual pulse
        float pulseTime = 0;
        Vector3 baseScale = originalScale * 1.2f; // Slightly larger at top
        while (pulseTime < collectionDelay)
        {
            // Pulse scale
            float pulse = 1.0f + 0.2f * Mathf.Sin(pulseTime * 10f);
            transform.localScale = baseScale * pulse;
            
            // Continue rotating
            transform.Rotate(Vector3.up, 360 * Time.deltaTime);
            
            pulseTime += Time.deltaTime;
            yield return null;
        }
        
        // Drop onto player animation with shrinking effect
        elapsedTime = 0;
        while (elapsedTime < 1.0f && playerTransform != null)
        {
            // Get current player position (with slight offset upward)
            Vector3 targetPosition = playerTransform.position + Vector3.up * 0.5f;
            
            // Calculate progress
            float progress = elapsedTime;
            
            // Move toward player
            transform.position = Vector3.Lerp(highPoint, targetPosition, progress);
            
            // Shrink as it moves down
            transform.localScale = Vector3.Lerp(baseScale, originalScale * 0.2f, progress);
            
            // Continue rotating faster as it drops
            transform.Rotate(Vector3.up, 540 * Time.deltaTime);
            
            elapsedTime += Time.deltaTime * dropSpeed;
            yield return null;
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isCollectable && !isBeingCollected)
        {
            Debug.Log($"Player entered trigger for {gameObject.name}, attempting collection");
            TryCollect();
        }
    }

    private void OnDestroy()
    {
        if (isAnimationRunning)
        {
            Debug.LogWarning($"Object {gameObject.name} was destroyed while animation was running!");
        }
    }
} 