using UnityEngine;
using System;

public class PlayerAttributes : MonoBehaviour
{
    [System.Serializable]
    public class AttributeStat
    {
        public string name;
        public float baseValue;
        public float valuePerLevel;
        [HideInInspector] public float currentValue;
        
        public float GetValue(int level)
        {
            return baseValue + (valuePerLevel * (level - 1));
        }
    }
    
    [Header("Base Attributes")]
    [SerializeField] private AttributeStat strength = new AttributeStat 
    { 
        name = "Strength", 
        baseValue = 10f, 
        valuePerLevel = 2f 
    };
    
    [SerializeField] private AttributeStat vitality = new AttributeStat 
    { 
        name = "Vitality", 
        baseValue = 100f, 
        valuePerLevel = 10f 
    };
    
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private LivingEntity playerHealth;
    
    // Event when attributes change
    public event Action OnAttributesChanged;
    
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
            
        // Initialize attributes for level 1
        UpdateAllAttributes(playerInventory != null ? playerInventory.CurrentLevel : 1);
    }
    
    private void HandleLevelUp(int newLevel)
    {
        // Update all attributes based on new level
        UpdateAllAttributes(newLevel);
        
        // Show message
        Debug.Log($"Level up! Your attributes have increased:\nStrength: {strength.currentValue}\nVitality: {vitality.currentValue}");
        
        // Notify listeners that attributes have changed
        OnAttributesChanged?.Invoke();
    }
    
    private void UpdateAllAttributes(int level)
    {
        // Calculate current value for each attribute
        strength.currentValue = strength.GetValue(level);
        vitality.currentValue = vitality.GetValue(level);
        
        // Apply attribute effects
        ApplyAttributeEffects();
    }
    
    private void ApplyAttributeEffects()
    {
        // Apply max health from vitality
        if (playerHealth != null)
        {
            playerHealth.SetMaxHealth(vitality.currentValue);
            
            // Heal to full when leveling up
            playerHealth.Heal(playerHealth.MaxHealth);
        }
        
        // Strength will be applied in attack scripts when they deal damage
    }
    
    // Getters for attributes to be used by other systems
    public float GetStrengthValue() => strength.currentValue;
    public float GetVitalityValue() => vitality.currentValue;
    
    // Method to apply strength to damage calculation
    public float ApplyStrengthToDamage(float baseDamage)
    {
        // Simple formula: baseDamage * (strength / baseStrength)
        return baseDamage * (strength.currentValue / strength.baseValue);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerInventory != null)
            playerInventory.OnLevelUp -= HandleLevelUp;
    }
} 