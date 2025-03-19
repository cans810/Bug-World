using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntIncubator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttributes playerAttributes;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Egg Settings")]
    [SerializeField] private GameObject antEggPrefab;
    [SerializeField] private int crumbsRequiredForEgg = 5;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private float spawnRadius = 1.5f;
    [SerializeField] private int maxEggs = 10;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem eggSpawnEffect;
    [SerializeField] private AudioClip eggCreationSound;

    // Track how many crumbs we've processed
    private int lastProcessedCrumbCount = 0;
    private int currentEggCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        // Find PlayerInventory if not assigned
        if (playerInventory == null)
            playerInventory = FindObjectOfType<PlayerInventory>();
            
        // Subscribe to crumb count changes
        if (playerInventory != null)
            playerInventory.OnCrumbCountChanged += CheckForEggCreation;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (playerInventory != null)
            playerInventory.OnCrumbCountChanged -= CheckForEggCreation;
    }

    private void CheckForEggCreation(int currentCrumbCount)
    {
        // Determine how many new crumbs have been collected since last check
        int newCrumbsCollected = currentCrumbCount - lastProcessedCrumbCount;
        
        // Update our tracking value
        lastProcessedCrumbCount = currentCrumbCount;
        
        // Check if any new eggs should be created
        int newEggsToCreate = newCrumbsCollected / crumbsRequiredForEgg;
        
        // Create the eggs
        for (int i = 0; i < newEggsToCreate; i++)
        {
            if (currentEggCount < maxEggs)
            {
                CreateAntEgg();
                currentEggCount++;
            }
        }
    }
    
    public void CreateAntEgg()
    {
        if (antEggPrefab == null)
        {
            Debug.LogError("Ant Egg prefab not assigned to AntIncubator!");
            return;
        }
        
        // Calculate a random position within the spawn radius
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + spawnOffset + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Instantiate the egg
        GameObject newEgg = Instantiate(antEggPrefab, spawnPosition, Quaternion.identity);
        
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
