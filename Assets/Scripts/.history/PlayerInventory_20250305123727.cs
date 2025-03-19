using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxChitinCapacity = 5;
    
    [Header("Collection Settings")]
    [SerializeField] private string lootLayerName = "Loot";
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private AudioClip collectionSound;
    
    [Header("Experience Settings")]
    [SerializeField] private int experiencePerChitin = 10;
    
    [Header("Level System")]
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private int[] xpRequirements = new int[10] { 0, 20, 60, 450, 700, 1000, 1350, 1750, 2200, 2700 };
    
    // Event that UI can subscribe to
    public event Action<int> OnChitinCountChanged;
    public event Action<int> OnExperienceChanged;
    public event Action<int> OnLevelUp; // New event for level up
    
    // Inventory data
    private int _chitinCount = 0;
    public int ChitinCount 
    { 
        get => _chitinCount; 
        private set 
        {
            _chitinCount = Mathf.Clamp(value, 0, maxChitinCapacity);
            OnChitinCountChanged?.Invoke(_chitinCount);
        }
    }
    
    // Experience data
    private int _experience = 0;
    public int Experience
    {
        get => _experience;
        private set
        {
            int oldExperience = _experience;
            _experience = Mathf.Max(0, value);
            OnExperienceChanged?.Invoke(_experience);
            
            // Check for level up
            int oldLevel = GetLevelFromExperience(oldExperience);
            int newLevel = GetLevelFromExperience(_experience);
            
            if (newLevel > oldLevel)
            {
                // Level up occurred!
                OnLevelUp?.Invoke(newLevel);
                
                if (showDebugMessages)
                {
                    Debug.Log($"Level up! Now level {newLevel}");
                }
            }
        }
    }
    
    // Level system properties
    public int CurrentLevel => GetLevelFromExperience(_experience);
    public int ExperienceForNextLevel => GetExperienceRequiredForNextLevel(_experience);
    public int CurrentLevelExperience => GetExperienceInCurrentLevel(_experience);
    public float LevelProgress => GetLevelProgress(_experience);
    public bool IsMaxLevel => CurrentLevel >= maxLevel;
    
    // Property for max chitin capacity
    public int MaxChitinCapacity => maxChitinCapacity;
    
    // Called when game starts
    private void Start()
    {
        // Initialize inventory
        ChitinCount = 0;
        Experience = 0;
    }
    
    // Get the level based on current experience
    private int GetLevelFromExperience(int exp)
    {
        int level = 1;
        
        for (int i = 1; i < xpRequirements.Length; i++)
        {
            if (exp >= xpRequirements[i])
            {
                level = i + 1;
            }
            else
            {
                break;
            }
        }
        
        return Mathf.Min(level, maxLevel);
    }
    
    // Calculate how much XP is needed for the next level
    private int GetExperienceRequiredForNextLevel(int exp)
    {
        int currentLevel = GetLevelFromExperience(exp);
        
        // If already at max level, return 0
        if (currentLevel >= maxLevel)
        {
            return 0;
        }
        
        return xpRequirements[currentLevel];
    }
    
    // Calculate how much XP has been earned in the current level
    private int GetExperienceInCurrentLevel(int exp)
    {
        int currentLevel = GetLevelFromExperience(exp);
        
        // If at level 1, all experience counts
        if (currentLevel == 1)
        {
            return exp;
        }
        
        // Otherwise, subtract the XP required for the current level
        return exp - xpRequirements[currentLevel - 2];
    }
    
    // Calculate progress to next level (0.0 to 1.0)
    private float GetLevelProgress(int exp)
    {
        int currentLevel = GetLevelFromExperience(exp);
        
        // If at max level, progress is 100%
        if (currentLevel >= maxLevel)
        {
            return 1.0f;
        }
        
        int currentLevelXP = GetExperienceInCurrentLevel(exp);
        int xpForCurrentLevel = xpRequirements[currentLevel - 1] - (currentLevel > 1 ? xpRequirements[currentLevel - 2] : 0);
        
        return (float)currentLevelXP / xpForCurrentLevel;
    }
    
    private int lootLayerNumber;
    private AudioSource audioSource;
    
    private void Awake()
    {
        // Cache the layer number for more efficient collision checks
        lootLayerNumber = LayerMask.NameToLayer(lootLayerName);
        
        // Get or add audio source for collection sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && collectionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object is on the Loot layer
        if (other.gameObject.layer == lootLayerNumber)
        {
            // Assume it's chitin for now (you can expand this for different item types later)
            CollectChitin(other.gameObject);
        }
    }
    
    private void CollectChitin(GameObject chitinObject)
    {
        // Increase chitin count
        ChitinCount++;
        
        // Play collection sound if assigned
        if (audioSource != null && collectionSound != null)
        {
            audioSource.PlayOneShot(collectionSound);
        }
        
        // Destroy the chitin object
        Destroy(chitinObject);
    }
    
    // Method to use chitin (for crafting, etc.)
    public bool UseChitin(int amount)
    {
        if (ChitinCount >= amount)
        {
            ChitinCount -= amount;
            return true;
        }
        return false;
    }
    
    // Method to add chitin (for testing or rewards)
    public bool AddChitin(int amount)
    {
        if (amount <= 0) return false;
        
        int previousCount = ChitinCount;
        ChitinCount += amount;
        
        // Return true if some chitin was collected
        return ChitinCount > previousCount;
    }
    
    // Remove chitin from inventory
    public bool RemoveChitin(int amount)
    {
        if (amount <= 0 || ChitinCount < amount) return false;
        
        ChitinCount -= amount;
        return true;
    }
    
    // Deposit chitin at the base and gain XP
    public void DepositChitinAtBase(int amount)
    {
        if (amount <= 0 || ChitinCount < amount) return;
        
        // Remove chitin from inventory
        ChitinCount -= amount;
        
        // Add experience
        Experience += amount * experiencePerChitin;
    }
    
    // Deposit all chitin at once
    public void DepositAllChitinAtBase()
    {
        int chitinToDeposit = ChitinCount;
        DepositChitinAtBase(chitinToDeposit);
    }
} 