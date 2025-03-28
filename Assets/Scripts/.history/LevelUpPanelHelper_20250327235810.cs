using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For Button component
using TMPro;

public class LevelUpPanelHelper : MonoBehaviour
{
    public GameObject attributePointRewardPrefab;
    public GameObject chitinCapacityRewardPrefab;
    public GameObject crumbCapacityRewardPrefab;
    public GameObject newAreaUnlockedRewardPrefab;

    public GameObject UpperPanel;
    public GameObject LowerPanelRewards;

    public PlayerAttributes playerAttributes;
    public PlayerInventory playerInventory;

    // Track instantiated rewards
    private GameObject attributePointReward;
    private GameObject chitinCapacityReward;
    private GameObject crumbCapacityReward;
    private GameObject newAreaUnlockedReward;
    private int pendingRewards = 0;
    
    // Store new area data for when the claim button is clicked
    private GameObject areaTarget;
    private int areaLevel;
    private string areaName;

    // Cache camera animations reference
    private CameraAnimations cameraAnimations;
    
    // Add this field to cache the player controller reference
    private PlayerController playerController;

    private void Awake()
    {
        // Find required references
        if (playerAttributes == null)
            playerAttributes = FindObjectOfType<PlayerAttributes>();
            
        if (playerInventory == null)
            playerInventory = FindObjectOfType<PlayerInventory>();
            
        // Cache the player controller reference
        playerController = FindObjectOfType<PlayerController>();

        cameraAnimations = FindObjectOfType<CameraAnimations>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowUpperPanel()
    {
        // Disable player controls when showing rewards
        if (playerController != null)
        {
            playerController.SetControlsEnabled(false);
            Debug.Log("Disabled player controls for level up rewards");
        }

        UpperPanel.transform.Find("NewLevel").GetComponent<TextMeshProUGUI>().text = playerInventory.CurrentLevel.ToString();

        GetComponent<Animator>().SetBool("ShowUp", true);
        GetComponent<Animator>().SetBool("Hide", false);
    }

    public void CloseUpperPanel()
    {
        GetComponent<Animator>().SetBool("ShowUp", false);
        GetComponent<Animator>().SetBool("Hide", true);
        
        // Re-enable player controls if panel is manually closed
        // This is a safety measure in case the panel is closed without claiming all rewards
        if (pendingRewards > 0 && playerController != null)
        {
            playerController.SetControlsEnabled(true);
            Debug.Log("Re-enabled player controls after manual panel close");
        }
    }

    public void UpdateChitinRewardText(){
        chitinCapacityReward.transform.Find("RewardText").GetComponent<TextMeshProUGUI>().text = playerInventory.maxChitinCapacity.ToString();
    }

    public void UpdateCrumbRewardText(){
        crumbCapacityReward.transform.Find("RewardText").GetComponent<TextMeshProUGUI>().text = playerInventory.maxCrumbCapacity.ToString();
    }

    public void UpdateAttributeRewardText(){
        attributePointReward.transform.Find("RewardText").GetComponent<TextMeshProUGUI>().text = playerAttributes.availablePoints.ToString();
    }

    public void InstantiateRewards()
    {
        // Clear any existing rewards
        ClearExistingRewards();
        
        // Reset pending rewards counter
        pendingRewards = 3; // Start with the base 3 rewards
        
        // Instantiate attribute point reward
        attributePointReward = Instantiate(attributePointRewardPrefab, LowerPanelRewards.transform);
        Button attributeButton = attributePointReward.transform.Find("ClaimButton").GetComponent<Button>();
        attributeButton.onClick.AddListener(() => {
            OnGetAttributePointsClaimButtonClicked();
            Destroy(attributePointReward);
            DecrementPendingRewards();
        });
        
        // Instantiate chitin capacity reward
        chitinCapacityReward = Instantiate(chitinCapacityRewardPrefab, LowerPanelRewards.transform);
        Button chitinButton = chitinCapacityReward.transform.Find("ClaimButton").GetComponent<Button>();
        chitinButton.onClick.AddListener(() => {
            OnGetChitinCapacityRewardButtonClicked();
            Destroy(chitinCapacityReward);
            DecrementPendingRewards();
        });
        
        // Instantiate crumb capacity reward
        crumbCapacityReward = Instantiate(crumbCapacityRewardPrefab, LowerPanelRewards.transform);
        Button crumbButton = crumbCapacityReward.transform.Find("ClaimButton").GetComponent<Button>();
        crumbButton.onClick.AddListener(() => {
            OnGetCrumbCapacityRewardButtonClicked();
            Destroy(crumbCapacityReward);
            DecrementPendingRewards();
        });
        
        // Check if there's a new area at this level
        CheckForNewAreaUnlock(playerInventory.CurrentLevel);
    }
    
    private void ClearExistingRewards()
    {
        // Remove any existing rewards
        if (attributePointReward != null) Destroy(attributePointReward);
        if (chitinCapacityReward != null) Destroy(chitinCapacityReward);
        if (crumbCapacityReward != null) Destroy(crumbCapacityReward);
        if (newAreaUnlockedReward != null) Destroy(newAreaUnlockedReward);
    }
    
    private void DecrementPendingRewards()
    {
        pendingRewards--;
        if (pendingRewards <= 0)
        {
            // All rewards have been claimed, close the panel
            CloseUpperPanel();
            
            // Re-enable player controls when all rewards are claimed
            if (playerController != null)
            {
                playerController.SetControlsEnabled(true);
                Debug.Log("Re-enabled player controls after all rewards claimed");
            }
        }
    }
    
    // Add method to check for new area unlocked at current level
    private void CheckForNewAreaUnlock(int level)
    {
        LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
        if (arrowManager == null) return;
        
        // Use reflection to access the private levelAreas field
        var areasField = typeof(LevelAreaArrowManager).GetField("levelAreas", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
            
        if (areasField == null) return;
        
        var areas = areasField.GetValue(arrowManager) as LevelAreaArrowManager.LevelAreaTarget[];
        if (areas == null) return;
        
        // Check if any area requires this level and hasn't been visited
        foreach (var area in areas)
        {
            if (area.requiredLevel == level && !area.hasBeenVisited && area.areaTarget != null)
            {
                Debug.Log($"Found new area to unlock at level {level}: {area.areaName}");
                
                // Store the area data for when claim button is clicked
                areaTarget = area.areaTarget;
                areaLevel = area.requiredLevel;
                areaName = area.areaName;
                
                // Add new area unlocked reward to the panel
                AddNewAreaUnlockedReward(area.areaName);
                break;
            }
        }
    }
    
    // Add method to create the new area unlocked reward
    private void AddNewAreaUnlockedReward(string areaName)
    {
        if (newAreaUnlockedRewardPrefab == null)
        {
            Debug.LogError("New Area Unlocked reward prefab is not assigned!");
            return;
        }
        
        // Instantiate the new area unlocked reward
        newAreaUnlockedReward = Instantiate(newAreaUnlockedRewardPrefab, LowerPanelRewards.transform);
        
        // Set the area name in the UI
        TextMeshProUGUI areaText = newAreaUnlockedReward.transform.Find("AreaNameText")?.GetComponent<TextMeshProUGUI>();
        if (areaText != null)
        {
            areaText.text = areaName;
        }
        
        // Add click listener to the claim button
        Button claimButton = newAreaUnlockedReward.transform.Find("ClaimButton").GetComponent<Button>();
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(() => {
                OnNewAreaUnlockedClaimButtonClicked();
                Destroy(newAreaUnlockedReward);
                DecrementPendingRewards();
            });
        }
        
        // Increment the pending rewards counter
        pendingRewards++;
    }
    
    // Add method to handle the new area unlocked claim button click
    public void OnNewAreaUnlockedClaimButtonClicked()
    {
        
        // Close the level up panel
        CloseUpperPanel();
        
        // Trigger the area unlocked animation after a short delay
        StartCoroutine(ShowNewAreaAfterDelay(0.5f));
    }
    
    // Coroutine to show the new area after a short delay
    private IEnumerator ShowNewAreaAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Make sure we have valid area data and camera animations
        if (areaTarget != null && cameraAnimations != null)
        {
            Debug.Log($"Showing animation for new area: {areaName}");
            
            // First remove border for this area
            RemoveBorderForArea(areaTarget, areaLevel);
            
            // Then trigger the camera animation to show the area
            cameraAnimations.AnimateToArea(areaTarget.transform, areaLevel, areaName);
            
            // Mark the area as visited in the arrow manager
            LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
            if (arrowManager != null)
            {
                // Use reflection to call the MarkAreaAsVisited method
                var markMethod = typeof(LevelAreaArrowManager).GetMethod("MarkAreaAsVisited", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                    
                if (markMethod != null)
                {
                    markMethod.Invoke(arrowManager, new object[] { areaTarget });
                }
            }
        }
        else
        {
            Debug.LogWarning("Cannot show new area: missing target or camera animations component");
        }
    }
    
    // Helper method to remove border for an area
    private void RemoveBorderForArea(GameObject areaTarget, int level)
    {
        // Find all border visualizers in the scene
        BorderVisualizer[] allBorders = FindObjectsOfType<BorderVisualizer>();
        
        // Pattern to match area names with border names
        string areaName = areaTarget.name;
        string areaNumber = System.Text.RegularExpressions.Regex.Match(areaName, @"\d+").Value;
        
        Debug.Log($"Looking for borders matching area {areaName} (number: {areaNumber})");
        
        foreach (BorderVisualizer border in allBorders)
        {
            string borderName = border.gameObject.name;
            bool isMatch = false;
            
            // Pattern 1: Direct number match (Area2 → MapBorder2)
            if (!string.IsNullOrEmpty(areaNumber))
            {
                string borderNumber = System.Text.RegularExpressions.Regex.Match(borderName, @"\d+").Value;
                if (borderNumber == areaNumber)
                {
                    isMatch = true;
                    Debug.Log($"Found matching border by number: {borderName}");
                }
            }
            
            // Pattern 2: Check if border's required level matches this level
            if (border.GetRequiredLevel() == level)
            {
                isMatch = true;
                Debug.Log($"Found matching border by level: {borderName} (level {level})");
            }
            
            // If we have a match, actually disable the border visualization
            if (isMatch)
            {
                Debug.Log($"<color=green>Disabling border visualization for {borderName}</color>");
                
                // Method 1: Use DisableBorderVisualization method if it exists
                var disableMethod = typeof(BorderVisualizer).GetMethod("DisableBorderVisualization", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                if (disableMethod != null)
                {
                    disableMethod.Invoke(border, null);
                }
                else
                {
                    // Method 2: Disable game object and set hasBeenDisabled flag
                    // This is a fallback method if DisableBorderVisualization doesn't exist
                    var collider = border.GetComponent<Collider>();
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }
                    
                    // Try to find and disable line renderer (perimeter visualization)
                    var lineRenderer = border.GetComponentInChildren<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        lineRenderer.enabled = false;
                    }
                    
                    // Set the hasBeenDisabled field via reflection
                    var hasBeenDisabledField = typeof(BorderVisualizer).GetField("hasBeenDisabled",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        
                    if (hasBeenDisabledField != null)
                    {
                        hasBeenDisabledField.SetValue(border, true);
                    }
                    
                    // Optionally disable the entire game object if needed
                    // border.gameObject.SetActive(false);
                }
                
                Debug.Log($"<color=green>Border {borderName} disabled!</color>");
            }
        }
    }

    public void OnGetAttributePointsClaimButtonClicked()
    {
        SoundEffectManager.Instance.PlaySound("Pickup3");

        playerAttributes.availablePoints += 2;
        
        // Update UI for attribute points
        if (playerAttributes != null)
        {
            // Force update the attributes panel if it exists
            AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
            if (attributeDisplay != null)
            {
                attributeDisplay.UpdateDisplay();
            }
        }
    }

    public void OnGetChitinCapacityRewardButtonClicked()
    {
        SoundEffectManager.Instance.PlaySound("Pickup3");
        playerInventory.maxChitinCapacity += 10;
        
        // Update UI using a public method instead of directly invoking the event
        if (playerInventory != null)
        {
            // This will update the UI without directly invoking the event
            playerInventory.ForceUpdateUIUpdateChitinCapacity();
        }
    }

    public void OnGetCrumbCapacityRewardButtonClicked()
    {
        SoundEffectManager.Instance.PlaySound("Pickup3");

        playerInventory.maxCrumbCapacity += 1;
        
        // Update UI using a public method instead of directly invoking the event
        if (playerInventory != null)
        {
            // This will update the UI without directly invoking the event
            playerInventory.ForceUpdateUIUpdateCrumbCapacity();
        }
    }
}
