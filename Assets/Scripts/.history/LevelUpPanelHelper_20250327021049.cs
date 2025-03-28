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

    public GameObject UpperPanel;
    public GameObject LowerPanelRewards;

    public PlayerAttributes playerAttributes;
    public PlayerInventory playerInventory;

    // Track instantiated rewards
    private GameObject attributePointReward;
    private GameObject chitinCapacityReward;
    private GameObject crumbCapacityReward;
    private int pendingRewards = 0;

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
        UpperPanel.transform.Find("NewLevel").GetComponent<TextMeshProUGUI>().text = playerInventory.CurrentLevel.ToString();

        GetComponent<Animator>().SetBool("ShowUp", true);
        GetComponent<Animator>().SetBool("Hide", false);

    }

    public void CloseUpperPanel()
    {
        GetComponent<Animator>().SetBool("ShowUp", false);
        GetComponent<Animator>().SetBool("Hide", true);
    }

    public void UpdateChitinRewardText(){
        chitinCapacityReward.transform.Find("RewardText").GetComponent<TextMeshProUGUI>().text = playerInventory.maxChitinCapacity.ToString();
    }

    public void UpdateCrumbRewardText(){
        crumbCapacityReward.transform.Find("RewardText").GetComponent<TextMeshProUGUI>().text = playerInventory.maxCrumbCapacity.ToString();
    }

    

    public void InstantiateRewards()
    {
        // Clear any existing rewards
        ClearExistingRewards();
        
        // Reset pending rewards counter
        pendingRewards = 3;
        
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
    }
    
    private void ClearExistingRewards()
    {
        // Remove any existing rewards
        if (attributePointReward != null) Destroy(attributePointReward);
        if (chitinCapacityReward != null) Destroy(chitinCapacityReward);
        if (crumbCapacityReward != null) Destroy(crumbCapacityReward);
    }
    
    private void DecrementPendingRewards()
    {
        pendingRewards--;
        if (pendingRewards <= 0)
        {
            // All rewards have been claimed, close the panel
            CloseUpperPanel();
        }
    }

    public void OnGetAttributePointsClaimButtonClicked()
    {
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
        playerInventory.maxChitinCapacity += 10;
        
        // Update UI using a public method instead of directly invoking the event
        if (playerInventory != null)
        {
            // This will update the UI without directly invoking the event
            playerInventory.ForceUpdateUIUpdateChitinAndCrumbCapacity();
        }
    }

    public void OnGetCrumbCapacityRewardButtonClicked()
    {
        playerInventory.maxCrumbCapacity += 1;
        
        // Update UI using a public method instead of directly invoking the event
        if (playerInventory != null)
        {
            // This will update the UI without directly invoking the event
            playerInventory.ForceUpdateUIUpdateChitinAndCrumbCapacity();
        }
    }
}
