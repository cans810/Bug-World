using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AttributeDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttributes playerAttributes;
    [SerializeField] private PlayerInventory playerInventory;
    
    [Header("Sound Effects")]
    [SerializeField] private string attributeAddedSoundName = "AttributeAdded";
    [SerializeField] private bool useSoundEffectManager = true;
    [SerializeField] private AudioClip attributeAddedSound; // Fallback sound
    
    [Header("Panel Settings")]
    [SerializeField] private bool showOnLevelUp = false;
    [SerializeField] private GameObject attributesPanel;
    [SerializeField] private Animator panelAnimator; // Optional
    
    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI availablePointsText;
    [SerializeField] private TextMeshProUGUI strengthValueText;
    [SerializeField] private TextMeshProUGUI vitalityValueText;
    [SerializeField] private TextMeshProUGUI incubationValueText;  // New attribute
    [SerializeField] private TextMeshProUGUI agilityValueText;     // New attribute
    [SerializeField] private TextMeshProUGUI strengthBonusText;
    [SerializeField] private TextMeshProUGUI vitalityBonusText;
    [SerializeField] private TextMeshProUGUI incubationBonusText;  // New attribute
    [SerializeField] private TextMeshProUGUI agilityBonusText;     // New attribute
    
    [Header("Buttons")]
    [SerializeField] private Button strengthButton;
    [SerializeField] private Button vitalityButton;
    [SerializeField] private Button incubationButton;  // New attribute
    [SerializeField] private Button agilityButton;     // New attribute
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
        {
            playerAttributes.OnAttributesChanged += UpdateDisplay;
            playerAttributes.OnIncubationChanged += UpdateIncubationDisplay;
        }
            
        if (playerInventory != null)
            playerInventory.OnLevelUp += OnPlayerLevelUp;
            
        // Set up button listeners
        if (strengthButton != null)
            strengthButton.onClick.AddListener(OnStrengthButtonClicked);
            
        if (vitalityButton != null)
            vitalityButton.onClick.AddListener(OnVitalityButtonClicked);
            
        if (incubationButton != null)
            incubationButton.onClick.AddListener(OnIncubationButtonClicked);
            
        if (agilityButton != null)
            agilityButton.onClick.AddListener(OnAgilityButtonClicked);
            
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
        // Update the display values but don't show the panel immediately
        // The panel will be shown by UIHelper after the level up animation
        UpdateDisplay();
        
        // Only show panel if we're set to show it immediately 
        // (which should now be disabled by default)
        if (showOnLevelUp && attributesPanel != null)
        {
            ShowPanel();
        }
    }
    
    public void ShowPanel()
    {
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(true);
            
            // If using an animator, trigger show animation
            if (panelAnimator != null)
            {
                panelAnimator.SetBool("ShowUp", true);
                panelAnimator.SetBool("Hide", false);
            }
        }
    }

    private void HidePanel()
    {
        attributesPanel.SetActive(false);
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
                
            // Update incubation
            if (incubationValueText != null)
                incubationValueText.text = $"{playerAttributes.IncubationPoints}/5";
                
            if (incubationBonusText != null)
                incubationBonusText.text = $"+{playerAttributes.IncubationPoints} Egg Capacity";
                
            // Update agility
            if (agilityValueText != null)
                agilityValueText.text = $"{playerAttributes.AgilityPoints}/10";
                
            if (agilityBonusText != null)
                agilityBonusText.text = $"-{(playerAttributes.AgilityMultiplier - 1) * 100:F0}% Attack Cooldown";
                
            // Update button interactability
            if (strengthButton != null)
                strengthButton.interactable = playerAttributes.AvailablePoints > 0 && playerAttributes.StrengthPoints < 10;
                
            if (vitalityButton != null)
                vitalityButton.interactable = playerAttributes.AvailablePoints > 0 && playerAttributes.VitalityPoints < 10;
                
            if (incubationButton != null)
                incubationButton.interactable = playerAttributes.AvailablePoints > 0 && playerAttributes.IncubationPoints < 5;
                
            if (agilityButton != null)
                agilityButton.interactable = playerAttributes.AvailablePoints > 0 && playerAttributes.AgilityPoints < 10;
        }
    }
    
    // Play sound when attribute is added
    private void PlayAttributeAddedSound()
    {
        if (useSoundEffectManager && SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound(attributeAddedSoundName, transform.position, false); // Use 2D sound for UI
        }
        else if (attributeAddedSound != null)
        {
            AudioSource.PlayClipAtPoint(attributeAddedSound, Camera.main.transform.position);
        }
    }
    
    private void OnStrengthButtonClicked()
    {
        if (playerAttributes != null)
        {
            bool success = playerAttributes.IncreaseStrength();
            if (success)
                PlayAttributeAddedSound();
        }
    }
    
    private void OnVitalityButtonClicked()
    {
        if (playerAttributes != null)
        {
            bool success = playerAttributes.IncreaseVitality();
            if (success)
                PlayAttributeAddedSound();
        }
    }
    
    private void OnIncubationButtonClicked()
    {
        if (playerAttributes != null)
        {
            bool success = playerAttributes.IncreaseIncubation();
            if (success)
                PlayAttributeAddedSound();
        }
    }
    
    private void OnAgilityButtonClicked()
    {
        if (playerAttributes != null)
        {
            bool success = playerAttributes.IncreaseAgility();
            if (success)
                PlayAttributeAddedSound();
        }
    }
    
    private void UpdateIncubationDisplay()
    {
        if (playerAttributes != null)
        {
            // Update incubation
            if (incubationValueText != null)
                incubationValueText.text = $"{playerAttributes.IncubationPoints}/5";
                
            if (incubationBonusText != null)
                incubationBonusText.text = $"+{playerAttributes.IncubationPoints} Egg Capacity";
                
            // Update button interactability
            if (incubationButton != null)
                incubationButton.interactable = playerAttributes.AvailablePoints > 0 && playerAttributes.IncubationPoints < 5;
        }
        
        // Find and update any AntIncubator UI that might be showing
        AntIncubator[] incubators = FindObjectsOfType<AntIncubator>();
        foreach (AntIncubator incubator in incubators)
        {
            incubator.UpdateUI();
        }
    }
    
    private void OnDestroy()
    {
        if (playerAttributes != null)
        {
            playerAttributes.OnAttributesChanged -= UpdateDisplay;
            playerAttributes.OnIncubationChanged -= UpdateIncubationDisplay;
        }
            
        if (playerInventory != null)
            playerInventory.OnLevelUp -= OnPlayerLevelUp;
            
        if (strengthButton != null)
            strengthButton.onClick.RemoveListener(OnStrengthButtonClicked);
            
        if (vitalityButton != null)
            vitalityButton.onClick.RemoveListener(OnVitalityButtonClicked);
            
        if (incubationButton != null)
            incubationButton.onClick.RemoveListener(OnIncubationButtonClicked);
            
        if (agilityButton != null)
            agilityButton.onClick.RemoveListener(OnAgilityButtonClicked);
            
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HidePanel);
    }
} 