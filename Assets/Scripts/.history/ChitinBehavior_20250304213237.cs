using UnityEngine;

public class ChitinBehavior : MonoBehaviour
{
    [SerializeField] private float stickDelay = 0.5f; // Time to wait after collision before sticking
    
    private Rigidbody rb;
    private bool hasLanded = false;
    private float landingTime = 0f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Check if this is a ground collision
        if (collision.gameObject.CompareTag("Ground") || 
            collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (!hasLanded)
            {
                hasLanded = true;
                landingTime = Time.time;
            }
        }
    }
    
    private void Update()
    {
        // Once landed and delay passed, stick to ground
        if (hasLanded && Time.time > landingTime + stickDelay && rb != null)
        {
            // Freeze position but allow rotation (optional)
            rb.constraints = RigidbodyConstraints.FreezePosition;
            
            // Alternative: Completely freeze everything
            // rb.constraints = RigidbodyConstraints.FreezeAll;
            
            // Or completely disable physics
            // rb.isKinematic = true;
            
            // Once stuck, no need to keep checking
            this.enabled = false;
        }
    }
} 