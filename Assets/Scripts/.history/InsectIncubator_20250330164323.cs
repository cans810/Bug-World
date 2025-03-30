using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InsectIncubator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttributes playerAttributes;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private UIHelper uiHelper;

    [Header("Egg Settings")]
    [SerializeField] private GameObject insectEggPrefab;
    [SerializeField] private int crumbsRequiredForEgg = 5;
    [SerializeField] private int maxEggs = 10;
    [SerializeField] private Transform eggPos;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem eggSpawnEffect;
    [SerializeField] private AudioClip eggCreationSound;

    [Header("Sound Effects")]
    [SerializeField] private string antHatchedSoundName = "AntHatched";
    [SerializeField] private bool useSoundEffectManager = true;
    [SerializeField] private AudioClip antHatchedSound; // Fallback sound

    [Header("UI Elements")]
    [SerializeField] private Text eggCapacityText;
    [SerializeField] private Transform eggSlotContainer;
    [SerializeField] private int defaultEggCapacity = 10;

    // Track how many crumbs we've processed
    private int currentEggCount = 0;

    // Static property to track if an egg exists in the scene
    public static bool EggExists { get; private set; } = false;

    // Public method to reset the egg exists flag
    public static void ResetEggExistsFlag()
    {
        EggExists = false;
    }

    // Add a new field to track which entity to hatch
    private string currentEntityTypeToHatch = "ant";

    // Start is called before the first frame update
    void Start()
    {
        // Find references if not assigned
        if (playerInventory == null)
            playerInventory = FindObjectOfType<PlayerInventory>();
            
        if (playerAttributes == null)
            playerAttributes = FindObjectOfType<PlayerAttributes>();
            
        if (uiHelper == null)
            uiHelper = FindObjectOfType<UIHelper>();
            
        // Subscribe to attribute changes
        if (playerAttributes != null)
        {
            playerAttributes.OnIncubationChanged += OnIncubationChanged;
            playerAttributes.OnAttributesChanged += UpdateUI;
        }
        
        // Initialize UI
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(currentEggCount);
        }
        
        // Update the UI with the correct max capacity
        UpdateUI();
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerAttributes != null)
        {
            playerAttributes.OnIncubationChanged -= OnIncubationChanged;
            playerAttributes.OnAttributesChanged -= UpdateUI;
        }
    }
    
    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (uiHelper != null && uiHelper.informPlayerText != null)
        {
            uiHelper.ShowInformText("");
        }
    }
    
    // Modify the CreateInsectEgg method to accept a position offset
    public GameObject CreateInsectEgg(string entityType, Vector3 positionOffset = default)
    {
        // No eggs allowed if at capacity
        if (currentEggCount >= maxEggs)
        {
            Debug.LogWarning("Cannot create more eggs - at capacity");
            return null;
        }
        
        // Save which entity type we're hatching
        currentEntityTypeToHatch = entityType;
        
        // Calculate spawn position with offset
        Vector3 spawnPosition = eggPos.position + positionOffset;
        
        // Create the egg at calculated position
        GameObject eggObj = Instantiate(insectEggPrefab, spawnPosition, Quaternion.identity);
        
        // Configure the egg
        AllyEggController eggController = eggObj.GetComponent<AllyEggController>();
        if (eggController != null)
        {
            // Set the entity type to hatch
            eggController.SetEntityType(entityType);
            
            // Set incubation time (optional: could be based on entity type)
            float incubationTime = 60f; // Default 60 seconds
            eggController.SetIncubationTime(incubationTime);
            
            // Save the egg data to game state
            SaveEggState(eggObj, entityType, incubationTime);
        }
        
        // Update egg count and UI
        currentEggCount++;
        UpdateEggCapacityUI();
        
        EggExists = true;
        return eggObj;
    }
    
    // Update the SaveEggState method to fix variable scope issues
    private void SaveEggState(GameObject eggObj, string entityType, float incubationTime)
    {
        // Find the GameManager to access game data
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null) return;
        
        // Create egg data
        EggData eggData = new EggData(
            entityType, 
            incubationTime, 
            eggObj.transform.position
        );
        
        // Add to GameData and save
        gameManager.AddEggData(eggData);
        
        // Register the egg for removal when destroyed
        AllyEggController eggController = eggObj.GetComponent<AllyEggController>();
        if (eggController != null)
        {
            eggController.SetEggData(eggData);
        }
    }

    // Call this when an egg hatches to decrement the count
    public void OnEggHatched()
    {
        currentEggCount = Mathf.Max(0, currentEggCount - 1);
        EggExists = false;
        
        // Update UI
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(currentEggCount);
        }
    }

    // Add this method to get the required number of crumbs
    public int GetRequiredCrumbs()
    {
        return crumbsRequiredForEgg;
    }

    // This method is called when an insect hatches
    public void OnInsectHatched(GameObject newInsect)
    {
        // Play hatching sound
        PlayAntHatchedSound(newInsect.transform.position);
        
        // Your existing hatching code...
    }
    
    // Method to play the ant hatched sound
    private void PlayAntHatchedSound(Vector3 position)
    {
        if (useSoundEffectManager && SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound(antHatchedSoundName, position);
            Debug.Log($"Playing ant hatched sound: {antHatchedSoundName}");
        }
        else if (antHatchedSound != null)
        {
            AudioSource.PlayClipAtPoint(antHatchedSound, position);
            Debug.Log("Playing ant hatched sound using AudioSource.PlayClipAtPoint");
        }
        else
        {
            Debug.LogWarning("No sound effect or audio clip assigned for ant hatched sound");
        }
    }

    // Add this public method to allow external systems to trigger UI updates
    public void UpdateUI()
    {
        // Get the current max capacity
        int maxCapacity = GetMaxEggCapacity();
        
        // Update the UI to reflect the current egg capacity
        if (eggCapacityText != null)
        {
            eggCapacityText.text = $"Egg Capacity: {currentEggCount}/{maxCapacity}";
            Debug.Log($"Updated egg capacity UI: {currentEggCount}/{maxCapacity}");
        }
        
        // Update any other UI elements that show egg capacity
        UpdateEggSlots(maxCapacity);
    }

    // Add a helper method to get the current max egg capacity
    private int GetMaxEggCapacity()
    {
        if (playerAttributes != null && playerAttributes.incubation.points > 0)
        {
            // Each incubation point adds 1 to the default capacity
            return defaultEggCapacity + playerAttributes.incubation.points;
        }
        return defaultEggCapacity;
    }

    // Modify the UpdateEggSlots method to take the max capacity as a parameter
    private void UpdateEggSlots(int maxCapacity)
    {
        // If you have visual slots for eggs, update them here
        if (eggSlotContainer != null)
        {
            Debug.Log($"Updating egg slots with max capacity: {maxCapacity}");
            
            // Example implementation - enable/disable egg slot objects
            for (int i = 0; i < eggSlotContainer.childCount; i++)
            {
                bool isValidSlot = i < maxCapacity;
                eggSlotContainer.GetChild(i).gameObject.SetActive(isValidSlot);
                
                if (isValidSlot)
                {
                    // If you have a visual for filled/empty slots
                    Transform slot = eggSlotContainer.GetChild(i);
                    if (slot.childCount > 0)
                    {
                        // Assuming first child is "filled" visual, second is "empty" visual
                        bool isFilled = i < currentEggCount;
                        if (slot.childCount >= 2)
                        {
                            slot.GetChild(0).gameObject.SetActive(isFilled); // Filled visual
                            slot.GetChild(1).gameObject.SetActive(!isFilled); // Empty visual
                        }
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Add a public method to get the current egg count
    public int GetCurrentEggCount()
    {
        return currentEggCount;
    }

    // Add a specific handler for incubation changes
    private void OnIncubationChanged()
    {
        // Update our UI
        UpdateUI();
        
        // Also directly update the UIHelper
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(currentEggCount);
            Debug.Log("InsectIncubator: Directly updated UIHelper after incubation change");
        }
    }

    // Add this method to check if the incubator is full
    public bool IsIncubatorFull()
    {
        // Get the current max capacity from player attributes
        int currentMaxCapacity = GetMaxEggCapacity();
        
        // Return true if we've reached the maximum number of eggs or if an egg already exists
        return currentEggCount >= currentMaxCapacity || EggExists;
    }

    // Add this method to get a message about why egg creation is blocked
    public string GetIncubatorFullMessage()
    {
        if (EggExists)
        {
            return "An egg is already incubating! Wait for it to hatch.";
        }
        else if (currentEggCount >= GetMaxEggCapacity())
        {
            return "Incubator is full! Cannot create more eggs.";
        }
        
        return string.Empty;
    }

    // Implement the SetEggCount method to properly update the egg count
    public void SetEggCount(int count)
    {
        // Make sure count doesn't exceed capacity
        int maxCapacity = GetMaxEggCapacity();
        currentEggCount = Mathf.Clamp(count, 0, maxCapacity);
        
        // Update UI
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(currentEggCount);
        }
        
        // Update the internal UI
        UpdateUI();
        
        Debug.Log($"Egg count set to {currentEggCount} (max capacity: {maxCapacity})");
    }

    // Add this method to update the egg capacity UI
    private void UpdateEggCapacityUI()
    {
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(currentEggCount);
        }
    }

    // Add method to remove egg data when hatched
    public void RemoveEggData(EggData eggData)
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.RemoveEggData(eggData);
        }
    }

    // Add method to get egg prefab for instantiation
    public GameObject GetEggPrefab()
    {
        return insectEggPrefab;
    }

    // Update CreateMultipleEggs to position eggs side by side
    public bool CreateMultipleEggs(string entityType, int count)
    {
        Debug.Log($"Attempting to create {count} {entityType} eggs");
        
        // Get the current max capacity from player attributes
        int currentMaxCapacity = GetMaxEggCapacity();
        
        // Check if we have enough space for all eggs
        if (currentEggCount + count > currentMaxCapacity)
        {
            Debug.LogWarning($"Not enough space to create {count} eggs. Current: {currentEggCount}, Max: {currentMaxCapacity}");
            return false;
        }
        
        // Special case for double threat - override EggExists
        bool originalEggExists = EggExists;
        
        // Calculate spacing between eggs (1.5 units is a good starting point)
        float eggSpacing = 1.5f;
        
        // Store created eggs
        List<GameObject> createdEggs = new List<GameObject>();
        
        // Create the requested number of eggs
        for (int i = 0; i < count; i++)
        {
            // Temporarily set EggExists to false to allow multiple eggs
            EggExists = false;
            
            // Calculate offset based on number of eggs and current index
            // Center the group of eggs around the original position
            Vector3 offset = Vector3.right * (i - (count-1)/2.0f) * eggSpacing;
            
            // Create egg with offset
            GameObject egg = CreateInsectEgg(entityType, offset);
            
            if (egg == null)
            {
                Debug.LogError($"Failed to create egg {i+1} of {count}");
                
                // Restore original EggExists state
                EggExists = originalEggExists || (i > 0);
                return false;
            }
            
            createdEggs.Add(egg);
        }
        
        // Set EggExists back to true since we have eggs now
        EggExists = true;
        
        Debug.Log($"Successfully created {count} {entityType} eggs in a row");
        return true;
    }

    // Update CreateScorpionEgg to handle multiple eggs if needed
    public GameObject CreateScorpionEgg()
    {
        Debug.Log("Attempting to create scorpion egg for market purchase");
        
        // Get the current max capacity from player attributes
        int currentMaxCapacity = GetMaxEggCapacity();
        
        // Check if we have enough space
        if (currentEggCount >= currentMaxCapacity)
        {
            Debug.LogWarning($"Not enough space to create scorpion egg. Current: {currentEggCount}, Max: {currentMaxCapacity}");
            return null;
        }
        
        // Special case - override EggExists flag temporarily
        bool originalEggExists = EggExists;
        EggExists = false;
        
        // Create the egg - use default position (no offset)
        GameObject egg = CreateInsectEgg("scorpion");
        
        // Set EggExists back to true since we have an egg now
        EggExists = true;
        
        Debug.Log($"Successfully created scorpion egg: {(egg != null ? egg.name : "null")}");
        return egg;
    }
}
