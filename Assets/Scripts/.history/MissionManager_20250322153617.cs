using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionManager : MonoBehaviour
{
    [Header("Mission Panel")]
    public GameObject missionPanel; // Reference to the mission panel prefab
    
    [Header("Mission Icons")]
    public List<Sprite> missionIcons = new List<Sprite>(); // List of possible mission icons
    
    // Child references - will be found automatically
    private TextMeshProUGUI missionText;
    private Image missionIcon;
    
    private void Awake()
    {
        // Get references from the children of mission panel
        if (missionPanel != null)
        {
            // Find the child components
            missionText = missionPanel.transform.Find("MissionText")?.GetComponent<TextMeshProUGUI>();
            missionIcon = missionPanel.transform.Find("MissionIcon")?.GetComponent<Image>();
            
            if (missionText == null)
                Debug.LogWarning("MissionText child not found on mission panel");
                
            if (missionIcon == null)
                Debug.LogWarning("MissionIcon child not found on mission panel");
        }
        else
        {
            Debug.LogError("Mission panel not assigned!");
        }
        
        // Initially hide the panel
        HideMissionPanel();
    }
    
    private void Start()
    {
        // For testing - show a mission notification on start
        ShowMission("Hunt 1 ant", 0);
    }
    
    /// <summary>
    /// Shows a mission notification with text and icon
    /// </summary>
    /// <param name="text">The mission text to display</param>
    /// <param name="iconIndex">Index of the icon in the missionIcons list</param>
    public void ShowMission(string text, int iconIndex)
    {
        if (missionPanel == null)
            return;
            
        // Set the mission text
        if (missionText != null)
            missionText.text = text;
            
        // Set the mission icon if valid index provided
        if (missionIcon != null && iconIndex >= 0 && iconIndex < missionIcons.Count)
            missionIcon.sprite = missionIcons[iconIndex];
            
        // Play a notification sound
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("Notification", transform.position, false);
        }
            
        // Show the mission panel
        missionPanel.SetActive(true);
    }
    
    /// <summary>
    /// Hides the mission panel
    /// </summary>
    public void HideMissionPanel()
    {
        if (missionPanel != null)
            missionPanel.SetActive(false);
    }
}
