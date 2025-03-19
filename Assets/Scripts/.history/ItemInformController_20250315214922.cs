using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemInformController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject panel; // The inner Panel containing all UI elements
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image itemImage;
    [SerializeField] private Button closeButton;
    
    [Header("Chitin Info")]
    [SerializeField] private string chitinDescription = "Chitin is a valuable resource that can be used to build your nest and upgrade your abilities. Collect it from defeating enemies.";
    [SerializeField] private Sprite chitinSprite;
    
    [Header("Crumb Info")]
    [SerializeField] private string crumbDescription = "Crumbs are a food resource that provides energy to your colony. Collect them from various locations in the world.";
    [SerializeField] private Sprite crumbSprite;
    
    private void Start()
    {
        // Ensure panel is hidden by default (not the parent object)
        if (panel != null)
        {
            panel.SetActive(false);
        }
        
        // Set up close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
    }
    
    public void ShowChitinInfo()
    {
        Debug.Log("ShowChitinInfo called on ItemInformController");
        
        if (descriptionText != null)
        {
            descriptionText.text = chitinDescription;
            Debug.Log($"Set description: {chitinDescription}");
        }
        else
        {
            Debug.LogWarning("descriptionText is null!");
        }
        
        if (itemImage != null && chitinSprite != null)
        {
            itemImage.sprite = chitinSprite;
            Debug.Log("Set chitin sprite");
        }
        else
        {
            Debug.LogWarning($"Cannot set sprite. itemImage: {itemImage != null}, chitinSprite: {chitinSprite != null}");
        }
        
        // Show panel instead of activating the entire GameObject
        if (panel != null)
        {
            panel.SetActive(true);
            Debug.Log("Activated panel");
        }
        else
        {
            Debug.LogWarning("panel reference is null!");
        }
    }
    
    public void ShowCrumbInfo()
    {
        if (descriptionText != null)
            descriptionText.text = crumbDescription;
            
        if (itemImage != null && crumbSprite != null)
            itemImage.sprite = crumbSprite;
            
        // Show panel instead of activating the entire GameObject
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }
    
    public void HidePanel()
    {
        // Hide the panel instead of deactivating the entire GameObject
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
    
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HidePanel);
        }
    }
}
