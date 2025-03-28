using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelUpPanelHelper : MonoBehaviour
{
    public GameObject attributePointRewardPrefab;
    public GameObject chitinCapacityRewardPrefab;
    public GameObject crumbCapacityRewardPrefab;

    public GameObject UpperPanel;
    public GameObject LowerPanelRewards;

    public PlayerAttributes playerAttributes;
    public PlayerInventory playerInventory;


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
        GetComponent<Animator>().SetBool("ShowUp", true);
    }

    public void ShowUpperPanel()
    {
        GetComponent<Animator>().SetBool("ShowUp", true);
    }

    public void InstantiateRewards()
    {

    }

    public void OnGetAttributePointsClaimButtonClicked()
    {
        playerAttributes.availablePoints += 2;
    }

    public void OnGetChitinCapacityRewardButtonClicked()
    {
        playerInventory.maxChitinCapacity += 5;
    }

    public void OnGetCrumbCapacityRewardButtonClicked()
    {
        playerInventory.maxCrumbCapacity += 1;
    }


}
