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
    public float slideDuration = 1.5f; // Time mission stays visible
    public float slideOutDuration = 0.5f; // Time it takes for panel to slide out
    
    [Header("Mission UI Elements")]
    public TextMeshProUGUI missionText; // Reference to the mission text component
    public Image missionIcon; // Reference to the mission icon component
    public RectTransform panelRectTransform; // Reference to the panel's RectTransform
    
    [Header("Mission Icons")]
    public List<Sprite> missionIcons = new List<Sprite>(); // List of possible mission icons
    
    [Header("Starting Position")]
    public Vector2 hiddenPosition = new Vector2(-400f, 0f); // Off-screen position
    public Vector2 visiblePosition = new Vector2(20f, 0f); // On-screen position
    
    private bool isAnimating = false;
    
    private void Awake()
    {
        // Get references if not set in inspector
        if (missionPanel != null)
        {
            if (missionText == null)
                missionText = missionPanel.transform.Find("MissionText")?.GetComponent<TextMeshProUGUI>();
                
            if (missionIcon == null)
                missionIcon = missionPanel.transform.Find("MissionIcon")?.GetComponent<Image>();
                
            if (panelRectTransform == null)
                panelRectTransform = missionPanel.GetComponent<RectTransform>();
        }
        
        // Initially hide the panel
        HideMissionPanel();
    }
    
    private void Start()
    {
        // Example of how to show a mission (for demonstration)
        // ShowMission("Collect 10 pieces of chitin", 0);
    }
    
    /// <summary>
    /// Shows a mission notification with text and icon
    /// </summary>
    /// <param name="text">The mission text to display</param>
    /// <param name="iconIndex">Index of the icon in the missionIcons list</param>
    public void ShowMission(string text, int iconIndex)
    {
        if (isAnimating)
            return;
            
        // Set the mission text
        if (missionText != null)
            missionText.text = text;
            
        // Set the mission icon if valid index provided
        if (missionIcon != null && iconIndex >= 0 && iconIndex < missionIcons.Count)
            missionIcon.sprite = missionIcons[iconIndex];
            
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
        
        // Slide in animation
        float elapsedTime = 0;
        Vector2 startPos = hiddenPosition;
        
        while (elapsedTime < slideInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / slideInDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            panelRectTransform.anchoredPosition = Vector2.Lerp(startPos, visiblePosition, smoothT);
            yield return null;
        }
        
        // Make sure it's at exactly the visible position
        panelRectTransform.anchoredPosition = visiblePosition;
        
        // Wait for the mission to be read
        yield return new WaitForSeconds(slideDuration);
        
        // Slide out animation
        elapsedTime = 0;
        startPos = visiblePosition;
        
        while (elapsedTime < slideOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / slideOutDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            panelRectTransform.anchoredPosition = Vector2.Lerp(startPos, hiddenPosition, smoothT);
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
