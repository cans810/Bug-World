using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] private GameObject attributesPanel;
    [SerializeField] private Animator panelAnimator; // Optional
    
    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI availablePointsText;
    [SerializeField] private TextMeshProUGUI strengthValueText;
    [SerializeField] private TextMeshProUGUI vitalityValueText;
    [SerializeField] private TextMeshProUGUI incubationValueText;
    [SerializeField] private TextMeshProUGUI agilityValueText;
    [SerializeField] private TextMeshProUGUI recoveryValueText;
    [SerializeField] private TextMeshProUGUI speedValueText;
    [SerializeField] private TextMeshProUGUI strengthBonusText;
    [SerializeField] private TextMeshProUGUI vitalityBonusText;
    [SerializeField] private TextMeshProUGUI incubationBonusText;
    [SerializeField] private TextMeshProUGUI agilityBonusText;
    [SerializeField] private TextMeshProUGUI recoveryBonusText;
    [SerializeField] private TextMeshProUGUI speedBonusText;

    [Header("Buttons")]
    [SerializeField] private Button strengthButton;
    [SerializeField] private Button vitalityButton;
    [SerializeField] private Button incubationButton;
    [SerializeField] private Button agilityButton;
    [SerializeField] private Button recoveryButton;
    [SerializeField] private Button speedButton;
    [SerializeField] private Button closeButton; // Optional close button
    
    private CameraAnimations cameraAnimations;
    private bool shouldShowAttributesPanel = false;
    private bool isInCameraAnimation = false;

    [Header("Auto-Show Settings")]
    [SerializeField] private bool autoShowOnLevelUp = false; // Set this to false to prevent auto-showing

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
            
        if (recoveryButton != null)
            recoveryButton.onClick.AddListener(OnRecoveryButtonClicked);
            
        if (speedButton != null)
            speedButton.onClick.AddListener(OnSpeedButtonClicked);
            
        // Initial update
        UpdateDisplay();
        
        // Hide panel initially - ALWAYS hide on game start/load
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(false);
        }

        // Subscribe to camera animation completion
        cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null)
        {
            cameraAnimations.OnCameraAnimationCompleted += ShowPanelAfterCameraAnimation;
        }
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        // Refresh the display when player levels up
        RefreshDisplay();
        
        // Only show panel automatically if the setting is enabled
        // We're setting this to false by default now
        if (autoShowOnLevelUp && !isCameraAnimationInProgress)
        {
            ShowPanel(true);
        }
    }
    
    public void ShowPanel(bool show)
    {
        // ALWAYS check if we're in camera animation first
        if (isInCameraAnimation)
        {
            Debug.Log("AttributeDisplay: Blocked attempt to show panel during camera animation");
            shouldShowAttributesPanel = show; // Remember to show it later
            return;
        }

        // Get a reference to camera animations if we don't have one
        if (cameraAnimations == null)
        {
            cameraAnimations = FindObjectOfType<CameraAnimations>();
        }

        // Double-check with the camera animations component
        if (cameraAnimations != null && cameraAnimations.IsAnimationInProgress())
        {
            Debug.Log("AttributeDisplay: Blocked panel display during camera animation (via IsAnimationInProgress)");
            shouldShowAttributesPanel = show;
            return;
        }

        if (attributesPanel != null)
        {
            // Add debug log to verify this method is being called
            Debug.Log($"AttributeDisplay.ShowPanel called. Panel active: {attributesPanel.activeSelf}, Show: {show}");
            
            // Only activate if we're showing
            attributesPanel.SetActive(show);
            
            // If using an animator, trigger show animation
            if (panelAnimator != null && show)
            {
                panelAnimator.SetBool("ShowUp", true);
                panelAnimator.SetBool("Hide", false);
                Debug.Log("Setting animator parameters: ShowUp=true, Hide=false");
            }
            else if (panelAnimator != null && !show)
            {
                panelAnimator.SetBool("ShowUp", false);
                panelAnimator.SetBool("Hide", true);
            }
        }
        else
        {
            Debug.LogError("AttributesPanel reference is null in ShowPanel method");
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
                availablePointsText.text = $"{playerAttributes.AvailablePoints}";
                
            // Update strength (use max from PlayerAttributes)
            if (strengthValueText != null)
                strengthValueText.text = $"{playerAttributes.StrengthPoints}/{playerAttributes.MaxStrengthPoints}";
                
            if (strengthBonusText != null)
                strengthBonusText.text = $"+{(playerAttributes.StrengthMultiplier - 1) * 100:F0}% Damage";
                
            // Update vitality (use max from PlayerAttributes)
            if (vitalityValueText != null)
                vitalityValueText.text = $"{playerAttributes.VitalityPoints}/{playerAttributes.MaxVitalityPoints}";
                
            if (vitalityBonusText != null)
                vitalityBonusText.text = $"+{(playerAttributes.VitalityMultiplier - 1) * 100:F0}% Health";
                
            // Update incubation (use max from PlayerAttributes)
            if (incubationValueText != null)
                incubationValueText.text = $"{playerAttributes.IncubationPoints}/{playerAttributes.MaxIncubationPoints}";
                
            if (incubationBonusText != null)
                incubationBonusText.text = $"+{playerAttributes.IncubationPoints} Egg Capacity";
                
            // Update agility (use max from PlayerAttributes)
            if (agilityValueText != null)
                agilityValueText.text = $"{playerAttributes.AgilityPoints}/{playerAttributes.MaxAgilityPoints}";
                
            if (agilityBonusText != null)
                agilityBonusText.text = $"-{(playerAttributes.AgilityMultiplier - 1) * 100:F0}% Attack Cooldown";
                
            // Update recovery (use max from PlayerAttributes)
            if (recoveryValueText != null)
                recoveryValueText.text = $"{playerAttributes.RecoveryPoints}/{playerAttributes.MaxRecoveryPoints}";
                
            if (recoveryBonusText != null)
                recoveryBonusText.text = $"-{(playerAttributes.RecoveryMultiplier - 1) * 100:F0}% Recovery Time";
                
            // Update speed (use max from PlayerAttributes)
            if (speedValueText != null)
                speedValueText.text = $"{playerAttributes.SpeedPoints}/{playerAttributes.MaxSpeedPoints}";
                
            if (speedBonusText != null)
                speedBonusText.text = $"+{(playerAttributes.SpeedMultiplier - 1) * 100:F0}% Movement Speed";
                
            // Update button interactability based on max values
            if (strengthButton != null)
                strengthButton.interactable = playerAttributes.AvailablePoints > 0 && 
                                             playerAttributes.StrengthPoints < playerAttributes.MaxStrengthPoints;
                
            if (vitalityButton != null)
                vitalityButton.interactable = playerAttributes.AvailablePoints > 0 && 
                                             playerAttributes.VitalityPoints < playerAttributes.MaxVitalityPoints;
                
            if (incubationButton != null)
                incubationButton.interactable = playerAttributes.AvailablePoints > 0 && 
                                              playerAttributes.IncubationPoints < playerAttributes.MaxIncubationPoints;
                
            if (agilityButton != null)
                agilityButton.interactable = playerAttributes.AvailablePoints > 0 && 
                                            playerAttributes.AgilityPoints < playerAttributes.MaxAgilityPoints;
                
            if (recoveryButton != null)
                recoveryButton.interactable = playerAttributes.AvailablePoints > 0 && 
                                             playerAttributes.RecoveryPoints < playerAttributes.MaxRecoveryPoints;
                
            if (speedButton != null)
                speedButton.interactable = playerAttributes.AvailablePoints > 0 && 
                                           playerAttributes.SpeedPoints < playerAttributes.MaxSpeedPoints;
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
        
        // Update the UI
        UpdateDisplay();
        
        // Check if we're out of points and should close the panel
        CheckAutoClosePanel();
    }
    
    private void OnVitalityButtonClicked()
    {
        if (playerAttributes != null)
        {
            bool success = playerAttributes.IncreaseVitality();
            if (success)
                PlayAttributeAddedSound();
        }
        
        // Update the UI
        UpdateDisplay();
        
        // Check if we're out of points and should close the panel
        CheckAutoClosePanel();
    }
    
    private void OnIncubationButtonClicked()
    {
        if (playerAttributes != null)
        {
            bool success = playerAttributes.IncreaseIncubation();
            if (success)
                PlayAttributeAddedSound();
        }
        
        // Update the UI
        UpdateDisplay();
        
        // Check if we're out of points and should close the panel
        CheckAutoClosePanel();
    }
    
    private void OnAgilityButtonClicked()
    {
        if (playerAttributes != null)
        {
            bool success = playerAttributes.IncreaseAgility();
            if (success)
                PlayAttributeAddedSound();
        }
        
        // Update the UI
        UpdateDisplay();
        
        // Check if we're out of points and should close the panel
        CheckAutoClosePanel();
    }
    
    private void OnRecoveryButtonClicked()
    {
        if (playerAttributes != null)
        {
            bool success = playerAttributes.IncreaseRecovery();
            if (success)
                PlayAttributeAddedSound();
        }
        
        // Update the UI
        UpdateDisplay();
        
        // Check if we're out of points and should close the panel
        CheckAutoClosePanel();
    }
    
    private void OnSpeedButtonClicked()
    {
        if (playerAttributes != null)
        {
            bool success = playerAttributes.IncreaseSpeed();
            if (success)
                PlayAttributeAddedSound();
        }
        
        // Update the UI
        UpdateDisplay();
        
        // Check if we're out of points and should close the panel
        CheckAutoClosePanel();
    }
    
    private void UpdateIncubationDisplay()
    {
        if (playerAttributes != null)
        {
            // Update incubation (max 20)
            if (incubationValueText != null)
                incubationValueText.text = $"{playerAttributes.IncubationPoints}/20";
                
            if (incubationBonusText != null)
                incubationBonusText.text = $"+{playerAttributes.IncubationPoints} Egg Capacity";
                
            // Update button interactability
            if (incubationButton != null)
                incubationButton.interactable = playerAttributes.AvailablePoints > 0 && playerAttributes.IncubationPoints < 20;
        }
        
        // Find and update any InsectIncubator UI that might be showing
        InsectIncubator[] incubators = FindObjectsOfType<InsectIncubator>();
        foreach (InsectIncubator incubator in incubators)
        {
            incubator.UpdateUI();
        }
    }
    
    // Connected to camera animation completion
    private void ShowPanelAfterCameraAnimation(GameObject areaTarget, int level, string areaName)
    {
        // Skip if we're loading data
        if (playerInventory != null && playerInventory.IsLoadingData)
            return;
        
        Debug.Log("ShowPanelAfterCameraAnimation called - camera animation completed");
        
        // Only show panel if we have points to spend
        if (playerAttributes != null && playerAttributes.AvailablePoints > 0)
        {
            // Don't use a coroutine on the inactive panel - instead set a flag
            // and let the Update method handle showing the panel
            Debug.Log("Setting shouldShowAttributesPanel flag to true");
            shouldShowAttributesPanel = true;
        }
    }

    // Update method to check if we should show the panel
    private void Update()
    {
        // Check if animation has ended and we should show the panel
        if (shouldShowAttributesPanel && cameraAnimations != null && !cameraAnimations.IsAnimationInProgress())
        {
            Debug.Log("Animation ended, now showing attributes panel");
            shouldShowAttributesPanel = false;
            
            // Make sure the panel is active before showing it
            if (attributesPanel != null)
            {
                // First activate the GameObject so we can use it
                attributesPanel.SetActive(true);
                
                // Then set the animator parameters if needed
                if (panelAnimator != null)
                {
                    panelAnimator.SetBool("ShowUp", true);
                    panelAnimator.SetBool("Hide", false);
                }
                
                Debug.Log("Attribute panel activated after camera animation");
            }
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
            
        if (recoveryButton != null)
            recoveryButton.onClick.RemoveListener(OnRecoveryButtonClicked);
            
        if (speedButton != null)
            speedButton.onClick.RemoveListener(OnSpeedButtonClicked);
            
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HidePanel);

        // Unsubscribe from camera animation
        if (cameraAnimations != null)
        {
            cameraAnimations.OnCameraAnimationCompleted -= ShowPanelAfterCameraAnimation;
        }
    }

    public void LoadGameData(GameData data)
    {
        if (data == null) return;
        
        Debug.Log($"AttributeDisplay.LoadGameData called with: Strength={data.currentStrength}, " +
                  $"Vitality={data.currentVitality}, Agility={data.currentAgility}, " +
                  $"Incubation={data.currentIncubation}, AvailablePoints={data.availableAttributePoints}");
        
        // Force update the display
        UpdateDisplay();
        
        // Log the load
        Debug.Log($"AttributeDisplay updated with loaded game data. Available points: {data.availableAttributePoints}");
    }

    // Add this method to manually update the display with specific values
    public void UpdateDisplayWithValues(int strength, int vitality, int agility, int incubation, int recovery, int speed, int availablePoints)
    {
        Debug.Log($"Manually updating AttributeDisplay with: Strength={strength}, " +
                  $"Vitality={vitality}, Agility={agility}, " +
                  $"Incubation={incubation}, Recovery={recovery}, " +
                  $"Speed={speed}, AvailablePoints={availablePoints}");
        
        // Update available points
        if (availablePointsText != null)
            availablePointsText.text = $"Attribute Points: {availablePoints}";
            
        // Update strength
        if (strengthValueText != null)
            strengthValueText.text = $"{strength}/100";
            
        if (strengthBonusText != null && playerAttributes != null)
            strengthBonusText.text = $"+{(playerAttributes.StrengthMultiplier - 1) * 100:F0}% Damage";
            
        // Update vitality
        if (vitalityValueText != null)
            vitalityValueText.text = $"{vitality}/100";
            
        if (vitalityBonusText != null && playerAttributes != null)
            vitalityBonusText.text = $"+{(playerAttributes.VitalityMultiplier - 1) * 100:F0}% Health";
            
        // Update incubation
        if (incubationValueText != null)
            incubationValueText.text = $"{incubation}/20";
            
        if (incubationBonusText != null)
            incubationBonusText.text = $"+{incubation} Egg Capacity";
            
        // Update agility
        if (agilityValueText != null)
            agilityValueText.text = $"{agility}/100";
            
        if (agilityBonusText != null && playerAttributes != null)
            agilityBonusText.text = $"-{(playerAttributes.AgilityMultiplier - 1) * 100:F0}% Attack Cooldown";
            
        // Update recovery
        if (recoveryValueText != null)
            recoveryValueText.text = $"{recovery}/100";
            
        if (recoveryBonusText != null && playerAttributes != null)
            recoveryBonusText.text = $"-{(playerAttributes.RecoveryMultiplier - 1) * 100:F0}% Recovery Time";
            
        // Update speed
        if (speedValueText != null)
            speedValueText.text = $"{speed}/100";
            
        if (speedBonusText != null && playerAttributes != null)
            speedBonusText.text = $"+{(playerAttributes.SpeedMultiplier - 1) * 100:F0}% Movement Speed";
            
        // Update button interactability
        if (strengthButton != null)
            strengthButton.interactable = availablePoints > 0 && strength < 100;
            
        if (vitalityButton != null)
            vitalityButton.interactable = availablePoints > 0 && vitality < 100;
            
        if (incubationButton != null)
            incubationButton.interactable = availablePoints > 0 && incubation < 20;
            
        if (agilityButton != null)
            agilityButton.interactable = availablePoints > 0 && agility < 100;
            
        if (recoveryButton != null)
            recoveryButton.interactable = availablePoints > 0 && recovery < 100;
            
        if (speedButton != null)
            speedButton.interactable = availablePoints > 0 && speed < 100;
    }

    // Add this method overload to maintain backward compatibility
    public void UpdateDisplayWithValues(int strength, int vitality, int agility, int incubation, int recovery, int availablePoints)
    {
        // Call the new method with default speed value of 0
        UpdateDisplayWithValues(strength, vitality, agility, incubation, recovery, 0, availablePoints);
        
        Debug.Log("Using backward compatibility method for UpdateDisplayWithValues - speed set to 0");
    }

    public void OnAttributeButtonClicked()
    {
        Debug.Log("Attribute button clicked!");
        ShowPanel(true); // Use force=true to ensure it shows
    }

    public void NotifyCameraAnimationStarted()
    {
        isInCameraAnimation = true;
        Debug.Log("AttributeDisplay: Notified that camera animation has started");
        
        // Immediately hide the panel if it's showing
        if (attributesPanel != null && attributesPanel.activeSelf)
        {
            Debug.Log("AttributeDisplay: Hiding panel because camera animation started");
            attributesPanel.SetActive(false);
        }
    }

    public void NotifyCameraAnimationEnded()
    {
        isInCameraAnimation = false;
        Debug.Log("AttributeDisplay: Notified that camera animation has ended");
        
        // Show the panel if we have points to spend
        if (playerAttributes != null && playerAttributes.AvailablePoints > 0)
        {
            Debug.Log("AttributeDisplay: Showing panel after camera animation with delay");
            StartCoroutine(ShowPanelWithDelay(0.5f));
        }
    }

    private IEnumerator ShowPanelWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowPanel(true);
    }

    // Add this method to check if we should auto-close the panel
    private void CheckAutoClosePanel()
    {
        // If we have no more points to spend, close the panel with animation
        if (playerAttributes != null && playerAttributes.AvailablePoints <= 0)
        {
            Debug.Log("No more attribute points available - auto-closing panel");
            
            // Use the existing panel animation if available
            if (panelAnimator != null)
            {
                panelAnimator.SetBool("ShowUp", false);
                panelAnimator.SetBool("Hide", true);
                
                // Start a coroutine to disable the panel after the animation
                StartCoroutine(DisablePanelAfterAnimation(0.5f));
            }
            else
            {
                // If no animator, just hide the panel directly
                HidePanel();
            }
        }
    }

    // Add coroutine to disable panel after animation
    private IEnumerator DisablePanelAfterAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(false);
        }
    }

    private void RefreshDisplay()
    {
        UpdateDisplay();
    }
} 