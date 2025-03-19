using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AllyAntPanelController : MonoBehaviour
{
    public GameObject allyAntButton;
    
    [SerializeField] private Transform buttonContainer; // Container to hold the buttons

    [SerializeField] private GameObject antOrdersPanel; // Container to hold the buttons
    
    private List<GameObject> createdButtons = new List<GameObject>();
    
    // Start is called before the first frame update
    void Start()
    {
        // If no container is assigned, use this object as the container
        if (buttonContainer == null)
            buttonContainer = transform;
            
        // Find and create buttons for all ally ants at start
        RefreshAllyAntButtons();
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
    }
    
    private void SelectAllyAnt(GameObject allyAnt)
    {
        // Implement your selection logic here
        Debug.Log($"Selected ally ant: {allyAnt.name}");
        antOrdersPanel.SetActive(true);
        // Example: You could focus the camera on this ant, select it for commands, etc.
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
