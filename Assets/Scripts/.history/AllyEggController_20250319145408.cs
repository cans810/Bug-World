using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AllyEggController : MonoBehaviour
{
    [Header("Insect Settings")]
    [SerializeField] private List<GameObject> insectPrefabs;
    
    [Tooltip("Hatching time in seconds. Set to 7200 for 2 hours")]
    [SerializeField] private float hatchTime = 7200f; // 2 hours in seconds
    
    [SerializeField] private float playerDetectionRadius = 1f;

    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem hatchParticles;
    [SerializeField] private GameObject pulsatingEffect;

    // Reference to needed components
    private InsectIncubator parentIncubator;
    private UIHelper uiHelper;
    private Transform playerTransform;
    private float hatchingProgress = 0f;
    private bool isPlayerNearby = false;
    private bool isHatching = false;
    
    // Store which entity type this egg will hatch into
    private string entityTypeToHatch = "ant"; // Default to ant
    
    // Track which insect index to spawn
    private int insectIndexToSpawn = 0;

    [Header("Incubation Settings")]
    [SerializeField] private float incubationTime = 60f; // Time in seconds to hatch
    [SerializeField] private float remainingTime;
    
    [Header("UI Components")]
    [SerializeField] private GameObject countdownCanvas;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float playerProximityDistance = 5f; // Distance at which player can see countdown
    
    private bool isPlayerInRange = false;
    private bool isCountdownActive = false;

    // Add field to track this egg's data
    private EggData myEggData;

    private void Awake()
    {
        // Default to ant (index 0) if no entity type is specified
        insectIndexToSpawn = 0;
    }

    // Add this method to set the entity type this egg will hatch into
    public void SetEntityTypeToHatch(string entityType)
    {
        entityTypeToHatch = entityType.ToLower();
        
        // Map entity type to insect prefab index
        switch (entityTypeToHatch)
        {
            case "ant": 
                insectIndexToSpawn = 0; 
                break;
            case "fly": 
                insectIndexToSpawn = 1; 
                break;
            case "ladybug": 
                insectIndexToSpawn = 2; 
                break;
            case "mosquito": 
                insectIndexToSpawn = 3; 
                break;
            case "grasshopper": 
                insectIndexToSpawn = 4; 
                break;
            case "wasp": 
                insectIndexToSpawn = 5; 
                break;
            case "wolfspider": 
                insectIndexToSpawn = 6; 
                break;
            case "beetle": 
                insectIndexToSpawn = 7; 
                break;
            case "stickinsect": 
                insectIndexToSpawn = 8; 
                break;
            case "centipede": 
                insectIndexToSpawn = 9; 
                break;
            case "mantis": 
                insectIndexToSpawn = 10; 
                break;
            case "tarantula": 
                insectIndexToSpawn = 11; 
                break;
            case "stagbeetle": 
                insectIndexToSpawn = 12; 
                break;
            case "scorpion": 
                insectIndexToSpawn = 13; 
                break;
            default:
                Debug.LogWarning($"Unknown entity type: {entityTypeToHatch}, defaulting to ant (index 0)");
                insectIndexToSpawn = 0; 
                break;
        }
        
        Debug.Log($"Egg set to hatch into: {entityTypeToHatch} (index {insectIndexToSpawn})");
    }

    private void Start()
    {
        // Find references
        parentIncubator = GetComponentInParent<InsectIncubator>();
        uiHelper = FindObjectOfType<UIHelper>();
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Start hatching process
        isHatching = true;
        StartCoroutine(HatchEgg());

        // Start checking for player proximity
        StartCoroutine(CheckPlayerProximityRoutine());
        
        // Log initial hatch time
        Debug.Log($"Egg started hatching. Will hatch in {FormatTime(hatchTime)} into a {entityTypeToHatch}");
        
        // Update egg count in UI
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(1);
        }

        // Initialize the remaining time
        remainingTime = incubationTime;
        
        // Hide countdown UI initially
        if (countdownCanvas != null)
        {
            countdownCanvas.SetActive(false);
        }
        
        // Schedule regular updates of the UI
        StartCoroutine(UpdateCountdownRoutine());
    }

    private IEnumerator HatchEgg()
    {
        if (!isHatching)
        {
            Debug.LogError("HatchEgg coroutine started, but isHatching is false!");
            yield break;
        }
        
        // Wait for the specified hatch time
        float elapsedTime = 0f;
        float lastLogTime = -10f; // Log progress every 10 seconds
        
        Debug.Log($"Starting egg hatching process for {entityTypeToHatch}...");
        
        while (elapsedTime < hatchTime)
        {
            // Update elapsed time and progress
            float deltaTime = Time.deltaTime;
            elapsedTime += deltaTime;
            hatchingProgress = elapsedTime / hatchTime;
            
            // Log progress periodically for debugging
            if (elapsedTime - lastLogTime > 10f)
            {
                float remaining = hatchTime - elapsedTime;
                Debug.Log($"Egg hatching progress: {hatchingProgress:P2}, time remaining: {FormatTime(remaining)}");
                lastLogTime = elapsedTime;
            }
            
            // Optionally, you could scale the egg or change its appearance based on progress
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, hatchingProgress);
            
            yield return null;
        }
        
        Debug.Log($"Egg hatched! Spawning {entityTypeToHatch}");
        
        // Play hatching effects
        if (hatchParticles != null)
        {
            hatchParticles.Play();
        }
        
        // Find the insect incubator to update egg count
        InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
        if (incubator != null)
        {
            // Decrease egg count in incubator
            incubator.DecrementEggCount();
        }
        
        // Spawn the entity
        SpawnAlly();
        
        // Remove egg data from persistence
        if (myEggData != null)
        {
            if (incubator != null)
            {
                incubator.RemoveEggData(myEggData);
            }
        }
        
        // Destroy the egg GameObject
        Destroy(gameObject);
    }
    
    private IEnumerator CheckPlayerProximityRoutine()
    {
        while (isHatching)
        {
            if (playerTransform != null)
            {
                // Check distance to player
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                bool wasPlayerNearby = isPlayerNearby;
                isPlayerNearby = distanceToPlayer <= playerDetectionRadius;
                
                // If player just entered proximity range, show message
                if (isPlayerNearby && !wasPlayerNearby)
                {
                    UpdatePlayerMessage();
                }
                // If player is staying in proximity, update message periodically
                else if (isPlayerNearby)
                {
                    if (Time.frameCount % 30 == 0) // Update every ~0.5 seconds at 60 FPS
                    {
                        UpdatePlayerMessage();
                    }
                }
                // If player left proximity, clear message
                else if (!isPlayerNearby && wasPlayerNearby)
                {
                    if (uiHelper != null && uiHelper.informPlayerText != null)
                    {
                        uiHelper.ShowInformText("");
                    }
                }
            }
            
            yield return new WaitForSeconds(0.1f); // Check proximity every 0.1 seconds
        }
    }
    
    private void UpdatePlayerMessage()
    {
        if (uiHelper != null && uiHelper.informPlayerText != null)
        {
            float remainingTime = hatchTime * (1 - hatchingProgress);
            
            // Capitalize first letter for display
            string insectName = char.ToUpper(entityTypeToHatch[0]) + entityTypeToHatch.Substring(1);
            uiHelper.ShowInformText($"{insectName} egg hatching in {FormatTime(remainingTime)}");
        }
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int totalSeconds = Mathf.CeilToInt(timeInSeconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        
        return $"{hours}:{minutes:D2}:{seconds:D2}";
    }
    
    private void SpawnAlly()
    {
        // Ensure index is within range
        if (insectIndexToSpawn >= 0 && insectIndexToSpawn < insectPrefabs.Count && insectPrefabs[insectIndexToSpawn] != null)
        {
            // Instantiate the insect slightly above the egg to avoid collision issues
            Vector3 spawnPosition = transform.position + Vector3.up * 0.1f;
            GameObject newInsect = Instantiate(insectPrefabs[insectIndexToSpawn], spawnPosition, Quaternion.identity);
            
            // Capitalize first letter for display
            string insectName = char.ToUpper(entityTypeToHatch[0]) + entityTypeToHatch.Substring(1);
            
            // Play the hatching sound
            if (SoundEffectManager.Instance != null)
            {
                // You might want to have different sounds for different insects
                SoundEffectManager.Instance.PlaySound("AntHatched", transform.position);
                Debug.Log($"Playing {insectName} hatched sound via SoundEffectManager");
            }
            
            // Notify incubator that the insect has hatched
            if (parentIncubator != null)
            {
                parentIncubator.OnInsectHatched(newInsect);
            }
            
            Debug.Log($"{insectName} hatched from egg!");
        }
        else
        {
            Debug.LogError($"Invalid insect index {insectIndexToSpawn} or no prefab assigned for {entityTypeToHatch}!");
            
            // Fallback to ant (index 0) if available
            if (insectPrefabs.Count > 0 && insectPrefabs[0] != null)
            {
                Vector3 spawnPosition = transform.position + Vector3.up * 0.1f;
                GameObject newAnt = Instantiate(insectPrefabs[0], spawnPosition, Quaternion.identity);
                
                if (parentIncubator != null)
                {
                    parentIncubator.OnInsectHatched(newAnt);
                }
                
                Debug.Log("Fallback to ant due to error!");
            }
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
    
    private void OnDestroy()
    {
        // Ensure we mark the egg as no longer hatching when destroyed
        isHatching = false;
        
        // Reset the static flag in InsectIncubator using the public method
        InsectIncubator.ResetEggExistsFlag();
        
        // Update egg count in UI when egg is destroyed
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(0);
        }
        
        // Remove egg data when destroyed
        if (myEggData != null)
        {
            InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
            if (incubator != null)
            {
                incubator.RemoveEggData(myEggData);
            }
        }
    }

    // Update is called to check player proximity
    private void Update()
    {
        // Check if player is in range
        CheckPlayerProximity();
        
        // Update the incubation progress
        remainingTime -= Time.deltaTime;
        
        // Update egg data if available
        if (myEggData != null)
        {
            myEggData.remainingTime = remainingTime;
        }
        
        // Check if incubation is complete
        if (remainingTime <= 0)
        {
            HatchEgg();
        }
    }
    
    // Merge the two CheckPlayerProximity methods into one
    private void CheckPlayerProximity()
    {
        if (playerTransform == null)
            return;
            
        // Calculate distance once to be used for both checks
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // ---- PART 1: Handle proximity messages ----
        bool wasPlayerNearby = isPlayerNearby;
        isPlayerNearby = distanceToPlayer <= playerDetectionRadius;
        
        // If player just entered proximity range, show message
        if (isPlayerNearby && !wasPlayerNearby)
        {
            UpdatePlayerMessage();
        }
        // If player is staying in proximity, update message periodically
        else if (isPlayerNearby)
        {
            if (Time.frameCount % 30 == 0) // Update every ~0.5 seconds at 60 FPS
            {
                UpdatePlayerMessage();
            }
        }
        // If player left proximity, clear message
        else if (!isPlayerNearby && wasPlayerNearby)
        {
            if (uiHelper != null && uiHelper.informPlayerText != null)
            {
                uiHelper.ShowInformText("");
            }
        }
        
        // ---- PART 2: Handle countdown UI visibility ----
        bool isInRange = distanceToPlayer <= playerProximityDistance;
        
        // Only update UI if range status has changed
        if (isInRange != isPlayerInRange)
        {
            isPlayerInRange = isInRange;
            
            // Update UI visibility
            if (countdownCanvas != null)
            {
                countdownCanvas.SetActive(isPlayerInRange);
                if (isPlayerInRange && !isCountdownActive)
                {
                    UpdateCountdownText();
                    isCountdownActive = true;
                }
                else if (!isPlayerInRange)
                {
                    isCountdownActive = false;
                }
            }
        }
    }
    
    // Update the countdown text
    private void UpdateCountdownText()
    {
        if (countdownText != null && isPlayerInRange)
        {
            // Format time as mm:ss
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            
            countdownText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    // Coroutine to update countdown regularly
    private IEnumerator UpdateCountdownRoutine()
    {
        while (remainingTime > 0)
        {
            // Update text if player is in range
            if (isPlayerInRange)
            {
                UpdateCountdownText();
            }
            
            // Wait a short time before updating again
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    // Method to set the incubation time
    public void SetIncubationTime(float time)
    {
        incubationTime = time;
        remainingTime = time;
        Debug.Log($"Egg incubation time set to: {time} seconds");
        
        // Update the UI if player is in range
        if (isPlayerInRange)
        {
            UpdateCountdownText();
        }
    }
    
    // Method to get remaining time (for external access)
    public float GetRemainingTime()
    {
        return remainingTime;
    }

    // Fix the inconsistency between InsectIncubator and AllyEggController
    // This is called from InsectIncubator.CreateInsectEgg
    public void SetEntityType(string entityType)
    {
        // Just redirect to the existing method
        SetEntityTypeToHatch(entityType);
    }

    // Add this method to set egg data reference
    public void SetEggData(EggData eggData)
    {
        myEggData = eggData;
    }
}
