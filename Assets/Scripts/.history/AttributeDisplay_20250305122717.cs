using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AttributeDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttributes playerAttributes;
    
    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI availablePointsText;
    [SerializeField] private TextMeshProUGUI strengthValueText;
    [SerializeField] private TextMeshProUGUI vitalityValueText;
    [SerializeField] private TextMeshProUGUI strengthBonusText;
    [SerializeField] private TextMeshProUGUI vitalityBonusText;
    
    [Header("Buttons")]
    [SerializeField] private Button strengthButton;
    [SerializeField] private Button vitalityButton;
    
    private void Start()
    {
        if (playerAttributes == null)
            playerAttributes = FindObjectOfType<PlayerAttributes>();
            
        // Subscribe to attribute changes
        if (playerAttributes != null)
            playerAttributes.OnAttributesChanged += UpdateDisplay;
            
        // Set up button listeners
        if (strengthButton != null)
            strengthButton.onClick.AddListener(OnStrengthButtonClicked);
            
        if (vitalityButton != null)
            vitalityButton.onClick.AddListener(OnVitalityButtonClicked);
            
        // Initial update
        UpdateDisplay();
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
            
        if (strengthButton != null)
            strengthButton.onClick.RemoveListener(OnStrengthButtonClicked);
            
        if (vitalityButton != null)
            vitalityButton.onClick.RemoveListener(OnVitalityButtonClicked);
    }
} 