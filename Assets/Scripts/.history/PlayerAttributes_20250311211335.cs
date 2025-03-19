using UnityEngine;
using System;

public class PlayerAttributes : MonoBehaviour
{
    [System.Serializable]
    public class AttributeStat
    {
        public string name;
        public int points; // 0-10 scale
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
        percentagePerPoint = 2f // 2% per point
    };
    
    [SerializeField] private AttributeStat vitality = new AttributeStat 
    { 
        name = "Vitality", 
        points = 0,
        percentagePerPoint = 5f // 5% per point
    };
    
    [SerializeField] private AttributeStat agility = new AttributeStat 
    { 
        name = "Agility", 
        points = 0,
        percentagePerPoint = 5f // 5% per point (reduces attack cooldown)
    };
    
    [Header("Incubation")]
    [SerializeField] private int incubationPoints = 0;
    [SerializeField] private int maxIncubationPoints = 5;
    
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
    
    // Event when attributes change
    public event Action OnAttributesChanged;
    
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
    
    private void Start()
    {
        // Find references if not assigned
        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventory>();
            
        if (playerHealth == null)
            playerHealth = GetComponent<LivingEntity>();
            
        // Subscribe to level up event
        if (playerInventory != null)
            playerInventory.OnLevelUp += HandleLevelUp;
            
        // Apply initial effects
        ApplyAttributeEffects();
    }
    
    private void HandleLevelUp(int newLevel)
    {
        // Grant attribute points
        availablePoints += pointsPerLevel;
        
        // Show message
        Debug.Log($"Level up! You have {availablePoints} attribute points to spend.");
        
        // Notify listeners that attributes have changed
        OnAttributesChanged?.Invoke();
    }
    
    // Method to increase strength (called from UI)
    public bool IncreaseStrength()
    {
        if (availablePoints <= 0 || strength.points >= 10)
            return false;
            
        availablePoints--;
        strength.points++;
        
        ApplyAttributeEffects();
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Strength to {strength.points}/10. Damage multiplier: {strength.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase vitality (called from UI)
    public bool IncreaseVitality()
    {
        if (availablePoints <= 0 || vitality.points >= 10)
            return false;
            
        availablePoints--;
        vitality.points++;
        
        ApplyAttributeEffects();
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Vitality to {vitality.points}/10. Health multiplier: {vitality.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase agility (called from UI)
    public bool IncreaseAgility()
    {
        if (availablePoints <= 0 || agility.points >= 10)
            return false;
            
        availablePoints--;
        agility.points++;
        
        ApplyAttributeEffects();
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Increased Agility to {agility.points}/10. Attack speed multiplier: {agility.GetMultiplier():F2}x");
        return true;
    }
    
    // Method to increase incubation (called from UI)
    public bool IncreaseIncubation()
    {
        if (availablePoints <= 0 || incubationPoints >= maxIncubationPoints)
            return false;
            
        availablePoints--;
        incubationPoints++;
        
        ApplyAttributeEffects();
        OnAttributesChanged?.Invoke();
        
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
} 