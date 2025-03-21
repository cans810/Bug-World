using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BaseInteraction : MonoBehaviour
{
    [Header("Nest Settings")]
    [SerializeField] private string nestLayerName = "PlayerNest1";
    [SerializeField] private string nestLayerName = "PlayerNest1";
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
    [SerializeField] private InsectIncubator insectIncubator;
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

    [Header("Recovery Attribute Impact")]
    [SerializeField] private bool usePlayerRecoveryAttribute = true;
    [SerializeField] private float baseHealInterval = 5f; // Original heal interval before attribute effects
    private PlayerAttributes playerAttributes; // Reference to player attributes

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
        if (insectIncubator == null)
        {
            insectIncubator = FindObjectOfType<InsectIncubator>();
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
            
            // Get player attributes for recovery calculation
            playerAttributes = other.GetComponent<PlayerAttributes>();
            
            // Apply recovery attribute to heal interval if enabled
            if (usePlayerRecoveryAttribute && playerAttributes != null)
            {
                // Calculate modified heal interval based on recovery attribute
                float recoveryMultiplier = playerAttributes.RecoveryMultiplier;
                healInterval = baseHealInterval / recoveryMultiplier;
                
                if (showDebugMessages)
                {
                    Debug.Log($"Applied recovery attribute: Base interval={baseHealInterval}s, " +
                              $"Recovery multiplier={recoveryMultiplier}x, " +
                              $"New interval={healInterval}s");
                }
            }
            
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
            // Reset heal interval to base value when player leaves
            healInterval = baseHealInterval;
            
            // Reset player attributes reference
            playerAttributes = null;
            
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
            // Calculate XP amount - ALWAYS 2 XP per chitin
            int xpAmount = currentChitin * 2;
            
            // Store the player's current XP before adding more
            int currentXP = playerInventory.TotalExperience;
            int targetXP = currentXP + xpAmount;
            
            // Spawn visual effects for each chitin
            if (visualEffectManager != null)
            {
                visualEffectManager.SpawnEffect("chitin", playerInventory.transform.position, currentChitin);
                
                // Pass the ACTUAL XP amount to the coroutine
                StartCoroutine(PlayXPGainAfterChitinDeposit(xpAmount, currentXP, targetXP));
            }

            // Add the XP to the player's total (but don't animate the UI yet)
            playerInventory.DepositChitin(currentChitin);
            
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
                Debug.Log($"Deposited {currentChitin} chitin! XP gained: {xpAmount}!");
            }
        }
    }
    
    // Modified to prevent automatic XP counter update and only update with symbol animation
    private IEnumerator PlayXPGainAfterChitinDeposit(int xpAmount, int startXP, int targetXP)
    {
        // Wait for chitins to reach the nest (based on the animation duration)
        if (visualEffectManager != null)
        {
            // Wait for chitin flight animation to complete
            yield return new WaitForSeconds(visualEffectManager.ChitinFlyDuration);
            
            // Play XP gain effect (using this object's position as the nest)
            visualEffectManager.PlayXPGainEffectVisualOnly(transform.position, xpAmount);
            
            // Wait a bit for the XP symbols to travel
            yield return new WaitForSeconds(0.5f);
            
            // Now animate the XP counter in UI
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                // Animate the XP counter from current value to target value
                StartCoroutine(AnimateXPCounter(uiHelper, startXP, targetXP, 0.7f));
            }
        }
    }
    
    // Keep the AnimateXPCounter method but add level-up check at the end
    private IEnumerator AnimateXPCounter(UIHelper uiHelper, int startXP, int targetXP, float duration)
    {
        if (uiHelper == null) yield break;
        
        float elapsed = 0f;
        int xpDifference = targetXP - startXP;
        int currentDisplayXP = startXP;
        
        // Update the counter incrementally
        while (elapsed < duration && currentDisplayXP < targetXP)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Calculate how many XP points to add this frame
            int targetDisplayXP = startXP + Mathf.FloorToInt(xpDifference * progress);
            
            // Only update the UI if the value changed
            if (targetDisplayXP > currentDisplayXP)
            {
                // Update the display with the new value
                uiHelper.UpdateExperienceDisplay(targetDisplayXP, false);
                currentDisplayXP = targetDisplayXP;
                
                // Reduce delay between increments
                yield return new WaitForSeconds(0.01f);
            }
            
            yield return null;
        }
        
        // Ensure we reach the final value
        uiHelper.UpdateExperienceDisplay(targetXP, false);
        
        // IMPORTANT: Now that the XP animation is complete, check if the player should level up
        // Since we modified _experience directly, we need to manually check for level up
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            // Use the public CheckAndTriggerLevelUp method instead of trying to access the private methods
            playerInventory.CheckAndTriggerLevelUp();
        }
    }
    
    private void DepositPlayerCrumbs()
    {
        if (playerInventory == null) return;
        
        int currentCrumbs = playerInventory.CrumbCount;
        
        if (currentCrumbs > 0)
        {
            // Always spawn visual effects for the crumbs being deposited
            if (visualEffectManager != null)
            {
                // Start the coroutine properly instead of calling the method directly
                StartCoroutine(visualEffectManager.SpawnCrumbCollectEffect(currentCrumbs));
            }
            
            // Always remove crumbs from player inventory
            playerInventory.RemoveCrumb(currentCrumbs);
            
            // Check if we have a discovered entity controller with an equipped entity
            DiscoveredEntitiesController entityController = FindObjectOfType<DiscoveredEntitiesController>();
            bool hasEquippedEntity = entityController != null && !string.IsNullOrEmpty(entityController.GetEquippedEntity());
            
            // If an entity is equipped, update its crumb counter after animation
            if (hasEquippedEntity && entityController != null)
            {
                StartCoroutine(UpdateEquippedEntityAfterAnimation(entityController, currentCrumbs));
            }
            
            // Always play deposit sound
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
                string message = hasEquippedEntity ? 
                    $"Deposited {currentCrumbs} crumbs for equipped entity." : 
                    $"Deposited {currentCrumbs} crumbs.";
                Debug.Log(message);
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
                    // Use the correct parameter count for PlaySound
                    AudioSource crumbSound = SoundEffectManager.Instance.PlaySound("CrumbCounter", transform.position, true);
                    if (crumbSound != null)
                    {
                        // Set the volume to be quieter
                        crumbSound.volume = 0.2f;
                    }
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