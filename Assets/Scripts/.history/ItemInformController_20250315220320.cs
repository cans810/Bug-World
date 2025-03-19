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
    [SerializeField] private string chitinDescription = "Chitin! Is used to ";
    [SerializeField] private Sprite chitinSprite;
    
    [Header("Crumb Info")]
    [SerializeField] private string crumbDescription = "Crumbs are a food resource that provides energy to your colony. Collect them from various locations in the world.";
    [SerializeField] private Sprite crumbSprite;
    
    // Add ResourceType enum and tracking field
    public enum ResourceType { None, Chitin, Crumb }
    private ResourceType currentResourceType = ResourceType.None;

    // Add reference to PlayerInventory
    private PlayerInventory playerInventory;

    private void Start()
    {
        // Find PlayerInventory
        playerInventory = FindObjectOfType<PlayerInventory>();

        // Ensure panel is hidden by default
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
    
    // Add method to set the resource type
    public void SetResourceType(ResourceType type)
    {
        currentResourceType = type;
    }

    // Update HidePanel to also set flags
    public void HidePanel()
    {
        // Hide the panel
        if (panel != null)
        {
            panel.SetActive(false);
        }
        
        // Update first-time flags based on which resource was shown
        if (playerInventory != null)
        {
            if (currentResourceType == ResourceType.Chitin)
            {
                Debug.Log("Marking chitin as collected after closing panel");
                playerInventory.HasYetToCollectChitin = false;
            }
            else if (currentResourceType == ResourceType.Crumb)
            {
                Debug.Log("Marking crumb as collected after closing panel");
                playerInventory.HasYetToCollectCrumb = false;
            }
        }
        else
        {
            Debug.LogWarning("PlayerInventory reference is null in ItemInformController!");
        }
        
        // Reset current resource type
        currentResourceType = ResourceType.None;
    }
    
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HidePanel);
        }
    }

    // Add this function for direct testing from a UI button
    public void TestShowChitinInfo()
    {
        Debug.Log("Test function called");
        ShowChitinInfo();
    }
}
