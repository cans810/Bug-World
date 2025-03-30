using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BaseInteraction : MonoBehaviour
{
    [Header("Nest Settings")]
    [SerializeField] private string nestLayerName1 = "PlayerNest1";
    [SerializeField] private string nestLayerName2 = "PlayerNest2";
    [SerializeField] private string nestLayerName3 = "PlayerNest3";
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
    
    // Event for when chitin is deposited
    public event System.Action<int> OnChitinDeposited;

    [Header("Recovery Attribute Impact")]
    [SerializeField] private bool usePlayerRecoveryAttribute = true;
    [SerializeField] private float baseHealInterval = 5f; // Original heal interval before attribute effects
    private PlayerAttributes playerAttributes; // Reference to player attributes

    // Add this field to track if nest is purchasable
    [SerializeField] private bool isPurchasable = false;
    [SerializeField] private bool isPurchased = true; // Default to true for Nest1, false for others

    // Add this field to track ongoing chitin deposit animations
    private bool isChitinDepositInProgress = false;

    // At the top of the class, add this constant to replace the ChitinFlyDuration reference
    [SerializeField] private float chitinFlyDuration = 1.5f; // Default duration for chitin animations

    private void Awake()
    {
        // Get or add audio source for sounds (fallback if SoundEffectManager is not available)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (depositSound != null || healSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Check if this object is on one of the nest layers
        int layer = gameObject.layer;
        string layerName = LayerMask.LayerToName(layer);
        bool isOnNestLayer = layerName == nestLayerName1 || 
                             layerName == nestLayerName2 || 
                             layerName == nestLayerName3;
        
        if (!isOnNestLayer)
        {
            Debug.LogWarning($"BaseInteraction: This object should be on one of the nest layers: {nestLayerName1}, {nestLayerName2}, or {nestLayerName3}");
        }
        
        // Disable functionality for Nest2 and Nest3 by default
        if (layerName == nestLayerName2 || layerName == nestLayerName3)
        {
            // Start disabled - will be enabled when purchased
            enabled = false;
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

        // Check if this is a purchasable nest (Nest2 or Nest3)
        isPurchasable = (layerName == nestLayerName2 || layerName == nestLayerName3);
        
        // Default to purchased for Nest1, not purchased for Nest2 and Nest3
        isPurchased = !isPurchasable;
        
        // Set initial visual state
        UpdateNestVisuals();
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
        
        // If this nest is purchasable but not purchased, don't process interactions
        if (isPurchasable && !isPurchased)
        {
            // Show message to player if they enter an unpurchased nest
            PlayerInventory playerInventory = other.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                UIHelper uiHelper = FindObjectOfType<UIHelper>();
                if (uiHelper != null)
                {
                    string nestType = LayerMask.LayerToName(gameObject.layer);
                    uiHelper.ShowInformText($"This nest needs to be purchased first!");
                }
                Debug.Log("Player entered unpurchased nest - interactions disabled");
            }
            return;
        }
        
        // Check if this is the player
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            // This is the player
            playerInventory = inventory;
            playerEntity = other.GetComponent<LivingEntity>();
            
            // Change circle image color to indicate active base
            if (CircleImage != null)
            {
                CircleImage.color = healingOutlineColor;
                Debug.Log($"Changed base circle color to: {healingOutlineColor}");
            }
            
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
            
            // Get outline component
            playerOutline = other.GetComponent<EntityOutline>();
            
            // Store original outline color
            if (playerOutline != null)
            {
                originalOutlineColor = playerOutline.CurrentOutlineColor;
            }
            
            // Auto-deposit chitin
            DepositPlayerChitin();
            
            // Auto-deposit crumbs if enabled
            if (autoDepositCrumbs)
            {
                DepositPlayerCrumbs();
            }
            
            // Start healing if player is injured
            if (healPlayerInBase && playerEntity != null && playerEntity.CurrentHealth < playerEntity.MaxHealth)
            {
                StartHealing();
            }
            
            if (showDebugMessages)
            {
                Debug.Log("Player entered the base.");
            }
            
            // Hide the arrow when player reaches the nest
            ChitinDepositArrowManager arrowManager = FindObjectOfType<ChitinDepositArrowManager>();
            if (arrowManager != null)
            {
                arrowManager.HideArrow();
            }
        }
        
        // Check if this is an ally
        AllyAI allyAI = other.GetComponent<AllyAI>();
        if (allyAI != null)
        {
            // This is an ally
            LivingEntity allyEntity = other.GetComponent<LivingEntity>();
            if (allyEntity != null && !allyEntity.IsDead && !alliesInBase.Contains(allyEntity))
            {
                // Add ally to list
                alliesInBase.Add(allyEntity);
                
                // Get outline component
                EntityOutline allyOutline = other.GetComponent<EntityOutline>();
                if (allyOutline != null)
                {
                    // Store reference to ally outline
                    allyOutlines[allyEntity] = allyOutline;
                    
                    // Store original ally outline color
                    originalAllyColors[allyEntity] = allyOutline.CurrentOutlineColor;
                    
                    // Change outline color if healing
                    if (isHealing)
                    {
                        allyOutline.SetOutlineColor(healingOutlineColor);
                    }
                }
                
                // Start healing if ally is injured and healing allies is enabled
                if (healAlliesInBase && allyEntity.CurrentHealth < allyEntity.MaxHealth)
                {
                    StartHealing();
                }
                
                if (showDebugMessages)
                {
                    Debug.Log($"Ally {allyEntity.gameObject.name} entered the base.");
                }
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player has left the base area.");
            
            // Only stop healing if no deposit is in progress
            if (!isChitinDepositInProgress)
            {
                StopHealing();
            }
            else
            {
                Debug.Log("Deposit in progress - not clearing references yet");
            }
            
            // We'll keep player references until deposit completes
            // playerInventory = null; - Don't clear this reference until deposit completes
            // playerEntity = null; - Don't clear these references until deposit completes
            
            // Remove player outline (if applied)
            if (playerOutline != null)
            {
                playerOutline.SetOutlineColor(originalOutlineColor);
                playerOutline = null;
            }
            
            // Update any other controller references to allow deposit to finish
            UpdateChitinDepositController();
        }
        else if (other.CompareTag("Ally") && healAlliesInBase)
        {
            // Handle ally exit...
        }
    }
    
    private void DepositPlayerChitin()
    {
        if (playerInventory == null) return;
        
        // Get current chitin count before deposit
        int currentChitin = playerInventory.ChitinCount;
        
        if (currentChitin > 0)
        {
            // Calculate XP amount - ALWAYS 2 XP per chitin
            int xpAmount = currentChitin * 1;
            
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

            // Remove chitin from inventory BEFORE giving XP
            // But don't add XP yet - that will happen during the animation
            playerInventory.RemoveChitin(currentChitin);
            
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
                Debug.Log($"Deposited {currentChitin} chitin! XP to be gained: {xpAmount}!");
            }
        }
    }
    
    // Modified to prevent double XP application
    private IEnumerator PlayXPGainAfterChitinDeposit(int xpAmount, int startXP, int targetXP)
    {
        // Ensure playerInventory is available
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
            Debug.LogWarning("playerInventory was null, attempting to find it: " + (playerInventory != null));
        }
        
        // Calculate how many chitins were deposited based on XP gained
        int depositedChitinAmount = xpAmount;
        
        // Wait for chitin flight animation to complete
        if (visualEffectManager != null)
        {
            yield return new WaitForSeconds(1.5f);
        }
        
        // Play UI-based deposit and XP animations
        UIVisualEffectController uiEffects = FindObjectOfType<UIVisualEffectController>();
        if (uiEffects != null && playerInventory != null)
        {
            // Pass the deposited chitin amount for visual effects and player inventory reference
            uiEffects.PlayChitinDepositEffect(depositedChitinAmount, xpAmount, playerInventory);
            Debug.Log("Deposited chitin count: " + depositedChitinAmount);
            
            // NOTE: We're no longer adding XP here - it will be added when the XP symbol reaches the panel
            
            // Let the animation play out
            yield return new WaitForSeconds(1.5f);
        }
        else
        {
            // Fallback if no UI effects controller or no player inventory
            Debug.Log($"XP Gained: {xpAmount} (No UI effects or no inventory)");
            
            // Add XP directly if no animations or no inventory reference
            if (playerInventory != null)
            {
                playerInventory.AddXP(xpAmount);
                Debug.Log($"*** Added {xpAmount} XP to player inventory (direct) ***");
                
                // Update UI to reflect changes
                UIHelper uiHelper = FindObjectOfType<UIHelper>();
                if (uiHelper != null)
                {
                    uiHelper.UpdateExperienceDisplay(playerInventory.TotalExperience, true);
                }
            }
            
            // Play a sound to indicate XP gain
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound("XPGained");
            }
        }
    }
    
    private void DepositPlayerCrumbs()
    {
        if (playerInventory == null) return;
        
        int currentCrumbs = playerInventory.CrumbCount;
        
        if (currentCrumbs > 0)
        {
            // Check if an entity is equipped before processing
            DiscoveredEntitiesController entityController = FindObjectOfType<DiscoveredEntitiesController>();
            bool hasEquippedEntity = entityController != null && !string.IsNullOrEmpty(entityController.GetEquippedEntity());
            
            if (!hasEquippedEntity)
            {
                // No entity equipped, show a message and return without processing crumbs
                if (UIHelper.Instance != null)
                {
                    UIHelper.Instance.ShowInformText("Select an entity first before depositing crumbs!");
                }
                return;
            }
            
            // Always spawn visual effects for the crumbs being deposited
            if (visualEffectManager != null)
            {
                // Start the coroutine properly instead of calling the method directly
                visualEffectManager.SpawnEffect("shine", playerInventory.transform.position, currentCrumbs);
            }
            
            // Always remove crumbs from player inventory
            playerInventory.RemoveCrumb(currentCrumbs);
            
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
        yield return new WaitForSeconds(1.5f);
        
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

    // Add this method to update nest visuals
    public void UpdateNestVisuals()
    {
        // Update visual to indicate active/inactive state
        if (CircleImage != null)
        {
            // Grey out inactive nests, use normal color for active ones
            CircleImage.color = isPurchased ? baseCircleColor : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    // Add this method to enable the nest when purchased
    public void SetPurchased(bool purchased)
    {
        isPurchased = purchased;
        enabled = purchased || !isPurchasable; // Always enable Nest1, others only when purchased
        UpdateNestVisuals();
        
        Debug.Log($"Nest {gameObject.name} purchase state set to: {isPurchased}");
    }

    // Update the AnimateXPCounter method in BaseInteraction.cs
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
        
        // We don't need to check for level-up here anymore since AddExperienceGradually handles it
    }

    private IEnumerator ShowDepositEffect(int depositAmount)
    {
        // Display healing effects if player was healed during this deposit
        if (healPlayerInBase && playerEntity != null && 
            playerEntity.CurrentHealth < playerEntity.MaxHealth)
        {
            // Only play the healing effect once per deposit
            if (playerEntity.HealthPercentage < 1f && visualEffectManager != null)
            {
                visualEffectManager.SpawnEffect("heal", playerEntity.transform.position);
            }
        }
        
        // Wait for player to register the deposit
        yield return new WaitForSeconds(0.5f);
        
        // Play deposit sound effect
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound(depositSoundEffectName, transform.position);
        }
        
        // Wait a bit more
        yield return new WaitForSeconds(0.5f);
        
        // We've removed the visual animations for deposits
        Debug.Log($"Deposit of {depositAmount} complete");
    }

    // Add this method to track and handle the completion of chitin deposits
    private void UpdateChitinDepositController()
    {
        // Find the UI effects controller and tell it to keep going
        UIVisualEffectController uiEffects = FindObjectOfType<UIVisualEffectController>();
        if (uiEffects != null)
        {
            uiEffects.EnsureChitinDepositCompletes();
        }
    }

    // Modify the DepositChitin method to properly track animation state
    private void DepositChitin()
    {
        // Mark that deposit is starting
        isChitinDepositInProgress = true;
        
        // Start coroutine to handle chitin deposit (with updated references)
        StartCoroutine(HandleChitinDeposit());
    }

    // Add a method to complete the deposit and clean up
    private IEnumerator HandleChitinDeposit()
    {
        // Replace any references to visualEffectManager.ChitinFlyDuration with chitinFlyDuration
        
        // For example, if you have code like:
        // float totalAnimationTime = 5f * visualEffectManager.ChitinFlyDuration;
        // Change it to:
        // float totalAnimationTime = 5f * chitinFlyDuration;
        
        // Replace with a simple delay using our class-level chitinFlyDuration
        yield return new WaitForSeconds(chitinFlyDuration + 0.5f);
        
        // Now it's safe to clean up references
        isChitinDepositInProgress = false;
        
        // Clean up references if player has left the base
        if (!isHealing)
        {
            playerInventory = null;
            playerEntity = null;
        }
        
        Debug.Log("Chitin deposit completed and references cleaned up");
    }

    // If there's a method that used SpawnCrumbCollectEffect, replace it with SpawnEffect
    private void ShowCrumbCollectionEffect(Vector3 position, int count)
    {
        // Instead of:
        // visualEffectManager.SpawnCrumbCollectEffect(position, count);
        
        // Use:
        if (visualEffectManager != null)
        {
            visualEffectManager.SpawnEffect("shine", position, count);
        }
    }
} 