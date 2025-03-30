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
        
        // Find the progress fill image
        Image progressFill = missionPanelInstance.transform.Find("Fill")?.GetComponent<Image>();
        if (progressFill != null)
        {
            // Set initial fill amount based on mission progress
            float fillAmount = mission.targetAmount > 0 ? 
                (float)mission.currentAmount / mission.targetAmount : 0f;
            progressFill.fillAmount = fillAmount;
            Debug.Log($"Set initial progress fill to {fillAmount} ({mission.currentAmount}/{mission.targetAmount})");
        }
        else
        {
            Debug.LogWarning("Fill image not found in mission panel");
        }
        
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
        
        // Set up the button for reward collection (will be initially hidden)
        Button rewardButton = missionPanelInstance.GetComponent<Button>();
        if (rewardButton != null)
        {
            // Initially disable the button if mission is not yet complete
            rewardButton.interactable = mission.CheckIsCompleted();
            
            // Add click event to collect rewards
            rewardButton.onClick.AddListener(() => CollectMissionRewards(mission.id));
            
            Debug.Log($"Set up reward button for mission {mission.id}, interactable: {rewardButton.interactable}");
        }
        else
        {
            Debug.LogWarning("Button component not found on mission panel");
        }
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
    /// Updates the mission panel UI to show current progress
    /// </summary>
    private void UpdateMissionPanelProgress(string missionId)
    {
        if (!allMissions.ContainsKey(missionId))
        {
            Debug.LogWarning($"Mission ID '{missionId}' not found for UI update");
            return;
        }
        
        MissionData mission = allMissions[missionId];
        GameObject missionPanel = null;
        
        // Find the panel for this mission
        foreach (var panel in activeMissionPanels)
        {
            if (panel != null && panelToMissionId.ContainsKey(panel) && panelToMissionId[panel] == missionId)
            {
                missionPanel = panel;
                break;
            }
        }
        
        if (missionPanel == null)
        {
            Debug.LogWarning($"Mission panel for '{missionId}' not found for UI update");
            return;
        }
        
        // Find the mission progress text
        TextMeshProUGUI missionText = missionPanel.transform.Find("MissionText")?.GetComponent<TextMeshProUGUI>();
        if (missionText != null)
        {
            // Update the text to show progress if applicable
            if (mission.targetAmount > 1)
            {
                // Extract the base text without any existing progress indicators
                string baseText = mission.missionText;
                
                // Add progress indicator
                missionText.text = $"{baseText} ({mission.currentAmount}/{mission.targetAmount})";
            }
            else
            {
                missionText.text = mission.missionText;
            }
        }
        
        // Update the fill image
        Image progressFill = missionPanel.transform.Find("Fill")?.GetComponent<Image>();
        if (progressFill != null)
        {
            // Calculate fill amount based on progress
            float fillAmount = mission.targetAmount > 0 ? 
                (float)mission.currentAmount / mission.targetAmount : 0f;
            
            // Smoothly animate the fill
            StartCoroutine(AnimateFillAmount(progressFill, progressFill.fillAmount, fillAmount, 0.5f));
        }
        
        // If mission is now complete, enable the reward button
        if (mission.CheckIsCompleted())
        {
            Button rewardButton = missionPanel.GetComponent<Button>();
            if (rewardButton != null)
            {
                rewardButton.interactable = true;
                
                // Optional: Add visual indicator that mission is completed
                // For example, change color or add a "Completed!" text
                TextMeshProUGUI completionText = missionPanel.transform.Find("CompletionText")?.GetComponent<TextMeshProUGUI>();
                if (completionText != null)
                {
                    completionText.text = "Completed! Click to collect rewards";
                    completionText.gameObject.SetActive(true);
                }
            }
        }
    }
    
    /// <summary>
    /// Animate fill amount smoothly
    /// </summary>
    private IEnumerator AnimateFillAmount(Image image, float startFill, float targetFill, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Use smoothstep interpolation for more natural movement
            float smoothProgress = progress * progress * (3f - 2f * progress);
            image.fillAmount = Mathf.Lerp(startFill, targetFill, smoothProgress);
            
            yield return null;
        }
        
        // Ensure we end at exactly the target fill
        image.fillAmount = targetFill;
    }
    
    /// <summary>
    /// Collect rewards when the player clicks the completed mission
    /// </summary>
    public void CollectMissionRewards(string missionId)
    {
        if (!allMissions.ContainsKey(missionId))
        {
            Debug.LogWarning($"Mission ID '{missionId}' not found for reward collection");
            return;
        }
        
        MissionData mission = allMissions[missionId];
        
        if (!mission.isCompleted)
        {
            // Mark the mission as completed
            mission.isCompleted = true;
            mission.isActive = false;
            
            Debug.Log($"Mission completed and rewards collected: {mission.missionText}");
            
            // Apply rewards to player
            PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
            {
                // Add coin reward
                if (mission.coinReward > 0)
                {
                    playerInventory.AddCoins(mission.coinReward);
                    Debug.Log($"Added {mission.coinReward} coins to player inventory");
                }
                
                // Add XP reward
                if (mission.xpReward > 0)
                {
                    playerInventory.AddXP(mission.xpReward);
                    Debug.Log($"Added {mission.xpReward} XP to player inventory");
                }
            }
            
            // Get the mission panel to play reward animations
            GameObject missionPanel = null;
            foreach (var panel in activeMissionPanels)
            {
                if (panel != null && panelToMissionId.ContainsKey(panel) && panelToMissionId[panel] == missionId)
                {
                    missionPanel = panel;
                    break;
                }
            }
            
            if (missionPanel != null)
            {
                // Get the VisualEffectManager
                VisualEffectManager visualEffectManager = FindObjectOfType<VisualEffectManager>();
                if (visualEffectManager != null && (mission.coinReward > 0 || mission.xpReward > 0))
                {
                    // Play visual effects for rewards
                    StartCoroutine(PlayRewardsAndHidePanel(missionPanel, visualEffectManager, mission.coinReward, mission.xpReward));
                }
                else
                {
                    // Just hide the panel if no reward effects to play
                    StartCoroutine(PlayHideAnimationAndDestroy(missionPanel));
                }
                
                // Remove from active panels
                activeMissionPanels.Remove(missionPanel);
                panelToMissionId.Remove(missionPanel);
            }
            
            // Activate any next missions
            if (mission.nextMissionIds != null)
            {
                foreach (string nextMissionId in mission.nextMissionIds)
                {
                    ActivateMission(nextMissionId);
                }
            }
            
            // Save progress
            if (!isLoadingData && gameManager != null)
            {
                SaveMissionProgress(gameManager.gameData);
            }
        }
    }
    
    /// <summary>
    /// Modified completion method - now just marks mission as complete but doesn't give rewards
    /// </summary>
    private void CompleteMission(string missionId)
    {
        if (!allMissions.ContainsKey(missionId))
        {
            Debug.LogWarning($"Mission ID '{missionId}' not found for completion");
            return;
        }
        
        MissionData mission = allMissions[missionId];
        
        // Only update the UI to show completion, but don't mark as completed yet
        // That will happen when player clicks the button
        UpdateMissionPanelProgress(missionId);
        
        Debug.Log($"Mission ready for reward collection: {mission.missionText}");
        
        // Play sound effect for mission completion
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("MissionComplete", transform.position, false);
        }
        
        // Note: We don't remove the panel or activate next missions here anymore
        // That happens in CollectMissionRewards when the player clicks the button
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
