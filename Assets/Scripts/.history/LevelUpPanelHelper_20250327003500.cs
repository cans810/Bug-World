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

    public void InstantiateRewards()
    {
        
    }


}
