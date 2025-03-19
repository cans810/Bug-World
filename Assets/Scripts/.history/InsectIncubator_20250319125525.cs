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

    // Add this field to track all active eggs
    private List<AllyEggController> activeEggControllers = new List<AllyEggController>();

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

    // This method is called from BaseInteraction when player deposits crumbs
    public bool ProcessCrumbDeposit(int crumbCount)
    {
        Debug.Log($"Processing {crumbCount} crumbs for egg creation");
        
        // Get the current max capacity from player attributes
        int currentMaxCapacity = GetMaxEggCapacity();
        
        // Check if we've reached the maximum number of eggs or if an egg already exists
        if (currentEggCount >= currentMaxCapacity || EggExists)
        {
            if (uiHelper != null && uiHelper.informPlayerText != null)
            {
                string message = EggExists ? "An egg is already incubating! Wait for it to hatch." : "Incubator is full! Cannot create more eggs.";
                uiHelper.ShowInformText(message);
            }
            return false;
        }
        
        // Calculate how many more crumbs are needed
        if (crumbCount < crumbsRequiredForEgg)
        {
            int crumbsNeeded = crumbsRequiredForEgg - crumbCount;
            if (uiHelper != null && uiHelper.informPlayerText != null)
            {
                uiHelper.ShowInformText($"Need {crumbsNeeded} more crumbs to create an egg.");
            }
            return false;
        }
        
        // If we have enough crumbs, create an egg
        CreateInsectEgg(currentEntityTypeToHatch); 
        currentEggCount++;
        EggExists = true;
        
        // Update UI
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(currentEggCount);
            uiHelper.ShowInformText("Ant egg created successfully!");
        }
        
        return true;
    }
    
    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (uiHelper != null && uiHelper.informPlayerText != null)
        {
            uiHelper.ShowInformText("");
        }
    }
    
    // Modify the CreateInsectEgg method to accept an entity type
    public GameObject CreateInsectEgg(string entityType)
    {
        // No eggs allowed if at capacity
        if (currentEggCount >= maxEggs)
        {
            Debug.LogWarning("Cannot create more eggs - at capacity");
            return null;
        }
        
        // Save which entity type we're hatching
        currentEntityTypeToHatch = entityType;
        
        // Create the egg at spawn position
        GameObject eggObj = Instantiate(insectEggPrefab, eggPos.position, Quaternion.identity);
        
        // Configure the egg
        AllyEggController eggController = eggObj.GetComponent<AllyEggController>();
        if (eggController != null)
        {
            // Set the entity type to hatch
            eggController.SetEntityType(entityType);
            
            // Set incubation time (optional: could be based on entity type)
            float incubationTime = 60f; // Default 60 seconds
            eggController.SetIncubationTime(incubationTime);
            
            // Add to tracked eggs
            activeEggControllers.Add(eggController);
        }
        
        // Update egg count and UI
        currentEggCount++;
        UpdateEggCapacityUI();
        
        EggExists = true;
        return eggObj;
    }
    
    // Call this when an egg hatches to decrement the count
    public void OnEggHatched(AllyEggController eggController)
    {
        // Remove the egg from tracking
        if (activeEggControllers.Contains(eggController))
        {
            activeEggControllers.Remove(eggController);
        }
        
        // Decrement the egg count
        currentEggCount = Mathf.Max(0, currentEggCount - 1);
        
        // Update the static flag
        EggExists = currentEggCount > 0;
        
        // Update UI
        UpdateEggCapacityUI();
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
        // Use player attributes if available, otherwise use the default
        return playerAttributes != null ? playerAttributes.MaxEggCapacity : defaultEggCapacity;
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

    // Add a method to save all active eggs to game data
    public void SaveEggs(GameData data)
    {
        if (data == null) return;
        
        // Clear existing egg data
        data.activeEggs.Clear();
        
        // Save each active egg
        foreach (var eggController in activeEggControllers)
        {
            // Skip destroyed or null eggs
            if (eggController == null) continue;
            
            // Create egg data
            EggData eggData = new EggData(
                eggController.GetEntityType(),
                eggController.GetRemainingTime(),
                eggController.transform.position,
                eggController.transform.rotation
            );
            
            // Add to game data
            data.activeEggs.Add(eggData);
            Debug.Log($"Saved egg data: {eggData.entityType}, time remaining: {eggData.remainingTime}");
        }
    }

    // Add a method to load eggs from game data
    public void LoadEggs(GameData data)
    {
        if (data == null || data.activeEggs == null) return;
        
        // Clear any existing eggs
        ClearAllEggs();
        
        // Restore each egg from saved data
        foreach (var eggData in data.activeEggs)
        {
            // Create a new egg at the saved position
            GameObject eggObj = Instantiate(insectEggPrefab, eggData.position, eggData.rotation);
            
            // Configure the egg
            AllyEggController eggController = eggObj.GetComponent<AllyEggController>();
            if (eggController != null)
            {
                // Set the entity type
                eggController.SetEntityType(eggData.entityType);
                
                // Set the remaining time
                eggController.SetIncubationTime(eggData.remainingTime);
                
                // Add to tracked eggs
                activeEggControllers.Add(eggController);
            }
            
            // Update egg count
            currentEggCount++;
            
            Debug.Log($"Loaded egg: {eggData.entityType}, time: {eggData.remainingTime}");
        }
        
        // Update UI
        UpdateEggCapacityUI();
        
        // Set the static flag if we have eggs
        EggExists = currentEggCount > 0;
    }

    // Helper method to clear all existing eggs
    private void ClearAllEggs()
    {
        // Remove all currently tracked eggs
        foreach (var eggController in activeEggControllers)
        {
            if (eggController != null)
            {
                Destroy(eggController.gameObject);
            }
        }
        
        // Clear the list
        activeEggControllers.Clear();
        
        // Reset the count
        currentEggCount = 0;
        
        // Reset the static flag
        EggExists = false;
    }
}
