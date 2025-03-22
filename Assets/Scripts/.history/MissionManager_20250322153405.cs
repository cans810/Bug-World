using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionManager : MonoBehaviour
{
    [Header("Mission Panel")]
    public GameObject missionPanel; // Reference to the mission panel prefab
    public float slideInDuration = 0.5f; // Time it takes for panel to slide in
    public float slideDuration = 3.0f; // Time mission stays visible (increased from 1.5)
    public float slideOutDuration = 0.5f; // Time it takes for panel to slide out
    
    [Header("Mission Icons")]
    public List<Sprite> missionIcons = new List<Sprite>(); // List of possible mission icons
    
    [Header("Panel Settings")]
    public Vector2 hiddenPosition = new Vector2(-400f, 0f); // Off-screen position
    public Vector2 visiblePosition = new Vector2(20f, 0f); // On-screen position
    
    // Child references - will be found automatically
    private TextMeshProUGUI missionText;
    private Image missionIcon;
    private RectTransform panelRectTransform;
    
    private bool isAnimating = false;
    
    private void Awake()
    {
        // Get references from the children of mission panel
        if (missionPanel != null)
        {
            // Get RectTransform for animations
            panelRectTransform = missionPanel.GetComponent<RectTransform>();
            
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
        // For testing - uncomment to see a mission notification on start
        ShowMission("Hunt 1 ant", 0);
    }
    
    /// <summary>
    /// Shows a mission notification with text and icon
    /// </summary>
    /// <param name="text">The mission text to display</param>
    /// <param name="iconIndex">Index of the icon in the missionIcons list</param>
    public void ShowMission(string text, int iconIndex)
    {
        if (isAnimating || missionPanel == null)
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
            
        // Show the mission panel with animation
        StartCoroutine(AnimateMissionPanel());
    }
    
    /// <summary>
    /// Animates the mission panel sliding in, staying, then sliding out
    /// </summary>
    private IEnumerator AnimateMissionPanel()
    {
        isAnimating = true;
        
        // Make panel visible
        missionPanel.SetActive(true);
        
        // Initial position
        panelRectTransform.anchoredPosition = hiddenPosition;
        
        // Slide in animation
        float elapsedTime = 0;
        
        while (elapsedTime < slideInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / slideInDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            panelRectTransform.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, smoothT);
            yield return null;
        }
        
        // Make sure it's at exactly the visible position
        panelRectTransform.anchoredPosition = visiblePosition;
        
        // Wait for the mission to be read
        yield return new WaitForSeconds(slideDuration);
        
        // Slide out animation
        elapsedTime = 0;
        
        while (elapsedTime < slideOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / slideOutDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            panelRectTransform.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, smoothT);
            yield return null;
        }
        
        // Make sure it's at exactly the hidden position
        panelRectTransform.anchoredPosition = hiddenPosition;
        
        // Hide the panel
        missionPanel.SetActive(false);
        
        isAnimating = false;
    }
    
    /// <summary>
    /// Hides the mission panel immediately
    /// </summary>
    public void HideMissionPanel()
    {
        if (panelRectTransform != null)
            panelRectTransform.anchoredPosition = hiddenPosition;
            
        if (missionPanel != null)
            missionPanel.SetActive(false);
            
        isAnimating = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
