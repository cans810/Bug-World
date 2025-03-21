using UnityEngine;
using System;
using System.IO;
using System.Collections;

public class PlayerAttributes : MonoBehaviour
{
    [System.Serializable]
    public class AttributeStat
    {
        public string name;
        public int points; // Now 0-100 scale for most attributes
        public float percentagePerPoint; // How much each point increases the base stat
        
        public float GetMultiplier()
        {
            // Calculate multiplier: 1.0 (base) + points * percentage per point
            return 1f + (points * percentagePerPoint / 100f);
        }
    }
    
    [Header("Attributes")]
    [SerializeField] public AttributeStat strength = new AttributeStat 
    { 
        name = "Strength", 
        points = 0,
        percentagePerPoint = 16f
    };
    
    [SerializeField] public AttributeStat vitality = new AttributeStat 
    { 
        name = "Vitality", 
        points = 0,
        percentagePerPoint = 6f
    };
    
    [SerializeField] public AttributeStat agility = new AttributeStat 
    { 
        name = "Agility", 
        points = 0,
        percentagePerPoint = 7.76f
    };

    [SerializeField] public AttributeStat recovery = new AttributeStat
    {
        name = "Recovery",
        points = 0,
        percentagePerPoint = 12f
    };
    
    [SerializeField] public AttributeStat speed = new AttributeStat
    {
        name = "Speed",
        points = 0,
        percentagePerPoint = 4f
    };
    
    [Header("Incubation")]
    [SerializeField] public int incubationPoints = 0;
    [SerializeField] public int maxIncubationPoints = 10; // Changed from 20 to 10
    
    [Header("Base Stats")]
    [SerializeField] public float baseMaxHealth = 100f;
    [SerializeField] public float baseRecoveryTime = 5f;
    [SerializeField] public float baseAttackDamage = 10f;
    [SerializeField] public float baseAttackCooldown = 1.0f;
    
    [Header("Attribute Points")]
    [SerializeField] public int availablePoints = 0;
    [SerializeField] public int pointsPerLevel = 2;
    
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private LivingEntity livingEntity;
    [SerializeField] private UIHelper uiHelper;
    
    [Header("Save/Load Settings")]
    [SerializeField] private bool autoSaveOnChange = true;
    
    // Event when attributes change
    public event Action OnAttributesChanged;
    
    // Add a specific event for incubation changes
    public event Action OnIncubationChanged;
    
    // Public properties
    public int AvailablePoints => availablePoints;
    public int StrengthPoints => strength.points;
    public int VitalityPoints => vitality.points;
    public int AgilityPoints => agility.points;
    public int IncubationPoints => incubationPoints;
    public float StrengthMultiplier => strength.GetMultiplier();
    public float VitalityMultiplier => vitality.GetMultiplier();
    public float AgilityMultiplier => agility.GetMultiplier();
    
    // Derived properties
    public int MaxEggCapacity => 1 + incubationPoints; // Base capacity of 1 + points
    public float AttackCooldownMultiplier => 1f / agility.GetMultiplier(); // Convert to cooldown reduction
    
    // Add property for recovery
    public int RecoveryPoints => recovery.points;
    public float RecoveryMultiplier => recovery.GetMultiplier();
    
    // Add these fields to explicitly define max values for all attributes
    [Header("Attribute Caps")]
    [SerializeField] private int maxStrengthPoints = 25; // Reduced from 100
    [SerializeField] private int maxVitalityPoints = 25; // Reduced from 100
    [SerializeField] private int maxAgilityPoints = 25;  // Reduced from 100
    [SerializeField] private int maxRecoveryPoints = 25; // Reduced from 100
    [SerializeField] private int maxSpeedPoints = 25;    // Added speed cap
    
    // Add public properties to access these caps
    public int MaxStrengthPoints => maxStrengthPoints;
    public int MaxVitalityPoints => maxVitalityPoints;
    public int MaxAgilityPoints => maxAgilityPoints;
    public int MaxRecoveryPoints => maxRecoveryPoints;
    public int MaxSpeedPoints => maxSpeedPoints;         // Added speed cap property
    public int MaxIncubationPoints => maxIncubationPoints;
    
