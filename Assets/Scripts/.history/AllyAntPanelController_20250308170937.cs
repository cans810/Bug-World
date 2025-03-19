using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add this for TextMeshProUGUI

public class AllyAntPanelController : MonoBehaviour
{
    public GameObject allyAntButton;
    
    [SerializeField] private Transform buttonContainer; // Container to hold the buttons

    [SerializeField] private GameObject antOrdersPanel; // Panel for giving orders
    [SerializeField] private GameObject allAntsOrdersPanel; // Panel for giving orders
    [SerializeField] private TextMeshProUGUI currentOrderText; // Text showing current order/state

    [SerializeField] private Button followButton;
    [SerializeField] private Button wanderButton;
    [SerializeField] private Toggle lootModeToggle;

    [SerializeField] private Button allAntsfollowModeToggle;
    [SerializeField] private TextMeshProUGUI allAntsFollowModeOrderText;

    [SerializeField] private Button allAntswanderModeToggle;
    [SerializeField] private TextMeshProUGUI allAntsWanderModeOrderText;

    [SerializeField] private Button allAntslootModeToggle;
    [SerializeField] private TextMeshProUGUI allAntsLootModeOrderText;
    
    
    [Header("Auto-Refresh Settings")]
    [SerializeField] private bool enableAutoRefresh = true;
    [SerializeField] private float refreshInterval = 2.0f; // Check for new ants every 2 seconds
    
    private List<GameObject> createdButtons = new List<GameObject>();
    private GameObject currentlySelectedAnt = null;
    private Button currentlySelectedButton = null;
    private float nextRefreshTime = 0f;
    private int lastAntCount = 0;
    
    // Reference to selected ant's GameObject ID to try to restore selection after refresh
    private int selectedAntInstanceID = -1;
    
    // Start is called before the first frame update
    void Start()
    {
        // If no container is assigned, use this object as the container
        if (buttonContainer == null)
            buttonContainer = transform;
            
        // Find and create buttons for all ally ants at start
        RefreshAllyAntButtons();
        
        // Hide the orders panel at start
        if (antOrdersPanel != null)
            antOrdersPanel.SetActive(false);
            
        // Set up command button listeners
        if (followButton != null)
            followButton.onClick.AddListener(CommandFollow);
            
        if (wanderButton != null)
            wanderButton.onClick.AddListener(CommandWander);
            
        // Set up loot mode toggle
        if (lootModeToggle != null)
            lootModeToggle.onValueChanged.AddListener(ToggleLootMode);
            
        // Set up all ants command buttons
        if (allAntsfollowModeToggle != null)
            allAntsfollowModeToggle.onClick.AddListener(CommandAllAntsFollow);
            
        if (allAntswanderModeToggle != null)
            allAntswanderModeToggle.onClick.AddListener(CommandAllAntsWander);
            
        if (allAntslootModeToggle != null)
            allAntslootModeToggle.onClick.AddListener(ToggleAllAntsLootMode);
        
        // Initialize refresh timer
        nextRefreshTime = Time.time + refreshInterval;
        
        // Clear any previous selection
        currentlySelectedAnt = null;
        currentlySelectedButton = null;
        
        // Initialize the All Ants Orders Panel
        UpdateAllAntsOrdersPanel();
        UpdateAllAntsModeText();
    }
    
    // Update is called once per frame
    void Update()
    {
        // If auto-refresh is enabled, periodically check for new ants
        if (enableAutoRefresh && Time.time > nextRefreshTime)
        {
            CheckForNewAnts();
            nextRefreshTime = Time.time + refreshInterval;
        }
        
        // Make sure orders panel is only shown when we have a selected ant
        if (antOrdersPanel != null)
        {
            if (currentlySelectedAnt == null && antOrdersPanel.activeSelf)
            {
                antOrdersPanel.SetActive(false);
            }
            else if (currentlySelectedAnt != null && antOrdersPanel.activeSelf)
            {
                // Update the panel if the ant still exists
                UpdateOrderPanelText();
            }
        }
        
        // Always make sure the all ants panel is visible when this controller is active
        if (allAntsOrdersPanel != null && !allAntsOrdersPanel.activeSelf)
        {
            allAntsOrdersPanel.SetActive(true);
        }
        
        // Update the all ants mode text every frame
        UpdateAllAntsModeText();
    }
    
