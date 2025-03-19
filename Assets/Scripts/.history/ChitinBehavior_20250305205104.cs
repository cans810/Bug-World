using UnityEngine;

public class ChitinBehavior : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float minArcHeight = 0.5f;  // Reduced peak height
    [SerializeField] private float stickHeight = 0.05f;  // Height at which to stick
    [SerializeField] private float initialUpForce = 3f;  // Reduced upward force
    [SerializeField] private float horizontalForce = 0.8f; // Reduced horizontal scatter
    [SerializeField] private float spinSpeed = 2f;       // How fast to spin while falling
    
    // Debug options
    [SerializeField] private bool debugMode = false;
    
    private Rigidbody rb;
    private bool hasReachedPeak = false;
    private bool hasStuck = false;
    private Vector3 startPosition;
    private float highestPoint;
    private bool hasAppliedForce = false;
    
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
        // Apply forces immediately in Start
        ApplyInitialForce();
        
        // Check position after a delay
        Invoke("ForcedHeightCheck", 1.0f);
    }
    
    private void ApplyInitialForce()
    {
        if (rb != null && !hasAppliedForce)
        {
            hasAppliedForce = true;
            
            // Clear existing velocity that might have been set by ChitinDropper
            rb.velocity = Vector3.zero;
            
            // Apply gentler upward force
            Vector3 upForce = Vector3.up * initialUpForce;
            
            // Apply minimal horizontal scatter
            Vector3 horizontalScatter = new Vector3(
                Random.Range(-1f, 1f), 
                0f, 
                Random.Range(-1f, 1f)
            ).normalized * horizontalForce;
            
            // Combine forces and apply
            rb.AddForce(upForce + horizontalScatter, ForceMode.Impulse);
            
            // Add some rotation
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
            rb.constraints = RigidbodyConstraints.FreezeAll;  // Freeze everything, not just position
            rb.isKinematic = true;  // Also make it kinematic for extra stability
        }
        
        if (debugMode)
            Debug.Log("Chitin stuck successfully at " + transform.position);
        
        // Disable this component since we're done
        this.enabled = false;
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