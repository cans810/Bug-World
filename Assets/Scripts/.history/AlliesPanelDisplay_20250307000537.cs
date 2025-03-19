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
    [SerializeField] private GameObject alliesPanel;
    [SerializeField] private Animator panelAnimator; // Optional
    
    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI availablePointsText;
    [SerializeField] private TextMeshProUGUI antPointsText;
    
    [Header("Buttons")]
    [SerializeField] private Button addPointsOnAnt;
    [SerializeField] private Button closeButton; // Optional close button
    
    private void Start()
    {
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
            
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);
            
        // Initial update
        UpdateDisplay();
        
        // Hide panel initially unless player has points to spend
        if (alliesPanel != null && (playerAttributes == null || playerAttributes.AvailablePoints <= 0))
        {
            alliesPanel.SetActive(false);
        }
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        // Show the panel when player levels up
        if (showOnBreadCrumbsFilled && alliesPanel != null)
        {
            ShowPanel();
        }
    }
    
    private void ShowPanel()
    {
        if (alliesPanel != null)
        {
            alliesPanel.SetActive(true);
            
            // If using an animator, trigger show animation
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger("Show");
            }
        }
    }
    
    private void HidePanel()
    {
        if (alliesPanel != null)
        {
            // If using an animator, trigger hide animation
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger("Hide");
                // Panel will be deactivated by animation event
            }
            else
            {
                alliesPanel.SetActive(false);
            }
        }
    }
    
    private void UpdateDisplay()
    {
        if (playerAttributes != null)
        {
            // Update available points
            if (availablePointsText != null)
                availablePointsText.text = $"Ally Points: {playerAttributes.AvailablePoints}";
        }
    }
    
    private void OnStrengthButtonClicked()
    {
        if (playerAttributes != null)
            playerAttributes.IncreaseStrength();
    }
    
    private void OnVitalityButtonClicked()
    {
        if (playerAttributes != null)
            playerAttributes.IncreaseVitality();
    }
    
    private void OnDestroy()
    {
        if (playerAttributes != null)
            playerAttributes.OnAttributesChanged -= UpdateDisplay;
            
        if (playerInventory != null)
            playerInventory.OnLevelUp -= OnPlayerLevelUp;
            
        if (strengthButton != null)
            strengthButton.onClick.RemoveListener(OnStrengthButtonClicked);
            
        if (vitalityButton != null)
            vitalityButton.onClick.RemoveListener(OnVitalityButtonClicked);
            
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HidePanel);
    }
} 
} 