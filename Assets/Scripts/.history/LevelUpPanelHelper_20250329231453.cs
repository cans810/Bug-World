using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For Button component
using TMPro;

public class LevelUpPanelHelper : MonoBehaviour
{
    public GameObject attributePointRewardPrefab;
    public GameObject chitinCapacityRewardPrefab;
    public GameObject crumbCapacityRewardPrefab;
    public GameObject newAreaUnlockedRewardPrefab;

    public GameObject UpperPanel;
    public GameObject LowerPanelRewards;

    public PlayerAttributes playerAttributes;
    public PlayerInventory playerInventory;

    // Track instantiated rewards
    private GameObject attributePointReward;
    private GameObject chitinCapacityReward;
    private GameObject crumbCapacityReward;
    private GameObject newAreaUnlockedReward;
    private int pendingRewards = 0;
    
    // Store new area data for when the claim button is clicked
    private GameObject areaTarget;
    private int areaLevel;
    private string areaName;

    // Cache camera animations reference
    private CameraAnimations cameraAnimations;
    
    // Add to the existing fields at the top of the class
    private bool isPanelActive = false;

    // Add this field at the top of the class
    private int lastLevelSeen = 1;

    // Add to the existing fields at the top of the class
    private BorderVisualizer[] allBorders;

    // Add this field at the top of the class
    private BorderVisualizer unlockedBorder;

    // Add these fields at the top of the class
    private float revivalCooldownTime = 5f; // Time in seconds to suppress level up panel after revival
    private float lastRevivalTime = -10f; // Track when the last revival happened