    private void CheckForNewAnts()
    {
        // Count current ants
        GameObject[] allyAnts = FindAllyAnts();
        
        // If the count has changed, refresh the buttons
        if (allyAnts.Length != lastAntCount)
        {
            Debug.Log($"Ant count changed from {lastAntCount} to {allyAnts.Length}. Refreshing buttons.");
            
            // Store the currently selected ant's instance ID if there is one
            selectedAntInstanceID = currentlySelectedAnt != null ? currentlySelectedAnt.GetInstanceID() : -1;
            
            // Refresh the buttons
            RefreshAllyAntButtons();
            
            // Try to restore the previous selection if possible
            if (selectedAntInstanceID != -1)
            {
                TryRestoreSelection();
            }
            
            lastAntCount = allyAnts.Length;
            
            // Update the mode text after detecting new ants
            UpdateAllAntsModeText();
        }
    }
    
    private void TryRestoreSelection()
    {
        // Look through all buttons to find the one with the matching ant
        foreach (GameObject buttonObj in createdButtons)
        {
            AntButtonData buttonData = buttonObj.GetComponent<AntButtonData>();
            Button button = buttonObj.GetComponent<Button>();
            
            if (buttonData != null && buttonData.LinkedAnt != null && 
                buttonData.LinkedAnt.GetInstanceID() == selectedAntInstanceID && button != null)
            {
                // Found the previously selected ant, restore selection
                SelectAllyAnt(buttonData.LinkedAnt, button);
                return;
            }
        }
        
        // If we got here, the previously selected ant is gone
        Debug.Log("Previously selected ant is no longer available");
        currentlySelectedAnt = null;
        currentlySelectedButton = null;
        
        // Close the orders panel since the selection is gone
        if (antOrdersPanel != null)
            antOrdersPanel.SetActive(false);
    }
    
    // Refresh buttons while trying to maintain selection
    public void RefreshAllyAntButtons()
    {
        // Clear existing buttons without clearing selection yet
        ClearButtonsPreserveSelection();
        
        // Find all GameObjects that have "AllyAnt(Clone)" in their name
        GameObject[] allyAnts = FindAllyAnts();
        
        if (allyAnts.Length > 0)
        {
            Debug.Log($"Found {allyAnts.Length} ally ants in the scene");
            
            // Create a button for each ally ant
            for (int i = 0; i < allyAnts.Length; i++)
            {
                CreateButtonForAllyAnt(allyAnts[i], i);
            }
            
            // Update the last count
            lastAntCount = allyAnts.Length;
        }
        else
        {
            Debug.Log("No ally ants found in the scene");
            lastAntCount = 0;
            
            // Clear selection if no ants exist
            currentlySelectedAnt = null;
            currentlySelectedButton = null;
            
            // Close panel
            if (antOrdersPanel != null)
                antOrdersPanel.SetActive(false);
        }
    }
    
    // Modified to preserve selection info
    private void ClearButtonsPreserveSelection()
    {
        // Just destroy buttons but don't clear selection references yet
        foreach (GameObject button in createdButtons)
        {
            Destroy(button);
        }
        
        createdButtons.Clear();
    }
    
    // Original clear that resets selection too
    private void ClearButtons()
    {
        // Clear the selected button reference first
        currentlySelectedButton = null;
        currentlySelectedAnt = null;
        selectedAntInstanceID = -1;
        
        // Destroy all previously created buttons
        foreach (GameObject button in createdButtons)
        {
            Destroy(button);
        }
        
        createdButtons.Clear();
    }
    
