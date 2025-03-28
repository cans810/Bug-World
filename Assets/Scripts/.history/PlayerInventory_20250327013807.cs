using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] public int maxChitinCapacity = 30;
    [SerializeField] public int maxCrumbCapacity = 1;
    
    [Header("Collection Settings")]
    [SerializeField] private string lootLayerName = "Loot";
    [SerializeField] private string carriedLootLayerName = "CarriedLoot";
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private AudioClip collectionSound;
    
    [Header("Experience Settings")]
    [SerializeField] private int experiencePerChitin = 2;
    
    [Header("Level System")]
    [SerializeField] private int maxLevel = 55;
    private int[] xpRequirements = new int[55] { 
        0, 30, 75, 125, 200, 290, 430, 560, 720, 870, 1070, 1300, 
        1570, 1870, 2200, 2560, 2950, 3370, 3820, 4300, 4810, 5350, 5920, 6520, 
        7150, 7810, 8500, 9220, 9970, 10750, 11560, 12400, 13270, 14170, 15100, 
        16060, 17050, 18070, 19120, 20200, 21310, 22450, 23620, 24820, 26050, 
        27310, 28600, 29920, 31270, 32650, 34060, 35500, 36970, 38470, 40000
    };
    
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
                Debug.Log($"PlayerInventory: Level up from {oldLevel} to {newLevel}");
                
                // IMPORTANT: Explicitly check for barrier removal at level 31
                if (newLevel == 31)
                {
                    Debug.Log("*** LEVEL 31 REACHED - CHECKING FOR BARRIER REMOVAL ***");
                    CheckForRockBarrierRemoval(newLevel);
                }
                
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
    public bool IsMaxLevel 
    {
        get 
        {
            bool result = CurrentLevel >= maxLevel;
            Debug.Log($"IsMaxLevel check: CurrentLevel={CurrentLevel}, maxLevel={maxLevel}, result={result}");
            return result;
        }
    }
    
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
        
        // Sync with PlayerAttributes and InsectIncubator
        SyncWithPlayerAttributes();
        SyncWithInsectIncubator();
        
        // Add to Start() method
        Debug.Log($"XP Requirements array has {xpRequirements.Length} elements. Max level set to {maxLevel}");
        for (int i = 1; i <= 15; i++) {
            Debug.Log($"XP required for level {i}: {GetXPRequirementForLevel(i-1)}");
        }

        // Add our level-up handler explicitly
        OnLevelUp += OnLevelChanged;
        
        // Call the check immediately if we're already level 31+
        if (CurrentLevel >= 31)
        {
            Debug.Log($"Already level {CurrentLevel} at start. Checking barriers...");
            StartCoroutine(TryRemoveRockBarriersWithRetry());
        }
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
        
        // Notify mission manager
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnItemCollected("chitin", 1);
        }
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
        
        // Notify mission manager
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnItemCollected("crumb", 1);
        }
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
                    
                    // Set the resource type so the panel knows which flag to set when closed
                    itemInfo.SetResourceType(ItemInformController.ResourceType.Chitin);
                    
                    // Show arrow to the nest using ChitinDepositArrowManager
                    ChitinDepositArrowManager arrowManager = FindObjectOfType<ChitinDepositArrowManager>();
                    if (arrowManager != null)
                    {
                        // Force show the arrow
                        arrowManager.ForceShowArrow();
                        
                        // Show inform text about depositing with updated message
                        if (uiHelper != null)
                        {
                            uiHelper.ShowInformText("Go to your nest to deposit your chitin.");
                        }
                    }
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

    // Update the DepositChitin method to use our gradual XP approach
    public void DepositChitin(int amount)
    {
        if (amount <= 0) return;
        
        // Calculate XP gain (2 XP per chitin)
        int xpGain = amount * experiencePerChitin;
        
        // Remove chitin from inventory
        _chitinCount = Mathf.Max(0, _chitinCount - amount);
        
        // Notify UI that chitin count has changed
        OnChitinCountChanged?.Invoke(_chitinCount);
        
        // Add XP gradually
        AddExperience(xpGain);
        
        if (showDebugMessages)
        {
            Debug.Log($"Deposited {amount} chitin for {xpGain} XP. New chitin count: {_chitinCount}");
        }
    }

    // Replace the current AddExperience method with this version that prevents level skipping
    public void AddExperience(int amount)
    {
        // Skip if we're loading data
        if (isLoadingData) return;
        
        // Instead of adding all XP at once, start a coroutine to add it gradually
        StartCoroutine(AddExperienceGradually(amount));
    }

    // Add this coroutine to add experience gradually, level by level
    private IEnumerator AddExperienceGradually(int amount)
    {
        DebugEventSubscriptions(); // Add this line to check subscriptions
        
        if (amount <= 0) yield break;
        
        int targetExperience = _experience + amount;
        int currentTargetExp = _experience;
        
        // Get the current level
        int startLevel = GetLevelFromExperience(_experience);
        int targetLevel = GetLevelFromExperience(targetExperience);
        
        Debug.Log($"Adding XP gradually from level {startLevel} to {targetLevel} (XP {_experience} to {targetExperience})");
        
        // If no level-up will occur, just add the XP directly
        if (targetLevel <= startLevel)
        {
            _experience = targetExperience;
            OnExperienceChanged?.Invoke(_experience);
            yield break;
        }
        
        // Process one level at a time
        for (int level = startLevel + 1; level <= targetLevel; level++)
        {
            // Calculate how much XP is needed to reach this level
            int xpForThisLevel = GetXPRequirementForLevel(level - 1);
            
            // Determine how much XP to add to reach the current target level
            currentTargetExp = xpForThisLevel;
            
            // Add XP to reach this level
            _experience = currentTargetExp;
            
            // Update the player's level for this step
            CurrentLevel = level;
            
            // Add level up rewards here
            // maxChitinCapacity += 10;  // Increase chitin capacity
            // maxCrumbCapacity += 1;    // Increase crumb capacity
            
            // Add attribute points
            PlayerAttributes playerAttributes = GetComponent<PlayerAttributes>();
            if (playerAttributes != null)
            {
                playerAttributes.availablePoints += 2;  // Directly increase available points
            }
            
            Debug.Log($"Level {level} rewards: +15 Chitin Capacity, +2 Crumb Capacity, +2 Attribute Points");
            
            // Notify UI that XP has changed
            OnExperienceChanged?.Invoke(_experience);
        
            
            // Check if this level has an area unlock
            bool hasAreaUnlock = CheckForAreaUnlock(level);
            
            // If this level has an area unlock, wait for animation to complete
            if (hasAreaUnlock)
            {
                Debug.Log($"Waiting for area unlock animation at level {level}");
                
                // Wait for camera animation to complete
                CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
                if (cameraAnimations != null)
                {
                    // Wait for animation plus a small extra delay
                    float startTime = Time.time;
                    
                    // Wait until animation is no longer in progress or a maximum of 10 seconds has passed
                    while (cameraAnimations.IsAnimationInProgress() && (Time.time - startTime < 10f))
                    {
                        yield return null;
                    }
                    
                    // Add a small buffer after animation completes
                    yield return new WaitForSeconds(1.0f);
                }
                else
                {
                    // Fallback wait time if camera animations reference is missing
                    yield return new WaitForSeconds(8f);
                }
            }
            else
            {
                // Small delay between level-ups with no area unlocks
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        // Add any remaining XP to reach the final target
        if (_experience < targetExperience)
        {
            _experience = targetExperience;
            OnExperienceChanged?.Invoke(_experience);
        }

        Debug.Log($"Finished adding XP gradually. Level now {CurrentLevel}, XP now {_experience}");
    }
    // Add this helper method to hide the rewards text
    private IEnumerator HideLevelUpRewardsTextAfterDelay(UIHelper uiHelper, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Hide the text
        if (uiHelper != null && uiHelper.onLevelUpThingsGainedText != null)
        {
            uiHelper.onLevelUpThingsGainedText.gameObject.SetActive(false);
        }
    }

    // Check if a level has an area unlock
    private bool CheckForAreaUnlock(int level)
    {
        LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
        if (arrowManager == null) return false;
        
        // Use reflection to access the private levelAreas field
        var areasField = typeof(LevelAreaArrowManager).GetField("levelAreas", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
            
        if (areasField == null) return false;
        
        var areas = areasField.GetValue(arrowManager) as LevelAreaArrowManager.LevelAreaTarget[];
        if (areas == null) return false;
        
        // Check if any area requires this level and hasn't been visited
        foreach (var area in areas)
        {
            if (area.requiredLevel == level && !area.hasBeenVisited && area.areaTarget != null)
            {
                Debug.Log($"Found area unlock at level {level}: {area.areaName}");
                return true;
            }
        }
        
        return false;
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
        
        // Immediately update the InsectIncubator
        UpdateInsectIncubator();
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

    // Add this method to sync with InsectIncubator
    public void SyncWithInsectIncubator()
    {
        InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
        if (incubator != null)
        {
            // Get the current egg count from the incubator
            int incubatorEggCount = incubator.GetCurrentEggCount();
            
            // Update our egg count to match
            currentEggCount = incubatorEggCount;
            
            // Debug log to verify sync
            Debug.Log($"PlayerInventory synced with InsectIncubator: Egg Count = {currentEggCount}");
        }
    }

    // Add this method to update InsectIncubator with current egg count
    public void UpdateInsectIncubator()
    {
        InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
        if (incubator != null)
        {
            incubator.SetEggCount(currentEggCount);
            Debug.Log($"Updated InsectIncubator with egg count: {currentEggCount}");
            
            // Also update the UI
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.UpdateEggDisplay(currentEggCount);
            }
        }
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
    public void SetExperience(int amount)
    {
        _experience = amount;
        OnExperienceChanged?.Invoke(_experience);
        
        // Update the current level based on the new experience
        CurrentLevel = GetLevelFromExperience(_experience);
        
        Debug.Log($"Experience directly set to {amount}, level is now {CurrentLevel}");
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
        Debug.Log($"Starting level-up sequence for level {newLevel}");
        
        // Existing code...
        
        // Important: Explicit check for barrier removal
        CheckForRockBarrierRemoval(newLevel);
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
        CurrentLevel = Mathf.Clamp(level, 1, maxLevel);
        OnLevelDisplayUpdate?.Invoke(CurrentLevel);
        
        Debug.Log($"Level directly set to {CurrentLevel}");
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

    // Get XP requirement for a specific level
    public int GetXPRequirementForLevel(int level)
    {
        if (level < 0 || level >= xpRequirements.Length)
            return 0;
        
        return xpRequirements[level];
    }

    // Get the maximum level
    public int GetMaxLevel()
    {
        return maxLevel;
    }

    // Add this debug method and call it whenever the level changes
    private void DebugLevelInfo()
    {
        Debug.Log($"LEVEL INFO: CurrentLevel={CurrentLevel}, MaxLevel={maxLevel}, " +
                  $"IsMaxLevel={IsMaxLevel}, TotalExp={TotalExperience}, " +
                  $"RequiredForNextLevel={ExperienceForNextLevel}");
    }

    // Add this after loading saved data
    private void VerifyAttributePointsAfterLoad()
    {
        PlayerAttributes playerAttributes = GetComponent<PlayerAttributes>();
        if (playerAttributes != null)
        {
            // Calculate expected attribute points based on level
            int expectedPoints = (CurrentLevel - 1) * playerAttributes.pointsPerLevel;
            int actualPoints = playerAttributes.AvailablePoints + 
                              playerAttributes.StrengthPoints + 
                              playerAttributes.VitalityPoints +
                              playerAttributes.AgilityPoints +
                              playerAttributes.IncubationPoints +
                              playerAttributes.RecoveryPoints +
                              playerAttributes.SpeedPoints;
            
            if (actualPoints < expectedPoints)
            {
                int missingPoints = expectedPoints - actualPoints;
                Debug.LogWarning($"Found {missingPoints} missing attribute points after load. Adding them.");
                playerAttributes.AddAttributePoints(missingPoints);
            }
            
            // Debug current state
            playerAttributes.DebugAttributePoints();
        }
    }

    // Add this debug method to check for duplicate event handlers
    private void DebugEventSubscriptions()
    {
        // Count OnLevelUp subscribers using reflection
        if (OnLevelUp != null)
        {
            var delegates = OnLevelUp.GetInvocationList();
            Debug.Log($"<color=magenta>OnLevelUp has {delegates.Length} subscribers:</color>");
            foreach (var d in delegates)
            {
                Debug.Log($" - {d.Target?.GetType().Name}.{d.Method.Name}");
            }
        }
        else
        {
            Debug.Log("<color=magenta>OnLevelUp has no subscribers</color>");
        }
    }

    // When items are added to inventory
    public void AddItems(string itemType, int amount)
    {
        // Existing item collection code...
        
        // Notify mission manager
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnItemCollected(itemType, amount);
        }
    }

    // Simplified barrier removal animation sequence
    private IEnumerator PlayBarrierRemovalAnimation(BarrierRocksController barrier)
    {
        Debug.Log("<color=cyan>Starting barrier removal sequence</color>");
        
        // Get references
        Camera mainCamera = Camera.main;
        Transform playerTransform = gameObject.transform;
        
        // *** IMPORTANT: Disable the camera controller during animation ***
        CameraController cameraController = mainCamera.GetComponent<CameraController>();
        bool wasControllerEnabled = false;
        
        if (cameraController != null)
        {
            Debug.Log("<color=cyan>Disabling camera controller during animation</color>");
            wasControllerEnabled = cameraController.enabled;
            cameraController.enabled = false;
        }
        else
        {
            Debug.LogWarning("No CameraController found on main camera");
        }
        
        // Store original camera position and rotation
        Vector3 originalCameraPosition = mainCamera.transform.position;
        Quaternion originalCameraRotation = mainCamera.transform.rotation;
        
        // Get barrier position
        Vector3 barrierPosition = barrier.transform.position;
        
        // Modified: Calculate an even more dramatic, wider angle viewing position
        Vector3 directionToBarrier = (barrierPosition - playerTransform.position).normalized;
        
        // Further increase height and distance for an even wider view
        Vector3 viewPosition = barrierPosition - directionToBarrier * 20f + Vector3.up * 20f;
        
        // Adjust rotation for a more dramatic top-down view
        Quaternion viewRotation = Quaternion.LookRotation((barrierPosition - viewPosition).normalized);
        
        // Increase the FOV significantly for a much wider perspective
        float originalFOV = mainCamera.fieldOfView;
        float cinematicFOV = 75f; // Even wider FOV (default is usually 60)
        
        // Disable player controls
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;
        
        // Show message
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
            uiHelper.ShowInformText("You reached level 31, your path is clear!");

        // Play level 31 sound
        if (SoundEffectManager.Instance != null)
            barrier.PlayLevel31Sound();
        
        // STEP 1: Move camera to view position with FOV change
        Debug.Log("<color=cyan>STEP 1: Moving camera to view barrier</color>");
        float moveDuration = 2.0f;
        float elapsed = 0;
        
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / moveDuration);
            
            mainCamera.transform.position = Vector3.Lerp(originalCameraPosition, viewPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(originalCameraRotation, viewRotation, t);
            mainCamera.fieldOfView = Mathf.Lerp(originalFOV, cinematicFOV, t);
            
            yield return null;
        }
        
        // Make sure camera is exactly at target position
        mainCamera.transform.position = viewPosition;
        mainCamera.transform.rotation = viewRotation;
        
        // STEP 2: Brief pause before animation
        Debug.Log("<color=cyan>STEP 2: Brief pause before animation</color>");
        yield return new WaitForSeconds(0.5f);
        
        // STEP 3: Start disappear animation and play sound
        Debug.Log("<color=cyan>STEP 3: Starting disappear animation</color>");
        barrier.RemoveBarrier();
        
        // STEP 4: Wait exactly 3 seconds to view animation
        Debug.Log("<color=cyan>STEP 4: Watching animation for 3 seconds</color>");
        yield return new WaitForSeconds(3.0f);
        
        // STEP 5: Move camera back to player with FOV reset
        Debug.Log("<color=cyan>STEP 5: Moving camera back to player</color>");
        elapsed = 0;
        float returnDuration = 2.0f;
        
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / returnDuration);
            
            mainCamera.transform.position = Vector3.Lerp(viewPosition, originalCameraPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(viewRotation, originalCameraRotation, t);
            mainCamera.fieldOfView = Mathf.Lerp(cinematicFOV, originalFOV, t);
            
            yield return null;
        }
        
        // Make sure FOV is reset
        mainCamera.fieldOfView = originalFOV;
        
        // STEP 6: Destroy barrier after camera returns
        Debug.Log("<color=cyan>STEP 6: Destroying barrier object</color>");
        if (barrier != null)
            barrier.DestroyBarrier();
        
        // Re-enable player controls
        if (playerController != null)
            playerController.enabled = true;
        
        // Show completion message
        if (uiHelper != null)
            uiHelper.ShowInformText("The rock barrier has been cleared!");
        
        // Save the state
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SetRockBarriersRemoved(true);
            gameManager.SaveGame();
        }
        
        // Re-enable the camera controller
        if (cameraController != null)
        {
            Debug.Log("<color=cyan>Re-enabling camera controller</color>");
            cameraController.enabled = wasControllerEnabled;
        }
    }

    // Update the CheckForRockBarrierRemoval method to use level 31
    private void CheckForRockBarrierRemoval(int newLevel)
    {
        // Log the current level check for debugging
        Debug.Log($"Checking for rock barrier removal at level {newLevel}");
        
        // Check if player has reached level 31 for rock barrier removal (changed from 30)
        if (newLevel >= 31)
        {
            Debug.Log("Level 31+ detected - looking for barrier rocks to remove");
            
            // Find the single barrier rocks controller
            BarrierRocksController rockBarrier = FindObjectOfType<BarrierRocksController>();
            
            if (rockBarrier != null)
            {
                Debug.Log($"Found barrier: {rockBarrier.gameObject.name} - starting removal animation");
                
                // Start the camera animation coroutine
                StartCoroutine(PlayBarrierRemovalAnimation(rockBarrier));
            }
            else
            {
                Debug.LogWarning("No barrier rocks found to remove, or it was already removed");
                
                // Still save the state even if barrier not found (might have been removed already)
                GameManager gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.SetRockBarriersRemoved(true);
                    gameManager.SaveGame();
                    Debug.Log("Rock barrier removal state saved anyway in case it was already removed");
                }
            }
        }
    }

    // Modify the OnLevelChanged method to delay barrier removal if needed
    private void OnLevelChanged(int newLevel)
    {
        Debug.Log($"<color=yellow>LEVEL CHANGE DETECTED: {newLevel}</color>");

        PlayerAttributes playerAttributes = GetComponent<PlayerAttributes>();
        PlayerController playerController = GetComponent<PlayerController>();

        // Add attribute points
        playerAttributes.availablePoints += 2;
        
        // Increase capacities and notify UI
        maxChitinCapacity += 10;
        maxCrumbCapacity += 1;
        
        // Trigger UI updates for chitin and crumb capacity changes
        OnChitinCountChanged?.Invoke(_chitinCount);
        OnCrumbCountChanged?.Invoke(_crumbCount);
        
        // Check for metamorphosis
        if (playerController != null)
        {
            playerController.CheckMetamorphosis(newLevel);
        }
        
        // Check for level 31 specifically for rock barriers
        if (newLevel >= 31)
        {
            Debug.Log($"<color=green>LEVEL {newLevel} REACHED - CHECKING FOR ROCK BARRIERS</color>");
            StartCoroutine(WaitForAreaUnlockThenRemoveBarrier());
        }
    }

    // New coroutine to sequence the animations properly
    private IEnumerator WaitForAreaUnlockThenRemoveBarrier()
    {
        Debug.Log("<color=orange>Checking if area unlock animation is in progress</color>");
        
        // Check if there's a camera animation in progress
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        bool animationWasInProgress = false;
        
        if (cameraAnimations != null && cameraAnimations.IsAnimationInProgress())
        {
            Debug.Log("<color=orange>Area unlock animation in progress, waiting before barrier removal</color>");
            animationWasInProgress = true;
            
            // Wait until the animation finishes
            float startTime = Time.time;
            while (cameraAnimations.IsAnimationInProgress() && (Time.time - startTime < 15f))
            {
                yield return null;
            }
            
            // Add extra buffer after area unlock
            Debug.Log("<color=orange>Area unlock animation completed, waiting buffer time</color>");
            yield return new WaitForSeconds(2f);
        }
        
        // Now it's safe to run the barrier removal
        Debug.Log("<color=orange>Starting barrier removal" + (animationWasInProgress ? " after area unlock" : "") + "</color>");
        StartCoroutine(TryRemoveRockBarriersWithRetry());
    }

    // Add coroutine with retry mechanism
    private IEnumerator TryRemoveRockBarriersWithRetry()
    {
        Debug.Log("Starting rock barrier removal with retry mechanism");
        
        // Declare GameManager once at the start of the method
        GameManager gameManager = FindObjectOfType<GameManager>();
        
        // Try multiple times in case barrier isn't loaded yet
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Debug.Log($"Rock barrier removal attempt {attempt+1}");
            
            // First check if barriers are already removed in saved data
            if (gameManager != null && gameManager.gameData != null && gameManager.gameData.isRockBarriersRemoved)
            {
                Debug.Log("Rock barriers are already marked as removed in game data");
                yield break;
            }
            
            // Find the barrier controller
            BarrierRocksController rockBarrier = FindObjectOfType<BarrierRocksController>();
            
            if (rockBarrier != null)
            {
                Debug.Log($"Found barrier at {rockBarrier.transform.position} - starting removal animation");
                StartCoroutine(PlayBarrierRemovalAnimation(rockBarrier));
                yield break; // Successfully found and removing, exit
            }
            else
            {
                Debug.LogWarning($"No barrier rocks found on attempt {attempt+1}. Waiting before retry...");
                yield return new WaitForSeconds(0.5f); // Wait before retry
            }
        }
        
        // If we got here, we couldn't find the barrier after multiple attempts
        Debug.LogWarning("Failed to find rock barriers after multiple attempts. Saving removal state anyway.");
        
        // We already have the gameManager from above, reuse it
        if (gameManager != null)
        {
            gameManager.SetRockBarriersRemoved(true);
            gameManager.SaveGame();
            Debug.Log("Rock barrier removal state saved for future game sessions");
        }
    }

    // Add this public debug method to PlayerInventory.cs
    public void DebugTriggerBarrierRemoval()
    {
        Debug.Log("<color=yellow>DEBUG: Starting barrier removal test</color>");
        
        // Find barrier controller
        BarrierRocksController rockBarrier = FindObjectOfType<BarrierRocksController>();
        
        if (rockBarrier != null)
        {
            Debug.Log($"<color=green>Found barrier at {rockBarrier.transform.position}</color>");
            StartCoroutine(PlayBarrierRemovalAnimation(rockBarrier));
        }
        else
        {
            Debug.LogWarning("No barrier rocks found for debug removal.");
            
            // Try with the retry coroutine as fallback
            StartCoroutine(TryRemoveRockBarriersWithRetry());
        }
    }

    // Add a simpler method to directly add XP without triggering level-up sequence
    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        
        _experience += amount;
        OnExperienceChanged?.Invoke(_experience);
        
        // Check for level up
        int oldLevel = CurrentLevel;
        int newLevel = GetLevelFromExperience(_experience);
        
        if (newLevel > oldLevel)
        {
            // Level up occurred!
            CurrentLevel = newLevel;
            OnLevelUp?.Invoke(newLevel);
            
            // Play level up sound
            PlayLevelUpSound();
        }
        
        if (showDebugMessages)
        {
            Debug.Log($"Added {amount} XP directly. New total: {_experience}");
        }
    }

    public void LevelUp()
    {
        // Existing level up logic...
        
        // Trigger visual effect
        VisualEffectManager effectManager = FindObjectOfType<VisualEffectManager>();
        if (effectManager != null)
        {
            effectManager.PlayLevelUpEffect(transform.position);
        }
        
        // Raise the level up event for other systems
        OnLevelUp?.Invoke(CurrentLevel);
    }
} 