using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool vSyncEnabled = true;
    [SerializeField] private bool limitFrameRate = true;
    
    [Header("Audio Settings")]
    [SerializeField] private string backgroundAmbientSound = "Ambient1";
    [SerializeField] private float ambientFadeInTime = 0f;
    [SerializeField] private bool playBackgroundAmbient = true;

    public GameData gameData;
    
    private bool isLoadingData = false;
    
    private void Awake()
    {
        // Load game data first thing
        gameData = SaveSystem.LoadGame();
        
        // Check if this is a first-time initialization
        bool isNewGame = (gameData.currentLevel == 1 && gameData.currentXP == 0 && 
                          gameData.currentChitin == 0 && gameData.currentCrumb == 0);
        
        if (isNewGame)
        {
            Debug.Log("First-time initialization detected. Setting up new player data.");
            // Any special first-time setup goes here
        }
        else
        {
            Debug.Log($"Loading existing player data: Level={gameData.currentLevel}, XP={gameData.currentXP}");
        }

        // Set a flag in all components that we're in initialization mode 
        // so they don't trigger saves during startup
        PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
        if (playerAttributes != null)
        {
            playerAttributes.SetLoadingDataState(true);
            Debug.Log("Set PlayerAttributes to loading state to prevent auto-saves during initialization");
        }

        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.SetLoadingDataState(true);
            Debug.Log("Set PlayerInventory to loading state to prevent auto-saves during initialization");
        }
        
        // Apply frame rate settings
        if (limitFrameRate)
        {
            // Set the target frame rate
            Application.targetFrameRate = targetFrameRate;
            
            // Enable or disable VSync
            QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
            
            // Set fixed timestep for physics (60 Hz is standard)
            Time.fixedDeltaTime = 0.01667f; // Exactly 60 physics updates per second
            
            // Set maximum allowed timestep to prevent large time jumps
            Time.maximumDeltaTime = 0.03333f; // Cap at 33.3ms (30 FPS minimum)
            
            // Set maximum allowed time step between frames for particles
            Time.maximumParticleDeltaTime = 0.03f; // Helps with particle effects
            
            // Improve frame pacing - use UnityEngine namespace explicitly
            Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.Low;
        }
        else
        {
            // If we don't want to limit the frame rate, set it to the maximum
            Application.targetFrameRate = -1;
        }
        
        // Additional platform-specific optimizations
        #if UNITY_ANDROID || UNITY_IOS
        // Mobile-specific optimizations
        Application.targetFrameRate = Mathf.Min(targetFrameRate, 60); // Ensure mobile doesn't exceed 60 FPS
        QualitySettings.antiAliasing = 0; // Disable antialiasing on mobile
        QualitySettings.shadowResolution = ShadowResolution.Low; // Lower shadow quality
        QualitySettings.shadowDistance = 20f; // Reduce shadow distance
        QualitySettings.particleRaycastBudget = 64; // Reduce particle raycast budget
        QualitySettings.asyncUploadTimeSlice = 1; // Reduce time spent on async uploads
        QualitySettings.asyncUploadBufferSize = 4; // Reduce async upload buffer size
        QualitySettings.realtimeReflectionProbes = false; // Disable realtime reflection probes
        QualitySettings.billboardsFaceCameraPosition = false; // Disable billboards facing camera position
        QualitySettings.resolutionScalingFixedDPIFactor = 1.0f; // Don't scale resolution
        
        // Reduce physics simulation quality on mobile
        Physics.defaultSolverIterations = 4; // Default is 6
        Physics.defaultSolverVelocityIterations = 1; // Default is 1
        Physics.sleepThreshold = 0.005f; // Make objects sleep sooner
        Physics.defaultContactOffset = 0.02f; // Increase default contact offset
        Physics.defaultMaxAngularSpeed = 7.0f; // Limit maximum angular velocity
        
        // Optimize memory usage
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        
        #endif
        
        // Call this method to optimize all rigidbodies in the scene
        OptimizeAllRigidbodies();
    }
    
    private void Start()
    {
        // Apply the loaded data to game state BEFORE any other initialization
        ApplyLoadedDataToGameState();
        
        // Start the background ambient sound if enabled
        if (playBackgroundAmbient && !string.IsNullOrEmpty(backgroundAmbientSound))
        {
            if (SoundEffectManager.Instance != null)
            {
                // Check if the sound effect exists
                if (SoundEffectManager.Instance.HasSoundEffect(backgroundAmbientSound))
                {
                    Debug.Log($"Playing background ambient sound: {backgroundAmbientSound}");
                    // Start ambient with no fade for perfect looping
                    SoundEffectManager.Instance.PlayBackgroundSound(backgroundAmbientSound, 0f);
                }
                else
                {
                    Debug.LogWarning($"Background ambient sound '{backgroundAmbientSound}' not found in SoundEffectManager");
                }
            }
            else
            {
                Debug.LogWarning("SoundEffectManager instance not found. Cannot play background ambient.");
            }
        }
        
        // Now that data is loaded and applied, allow components to save again
        PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
        if (playerAttributes != null)
        {
            playerAttributes.SetLoadingDataState(false);
            Debug.Log("PlayerAttributes initialization complete, auto-save enabled");
        }

        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.SetLoadingDataState(false);
            Debug.Log("PlayerInventory initialization complete, auto-save enabled");
        }
    }

    public void SaveGame()
    {
        // Don't save during loading
        if (isLoadingData)
        {
            Debug.Log("Skipping save operation during data loading");
            return;
        }

        Debug.Log("Starting save operation...");
        
        // Create references to necessary components
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
        DiscoveredEntitiesController entitiesController = FindObjectOfType<DiscoveredEntitiesController>();
        LivingEntity playerEntity = null;
        
        // Find the player's LivingEntity component
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerEntity = player.GetComponent<LivingEntity>();
        }
        
        // Only save if we have the necessary components
        if (playerInventory == null)
        {
            Debug.LogError("Cannot save game: PlayerInventory not found");
            return;
        }
        
        // Update all game data before saving
        gameData.currentLevel = playerInventory.CurrentLevel;
        gameData.currentXP = playerInventory.TotalExperience;
        gameData.currentChitin = playerInventory.ChitinCount;
        gameData.currentCrumb = playerInventory.CrumbCount;
        gameData.hasYetToCollectChitin = playerInventory.HasYetToCollectChitin;
        gameData.hasYetToCollectCrumb = playerInventory.HasYetToCollectCrumb;
        gameData.currentCoin = playerInventory.CoinCount;
        gameData.currentEgg = playerInventory.CurrentEggCount;
        gameData.maxEggCapacity = playerInventory.MaxEggCapacity;
        
        // Add these lines to save max capacities:
        gameData.maxChitinCapacity = playerInventory.MaxChitinCapacity;
        gameData.maxCrumbCapacity = playerInventory.MaxCrumbCapacity;
        
        // Log the data to be saved
        Debug.Log($"Saving player data: Level={gameData.currentLevel}, XP={gameData.currentXP}, " +
                  $"Chitin={gameData.currentChitin}, Crumb={gameData.currentCrumb}, " +
                  $"Coin={gameData.currentCoin}, Egg={gameData.currentEgg}/{gameData.maxEggCapacity}");
        
        // Save player attributes
        if (playerAttributes != null)
        {
            playerAttributes.SavePlayerAttributes(gameData);
        }
        
        // Save player health
        if (playerEntity != null)
        {
            gameData.currentHP = Mathf.RoundToInt(playerEntity.CurrentHealth);
            gameData.currentMaxHP = Mathf.RoundToInt(playerEntity.MaxHealth);
        }
        
        // Save discovered entities
        if (entitiesController != null)
        {
            entitiesController.SaveDiscoveredEntities(gameData);
            entitiesController.SaveEquippedEntityState(gameData);
            entitiesController.SavePurchasedEntities(gameData);
        }
        
        // Save mission progress
        MissionManager missionManager = FindObjectOfType<MissionManager>();
        if (missionManager != null)
        {
            missionManager.SaveMissionProgress(gameData);
        }
        
        // Call the actual save system
        SaveSystem.SaveGame(gameData);
        
        Debug.Log("Game saved successfully!");
    }
    
    // Add this method to optimize all rigidbodies in the scene
    private void OptimizeAllRigidbodies()
    {
        Rigidbody[] allRigidbodies = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in allRigidbodies)
        {
            if (rb == null) continue;
            
            try
            {
                // Important settings for smoother movement
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                
                // Optimize physics behavior
                rb.sleepThreshold = 0.005f;
                rb.maxAngularVelocity = 7f;
                rb.angularDrag = 0.05f;
                
                // Adjust drag based on entity type
                LivingEntity entity = rb.GetComponent<LivingEntity>();
                if (entity != null)
                {
                    if (rb.gameObject.CompareTag("Player"))
                    {
                        rb.mass = 10f;
                        rb.drag = 1f;
                        // Don't freeze rotation for player - needs full control
                        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    }
                    else if (rb.gameObject.CompareTag("Ally"))
                    {
                        rb.mass = 8f;
                        rb.drag = 0.8f;
                        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    }
                    else
                    {
                        // For enemies
                        rb.mass = 5f;
                        rb.drag = 0.5f;
                        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error optimizing rigidbody on {rb.gameObject.name}: {e.Message}");
            }
        }
        
        Debug.Log($"Optimized {allRigidbodies.Length} rigidbodies in the scene");
    }
    
    // Optional: Monitor and log the actual frame rate
    [Header("Debug")]
    [SerializeField] private bool showFpsInLog = false;
    [SerializeField] private float fpsLogInterval = 5f;
    
    private float fpsTimer;
    private float[] fpsBuffer = new float[10]; // Store last 10 FPS values
    private int fpsBufferIndex = 0;
    
    private void Update()
    {
        if (showFpsInLog)
        {
            fpsTimer += Time.deltaTime;
            
            // Store current FPS in buffer
            float currentFps = 1.0f / Time.deltaTime;
            fpsBuffer[fpsBufferIndex] = currentFps;
            fpsBufferIndex = (fpsBufferIndex + 1) % fpsBuffer.Length;
            
            if (fpsTimer >= fpsLogInterval)
            {
                // Calculate average FPS
                float sum = 0;
                foreach (float fps in fpsBuffer)
                {
                    sum += fps;
                }
                float averageFps = sum / fpsBuffer.Length;
                
                fpsTimer = 0f;
            }
        }
    }

    public void LoadGameAndUpdateUI()
    {
        try
        {
            isLoadingData = true;
            Debug.Log("Starting data load operation...");
            
            // Load the game data
            gameData = SaveSystem.LoadGame();
            
            // Apply loaded data to game state
            ApplyLoadedDataToGameState();
            
            // Update UI components
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.LoadGameData(gameData);
            }
            
            Debug.Log("Game data loaded and applied to game state and UI.");
        }
        finally
        {
            isLoadingData = false;
            Debug.Log("Data load operation complete.");
        }
    }

    private void ApplyLoadedDataToGameState()
    {
        if (gameData == null) return;
        
        // Find necessary components
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
        LivingEntity playerEntity = null;
        
        // Find the player entity
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerEntity = player.GetComponent<LivingEntity>();
        }
        
        // Set loading flags before applying data
        if (playerInventory != null)
            playerInventory.SetLoadingDataState(true);
        if (playerAttributes != null)
            playerAttributes.SetLoadingDataState(true);
        
        // Disable camera animations during loading
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null)
        {
            cameraAnimations.SetLoadingState(true);
        }
        
        // Apply inventory data
        if (playerInventory != null)
        {
            // Set loading state
            playerInventory.SetLoadingDataState(true);
            
            // Try setting experience first, which might calculate the proper level
            playerInventory.SetExperience(gameData.currentXP, false);
            
            // Then set the level to make sure they match, but use the silentMode parameter (false)
            playerInventory.SetLevel(gameData.currentLevel, false); 

            // Additional check to prevent level up effect
            // Add this line after setting level and XP:
            playerInventory.SupressLevelUpEffects(true);
            
            // Use public methods/properties to set values
            playerInventory.SetChitinCount(gameData.currentChitin);
            playerInventory.SetCrumbCount(gameData.currentCrumb);
            playerInventory.SetCoinCount(gameData.currentCoin);
            
            // Load egg data directly to PlayerInventory
            playerInventory.LoadEggData(gameData);
            
            // Debug log to verify egg data loading
            Debug.Log($"Loaded egg data: Count = {playerInventory.CurrentEggCount}, Max Capacity = {playerInventory.MaxEggCapacity}");
            
            // Set flags
            playerInventory.HasYetToCollectChitin = gameData.hasYetToCollectChitin;
            playerInventory.HasYetToCollectCrumb = gameData.hasYetToCollectCrumb;
            
            // Force update UI after setting values
            playerInventory.ForceUpdateUI();
            
            // Add these lines to load max capacities:
            playerInventory.SetMaxChitinCapacity(gameData.maxChitinCapacity);
            playerInventory.SetMaxCrumbCapacity(gameData.maxCrumbCapacity);

            // Re-enable level up effects at the end
            playerInventory.SupressLevelUpEffects(false);
        }
        
        // Apply attribute data
        if (playerAttributes != null)
        {
            // Use the direct method to set attribute values
            playerAttributes.SetAttributeValues(
                gameData.currentStrength,
                gameData.currentVitality,
                gameData.currentAgility,
                gameData.currentIncubation,
                gameData.currentRecovery,
                gameData.currentSpeed,
                gameData.availableAttributePoints
            );
            
            // Apply the effects of the attributes
            playerAttributes.ApplyAttributeEffects();
        }
        
        // Apply health data
        if (playerEntity != null)
        {
            if (gameData.currentMaxHP > 0)
            {
                playerEntity.SetMaxHealth(gameData.currentMaxHP);
            }
            else
            {
                playerEntity.SetMaxHealth(100);
                gameData.currentMaxHP = 100;
            }
            
            if (gameData.currentHP > 0 && gameData.currentHP <= gameData.currentMaxHP)
            {
                playerEntity.SetHealth(gameData.currentHP);
            }
            else
            {
                playerEntity.SetHealth(gameData.currentMaxHP);
                gameData.currentHP = gameData.currentMaxHP;
            }
        }
        
        // Set eggs in incubator (as a fallback)
        InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
        if (incubator != null)
        {
            // Use the egg count from PlayerInventory if available
            if (playerInventory != null)
            {
                incubator.SetEggCount(playerInventory.CurrentEggCount);
                Debug.Log($"Set incubator egg count to {playerInventory.CurrentEggCount}");
            }
            else
            {
                incubator.SetEggCount(gameData.currentEgg);
                Debug.Log($"Set incubator egg count to {gameData.currentEgg} (fallback)");
            }
        }
        
        Debug.Log($"Loaded data - Level: {gameData.currentLevel}, XP: {gameData.currentXP}, " +
                  $"Chitin: {gameData.currentChitin}, Crumb: {gameData.currentCrumb}, " +
                  $"HP: {gameData.currentHP}/{gameData.currentMaxHP}");

        // After applying to player
        if (playerInventory != null)
        {
            Debug.Log($"After loading - Level: {playerInventory.CurrentLevel}, " + 
                      $"XP: {playerInventory.TotalExperience}, " +
                      $"Chitin: {playerInventory.ChitinCount}, Crumb: {playerInventory.CrumbCount}");
        }

        if (playerEntity != null)
        {
            Debug.Log($"After loading - HP: {playerEntity.CurrentHealth}/{playerEntity.MaxHealth}");
        }
        
        Debug.Log("Applied loaded game data to game state. Player Level: " + gameData.currentLevel);
        
        // Reset loading flags after all data is applied
        if (playerInventory != null)
            playerInventory.SetLoadingDataState(false);
        if (playerAttributes != null)
            playerAttributes.SetLoadingDataState(false);
        if (cameraAnimations != null)
            cameraAnimations.SetLoadingState(false);

        // Apply discovered entities data
        DiscoveredEntitiesController entitiesController = FindObjectOfType<DiscoveredEntitiesController>();
        if (entitiesController != null)
        {
            entitiesController.LoadDiscoveredEntities(gameData);
            entitiesController.LoadPurchasedEntities(gameData);
            
            // Force refresh the UI
            entitiesController.UpdateDiscoveredEntitiesUI();
            
            // Load equipped entity after other entity data
            entitiesController.LoadEquippedEntityState(gameData);
        }
        else
        {
            Debug.LogWarning("DiscoveredEntitiesController not found - entity discovery data not loaded");
        }

        // After applying all data, force update the AttributeDisplay
        AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
        if (attributeDisplay != null)
        {
            attributeDisplay.UpdateDisplayWithValues(
                gameData.currentStrength,
                gameData.currentVitality,
                gameData.currentAgility,
                gameData.currentIncubation,
                gameData.currentRecovery,
                gameData.currentSpeed,
                gameData.availableAttributePoints
            );
            
            Debug.Log("Manually updated AttributeDisplay with saved values");
        }
        
        // Force update the egg display in UIHelper
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null && playerInventory != null)
        {
            uiHelper.UpdateEggDisplay(playerInventory.CurrentEggCount);
            uiHelper.UpdateChitinDisplay(playerInventory.ChitinCount);
            uiHelper.UpdateCrumbDisplay(playerInventory.CrumbCount);
            uiHelper.UpdateExperienceDisplay(playerInventory.TotalExperience, false);
            uiHelper.UpdateCoinDisplay(playerInventory.CoinCount);
            
            Debug.Log("Forced update of all UI displays");
        }

        // Load saved eggs
        LoadSavedEggs();

        // After everything else is loaded, update border visibility
        // Use Invoke to ensure this happens after all other initialization
        Invoke("UpdateBorderVisibilityAfterLoad", 0.5f);

        // Apply nest purchase status
        NestMarketController nestMarketController = FindObjectOfType<NestMarketController>();
        if (nestMarketController != null)
        {
            nestMarketController.LoadNestPurchases(gameData);
            Debug.Log("Applied nest purchase status from saved data");
        }

        // Handle rock barriers based on saved state
        Debug.Log($"Checking rock barrier state on load: isRockBarriersRemoved = {gameData.isRockBarriersRemoved}");
        if (gameData.isRockBarriersRemoved)
        {
            Debug.Log("Starting coroutine to remove rock barriers based on saved state");
            StartCoroutine(RemoveRockBarriersAfterDelay());
        }

        // Load mission data
        MissionManager missionManager = FindObjectOfType<MissionManager>();
        if (missionManager != null)
        {
            missionManager.LoadMissionProgress(gameData);
            Debug.Log("Loaded mission progress from saved data");
            
            // Add this line to update the mission UI after loading
            missionManager.UpdateMissionUI();
        }
    }

    // Helper method to set private fields using reflection
    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"Could not find private field '{fieldName}' in {obj.GetType().Name}");
        }
    }

    // Method to set entity discovered status
    public void SetEntityDiscovered(string entityType, bool discovered)
    {
        switch (entityType.ToLower())
        {
            case "ant":
                gameData.hasDiscoveredAnt = discovered;
                break;
            case "fly":
                gameData.hasDiscoveredFly = discovered;
                break;
            case "ladybug":
                gameData.hasDiscoveredLadybug = discovered;
                break;
            case "flea":
                gameData.hasDiscoveredFlea = discovered;
                break;
            default:
                Debug.LogWarning($"Unknown entity type: {entityType}");
                break;
        }
    }

    private void SaveGameData(GameData data)
    {
        // Save player attributes
        PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
        if (playerAttributes != null)
        {
            playerAttributes.SavePlayerAttributes(data);
        }
        // ... other save operations
    }

    private void LoadGameData(GameData data)
    {
        // Load player attributes
        PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
        if (playerAttributes != null)
        {
            playerAttributes.LoadPlayerAttributes(data);
        }
        // ... other load operations
    }

    // Method to add egg data to GameData
    public void AddEggData(EggData eggData)
    {
        if (gameData == null)
        {
            gameData = new GameData();
        }
        
        // Add egg data to list
        gameData.activeEggs.Add(eggData);
        
        // Immediately save the game to preserve the egg data
        SaveGame();
        
        Debug.Log($"Added egg data for {eggData.entityType} with {eggData.remainingTime}s remaining");
    }

    // Method to remove egg data from GameData
    public void RemoveEggData(EggData eggData)
    {
        if (gameData == null) return;
        
        // Find and remove the egg data
        for (int i = gameData.activeEggs.Count - 1; i >= 0; i--)
        {
            EggData saved = gameData.activeEggs[i];
            if (saved.entityType == eggData.entityType && 
                Vector3.Distance(saved.position, eggData.position) < 0.1f)
            {
                gameData.activeEggs.RemoveAt(i);
                SaveGame();
                Debug.Log($"Removed egg data for {eggData.entityType}");
                break;
            }
        }
    }

    // Add this to the ApplyLoadedDataToGameState method to load eggs
    private void LoadSavedEggs()
    {
        // Get reference to insect incubator
        InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
        if (incubator == null || gameData == null || gameData.activeEggs == null) return;
        
        // Clear any existing eggs (for safety)
        foreach (var egg in FindObjectsOfType<AllyEggController>())
        {
            Destroy(egg.gameObject);
        }
        
        // Spawn eggs based on saved data
        foreach (var eggData in gameData.activeEggs)
        {
            GameObject eggObj = Instantiate(
                incubator.GetEggPrefab(), 
                eggData.position, 
                Quaternion.identity
            );
            
            AllyEggController eggController = eggObj.GetComponent<AllyEggController>();
            if (eggController != null)
            {
                eggController.SetEntityType(eggData.entityType);
                eggController.SetIncubationTime(eggData.remainingTime);
                eggController.SetEggData(eggData);
            }
        }
        
        // Update incubator UI
        incubator.UpdateUI();
    }

    private void UpdateBorderVisibilityAfterLoad()
    {
        // Get the player level
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null) return;
        
        int playerLevel = playerInventory.CurrentLevel;
        
        // Get all border visualizers
        BorderVisualizer[] borders = FindObjectsOfType<BorderVisualizer>();
        
        // Log what we're doing
        Debug.Log($"Updating border visibility for player level {playerLevel}. Found {borders.Length} borders.");
        
        foreach (var border in borders)
        {
            string borderName = border.gameObject.name;
            
            // Set required level for each border
            if (borderName == "MapBorder2")
                border.SetRequiredLevel(5);
            else if (borderName == "MapBorder3")
                border.SetRequiredLevel(10);
            else if (borderName == "MapBorder4")
                border.SetRequiredLevel(15);
            else if (borderName == "MapBorder5")
                border.SetRequiredLevel(20);
            else if (borderName == "MapBorder6")
                border.SetRequiredLevel(25);
            else if (borderName == "MapBorder7")
                border.SetRequiredLevel(30);
            else if (borderName == "MapBorder9")
                border.SetRequiredLevel(35);
            else if (borderName == "MapBorder10")
                border.SetRequiredLevel(40);
            else if (borderName == "MapBorder11")
                border.SetRequiredLevel(45);
            else if (borderName == "MapBorder12")
                border.SetRequiredLevel(50);
            else if (borderName == "MapBorder13")
                border.SetRequiredLevel(55);
                
            
            // If player level is high enough, disable the border visualization immediately
            int requiredLevel = border.GetRequiredLevel();
            if (requiredLevel > 0 && playerLevel >= requiredLevel)
            {
                Debug.Log($"Disabled border {borderName} (requires level {requiredLevel}, player is level {playerLevel})");
            }
        }
    }

    // Add this method to GameManager to set the rock barriers removed state
    public void SetRockBarriersRemoved(bool removed)
    {
        if (gameData != null)
        {
            gameData.isRockBarriersRemoved = removed;
        }
    }

    // Improve the barrier removal method in GameManager
    private IEnumerator RemoveRockBarriersAfterDelay()
    {
        Debug.Log("<color=orange>Starting rock barriers removal coroutine</color>");
        
        // Check if the player is high enough level - if not, let the animation handle it
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null && playerInventory.CurrentLevel < 31)
        {
            Debug.Log("<color=orange>Player level is below 31, letting animation handle barrier removal</color>");
            yield break;
        }
        
        // Ensure we wait for scene to be fully loaded
        yield return new WaitForSeconds(1.0f);
        
        // Try multiple times to find all barriers
        for (int attempt = 0; attempt < 5; attempt++)
        {
            // Find all barrier controllers
            BarrierRocksController[] rockBarriers = FindObjectsOfType<BarrierRocksController>();
            
            if (rockBarriers != null && rockBarriers.Length > 0)
            {
                Debug.Log($"<color=orange>Found {rockBarriers.Length} rock barriers on attempt {attempt+1}</color>");
                
                // Destroy each barrier
                foreach (var barrier in rockBarriers)
                {
                    if (barrier != null)
                    {
                        Debug.Log($"Destroying barrier at {barrier.transform.position}");
                        barrier.DestroyBarrier();
                    }
                }
                
                // We found and destroyed barriers, so exit
                Debug.Log("<color=green>Successfully removed rock barriers</color>");
                break;
            }
            else
            {
                Debug.LogWarning($"No barriers found on attempt {attempt+1}. Will retry after delay.");
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