    private void Awake()
    {
        // Find required references
        if (playerAttributes == null)
            playerAttributes = FindObjectOfType<PlayerAttributes>();
            
        if (playerInventory == null)
            playerInventory = FindObjectOfType<PlayerInventory>();
            
        cameraAnimations = FindObjectOfType<CameraAnimations>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Update the IsPlayerReviving method to include a cooldown after revival
    private bool IsPlayerReviving()
    {
        // Check if we're within the cooldown period after revival
        if (Time.time - lastRevivalTime < revivalCooldownTime)
        {
            Debug.Log($"Still in revival cooldown period ({Time.time - lastRevivalTime:F1}s / {revivalCooldownTime}s)");
            return true;
        }
        
        // Find player inventory and check revival flag
        if (playerInventory != null)
        {
            // If the player is currently reviving, record the time
            if (playerInventory.IsReviving)
            {
                lastRevivalTime = Time.time;
            }
            return playerInventory.IsReviving;
        }
        
        // Fallback - try to find player inventory
        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
        if (inventory != null)
        {
            // If the player is currently reviving, record the time
            if (inventory.IsReviving)
            {
                lastRevivalTime = Time.time;
            }
            return inventory.IsReviving;
        }
        
        return false;
    }

    // Modify ShowUpperPanel to check revival state
    public void ShowUpperPanel()
    {
        // Skip showing panel if player is being revived
        if (IsPlayerReviving())
        {
            Debug.Log("Skipping level up panel during revival");
            return;
        }

        UpperPanel.transform.Find("NewLevel").GetComponent<TextMeshProUGUI>().text = playerInventory.CurrentLevel.ToString();

        GetComponent<Animator>().SetBool("ShowUp", true);
        GetComponent<Animator>().SetBool("Hide", false);
    }

    public void CloseUpperPanel()
    {
        // Start the closing animation
        GetComponent<Animator>().SetBool("ShowUp", false);
        GetComponent<Animator>().SetBool("Hide", true);
        
        // Mark panel as inactive
        isPanelActive = false;
        
        Debug.Log("Level up panel closed");
    }

    public void UpdateChitinRewardText(){
        chitinCapacityReward.transform.Find("RewardText").GetComponent<TextMeshProUGUI>().text = "+10 Chitin Capacity";
    }

    public void UpdateCrumbRewardText(){
        crumbCapacityReward.transform.Find("RewardText").GetComponent<TextMeshProUGUI>().text = "+1 Crumb Capacity";
    }

    public void UpdateAttributeRewardText(){
        attributePointReward.transform.Find("RewardText").GetComponent<TextMeshProUGUI>().text = "+2 Attribute Points";
    }

    // Modify InstantiateRewards to also handle the area unlock reward
    public void InstantiateRewards()
    {
        // Skip instantiating rewards if player is being revived
        if (IsPlayerReviving())
        {
            Debug.Log("Skipping level up rewards during revival");
            return;
        }
        
        // Clear any existing rewards
        ClearExistingRewards();
        
        // Reset pending rewards counter
        pendingRewards = 3; // Start with the base 3 rewards
        
        // Instantiate attribute point reward
        attributePointReward = Instantiate(attributePointRewardPrefab, LowerPanelRewards.transform);
        Button attributeButton = attributePointReward.transform.Find("ClaimButton").GetComponent<Button>();
        attributeButton.onClick.AddListener(() => {
            OnGetAttributePointsClaimButtonClicked();
            Destroy(attributePointReward);
            DecrementPendingRewards();
        });
        
        // Instantiate chitin capacity reward
        chitinCapacityReward = Instantiate(chitinCapacityRewardPrefab, LowerPanelRewards.transform);
        Button chitinButton = chitinCapacityReward.transform.Find("ClaimButton").GetComponent<Button>();
        chitinButton.onClick.AddListener(() => {
            OnGetChitinCapacityRewardButtonClicked();
            Destroy(chitinCapacityReward);
            DecrementPendingRewards();
        });
        
        // Instantiate crumb capacity reward
        crumbCapacityReward = Instantiate(crumbCapacityRewardPrefab, LowerPanelRewards.transform);
        Button crumbButton = crumbCapacityReward.transform.Find("ClaimButton").GetComponent<Button>();
        crumbButton.onClick.AddListener(() => {
            OnGetCrumbCapacityRewardButtonClicked();
            Destroy(crumbCapacityReward);
            DecrementPendingRewards();
        });
        
        // Check for area unlock at current level - do this AFTER clearing rewards
        int currentLevel = playerInventory.CurrentLevel;
        CheckForUnlockedAreas(currentLevel, true); // Pass true to indicate we're in InstantiateRewards
        
        // Update the rewards text
        UpdateRewardTexts();
    }
    
    private void ClearExistingRewards()
    {
        // Remove any existing rewards
        if (attributePointReward != null) Destroy(attributePointReward);
        if (chitinCapacityReward != null) Destroy(chitinCapacityReward);
        if (crumbCapacityReward != null) Destroy(crumbCapacityReward);
        if (newAreaUnlockedReward != null) Destroy(newAreaUnlockedReward);
    }
    
    private void DecrementPendingRewards()
    {
        pendingRewards--;
        if (pendingRewards <= 0)
        {
            // All rewards have been claimed, close the panel
            CloseUpperPanel();
        }
    }
    
    // Updated method to handle the area unlock rewards properly
    private void CheckForUnlockedAreas(int newLevel, bool isFromInstantiate = false)
    {
        // Find all borders in the scene if not already cached
        if (allBorders == null || allBorders.Length == 0)
        {
            allBorders = FindObjectsOfType<BorderVisualizer>();
        }
        
        // Check if any borders have a level requirement matching the new level
        bool areaUnlocked = false;
        foreach (BorderVisualizer border in allBorders)
        {
            if (border.GetRequiredLevel() == newLevel)
            {
                // We found a border that unlocks at this level
                areaUnlocked = true;
                
                // Only create the reward if we're being called from InstantiateRewards
                // This prevents the reward from being created and then immediately destroyed
                if (isFromInstantiate)
                {
                    CreateNewAreaUnlockedReward(border.gameObject.name, newLevel);
                }
                
                // Always disable the border visualization
                border.DisableBorderVisualization();
                
                Debug.Log($"Unlocked area: {border.gameObject.name} at level {newLevel}");
            }
        }
        
        // Log if no areas were unlocked at this level
        if (!areaUnlocked && isFromInstantiate)
        {
            Debug.Log($"No areas unlocked at level {newLevel}");
        }
    }

    // Updated method to ensure the reward has a claim button
    private void CreateNewAreaUnlockedReward(string areaName, int level)
    {
        // Find and store the border reference for this area
        BorderVisualizer border = FindBorderByName(areaName);
        unlockedBorder = border; // Store for later use when claimed
        
        // Make sure we have a prefab to instantiate
        if (newAreaUnlockedRewardPrefab == null)
        {
            Debug.LogError("New Area Unlocked reward prefab not assigned!");
            return;
        }
        
        // Create the reward object
        newAreaUnlockedReward = Instantiate(newAreaUnlockedRewardPrefab, LowerPanelRewards.transform);
        
        // Clean up the area name
        string cleanAreaName = CleanupAreaName(areaName);
        
        // Set the reward text
        TextMeshProUGUI rewardText = newAreaUnlockedReward.transform.Find("RewardText").GetComponent<TextMeshProUGUI>();
        if (rewardText != null)
        {
            rewardText.text = $"New Area Unlocked!";
        }
        
        // Add a claim button listener with area information
        Button areaButton = newAreaUnlockedReward.transform.Find("ClaimButton").GetComponent<Button>();
        if (areaButton != null)
        {
            // Pass the actual area info to the claim handler
            areaButton.onClick.AddListener(() => {
                OnAreaUnlockedClaimButtonClicked(cleanAreaName, border, level);
                Destroy(newAreaUnlockedReward);
                DecrementPendingRewards();
            });
            
            // Increment pending rewards counter
            pendingRewards++;
        }
        
        Debug.Log($"Created new area unlocked reward for {cleanAreaName}");
    }

    // Helper method to find a border by name
    private BorderVisualizer FindBorderByName(string borderName)
    {
        if (allBorders == null || allBorders.Length == 0)
        {
            allBorders = FindObjectsOfType<BorderVisualizer>();
        }
        
        foreach (BorderVisualizer border in allBorders)
        {
            if (border.gameObject.name == borderName)
            {
                return border;
            }
        }
        
        Debug.LogWarning($"Could not find border with name: {borderName}");
        return null;
    }

    // Updated claim button handler to trigger camera animation
    private void OnAreaUnlockedClaimButtonClicked(string areaName, BorderVisualizer border, int level)
    {
        // Play sound effect
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("Pickup3");
        }
        
        // Log the claim
        Debug.Log($"Player claimed the new area: {areaName}");
        
        // Find the camera animations controller
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        
        if (cameraAnimations != null && border != null)
        {
            // Trigger camera animation to show the unlocked area
            Debug.Log($"Triggering camera animation to show area: {areaName}");
            cameraAnimations.AnimateToArea(border.transform, level, areaName);
        }
        else
        {
            Debug.LogWarning($"Could not animate to area {areaName} - CameraAnimations: {(cameraAnimations == null ? "null" : "valid")}, Border: {(border == null ? "null" : "valid")}");
        }
    }

