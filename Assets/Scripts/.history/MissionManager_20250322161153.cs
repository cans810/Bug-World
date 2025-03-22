using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MissionManager : MonoBehaviour
{
    [Header("Mission Panel")]
    public GameObject missionPanelPrefab; // Reference to the mission panel prefab
    
    [Header("Mission Icons")]
    public List<Sprite> missionIcons = new List<Sprite>(); // List of possible mission icons
    
    [Header("Position Settings")]
    public Transform missionContainer; // Container for instantiated mission panels
    
    [Header("Mission Settings")]
    public bool startMissionsOnAwake = true;
    
    // Definition for a mission
    [System.Serializable]
    public class MissionData
    {
        public string id; // Unique identifier for the mission
        public string missionText; // The text displayed to the player
        public int iconIndex; // Index of the icon to use
        public string[] nextMissionIds; // IDs of missions to activate upon completion
        public bool isActive = false; // Is this mission currently active
        public bool isCompleted = false; // Has this mission been completed
        public MissionType missionType = MissionType.Kill; // Type of mission
        public string targetEntityType = ""; // Entity type for kill/collect missions
        public int targetAmount = 1; // Amount required for completion
        public int currentAmount = 0; // Current progress
        
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
    
    private void Awake()
    {
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
    }
    
    private void Start()
    {
        // Start the initial missions if enabled
        if (startMissionsOnAwake)
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
            
        // Set the mission icon if valid index provided
        if (missionIcon != null && mission.iconIndex >= 0 && mission.iconIndex < missionIcons.Count)
            missionIcon.sprite = missionIcons[mission.iconIndex];
        else
            Debug.LogWarning("MissionIcon component not found or invalid icon index");
            
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
            Destroy(panelToRemove);
        }
    }
    
    /// <summary>
    /// For kill missions: call this when an enemy is killed
    /// </summary>
    public void OnEntityKilled(string entityType)
    {
        // Check all active missions for kill missions targeting this entity
        foreach (var entry in allMissions)
        {
            MissionData mission = entry.Value;
            if (mission.isActive && !mission.isCompleted && 
                mission.missionType == MissionType.Kill && 
                mission.targetEntityType == entityType)
            {
                UpdateMissionProgress(mission.id);
            }
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
}
