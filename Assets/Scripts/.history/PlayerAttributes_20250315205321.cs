using UnityEngine;
using System;

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
        percentagePerPoint = 0.2f // Adjusted to 0.2% per point (was 2%)
    };
    
    [SerializeField] private AttributeStat vitality = new AttributeStat 
    { 
        name = "Vitality", 
        points = 0,
        percentagePerPoint = 0.5f // Adjusted to 0.5% per point (was 5%)
    };
    
    [SerializeField] private AttributeStat agility = new AttributeStat 
    { 
        name = "Agility", 
        points = 0,
        percentagePerPoint = 0.5f // Adjusted to 0.5% per point (was 5%)
    };
    
    [Header("Incubation")]
    [SerializeField] private int incubationPoints = 0;
    [SerializeField] private int maxIncubationPoints = 20; // Changed from 5 to 20
    
    [Header("Base Stats")]
    [SerializeField] private float baseAttackDamage = 10f;
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float baseAttackCooldown = 1.0f;
    
    [Header("Attribute Points")]
    [SerializeField] private int availablePoints = 0;
    [SerializeField] private int pointsPerLevel = 1;
    
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private LivingEntity playerHealth;
    [SerializeField] private UIHelper uiHelper;
    
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
    
    private void Start()
    {
        // Find references if not assigned
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();
            
        if (playerHealth == null)
            playerHealth = GetComponent<LivingEntity>();
            
        if (uiHelper == null)
            uiHelper = FindObjectOfType<UIHelper>();
            
        // Subscribe to level up event
        if (playerInventory != null)
            playerInventory.OnLevelUp += HandleLevelUp;
            
        // Apply initial effects
        ApplyAttributeEffects();
    }
    
    private void HandleLevelUp(int newLevel)
    {
        // Skip giving attribute points if we're loading data
        if (isLoadingData) return;
        
        // Award attribute points based on level
        availablePoints++;
        
        // Show message
        Debug.Log($"Level up! You have {availablePoints} attribute points to spend.");
        
        // Notify listeners that attributes have changed
        OnAttributesChanged?.Invoke();
    }
    
    // Method to increase strength (called from UI)
    public bool IncreaseStrength()
    {
        if (availablePoints <= 0 || strength.points >= 100) // Changed from 10 to 100
            return false;
            
        availablePoints--;
        strength.points++;
        
        ApplyAttributeEffects();
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Strength to {strength.points}/100. Damage multiplier: {strength.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase vitality (called from UI)
    public bool IncreaseVitality()
    {
        if (availablePoints <= 0 || vitality.points >= 100) // Changed from 10 to 100
            return false;
            
        availablePoints--;
        vitality.points++;
        
        ApplyAttributeEffects();
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Vitality to {vitality.points}/100. Health multiplier: {vitality.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase agility (called from UI)
    public bool IncreaseAgility()
    {
        if (availablePoints <= 0 || agility.points >= 100) // Changed from 10 to 100
            return false;
            
        availablePoints--;
        agility.points++;
        
        ApplyAttributeEffects();
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Agility to {agility.points}/100. Attack speed multiplier: {agility.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase incubation (called from UI)
    public bool IncreaseIncubation()
    {
        if (availablePoints <= 0 || incubationPoints >= maxIncubationPoints) // Now uses maxIncubationPoints (20)
            return false;
            
        availablePoints--;
        incubationPoints++;
        
        ApplyAttributeEffects();
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
    
    private void ApplyAttributeEffects()
    {
        // Apply max health from vitality
        if (playerHealth != null)
        {
            float newMaxHealth = baseMaxHealth * vitality.GetMultiplier();
            playerHealth.SetMaxHealth(newMaxHealth);
            
            // Apply attack cooldown from agility
            float newAttackCooldown = baseAttackCooldown * AttackCooldownMultiplier;
            playerHealth.SetAttackCooldown(newAttackCooldown);
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

    public void RecalculateAvailablePoints()
    {
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
} 