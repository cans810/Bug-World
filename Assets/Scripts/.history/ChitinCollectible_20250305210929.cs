using UnityEngine;

public class ChitinCollectible : MonoBehaviour
{
    [SerializeField] private float collectRadius = 1.5f; // Distance at which player can collect
    [SerializeField] private float attractionRadius = 3f; // Distance at which chitin gets pulled toward player
    [SerializeField] private float attractionSpeed = 5f; // How fast chitin moves toward player
    [SerializeField] private float lifetimeSeconds = 60f; // How long before the chitin disappears
    [SerializeField] private AudioClip pickupSound; // Sound when collected
    
    private Transform playerTransform;
    private PlayerInventory playerInventory;
    private Rigidbody rb;
    private float spawnTime;
    
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
        
        // Destroy after lifetime
        Destroy(gameObject, lifetimeSeconds);
    }
    
    private void Update()
    {
        if (playerTransform == null || playerInventory == null)
            return;
            
        // Get distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // If close enough to collect
        if (distanceToPlayer <= collectRadius)
        {
            // Play sound if available
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            
            // Add to player inventory
            playerInventory.AddChitin(1);
            
            // Destroy this collectible
            Destroy(gameObject);
        }
        // If within attraction radius but not collection radius
        else if (distanceToPlayer <= attractionRadius)
        {
            // Move toward player
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            
            if (rb != null)
            {
                // If we have a rigidbody, use it for movement (more physically accurate)
                rb.velocity = directionToPlayer * attractionSpeed;
            }
            else
            {
                // Otherwise use basic transform movement
                transform.position += directionToPlayer * attractionSpeed * Time.deltaTime;
            }
        }
    }
    
    // Optional: Make the chitin glow or pulse to be more visible
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collectRadius);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attractionRadius);
    }
} 