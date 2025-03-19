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
    [SerializeField] private TextMeshProUGUI currentOrderText; // Text showing current order/state

    [SerializeField] private Butt currentOrderText; // Text showing current order/state
    
    private List<GameObject> createdButtons = new List<GameObject>();
    private GameObject currentlySelectedAnt = null;
    
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
    }
    
    // Can be called to refresh the buttons when new ally ants are spawned
    public void RefreshAllyAntButtons()
    {
        // Clear any existing buttons
        ClearButtons();
        
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
        }
        else
        {
            Debug.Log("No ally ants found in the scene");
        }
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
            // Store a reference to the ally ant
            buttonComponent.onClick.AddListener(() => SelectAllyAnt(allyAnt));
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
    
    private void SelectAllyAnt(GameObject allyAnt)
    {
        // Store the currently selected ant
        currentlySelectedAnt = allyAnt;
        
        // Show the orders panel
        if (antOrdersPanel != null)
            antOrdersPanel.SetActive(true);
        
        // Update the current order text with the ant's state
        UpdateOrderPanelText();
        
        Debug.Log($"Selected ally ant: {allyAnt.name}");
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
            
            // Update the text with the current state and health
            currentOrderText.text = $"Current Order: {GetAIStateText(allyAI)}{healthInfo}";
        }
        else
        {
            currentOrderText.text = "Unable to get ant state";
        }
    }
    
    private void ClearButtons()
    {
        // Destroy all previously created buttons
        foreach (GameObject button in createdButtons)
        {
            Destroy(button);
        }
        
        createdButtons.Clear();
    }
    
    // You might want to have a method that gets called when a new ally ant is spawned
    public void OnAllyAntSpawned()
    {
        // Refresh the entire panel
        RefreshAllyAntButtons();
    }
    
    // Close the orders panel
    public void CloseOrdersPanel()
    {
        if (antOrdersPanel != null)
            antOrdersPanel.SetActive(false);
            
        currentlySelectedAnt = null;
    }
    
    // Command the selected ant to follow player
    public void CommandFollow()
    {
        if (currentlySelectedAnt == null)
            return;
            
        AllyAI allyAI = currentlySelectedAnt.GetComponent<AllyAI>();
        if (allyAI != null)
        {
            allyAI.SetMode(AllyAI.AIMode.Follow);
            UpdateOrderPanelText();
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
            allyAI.SetMode(AllyAI.AIMode.Wander);
            UpdateOrderPanelText();
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // If we have a selected ant, keep updating the info
        if (currentlySelectedAnt != null && antOrdersPanel.activeSelf)
        {
            UpdateOrderPanelText();
        }
    }
}

// Helper class to store ant reference on button
public class AntButtonData : MonoBehaviour
{
    public GameObject LinkedAnt;
}