    private GameObject[] FindAllyAnts()
    {
        // Find all active GameObjects in the scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        // Filter for objects with "AllyAnt(Clone)" in their name
        List<GameObject> allyAnts = new List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("AllyAnt(Clone)") || obj.name.Equals("AllyAnt"))
            {
                allyAnts.Add(obj);
            }
        }
        
        return allyAnts.ToArray();
    }
    
    private void CreateButtonForAllyAnt(GameObject allyAnt, int index)
    {
        // Check if the button prefab exists
        if (allyAntButton == null)
        {
            Debug.LogError("Ally ant button prefab is not assigned!");
            return;
        }
        
        // Instantiate a new button as a child of the container
        GameObject newButton = Instantiate(allyAntButton, buttonContainer);
        
        // Add to our list of created buttons
        createdButtons.Add(newButton);
        
        // Store a reference to the ally ant in the button itself using a custom component
        AntButtonData buttonData = newButton.AddComponent<AntButtonData>();
        buttonData.LinkedAnt = allyAnt;
        
        // Set up a reference to the corresponding ally ant
        Button buttonComponent = newButton.GetComponent<Button>();
        if (buttonComponent != null)
        {
            // We need to capture the button in the closure for the callback
            Button capturedButton = buttonComponent;
            
            // Store a reference to the ally ant and pass the button reference
            buttonComponent.onClick.AddListener(() => SelectAllyAnt(allyAnt, capturedButton));
        }
        
        // Set the button text if it has a Text component
        Text buttonText = newButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = $"Ally Ant {index + 1}";
        }
        
        // Alternative for TextMeshProUGUI
        TextMeshProUGUI tmpText = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = $"Ally Ant {index + 1}";
        }
    }
    
    private string GetAIStateText(AllyAI allyAI)
    {
        // Get the current mode of the ally
        AllyAI.AIMode mode = allyAI.GetCurrentMode();
        
        // Convert the mode to a friendly string
        switch (mode)
        {
            case AllyAI.AIMode.Follow:
                return "Following";
            case AllyAI.AIMode.Wander:
                return "Wandering";
            case AllyAI.AIMode.Carrying:
                return "Carrying Loot";
            case AllyAI.AIMode.GoingToLoot:
                return "Going to Loot";
            case AllyAI.AIMode.Attacking:
                return "Attacking";
            default:
                return "Unknown";
        }
    }
    
    private void SelectAllyAnt(GameObject allyAnt, Button selectedButton)
    {
        // If we already have a selected button, re-enable it
        if (currentlySelectedButton != null)
        {
            currentlySelectedButton.interactable = true;
        }
        
        // Store the currently selected ant and button
        currentlySelectedAnt = allyAnt;
        currentlySelectedButton = selectedButton;
        
        // Store the ant's instance ID for future reference
        selectedAntInstanceID = allyAnt.GetInstanceID();
        
        // Disable the newly selected button to indicate it's the active one
        if (currentlySelectedButton != null)
        {
            currentlySelectedButton.interactable = false;
        }
        
        // Show the orders panel only if we have a valid ant
        if (antOrdersPanel != null && currentlySelectedAnt != null)
        {
            antOrdersPanel.SetActive(true);
            
            // Update the loot mode toggle to match the ant's current setting
            UpdateLootModeToggle();
            
            // Update the current order text with the ant's state
            UpdateOrderPanelText();
            
            // Update button states based on current mode
            UpdateCommandButtonStates();
            
            Debug.Log($"Selected ally ant: {allyAnt.name}");
        }
    }
    
    private void UpdateLootModeToggle()
    {
        if (currentlySelectedAnt == null || lootModeToggle == null)
            return;
            
        AllyAI allyAI = currentlySelectedAnt.GetComponent<AllyAI>();
        if (allyAI != null)
        {
            // Update toggle without triggering the callback
            lootModeToggle.SetIsOnWithoutNotify(allyAI.GetLootModeEnabled());
        }
    }
    
    // Called when the loot mode toggle is changed
    private void ToggleLootMode(bool isOn)
    {
        if (currentlySelectedAnt == null)
            return;
            
        AllyAI allyAI = currentlySelectedAnt.GetComponent<AllyAI>();
        if (allyAI != null)
        {
            allyAI.SetLootModeEnabled(isOn);
            UpdateOrderPanelText();
            
            Debug.Log($"Set loot mode to {(isOn ? "ON" : "OFF")} for {currentlySelectedAnt.name}");
        }
    }
    
    private void UpdateOrderPanelText()
    {
        if (currentlySelectedAnt == null || currentOrderText == null)
            return;
            
        // Get the AllyAI component to check its current state
        AllyAI allyAI = currentlySelectedAnt.GetComponent<AllyAI>();
        if (allyAI != null)
        {
            // Get health information if available
            LivingEntity livingEntity = currentlySelectedAnt.GetComponent<LivingEntity>();
            string healthInfo = "";
            if (livingEntity != null)
            {
                healthInfo = $"\nHealth: {livingEntity.CurrentHealth}/{livingEntity.MaxHealth}";
            }
            
            // Get the loot mode information
            string lootModeInfo = allyAI.GetLootModeEnabled() ? " (Collecting Loot)" : " (Not Collecting Loot)";
            
            // Update the text with the current state, loot mode, and health
            currentOrderText.text = $"Current Order: {GetAIStateText(allyAI)}{lootModeInfo}{healthInfo}";
            
            // Also update button states
            UpdateCommandButtonStates();
        }
        else
        {
            currentOrderText.text = "Unable to get ant state";
        }
    }
    
    private void UpdateCommandButtonStates()
    {
        if (currentlySelectedAnt == null)
            return;
            
        AllyAI allyAI = currentlySelectedAnt.GetComponent<AllyAI>();
        if (allyAI != null)
        {
            AllyAI.AIMode currentMode = allyAI.GetCurrentMode();
            
            // Update Follow button state
            if (followButton != null)
            {
                // Disable Follow button if already in Follow mode or in special states
                followButton.interactable = (currentMode != AllyAI.AIMode.Follow && 
                                           currentMode != AllyAI.AIMode.Carrying && 
                                           currentMode != AllyAI.AIMode.GoingToLoot);
            }
            
            // Update Wander button state
            if (wanderButton != null)
            {
                // Disable Wander button if already in Wander mode or in special states
                wanderButton.interactable = (currentMode != AllyAI.AIMode.Wander && 
                                          currentMode != AllyAI.AIMode.Carrying && 
                                          currentMode != AllyAI.AIMode.GoingToLoot);
            }
        }
        else
        {
            // If we can't get the AI component, disable both buttons
            if (followButton != null) followButton.interactable = false;
            if (wanderButton != null) wanderButton.interactable = false;
        }
    }
    
    // You might want to have a method that gets called when a new ally ant is spawned
    public void OnAllyAntSpawned()
    {
        // Refresh the entire panel
        RefreshAllyAntButtons();
    }
    
    // Close the orders panel and reset selection
    public void CloseOrdersPanel()
    {
        if (antOrdersPanel != null)
            antOrdersPanel.SetActive(false);
        
        // Re-enable the currently selected button
        if (currentlySelectedButton != null)
        {
            currentlySelectedButton.interactable = true;
        }
        
        // Clear all selection references
        currentlySelectedButton = null;
        currentlySelectedAnt = null;
        selectedAntInstanceID = -1;
    }
    
    // Command the selected ant to follow player
    public void CommandFollow()
    {
        if (currentlySelectedAnt == null)
            return;
            
        AllyAI allyAI = currentlySelectedAnt.GetComponent<AllyAI>();
        if (allyAI != null)
        {
            // Only change mode if it's not already in a special state
            AllyAI.AIMode currentMode = allyAI.GetCurrentMode();
            if (currentMode != AllyAI.AIMode.Carrying && currentMode != AllyAI.AIMode.GoingToLoot)
            {
                allyAI.SetMode(AllyAI.AIMode.Follow);
                Debug.Log($"Commanded ant to Follow");
                
                // Update the UI
                UpdateOrderPanelText();
                UpdateCommandButtonStates();
            }
        }
    }
    
    // Command the selected ant to wander
    public void CommandWander()
    {
        if (currentlySelectedAnt == null)
            return;
            
        AllyAI allyAI = currentlySelectedAnt.GetComponent<AllyAI>();
        if (allyAI != null)
        {
            // Only change mode if it's not already in a special state
            AllyAI.AIMode currentMode = allyAI.GetCurrentMode();
            if (currentMode != AllyAI.AIMode.Carrying && currentMode != AllyAI.AIMode.GoingToLoot)
            {
                allyAI.SetMode(AllyAI.AIMode.Wander);
                Debug.Log($"Commanded ant to Wander");
                
                // Update the UI
                UpdateOrderPanelText();
                UpdateCommandButtonStates();
            }
        }
    }

    // Handle panel being enabled/disabled
    private void OnEnable()
    {
        // Clear any previous selection when the panel is enabled
        currentlySelectedAnt = null;
        currentlySelectedButton = null;
        selectedAntInstanceID = -1;
        
        // Hide individual orders panel
        if (antOrdersPanel != null)
            antOrdersPanel.SetActive(false);
            
        // Show the all ants orders panel
        if (allAntsOrdersPanel != null)
            allAntsOrdersPanel.SetActive(true);
            
        // Update the All Ants panel
        UpdateAllAntsOrdersPanel();
        
        // Force update the mode text
        UpdateAllAntsModeText();
    }
    
    // Add this method to close the orders panel when the controller is disabled
    private void OnDisable()
    {
        // Make sure orders panel is closed when controller is disabled
        if (antOrdersPanel != null)
        {
            antOrdersPanel.SetActive(false);
        }
        
        // Also hide the all ants orders panel
        if (allAntsOrdersPanel != null)
            allAntsOrdersPanel.SetActive(false);
        
        // Re-enable any selected button
        if (currentlySelectedButton != null)
        {
            currentlySelectedButton.interactable = true;
        }
        
        // Clear all selection references
        currentlySelectedButton = null;
        currentlySelectedAnt = null;
        selectedAntInstanceID = -1;
    }

    // Method to update the All Ants Orders Panel
    private void UpdateAllAntsOrdersPanel()
    {
        if (allAntsOrdersPanel == null)
            return;
            
        // Show the all ants panel
        allAntsOrdersPanel.SetActive(true);
    }
    
    // Command all ants to follow player
    private void CommandAllAntsFollow()
    {
        // Find all ally ants
        GameObject[] allyAnts = FindAllyAnts();
        
        foreach (GameObject ant in allyAnts)
        {
            AllyAI allyAI = ant.GetComponent<AllyAI>();
            if (allyAI != null)
            {
                // Don't change if the ant is in a special state
                AllyAI.AIMode currentMode = allyAI.GetCurrentMode();
                if (currentMode != AllyAI.AIMode.Carrying && currentMode != AllyAI.AIMode.GoingToLoot)
                {
                    allyAI.SetMode(AllyAI.AIMode.Follow);
                }
            }
        }
        
        Debug.Log($"Set all {allyAnts.Length} ants to Follow mode");
        
        // Update the mode text
        UpdateAllAntsModeText();
        
        // Update the selected ant's panel if needed
        if (currentlySelectedAnt != null)
            UpdateOrderPanelText();
    }
    
    // Command all ants to wander
    private void CommandAllAntsWander()
    {
        // Find all ally ants
        GameObject[] allyAnts = FindAllyAnts();
        
        foreach (GameObject ant in allyAnts)
        {
            AllyAI allyAI = ant.GetComponent<AllyAI>();
            if (allyAI != null)
            {
                // Don't change if the ant is in a special state
                AllyAI.AIMode currentMode = allyAI.GetCurrentMode();
                if (currentMode != AllyAI.AIMode.Carrying && currentMode != AllyAI.AIMode.GoingToLoot)
                {
                    allyAI.SetMode(AllyAI.AIMode.Wander);
                }
            }
        }
        
        Debug.Log($"Set all {allyAnts.Length} ants to Wander mode");
        
        // Update the mode text
        UpdateAllAntsModeText();
        
        // Update the selected ant's panel if needed
        if (currentlySelectedAnt != null)
            UpdateOrderPanelText();
    }
    
    // Toggle loot collection for all ants
    private void ToggleAllAntsLootMode()
    {
        // Find all ally ants
        GameObject[] allyAnts = FindAllyAnts();
        
        // Check current loot mode status
        bool allHaveLootEnabled = IsAllAntsLootEnabled();
        bool newLootMode = !allHaveLootEnabled;
        
        foreach (GameObject ant in allyAnts)
        {
            AllyAI allyAI = ant.GetComponent<AllyAI>();
            if (allyAI != null)
            {
                allyAI.SetLootModeEnabled(newLootMode);
            }
        }
        
        Debug.Log($"Set all {allyAnts.Length} ants loot collection to: {(newLootMode ? "ENABLED" : "DISABLED")}");
        
        // Update the mode text
        UpdateAllAntsModeText();
        
        // Update the selected ant's toggle and panel if needed
        if (currentlySelectedAnt != null)
        {
            UpdateLootModeToggle();
            UpdateOrderPanelText();
        }
    }
    
    // Check if all ants have loot collection enabled
    private bool IsAllAntsLootEnabled()
    {
        GameObject[] allyAnts = FindAllyAnts();
        
        if (allyAnts.Length == 0)
            return false;
            
        // Check if all ants have loot enabled
        foreach (GameObject ant in allyAnts)
        {
            AllyAI allyAI = ant.GetComponent<AllyAI>();
            if (allyAI != null && !allyAI.GetLootModeEnabled())
            {
                return false; // Found at least one with loot disabled
            }
        }
        
        return true; // All ants have loot enabled
    }

    // Add this method to update the all ants mode text
    private void UpdateAllAntsModeText()
    {
        if (allAntsFollowModeOrderText == null || 
            allAntsWanderModeOrderText == null ||
            allAntsLootModeOrderText == null)
            return;
            
        // Find all ally ants
        GameObject[] allyAnts = FindAllyAnts();
        
        if (allyAnts.Length == 0)
        {
            allAntsFollowModeOrderText.text = "None Following";
            allAntsWanderModeOrderText.text = "None Wandering";
            allAntsLootModeOrderText.text = "None Looting";
            return;
        }
        
        // Counters for different states
        int followingCount = 0;
        int wanderingCount = 0;
        int lootEnabledCount = 0;
        int otherCount = 0;
        
        foreach (GameObject ant in allyAnts)
        {
            AllyAI allyAI = ant.GetComponent<AllyAI>();
            if (allyAI != null)
            {
                AllyAI.AIMode currentMode = allyAI.GetCurrentMode();
                
                // Skip ants in special states for movement mode counting
                if (currentMode == AllyAI.AIMode.Carrying || 
                    currentMode == AllyAI.AIMode.GoingToLoot)
                    continue;
                    
                if (currentMode == AllyAI.AIMode.Follow)
                    followingCount++;
                else if (currentMode == AllyAI.AIMode.Wander)
                    wanderingCount++;
                else
                    otherCount++;
                    
                // Count loot mode status
                if (allyAI.GetLootModeEnabled())
                    lootEnabledCount++;
            }
        }
        
        // Calculate total ants in normal states
        int totalNormalAnts = followingCount + wanderingCount + otherCount;
        
        // Update follow text
        if (followingCount == 0)
            allAntsFollowModeOrderText.text = "None Following";
        else if (followingCount == totalNormalAnts)
            allAntsFollowModeOrderText.text = "All Following";
        else
            allAntsFollowModeOrderText.text = "Custom";
        
        // Update wander text
        if (wanderingCount == 0)
            allAntsWanderModeOrderText.text = "None Wandering";
        else if (wanderingCount == totalNormalAnts)
            allAntsWanderModeOrderText.text = "All Wandering";
        else
            allAntsWanderModeOrderText.text = "Custom";
        
        // Update loot text
        if (lootEnabledCount == 0)
            allAntsLootModeOrderText.text = "None Collecting";
        else if (lootEnabledCount == allyAnts.Length)
            allAntsLootModeOrderText.text = "All Collecting";
        else
            allAntsLootModeOrderText.text = "Custom";
    }
}

// Helper class to store ant reference on button
public class AntButtonData : MonoBehaviour
{
    public GameObject LinkedAnt;
}
