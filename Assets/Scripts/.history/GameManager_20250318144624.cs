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
    
    private bool hasCompletedDelayedLoad = false;
    
    private void Awake()
    {
        gameData = SaveSystem.LoadGame();

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
        
        // Perform IMMEDIATE load to avoid UI flicker
        LoadGameAndUpdateUI();
        
        // Schedule a delayed load to override any components that might reset values
        Invoke("PerformDelayedLoad", 1.0f);
    }

    private void PerformDelayedLoad()
    {
        Debug.Log("===== PERFORMING DELAYED LOAD TO OVERRIDE ANY DEFAULTS =====");
        
        // Load the game data again
        GameData savedData = SaveSystem.LoadGame();
        
        if (savedData != null)
        {
            // Set the data
            gameData = savedData;
            
            // Apply to all components with force flag
            ApplyLoadedDataWithForce();
            
            // Force UI update one more time
            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.ForceUpdateUI();
            }
            
            Debug.Log("Delayed load complete - Values should now be permanently set");
            hasCompletedDelayedLoad = true;
        }
    }

    // Add this method to force-apply values
    private void ApplyLoadedDataWithForce()
    {
        try
        {
            // Find necessary components
            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
            PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
            
            // FORCE VALUES with NO EVENTS first
            if (playerInventory != null)
            {
                // Set direct field values to bypass any logic that might reset them
                typeof(PlayerInventory).GetField("_chitinCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(playerInventory, gameData.currentChitin);
                    
                typeof(PlayerInventory).GetField("_coinCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(playerInventory, gameData.currentCoin);
                    
                // More direct field assignments as needed
                
                // Now trigger UI update
                playerInventory.ForceUpdateUI();
                
                Debug.Log($"FORCED inventory values: Chitin={gameData.currentChitin}, Coin={gameData.currentCoin}");
            }
            
            // Same for attributes
            if (playerAttributes != null)
            {
                // Force attribute values
                playerAttributes.SetAttributeValues(
                    gameData.currentStrength,
                    gameData.currentVitality,
                    gameData.currentAgility,
                    gameData.currentIncubation,
                    gameData.availableAttributePoints,
                    true  // Add a forceApply parameter to your method
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during forced apply: {e.Message}");
        }
    }

    public void SaveGame()
    {
        try
        {
            Debug.Log("GameManager.SaveGame called - starting save process");
            
            // Create references to necessary components
            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
            PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
            LivingEntity playerEntity = null;
            DiscoveredEntitiesController entitiesController = FindObjectOfType<DiscoveredEntitiesController>();
            
            // Find the player's LivingEntity component
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerEntity = player.GetComponent<LivingEntity>();
            }
            
            // Update all game data before saving
            if (playerInventory != null)
            {
                // Use the direct save method instead of setting fields individually
                playerInventory.SaveAllInventoryData(gameData);
                
                // Log the coin count specifically for debugging
                Debug.Log($"Saving coin count: {playerInventory.CoinCount}");
            }
            else
            {
                Debug.LogWarning("PlayerInventory not found during save!");
            }
            
            // Save player attributes
            if (playerAttributes != null)
            {
                playerAttributes.SavePlayerAttributes(gameData);
                
                Debug.Log($"Saved player attributes: Strength={gameData.currentStrength}, " +
                          $"Vitality={gameData.currentVitality}, Agility={gameData.currentAgility}, " +
                          $"Incubation={gameData.currentIncubation}, AvailablePoints={gameData.availableAttributePoints}");
            }
            else
            {
                Debug.LogWarning("PlayerAttributes not found during save!");
            }
            
            // Save player health
            if (playerEntity != null)
            {
                gameData.currentHP = Mathf.RoundToInt(playerEntity.CurrentHealth);
                gameData.currentMaxHP = Mathf.RoundToInt(playerEntity.MaxHealth);
                
                Debug.Log($"Saved player health: HP={gameData.currentHP}/{gameData.currentMaxHP}");
            }
            else
            {
                Debug.LogWarning("Player LivingEntity not found during save!");
            }
            
            // Save entity discovery and purchase status
            if (entitiesController != null)
            {
                // Save equipped entity
                gameData.equippedEntity = entitiesController.GetEquippedEntity();
                
                // Save entity purchase status
                entitiesController.SaveEntityPurchaseStatus(gameData);
                
                Debug.Log("Saved entity discovery and purchase status");
            }
            
            // Call the actual save system
            SaveSystem.SaveGame(gameData);
            
            // Verify the saved data
            VerifySaveData();
            
            Debug.Log("Game saved successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving game: {e.Message}\n{e.StackTrace}");
        }
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
        Debug.Log("===== STARTING GAME LOAD PROCESS =====");
        // Load game data
        gameData = SaveSystem.LoadGame();
        
        if (gameData == null)
        {
            Debug.Log("No save data found. Creating new game data.");
            gameData = new GameData();
            SaveGame(); // Create initial save
            return;
        }
        
        Debug.Log("===== LOADED SAVE DATA FROM DISK =====");
        // Verify the loaded data
        VerifySaveData();
        
        // Find all required components before starting the load process
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
        LivingEntity playerEntity = null;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerEntity = player.GetComponent<LivingEntity>();
        }
        
        // Disable any auto-initialization in these components
        if (playerInventory != null)
        {
            playerInventory.SetLoadingDataState(true);
        }
        
        // Now apply the data to the game state
        Debug.Log("===== APPLYING LOADED DATA TO GAME STATE =====");
        ApplyLoadedDataToGameState();
        
        // Update UI after data is applied
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            uiHelper.LoadGameData(gameData);
        }
        
        // Re-enable normal processing in components
        if (playerInventory != null)
        {
            playerInventory.SetLoadingDataState(false);
            // Force a UI update
            playerInventory.ForceUpdateUI();
        }
        
        Debug.Log("===== GAME LOAD PROCESS COMPLETE =====");
    }

    public void ApplyLoadedDataToGameState()
    {
        try
        {
            // Find necessary components
            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
            PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
            LivingEntity playerEntity = null;
            DiscoveredEntitiesController entitiesController = FindObjectOfType<DiscoveredEntitiesController>();
            
            // Find the player's LivingEntity component
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerEntity = player.GetComponent<LivingEntity>();
            }
            
            // Force player inventory to load from the game data
            if (playerInventory != null)
            {
                Debug.Log("Loading inventory data from save file...");
                playerInventory.LoadInventoryData(gameData);
                
                // Force UI update after loading
                playerInventory.ForceUpdateUI();
                
                Debug.Log($"VERIFY: Coins loaded: {playerInventory.CoinCount}, Save data has: {gameData.currentCoin}");
            }
            else
            {
                Debug.LogError("PlayerInventory not found during load!");
            }
            
            // Load player attributes
            if (playerAttributes != null)
            {
                Debug.Log("Loading player attributes from save file...");
                playerAttributes.SetAttributeValues(
                    gameData.currentStrength,
                    gameData.currentVitality,
                    gameData.currentAgility,
                    gameData.currentIncubation,
                    gameData.availableAttributePoints
                );
                
                // Apply the effects of the attributes
                playerAttributes.ApplyAttributeEffects();
                
                Debug.Log("Player attributes loaded and applied.");
            }
            else
            {
                Debug.LogError("PlayerAttributes not found during load!");
            }
            
            // Set player health
            if (playerEntity != null)
            {
                // First set max health
                if (gameData.currentMaxHP > 0)
                {
                    playerEntity.SetMaxHealth(gameData.currentMaxHP);
                }
                else
                {
                    // Use default value
                    playerEntity.SetMaxHealth(100);
                    gameData.currentMaxHP = 100;
                }
                
                // Then set current health
                if (gameData.currentHP > 0 && gameData.currentHP <= gameData.currentMaxHP)
                {
                    playerEntity.SetHealth(gameData.currentHP);
                }
                else
                {
                    // Use max health as current health if invalid
                    playerEntity.SetHealth(gameData.currentMaxHP);
                    gameData.currentHP = gameData.currentMaxHP;
                }
                
                Debug.Log($"Applied health data: HP={gameData.currentHP}/{gameData.currentMaxHP}");
            }
            else
            {
                Debug.LogError("Player LivingEntity not found during load!");
            }
            
            // Load discovered entities
            if (entitiesController != null)
            {
                entitiesController.LoadEntityPurchaseStatus(gameData);
            }
            
            // Final verification
            Debug.Log("===== VERIFYING LOADED DATA WAS APPLIED =====");
            // Get fresh references to check current values
            playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
            {
                Debug.Log($"After load - Chitin: {playerInventory.ChitinCount}, Coin: {playerInventory.CoinCount}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during ApplyLoadedDataToGameState: {e.Message}\n{e.StackTrace}");
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
            default:
                Debug.LogWarning($"Unknown entity type: {entityType}");
                break;
        }
    }

    // Add this method to verify save data
    private void VerifySaveData()
    {
        Debug.Log("=== SAVE DATA VERIFICATION ===");
        Debug.Log($"Level: {gameData.currentLevel}");
        Debug.Log($"XP: {gameData.currentXP}");
        Debug.Log($"Chitin: {gameData.currentChitin}");
        Debug.Log($"Crumb: {gameData.currentCrumb}");
        Debug.Log($"Egg: {gameData.currentEgg}");
        Debug.Log($"Max Egg Capacity: {gameData.maxEggCapacity}");
        Debug.Log($"Strength: {gameData.currentStrength}");
        Debug.Log($"Vitality: {gameData.currentVitality}");
        Debug.Log($"Agility: {gameData.currentAgility}");
        Debug.Log($"Incubation: {gameData.currentIncubation}");
        Debug.Log($"Available Points: {gameData.availableAttributePoints}");
        Debug.Log($"Coins: {gameData.currentCoin}");
        Debug.Log("==============================");
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

    public void DebugSaveLoad()
    {
        Debug.Log("=== DEBUG SAVE/LOAD TEST ===");
        
        // Save the game
        SaveGame();
        
        // Create a new GameData instance
        GameData originalData = gameData;
        
        // Load the game
        gameData = SaveSystem.LoadGame();
        
        // Compare values
        Debug.Log($"Original Level: {originalData.currentLevel}, Loaded Level: {gameData.currentLevel}");
        Debug.Log($"Original XP: {originalData.currentXP}, Loaded XP: {gameData.currentXP}");
        Debug.Log($"Original Strength: {originalData.currentStrength}, Loaded Strength: {gameData.currentStrength}");
        Debug.Log($"Original Vitality: {originalData.currentVitality}, Loaded Vitality: {gameData.currentVitality}");
        Debug.Log($"Original Agility: {originalData.currentAgility}, Loaded Agility: {gameData.currentAgility}");
        Debug.Log($"Original Incubation: {originalData.currentIncubation}, Loaded Incubation: {gameData.currentIncubation}");
        
        // Apply the loaded data
        ApplyLoadedDataToGameState();
        
        Debug.Log("=== DEBUG TEST COMPLETE ===");
    }

    public void SetGameData(GameData data)
    {
        if (data != null)
        {
            gameData = data;
            Debug.Log("Game data set from external source");
        }
    }
}
