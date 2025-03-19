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
    
    [Header("Base Stats")]
    [SerializeField] private float baseAttackDamage = 10f;
    [SerializeField] private float baseMaxHealth = 100f;
    
    [Header("Attribute Points")]
    [SerializeField] private int availablePoints = 0;
    [SerializeField] private int pointsPerLevel = 1;
    
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private LivingEntity playerHealth;
    
    [Header("Ally Points")]
    [SerializeField] private int allyPoints = 0;
    
    // Event when attributes change
    public event Action OnAttributesChanged;
    
    // Public properties
    public int AvailablePoints => availablePoints;
    public int StrengthPoints => strength.points;
    public int VitalityPoints => vitality.points;
    public float StrengthMultiplier => strength.GetMultiplier();
    public float VitalityMultiplier => vitality.GetMultiplier();
    public int AllyPoints => allyPoints;
    
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
    
    private void ApplyAttributeEffects()
    {
        // Apply max health from vitality
        if (playerHealth != null)
        {
            float newMaxHealth = baseMaxHealth * vitality.GetMultiplier();
            playerHealth.SetMaxHealth(newMaxHealth);
        }
    }
    
    // Method to apply strength to damage calculation
    public float GetModifiedDamage(float rawDamage)
    {
        // Apply strength multiplier to damage
        return rawDamage * strength.GetMultiplier();
    }
    
    // Add ally points
    public void AddAllyPoints(int amount)
    {
        if (amount <= 0) return;
        
        allyPoints += amount;
        
        // Notify listeners that attributes have changed
        OnAttributesChanged?.Invoke();
        
        Debug.Log($"Added {amount} ally points. Total: {allyPoints}");
    }
    
    // Use an ally point
    public bool UseAllyPoint()
    {
        if (allyPoints <= 0)
            return false;
            
        allyPoints--;
        OnAttributesChanged?.Invoke();
        return true;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerInventory != null)
            playerInventory.OnLevelUp -= HandleLevelUp;
    }
} 