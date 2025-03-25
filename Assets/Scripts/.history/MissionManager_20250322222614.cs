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
            
            // Check if this mission has a coin reward
            if (allMissions.ContainsKey(missionId) && allMissions[missionId].coinReward > 0)
            {
                // Get mission data to access the coin reward amount
                MissionData mission = allMissions[missionId];
                
                // Play coin animation before hiding the panel
                StartCoroutine(PlayCoinAnimationThenHide(panelToRemove, mission.coinReward));
            }
            else
            {
                // No coin reward, just play the hide animation
                StartCoroutine(PlayHideAnimationAndDestroy(panelToRemove));
            }
        }
    }
    
    /// <summary>
    /// Plays coin animation then hides the panel
    /// </summary>
    private IEnumerator PlayCoinAnimationThenHide(GameObject panel, int coinAmount)
    {
        // Wait a brief moment before starting coin animation
        yield return new WaitForSeconds(0.3f);
        
        // Determine how many coin symbols to show (cap at 7)
        int numberOfCoins = Mathf.Min(7, coinAmount);
        if (numberOfCoins < 1) numberOfCoins = 1;
        
        // Get the RectTransform to calculate screen position
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            StartCoroutine(PlayHideAnimationAndDestroy(panel));
            yield break;
        }
        
        // Create a UI coin prefab - more reliable for UI animations
        GameObject coinPrefab = Resources.Load<GameObject>("UI/CoinIcon");
        if (coinPrefab == null)
        {
            // Try to find an existing coin icon in scene to clone
            GameObject existingCoin = GameObject.Find("CoinIcon");
            if (existingCoin != null)
            {
                coinPrefab = existingCoin;
            }
            else
            {
                // If we can't get a coin prefab, just hide the panel
                StartCoroutine(PlayHideAnimationAndDestroy(panel));
                yield break;
            }
        }
        
        // Play a coin sound
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("Coin", Vector3.zero, false);
        }
        
        // Create temporary canvas for the coins to use
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            StartCoroutine(PlayHideAnimationAndDestroy(panel));
            yield break;
        }
        
        // Target position - top right corner (coin counter)
        Vector2 targetPosition = new Vector2(Screen.width - 150f, Screen.height - 75f);
        Vector3 worldTargetPosition = Camera.main.ScreenToWorldPoint(new Vector3(targetPosition.x, targetPosition.y, 10f));
        
        List<GameObject> coinInstances = new List<GameObject>();
        
        // Create coins directly in world space around the panel
        for (int i = 0; i < numberOfCoins; i++)
        {
            // Create coin directly in world space
            GameObject coin = Instantiate(coinPrefab, canvas.transform);
            coinInstances.Add(coin);
            
            // Make the coin visible and independent of the panel
            coin.SetActive(true);
            
            // Convert panel position to screen position
            Vector3 screenPos;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                RectTransformUtility.WorldToScreenPoint(Camera.main, panel.transform.position),
                Camera.main,
                out screenPos);
                
            // Position randomly around the panel
            float offsetX = UnityEngine.Random.Range(-50f, 50f);
            float offsetY = UnityEngine.Random.Range(-25f, 25f);
            
            RectTransform coinRect = coin.GetComponent<RectTransform>();
            coinRect.position = screenPos + new Vector3(offsetX, offsetY, 0);
            
            // Make the coin a reasonable size and visible
            coinRect.localScale = new Vector3(1f, 1f, 1f);
            
            // Small delay between spawns
            yield return new WaitForSeconds(0.05f);
        }
        
        // Wait before animation
        yield return new WaitForSeconds(0.2f);
        
        // Animate the coins to the target position
        float duration = 0.8f;
        float elapsed = 0f;
        
        // Store starting positions
        Dictionary<GameObject, Vector3> startPositions = new Dictionary<GameObject, Vector3>();
        foreach (GameObject coin in coinInstances)
        {
            startPositions[coin] = coin.GetComponent<RectTransform>().position;
        }
        
        // Do animation
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.SmoothStep(0, 1, elapsed / duration);
            
            foreach (GameObject coin in coinInstances)
            {
                if (coin == null) continue;
                
                RectTransform coinRect = coin.GetComponent<RectTransform>();
                Vector3 startPos = startPositions[coin];
                
                // Calculate a curved path
                Vector3 midPoint = Vector3.Lerp(startPos, worldTargetPosition, 0.5f);
                midPoint.y += 20f; // Arc height
                
                Vector3 a = Vector3.Lerp(startPos, midPoint, normalizedTime);
                Vector3 b = Vector3.Lerp(midPoint, worldTargetPosition, normalizedTime);
                
                // Update position
                coinRect.position = Vector3.Lerp(a, b, normalizedTime);
                
                // Scale animation
                if (normalizedTime < 0.5f)
                {
                    coinRect.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.3f, 1.3f, 1.3f), normalizedTime * 2f);
                }
                else
                {
                    coinRect.localScale = Vector3.Lerp(new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0.7f, 0.7f, 0.7f), (normalizedTime - 0.5f) * 2f);
                }
            }
            
            yield return null;
        }
        
        // Clean up coins
        foreach (GameObject coin in coinInstances)
        {
            if (coin != null)
                Destroy(coin);
        }
        
        // Now hide the panel
        StartCoroutine(PlayHideAnimationAndDestroy(panel));
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
        Debug.Log($"MissionManager: Entity of type '{entityType}' killed by player");
        
        bool foundMatchingMission = false;
        
        // Check all active missions for kill missions targeting this entity
        foreach (var entry in allMissions)
        {
            MissionData mission = entry.Value;
            
            // Log mission info for debugging
            if (mission.isActive && !mission.isCompleted && mission.missionType == MissionType.Kill)
            {
                Debug.Log($"Checking mission '{mission.id}': Target={mission.targetEntityType}, Current={mission.currentAmount}/{mission.targetAmount}");
                
                // Check if this entity matches the mission target - more flexible matching
                bool isMatch = mission.targetEntityType.ToLower() == entityType.ToLower() || 
                              (mission.targetEntityType.ToLower().Contains(entityType.ToLower()) ||
                               entityType.ToLower().Contains(mission.targetEntityType.ToLower()));
                
                if (isMatch)
                {
                    foundMatchingMission = true;
                    Debug.Log($"Mission match found! Updating progress for '{mission.id}'");
                    
                    // Update the mission progress
                    bool completed = mission.UpdateProgress(1);
                    
                    // Update the mission display
                    UpdateMissionPanelProgress(mission.id);
                    
                    // If mission is now completed, mark it as such
                    if (completed)
                    {
                        Debug.Log($"Mission '{mission.id}' completed!");
                        CompleteMission(mission.id);
                    }
                    else
                    {
                        Debug.Log($"Mission '{mission.id}' progress updated: {mission.currentAmount}/{mission.targetAmount}");
                    }
                }
            }
        }
        
        if (!foundMatchingMission)
        {
            Debug.Log($"No matching active kill missions found for entity type: {entityType}");
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
