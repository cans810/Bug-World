using UnityEngine;

public class ChitinBehavior : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float minArcHeight = 1.0f;  // Lowered to ensure it reaches peak
    [SerializeField] private float stickHeight = 0.05f;  // Height at which to stick
    [SerializeField] private float initialUpForce = 5f;  // Force to send chitin upward
    [SerializeField] private float spinSpeed = 2f;       // How fast to spin while falling
    
    // Debug options
    [SerializeField] private bool debugMode = false;
    
    private Rigidbody rb;
    private bool hasReachedPeak = false;
    private bool hasStuck = false;
    private Vector3 startPosition;
    private float highestPoint;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        highestPoint = startPosition.y;
    }
    
    private void Start()
    {
        // Ensure we start with an upward trajectory
        if (rb != null)
        {
            // Override any existing velocity with a guaranteed upward force
            rb.velocity = Vector3.up * initialUpForce + Random.insideUnitSphere * 2f;
            
            // Add random rotation
            rb.angularVelocity = new Vector3(
                Random.Range(-spinSpeed, spinSpeed),
                Random.Range(-spinSpeed, spinSpeed),
                Random.Range(-spinSpeed, spinSpeed)
            );
        }
        
        // Force a delayed check in case regular updates miss the exact height
        Invoke("ForcedHeightCheck", 2.0f);
    }
    
    // Use FixedUpdate for physics-based checks
    private void FixedUpdate()
    {
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
        
        // Check for sticking
        CheckForSticking();
    }
    
    private void CheckForSticking()
    {
        // Exit if already stuck
        if (hasStuck) return;
        
        // Only allow sticking after reaching peak of arc and falling to stick height
        if (hasReachedPeak && transform.position.y <= stickHeight)
        {
            if (debugMode)
                Debug.Log($"Sticking at height: {transform.position.y}, target: {stickHeight}");
            
            StickInPlace();
        }
        
        // Secondary check: cast ray downward to see if we're close to ground
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 0.2f))
        {
            if (debugMode)
                Debug.Log($"Ground detected below at distance: {hit.distance}");
            
            StickInPlace();
        }
    }
    
    // Fallback method in case we miss the exact height during normal updates
    private void ForcedHeightCheck()
    {
        if (!hasStuck)
        {
            if (debugMode)
                Debug.Log("Forced height check triggered");
            
            // Force peak detection if it's been this long
            hasReachedPeak = true;
            
            // If we're below the stick height, stick now
            if (transform.position.y <= stickHeight)
            {
                StickInPlace();
            }
        }
    }
    
    private void StickInPlace()
    {
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