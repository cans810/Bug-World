using UnityEngine;
using System.Collections;

public class BaseInteraction : MonoBehaviour
{
    [Header("Nest Settings")]
    [SerializeField] private string nestLayerName = "PlayerNest1";
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private AudioClip depositSound;
    
    [Header("Sound Effects")]
    [SerializeField] private string depositSoundEffectName = "LootDeposited";
    [SerializeField] private string healSoundEffectName = "PlayerHealed";
    [SerializeField] private bool useSoundEffectManager = true;
    
    [Header("Healing Settings")]
    [SerializeField] private bool healPlayerInBase = true;
    [SerializeField] private float healAmount = 5f; // Amount to heal each interval
    [SerializeField] private float healInterval = 5f; // Seconds between healing
    [SerializeField] private AudioClip healSound;
    [SerializeField] private Color healingOutlineColor = Color.green;
    
    [Header("Egg Creation")]
    [SerializeField] private AntIncubator antIncubator;
    [SerializeField] private bool autoDepositCrumbs = true;
    
    // Debug logging for healing
    [SerializeField] private float debugMessageInterval = 1f; // How often to show debug messages
    
    private PlayerInventory playerInventory;
    private LivingEntity playerEntity;
    private EntityOutline playerOutline;
    private AudioSource audioSource;
    private Color originalOutlineColor;
    private bool isHealing = false;
    private float healSoundCooldown = 0f;
    private float debugMessageTimer = 0f;
    private float totalHealedAmount = 0f; // Track healed amount for debug purposes
    private float healTimer = 0f; // Track time until next heal
    
    private void Awake()
    {
        // Get or add audio source for sounds (fallback if SoundEffectManager is not available)
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
        
        // Find incubator if not assigned
        if (antIncubator == null)
        {
            antIncubator = FindObjectOfType<AntIncubator>();
        }
    }
    
    private void Update()
    {
        // If the player is in the base and needs healing, heal them at intervals
        if (isHealing && playerEntity != null && healPlayerInBase)
        {
            // Check if player needs healing
            if (playerEntity.CurrentHealth < playerEntity.MaxHealth)
            {
                // Increment timer
                healTimer += Time.deltaTime;
                
                // Check if it's time to heal
                if (healTimer >= healInterval)
                {
                    // Apply healing
                    ApplyIntervalHealing();
                    
                    // Reset timer
                    healTimer = 0f;
                }
            }
            
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
            playerOutline = other.GetComponent<EntityOutline>();
            if (playerOutline != null)
            {
                // Store the original outline color
                originalOutlineColor = playerOutline.OutlineColor;
            }
            
            // Automatically deposit all chitin when player enters the nest
            DepositPlayerChitin();
            
            // Also deposit crumbs if auto-deposit is enabled
            if (autoDepositCrumbs)
            {
                DepositPlayerCrumbs();
            }
            
            // Start continuous healing
            if (healPlayerInBase && playerEntity != null)
            {
                StartHealing();
            }
        }
    }

    private void OnTriggerStay(Collider other){
        playerInventory = other.GetComponent<PlayerInventory>();
        
        if (playerInventory != null)
        {
            DepositPlayerChitin();
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
            
            // Play deposit sound using SoundEffectManager if available
            if (useSoundEffectManager && SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound(depositSoundEffectName, transform.position);
            }
            // Fallback to direct AudioSource if SoundEffectManager is not available
            else if (audioSource != null && depositSound != null)
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
    
    private void DepositPlayerCrumbs()
    {
        if (playerInventory == null) return;
        
        // Get current crumb count
        int currentCrumbs = playerInventory.CrumbCount;
        
        if (currentCrumbs > 0)
        {
            // Check if we have enough crumbs for egg creation
            if (antIncubator != null)
            {
                int requiredCrumbs = antIncubator.GetRequiredCrumbs();
                
                if (currentCrumbs >= requiredCrumbs)
                {
                    // We have enough crumbs, try to create an egg
                    bool eggCreated = antIncubator.ProcessCrumbDeposit(currentCrumbs);
                    
                    if (eggCreated)
                    {
                        // Egg created successfully, remove exactly the required number of crumbs
                        playerInventory.RemoveCrumb(requiredCrumbs);
                        
                        // Play sound using SoundEffectManager if available
                        if (useSoundEffectManager && SoundEffectManager.Instance != null)
                        {
                            SoundEffectManager.Instance.PlaySound(depositSoundEffectName, transform.position);
                        }
                        // Fallback to direct AudioSource
                        else if (audioSource != null && depositSound != null)
                        {
                            audioSource.PlayOneShot(depositSound);
                        }
                        
                        if (showDebugMessages)
                        {
                            Debug.Log($"Used {requiredCrumbs} crumbs to create an egg!");
                        }
                    }
                }
                else
                {
                    // Not enough crumbs, just inform the player but DON'T take their crumbs
                    antIncubator.ProcessCrumbDeposit(currentCrumbs); // This will show the "need more crumbs" message
                    
                    if (showDebugMessages)
                    {
                        Debug.Log($"Not enough crumbs ({currentCrumbs}/{requiredCrumbs}), keeping them for later.");
                    }
                }
            }
        }
    }
    
    private void StartHealing()
    {
        isHealing = true;
        healSoundCooldown = 0f;
        debugMessageTimer = 0f;
        totalHealedAmount = 0f;
        healTimer = 0f; // Reset heal timer when starting
        
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
    
    private void ApplyIntervalHealing()
    {
        // Make sure we have a valid player entity
        if (playerEntity == null) return;
        
        // Store health before healing for feedback
        float previousHealth = playerEntity.CurrentHealth;
        
        // Apply the healing
        playerEntity.Heal(healAmount);
        
        // Track total healing for debug messages
        float healedThisInterval = playerEntity.CurrentHealth - previousHealth;
        totalHealedAmount += healedThisInterval;
        
        // Play heal sound using SoundEffectManager if available and not on cooldown
        if (healSoundCooldown <= 0f)
        {
            if (useSoundEffectManager && SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound(healSoundEffectName, transform.position);
            }
            // Fallback to direct AudioSource
            else if (audioSource != null && healSound != null)
            {
                audioSource.PlayOneShot(healSound);
            }
            
            healSoundCooldown = 2f; // Set cooldown to avoid sound spam
        }
        
        // Show debug message if enabled
        if (showDebugMessages && debugMessageTimer <= 0f)
        {
            Debug.Log($"Healed player for {healedThisInterval} HP. Total healed: {totalHealedAmount}");
            debugMessageTimer = debugMessageInterval;
        }
    }
} 