    public void OnGetAttributePointsClaimButtonClicked()
    {
        SoundEffectManager.Instance.PlaySound("Pickup3");

        playerAttributes.availablePoints += 2;
        
        // Update UI for attribute points
        if (playerAttributes != null)
        {
            // Force update the attributes panel if it exists
            AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
            if (attributeDisplay != null)
            {
                attributeDisplay.UpdateDisplay();
            }
        }
    }

    public void OnGetChitinCapacityRewardButtonClicked()
    {
        SoundEffectManager.Instance.PlaySound("Pickup3");
        playerInventory.maxChitinCapacity += 10;
        
        // Update UI using a public method instead of directly invoking the event
        if (playerInventory != null)
        {
            // This will update the UI without directly invoking the event
            playerInventory.ForceUpdateUIUpdateChitinCapacity();
        }
    }

    public void OnGetCrumbCapacityRewardButtonClicked()
    {
        SoundEffectManager.Instance.PlaySound("Pickup3");

        playerInventory.maxCrumbCapacity += 1;
        
        // Update UI using a public method instead of directly invoking the event
        if (playerInventory != null)
        {
            // This will update the UI without directly invoking the event
            playerInventory.ForceUpdateUIUpdateCrumbCapacity();
        }
    }

    private void OnEnable()
    {
        // Subscribe to the level up event from PlayerInventory
        if (playerInventory == null)
            playerInventory = FindObjectOfType<PlayerInventory>();
        
        if (playerInventory != null)
        {
            // Set the initial last level seen value to current level
            lastLevelSeen = playerInventory.CurrentLevel;
            
            // Subscribe to level up events
            playerInventory.OnLevelUp += HandleLevelUp;
            Debug.Log($"Subscribed to player level up events. Initial level: {lastLevelSeen}");
        }

        // Also subscribe to revival events if possible
        PlayerRevivalManager revivalManager = FindObjectOfType<PlayerRevivalManager>();
        if (revivalManager != null)
        {
            revivalManager.OnRevivalComplete += HandleRevivalComplete;
            Debug.Log("Subscribed to player revival events");
        }
    }

