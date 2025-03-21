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

    [SerializeField] private Animator animator;

    
    [Header("Chitin Info")]
    private string chitinDescription = "Chitin! Is used to gain xp, level up, and upgrade your attributes. It can be collected from defeated enemies.";
    [SerializeField] private Sprite chitinSprite;
    
    [Header("Crumb Info")]
    private string crumbDescription = "Crumbs! Collect them from various locations in the world, and use them to get allies to join you.";
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
            animator.SetBool("ShowUp", true);
            Debug.Log("Activated panel");
        }
        else
        {
            Debug.LogWarning("panel reference is null!");
        }
    }
    
    public void ShowCrumbInfo()
    {
        Debug.Log("ShowCrumbInfo called on ItemInformController");
        
        if (descriptionText != null)
        {
            descriptionText.text = crumbDescription;
            Debug.Log($"Set description: {crumbDescription}");
        }
        else
        {
            Debug.LogWarning("descriptionText is null!");
        }
        
        if (itemImage != null && crumbSprite != null)
        {
            itemImage.sprite = crumbSprite;
            Debug.Log("Set crumb sprite");
        }
        else
        {
            Debug.LogWarning($"Cannot set sprite. itemImage: {itemImage != null}, crumbSprite: {crumbSprite != null}");
        }
        
        // Show panel instead of activating the entire GameObject
        if (panel != null)
        {
            panel.SetActive(true);
            animator.SetBool("ShowUp", true);
            animator.SetBool("Hide", false);
            Debug.Log("Activated crumb info panel");
        }
        else
        {
            Debug.LogWarning("panel reference is null!");
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
            animator.SetBool("ShowUp", false);
            animator.SetBool("Hide", true);
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

    public void HidePanelEvent(){
        panel.SetActive(false);
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
