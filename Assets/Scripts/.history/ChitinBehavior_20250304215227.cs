using UnityEngine;

public class ChitinBehavior : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float minArcHeight = 1.0f;  // Minimum peak height 
    [SerializeField] private float stickHeight = 0.05f;  // Height at which to stick
    [SerializeField] private float initialUpForce = 8f;  // Increased upward force
    [SerializeField] private float horizontalForce = 2f; // Separate horizontal scatter force
    [SerializeField] private float spinSpeed = 2f;       // How fast to spin while falling
    
    // Debug options
    [SerializeField] private bool debugMode = false;
    
    // Add these new properties:
    [Header("Collection Settings")]
    [SerializeField] private float collectibleDelay = 0.5f; // Time before chitin can be collected
    [SerializeField] private string playerTag = "Player";   // Tag to identify the player
    [SerializeField] private bool autoCollect = false;      // If true, auto-collect on player contact
    [SerializeField] private float autoCollectRadius = 1.5f; // Distance for auto-collection
    
    private Rigidbody rb;
    private bool hasReachedPeak = false;
    private bool hasStuck = false;
    private Vector3 startPosition;
    private float highestPoint;
    private bool hasAppliedForce = false;
    private bool isCollectible = false;
    private float stuckTime = 0f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        highestPoint = startPosition.y;
        
        // Ensure the rigidbody is properly configured
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }
    
    private void Start()
    {
        // Apply forces immediately in Start to ensure proper launching
        ApplyInitialForce();
        
        // Force a delayed check in case regular updates miss the exact height
        Invoke("ForcedHeightCheck", 2.0f);
    }
    
    // Apply force in a separate method
    private void ApplyInitialForce()
    {
        if (rb != null && !hasAppliedForce)
        {
            hasAppliedForce = true;
            
            // Clear any existing velocity
            rb.velocity = Vector3.zero;
            
            // Apply strong upward force
            Vector3 upForce = Vector3.up * initialUpForce;
            
            // Apply horizontal scatter separately
            Vector3 horizontalScatter = new Vector3(
                Random.Range(-1f, 1f), 
                0f, 
                Random.Range(-1f, 1f)
            ).normalized * horizontalForce;
            
            // Combine forces and apply
            rb.AddForce(upForce + horizontalScatter, ForceMode.Impulse);
            
            // Add random rotation
            rb.angularVelocity = new Vector3(
                Random.Range(-spinSpeed, spinSpeed),
                Random.Range(-spinSpeed, spinSpeed),
                Random.Range(-spinSpeed, spinSpeed)
            );
            
            if (debugMode)
                Debug.Log($"Applied launch force: {upForce + horizontalScatter}, Upward: {initialUpForce}");
        }
    }
    
    // Use FixedUpdate for physics-based checks
    private void FixedUpdate()
    {
        if (hasStuck) return;
        
        // Track the highest point reached
        if (transform.position.y > highestPoint)
        {
            highestPoint = transform.position.y;
            
            if (debugMode)
                Debug.Log($"New highest point: {highestPoint}");
        }
        
        // Check if we've reached the minimum arc height
        if (!hasReachedPeak && highestPoint >= startPosition.y + minArcHeight)
        {
            hasReachedPeak = true;
            if (debugMode)
                Debug.Log("Reached peak of arc");
        }
        
        // Check for sticking - simplified to check y position more directly
        if (transform.position.y <= stickHeight)
        {
            if (debugMode)
                Debug.Log($"Sticking at height: {transform.position.y}, target: {stickHeight}");
            
            StickInPlace();
        }
    }
    
    // Fallback method in case we miss the exact height during normal updates
    private void ForcedHeightCheck()
    {
        if (!hasStuck && transform.position.y <= stickHeight)
        {
            if (debugMode)
                Debug.Log("Forced height check triggered");
            
            StickInPlace();
        }
    }
    
    // Modify StickInPlace() to start the collectible timer
    private void StickInPlace()
    {
        // Exit if already stuck
        if (hasStuck) return;
        
        // Mark as stuck
        hasStuck = true;
        
        // Set exact Y position to stick height
        Vector3 stickPosition = transform.position;
        stickPosition.y = stickHeight;
        transform.position = stickPosition;
        
        // Freeze the rigidbody completely
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.isKinematic = true;
        }
        
        // Start timer to make collectible
        stuckTime = Time.time;
        
        if (debugMode)
            Debug.Log("Chitin stuck successfully at " + transform.position);
    }
    
    // Add collection detection
    private void Update()
    {
        // Skip if not stuck yet or already collectible
        if (!hasStuck || isCollectible) return;
        
        // Check if enough time has passed since sticking
        if (Time.time >= stuckTime + collectibleDelay)
        {
            isCollectible = true;
            
            if (debugMode)
                Debug.Log("Chitin is now collectible");
            
            // Optional: visual indication that chitin is collectible
            // For example, you could add a small glow effect or animation here
        }
        
        // Check for auto-collection if enabled
        if (isCollectible && autoCollect)
        {
            CheckForAutoCollection();
        }
    }
    
    // Auto-collect when player is nearby
    private void CheckForAutoCollection()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= autoCollectRadius)
            {
                CollectChitin();
            }
        }
    }
    
    // Handle manual collection via trigger
    private void OnTriggerEnter(Collider other)
    {
        if (isCollectible && other.CompareTag(playerTag))
        {
            CollectChitin();
        }
    }
    
    // Collection logic
    private void CollectChitin()
    {
        if (ChitinCollector.Instance != null)
        {
            ChitinCollector.Instance.CollectChitin();
            
            if (debugMode)
                Debug.Log("Chitin collected!");
                
            // Destroy the chitin object
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("No ChitinCollector found in scene!");
        }
    }
    
    // Optional: Draw collection radius in editor
    private void OnDrawGizmosSelected()
    {
        if (debugMode && autoCollect)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, autoCollectRadius);
        }
    }
    
    // Optional: visualization for debugging
    private void OnDrawGizmos()
    {
        if (debugMode)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 0.2f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(transform.position.x, stickHeight, transform.position.z), 0.1f);
        }
    }
} 