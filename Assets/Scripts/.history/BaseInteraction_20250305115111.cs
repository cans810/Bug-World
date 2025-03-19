using UnityEngine;

public class BaseInteraction : MonoBehaviour
{
    [Header("Nest Settings")]
    [SerializeField] private string nestLayerName = "PlayerNest1";
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private AudioClip depositSound;
    
    [Header("Healing Settings")]
    [SerializeField] private bool healPlayerInBase = true;
    [SerializeField] private float healPercentPerSecond = 2f; // Heal 2% of max health per second
    [SerializeField] private AudioClip healSound;
    [SerializeField] private Color healingOutlineColor = Color.green;
    
    private PlayerInventory playerInventory;
    private LivingEntity playerEntity;
    private PlayerOutline playerOutline;
    private AudioSource audioSource;
    private Color originalOutlineColor;
    private bool isHealing = false;
    private float healTimer = 0f;
    private float healSoundCooldown = 0f;
    
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
    
    private void Update()
    {
        // If the player is in the base and needs healing, heal them over time
        if (isHealing && playerEntity != null && healPlayerInBase)
        {
            healTimer += Time.deltaTime;
            healSoundCooldown -= Time.deltaTime;
            
            // Apply healing every second
            if (healTimer >= 1.0f)
            {
                ApplyHealingTick();
                healTimer = 0f;
            }
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
            
            // Get the player outline component for visual feedback
            playerOutline = other.GetComponent<PlayerOutline>();
            if (playerOutline != null)
            {
                // Store the original outline color
                originalOutlineColor = playerOutline.OutlineColor;
            }
            
            // Automatically deposit all chitin when player enters the nest
            DepositPlayerChitin();
            
            // Start continuous healing
            if (healPlayerInBase && playerEntity != null)
            {
                StartHealing();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting object is the player with inventory
        if (other.GetComponent<PlayerInventory>() == playerInventory)
        {
            // Stop healing and reset outline color
            StopHealing();
            
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
    
    private void StartHealing()
    {
        isHealing = true;
        healTimer = 0f;
        healSoundCooldown = 0f;
        
        // Change outline color to healing color
        if (playerOutline != null)
        {
            playerOutline.SetOutlineColor(healingOutlineColor);
        }
    }
    
    private void StopHealing()
    {
        isHealing = false;
        
        // Restore original outline color
        if (playerOutline != null)
        {
            playerOutline.SetOutlineColor(originalOutlineColor);
        }
    }
    
    private void ApplyHealingTick()
    {
        // Make sure we have a valid player entity
        if (playerEntity == null) return;
        
        // Check if player needs healing
        if (playerEntity.CurrentHealth < playerEntity.MaxHealth)
        {
            // Calculate heal amount (2% of max health)
            float healAmount = playerEntity.MaxHealth * (healPercentPerSecond / 100f);
            
            // Store health before healing for feedback
            float previousHealth = playerEntity.CurrentHealth;
            
            // Apply the healing
            playerEntity.Heal(healAmount);
            
            // Play heal sound if assigned and not on cooldown
            if (audioSource != null && healSound != null && healSoundCooldown <= 0f)
            {
                audioSource.PlayOneShot(healSound);
                healSoundCooldown = 2f; // Set cooldown to avoid sound spam
            }
            
            // Show feedback
            if (showDebugMessages)
            {
                float healedAmount = playerEntity.CurrentHealth - previousHealth;
                Debug.Log($"Healed player for {healedAmount} health ({healPercentPerSecond}% of max health)");
            }
        }
        else if (isHealing)
        {
            // Player is at full health, stop the healing effects but stay in healing mode
            // while in the base in case they take damage later
            if (playerOutline != null && playerOutline.OutlineColor == healingOutlineColor)
            {
                // Keep green outline to show they're in healing zone, even at full health
            }
        }
    }
} 