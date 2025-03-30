using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;
using TMPro;

public class MissionController : MonoBehaviour
{
    [Header("Mission Settings")]
    [SerializeField] private string missionID;
    [SerializeField] private string missionTitle = "Collect Chitin";
    [SerializeField] private string missionDescription = "Collect 10 chitin fragments";
    [SerializeField] private int targetValue = 10;
    [SerializeField] private int currentValue = 0;
    
    [Header("Reward Settings")]
    [SerializeField] private int experienceReward = 50;
    [SerializeField] private int coinReward = 10;
    [SerializeField] private bool isRewardClaimed = false;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI missionTitleText;
    [SerializeField] private TextMeshProUGUI missionDescriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Transform progressBarParent; // Parent object containing the fill image
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject completionEffect;
    
    // Direct reference to the fill image
    private Image fillImage;
    
    [Header("Events")]
    public UnityEvent OnMissionCompleted;
    public UnityEvent OnRewardClaimed;
    
    private bool isMissionCompleted = false;
    
    private void Awake()
    {
        // Find the fill image in children if not assigned
        if (progressBarParent != null)
        {
            Transform fillTransform = progressBarParent.Find("fill");
            if (fillTransform != null)
            {
                fillImage = fillTransform.GetComponent<Image>();
                if (fillImage == null)
                {
                    Debug.LogError($"Mission {missionID}: 'fill' object found but has no Image component!");
                }
            }
            else
            {
                Debug.LogError($"Mission {missionID}: Could not find 'fill' child object in progress bar!");
            }
        }
        else
        {
            // Try to find it directly as a child of this object
            Transform fillTransform = transform.Find("fill");
            if (fillTransform != null)
            {
                fillImage = fillTransform.GetComponent<Image>();
            }
            
            if (fillImage == null)
            {
                Debug.LogError($"Mission {missionID}: No progress bar parent or fill image found!");
            }
        }
    }
    
    private void Start()
    {
        // Initialize UI
        UpdateUI();
        
        // Set button state
        UpdateButtonState();
        
        // Add click listener to claim button
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(ClaimReward);
        }
        
        // Debug log to confirm fill image setup
        if (fillImage != null)
        {
            Debug.Log($"Mission {missionID}: Fill image setup successfully. Initial fill: {fillImage.fillAmount}");
        }
    }
    
    // Method to update progress
    public void UpdateProgress(int newValue)
    {
        int oldValue = currentValue;
        // Set current value, clamped to target
        currentValue = Mathf.Clamp(newValue, 0, targetValue);
        
        // Log progress update
        if (oldValue != currentValue)
        {
            Debug.Log($"Mission '{missionTitle}' progress updated: {oldValue} -> {currentValue}/{targetValue}");
        }
        
        // Update progress fill
        UpdateUI();
        
        // Check if mission is now completed
        CheckCompletion();
    }
    
    // Method to increment progress
    public void IncrementProgress(int amount = 1)
    {
        UpdateProgress(currentValue + amount);
    }
    
    // Method to check if mission is completed
    private void CheckCompletion()
    {
        if (currentValue >= targetValue && !isMissionCompleted)
        {
            isMissionCompleted = true;
            
            // Show completion effect if available
            if (completionEffect != null)
            {
                completionEffect.SetActive(true);
                StartCoroutine(HideCompletionEffect(3f));
            }
            
            // Trigger completion event
            OnMissionCompleted?.Invoke();
            
            // Enable claim button
            UpdateButtonState();
            
            Debug.Log($"Mission '{missionTitle}' completed!");
        }
    }
    
    // Method to claim reward (attach this to the button's onClick event)
    public void ClaimReward()
    {
        if (!isMissionCompleted || isRewardClaimed)
            return;
        
        // Play claim sound
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("Reward");
        }
        
        // Give rewards to player
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            // Add experience
            if (experienceReward > 0)
            {
                playerInventory.AddXP(experienceReward);
                Debug.Log($"Rewarded {experienceReward} XP for mission completion");
            }
            
            // Add coins
            if (coinReward > 0)
            {
                playerInventory.AddCoins(coinReward);
                Debug.Log($"Rewarded {coinReward} coins for mission completion");
            }
        }
        else
        {
            Debug.LogWarning("PlayerInventory not found! Cannot give rewards.");
        }
        
        // Mark reward as claimed
        isRewardClaimed = true;
        
        // Update button state
        UpdateButtonState();
        
        // Trigger reward claimed event
        OnRewardClaimed?.Invoke();
        
        // Show reward popup
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            uiHelper.ShowInformText($"Mission Complete! Rewarded {experienceReward} XP and {coinReward} coins");
        }
    }
    
    // Update UI elements
    private void UpdateUI()
    {
        // Update mission title
        if (missionTitleText != null)
        {
            missionTitleText.text = missionTitle;
        }
        
        // Update mission description
        if (missionDescriptionText != null)
        {
            missionDescriptionText.text = missionDescription;
        }
        
        // Update progress text
        if (progressText != null)
        {
            progressText.text = $"{currentValue}/{targetValue}";
        }
        
        // Update fill image - IMPORTANT PART
        if (fillImage != null)
        {
            float fillAmount = (float)currentValue / targetValue;
            fillImage.fillAmount = fillAmount;
            Debug.Log($"Updated fill image to {fillAmount:F2} ({currentValue}/{targetValue})");
            
            // Make sure the fill Image is set to filled mode
            if (fillImage.type != Image.Type.Filled)
            {
                Debug.LogWarning($"Mission {missionID}: Fill image is not set to Filled type! Current type: {fillImage.type}");
                fillImage.type = Image.Type.Filled;
            }
        }
        else
        {
            Debug.LogWarning($"Mission {missionID}: Fill image is null, cannot update progress bar!");
            
            // Try to find it again in case it wasn't available during Awake
            Transform fillTransform = transform.Find("fill");
            if (fillTransform != null)
            {
                fillImage = fillTransform.GetComponent<Image>();
                if (fillImage != null)
                {
                    float fillAmount = (float)currentValue / targetValue;
                    fillImage.fillAmount = fillAmount;
                    Debug.Log($"Found and updated fill image to {fillAmount:F2}");
                }
            }
        }
    }
    
    // Update button state based on mission completion and reward status
    private void UpdateButtonState()
    {
        if (claimButton != null)
        {
            // Enable button only if mission is completed and reward not claimed
            claimButton.interactable = isMissionCompleted && !isRewardClaimed;
            
            // Update button text
            TextMeshProUGUI buttonText = claimButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isRewardClaimed ? "Claimed" : "Claim Reward";
            }
            else
            {
                // Fallback to regular Text component
                Text legacyButtonText = claimButton.GetComponentInChildren<Text>();
                if (legacyButtonText != null)
                {
                    legacyButtonText.text = isRewardClaimed ? "Claimed" : "Claim Reward";
                }
            }
        }
    }
    
    // Hide completion effect after delay
    private IEnumerator HideCompletionEffect(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (completionEffect != null)
        {
            completionEffect.SetActive(false);
        }
    }
    
    // Getter for mission completion state
    public bool IsMissionCompleted()
    {
        return isMissionCompleted;
    }
    
    // Getter for reward claimed state
    public bool IsRewardClaimed()
    {
        return isRewardClaimed;
    }
    
    // Get mission progress percentage (0-1)
    public float GetProgressPercentage()
    {
        return (float)currentValue / targetValue;
    }
} 