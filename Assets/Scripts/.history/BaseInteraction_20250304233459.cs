using UnityEngine;

public class BaseInteraction : MonoBehaviour
{
    [Header("Nest Settings")]
    [SerializeField] private string nestLayerName = "PlayerNest1";
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private AudioClip depositSound;
    
    private PlayerInventory playerInventory;
    private AudioSource audioSource;
    
    private void Awake()
    {
        // Get or add audio source for deposit sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && depositSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Make sure this object is on the right layer
        if (gameObject.layer != LayerMask.NameToLayer(nestLayerName))
        {
            Debug.LogWarning($"BaseInteraction: This object should be on the {nestLayerName} layer");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Try to get the player inventory from the entering object
        playerInventory = other.GetComponent<PlayerInventory>();
        
        if (playerInventory != null)
        {
            // Automatically deposit all chitin when player enters the nest
            DepositPlayerChitin();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting object is the player with inventory
        if (other.GetComponent<PlayerInventory>() == playerInventory)
        {
            playerInventory = null;
        }
    }
    
    private void DepositPlayerChitin()
    {
        if (playerInventory == null) return;
        
        // Get current chitin count before deposit
        int currentChitin = playerInventory.ChitinCount;
        
        if (currentChitin > 0)
        {
            // Deposit all chitin at the base
            playerInventory.DepositAllChitinAtBase();
            
            // Play deposit sound if assigned
            if (audioSource != null && depositSound != null)
            {
                audioSource.PlayOneShot(depositSound);
            }
            
            // Show feedback
            if (showDebugMessages)
            {
                Debug.Log($"Deposited {currentChitin} chitin! XP gained: {currentChitin * 10}!");
            }
        }
    }
} 