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
    private bool animationStarted = false;
    
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
        
        // Check if we were destroyed during animation
        if (animationStarted && !isBeingCollected)
        {
            Debug.LogError("[COLLECTION DEBUG] Object was destroyed before animation could properly start!");
        }
    }
    
    // Called when the chitin sticks to the ground
    private void OnChitinStuck()
    {
        Debug.Log("Chitin stuck event received - making collectable");
        isCollectable = true;
        
        // Make sure we're on the correct layer
        gameObject.layer = LayerMask.NameToLayer("Loot");
        
        // Make the Rigidbody kinematic and discrete once stuck
        if (rb != null)
        {
            Debug.Log($"Setting Rigidbody to kinematic and discrete. Previous state: isKinematic={rb.isKinematic}, collisionMode={rb.collisionDetectionMode}");
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Debug.Log($"Rigidbody state after change: isKinematic={rb.isKinematic}, collisionMode={rb.collisionDetectionMode}");
        }
        else
        {
            Debug.LogWarning("Rigidbody is null in OnChitinStuck!");
        }
    }
    
    private void Update()
    {
        // Don't process collection logic if not collectable yet or already being collected
        if (!isCollectable || isBeingCollected)
            return;
            
        // Ensure Rigidbody settings remain consistent when collectable
        if (isCollectable && rb != null && (!rb.isKinematic || rb.collisionDetectionMode != CollisionDetectionMode.Discrete))
        {
            Debug.Log("Fixing Rigidbody settings that were changed after becoming collectable");
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        
        // Ensure we're on the correct layer
        if (isCollectable && gameObject.layer != LayerMask.NameToLayer("Loot"))
        {
            Debug.Log("[COLLECTION DEBUG] Fixing layer - setting to Loot layer");
            gameObject.layer = LayerMask.NameToLayer("Loot");
        }
        
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
    
    private IEnumerator CollectionAnimation()
    {
        animationStarted = true;
        Debug.Log($"[COLLECTION DEBUG] Starting collection animation for {gameObject.name}");
        
        // Add a yield return null to ensure we get at least one frame
        yield return null;
        
        if (this == null || gameObject == null) {
            Debug.LogError("[COLLECTION DEBUG] Object destroyed immediately after starting animation!");
            yield break;
        }
        
        isBeingCollected = true;
        isCollectable = false;
        
        // Disable any colliders to prevent further interactions
        Collider collider = GetComponent<Collider>();
        if (collider != null) {
            Debug.Log($"[COLLECTION DEBUG] Disabling collider");
            collider.enabled = false;
        }
        
        // Disable any rigidbody physics
        if (rb != null)
        {
            Debug.Log($"[COLLECTION DEBUG] Setting rigidbody: isKinematic={true}, velocity={Vector3.zero}");
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }
        
        // Store initial position
        Vector3 startPosition = transform.position;
        Vector3 highPoint = startPosition + Vector3.up * floatHeight;
        Debug.Log($"[COLLECTION DEBUG] Animation path: start={startPosition}, highPoint={highPoint}");
        
        // Float up animation
        float elapsedTime = 0;
        while (elapsedTime < 1.0f)
        {
            if (this == null || gameObject == null) {
                Debug.LogError("[COLLECTION DEBUG] Object destroyed during float-up animation!");
                yield break;
            }
            
            transform.position = Vector3.Lerp(startPosition, highPoint, elapsedTime);
            Debug.Log($"[COLLECTION DEBUG] Float up: pos={transform.position}, progress={elapsedTime}");
            elapsedTime += Time.deltaTime * floatSpeed;
            yield return null;
        }
        
        // Ensure we reach the high point
        if (this == null || gameObject == null) {
            Debug.LogError("[COLLECTION DEBUG] Object destroyed before reaching high point!");
            yield break;
        }
        
        transform.position = highPoint;
        Debug.Log($"[COLLECTION DEBUG] Reached high point: {highPoint}");
        
        // Small pause at the top
        yield return new WaitForSeconds(0.1f);
        
        if (this == null || gameObject == null) {
            Debug.LogError("[COLLECTION DEBUG] Object destroyed during pause!");
            yield break;
        }
        
        // Drop onto player animation
        elapsedTime = 0;
        while (elapsedTime < 1.0f && playerTransform != null)
        {
            if (this == null || gameObject == null) {
                Debug.LogError("[COLLECTION DEBUG] Object destroyed during drop animation!");
                yield break;
            }
            
            // Get current player position (with slight offset upward)
            Vector3 targetPosition = playerTransform.position + Vector3.up * 0.5f;
            
            // Move toward player
            transform.position = Vector3.Lerp(highPoint, targetPosition, elapsedTime);
            Debug.Log($"[COLLECTION DEBUG] Drop to player: pos={transform.position}, progress={elapsedTime}");
            
            elapsedTime += Time.deltaTime * dropSpeed;
            yield return null;
        }
        
        Debug.Log("[COLLECTION DEBUG] Animation complete, adding to inventory");
        
        // Add to player inventory
        if (playerInventory != null)
        {
            playerInventory.AddChitin(1);
            Debug.Log("[COLLECTION DEBUG] Added chitin to inventory");
        }
        
        // Destroy the object
        Debug.Log("[COLLECTION DEBUG] Destroying chitin object");
        Destroy(gameObject);
    }

    // Improved force collect method
    public void ForceCollect()
    {
        Debug.Log("[COLLECTION DEBUG] Force collect called on chitin");
        
        // Make sure we have a reference to the player
        if (playerTransform == null || playerInventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Debug.Log("[COLLECTION DEBUG] Found player in ForceCollect");
                playerTransform = player.transform;
                playerInventory = player.GetComponent<PlayerInventory>();
            }
            else
            {
                Debug.LogError("[COLLECTION DEBUG] Could not find player in ForceCollect!");
                return;
            }
        }
        
        // Make sure we have a reference to the rigidbody
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            Debug.Log("[COLLECTION DEBUG] Got rigidbody reference in ForceCollect");
        }
        
        // Only proceed if we have the player and aren't already being collected
        if (!isBeingCollected && playerInventory != null)
        {
            Debug.Log("[COLLECTION DEBUG] Starting forced chitin collection");
            isCollectable = true;
            
            // Ensure rigidbody is properly set up for animation
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                Debug.Log("[COLLECTION DEBUG] Set up rigidbody in ForceCollect");
            }
            
            // Start the collection animation
            StartCoroutine(CollectionAnimation());
        }
        else
        {
            Debug.LogWarning("[COLLECTION DEBUG] ForceCollect failed: " + 
                            (isBeingCollected ? "already being collected" : "no player inventory"));
        }
    }

    // Make collection logic accessible for both proximity and trigger methods
    public bool TryCollect()
    {
        Debug.Log($"[COLLECTION DEBUG] TryCollect called. isCollectable={isCollectable}, isBeingCollected={isBeingCollected}");
        
        if (!isCollectable || isBeingCollected || playerInventory == null)
        {
            Debug.Log("[COLLECTION DEBUG] Collection failed: " + 
                     (!isCollectable ? "not collectable" : 
                      isBeingCollected ? "already being collected" : "no player inventory"));
            return false;
        }
        
        // Check if player's chitin inventory is full
        if (!playerInventory.IsChitinFull)
        {
            Debug.Log("[COLLECTION DEBUG] Inventory not full, starting collection");
            
            // Play pickup sound if available
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                Debug.Log("[COLLECTION DEBUG] Playing pickup sound");
            }
            
            // Start collection animation
            Debug.Log("[COLLECTION DEBUG] Starting collection animation coroutine");
            StartCoroutine(CollectionAnimation());
            return true;
        }
        else
        {
            Debug.Log("[COLLECTION DEBUG] Inventory full, cannot collect");
            
            // Player inventory is full - play different sound and/or show message
            if (fullInventorySound != null)
            {
                // Only play this sound occasionally to avoid spamming
                if (Time.time % 1.0f < 0.1f) // Play roughly every second
                {
                    AudioSource.PlayClipAtPoint(fullInventorySound, transform.position);
                    Debug.Log("[COLLECTION DEBUG] Playing full inventory sound");
                }
            }
        }
        
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // If player enters the trigger and we're collectable but not being collected
        if (other.CompareTag("Player") && isCollectable && !isBeingCollected)
        {
            Debug.Log("[COLLECTION DEBUG] Player entered chitin trigger - attempting collection");
            
            // Check if we're within collection radius
            float distanceToPlayer = Vector3.Distance(transform.position, other.transform.position);
            Debug.Log($"[COLLECTION DEBUG] Distance to player: {distanceToPlayer}, collectRadius: {collectRadius}");
            
            if (distanceToPlayer <= collectRadius)
            {
                // Make sure we have a reference to the player inventory
                if (playerInventory == null)
                {
                    Debug.Log("[COLLECTION DEBUG] Getting playerInventory from colliding player");
                    playerInventory = other.GetComponent<PlayerInventory>();
                }
                
                // Try to collect
                bool collected = TryCollect();
                Debug.Log($"[COLLECTION DEBUG] TryCollect result: {collected}");
            }
        }
    }

    private void OnEnable()
    {
        Debug.Log("[COLLECTION DEBUG] ChitinCollectible enabled");
        
        // Make sure we have references when enabled
        if (playerTransform == null || playerInventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerInventory = player.GetComponent<PlayerInventory>();
                Debug.Log("[COLLECTION DEBUG] Found player references in OnEnable");
            }
        }
        
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            Debug.Log("[COLLECTION DEBUG] Got rigidbody reference in OnEnable");
        }
    }

    // Public method to check if being collected
    public bool IsBeingCollected()
    {
        return isBeingCollected;
    }
} 