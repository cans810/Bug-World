using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NestMarketController : MonoBehaviour
{
    [Header("Nest Panels")]
    public GameObject Nest2Panel;
    public GameObject Nest3Panel;
    
    [Header("Nest References")]
    [SerializeField] private GameObject playerNest2GameObject;
    [SerializeField] private GameObject playerNest3GameObject;
    
    [Header("Purchase State")]
    [SerializeField] private bool isNest2Purchased = false;
    [SerializeField] private bool isNest3Purchased = false;
    
    // UI References
    private Button nest2BuyButton;
    private Button nest3BuyButton;
    private TextMeshProUGUI nest2PriceText;
    private TextMeshProUGUI nest3PriceText;
    
    // Prices
    private int nest2Price = 500;
    private int nest3Price = 1000;
    
    // Player inventory reference
    private PlayerInventory playerInventory;

    private void Awake()
    {
        // Find Player Inventory
        playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogError("NestMarketController: PlayerInventory not found!");
        }
        
        // Find nest GameObjects if not set
        if (playerNest2GameObject == null)
        {
            playerNest2GameObject = GameObject.FindGameObjectWithTag("PlayerNest2");
        }
        
        if (playerNest3GameObject == null)
        {
            playerNest3GameObject = GameObject.FindGameObjectWithTag("PlayerNest3");
        }
        
        if (playerNest2GameObject == null || playerNest3GameObject == null)
        {
            Debug.LogWarning("NestMarketController: One or more nest GameObjects not found!");
        }
    }
    
    private void Start()
    {
        // Get UI elements
        if (Nest2Panel != null)
        {
            nest2BuyButton = Nest2Panel.GetComponentInChildren<Button>();
            nest2PriceText = Nest2Panel.GetComponentInChildren<TextMeshProUGUI>();
            
            if (nest2PriceText != null)
            {
                // Try to parse price from text
                string priceStr = nest2PriceText.text.Replace("Price: ", "").Replace(" coins", "");
                if (int.TryParse(priceStr, out int parsedPrice))
                {
                    nest2Price = parsedPrice;
                }
                Debug.Log($"Nest 2 price: {nest2Price} coins");
            }
            
            if (nest2BuyButton != null)
            {
                nest2BuyButton.onClick.AddListener(PurchaseNest2);
            }
        }
        
        if (Nest3Panel != null)
        {
            nest3BuyButton = Nest3Panel.GetComponentInChildren<Button>();
            nest3PriceText = Nest3Panel.GetComponentInChildren<TextMeshProUGUI>();
            
            if (nest3PriceText != null)
            {
                // Try to parse price from text
                string priceStr = nest3PriceText.text.Replace("Price: ", "").Replace(" coins", "");
                if (int.TryParse(priceStr, out int parsedPrice))
                {
                    nest3Price = parsedPrice;
                }
                Debug.Log($"Nest 3 price: {nest3Price} coins");
            }
            
            if (nest3BuyButton != null)
            {
                nest3BuyButton.onClick.AddListener(PurchaseNest3);
            }
        }
        
        // Set initial state of nests based on purchase status
        UpdateNestStatus();
    }
    
    public void PurchaseNest2()
    {
        if (isNest2Purchased)
        {
            Debug.Log("Nest 2 is already purchased.");
            return;
        }
        
        if (playerInventory != null && playerInventory.CoinCount >= nest2Price)
        {
            // Remove coins
            playerInventory.RemoveCoins(nest2Price);
            
            // Mark as purchased
            isNest2Purchased = true;
            
            // Update UI
            UpdateNestStatus();
            
            // Save purchase
            SaveNestPurchases();
            
            Debug.Log($"Purchased Nest 2 for {nest2Price} coins!");
        }
        else
        {
            Debug.Log($"Not enough coins to purchase Nest 2. Need {nest2Price} coins.");
            
            // Show UI message
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowInformText($"Not enough coins! Need {nest2Price} coins.");
            }
        }
    }
    
    public void PurchaseNest3()
    {
        if (isNest3Purchased)
        {
            Debug.Log("Nest 3 is already purchased.");
            return;
        }
        
        if (playerInventory != null && playerInventory.CoinCount >= nest3Price)
        {
            // Remove coins
            playerInventory.RemoveCoins(nest3Price);
            
            // Mark as purchased
            isNest3Purchased = true;
            
            // Update UI
            UpdateNestStatus();
            
            // Save purchase
            SaveNestPurchases();
            
            Debug.Log($"Purchased Nest 3 for {nest3Price} coins!");
        }
        else
        {
            Debug.Log($"Not enough coins to purchase Nest 3. Need {nest3Price} coins.");
            
            // Show UI message
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowInformText($"Not enough coins! Need {nest3Price} coins.");
            }
        }
    }
    
    private void UpdateNestStatus()
    {
        // Update Nest 2
        if (nest2BuyButton != null)
        {
            TextMeshProUGUI buttonText = nest2BuyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isNest2Purchased ? "Active" : "Buy";
            }
            
            // Disable button if purchased
            nest2BuyButton.interactable = !isNest2Purchased;
        }
        
        // Update Nest 3
        if (nest3BuyButton != null)
        {
            TextMeshProUGUI buttonText = nest3BuyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isNest3Purchased ? "Active" : "Buy";
            }
            
            // Disable button if purchased
            nest3BuyButton.interactable = !isNest3Purchased;
        }
        
        // Enable or disable nest functionality
        if (playerNest2GameObject != null)
        {
            EnableNestFunctionality(playerNest2GameObject, isNest2Purchased);
        }
        
        if (playerNest3GameObject != null)
        {
            EnableNestFunctionality(playerNest3GameObject, isNest3Purchased);
        }
    }
    
    private void EnableNestFunctionality(GameObject nestObject, bool enable)
    {
        // Enable or disable the base interaction component
        BaseInteraction baseInteraction = nestObject.GetComponent<BaseInteraction>();
        if (baseInteraction != null)
        {
            baseInteraction.enabled = enable;
            Debug.Log($"Set {nestObject.name} BaseInteraction to {enable}");
        }
        
        // Update visual to indicate active/inactive state
        SpriteRenderer circleImage = nestObject.GetComponentInChildren<SpriteRenderer>();
        if (circleImage != null)
        {
            // Set color to indicate status (darker if inactive)
            circleImage.color = enable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }
    
    public void LoadNestPurchases(GameData gameData)
    {
        if (gameData != null)
        {
            isNest2Purchased = gameData.isNest2Purchased;
            isNest3Purchased = gameData.isNest3Purchased;
            
            Debug.Log($"Loaded nest purchase status: Nest2={isNest2Purchased}, Nest3={isNest3Purchased}");
            
            // Update nest status based on loaded data
            UpdateNestStatus();
        }
    }
    
    public void SaveNestPurchases()
    {
        // Find the GameManager to save the data
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            // Update the GameData
            if (gameManager.gameData != null)
            {
                gameManager.gameData.isNest2Purchased = isNest2Purchased;
                gameManager.gameData.isNest3Purchased = isNest3Purchased;
                
                // Save the game
                gameManager.SaveGame();
                Debug.Log($"Saved nest purchase status: Nest2={isNest2Purchased}, Nest3={isNest3Purchased}");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
