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
        
        // Load game data and update UI
        LoadGameAndUpdateUI();
    }

    public void SaveGame()
    {
        GameData data = new GameData();
        
        // Save player inventory data
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.SaveGameData(data);
        }
        
        // Save player attributes
        PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
        if (playerAttributes != null)
        {
            data.strengthPoints = playerAttributes.StrengthPoints;
            data.vitalityPoints = playerAttributes.VitalityPoints;
            data.agilityPoints = playerAttributes.AgilityPoints;
            data.incubationPoints = playerAttributes.IncubationPoints;
            data.availablePoints = playerAttributes.AvailablePoints;
        }
        
        SaveSystem.SaveGame(data);
    }

    public GameData LoadGame()
    {
        GameData data = SaveSystem.LoadGame();
        
        // Load player inventory data
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.LoadGameData(data);
        }
        
        // Load player attributes
        PlayerAttributes playerAttributes = FindObjectOfType<PlayerAttributes>();
        if (playerAttributes != null)
        {
            playerAttributes.SetAttributeValues(
                data.strengthPoints,
                data.vitalityPoints,
                data.agilityPoints,
                data.incubationPoints,
                data.availablePoints
            );
        }
        
        return data;
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
        // Load the game data
        gameData = SaveSystem.LoadGame();
        
        // Verify the loaded data
        VerifySaveData();
        
        // Apply loaded data to game state
        ApplyLoadedDataToGameState();
        
        // Update UI components
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            uiHelper.LoadGameData(gameData);
        }
        
        AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
        if (attributeDisplay != null)
        {
            attributeDisplay.LoadGameData(gameData);
        }
        
        Debug.Log("Game data loaded and applied to game state and UI.");
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
            // Use reflection to set private fields since they may not have setters
            SetPrivateField(playerInventory, "_currentLevel", gameData.currentLevel);
            SetPrivateField(playerInventory, "_experience", gameData.currentXP);
            
            // Resources - use reflection to set them directly instead of adding
            SetPrivateField(playerInventory, "_chitinCount", gameData.currentChitin);
            SetPrivateField(playerInventory, "_crumbCount", gameData.currentCrumb);
            
            // Load egg data directly to PlayerInventory
            playerInventory.LoadEggData(gameData);
            
            // Debug log to verify egg data loading
            Debug.Log($"Loaded egg data: Count = {playerInventory.CurrentEggCount}, Max Capacity = {playerInventory.MaxEggCapacity}");
            
            // Force update UI after setting values
            playerInventory.ForceUpdateUI();
            
            // Add these lines
            playerInventory.HasYetToCollectChitin = gameData.hasYetToCollectChitin;
            playerInventory.HasYetToCollectCrumb = gameData.hasYetToCollectCrumb;
            
            // Set coin count
            SetPrivateField(playerInventory, "currentCoinCount", gameData.currentCoin);
        }
        
        // Apply attribute data
        if (playerAttributes != null)
        {
            // First set loading flag to true before applying data
            playerAttributes.SetLoadingDataState(true);
            
            // Use the direct method to set attribute values
            playerAttributes.SetAttributeValues(
                gameData.currentStrength,
                gameData.currentVitality,
                gameData.currentAgility,
                gameData.currentIncubation,
                gameData.availableAttributePoints
            );
            
            Debug.Log($"Set attribute values directly: Strength={gameData.currentStrength}, " +
                      $"Vitality={gameData.currentVitality}, Agility={gameData.currentAgility}, " +
                      $"Incubation={gameData.currentIncubation}, AvailablePoints={gameData.availableAttributePoints}");
            
            // Apply the effects of the attributes
            playerAttributes.ApplyAttributeEffects();
            
            // Sync PlayerInventory with PlayerAttributes after loading
            if (playerInventory != null)
            {
                playerInventory.UpdateEggCapacity(playerAttributes.MaxEggCapacity);
                Debug.Log($"Updated PlayerInventory egg capacity to {playerInventory.MaxEggCapacity}");
            }
        }
        
        // Apply health data
        if (playerEntity != null)
        {
            // Only apply health values if they are valid (greater than 0)
            if (gameData.currentMaxHP > 0)
            {
                playerEntity.SetMaxHealth(gameData.currentMaxHP);
            }
            else
            {
                // Use default value instead
                playerEntity.SetMaxHealth(100);
                // Update the game data with this default value
                gameData.currentMaxHP = 100;
            }
            
            // Only set current health after max health is properly set
            if (gameData.currentHP > 0 && gameData.currentHP <= gameData.currentMaxHP)
            {
                playerEntity.SetHealth(gameData.currentHP);
            }
            else
            {
                // Use max health as current health if invalid
                playerEntity.SetHealth(gameData.currentMaxHP);
                // Update the game data
                gameData.currentHP = gameData.currentMaxHP;
            }
        }
        
        // Set eggs in incubator (as a fallback)
        AntIncubator incubator = FindObjectOfType<AntIncubator>();
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

        // Load discovered entities data
        DiscoveredEntitiesController entitiesController = FindObjectOfType<DiscoveredEntitiesController>();
        if (entitiesController != null)
        {
            entitiesController.LoadDiscoveredEntities(gameData);
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
                gameData.availableAttributePoints
            );
            
            Debug.Log("Manually updated AttributeDisplay with saved values");
        }
        
        // Force update the egg display in UIHelper
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null && playerInventory != null)
        {
            uiHelper.UpdateEggDisplay(playerInventory.CurrentEggCount);
            Debug.Log($"Forced update of egg display: {playerInventory.CurrentEggCount}/{playerInventory.MaxEggCapacity}");
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
}
