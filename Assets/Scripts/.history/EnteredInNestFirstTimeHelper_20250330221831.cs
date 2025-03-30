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
        nestMarketPanel.GetComponent<Animator>().SetBool("ShowUp", false);
        nestMarketPanel.GetComponent<Animator>().SetBool("Hide", true);
    }

    public void OpenNestMarketPanel(){
        nestMarketPanel.GetComponent<Animator>().SetBool("ShowUp", true);
        nestMarketPanel.GetComponent<Animator>().SetBool("Hide", false);
    }
}
