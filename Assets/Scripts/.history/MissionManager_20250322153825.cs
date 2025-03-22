using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionManager : MonoBehaviour
{
    [Header("Mission Panel")]
    public GameObject missionPanelPrefab; // Reference to the mission panel prefab
    
    [Header("Mission Icons")]
    public List<Sprite> missionIcons = new List<Sprite>(); // List of possible mission icons
    
    [Header("Position Settings")]
    public Transform missionContainer; // Container for instantiated mission panels
    
    // Currently active mission panels
    private List<GameObject> activeMissionPanels = new List<GameObject>();
    
    private void Awake()
    {
        // If no container is assigned, use this transform
        if (missionContainer == null)
        {
            missionContainer = transform;
        }
    }
    
    private void Start()
    {
        // For testing - show a mission notification on start
        ShowMission("Hunt 1 ant", 0);
    }
    
    /// <summary>
    /// Shows a mission by instantiating a mission panel from the prefab
    /// </summary>
    /// <param name="text">The mission text to display</param>
    /// <param name="iconIndex">Index of the icon in the missionIcons list</param>
    public void ShowMission(string text, int iconIndex)
    {
        if (missionPanelPrefab == null)
        {
            Debug.LogError("Mission panel prefab not assigned!");
            return;
        }
        
        // Instantiate the mission panel from the prefab
        GameObject missionPanelInstance = Instantiate(missionPanelPrefab, missionContainer);
        
        // Find the components in the instantiated panel
        TextMeshProUGUI missionText = missionPanelInstance.transform.Find("MissionText")?.GetComponent<TextMeshProUGUI>();
        Image missionIcon = missionPanelInstance.transform.Find("MissionIcon")?.GetComponent<Image>();
        
        // Set the mission text
        if (missionText != null)
            missionText.text = text;
        else
            Debug.LogWarning("MissionText component not found in instantiated panel");
            
        // Set the mission icon if valid index provided
        if (missionIcon != null && iconIndex >= 0 && iconIndex < missionIcons.Count)
            missionIcon.sprite = missionIcons[iconIndex];
        else
            Debug.LogWarning("MissionIcon component not found or invalid icon index");
            
        // Play a notification sound
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("Notification", transform.position, false);
        }
        
        // Add to active missions list
        activeMissionPanels.Add(missionPanelInstance);
        
        Debug.Log($"Mission created: {text}");
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
    }
    
    /// <summary>
    /// Completes a mission by text (removes its panel)
    /// </summary>
    public void CompleteMission(string missionText)
    {
        GameObject panelToRemove = null;
        
        // Find the mission panel with matching text
        foreach (GameObject panel in activeMissionPanels)
        {
            if (panel == null)
                continue;
                
            TextMeshProUGUI textComponent = panel.transform.Find("MissionText")?.GetComponent<TextMeshProUGUI>();
            if (textComponent != null && textComponent.text == missionText)
            {
                panelToRemove = panel;
                break;
            }
        }
        
        // Remove the panel if found
        if (panelToRemove != null)
        {
            activeMissionPanels.Remove(panelToRemove);
            Destroy(panelToRemove);
            Debug.Log($"Mission completed: {missionText}");
            
            // Play completion sound
            SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
            if (soundManager != null)
            {
                soundManager.PlaySound("Success", transform.position, false);
            }
        }
    }
}
