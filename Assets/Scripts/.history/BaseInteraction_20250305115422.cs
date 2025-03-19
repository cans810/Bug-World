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
    
    // Debug logging for healing
    [SerializeField] private float debugMessageInterval = 1f; // How often to show debug messages
    
    private PlayerInventory playerInventory;
    private LivingEntity playerEntity;
    private PlayerOutline playerOutline;
    private AudioSource audioSource;
    private Color originalOutlineColor;
    private bool isHealing = false;
    private float healSoundCooldown = 0f;
    private float debugMessageTimer = 0f;
    private float totalHealedAmount = 0f; // Track healed amount for debug purposes
    
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
        // If the player is in the base and needs healing, heal them continuously
        if (isHealing && playerEntity != null && healPlayerInBase)
        {
            // Apply healing every frame for smooth regeneration
            ApplySmoothHealing();
            
            // Decrement cooldowns
            healSoundCooldown -= Time.deltaTime;
            debugMessageTimer -= Time.deltaTime;
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
        healSoundCooldown = 0f;
        debugMessageTimer = 0f;
        totalHealedAmount = 0f;
        
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
    
    private void ApplySmoothHealing()
    {
        // Make sure we have a valid player entity
        if (playerEntity == null) return;
        
        // Check if player needs healing
        if (playerEntity.CurrentHealth < playerEntity.MaxHealth)
        {
            // Calculate heal amount per frame (2% of max health per second * deltaTime)
            float healAmountThisFrame = playerEntity.MaxHealth * (healPercentPerSecond / 100f) * Time.deltaTime;
            
            // Store health before healing for feedback
            float previousHealth = playerEntity.CurrentHealth;
            
            // Apply the healing
            playerEntity.Heal(healAmountThisFrame);
            
            // Track total healing for debug messages
            float healedThisFrame = playerEntity.CurrentHealth - previousHealth;
            totalHealedAmount += healedThisFrame;
            
            // Play heal sound if not on cooldown
            if (audioSource != null && healSound != null && healSoundCooldown <= 0f)
            {
                audioSource.PlayOneShot(healSound);
                healSoundCooldown = 2f; // Set cooldown to avoid sound spam
            }
            
        }
        else if (isHealing)
        {
            // Player is at full health, keep green outline to show they're in healing zone
            if (playerOutline != null && playerOutline.OutlineColor == healingOutlineColor)
            {
                // Keep the green outline
            }
        }
    }
} 