    // Add property for speed
    public int SpeedPoints => speed.points;
    public float SpeedMultiplier => speed.GetMultiplier();
    
    private bool isLoadingData = false;

    [Header("Map Border Settings")]
    [SerializeField] public string borderObjectName = "MapBorder";
    
    public void SetLoadingDataState(bool loading)
    {
        isLoadingData = loading;
    }
    
    private void Awake()
    {
        // InitializeSavePath();
    }
    
    private void Start()
    {
        // Find references if not assigned
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();
            
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (uiHelper == null)
            uiHelper = FindObjectOfType<UIHelper>();
            
        // Subscribe to level up event
        if (playerInventory != null)
        {
            playerInventory.OnLevelUp += HandleLevelUp;
            
            // Sync the egg capacity with PlayerInventory
            playerInventory.UpdateEggCapacity(MaxEggCapacity);
            Debug.Log($"PlayerAttributes: Synced egg capacity with PlayerInventory: {MaxEggCapacity}");
        }
        
        // Apply initial effects
        ApplyAttributeEffects();
    }
    
    private void HandleLevelUp(int newLevel)
    {
        // Skip giving attribute points if we're loading data
        if (isLoadingData) return;
        
        // Award attribute points based on level
        availablePoints += 2;
        
        // Increase chitin and crumb capacity by 15 each time the player levels up
        if (playerInventory != null)
        {
            playerInventory.IncreaseChitinCapacity(15);
            playerInventory.IncreaseCrumbCapacity(10);
        }
        
        
        // Display the level up rewards text
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null && uiHelper.onLevelUpThingsGainedText != null)
        {
            // Set the text with the rewards information
            uiHelper.onLevelUpThingsGainedText.text = "+15 Chitin Capacity\n+10 Crumb Capacity\n+1 Attribute Point";
            
            // Make the text visible
            uiHelper.onLevelUpThingsGainedText.gameObject.SetActive(true);
            
            // Start a coroutine to hide the text after a delay
            StartCoroutine(HideLevelUpRewardsTextAfterDelay(uiHelper, 4.5f));
        }
        