    private void OnDisable()
    {
        // Unsubscribe when disabled to prevent memory leaks
        if (playerInventory != null)
        {
            playerInventory.OnLevelUp -= HandleLevelUp;
            Debug.Log("Unsubscribed from player level up events");
        }

        // Also unsubscribe from revival events
        PlayerRevivalManager revivalManager = FindObjectOfType<PlayerRevivalManager>();
        if (revivalManager != null)
        {
            revivalManager.OnRevivalComplete -= HandleRevivalComplete;
            Debug.Log("Unsubscribed from player revival events");
        }
    }

    // This method will be called when the player levels up
    private void HandleLevelUp(int newLevel)
    {
        // Skip if panel is already active or player is reviving
        if (isPanelActive || IsPlayerReviving())
        {
            Debug.Log($"Skipping level up panel for level {newLevel} - Panel active: {isPanelActive}, Reviving: {IsPlayerReviving()}");
            return;
        }

        // Only show panel if it's an actual level up from previous level
        if (newLevel <= lastLevelSeen)
        {
            Debug.Log($"Skipping level up panel for level {newLevel} - Not higher than last seen level {lastLevelSeen}");
            lastLevelSeen = newLevel; // Update last seen level anyway
            return;
        }

        Debug.Log($"Handling level up from {lastLevelSeen} to {newLevel}");
        lastLevelSeen = newLevel; // Update the last seen level
        
        // Check for unlocked areas at this level
        CheckForUnlockedAreas(newLevel);
        
        // Show the level up panel with small delay to let other level-up effects finish
        StartCoroutine(ShowLevelUpPanelDelayed(0.5f, newLevel));
    }

    // Coroutine to show the level up panel after a short delay
    private IEnumerator ShowLevelUpPanelDelayed(float delay, int newLevel)
    {
        yield return new WaitForSeconds(delay);
        
        // Mark panel as active
        isPanelActive = true;
        
        // Show the level up panel
        ShowUpperPanel();
        
        // Create all reward items
        InstantiateRewards();
        
        // Update the text values of the rewards
        UpdateRewardTexts();
        
        // If sound effect manager exists, play the level up sound
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("LevelUp");
        }
        
        Debug.Log($"Level up panel shown for level {newLevel} with all rewards");
    }

    // Helper method to clean up area names
    private string CleanupAreaName(string rawName)
    {
        // Remove common prefixes/suffixes
        string cleanName = rawName.Replace("Border", "")
                                  .Replace("Visualization", "")
                                  .Replace("Collider", "")
                                  .Trim();
        
        // Add spaces before capital letters (CamelCase to "Camel Case")
        if (cleanName.Length > 0)
        {
            cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, "([a-z])([A-Z])", "$1 $2");
        }
        
        return cleanName;
    }

    // Add this method to call all individual reward text update methods
    private void UpdateRewardTexts()
    {
        // Update each reward text
        if (chitinCapacityReward != null)
            UpdateChitinRewardText();
        
        if (crumbCapacityReward != null)
            UpdateCrumbRewardText();
        
        if (attributePointReward != null)
            UpdateAttributeRewardText();
        
        // We don't need to update the area unlocked reward text here
        // since it's set directly when the reward is created
        
        Debug.Log("Updated all reward texts");
    }

    // Add this method to handle when revival completes
    private void HandleRevivalComplete()
    {
        // Record the time of revival completion
        lastRevivalTime = Time.time;
        Debug.Log($"Player revival completed at time {lastRevivalTime}. Level up panel suppressed for {revivalCooldownTime} seconds.");
    }
}
