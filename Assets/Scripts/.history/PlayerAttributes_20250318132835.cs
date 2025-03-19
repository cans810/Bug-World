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
    [SerializeField] private AttributeStat strength = new AttributeStat 
    { 
        name = "Strength", 
        points = 0,
        percentagePerPoint = 5f
    };
    
    [SerializeField] private AttributeStat vitality = new AttributeStat 
    { 
        name = "Vitality", 
        points = 0,
        percentagePerPoint = 2f
    };
    
    [SerializeField] private AttributeStat agility = new AttributeStat 
    { 
        name = "Agility", 
        points = 0,
        percentagePerPoint = 2f
    };
    
    [Header("Incubation")]
    [SerializeField] private int incubationPoints = 0;
    [SerializeField] private int maxIncubationPoints = 20; // Changed from 5 to 20
    
    [Header("Base Stats")]
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float baseAttackDamage = 10f;
    [SerializeField] private float baseAttackCooldown = 1.0f;
    
    [Header("Attribute Points")]
    [SerializeField] private int availablePoints = 0;
    [SerializeField] private int pointsPerLevel = 1;
    
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
    
    private bool isLoadingData = false;
    
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
        availablePoints++;
        
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
        if (availablePoints <= 0 || strength.points >= 100)
            return false;
            
        availablePoints--;
        strength.points++;
        
        ApplyAttributeEffects(); // This now includes auto-saving
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Strength to {strength.points}/100. Damage multiplier: {strength.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase vitality (called from UI)
    public bool IncreaseVitality()
    {
        if (availablePoints <= 0 || vitality.points >= 100)
            return false;
            
        availablePoints--;
        vitality.points++;
        
        ApplyAttributeEffects(); // This now includes auto-saving
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Vitality to {vitality.points}/100. Health multiplier: {vitality.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase agility (called from UI)
    public bool IncreaseAgility()
    {
        if (availablePoints <= 0 || agility.points >= 100)
            return false;
            
        availablePoints--;
        agility.points++;
        
        ApplyAttributeEffects(); // This now includes auto-saving
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Agility to {agility.points}/100. Attack speed multiplier: {agility.GetMultiplier():F2}x");
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
            AntIncubator incubator = FindObjectOfType<AntIncubator>();
            int currentEggCount = incubator != null ? incubator.GetCurrentEggCount() : 0;
            uiHelper.UpdateEggDisplay(currentEggCount);
            Debug.Log($"Directly updated UI with egg capacity: {MaxEggCapacity}");
        }
        
        Debug.Log($"Increased Incubation to {incubationPoints}/{maxIncubationPoints}. Max eggs: {MaxEggCapacity}");
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
            
            Debug.Log($"Applied attribute effects: Health={newMaxHealth}, " +
                      $"Damage={newAttackDamage}, Cooldown={newAttackCooldown}");
        }
        
        // Sync with PlayerInventory
        if (playerInventory != null)
        {
            playerInventory.UpdateEggCapacity(MaxEggCapacity);
            Debug.Log($"Applied attribute effects: Max Egg Capacity = {MaxEggCapacity}");
        }
        
        // Auto-save if enabled
        if (autoSaveOnChange)
        {
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
        int spentPoints = StrengthPoints + VitalityPoints + AgilityPoints + IncubationPoints;
        
        // Calculate available points
        availablePoints = Mathf.Max(0, totalPoints - spentPoints);
        
        // Notify listeners
        if (OnAttributesChanged != null)
            OnAttributesChanged();
        
        if (OnIncubationChanged != null)
            OnIncubationChanged();
    }

    // Add this method to directly set attribute values
    public void SetAttributeValues(int strength, int vitality, int agility, int incubation, int availablePoints)
    {
        // Set the values directly
        this.strength.points = strength;
        this.vitality.points = vitality;
        this.agility.points = agility;
        this.incubationPoints = incubation;
        this.availablePoints = availablePoints;
        
        // Apply the effects
        ApplyAttributeEffects();
        
        // Notify listeners
        OnAttributesChanged?.Invoke();
        OnIncubationChanged?.Invoke();
        
        Debug.Log($"Directly set attribute values: Strength={strength}, Vitality={vitality}, " +
                  $"Agility={agility}, Incubation={incubation}, AvailablePoints={availablePoints}");
    }

    // Save player attributes to disk
    public void SavePlayerAttributes(GameData saveData)
    {
        if (saveData == null) return;
        
        // Save attribute data to GameData
        saveData.strengthPoints = strength.points;
        saveData.vitalityPoints = vitality.points;
        saveData.agilityPoints = agility.points;
        saveData.incubationPoints = incubationPoints;
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
            strength.points = saveData.strengthPoints;
            vitality.points = saveData.vitalityPoints;
            agility.points = saveData.agilityPoints;
            incubationPoints = saveData.incubationPoints;
            availablePoints = saveData.availableAttributePoints;
            
            // Apply the loaded attributes to the player
            if (livingEntity != null)
            {
                float newMaxHealth = baseMaxHealth * vitality.GetMultiplier();
                float newAttackDamage = baseAttackDamage * strength.GetMultiplier();
                float newAttackCooldown = baseAttackCooldown * AttackCooldownMultiplier;
                
                livingEntity.SetMaxHealth(newMaxHealth);
                livingEntity.SetAttackDamage(newAttackDamage);
                livingEntity.SetAttackCooldown(newAttackCooldown);
                
                Debug.Log($"Loaded player attributes: Health={newMaxHealth}, " +
                          $"Damage={newAttackDamage}, Cooldown={newAttackCooldown}");
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
} 