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
    /// Shows a mission panel for a mission
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
        else
        {
            Debug.LogWarning("No Animator component found on mission panel prefab");
        }
        
        // Find the components in the instantiated panel
        TextMeshProUGUI missionText = missionPanelInstance.transform.Find("MissionText")?.GetComponent<TextMeshProUGUI>();
        Image missionIcon = missionPanelInstance.transform.Find("MissionIcon")?.GetComponent<Image>();
        
        // Set the mission text
        if (missionText != null)
            missionText.text = mission.missionText;
        else
            Debug.LogWarning("MissionText component not found in instantiated panel");
            
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
    /// Update mission progress
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
            bool wasCompleted = mission.UpdateProgress(amount);
            
            if (wasCompleted)
            {
                CompleteMission(missionId);
            }
            else
            {
                // Update the mission panel text to show progress
                UpdateMissionPanelProgress(missionId);
            }
        }
        
        // After updating mission progress, save it
        if (!isLoadingData)
        {
            if (gameManager != null)
            {
                SaveMissionProgress(gameManager.gameData);
            }
        }
    }
    
    /// <summary>
    /// Update the mission panel text to show progress
    /// </summary>
    private void UpdateMissionPanelProgress(string missionId)
    {
        if (!allMissions.ContainsKey(missionId))
            return;
            
        MissionData mission = allMissions[missionId];
        
        // Find the panel for this mission
        foreach (GameObject panel in activeMissionPanels)
        {
            if (panel == null)
                continue;
                
            if (panelToMissionId.ContainsKey(panel) && panelToMissionId[panel] == missionId)
            {
                TextMeshProUGUI textComponent = panel.transform.Find("MissionText")?.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    // Update the text to show progress
                    textComponent.text = $"{mission.missionText} ({mission.currentAmount}/{mission.targetAmount})";
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// Called when the player completes a mission
    /// </summary>
    public void CompleteMission(string missionId)
    {
        if (!allMissions.ContainsKey(missionId))
        {
            Debug.LogWarning($"Mission ID '{missionId}' not found");
            return;
        }
        
        MissionData mission = allMissions[missionId];
        
        // Mark as completed
        mission.isCompleted = true;
        mission.isActive = false;
        
        // Award rewards to the player
        if (mission.coinReward > 0 || mission.xpReward > 0)
        {
            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
            {
                // Award coins if any
                if (mission.coinReward > 0)
                {
                    playerInventory.AddCoins(mission.coinReward);
                    Debug.Log($"Awarded {mission.coinReward} coins for completing mission '{mission.id}'");
                }
                
                // Award XP if any
                if (mission.xpReward > 0)
                {
                    playerInventory.AddExperience(mission.xpReward);
                    Debug.Log($"Awarded {mission.xpReward} XP for completing mission '{mission.id}'");
                }
                
                // Show reward notification
                UIHelper uiHelper = FindObjectOfType<UIHelper>();
                if (uiHelper != null)
                {
                    string rewardText = "";
                    if (mission.coinReward > 0)
                        rewardText += $"{mission.coinReward} coins";
                        
                    if (mission.coinReward > 0 && mission.xpReward > 0)
                        rewardText += " and ";
                        
                    if (mission.xpReward > 0)
                        rewardText += $"{mission.xpReward} XP";
                        
                    uiHelper.ShowInformText($"Mission completed! Received {rewardText}");
                }
            }
        }
        
        // Play completion sound
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("Success", transform.position, false);
        }
        
        Debug.Log($"Mission completed: {mission.missionText}");
        
        // Remove the mission panel
        RemoveMissionPanel(missionId);
        
        // Activate any next missions
        if (mission.nextMissionIds != null)
        {
            foreach (string nextMissionId in mission.nextMissionIds)
            {
                ActivateMission(nextMissionId);
            }
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
    /// Removes the mission panel for a completed mission
    /// </summary>
    private void RemoveMissionPanel(string missionId)
    {
        GameObject panelToRemove = null;
        
        // Find the panel for this mission
        foreach (GameObject panel in activeMissionPanels)
        {
            if (panel == null)
                continue;
                
            if (panelToMissionId.ContainsKey(panel) && panelToMissionId[panel] == missionId)
            {
                panelToRemove = panel;
                break;
            }
        }
        
        // Remove the panel if found
        if (panelToRemove != null)
        {
            activeMissionPanels.Remove(panelToRemove);
            panelToMissionId.Remove(panelToRemove);
            
            // Get the mission data to check for rewards
            MissionData mission = allMissions[missionId];
            
            // If there's a coin reward, play the coin animation first
            if (mission.coinReward > 0)
            {
                // Get the VisualEffectManager
                VisualEffectManager visualEffectManager = FindObjectOfType<VisualEffectManager>();
                if (visualEffectManager != null)
                {
                    // Start a coroutine to play coin animation and then hide the panel
                    StartCoroutine(PlayRewardsAndHidePanel(panelToRemove, visualEffectManager, mission.coinReward, mission.xpReward));
                }
                else
                {
                    // If no VisualEffectManager found, just hide the panel
                    StartCoroutine(PlayHideAnimationAndDestroy(panelToRemove));
                }
            }
            else
            {
                // Just hide the panel if no coin reward
                StartCoroutine(PlayHideAnimationAndDestroy(panelToRemove));
            }
        }
    }
    
    /// <summary>
    /// Plays coin animation before hiding the panel
    /// </summary>
    private IEnumerator PlayRewardsAndHidePanel(GameObject panel, VisualEffectManager visualEffectManager, int coinReward, int xpReward)
    {
        // Convert UI position to screen position, then to world position
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        
        if (rectTransform != null && canvas != null)
        {
            // Get the panel's position in screen space
            Vector3 screenPosition = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rectTransform.position);
            
            // Log positions for debugging
            Debug.Log($"Panel UI position: {rectTransform.position}, Screen position: {screenPosition}");
            
            // Convert to world position (This creates a point in front of the camera)
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            Vector3 worldPosition = ray.GetPoint(10f); // 10 units in front of camera
            
            // Wait before playing the animations to ensure the player sees the mission completion
            yield return new WaitForSeconds(0.5f);
            
            // If there's a coin reward, play the coin animation
            if (coinReward > 0)
            {
                // Play the coin animation with the world position
                visualEffectManager.PlayCoinRewardEffect(worldPosition, coinReward);
                
                Debug.Log($"Playing coin animation at world position: {worldPosition}");
                
                // Wait for the coins to finish flying
                yield return new WaitForSeconds(1.5f);
            }
            
            // If there's an XP reward, play the XP animation
            if (xpReward > 0)
            {
                // Play the XP animation from the mission panel position
                visualEffectManager.PlayXPRewardEffect(worldPosition, xpReward);
                
                Debug.Log($"Playing XP animation at world position: {worldPosition}");
                
                // Wait for the XP to finish flying
                yield return new WaitForSeconds(1.5f);
            }
        }
        else
        {
            // Fallback to using the panel's direct position (may not work correctly)
            Debug.LogWarning("Couldn't get RectTransform or Canvas - using fallback position method");
            
            if (coinReward > 0)
            {
                visualEffectManager.PlayCoinRewardEffect(panel.transform.position, coinReward);
            }
            
            if (xpReward > 0)
            {
                visualEffectManager.PlayXPRewardEffect(panel.transform.position, xpReward);
            }
            
            // Wait for the animations to finish
            yield return new WaitForSeconds(2.0f);
        }
        
        // Now hide the panel
        yield return PlayHideAnimationAndDestroy(panel);
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
}
