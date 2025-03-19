using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Track how many crumbs we've processed
    private int currentEggCount = 0;

    // Static property to track if an egg exists in the scene
    public static bool EggExists { get; private set; } = false;

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

    // Update is called once per frame
    void Update()
    {
        
    }
}
