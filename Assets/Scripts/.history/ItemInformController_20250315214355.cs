using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemInformController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
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
        // Ensure panel is hidden by default
        gameObject.SetActive(false);
        
        // Set up close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
    }
    
    public void ShowChitinInfo()
    {
        if (descriptionText != null)
            descriptionText.text = chitinDescription;
            
        if (itemImage != null && chitinSprite != null)
            itemImage.sprite = chitinSprite;
            
        gameObject.SetActive(true);
    }
    
    public void ShowCrumbInfo()
    {
        if (titleText != null)
            titleText.text = crumbTitle;
            
        if (descriptionText != null)
            descriptionText.text = crumbDescription;
            
        if (itemImage != null && crumbSprite != null)
            itemImage.sprite = crumbSprite;
            
        gameObject.SetActive(true);
    }
    
    public void HidePanel()
    {
        gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HidePanel);
        }
    }
}
