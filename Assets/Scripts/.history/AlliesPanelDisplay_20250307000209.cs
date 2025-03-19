using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AlliesPanelDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttributes playerAttributes;
    [SerializeField] private PlayerInventory playerInventory;
    
    [Header("Panel Settings")]
    [SerializeField] private bool showOnBreadCrumbsFilled = true;
    [SerializeField] private bool showOnLevelUp = true;
    [SerializeField] private GameObject alliesPanel;
    [SerializeField] private Animator panelAnimator; // Optional
    
    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI availablePointsText;
    [SerializeField] private TextMeshProUGUI antPointsText;
    
    [Header("Ant Stats")]
    [SerializeField] private TextMeshProUGUI antValueText;
    [SerializeField] private TextMeshProUGUI antBonusText;
    
    [Header("Buttons")]
    [SerializeField] private Button addPointsOnAnt;
    [SerializeField] private Button closeButton; // Optional close button
    
    // Reference to the main panel
    private GameObject attributesPanel;
    
    private void Start()
    {
        // Set the attributesPanel to the alliesPanel
        attributesPanel = alliesPanel;
        
        // Find references if not assigned
        if (playerAttributes == null)
            playerAttributes = FindObjectOfType<PlayerAttributes>();
            
        if (playerInventory == null)
            playerInventory = FindObjectOfType<PlayerInventory>();
            
        // Subscribe to attribute changes and level up events
        if (playerAttributes != null)
            playerAttributes.OnAttributesChanged += UpdateDisplay;
            
        if (playerInventory != null)
            playerInventory.OnLevelUp += OnPlayerLevelUp;
            
        // Set up button listeners
        if (addPointsOnAnt != null)
            addPointsOnAnt.onClick.AddListener(OnAddAntPointsClicked);
            
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);
            
        // Initial update
        UpdateDisplay();
        
        // Hide panel initially unless player has points to spend
        if (attributesPanel != null && (playerAttributes == null || playerAttributes.AvailablePoints <= 0))
        {
            attributesPanel.SetActive(false);
        }
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        // Show the panel when player levels up
        if (showOnLevelUp && attributesPanel != null)
        {
            ShowPanel();
        }
    }
    
    private void ShowPanel()
    {
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(true);
            
            // If using an animator, trigger show animation
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger("Show");
            }
        }
    }
    
    private void HidePanel()
    {
        if (attributesPanel != null)
        {
            // If using an animator, trigger hide animation
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger("Hide");
                // Panel will be deactivated by animation event
            }
            else
            {
                attributesPanel.SetActive(false);
            }
        }
    }
    
    private void UpdateDisplay()
    {
        if (playerAttributes != null)
        {
            // Update available points
            if (availablePointsText != null)
                availablePointsText.text = $"Available Points: {playerAttributes.AvailablePoints}";
                
            // Update ant points
            if (antValueText != null)
                antValueText.text = $"{playerAttributes.AntPoints}/10";
                
            if (antBonusText != null)
                antBonusText.text = $"+{(playerAttributes.AntMultiplier - 1) * 100:F0}% Ant Strength";
                
            // Update button interactability
            if (addPointsOnAnt != null)
                addPointsOnAnt.interactable = playerAttributes.AvailablePoints > 0 && playerAttributes.AntPoints < 10;
        }
    }
    
    private void OnAddAntPointsClicked()
    {
        if (playerAttributes != null)
            playerAttributes.IncreaseAntPoints();
    }
    
    private void OnDestroy()
    {
        if (playerAttributes != null)
            playerAttributes.OnAttributesChanged -= UpdateDisplay;
            
        if (playerInventory != null)
            playerInventory.OnLevelUp -= OnPlayerLevelUp;
            
        if (addPointsOnAnt != null)
            addPointsOnAnt.onClick.RemoveListener(OnAddAntPointsClicked);
            
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HidePanel);
    }
} 
} 