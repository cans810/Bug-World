using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AlliesPanelDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttributes playerAttributes;
    [SerializeField] private PlayerInventory playerInventory;
    
    [Header("Panel Settings")]
    [SerializeField] private bool showOnLevelUp = true;
    [SerializeField] private GameObject attributesPanel;
    [SerializeField] private Animator panelAnimator; // Optional
    
    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI availablePointsText;
    [SerializeField] private TextMeshProUGUI strengthValueText;
    [SerializeField] private TextMeshProUGUI vitalityValueText;
    [SerializeField] private TextMeshProUGUI strengthBonusText;
    [SerializeField] private TextMeshProUGUI vitalityBonusText;
    
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
            
        // Set up button listeners
        if (strengthButton != null)
            strengthButton.onClick.AddListener(OnStrengthButtonClicked);
            
        if (vitalityButton != null)
            vitalityButton.onClick.AddListener(OnVitalityButtonClicked);
            
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
                availablePointsText.text = $"Attribute Points: {playerAttributes.AvailablePoints}";
                
            // Update strength
            if (strengthValueText != null)
                strengthValueText.text = $"{playerAttributes.StrengthPoints}/10";
                
            if (strengthBonusText != null)
                strengthBonusText.text = $"+{(playerAttributes.StrengthMultiplier - 1) * 100:F0}% Damage";
                
            // Update vitality
            if (vitalityValueText != null)
                vitalityValueText.text = $"{playerAttributes.VitalityPoints}/10";
                
            if (vitalityBonusText != null)
                vitalityBonusText.text = $"+{(playerAttributes.VitalityMultiplier - 1) * 100:F0}% Health";
                
            // Update button interactability
            if (strengthButton != null)
                strengthButton.interactable = playerAttributes.AvailablePoints > 0 && playerAttributes.StrengthPoints < 10;
                
            if (vitalityButton != null)
                vitalityButton.interactable = playerAttributes.AvailablePoints > 0 && playerAttributes.VitalityPoints < 10;
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