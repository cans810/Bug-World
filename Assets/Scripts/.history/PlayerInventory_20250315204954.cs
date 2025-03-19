using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxChitinCapacity = 5;
    [SerializeField] private int maxCrumbCapacity = 5;
    
    [Header("Collection Settings")]
    [SerializeField] private string lootLayerName = "Loot";
    [SerializeField] private string carriedLootLayerName = "CarriedLoot";
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private AudioClip collectionSound;
    
    [Header("Experience Settings")]
    [SerializeField] private int experiencePerChitin = 5;
    
    [Header("Level System")]
    [SerializeField] private int maxLevel = 10;
    private int[] xpRequirements = new int[10] { 0, 20, 60, 110, 150, 210, 280, 350, 430, 510 };
    
    [Header("Sound Effects")]
    [SerializeField] private string pickupSoundEffectName = "Pickup2";
    [SerializeField] private string levelUpSoundEffectName = "LevelUp";
    [SerializeField] private bool useSoundEffectManager = true;
    [SerializeField] private AudioClip pickupSound; // Fallback sound if SoundEffectManager is not available
    [SerializeField] private AudioClip levelUpSound; // Fallback sound for level up
    
    // Events that UI can subscribe to
    public event Action<int> OnChitinCountChanged;
    public event Action<int> OnCrumbCountChanged;
    public event Action<int> OnExperienceChanged;
    public event Action<int> OnLevelUp;
    public event Action OnChitinMaxed;
    public event Action OnCrumbMaxed;
    
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
    
    // Crumb inventory data
    private int _crumbCount = 0;
    public int CrumbCount
    {
        get => _crumbCount;
        private set
        {
            _crumbCount = Mathf.Clamp(value, 0, maxCrumbCapacity);
            OnCrumbCountChanged?.Invoke(_crumbCount);
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
                
                // Play level up sound
                PlayLevelUpSound();
                
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
    public int TotalExperience => _experience;
    public float LevelProgress => GetLevelProgress(_experience);
    public bool IsMaxLevel => CurrentLevel >= maxLevel;
    
    // Property for max capacities
    public int MaxChitinCapacity => maxChitinCapacity;
    public int MaxCrumbCapacity => maxCrumbCapacity;
    
    // Inventory state properties
    public bool IsChitinFull => ChitinCount >= maxChitinCapacity;
    public bool IsCrumbFull => CrumbCount >= maxCrumbCapacity;
    
    // Add this to PlayerInventory class
    public bool IsLoadingData { get; private set; } = false;
    
    // Called when game starts
    private void Start()
    {
        // Initialize inventory
        ChitinCount = 0;
        CrumbCount = 0;
        Experience = 0;
        
        // Add diagnostic listener
        OnChitinCountChanged += CheckExperienceGainIssue;
    }
    
    private void OnDestroy()
    {
        // Remove diagnostic listener
        OnChitinCountChanged -= CheckExperienceGainIssue;
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
                break; // Stop once we find a threshold we haven't reached
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
    
    // Calculate progress to next level (0.0 to 1.0)
    private float GetLevelProgress(int exp)
    {
        int currentLevel = GetLevelFromExperience(exp);
        
        // If at max level, progress is 100%
        if (currentLevel >= maxLevel)
        {
            return 1.0f;
        }
        
        // Get XP thresholds for current and next level
        int currentLevelThreshold = currentLevel > 1 ? xpRequirements[currentLevel - 1] : 0;
        int nextLevelThreshold = xpRequirements[currentLevel];
        
        // Calculate progress based on total XP position between thresholds
        float progress = (float)(exp - currentLevelThreshold) / (nextLevelThreshold - currentLevelThreshold);
        return Mathf.Clamp01(progress);
    }
    
    private int lootLayerNumber;
    private int carriedLootLayerNumber;
    private AudioSource audioSource;
    
    private void Awake()
    {
        // Cache the layer numbers for more efficient collision checks
        lootLayerNumber = LayerMask.NameToLayer(lootLayerName);
        carriedLootLayerNumber = LayerMask.NameToLayer(carriedLootLayerName);
        
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
        // Check if the collided object is on either the Loot or CarriedLoot layer
        if (other.gameObject.layer == lootLayerNumber || other.gameObject.layer == carriedLootLayerNumber)
        {
            string objectName = other.gameObject.name.ToLower();
            
            // Check name first - if it contains "crumb", treat as a crumb
            if (objectName.Contains("crumb"))
            {
                if (showDebugMessages)
                {
                    Debug.Log($"Identified crumb by name: {other.gameObject.name}");
                }
                CollectCrumb(other.gameObject);
                return;
            }
            
            // Try component-based detection next
            CrumbCollectible crumbComponent = other.GetComponentInParent<CrumbCollectible>();
            if (crumbComponent != null)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"Identified crumb by component: {other.gameObject.name}");
                }
                CollectCrumb(other.gameObject);
                return;
            }
            
            // Also check children for crumb component
            CrumbCollectible[] childCrumbs = other.GetComponentsInChildren<CrumbCollectible>();
            if (childCrumbs != null && childCrumbs.Length > 0)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"Identified crumb by child component: {other.gameObject.name}");
                }
                CollectCrumb(other.gameObject);
                return;
            }
            
            // If none of the above, assume it's chitin
            if (showDebugMessages)
            {
                Debug.Log($"Defaulting to chitin collection for: {other.gameObject.name}");
            }
            CollectChitin(other.gameObject);
        }
    }
    
    public void CollectChitin(GameObject chitinObject)
    {
        // Don't collect objects that are being animated
        if (chitinObject.name.Contains("CHITIN_ANIMATION_LOCK"))
        {
            Debug.Log($"Skipping collection of {chitinObject.name} - animation in progress");
            return;
        }
        
        // Check if the chitin object has the ChitinCollectible component
        ChitinCollectible chitinCollectible = chitinObject.GetComponent<ChitinCollectible>();
        
        if (chitinCollectible == null)
        {
            Debug.LogError("No ChitinCollectible component found on " + chitinObject.name);
            
            // Try to collect it anyway
            AddChitin(1);
            Destroy(chitinObject);
            return;
        }
        
        // Use the collectible's own collection logic and let it handle destruction
        bool collectionStarted = chitinCollectible.TryCollect();
        
        // If the collection animation started successfully, return immediately
        // and let the animation handle the destruction
        if (collectionStarted)
        {
            return;
        }
        
        // Only proceed with fallback logic if TryCollect() returned false
        // (which means collection animation didn't start)
        
        // Check if we're at max capacity before collecting
        if (IsChitinFull)
        {
            // Trigger the maxed event to notify UI/audio systems
            OnChitinMaxed?.Invoke();
            
            if (showDebugMessages)
            {
                Debug.Log($"Cannot collect chitin: inventory full ({ChitinCount}/{maxChitinCapacity})");
            }
            
            return;
        }
        
        // Increase chitin count
        ChitinCount++;
        
        // Play collection sound if assigned
        if (audioSource != null && collectionSound != null)
        {
            audioSource.PlayOneShot(collectionSound);
        }
        
        // Only destroy the object if the animation didn't start
        Destroy(chitinObject);
    }
    
    private void CollectCrumb(GameObject crumbObject)
    {
        // Try to use the collectible's own collection logic if available
        CrumbCollectible crumbCollectible = crumbObject.GetComponent<CrumbCollectible>();
        if (crumbCollectible != null)
        {
            crumbCollectible.TryCollect();
            return;
        }
        
        // Fallback to manual collection if no component found
        // Check if we're at max capacity before collecting
        if (IsCrumbFull)
        {
            // Trigger the maxed event to notify UI/audio systems
            OnCrumbMaxed?.Invoke();
            
            if (showDebugMessages)
            {
                Debug.Log($"Cannot collect crumb: inventory full ({CrumbCount}/{maxCrumbCapacity})");
            }
            
            return;
        }
        
        // Increase crumb count
        CrumbCount++;
        
        if (showDebugMessages)
        {
            Debug.Log($"Collected crumb: {CrumbCount}/{maxCrumbCapacity}");
        }
        
        // Play collection sound if assigned
        if (audioSource != null && collectionSound != null)
        {
            audioSource.PlayOneShot(collectionSound);
        }
        
        // Destroy the crumb object
        Destroy(crumbObject);
    }
    
    // Method to add crumbs (for testing or rewards)
    public bool AddCrumb(int amount)
    {
        // Don't allow adding more if we're at max capacity
        if (CrumbCount >= maxCrumbCapacity)
        {
            // Trigger the maxed event to notify UI/audio systems
            OnCrumbMaxed?.Invoke();
            
            // Log for debugging
            if (showDebugMessages)
            {
                Debug.Log($"Cannot add crumb: inventory full ({CrumbCount}/{maxCrumbCapacity})");
            }
            
            return false;
        }
        
        // Calculate how much we can actually add without exceeding max
        int amountToAdd = Mathf.Min(amount, maxCrumbCapacity - CrumbCount);
        
        // Add the crumbs
        CrumbCount += amountToAdd;
        
        // Notify listeners of the change
        OnCrumbCountChanged?.Invoke(CrumbCount);
        
        // Play pickup sound
        PlayPickupSound();
        
        return amountToAdd > 0;
    }
    
    // Remove crumbs from inventory
    public bool RemoveCrumb(int amount)
    {
        if (amount <= 0 || CrumbCount < amount) return false;
        
        CrumbCount -= amount;
        return true;
    }
    
    // Method to use crumbs (for feeding, etc.)
    public bool UseCrumb(int amount)
    {
        if (CrumbCount >= amount)
        {
            CrumbCount -= amount;
            OnCrumbCountChanged?.Invoke(CrumbCount);
            return true;
        }
        return false;
    }
    
    // Deposit crumbs at the base and gain XP
    public void DepositCrumbsAtBase(int amount)
    {
        if (amount <= 0 || CrumbCount < amount) return;
        // Remove crumbs from inventory
        CrumbCount -= amount;
        
        // Debug logs to track XP gains
        Debug.Log($"XP SYSTEM: Deposited {amount} crumbs");
    }
    
    // Deposit all crumbs at once
    public void DepositAllCrumbsAtBase()
    {
        int crumbsToDeposit = CrumbCount;
        DepositCrumbsAtBase(crumbsToDeposit);
    }
    
    // Method to use chitin (for crafting, etc.)
    public bool UseChitin(int amount)
    {
        if (ChitinCount >= amount)
        {
            ChitinCount -= amount;
            OnChitinCountChanged?.Invoke(ChitinCount);
            return true;
        }
        return false;
    }
    
    // Method to add chitin (for testing or rewards)
    public bool AddChitin(int amount)
    {
        // Don't allow adding more if we're at max capacity
        if (ChitinCount >= maxChitinCapacity)
        {
            // Trigger the maxed event to notify UI/audio systems
            OnChitinMaxed?.Invoke();
            
            // Log for debugging
            Debug.Log($"Cannot add chitin: inventory full ({ChitinCount}/{maxChitinCapacity})");
            
            return false;
        }
        
        // Calculate how much we can actually add without exceeding max
        int amountToAdd = Mathf.Min(amount, maxChitinCapacity - ChitinCount);
        
        // Add the chitin
        ChitinCount += amountToAdd;
        
        // Notify listeners of the change
        OnChitinCountChanged?.Invoke(ChitinCount);
        
        // Play pickup sound
        PlayPickupSound();
        
        return amountToAdd > 0;
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
        
        // Get previous XP and level for debugging
        int previousXP = _experience;
        int previousLevel = GetLevelFromExperience(previousXP);
        
        // Remove chitin from inventory
        ChitinCount -= amount;
        
        // Add experience only when explicitly depositing
        int xpGained = amount * experiencePerChitin;
        Experience += xpGained;
        
        // Check if level up occurred
        int newLevel = GetLevelFromExperience(_experience);
        
        // Dump the XP requirements for each level for reference
        string levelRequirements = "XP SYSTEM: Level requirements: ";
        for (int i = 1; i < xpRequirements.Length; i++)
        {
            levelRequirements += $"Level {i+1}={xpRequirements[i]} XP, ";
        }
        Debug.Log(levelRequirements);
    }
    
    // Deposit all chitin at once
    public void DepositAllChitinAtBase()
    {
        int chitinToDeposit = ChitinCount;
        DepositChitinAtBase(chitinToDeposit);
    }

    public void AddChitinFromEnemy(LivingEntity enemy)
    {
        if (enemy != null)
        {
            int amount = enemy.chitinAmount;
            Debug.Log($"AddChitinFromEnemy called with {amount} chitin from {enemy.name}");
            AddChitin(amount);
        }
        else
        {
            // Fallback if enemy reference is lost
            Debug.LogWarning("AddChitinFromEnemy called with null enemy");
            AddChitin(1);
        }
    }

    // Add this method to check if the issue is related to automatic deposits
    private void CheckExperienceGainIssue(int chitinCount)
    {
        // If we're using AddChitin but somehow it's also giving XP directly, 
        // let's log this occurrence
        Debug.Log($"Chitin count changed to {chitinCount}. If experience is increasing without deposits, there's an issue.");
    }

    // Add this method for debugging
    public void DebugDisplayLevelingStatus()
    {
        Debug.Log($"------ LEVELING DEBUG INFO ------");
        Debug.Log($"Current XP: {_experience}");
        Debug.Log($"Current Level: {CurrentLevel}");
        Debug.Log($"XP for next level: {ExperienceForNextLevel}");
        Debug.Log($"XP requirements array: {string.Join(", ", xpRequirements)}");
    }

    // Add this method to play pickup sound
    private void PlayPickupSound()
    {
        // Play pickup sound using SoundEffectManager if available
        if (useSoundEffectManager && SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound(pickupSoundEffectName, transform.position);
        }
        // Fallback to direct AudioSource if SoundEffectManager is not available
        else if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
    }
    
    // Add this method to play level up sound
    private void PlayLevelUpSound()
    {
        // Play level up sound using SoundEffectManager if available
        if (useSoundEffectManager && SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound(levelUpSoundEffectName, transform.position, false); // Use 2D sound for UI feedback
        }
        // Fallback to direct AudioSource if SoundEffectManager is not available
        else if (levelUpSound != null)
        {
            AudioSource.PlayClipAtPoint(levelUpSound, Camera.main.transform.position, 1.0f);
        }
    }

    public void ForceUpdateUI()
    {
        // Trigger UI update events
        if (OnChitinCountChanged != null)
            OnChitinCountChanged(ChitinCount);
        
        if (OnCrumbCountChanged != null)
            OnCrumbCountChanged(CrumbCount);
        
        if (OnExperienceChanged != null)
            OnExperienceChanged(TotalExperience);
        
        // Update level display
        if (OnLevelUp != null)
            OnLevelUp(CurrentLevel);
    }

    // Add this method to set loading data state
    public void SetLoadingDataState(bool isLoading)
    {
        IsLoadingData = isLoading;
    }

    // Modify your CheckLevelUp method to ignore level up during loading
    private void CheckLevelUp()
    {
        // Skip level up check if we're loading saved data
        if (IsLoadingData) return;
        
        // Existing level-up logic...
        // (All your current level-up detection code)
    }
} 