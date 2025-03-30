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

    private bool hasHatched = false;
    private CameraAnimations cameraAnimations;

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

        cameraAnimations = FindObjectOfType<CameraAnimations>();
    }

    private IEnumerator HatchEgg()
    {
        if (hasHatched) 
        {
            yield break; // Proper way to exit a coroutine early
        }
        
        hasHatched = true;
        
        Debug.Log($"AllyEggController: Hatching egg of type {entityTypeToHatch}");
        
        // Create the appropriate insect based on entity type
        GameObject insectPrefab = SelectInsectPrefabForType(entityTypeToHatch);
        
        if (insectPrefab != null)
        {
            // Spawn insect at egg position
            GameObject newInsect = Instantiate(insectPrefab, transform.position, Quaternion.identity);
            
            // Disable movement temporarily during animation
            AllyAI allyAI = newInsect.GetComponent<AllyAI>();
            if (allyAI != null)
            {
                allyAI.enabled = false;
                Debug.Log($"AllyEggController: Temporarily disabled AllyAI for {newInsect.name}");
            }
            
            AIWandering wandering = newInsect.GetComponent<AIWandering>();
            if (wandering != null)
            {
                wandering.SetWanderingEnabled(false);
                Debug.Log($"AllyEggController: Temporarily disabled wandering for {newInsect.name}");
            }
            
            // Pass reference to insect incubator
            FindObjectOfType<InsectIncubator>()?.OnInsectHatched(newInsect);
            
            // Start hatching animation
            if (cameraAnimations != null)
            {
                cameraAnimations.AnimateToHatch(transform, newInsect.transform);
                Debug.Log($"AllyEggController: Started hatching animation for {newInsect.name}");
            }
            else
            {
                // No camera animations, directly enable movement
                if (allyAI != null)
                {
                    allyAI.enabled = true;
                    allyAI.ForceEnableMovement();
                    Debug.Log($"AllyEggController: No camera animation, directly enabled AllyAI for {newInsect.name}");
                }
                
                if (wandering != null)
                {
                    wandering.SetWanderingEnabled(true);
                    Debug.Log($"AllyEggController: No camera animation, directly enabled wandering for {newInsect.name}");
                }
            }
            
            // Register this entity with player inventory
            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.RegisterAllyEntity(newInsect);
            }
        }
        
        // Update incubator state when an egg hatches
        InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
        if (incubator != null)
        {
            incubator.OnEggHatched();
        }
        
        // Clean up egg data
        RemoveEggData();
        
        // Destroy the egg object
        Destroy(gameObject);
        
        yield break; // Proper way to end a coroutine
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
        // Safety check
        if (insectPrefabs.Count == 0)
        {
            Debug.LogError("No insect prefabs assigned!");
            return;
        }
        
        try
        {
            // Verify that the insect prefab exists and index is valid
            if (insectIndexToSpawn >= 0 && insectIndexToSpawn < insectPrefabs.Count && insectPrefabs[insectIndexToSpawn] != null)
            {
                // Capitalize first letter for display
                string insectName = char.ToUpper(entityTypeToHatch[0]) + entityTypeToHatch.Substring(1);
                
                // Spawn the insect slightly above ground
                Vector3 spawnPosition = transform.position + Vector3.up * 0.1f;
                GameObject newInsect = Instantiate(insectPrefabs[insectIndexToSpawn], spawnPosition, Quaternion.identity);
                
                // Name the insect with its type to help with identification
                newInsect.name = $"{insectName}_{Time.time}";
                
                // Temporarily freeze the ally's movement
                AllyAI allyAI = newInsect.GetComponent<AllyAI>();
                AIWandering wandering = newInsect.GetComponent<AIWandering>();
                
                // Disable wandering/movement while showcasing the insect
                if (wandering != null)
                    wandering.SetWanderingEnabled(false);
                    
                if (allyAI != null)
                    allyAI.enabled = false;
                    
                // Play hatch particles if assigned
                if (hatchParticles != null)
                {
                    hatchParticles.Play();
                }
                
                // Find the camera controller and trigger the showcase animation
                CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
                if (cameraAnimations != null)
                {
                    cameraAnimations.ShowNewlyHatchedInsect(newInsect);
                }
                
                // Show message directly (in case camera animation fails)
                UIHelper uiHelper = FindObjectOfType<UIHelper>();
                if (uiHelper != null)
                {
                    uiHelper.ShowInformText($"{insectName} egg has hatched!", 5f);
                }
                
                // Start a coroutine to re-enable movement after the showcase
                StartCoroutine(ReEnableMovementAfterShowcase(newInsect));
                
                // Notify incubator that an insect has hatched
                if (parentIncubator != null)
                {
                    parentIncubator.OnInsectHatched(newInsect);
                }
                
                Debug.Log($"Successfully spawned {entityTypeToHatch} ally");
            }
            else
            {
                // Fall back to ant if index is invalid
                Debug.LogWarning($"Invalid insect index {insectIndexToSpawn}, falling back to ant (index 0)");
                Vector3 spawnPosition = transform.position + Vector3.up * 0.1f;
                GameObject newAnt = Instantiate(insectPrefabs[0], spawnPosition, Quaternion.identity);
                
                if (parentIncubator != null)
                {
                    parentIncubator.OnInsectHatched(newAnt);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error spawning ally: {e.Message}");
            
            // Attempt to spawn default ant as fallback
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
    
    // Update the re-enable movement coroutine to be more reliable
    private IEnumerator ReEnableMovementAfterShowcase(GameObject insect)
    {
        if (insect == null) yield break;
        
        // Wait for the showcase duration (3 seconds) plus animation time (2 seconds)
        yield return new WaitForSeconds(5f);
        
        // Check if the insect still exists
        if (insect == null) yield break;
        
        // Re-enable movement components
        AllyAI allyAI = insect.GetComponent<AllyAI>();
        AIWandering wandering = insect.GetComponent<AIWandering>();
        
        if (wandering != null)
        {
            wandering.SetWanderingEnabled(true);
            Debug.Log($"Re-enabled wandering for {insect.name}");
        }
            
        if (allyAI != null)
        {
            allyAI.enabled = true;
            Debug.Log($"Re-enabled AllyAI for {insect.name}");
        }
            
        Debug.Log($"Re-enabled movement for {insect.name} after showcase");
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

    // Add this method to handle camera animation completion message
    public void OnCameraAnimationComplete(GameObject insect)
    {
        // This method is called by camera animation when complete
        if (insect == null)
        {
            Debug.LogWarning("AllyEggController: OnCameraAnimationComplete called with null insect");
            return;
        }
        
        Debug.Log($"AllyEggController: Camera animation complete for {insect.name}");
        
        // Re-enable the AllyAI component on the insect
        AllyAI allyAI = insect.GetComponent<AllyAI>();
        if (allyAI != null)
        {
            allyAI.enabled = true;
            allyAI.ForceEnableMovement(); // Use our more robust method
            Debug.Log($"AllyEggController: Re-enabled AllyAI for {insect.name}");
        }
        
        // Re-enable wandering behavior
        AIWandering wandering = insect.GetComponent<AIWandering>();
        if (wandering != null)
        {
            wandering.SetWanderingEnabled(true);
            Debug.Log($"AllyEggController: Re-enabled wandering for {insect.name}");
        }
    }

    private GameObject SelectInsectPrefabForType(string entityType)
    {
        // Implement the logic to select the appropriate insect prefab based on the entity type
        // This is a placeholder and should be replaced with the actual implementation
        return insectPrefabs[insectIndexToSpawn];
    }

    private void RemoveEggData()
    {
        // Implement the logic to remove egg data from the incubator
        // This is a placeholder and should be replaced with the actual implementation
        if (myEggData != null)
        {
            InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
            if (incubator != null)
            {
                incubator.RemoveEggData(myEggData);
            }
        }
    }
}
