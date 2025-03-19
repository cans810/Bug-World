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

    // Start is called before the first frame update
    void Start()
    {
        // Find references if not assigned
        if (playerInventory == null)
            playerInventory = FindObjectOfType<PlayerInventory>();
            
        if (uiHelper == null)
            uiHelper = FindObjectOfType<UIHelper>();
    }

    // This method is called from BaseInteraction when player deposits crumbs
    public bool ProcessCrumbDeposit(int crumbCount)
    {
        Debug.Log($"Processing {crumbCount} crumbs for egg creation");
        
        // Check if we've reached the maximum number of eggs
        if (currentEggCount >= maxEggs)
        {
            if (uiHelper != null && uiHelper.informPlayerText != null)
            {
                uiHelper.informPlayerText.text = "Incubator is full! Cannot create more eggs.";
                StartCoroutine(ClearMessageAfterDelay(3f));
            }
            return false;
        }
        
        // Calculate how many more crumbs are needed
        if (crumbCount < crumbsRequiredForEgg)
        {
            int crumbsNeeded = crumbsRequiredForEgg - crumbCount;
            if (uiHelper != null && uiHelper.informPlayerText != null)
            {
                uiHelper.informPlayerText.text = $"Need {crumbsNeeded} more crumbs to create an egg.";
                StartCoroutine(ClearMessageAfterDelay(3f));
            }
            return false;
        }
        
        // If we have enough crumbs, create an egg
        CreateAntEgg();
        currentEggCount++;
        
        // Show success message
        if (uiHelper != null && uiHelper.informPlayerText != null)
        {
            uiHelper.informPlayerText.text = "Ant egg created successfully!";
            StartCoroutine(ClearMessageAfterDelay(3f));
        }
        
        return true;
    }
    
    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (uiHelper != null && uiHelper.informPlayerText != null)
        {
            uiHelper.informPlayerText.text = "";
        }
    }
    
    public void CreateAntEgg()
    {
        if (antEggPrefab == null)
        {
            Debug.LogError("Ant Egg prefab not assigned to AntIncubator!");
            return;
        }
        
        // Instantiate the egg
        GameObject newEgg = Instantiate(antEggPrefab, eggPos, Quaternion.identity);
        
        // Make it a child of this incubator (optional)
        newEgg.transform.SetParent(transform);
        
        // Play effects
        if (eggSpawnEffect != null)
        {
            eggSpawnEffect.transform.position = spawnPosition;
            eggSpawnEffect.Play();
        }
        
        if (eggCreationSound != null)
        {
            AudioSource.PlayClipAtPoint(eggCreationSound, spawnPosition);
        }
        
        Debug.Log("Ant egg created in incubator!");
    }
    
    // Call this when an egg hatches to decrement the count
    public void OnEggHatched()
    {
        currentEggCount = Mathf.Max(0, currentEggCount - 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
