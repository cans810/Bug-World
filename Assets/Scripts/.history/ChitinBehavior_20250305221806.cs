using UnityEngine;
using System;

public class ChitinBehavior : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float minArcHeight = 0.1f;  // REDUCED from 0.5f
    [SerializeField] private float stickHeight = 0.05f;  // Keep this as is
    [SerializeField] private float initialUpForce = 0f;  // ZERO - no additional upward force
    [SerializeField] private float horizontalForce = 0f; // ZERO - no additional horizontal force
    [SerializeField] private float spinSpeed = 1f;       // REDUCED from 2f
    
    // Debug options
    [SerializeField] private bool debugMode = false;
    
    // Chitin state
    public bool HasStuck { get { return hasStuck; } }
    public event Action OnChitinStuck;
    
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
        
        // Find and disable the collectible script until we stick
        ChitinCollectible collectible = GetComponent<ChitinCollectible>();
        if (collectible != null)
        {
            collectible.enabled = false;
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
            
            // Check if we already have significant velocity from ChitinDropper
            if (rb.velocity.magnitude < 0.1f)
            {
                // Only apply force if we don't already have velocity
                
                // Clear any existing velocity
                rb.velocity = Vector3.zero;
                
                // Apply strong upward force
                Vector3 upForce = Vector3.up * initialUpForce;
                
                // Apply horizontal scatter separately
                Vector3 horizontalScatter = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f), 
                    0f, 
                    UnityEngine.Random.Range(-1f, 1f)
                ).normalized * horizontalForce;
                
                // Combine forces and apply
                rb.AddForce(upForce + horizontalScatter, ForceMode.Impulse);                
                
                if (debugMode)
                    Debug.Log($"Applied launch force: {upForce + horizontalScatter}, Upward: {initialUpForce}");
            }
            else if (debugMode)
            {
                Debug.Log("Chitin already has velocity, skipping force application");
            }
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
            // Only try to set velocity if the rigidbody is not kinematic
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            rb.constraints = RigidbodyConstraints.FreezeAll;  // Freeze everything, not just position
            rb.isKinematic = true;  // Also make it kinematic for extra stability
        }
        
        if (debugMode)
            Debug.Log("Chitin stuck successfully at " + transform.position);
        
        // Enable the collectible script now that we're stuck
        ChitinCollectible collectible = GetComponent<ChitinCollectible>();
        if (collectible != null)
        {
            collectible.enabled = true;
        }
        
        // Notify listeners
        OnChitinStuck?.Invoke();
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

    // Method to force sticking immediately (used by animation)
    public void ForceStick()
    {
        StickInPlace();
    }
} 