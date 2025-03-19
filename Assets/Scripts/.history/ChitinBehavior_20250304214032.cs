using UnityEngine;

public class ChitinBehavior : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float minArcHeight = 1.5f; // Minimum peak height
    [SerializeField] private float stickHeight = 0.05f; // Height at which to stick
    [SerializeField] private float initialUpForce = 5f; // Force to send chitin upward
    [SerializeField] private float spinSpeed = 2f; // How fast to spin while falling
    
    [Header("Optional Effects")]
    [SerializeField] private bool sparkOnLand = false;
    [SerializeField] private GameObject sparkEffect;
    
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
    }
    
    private void Update()
    {
        // Track the highest point reached
        if (transform.position.y > highestPoint)
        {
            highestPoint = transform.position.y;
        }
        
        // Check if we've reached the minimum arc height
        if (!hasReachedPeak && highestPoint >= startPosition.y + minArcHeight)
        {
            hasReachedPeak = true;
        }
        
        // Only allow sticking after reaching peak of arc and falling to stick height
        if (hasReachedPeak && !hasStuck && transform.position.y <= stickHeight)
        {
            StickInPlace();
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
        
        // Freeze the rigidbody
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezePosition;
        }
        
        // Optional landing effect
        if (sparkOnLand && sparkEffect != null)
        {
            Instantiate(sparkEffect, transform.position, Quaternion.identity);
        }
        
        // Disable this component since we're done
        this.enabled = false;
    }
} 