        // Notify listeners that attributes have changed
        OnAttributesChanged?.Invoke();
    }
    
    // Coroutine to hide the level up rewards text after a delay
    private IEnumerator HideLevelUpRewardsTextAfterDelay(UIHelper uiHelper, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Hide the text
        if (uiHelper != null && uiHelper.onLevelUpThingsGainedText != null)
        {
            uiHelper.onLevelUpThingsGainedText.gameObject.SetActive(false);
        }
    }
    
    // Method to increase strength (called from UI)
    public bool IncreaseStrength()
    {
        if (availablePoints <= 0 || strength.points >= maxStrengthPoints)
            return false;
            
        availablePoints--;
        strength.points++;
        
        ApplyAttributeEffects(); // This now includes auto-saving
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Strength to {strength.points}/{maxStrengthPoints}. Damage multiplier: {strength.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase vitality (called from UI)
    public bool IncreaseVitality()
    {
        if (availablePoints <= 0 || vitality.points >= maxVitalityPoints)
            return false;
            
        availablePoints--;
        vitality.points++;
        
        ApplyAttributeEffects(); // This now includes auto-saving
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Vitality to {vitality.points}/{maxVitalityPoints}. Health multiplier: {vitality.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase agility (called from UI)
    public bool IncreaseAgility()
    {
        if (availablePoints <= 0 || agility.points >= maxAgilityPoints)
            return false;
            
        availablePoints--;
        agility.points++;
        
        ApplyAttributeEffects(); // This now includes auto-saving
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Agility to {agility.points}/{maxAgilityPoints}. Attack speed multiplier: {agility.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase incubation (called from UI)
    public bool IncreaseIncubation()
    {
        if (availablePoints <= 0 || incubationPoints >= maxIncubationPoints)
            return false;
            
        availablePoints--;
        incubationPoints++;
        
        ApplyAttributeEffects(); // This now includes auto-saving
        OnAttributesChanged?.Invoke();
        OnIncubationChanged?.Invoke();
        
        // Directly update the UI
        if (uiHelper == null)
            uiHelper = FindObjectOfType<UIHelper>();
        
        if (uiHelper != null)
        {
            // Force update the egg display with current count
            InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
            int currentEggCount = incubator != null ? incubator.GetCurrentEggCount() : 0;
            uiHelper.UpdateEggDisplay(currentEggCount);
            Debug.Log($"Directly updated UI with egg capacity: {MaxEggCapacity}");
        }
        
        Debug.Log($"Increased Incubation to {incubationPoints}/{maxIncubationPoints}. Max eggs: {MaxEggCapacity}");
        return true;
    }
    
    // Method to increase recovery (called from UI)
    public bool IncreaseRecovery()
    {
        if (availablePoints <= 0 || recovery.points >= maxRecoveryPoints)
            return false;
            
        availablePoints--;
        recovery.points++;
        
        ApplyAttributeEffects(); // This now includes auto-saving
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Recovery to {recovery.points}/{maxRecoveryPoints}. Recovery multiplier: {recovery.GetMultiplier():F2}x");
        return true;
    }
    
    // Modify this method to save attributes after applying effects
    public void ApplyAttributeEffects()
    {
        // Apply max health from vitality
        if (livingEntity != null)
        {
            float newAttackDamage = baseAttackDamage * strength.GetMultiplier();
            livingEntity.SetAttackDamage(newAttackDamage);

            float newMaxHealth = baseMaxHealth * vitality.GetMultiplier();
            livingEntity.SetMaxHealth(newMaxHealth);
            
            // Apply attack cooldown from agility
            float newAttackCooldown = baseAttackCooldown * AttackCooldownMultiplier;
            livingEntity.SetAttackCooldown(newAttackCooldown);
            
            // Apply recovery time from recovery attribute
            // Lower is better for recovery time, so we divide by the multiplier
            float newRecoveryTime = baseRecoveryTime / recovery.GetMultiplier();
            livingEntity.SetRecoveryTime(newRecoveryTime);
            
            Debug.Log($"Applied attribute effects: Health={newMaxHealth}, " +
                      $"Damage={newAttackDamage}, Cooldown={newAttackCooldown}, " +
                      $"Recovery Time={newRecoveryTime}");
        }
        
        // Sync with PlayerInventory
        if (playerInventory != null)
        {
            playerInventory.UpdateEggCapacity(MaxEggCapacity);
            Debug.Log($"Applied attribute effects: Max Egg Capacity = {MaxEggCapacity}");
        }
        
        // Only perform save operations if we're not currently loading data
        if (!isLoadingData && autoSaveOnChange)
        {
            // Get the gameManager and save the game
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.SaveGame();
            }
        }
    }
    
    // Method to apply strength to damage calculation
    public float GetModifiedDamage(float rawDamage)
    {
        // Apply strength multiplier to damage
        return rawDamage * strength.GetMultiplier();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerInventory != null)
            playerInventory.OnLevelUp -= HandleLevelUp;
    }

    public void RecalculateAvailablePoints(bool forceRecalculate = false)
    {
        // If we're loading data and not forcing recalculation, don't override the loaded value
        if (isLoadingData && !forceRecalculate)
        {
            // Just notify listeners if needed, but don't change the value
            if (OnAttributesChanged != null)
                OnAttributesChanged();
            
            if (OnIncubationChanged != null)
                OnIncubationChanged();
            
            return;
        }
        
        // Regular calculation for normal gameplay
        // Calculate total points based on player level (assumed to be level - 1)
        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
        int totalPoints = inventory != null ? inventory.CurrentLevel - 1 : 0;
        
        // Calculate spent points
        int spentPoints = StrengthPoints + VitalityPoints + AgilityPoints + IncubationPoints + RecoveryPoints;
        
        // Calculate available points
        availablePoints = Mathf.Max(0, totalPoints - spentPoints);
        
        // Notify listeners
        if (OnAttributesChanged != null)
            OnAttributesChanged();
        
        if (OnIncubationChanged != null)
            OnIncubationChanged();
    }

    // Add this method to directly set attribute values
    public void SetAttributeValues(int strength, int vitality, int agility, int incubation, int recovery, int availablePoints)
    {
        // Set the values directly
        this.strength.points = strength;
        this.vitality.points = vitality;
        this.agility.points = agility;
        this.incubationPoints = incubation;
        this.recovery.points = recovery;
        this.availablePoints = availablePoints;
        
        // Apply the effects
        ApplyAttributeEffects();
        
        // Notify listeners
        OnAttributesChanged?.Invoke();
        OnIncubationChanged?.Invoke();
        
        Debug.Log($"Directly set attribute values: Strength={strength}, Vitality={vitality}, " +
                  $"Agility={agility}, Incubation={incubation}, Recovery={recovery}, " +
                  $"AvailablePoints={availablePoints}");
    }

    // Add this method overload to maintain backward compatibility
    public void SetAttributeValues(int strength, int vitality, int agility, int incubation, int availablePoints)
    {
        // Call the new method with default recovery value of 0
        SetAttributeValues(strength, vitality, agility, incubation, 0, availablePoints);
        
        Debug.Log("Using backward compatibility method for SetAttributeValues - recovery set to 0");
    }

    // Save player attributes to disk
    public void SavePlayerAttributes(GameData saveData)
    {
        if (saveData == null) return;
        
        // Save attribute data to GameData
        saveData.currentStrength = strength.points;
        saveData.currentVitality = vitality.points;
        saveData.currentAgility = agility.points;
        saveData.currentIncubation = incubationPoints;
        saveData.currentRecovery = recovery.points;
        saveData.availableAttributePoints = availablePoints;
        
        Debug.Log("Player attributes saved to GameData");
    }
    
    // Load player attributes from disk
    public void LoadPlayerAttributes(GameData saveData)
    {
        if (saveData == null)
        {
            Debug.Log("No saved player attributes found. Using defaults.");
            return;
        }
        
        try
        {
            // Load attribute data from GameData
            strength.points = saveData.currentStrength;
            vitality.points = saveData.currentVitality;
            agility.points = saveData.currentAgility;
            incubationPoints = saveData.currentIncubation;
            recovery.points = saveData.currentRecovery;
            availablePoints = saveData.availableAttributePoints;
            
            // Apply the loaded attributes to the player
            if (livingEntity != null)
            {
                float newMaxHealth = baseMaxHealth * vitality.GetMultiplier();
                float newAttackDamage = baseAttackDamage * strength.GetMultiplier();
                float newAttackCooldown = baseAttackCooldown * AttackCooldownMultiplier;
                float newRecoveryTime = baseRecoveryTime / recovery.GetMultiplier();
                
                livingEntity.SetMaxHealth(newMaxHealth);
                livingEntity.SetAttackDamage(newAttackDamage);
                livingEntity.SetAttackCooldown(newAttackCooldown);
                livingEntity.SetRecoveryTime(newRecoveryTime);
                
                Debug.Log($"Loaded player attributes: Health={newMaxHealth}, " +
                          $"Damage={newAttackDamage}, Cooldown={newAttackCooldown}, " +
                          $"Recovery Time={newRecoveryTime}");
            }
            else
            {
                Debug.LogWarning("LivingEntity reference not found. Could not apply loaded attributes.");
            }
            
            // Notify UI of attribute changes
            OnAttributesChanged?.Invoke();
            OnIncubationChanged?.Invoke();
            
            Debug.Log("Player attributes loaded from GameData");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load player attributes: {e.Message}");
        }
    }

    // Add this method to PlayerAttributes.cs
    public void AddAttributePoints(int pointsToAdd)
    {
        availablePoints += pointsToAdd;
        
        // Notify listeners that attributes have changed
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Added {pointsToAdd} attribute points. New total: {availablePoints}");
    }
} 