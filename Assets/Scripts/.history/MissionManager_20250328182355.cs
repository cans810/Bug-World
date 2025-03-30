using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MissionManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static MissionManager Instance { get; private set; }
    
    [Header("Mission Panel")]
    public GameObject missionPanelPrefab; // Reference to the mission panel prefab
    
    [Header("Position Settings")]
    public Transform missionContainer; // Container for instantiated mission panels
    
    [Header("Mission Settings")]
    public bool startMissionsOnAwake = true;
    
    [Header("Save/Load")]
    private bool isLoadingData = false;
    
    // Replace multiple individual save calls with a delayed save system
    private float saveDelayTimer = 0f;
    private bool needsSave = false;
    private const float SAVE_DELAY = 3.0f; // Only save every 3 seconds at most
    
    // Add a debug flag to control logging
    [SerializeField] private bool enableDebugLogs = false;
    
    // Add these fields to the MissionManager class
    [Header("UI Components")]
    [SerializeField] private string fillImagePath = "Fill"; // Path to the Fill image within the mission prefab
    
    // Dictionary to track mission panel fill images
    private Dictionary<string, Image> missionFillImages = new Dictionary<string, Image>();
    
    // Add these color constants to the MissionManager class
    [Header("Progress Fill Colors")]
    [SerializeField] private Color progressFillColor = new Color(0.2f, 0.5f, 1f, 1f); // Solid blue by default
    [SerializeField] private Color completedFillColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Solid green by default
    
    // Definition for a mission
    [System.Serializable]
    public class MissionData
    {
        public string id; // Unique identifier for the mission
        public string missionText; // The text displayed to the player
        [Tooltip("The icon to display for this mission")]
        public Sprite missionIcon; // Direct reference to the icon sprite
        public string[] nextMissionIds; // IDs of missions to activate upon completion
        public bool isActive = false; // Is this mission currently active
        public bool isCompleted = false; // Has this mission been completed
        public MissionType missionType = MissionType.Kill; // Type of mission
        public string targetEntityType = ""; // Entity type for kill/collect missions
        public int targetAmount = 1; // Amount required for completion
        public int currentAmount = 0; // Current progress
        
        [Header("Rewards")]
        [Tooltip("Amount of coins to award when mission is completed")]
        public int coinReward = 0;
        [Tooltip("Amount of XP to award when mission is completed")]
        public int xpReward = 0;
        
        // Helper for checking if mission is completed based on its criteria
        public bool CheckIsCompleted()
        {
            return currentAmount >= targetAmount;
        }
        
        // Update progress and check if completed
        public bool UpdateProgress(int amount = 1)
        {
            currentAmount += amount;
            if (currentAmount > targetAmount)
            {
                currentAmount = targetAmount;
            }
            return CheckIsCompleted();
        }
    }
    
    // Types of missions
    public enum MissionType
    {
        Kill,       // Kill specific enemies
        Collect,    // Collect specific items
        Discover,   // Discover new areas/entities
        Custom      // Custom mission requiring manual completion
    }
    
    // Store all missions in a dictionary for easy lookup
    private Dictionary<string, MissionData> allMissions = new Dictionary<string, MissionData>();
    
    // Currently active mission panels
    private List<GameObject> activeMissionPanels = new List<GameObject>();
    
    // Map mission panels to mission IDs
    private Dictionary<GameObject, string> panelToMissionId = new Dictionary<GameObject, string>();
    
    // List of starting missions (IDs of missions that start the sequence)
    [SerializeField] private List<string> startingMissionIds = new List<string>();
    
    // List of missions defined in the inspector
    [SerializeField] private List<MissionData> missions = new List<MissionData>();
    
    private GameManager gameManager;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // If no container is assigned, use this transform
        if (missionContainer == null)
        {
            missionContainer = transform;
        }
        
        // Load all missions into the dictionary
        foreach (MissionData mission in missions)
        {
            allMissions[mission.id] = mission;
        }
        
        gameManager = FindObjectOfType<GameManager>();
    }
    
    private void Start()
    {
        // Instead of starting missions on awake, load from saved data first
        if (gameManager != null && gameManager.gameData != null)
        {
            isLoadingData = true;
            LoadMissionProgress(gameManager.gameData);
            isLoadingData = false;
        }
        
        // If no saved missions were loaded and startMissionsOnAwake is true, start initial missions
        if (activeMissionPanels.Count == 0 && startMissionsOnAwake)
        {
            StartInitialMissions();
        }
    }
    
    /// <summary>
    /// Start the initial missions in the sequence
    /// </summary>
    public void StartInitialMissions()
    {
        foreach (string missionId in startingMissionIds)
        {
            ActivateMission(missionId);
        }
    }
    
    /// <summary>
    /// Activate a mission by its ID
    /// </summary>
    public void ActivateMission(string missionId)
    {
        if (!allMissions.ContainsKey(missionId))
        {
            Debug.LogWarning($"Mission ID '{missionId}' not found");
            return;
        }
        
        MissionData mission = allMissions[missionId];
        
        // Only activate if not already active or completed
        if (!mission.isActive && !mission.isCompleted)
        {
            mission.isActive = true;
            
            // Show mission panel
            ShowMissionPanel(mission);
            
            Debug.Log($"Mission activated: {mission.missionText}");
        }
        
        // After activating a mission, save progress
        if (!isLoadingData)
        {
            if (gameManager != null)
            {
                SaveMissionProgress(gameManager.gameData);
            }
        }
    }
    
    /// <summary>
    /// Shows a mission panel for a mission with progress bar
    /// </summary>
    private void ShowMissionPanel(MissionData mission)
    {
        if (missionPanelPrefab == null)
        {
            Debug.LogError("Mission panel prefab not assigned!");
            return;
        }
        
        // Instantiate the mission panel from the prefab
        GameObject missionPanelInstance = Instantiate(missionPanelPrefab, missionContainer);
        
        // Play the ShowUp animation
        Animator animator = missionPanelInstance.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("ShowUp", true);
            animator.SetBool("Hide", false);
            Debug.Log("Playing ShowUp animation for mission panel");
        }
        
        // Find the components in the instantiated panel
        TextMeshProUGUI missionText = missionPanelInstance.transform.Find("MissionText")?.GetComponent<TextMeshProUGUI>();
        Image missionIcon = missionPanelInstance.transform.Find("MissionIcon")?.GetComponent<Image>();
        
        // Add this: Find the progress text component if it exists
        TextMeshProUGUI progressText = missionPanelInstance.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
        
        // Find the Fill image for progress tracking
        Image fillImage = missionPanelInstance.transform.Find(fillImagePath)?.GetComponent<Image>();
        if (fillImage != null)
        {
            // Initialize fill amount based on current progress
            float progress = mission.targetAmount > 0 ? 
                (float)mission.currentAmount / mission.targetAmount : 0f;
            fillImage.fillAmount = progress;
            
            // Set a non-transparent color for the fill image
            fillImage.color = progressFillColor;
            
            // Store reference to the fill image for later updates
            missionFillImages[mission.id] = fillImage;
            
            Debug.Log($"Initialized mission progress bar: {progress:P0}");
        }
        else
        {
            Debug.LogWarning($"Fill image not found at path: {fillImagePath}");
        }
        
        // Get the Button component directly from the panel
        Button panelButton = missionPanelInstance.GetComponent<Button>();
        if (panelButton != null)
        {
            // Initially disable the button interactability (will be enabled when mission completes)
            panelButton.interactable = false;
            
            // Add click listener to give rewards
            panelButton.onClick.AddListener(() => GiveRewardsForMission(mission.id, missionPanelInstance));
        }
        else
        {
            Debug.LogWarning("Button component not found on mission panel");
        }
        
        // Set the mission text
        if (missionText != null)
            missionText.text = mission.missionText;
        else
            Debug.LogWarning("MissionText component not found in instantiated panel");
            
        // Add this: Set the progress text if it exists
        if (progressText != null)
        {
            progressText.text = $"{mission.currentAmount}/{mission.targetAmount}";
        }
            
        // Set the mission icon directly from the mission data
        if (missionIcon != null && mission.missionIcon != null)
            missionIcon.sprite = mission.missionIcon;
        else if (missionIcon != null && mission.missionIcon == null)
            missionIcon.gameObject.SetActive(false); // Hide the icon if none assigned
        else
            Debug.LogWarning("MissionIcon component not found in instantiated panel");
            
        // Play a notification sound
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("Notification", transform.position, false);
        }
        
        // Add to active missions list and map panel to mission ID
        activeMissionPanels.Add(missionPanelInstance);
        panelToMissionId[missionPanelInstance] = mission.id;
    }
    
    /// <summary>
    /// Update mission panel progress
    /// </summary>
    private void UpdateMissionPanelProgress(string missionId)
    {
        if (!allMissions.ContainsKey(missionId))
            return;
            
        MissionData mission = allMissions[missionId];
        
        // Update the fill image if we have a reference to it
        if (missionFillImages.ContainsKey(missionId))
        {
            Image fillImage = missionFillImages[missionId];
            if (fillImage != null)
            {
                // Calculate and set the fill amount
                float progress = mission.targetAmount > 0 ? 
                    (float)mission.currentAmount / mission.targetAmount : 0f;
                fillImage.fillAmount = progress;
                
                // Change the fill color to green if the mission is completed
                if (mission.CheckIsCompleted())
                {
                    fillImage.color = completedFillColor;
                }
                else
                {
                    fillImage.color = progressFillColor;
                }
                
                Debug.Log($"Updated mission progress bar: {progress:P0}");
            }
        }
        
        // Find the panel for this mission
        GameObject panel = null;
        foreach (GameObject p in activeMissionPanels)
        {
            if (panelToMissionId.ContainsKey(p) && panelToMissionId[p] == missionId)
            {
                panel = p;
                break;
            }
        }
        
        // If panel was found, update the progress text
        if (panel != null)
        {
            // Find and update the progress text
            TextMeshProUGUI progressText = panel.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
            if (progressText != null)
            {
                progressText.text = $"{mission.currentAmount}/{mission.targetAmount}";
                
                // If mission is completed, you could also change the color of the progress text
                if (mission.CheckIsCompleted())
                {
                    progressText.color = completedFillColor;
                }
            }
        }
        
        // If mission is completed, enable the button
        if (mission.CheckIsCompleted() && panel != null)
        {
            Button panelButton = panel.GetComponent<Button>();
            if (panelButton != null)
            {
                panelButton.interactable = true;
                
                // Add visual indication that the button is now interactive
                ColorBlock colors = panelButton.colors;
                colors.normalColor = new Color(0.8f, 0.8f, 1f); // Light blue highlight
                panelButton.colors = colors;
                
                // Optional: Add a text component to show "CLAIM!" in yellow
                TextMeshProUGUI buttonText = panel.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.gameObject.SetActive(true);
                    buttonText.text = "CLAIM!";
                    buttonText.color = Color.yellow; // Set text color to yellow
                }
            }
        }
    }
    
    /// <summary>
    /// Complete a mission but don't give rewards yet (wait for button click)
    /// </summary>
    public void CompleteMission(string missionId)
    {
        if (!allMissions.ContainsKey(missionId))
        {
            Debug.LogWarning($"Mission ID '{missionId}' not found");
            return;
        }
        
        MissionData mission = allMissions[missionId];
        
        if (mission.isActive && !mission.isCompleted)
        {
            // Mark the mission as completed
            mission.isCompleted = true;
            
            // Play completion sound
            SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
            if (soundManager != null)
            {
                soundManager.PlaySound("MissionComplete", transform.position, false);
            }
            
            Debug.Log($"Mission completed: {mission.missionText}");
            
            // Update the UI to show completion and enable the complete button
            UpdateMissionPanelProgress(missionId);
            
            // No longer automatically removing the panel or giving rewards here
            // That will happen when the button is clicked
        }
        
        // After completing the mission, save progress
        if (!isLoadingData)
        {
            if (gameManager != null)
            {
                SaveMissionProgress(gameManager.gameData);
            }
        }
    }
    
    /// <summary>
    /// Give rewards for a completed mission (called when button is clicked)
    /// </summary>
    public void GiveRewardsForMission(string missionId, GameObject panel)
    {
        if (!allMissions.ContainsKey(missionId))
            return;
            
        MissionData mission = allMissions[missionId];
        
        // Get the reward amounts
        int coinReward = mission.coinReward;
        int xpReward = mission.xpReward;
        
        // Play visual effects for the rewards BEFORE giving rewards
        VisualEffectManager visualEffectManager = FindObjectOfType<VisualEffectManager>();
        if (visualEffectManager != null)
        {
            // Get screen position of the panel (for ScreenSpace Overlay canvas)
            Vector3 panelScreenPosition = Vector3.zero;
            
            // Get RectTransform for screen position calculation
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Get the panel's position in screen space
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                
                // Use the center of the panel
                panelScreenPosition = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;
                
                // For ScreenSpace Overlay, corners are already in screen coordinates
                Debug.Log($"Mission panel screen position: {panelScreenPosition}");
                
                // Play the coin effect from the panel position
                if (coinReward > 0)
                {
                    visualEffectManager.PlayCoinRewardEffect(panelScreenPosition, coinReward);
                }
                
                if (xpReward > 0)
                {
                    visualEffectManager.PlayXPRewardEffect(panelScreenPosition, xpReward);
                }
            }
        }
        
        // Play sound effect for reward
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("CoinReward", Vector3.zero, false);
        }
        
        // Slightly delay giving actual rewards to let animations start
        StartCoroutine(DelayedRewards(mission, coinReward, xpReward));
        
        // After giving rewards, activate next missions and remove this panel
        // Activate any next missions
        if (mission.nextMissionIds != null)
        {
            foreach (string nextMissionId in mission.nextMissionIds)
            {
                ActivateMission(nextMissionId);
            }
        }
        
        // Now remove the panel with animation
        StartCoroutine(PlayHideAnimationAndDestroy(panel));
        
        // Clean up our references
        activeMissionPanels.Remove(panel);
        panelToMissionId.Remove(panel);
        if (missionFillImages.ContainsKey(missionId))
        {
            missionFillImages.Remove(missionId);
        }
    }
    
    // Add this new method for delayed rewards
    private IEnumerator DelayedRewards(MissionData mission, int coinReward, int xpReward)
    {
        // Wait a short time to let animations begin
        yield return new WaitForSeconds(0.5f);
        
        // Give rewards to the player
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            // Add coins if there's a coin reward
            if (coinReward > 0)
            {
                playerInventory.AddCoins(coinReward);
                Debug.Log($"Gave {coinReward} coins to player");
            }
            
            // Add XP if there's an XP reward
            if (xpReward > 0)
            {
                playerInventory.AddXP(xpReward);
                Debug.Log($"Gave {xpReward} XP to player");
            }
        }
    }
    
    /// <summary>
    /// Plays the hide animation and then destroys the panel
    /// </summary>
    private IEnumerator PlayHideAnimationAndDestroy(GameObject panel)
    {
        // Get animator component
        Animator animator = panel.GetComponent<Animator>();
        if (animator != null)
        {
            // Trigger the Hide animation
            animator.SetBool("ShowUp", false);
            animator.SetBool("Hide", true);
            Debug.Log("Playing Hide animation for mission panel");
            
            // Wait for the animation to play
            yield return new WaitForSeconds(1f);
        }
        
        // Destroy the panel
        Destroy(panel);
    }
    
    /// <summary>
    /// For kill missions: call this when an enemy is killed
    /// </summary>
    public void OnEntityKilled(string entityType)
    {
        // Skip if we're in loading state
        if (isLoadingData)
            return;
        
        bool foundMatchingMission = false;
        bool madeProgress = false;
        
        // Check all active missions for kill missions targeting this entity
        foreach (var entry in allMissions)
        {
            MissionData mission = entry.Value;
            
            // Only process active, non-completed kill missions
            if (mission.isActive && !mission.isCompleted && mission.missionType == MissionType.Kill)
            {
                // Check if this entity matches the mission target using optimized method
                if (IsEntityTypeMatch(mission.targetEntityType, entityType))
                {
                    foundMatchingMission = true;
                    
                    // Update the mission progress
                    bool wasCompleted = mission.UpdateProgress(1);
                    madeProgress = true;
                    
                    // Update the mission display
                    UpdateMissionPanelProgress(mission.id);
                    
                    // If mission is now completed, mark it as such
                    if (wasCompleted)
                    {
                        CompleteMission(mission.id);
                    }
                }
            }
        }
        
        // Only trigger save if progress was made
        if (madeProgress)
        {
            needsSave = true;
        }
    }
    
    /// <summary>
    /// For collection missions: call this when items are collected
    /// </summary>
    public void OnItemCollected(string itemType, int amount = 1)
    {
        // Check all active missions for collection missions targeting this item
        foreach (var entry in allMissions)
        {
            MissionData mission = entry.Value;
            if (mission.isActive && !mission.isCompleted && 
                mission.missionType == MissionType.Collect && 
                mission.targetEntityType == itemType)
            {
                UpdateMissionProgress(mission.id, amount);
            }
        }
    }
    
    /// <summary>
    /// For discovery missions: call this when something is discovered
    /// </summary>
    public void OnEntityDiscovered(string entityType)
    {
        // Check all active missions for discovery missions targeting this entity
        foreach (var entry in allMissions)
        {
            MissionData mission = entry.Value;
            if (mission.isActive && !mission.isCompleted && 
                mission.missionType == MissionType.Discover && 
                mission.targetEntityType == entityType)
            {
                CompleteMission(mission.id); // Discovery missions complete immediately
            }
        }
    }
    
    /// <summary>
    /// Hides all active mission panels
    /// </summary>
    public void HideAllMissions()
    {
        foreach (GameObject panel in activeMissionPanels)
        {
            if (panel != null)
            {
                Destroy(panel);
            }
        }
        
        activeMissionPanels.Clear();
        panelToMissionId.Clear();
    }
    
    /// <summary>
    /// Save current mission progress to GameData
    /// </summary>
    public void SaveMissionProgress(GameData gameData)
    {
        if (isLoadingData) return;
        
        // Clear existing mission data
        gameData.missionProgress.Clear();
        
        // Save all missions (including active and completed)
        foreach (var entry in allMissions)
        {
            MissionData mission = entry.Value;
            
            // Only save missions that are active or completed
            if (mission.isActive || mission.isCompleted)
            {
                SavedMissionData savedMission = new SavedMissionData(
                    mission.id,
                    mission.isActive,
                    mission.isCompleted,
                    mission.currentAmount
                );
                
                gameData.missionProgress.Add(savedMission);
            }
        }
        
        // Instead of immediately calling SaveGame, set flag to save soon
        needsSave = true;
    }
    
    /// <summary>
    /// Load mission progress from GameData
    /// </summary>
    public void LoadMissionProgress(GameData gameData)
    {
        if (gameData.missionProgress == null || gameData.missionProgress.Count == 0)
        {
            Debug.Log("No saved mission data found.");
            return;
        }
        
        Debug.Log($"Loading {gameData.missionProgress.Count} missions from saved data");
        
        // First clear any active mission panels
        HideAllMissions();
        
        // Apply saved mission data
        foreach (SavedMissionData savedMission in gameData.missionProgress)
        {
            if (allMissions.TryGetValue(savedMission.id, out MissionData mission))
            {
                // Restore mission state
                mission.isActive = savedMission.isActive;
                mission.isCompleted = savedMission.isCompleted;
                mission.currentAmount = savedMission.currentAmount;
                
                Debug.Log($"Loaded mission: {mission.id}, Active: {mission.isActive}, Completed: {mission.isCompleted}, Progress: {mission.currentAmount}/{mission.targetAmount}");
                
                // If mission is active, show its panel
                if (mission.isActive && !mission.isCompleted)
                {
                    ShowMissionPanel(mission);
                }
            }
            else
            {
                Debug.LogWarning($"Could not find mission with ID: {savedMission.id} in mission definitions");
            }
        }
    }

    private bool IsEntityTypeMatch(string missionTarget, string killedEntityType)
    {
        // Cache the lowercase strings to avoid multiple ToLower() calls
        string missionTargetLower = missionTarget.ToLower();
        string killedEntityTypeLower = killedEntityType.ToLower();
        
        // Direct match is fastest
        if (missionTargetLower == killedEntityTypeLower)
            return true;
        
        // Only do contains checks if needed
        return missionTargetLower.Contains(killedEntityTypeLower) || 
               killedEntityTypeLower.Contains(missionTargetLower);
    }

    private void Update()
    {
        // Handle delayed saving
        if (needsSave)
        {
            saveDelayTimer += Time.deltaTime;
            if (saveDelayTimer >= SAVE_DELAY)
            {
                saveDelayTimer = 0f;
                needsSave = false;
                
                // Actually perform the save
                if (gameManager != null)
                {
                    SaveMissionProgress(gameManager.gameData);
                }
            }
        }
    }

    /// <summary>
    /// Updates all mission UI panels to reflect current progress
    /// </summary>
    public void UpdateMissionUI()
    {
        // Iterate through all active missions
        foreach (var entry in allMissions)
        {
            MissionData mission = entry.Value;
            
            // Only update UI for active, non-completed missions
            if (mission.isActive && !mission.isCompleted)
            {
                // Update the mission panel text to show current progress
                UpdateMissionPanelProgress(mission.id);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Updated UI for mission: {mission.id}, Progress: {mission.currentAmount}/{mission.targetAmount}");
                }
            }
        }
    }

    /// <summary>
    /// Updates the progress of a mission by the specified amount
    /// </summary>
    public void UpdateMissionProgress(string missionId, int amount = 1)
    {
        if (!allMissions.ContainsKey(missionId))
        {
            Debug.LogWarning($"Mission ID '{missionId}' not found");
            return;
        }
        
        MissionData mission = allMissions[missionId];
        
        if (mission.isActive && !mission.isCompleted)
        {
            // Update the progress
            bool wasCompleted = mission.UpdateProgress(amount);
            
            // Update the UI
            UpdateMissionPanelProgress(missionId);
            
            Debug.Log($"Updated mission progress: {mission.missionText} - {mission.currentAmount}/{mission.targetAmount}");
            
            // If the mission was just completed, mark it as such
            if (wasCompleted)
            {
                CompleteMission(missionId);
            }
            
            // Save progress
            if (!isLoadingData)
            {
                needsSave = true; // Use the delayed saving mechanism
            }
        }
    }
}
