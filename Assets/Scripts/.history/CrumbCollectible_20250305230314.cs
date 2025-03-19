using UnityEngine;

public class CrumbCollectible : MonoBehaviour
{
    [SerializeField] private float collectRadius = 1.5f; // Distance at which player can collect
    [SerializeField] private float lifetimeSeconds = 60f; // How long before the crumb disappears
    [SerializeField] private AudioClip pickupSound; // Sound when collected
    [SerializeField] private AudioClip fullInventorySound; // Sound when inventory is full
    
    private Transform playerTransform;
    private PlayerInventory playerInventory;
    private float spawnTime;
    private bool isCollectable = true;
    
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
    }
    
    private void Update()
    {
        // Don't process collection logic if not collectable
        if (!isCollectable)
            return;
            
        if (playerTransform == null || playerInventory == null)
            return;
            
        // Get distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // If close enough to collect
        if (distanceToPlayer <= collectRadius)
        {
            // Check if player's crumb inventory is full
            if (!playerInventory.IsCrumbFull)
            {
                // Play pickup sound if available
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
                
                // Add to player inventory and destroy if successful
                if (playerInventory.AddCrumb(1))
                {
                    Destroy(gameObject);
                }
            }
            else
            {
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
        }
    }
} 