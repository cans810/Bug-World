using UnityEngine;

public class ChitinCollectible : MonoBehaviour
{
    [SerializeField] private float collectRadius = 1.5f; // Distance at which player can collect
    [SerializeField] private float lifetimeSeconds = 60f; // How long before the chitin disappears
    [SerializeField] private AudioClip pickupSound; // Sound when collected
    [SerializeField] private AudioClip fullInventorySound; // Sound when inventory is full
    
    private Transform playerTransform;
    private PlayerInventory playerInventory;
    private Rigidbody rb;
    private float spawnTime;
    private ChitinBehavior chitinBehavior;
    private bool isCollectable = false;
    
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
    }
    
    // Called when the chitin sticks to the ground
    private void OnChitinStuck()
    {
        isCollectable = true;
    }
    
    private void Update()
    {
        // Don't process collection logic if not collectable yet
        if (!isCollectable)
            return;
            
        if (playerTransform == null || playerInventory == null)
            return;
            
        // Get distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // If close enough to collect
        if (distanceToPlayer <= collectRadius)
        {
            // Check if player has room in inventory
            if (!playerInventory.IsChitinFull)
            {
                // Play pickup sound if available
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
                
                // Add to player inventory and destroy if successful
                if (playerInventory.AddChitin(1))
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                // Player inventory is full - play different sound and/or show message
                if (fullInventorySound != null)
                {
                    AudioSource.PlayClipAtPoint(fullInventorySound, transform.position);
                }
                
                // Optional: Add some visual feedback like a UI message
                // For example, could trigger a UI manager to show "Inventory Full" text
            }
        }
    }
} 