using UnityEngine;
using UnityEngine.UI;

public class AttributeButtonHandler : MonoBehaviour
{
    [SerializeField] private GameObject attributesPanel;
    
    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            Debug.Log("AttributeButtonHandler: Initialized button handler");
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError("AttributeButtonHandler: No Button component found!");
        }
    }
    
    public void OnButtonClicked()
    {
        Debug.Log("AttributeButtonHandler: Button clicked!");
        
        // Direct panel access approach
        if (attributesPanel != null)
        {
            attributesPanel.SetActive(true);
            Debug.Log("AttributeButtonHandler: Directly activated attributes panel");
            
            // Optional: Try to find and trigger animator
            Animator animator = attributesPanel.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("ShowUp", true);
                animator.SetBool("Hide", false);
            }
            
            return;
        }
        
        // Fallback to finding attribute display
        AttributeDisplay display = FindObjectOfType<AttributeDisplay>();
        if (display != null)
        {
            display.ShowPanelFromPlayerAction(true);
            Debug.Log("AttributeButtonHandler: Called ShowPanelFromPlayerAction");
        }
        else
        {
            Debug.LogError("AttributeButtonHandler: Failed to find AttributeDisplay!");
        }
    }
} 