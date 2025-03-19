using UnityEngine;

public class BaseInteraction : MonoBehaviour
{
    [Header("Nest Settings")]
    [SerializeField] private string nestLayerName = "PlayerNest1";
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private AudioClip depositSound;
    
    [Header("Healing Settings")]
    [SerializeField] private bool healPlayerInBase = true;
    [SerializeField] private float healAmount = 100f; // Full heal by default
    [SerializeField] private AudioClip healSound;
    
    private PlayerInventory playerInventory;
    private LivingEntity playerEntity;
    private AudioSource audioSource;
    
    private void Awake()
    {
        // Get or add audio source for sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (depositSound != null || healSound != null))
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
            // Also try to get the LivingEntity component for healing
            playerEntity = other.GetComponent<LivingEntity>();
            
            // Automatically deposit all chitin when player enters the nest
            DepositPlayerChitin();
            
            // Heal the player if enabled
            if (healPlayerInBase && playerEntity != null)
            {
                HealPlayer();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting object is the player with inventory
        if (other.GetComponent<PlayerInventory>() == playerInventory)
        {
            playerInventory = null;
            playerEntity = null;
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
    
    private void HealPlayer()
    {
        // Make sure we have a valid player entity
        if (playerEntity == null) return;
        
        // Check if player needs healing
        if (playerEntity.CurrentHealth < playerEntity.MaxHealth)
        {
            // Heal the player
            float previousHealth = playerEntity.CurrentHealth;
            playerEntity.Heal(healAmount);
            
            // Play heal sound if assigned
            if (audioSource != null && healSound != null)
            {
                audioSource.PlayOneShot(healSound);
            }
            
            // Show feedback
            if (showDebugMessages)
            {
                float healedAmount = playerEntity.CurrentHealth - previousHealth;
                Debug.Log($"Healed player for {healedAmount} health!");
            }
        }
    }
} 