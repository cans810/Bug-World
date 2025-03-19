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
    [SerializeField] private SpriteRenderer CircleImage;
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
    
    [Header("Chitin Effects")]
    [SerializeField] private Transform nestTransform;
    [SerializeField] private GameObject actualNestGameObject;
    
    // Event for when chitin is deposited
    public event System.Action<int> OnChitinDeposited;

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
                
                // Calculate XP amount (from the debug message, it appears to be currentChitin * 5)
                int xpAmount = currentChitin * 2;
                
                // Add XP gain visual effect once chitins are deposited at the nest
                StartCoroutine(PlayXPGainAfterChitinDeposit(xpAmount));
            }

            // Deposit all chitin at the base
            playerInventory.DepositAllChitinAtBase();
            
            // Trigger the chitin deposited event
            OnChitinDeposited?.Invoke(currentChitin);
            
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
                Debug.Log($"Deposited {currentChitin} chitin! XP gained: {currentChitin * 2}!");
            }
        }
    }
    
    // New coroutine to play XP gain effect after chitin deposit animation completes
    private IEnumerator PlayXPGainAfterChitinDeposit(int xpAmount)
    {
        // Wait for chitins to reach the nest (based on the animation duration)
        if (visualEffectManager != null)
        {
            // Wait for chitin flight animation to complete
            yield return new WaitForSeconds(visualEffectManager.ChitinFlyDuration);
            
            // Play XP gain effect (using this object's position as the nest)
            visualEffectManager.PlayXPGainEffect(transform.position, xpAmount);
        }
    }
    
    private void DepositPlayerCrumbs()
    {
        if (playerInventory == null) return;
        
        int currentCrumbs = playerInventory.CrumbCount;
        
        if (currentCrumbs > 0)
        {
            // First check if we have a discovered entity controller with an equipped entity
            DiscoveredEntitiesController entityController = FindObjectOfType<DiscoveredEntitiesController>();
            bool hasEquippedEntity = entityController != null && !string.IsNullOrEmpty(entityController.GetEquippedEntity());
            
            // Spawn visual effects for the crumbs being deposited
            if (visualEffectManager != null)
            {
                // Add a flag to indicate there's an equipped entity that should receive crumbs
                visualEffectManager.SpawnCrumbCollectEffect(currentCrumbs);
            }

            if (antIncubator != null && !hasEquippedEntity)
            {
                // If no entity is equipped, use crumbs for egg creation as before
                int requiredCrumbs = antIncubator.GetRequiredCrumbs();
                
                if (currentCrumbs >= requiredCrumbs)
                {
                    bool eggCreated = antIncubator.ProcessCrumbDeposit(currentCrumbs);
                    
                    if (eggCreated)
                    {
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
                    // Not enough crumbs for egg, but show the message
                    antIncubator.ProcessCrumbDeposit(currentCrumbs);
                    
                    if (showDebugMessages)
                    {
                        Debug.Log($"Not enough crumbs ({currentCrumbs}/{requiredCrumbs}) for egg creation.");
                    }
                }
            }
            else if (hasEquippedEntity)
            {
                // If entity is equipped, remove crumbs from inventory
                playerInventory.RemoveCrumb(currentCrumbs);
                
                // Notify the entity controller to update crumb count
                StartCoroutine(UpdateEquippedEntityAfterAnimation(entityController, currentCrumbs));
                
                // Play deposit sound
                if (useSoundEffectManager && SoundEffectManager.Instance != null)
                {
                    SoundEffectManager.Instance.PlaySound(depositSoundEffectName, transform.position);
                }
                else if (audioSource != null && depositSound != null)
                {
                    audioSource.PlayOneShot(depositSound);
                }
                
                if (showDebugMessages)
                {
                    Debug.Log($"Deposited {currentCrumbs} crumbs for equipped entity.");
                }
            }
        }
    }
    
    // Update the UpdateEquippedEntityAfterAnimation method to include a gradual fill effect
    private IEnumerator UpdateEquippedEntityAfterAnimation(DiscoveredEntitiesController controller, int crumbCount)
    {
        // Wait for the animation to complete (crumbs flying to nest)
        yield return new WaitForSeconds(visualEffectManager != null ? visualEffectManager.ChitinFlyDuration : 1.5f);
        
        // Wait a bit more for the secondary animation (nest to counter)
        yield return new WaitForSeconds(0.5f);
        
        // Now gradually increment the crumb counter instead of setting it all at once
        // Get the current crumb counter
        SelectedEntityCrumbCollectionCounter crumbCounter = controller.GetCrumbCounter();
        
        if (crumbCounter != null)
        {
            // Increment crumbs one by one with a short delay
            for (int i = 0; i < crumbCount; i++)
            {
                crumbCounter.IncrementCrumbCount();
                
                // Play a small sound or visual effect for each increment
                if (SoundEffectManager.Instance != null)
                {
                    SoundEffectManager.Instance.PlaySound("CrumbCounter", transform.position, true);
                    SoundEffectManager.Instance.PlaySound("CrumbCounter", transform.position, true, 0.2f);
                }
                
                // Wait a short delay between each increment
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            // Fallback to the original method if we can't get the counter
            controller.UpdateEquippedEntityCrumbCount(crumbCount);
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
        if (healedThisInterval > 0)
        {
            if (visualEffectManager != null)
            {
                visualEffectManager.SpawnEffect("heal", entity.transform.position);
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
} 