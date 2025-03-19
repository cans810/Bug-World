using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxChitinCapacity = 50;
    [SerializeField] private int maxCrumbCapacity = 5;
    
    [Header("Collection Settings")]
    [SerializeField] private string lootLayerName = "Loot";
    [SerializeField] private string carriedLootLayerName = "CarriedLoot";
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private AudioClip collectionSound;
    
    [Header("Experience Settings")]
    [SerializeField] private int experiencePerChitin = 2;
    
    [Header("Level System")]
    [SerializeField] private int maxLevel = 10;
    private int[] xpRequirements = new int[10] { 0, 20, 60, 110, 150, 210, 280, 350, 430, 510 };
    
    [Header("Sound Effects")]
    [SerializeField] private string pickupSoundEffectName = "Pickup2";
    [SerializeField] private string levelUpSoundEffectName = "LevelUp";
    [SerializeField] private bool useSoundEffectManager = true;
    [SerializeField] private AudioClip pickupSound; // Fallback sound if SoundEffectManager is not available
    [SerializeField] private AudioClip levelUpSound; // Fallback sound for level up
    
    [Header("First-time Collection")]
    [SerializeField] private bool hasYetToCollectChitin = true;  // Initially true
    [SerializeField] private bool hasYetToCollectCrumb = true;   // Initially true
    
    [Header("Egg Inventory")]
    [SerializeField] private int currentEggCount = 0;
    [SerializeField] private int maxEggCapacity = 1;
    
    [Header("Coin Inventory")]
    [SerializeField] public int currentCoinCount = 0;
    
    // Events that UI can subscribe to
    public event Action<int> OnChitinCountChanged;
    public event Action<int> OnCrumbCountChanged;
    public event Action<int> OnExperienceChanged;
    public event Action<int> OnLevelUp;
    public event Action OnChitinMaxed;
    public event Action OnCrumbMaxed;
    public event System.Action<int> OnLevelDisplayUpdate;
    public event Action<int> OnCoinCountChanged;
    
    // Inventory data
    public int _chitinCount = 0;
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
    public int _crumbCount = 0;
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
    public int _experience = 0;
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
                
                // Handle the level-up sequence with proper flow
                if (!isLoadingData) // Skip animations if loading data
                {
                    HandleLevelUpSequence(newLevel);
                }
                
                if (showDebugMessages)
                {
                    Debug.Log($"Level up! Now level {newLevel}");
                }
            }
        }
    }
    
    // Level system properties
    private int _currentLevel = 1;
    public int CurrentLevel
    {
        get => _currentLevel;
        private set
        {
            _currentLevel = Mathf.Clamp(value, 1, maxLevel);
            OnLevelDisplayUpdate?.Invoke(CurrentLevel);
        }
    }
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
    
    // Add properties to access these
    public bool HasYetToCollectChitin { get => hasYetToCollectChitin; set => hasYetToCollectChitin = value; }
    public bool HasYetToCollectCrumb { get => hasYetToCollectCrumb; set => hasYetToCollectCrumb = value; }
    
    // Add properties for egg inventory
    public int CurrentEggCount { get => currentEggCount; set => currentEggCount = Mathf.Clamp(value, 0, maxEggCapacity); }
    public int MaxEggCapacity { get => maxEggCapacity; set => maxEggCapacity = value; }
    
    // Add coin property
    public int CoinCount
    {
        get => currentCoinCount;
        private set
        {
            currentCoinCount = Mathf.Max(0, value);
            OnCoinCountChanged?.Invoke(currentCoinCount);
        }
    }
    
    // Add this field to the PlayerInventory class (near the other private fields)
    private bool isLoadingData = false;

    // Add public property to access the loading state
    public bool IsLoadingData 
    { 
        get { return isLoadingData; } 
        private set { isLoadingData = value; }
    }

    // Called when game starts
    private void Start()
    {
        // Initialize inventory
        ChitinCount = 0;
        CrumbCount = 0;
        Experience = 0;
        
        // Add diagnostic listener
        OnChitinCountChanged += CheckExperienceGainIssue;
        
        // Sync with PlayerAttributes and AntIncubator
        SyncWithPlayerAttributes();
        SyncWithInsectIncubator();  
    }
    
    private void OnDestroy()
    {
        // Remove diagnostic listener
        OnChitinCountChanged -= CheckExperienceGainIssue;
    }
    
    // Get the level based on current experience
    public int GetLevelFromExperience(int experience)
    {
        for (int i = maxLevel - 1; i >= 0; i--)
        {
            if (experience >= xpRequirements[i])
            {
                return i + 1; // Level is 1-based (level 1, 2, 3, etc.)
            }
        }
        return 1; // Default to level 1
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
        // Check if this is the first time collection
        if (hasYetToCollectCrumb)
        {
            Debug.Log("Player has yet to collect crumb! Showing info panel...");
            
            // Show the info panel
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            Debug.Log($"UIHelper found: {uiHelper != null}");
            
            if (uiHelper != null && uiHelper.ItemInformPanel != null)
            {
                Debug.Log($"ItemInformPanel found: {uiHelper.ItemInformPanel != null}");
                
                // Set up the tutorial/info panel
                ItemInformController itemInfo = uiHelper.ItemInformPanel.GetComponent<ItemInformController>();
                Debug.Log($"ItemInformController found: {itemInfo != null}");
                
                if (itemInfo != null)
                {
                    Debug.Log("Calling ShowCrumbInfo on ItemInformController");
                    itemInfo.ShowCrumbInfo();
                    
                    // Set the resource type so the panel knows which flag to set when closed
                    itemInfo.SetResourceType(ItemInformController.ResourceType.Crumb);
                }
                else
                {
                    Debug.LogWarning("ItemInformController not found on ItemInformPanel!");
                    // If we can't show the panel, mark as collected anyway
                    hasYetToCollectCrumb = false;
                }
            }
            else
            {
                Debug.LogWarning($"Cannot show info panel. UIHelper: {uiHelper != null}, ItemInformPanel: {(uiHelper != null ? uiHelper.ItemInformPanel != null : false)}");
                // If we can't show the panel, mark as collected anyway
                hasYetToCollectCrumb = false;
            }
        }
        
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
        // Check if this is the first time collection
        if (hasYetToCollectChitin)
        {
            Debug.Log("Player has yet to collect chitin! Showing info panel...");
            
            // Show the info panel
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            Debug.Log($"UIHelper found: {uiHelper != null}");
            
            if (uiHelper != null && uiHelper.ItemInformPanel != null)
            {
                Debug.Log($"ItemInformPanel found: {uiHelper.ItemInformPanel != null}");
                
                // Set up the tutorial/info panel
                ItemInformController itemInfo = uiHelper.ItemInformPanel.GetComponent<ItemInformController>();
                Debug.Log($"ItemInformController found: {itemInfo != null}");
                
                if (itemInfo != null)
                {
                    Debug.Log("Calling ShowChitinInfo on ItemInformController");
                    itemInfo.ShowChitinInfo();
                    
                    // Set the flag to false after showing the panel - we'll let the panel handle this
                    itemInfo.SetResourceType(ItemInformController.ResourceType.Chitin);
                }
                else
                {
                    Debug.LogWarning("ItemInformController not found on ItemInformPanel!");
                    // If we can't show the panel, mark as collected anyway
                    hasYetToCollectChitin = false;
                }
            }
            else
            {
                Debug.LogWarning($"Cannot show info panel. UIHelper: {uiHelper != null}, ItemInformPanel: {(uiHelper != null ? uiHelper.ItemInformPanel != null : false)}");
                // If we can't show the panel, mark as collected anyway
                hasYetToCollectChitin = false;
            }
        }
        
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
        
        // Update level display but only fire level up event if not loading data
        if (OnLevelUp != null && !IsLoadingData)
            OnLevelUp(CurrentLevel);
        // If we're loading data, just update the level text without triggering animations
        else if (OnLevelDisplayUpdate != null && IsLoadingData)
            OnLevelDisplayUpdate(CurrentLevel);
        else if (OnLevelUp != null)
            OnLevelUp(CurrentLevel); // Fallback to using OnLevelUp for backward compatibility
        
        // Update egg display
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(currentEggCount);
        }
        
        if (OnCoinCountChanged != null)
            OnCoinCountChanged(CoinCount);
    }

    // Add this method to PlayerInventory
    public void SetLoadingDataState(bool loading)
    {
        isLoadingData = loading;
        Debug.Log($"PlayerInventory loading state set to: {loading}");
    }

    // Modify your CheckLevelUp method to ignore level up during loading
    private void CheckLevelUp()
    {
        // Skip level up check if we're loading saved data
        if (IsLoadingData) return;
        
        // Existing level-up logic...
        // (All your current level-up detection code)
    }

    // Modify the DepositChitin method to not update UI automatically
    public void DepositChitin(int amount)
    {
        if (amount <= 0 || ChitinCount < amount) return;
        
        // Always calculate XP as 2 per chitin, regardless of other settings
        int xpGain = amount * 2;
        
        Debug.Log($"DepositChitin called with {amount} chitin for {xpGain} XP");
        
        // Add experience but don't trigger UI update
        _experience += xpGain; // Add directly to the field, not property
        
        // Debug log
        Debug.Log($"Deposited {amount} chitin for {xpGain} XP");
        
        // Remove the deposited chitin
        ChitinCount -= amount;
    }

    // Add this method to PlayerInventory.cs
    public void AddExperience(int amount)
    {
        if (amount <= 0) return;
        
        // Add debug log with stack trace to find where this is called from
        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
        Debug.Log($"AddExperience called with {amount} XP from:\n{stackTrace}");
        
        // Get previous XP and level for debugging
        int previousXP = _experience;
        int previousLevel = GetLevelFromExperience(previousXP);
        
        // Add the experience
        Experience += amount;
        
        Debug.Log($"Added {amount} experience. Total: {_experience}");
        
        // Check if level up occurred (for debugging)
        int newLevel = GetLevelFromExperience(_experience);
        if (newLevel > previousLevel)
        {
            Debug.Log($"Level up from {previousLevel} to {newLevel}!");
        }
    }

    // Modify the AddXPIncremental method
    public void AddXPIncremental(int amount)
    {
        if (amount <= 0) return;
        
        // Add the experience one by one
        Experience += amount;
        
        // Update UI with animation
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            // This will trigger the animated counter
            uiHelper.UpdateExperienceDisplay(_experience, true);
        }
        
        // Debug log
        Debug.Log($"Added {amount} XP incrementally. Total: {_experience}");
    }

    // Add this method to update egg count
    public bool AddEgg(int amount)
    {
        if (amount <= 0) return false;
        
        // Don't allow adding more if we're at max capacity
        if (currentEggCount >= maxEggCapacity)
        {
            // Log for debugging
            if (showDebugMessages)
            {
                Debug.Log($"Cannot add egg: inventory full ({currentEggCount}/{maxEggCapacity})");
            }
            
            return false;
        }
        
        // Calculate how much we can actually add without exceeding max
        int amountToAdd = Mathf.Min(amount, maxEggCapacity - currentEggCount);
        
        // Add the eggs
        currentEggCount += amountToAdd;
        
        // Play pickup sound
        PlayPickupSound();
        
        return amountToAdd > 0;
    }

    // Add this method to remove eggs
    public bool RemoveEgg(int amount)
    {
        if (amount <= 0 || currentEggCount < amount) return false;
        
        currentEggCount -= amount;
        return true;
    }

    // Add this method to set egg capacity (called from PlayerAttributes)
    public void UpdateEggCapacity(int newCapacity)
    {
        maxEggCapacity = Mathf.Max(1, newCapacity);
        currentEggCount = Mathf.Min(currentEggCount, maxEggCapacity);
    }

    // Add these methods to handle saving and loading egg data
    public void SaveEggData(GameData saveData)
    {
        saveData.currentEgg = currentEggCount;
        saveData.maxEggCapacity = maxEggCapacity;
    }

    public void LoadEggData(GameData saveData)
    {
        currentEggCount = saveData.currentEgg;
        maxEggCapacity = saveData.maxEggCapacity;
        
        Debug.Log($"Loaded egg data from save: Count = {currentEggCount}, Max Capacity = {maxEggCapacity}");
        
        // Immediately update the AntIncubator
        UpdateAntIncubator();
    }

    // Add this method to sync with PlayerAttributes
    public void SyncWithPlayerAttributes()
    {
        PlayerAttributes playerAttributes = GetComponent<PlayerAttributes>();
        if (playerAttributes != null)
        {
            // Update max egg capacity from PlayerAttributes
            maxEggCapacity = playerAttributes.MaxEggCapacity;
            
            // Ensure current count doesn't exceed max capacity
            currentEggCount = Mathf.Min(currentEggCount, maxEggCapacity);
            
            // Debug log to verify sync
            Debug.Log($"PlayerInventory synced with PlayerAttributes: Max Egg Capacity = {maxEggCapacity}");
        }
    }

    // Add this method to sync with AntIncubator
    public void SyncWithAntIncubator()
    {
        AntIncubator incubator = FindObjectOfType<AntIncubator>();
        if (incubator != null)
        {
            // Get the current egg count from the incubator
            int incubatorEggCount = incubator.GetCurrentEggCount();
            
            // Update our egg count to match
            currentEggCount = incubatorEggCount;
            
            // Debug log to verify sync
            Debug.Log($"PlayerInventory synced with AntIncubator: Egg Count = {currentEggCount}");
        }
    }

    // Add this method to update AntIncubator with current egg count
    public void UpdateAntIncubator()
    {
        AntIncubator incubator = FindObjectOfType<AntIncubator>();
        if (incubator != null)
        {
            incubator.SetEggCount(currentEggCount);
            Debug.Log($"Updated AntIncubator with egg count: {currentEggCount}");
            
            // Also update the UI
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.UpdateEggDisplay(currentEggCount);
            }
        }
    }

    // Method to increase chitin capacity
    public void IncreaseChitinCapacity(int amount)
    {
        if (amount <= 0) return;
        
        maxChitinCapacity += amount;
        Debug.Log($"Increased chitin capacity by {amount}. New max: {maxChitinCapacity}");
        
        // Notify UI if necessary
        OnChitinCountChanged?.Invoke(ChitinCount);
    }

    // Method to increase crumb capacity
    public void IncreaseCrumbCapacity(int amount)
    {
        if (amount <= 0) return;
        
        maxCrumbCapacity += amount;
        Debug.Log($"Increased crumb capacity by {amount}. New max: {maxCrumbCapacity}");
        
        // Notify UI if necessary
        OnCrumbCountChanged?.Invoke(CrumbCount);
    }

    // Add methods to add and remove coins
    public bool AddCoins(int amount)
    {
        if (amount <= 0) 
        {
            Debug.LogWarning($"Attempted to add invalid coin amount: {amount}");
            return false;
        }
        
        CoinCount += amount;
        
        // Play pickup sound
        PlayPickupSound();
        
        Debug.Log($"Added {amount} coins. New total: {CoinCount}");
        return true;
    }
    
    public bool RemoveCoins(int amount)
    {
        if (amount <= 0 || CoinCount < amount) return false;
        
        CoinCount -= amount;
        return true;
    }

    // Add these methods to PlayerInventory
    public void SetExperience(int value)
    {
        _experience = value;
        // Don't trigger events here to avoid level-up effects
        Debug.Log($"Set experience directly to {value}");
    }

    public void SetChitinCount(int value)
    {
        _chitinCount = Mathf.Clamp(value, 0, maxChitinCapacity);
        Debug.Log($"Set chitin count directly to {_chitinCount}");
    }

    public void SetCrumbCount(int value)
    {
        _crumbCount = Mathf.Clamp(value, 0, maxCrumbCapacity);
        Debug.Log($"Set crumb count directly to {_crumbCount}");
    }

    public void SetCoinCount(int value)
    {
        CoinCount = value; // Use the property to ensure events are triggered
        Debug.Log($"Set coin count directly to {CoinCount}");
    }

    // Add this method to properly handle level-up sequence
    private void HandleLevelUpSequence(int newLevel)
    {
        // Log the level-up
        Debug.Log($"Level up! Player is now level {newLevel}");
        
        // Check if there's a new area unlocked at this level
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null)
        {
            bool newAreaUnlocked = cameraAnimations.IsNewAreaUnlockedAtLevel(newLevel);
            
            if (newAreaUnlocked)
            {
                // If there's a new area, show it first, and the attributes panel will be shown after
                Debug.Log($"New area unlocked at level {newLevel}, showing camera animation");
                cameraAnimations.ShowAreaByLevel(newLevel);
                
                // The attributes panel will be shown by the OnCameraAnimationCompleted event
            }
            else
            {
                // If there's no new area, just show the attributes panel immediately
                Debug.Log($"No new area at level {newLevel}, showing attributes panel directly");
                AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
                if (attributeDisplay != null)
                {
                    attributeDisplay.ShowPanel(true);
                }
            }
        }
        else
        {
            // Fallback if there's no camera animations component
            Debug.Log("No CameraAnimations component found, showing attributes panel directly");
            AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
            if (attributeDisplay != null)
            {
                attributeDisplay.ShowPanel(true);
            }
        }
    }

    // Modify this method to not add attribute points directly
    public void CheckAndTriggerLevelUp()
    {
        // Get the current level based on experience
        int newLevel = GetLevelFromExperience(_experience);
        
        // Only proceed if this is actually a new level
        if (newLevel > CurrentLevel)
        {
            Debug.Log($"Manual level up triggered: {CurrentLevel} → {newLevel}");
            
            // Use the property to ensure events are triggered
            CurrentLevel = newLevel;
            
            // Trigger the level up event - THIS WILL HANDLE ADDING ATTRIBUTE POINTS
            OnLevelUp?.Invoke(newLevel);
            
            // Play level up sound
            PlayLevelUpSound();
            
            // Handle any other level up logic needed
            HandleLevelUpSequence(newLevel);
        }
    }

    public void SetLevel(int level)
    {
        // Use the property to ensure events are triggered
        CurrentLevel = level;
        Debug.Log($"Set player level directly to {CurrentLevel}");
        
        // Also invoke the level display update event explicitly to ensure UI updates
        OnLevelDisplayUpdate?.Invoke(CurrentLevel);
    }

    // Add these methods to PlayerInventory class
    public void SetMaxChitinCapacity(int capacity)
    {
        maxChitinCapacity = Mathf.Max(1, capacity);
        Debug.Log($"Set max chitin capacity to {maxChitinCapacity}");
        
        // Optional: Trigger UI update
        OnChitinCountChanged?.Invoke(_chitinCount);
    }

    public void SetMaxCrumbCapacity(int capacity)
    {
        maxCrumbCapacity = Mathf.Max(1, capacity);
        Debug.Log($"Set max crumb capacity to {maxCrumbCapacity}");
        
        // Optional: Trigger UI update
        OnCrumbCountChanged?.Invoke(_crumbCount);
    }
} 