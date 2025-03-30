using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPanelController : MonoBehaviour
{
    public GameObject mapPanel;
    private UIHelper uiHelper;
    private BorderVisualizer[] allBorders;
    private bool isMapActive = false;

    // Start is called before the first frame update
    void Start()
    {
        uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper == null)
        {
            Debug.LogError("UIHelper not found!");
        }
        
        // Find all border visualizers in the scene
        allBorders = FindObjectsOfType<BorderVisualizer>();
        Debug.Log($"Found {allBorders.Length} border visualizers for map display");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HideMapPanel()
    {
        isMapActive = false;
        
        // Hide entity icons for all borders
        foreach (var border in allBorders)
        {
            if (border != null)
            {
                border.HideEntityIcons();
            }
        }
        
        if (uiHelper != null)
        {
            uiHelper.HideMapPanel();
        }
        
        Debug.Log("Map panel hidden, entity icons deactivated");
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

    // Add a method to show the map panel and update entity icons
    public void ShowMapPanel()
    {
        isMapActive = true;
        
        // Show entity icons for all locked borders
        foreach (var border in allBorders)
        {
            if (border != null)
            {
                border.ShowEntityIcons();
            }
        }
        
        // Log that we've shown the map
        Debug.Log("Map panel shown, entity icons activated for locked areas");
    }

    // Add an OnEnable method to handle map re-activations
    private void OnEnable()
    {
        if (isMapActive)
        {
            // If map is being re-enabled, show entity icons
            foreach (var border in allBorders)
            {
                if (border != null)
                {
                    border.ShowEntityIcons();
                }
            }
        }
    }
}
