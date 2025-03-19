using UnityEngine;
using UnityEngine.UI;

public class AttributesPanelButton : MonoBehaviour
{
    [SerializeField] private AttributeDisplay attributeDisplay;
    private Button button;

    private void Start()
    {
        // Find the AttributeDisplay if not assigned
        if (attributeDisplay == null)
            attributeDisplay = FindObjectOfType<AttributeDisplay>();
            
        // Get button component
        button = GetComponent<Button>();
        
        // Add listener to button click
        if (button != null && attributeDisplay != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }
    
    private void OnButtonClicked()
    {
        if (attributeDisplay != null)
        {
            attributeDisplay.ToggleAttributesPanel();
        }
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
} 