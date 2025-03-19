using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BaseInteraction : MonoBehaviour
{
    [Header("Nest Settings")]
    [SerializeField] private string nestLayerName = "PlayerNest1";
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private AudioClip depositSound;
    
    [Header("Sound Effects")]
    [SerializeField] private string depositSoundEffectName = "Pickup1";
    [SerializeField] private string healSoundEffectName = "PlayerHealed";
    [SerializeField] private bool useSoundEffectManager = true;
    
    [Header("Healing Settings")]
    [SerializeField] private bool healPlayerInBase = true;
    [SerializeField] private bool healAlliesInBase = true; // New setting for allies
    [SerializeField] private float healAmount = 5f; // Amount to heal each interval
    [SerializeField] private float healInterval = 5f; // Seconds between healing
    [SerializeField] private AudioClip healSound;
    [SerializeField] private Color healingOutlineColor = Color.green;
    
    [Header("Egg Creation")]
    [SerializeField] private AntIncubator antIncubator;
    [SerializeField] private bool autoDepositCrumbs = true;
    
    // Debug logging for healing
    [SerializeField] private float debugMessageInterval = 1f; // How often to show debug messages

    [Header("UI")]
    [SerializeField] private Image CircleImage;
    [SerializeField] private Color baseCircleColor = Color.white;
    
    // Track player
    private PlayerInventory playerInventory;
    private LivingEntity playerEntity;
    private EntityOutline playerOutline;
    
    // Track allies
    private List<LivingEntity> alliesInBase = new List<LivingEntity>();
    private Dictionary<LivingEntity, EntityOutline> allyOutlines = new Dictionary<LivingEntity, EntityOutline>();
    private Dictionary<LivingEntity, Color> originalAllyColors = new Dictionary<LivingEntity, Color>();
    
    private AudioSource audioSource;
    private Color originalOutlineColor;
    private bool isHealing = false;
    private float healSoundCooldown = 0f;
    private float debugMessageTimer = 0f;
    private float totalHealedAmount = 0f; // Track healed amount for debug purposes
    private float healTimer = 0f; // Track time until next heal
    
    [Header("Visual Effects")]
    [SerializeField] private VisualEffectManager visualEffectManager;
    [SerializeField] private float effectSpawnInterval = 0.5f;
    private float effectTimer = 0f;
    
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

        // Find visual effect manager if not assigned
        if (visualEffectManager == null)
        {
            visualEffectManager = FindObjectOfType<VisualEffectManager>();
        }
    }
    
    private void Start()
    {
        // Check if the sound effect exists in the manager
        if (useSoundEffectManager && SoundEffectManager.Instance != null)
        {
            bool hasSound = SoundEffectManager.Instance.HasSoundEffect(depositSoundEffectName);
            Debug.Log($"Sound effect '{depositSoundEffectName}' exists: {hasSound}");
        }
    }
    
    private void Update()
    {
        // If healing is active, heal entities at intervals
        if (isHealing)
        {
            // Increment timer
            healTimer += Time.deltaTime;
            
            // Check if it's time to heal
            if (healTimer >= healInterval)
            {
                // Heal player if needed
                if (healPlayerInBase && playerEntity != null && playerEntity.CurrentHealth < playerEntity.MaxHealth)
                {
                    ApplyIntervalHealing(playerEntity);
                }
                
                // Heal allies if needed
                if (healAlliesInBase && alliesInBase.Count > 0)
                {
                    foreach (LivingEntity ally in alliesInBase)
                    {
                        if (ally != null && !ally.IsDead && ally.CurrentHealth < ally.MaxHealth)
                        {
                            ApplyIntervalHealing(ally);
                        }
                    }
                }
                
                // Reset timer
                healTimer = 0f;
            }
            
            // Decrement cooldowns
            healSoundCooldown -= Time.deltaTime;
            debugMessageTimer -= Time.deltaTime;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Debug all entities entering the base
        if (showDebugMessages)
        {
            Debug.Log($"Entity entered base: {other.gameObject.name}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        }
        
        // Check if this is the player
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            // This is the player
            playerInventory = inventory;
            playerEntity = other.GetComponent<LivingEntity>();
            
            // Change circle color to green when player enters
            if (CircleImage != null)
            {
                CircleImage.color = healingOutlineColor;
            }
            
            // Get the player outline component for visual feedback
            playerOutline = other.GetComponent<EntityOutline>();
            if (playerOutline != null)
            {
                // Store the original outline color
                originalOutlineColor = playerOutline.OutlineColor;
                
                // Set healing outline color with improved approach
                Debug.Log($"Setting player outline color to healing color: {healingOutlineColor}");
                playerOutline.SetOutlineColor(healingOutlineColor);
                
                // Force an immediate update of the outline
                StartCoroutine(ForceOutlineUpdateDelayed(playerOutline));
            }
            else
            {
                Debug.LogWarning("Player entered base but has no EntityOutline component");
            }
            
            // Automatically deposit all chitin when player enters the nest
            DepositPlayerChitin();
            
            // Also deposit crumbs if auto-deposit is enabled
            if (autoDepositCrumbs)
            {
                DepositPlayerCrumbs();
            }
        }
        else
        {
            // Check if this is an ally
            LivingEntity entity = other.GetComponent<LivingEntity>();
            if (entity != null)
            {
                bool isAlly = IsAlly(entity);
                if (showDebugMessages)
                {
                    Debug.Log($"Entity {other.gameObject.name} has LivingEntity component. IsAlly: {isAlly}");
                }
                
                if (isAlly)
                {
                    // Add to allies list
                    if (!alliesInBase.Contains(entity))
                    {
                        alliesInBase.Add(entity);
                        
                        // Get outline component if available
                        EntityOutline outline = other.GetComponent<EntityOutline>();
                        if (outline != null)
                        {
                            allyOutlines[entity] = outline;
                            originalAllyColors[entity] = outline.OutlineColor;
                            outline.SetOutlineColor(healingOutlineColor);
                            
                            Debug.Log($"Set ally {entity.gameObject.name} outline color to {healingOutlineColor}");
                        }
                        else
                        {
                            Debug.LogWarning($"Ally {entity.gameObject.name} does not have an EntityOutline component");
                        }
                        
                        if (showDebugMessages)
                        {
                            Debug.Log($"Ally {entity.gameObject.name} entered the base.");
                        }
                    }
                }
            }
        }
        
        // Start healing if we have entities to heal
        if ((playerEntity != null || alliesInBase.Count > 0) && !isHealing)
        {
            StartHealing();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if this is the player leaving
        PlayerInventory exitingInventory = other.GetComponent<PlayerInventory>();
        if (exitingInventory != null && exitingInventory == playerInventory)
        {
            // Reset circle color to white when player exits
            if (CircleImage != null)
            {
                CircleImage.color = baseCircleColor;
            }
            
            // Reset player outline
            if (playerOutline != null)
            {
                Debug.Log($"Resetting player outline color from {playerOutline.OutlineColor} to {originalOutlineColor}");
                playerOutline.SetOutlineColor(originalOutlineColor);
            }
            
            // Clear player references
            playerInventory = null;
            playerEntity = null;
            playerOutline = null;
            
            if (showDebugMessages)
            {
                Debug.Log("Player left the base.");
            }
        }
        else
        {
            // Check if this is an ally leaving
            LivingEntity entity = other.GetComponent<LivingEntity>();
            if (entity != null && alliesInBase.Contains(entity))
            {
                // Reset ally outline
                if (allyOutlines.ContainsKey(entity) && originalAllyColors.ContainsKey(entity))
                {
                    EntityOutline outline = allyOutlines[entity];
                    Color originalColor = originalAllyColors[entity];
                    
                    if (outline != null)
                    {
                        Debug.Log($"Resetting ally outline color from {outline.OutlineColor} to {originalColor}");
                        outline.SetOutlineColor(originalColor);
                    }
                    
                    allyOutlines.Remove(entity);
                    originalAllyColors.Remove(entity);
                }
                
                // Remove from allies list
                alliesInBase.Remove(entity);
                
                if (showDebugMessages)
                {
                    Debug.Log($"Ally {entity.gameObject.name} left the base.");
                }
            }
        }
        
        // Stop healing if no entities remain in the base
        if (playerEntity == null && alliesInBase.Count == 0 && isHealing)
        {
            StopHealing();
        }
    }
    
    // Helper method to check if an entity is an ally
    private bool IsAlly(LivingEntity entity)
    {
        // Check if the entity is on the "Ally" layer
        return entity.gameObject.layer == LayerMask.NameToLayer("Ally");
    }
    
    private void DepositPlayerChitin()
    {
        if (playerInventory == null) return;
        
        // Get current chitin count before deposit
        int currentChitin = playerInventory.ChitinCount;
        
        if (currentChitin > 0)
        {
            // Spawn visual effects for each chitin
            if (visualEffectManager != null)
            {
                visualEffectManager.SpawnEffect("chitin", playerInventory.transform.position, currentChitin);
            }

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
                Debug.Log($"Deposited {currentChitin} chitin! XP gained: {currentChitin * 5}!");
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
        
        // Change player outline color to healing color with additional checks
        if (playerEntity != null && playerOutline != null)
        {
            Debug.Log($"Setting player outline color to healing color: {healingOutlineColor}");
            playerOutline.SetOutlineColor(healingOutlineColor);
            
            // Force an immediate update of the outline with the improved approach
            StartCoroutine(ForceOutlineUpdateDelayed(playerOutline));
        }
        
        // Change ally outline colors to healing color
        foreach (var entity in alliesInBase)
        {
            if (entity != null && allyOutlines.ContainsKey(entity))
            {
                EntityOutline outline = allyOutlines[entity];
                if (outline != null)
                {
                    Debug.Log($"Setting ally {entity.gameObject.name} outline color to healing color: {healingOutlineColor}");
                    outline.SetOutlineColor(healingOutlineColor);
                    
                    // Force an immediate update of the outline
                    StartCoroutine(ForceOutlineUpdateDelayed(outline));
                }
            }
        }
        
        if (showDebugMessages)
        {
            Debug.Log("Base healing started.");
        }
    }
    
    // Add a new coroutine to force outline update with a slight delay
    private IEnumerator ForceOutlineUpdateDelayed(EntityOutline outline)
    {
        // Wait for end of frame
        yield return new WaitForEndOfFrame();
        
        // Force outline to update
        if (outline != null)
        {
            outline.ForceUpdate();
            
            // Add a second update after a short delay for reliability
            yield return new WaitForSeconds(0.2f);
            outline.ForceUpdate();
        }
    }
    
    private void StopHealing()
    {
        isHealing = false;
        
        if (showDebugMessages)
        {
            Debug.Log("Base healing stopped.");
        }
    }
    
    private void ApplyIntervalHealing(LivingEntity entity)
    {
        if (entity == null) return;
        
        float previousHealth = entity.CurrentHealth;
        entity.Heal(healAmount);
        float healedThisInterval = entity.CurrentHealth - previousHealth;

        // Only show effects if actual healing occurred
        if (healedThisInterval > 0 && healingEffectManager != null)
        {
            // Spawn healing effect at the entity's position
            healingEffectManager.SpawnHealingEffect(entity.transform.position);
        }

        // Track total healing for debug messages
        totalHealedAmount += healedThisInterval;
        
        // Only play sound and show effects if actual healing occurred
        if (healedThisInterval > 0)
        {
            // Play heal sound using SoundEffectManager if available and not on cooldown
            if (healSoundCooldown <= 0f)
            {
                if (useSoundEffectManager && SoundEffectManager.Instance != null)
                {
                    SoundEffectManager.Instance.PlaySound(healSoundEffectName, entity.transform.position);
                }
                // Fallback to direct AudioSource
                else if (audioSource != null && healSound != null)
                {
                    AudioSource.PlayClipAtPoint(healSound, entity.transform.position);
                }
                
                healSoundCooldown = 2f; // Set cooldown to avoid sound spam
            }
            
            // Show debug message if enabled
            if (showDebugMessages && debugMessageTimer <= 0f)
            {
                string entityType = entity == playerEntity ? "player" : "ally";
                Debug.Log($"Healed {entityType} for {healedThisInterval} HP. Total healed: {totalHealedAmount}");
                debugMessageTimer = debugMessageInterval;
            }
        }
    }
} 