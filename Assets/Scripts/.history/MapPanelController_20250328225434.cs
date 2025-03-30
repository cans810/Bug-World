using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPanelController : MonoBehaviour
{
    public GameObject mapPanel;
    private UIHelper uiHelper;

    // Start is called before the first frame update
    void Start()
    {
        uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper == null)
        {
            Debug.LogError("UIHelper not found!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HideMapPanel()
    {
        if (uiHelper != null)
        {
            uiHelper.HideMapPanel();
        }
    }

    // Add this as a backup - it will be called when the panel animation finishes hiding
    public void OnMapPanelHidden()
    {
        // Deactivate the map panel if needed
        if (mapPanel != null)
        {
            StartCoroutine(DeactivateAfterDelay());
        }
    }
    
    // Wait a short time to ensure animations complete
    private IEnumerator DeactivateAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);
        mapPanel.SetActive(false);
    }
}
