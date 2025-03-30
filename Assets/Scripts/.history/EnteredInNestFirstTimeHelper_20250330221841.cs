using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnteredInNestFirstTimeHelper : MonoBehaviour
{
    public GameObject nestMarketPanel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CloseNestMarketPanel(){
        nestMarketPanel.SetActive(false);
    }

    public void OpenNestMarketPanel(){
        nestMarketPanel.SetActive(true);
    }
}
