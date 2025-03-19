using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AntIncubator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttributes playerAttributes;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private UIHelper uiHelper;

    [Header("Egg Settings")]
    [SerializeField] private GameObject antEggPrefab;
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

    // Start is called before the first frame update
    void Start()
    {
        // Find references if not assigned
        if (playerInventory == null)
            playerInventory = FindObjectOfType<PlayerInventory>();
            
        if (uiHelper == null)
            uiHelper = FindObjectOfType<UIHelper>();
            
        // Initialize UI
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(currentEggCount);
        }
    }

    // This method is called from BaseInteraction when player deposits crumbs
    public bool ProcessCrumbDeposit(int crumbCount)
    {
        Debug.Log($"Processing {crumbCount} crumbs for egg creation");
        
        // Check if we've reached the maximum number of eggs or if an egg already exists
        if (currentEggCount >= maxEggs || EggExists)
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
        CreateAntEgg();
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
    
    public void CreateAntEgg()
    {
        if (antEggPrefab == null)
        {
            Debug.LogError("Ant Egg prefab not assigned to AntIncubator!");
            return;
        }
        
        // Don't create an egg if one already exists
        if (EggExists)
        {
            Debug.Log("Cannot create egg: an egg is already incubating");
            return;
        }
        
        // Instantiate the egg
        GameObject newEgg = Instantiate(antEggPrefab, eggPos.position, Quaternion.identity);
        
        // Make it a child of this incubator (optional)
        newEgg.transform.SetParent(transform);
        
        // Play effects
        if (eggSpawnEffect != null)
        {
            eggSpawnEffect.transform.position = eggPos.position;
            eggSpawnEffect.Play();
        }
        
        if (eggCreationSound != null)
        {
            AudioSource.PlayClipAtPoint(eggCreationSound, eggPos.position);
        }
        
        // Set the static flag
        EggExists = true;
        
        // Update UI
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(1);
        }
        
        Debug.Log("Ant egg created in incubator!");
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

    // This method is called when an ant hatches
    public void OnAntHatched(GameObject newAnt)
    {
        // Play hatching sound
        PlayAntHatchedSound(newAnt.transform.position);
        
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
        // Update the UI to reflect the current egg capacity
        if (eggCapacityText != null)
        {
            int maxCapacity = playerAttributes != null ? playerAttributes.MaxEggCapacity : defaultEggCapacity;
            eggCapacityText.text = $"Egg Capacity: {currentEggCount}/{maxCapacity}";
        }
        
        // Update any other UI elements that show egg capacity
        UpdateEggSlots();
    }

    // Add this helper method to update egg slot visuals
    private void UpdateEggSlots()
    {
        // If you have visual slots for eggs, update them here
        // This is just a placeholder - implement based on your actual UI design
        if (eggSlotContainer != null)
        {
            // Update egg slot visuals based on current capacity
            int maxCapacity = playerAttributes != null ? playerAttributes.MaxEggCapacity : defaultEggCapacity;
            
            // Example implementation - enable/disable egg slot objects
            for (int i = 0; i < eggSlotContainer.childCount; i++)
            {
                if (i < maxCapacity)
                {
                    // This is a valid egg slot
                    eggSlotContainer.GetChild(i).gameObject.SetActive(true);
                    
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
                else
                {
                    // This slot exceeds capacity, hide it
                    eggSlotContainer.GetChild(i).gameObject.SetActive(false);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
