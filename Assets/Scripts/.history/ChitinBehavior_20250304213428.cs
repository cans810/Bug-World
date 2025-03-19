using UnityEngine;

public class ChitinBehavior : MonoBehaviour
{
    [SerializeField] private float stickHeight = 0.5f; // Height at which to stick
    
    private Rigidbody rb;
    private bool hasStuck = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        // If we haven't stuck yet and we've fallen to or below the stick height
        if (!hasStuck && transform.position.y <= stickHeight)
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
        
        // Freeze the rigidbody position
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezePosition;
            
            // Optional: completely freeze or make kinematic
            // rb.constraints = RigidbodyConstraints.FreezeAll;
            // rb.isKinematic = true;
        }
        
        // Disable this component since we're done
        this.enabled = false;
    }
} 