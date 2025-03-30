using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using UnityEngine.SceneManagement;

public class MarketManager : MonoBehaviour, IStoreListener
{
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;

    public PlayerInventory playerInventory;

    public TextMeshProUGUI coinsText;

    public GameObject MarketPanel;

    public TextMeshProUGUI DoubleThreatPriceText;
    public TextMeshProUGUI DoubleThreatCurrencyText;

    public TextMeshProUGUI Point250PriceText;
    public TextMeshProUGUI Point250CurrencyText;

    public TextMeshProUGUI Point500PriceText;
    public TextMeshProUGUI Point500CurrencyText;

    public TextMeshProUGUI Point750PriceText;
    public TextMeshProUGUI Point750CurrencyText;

    public TextMeshProUGUI Point1000PriceText;
    public TextMeshProUGUI Point1000CurrencyText;

    // Example point packages
    public readonly Dictionary<string, int> coinPackages = new Dictionary<string, int>
    {
        { "coin250", 250 },
        { "coin500", 500 },
        { "coin750", 750 },
        { "coin1000", 1000 },
        { "doubleThreat", 0 }, // Special product, doesn't give coins directly
    };

    // Add this field to track initialization status
    public bool isInitialized = false;
    
    // Dictionary mapping product IDs to their respective UI text components
    private Dictionary<string, PriceTextPair> productTextMapping;
    
    // Define a struct to hold price and currency text components
    private struct PriceTextPair
    {
        public TextMeshProUGUI PriceText;
        public TextMeshProUGUI CurrencyText;

        public PriceTextPair(TextMeshProUGUI priceText, TextMeshProUGUI currencyText)
        {
            PriceText = priceText;
            CurrencyText = currencyText;
        }
    }
    
    public MarketManager()
    {
        // Initialize the dictionary in the constructor
        productTextMapping = new Dictionary<string, PriceTextPair>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        InitializePurchasing();
    }
    
    private void SetupProductTextMapping()
    {
        // Map each product ID to its corresponding UI text elements
        productTextMapping.Clear();
        productTextMapping.Add("coin250", new PriceTextPair(Point250PriceText, Point250CurrencyText));
        productTextMapping.Add("coin500", new PriceTextPair(Point500PriceText, Point500CurrencyText));
        productTextMapping.Add("coin750", new PriceTextPair(Point750PriceText, Point750CurrencyText));
        productTextMapping.Add("coin1000", new PriceTextPair(Point1000PriceText, Point1000CurrencyText));
        productTextMapping.Add("doubleThreat", new PriceTextPair(DoubleThreatPriceText, DoubleThreatCurrencyText));
    }

    public void InitializePurchasing()
    {
        // If already initialized, return
        if (isInitialized) 
        {
            UpdatePriceDisplay();
            return;
        }
        
        // Setup the mapping of products to text components
        SetupProductTextMapping();

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        
        // Add products
        builder.AddProduct("coin250", ProductType.Consumable);
        builder.AddProduct("coin500", ProductType.Consumable);
        builder.AddProduct("coin750", ProductType.Consumable);
        builder.AddProduct("coin1000", ProductType.Consumable);
        builder.AddProduct("doubleThreat", ProductType.Consumable); // Add Double Threat as a consumable product

        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
        isInitialized = true; // Set initialized flag
        
        Debug.Log("IAP Initialization successful!");
        
        // Update the UI with localized pricing
        UpdatePriceDisplay();
    }
    
    private void UpdatePriceDisplay()
    {
        if (storeController == null)
        {
            Debug.LogWarning("Cannot update price display: store controller is not initialized");
            return;
        }

        foreach (var productId in coinPackages.Keys)
        {
            Product product = storeController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                if (productTextMapping.TryGetValue(productId, out PriceTextPair textPair))
                {
                    // Extract price and currency information
                    decimal price = product.metadata.localizedPrice;
                    string currencyCode = product.metadata.isoCurrencyCode;
                    
                    // Set the price and currency texts
                    if (textPair.PriceText != null)
                    {
                        textPair.PriceText.text = price.ToString("F2");
                    }
                    
                    if (textPair.CurrencyText != null)
                    {
                        textPair.CurrencyText.text = currencyCode;
                    }
                    
                    Debug.Log($"Updated price for {productId}: {price} {currencyCode}");
                }
            }
            else
            {
                Debug.LogWarning($"Product {productId} is not available for purchase or not found");
            }
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"IAP Initialization failed: {error}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"IAP Initialization failed: {error}. Message: {message}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        Debug.Log($"Processing purchase: {productId}");

        if (productId == "doubleThreat")
        {
            // Process Double Threat purchase
            Debug.Log("Processing Double Threat purchase");
            
            // Call our method to handle the purchase
            OnDoubleThreatPurchase();
        }
        else if (coinPackages.ContainsKey(productId))
        {
            Debug.Log($"Processing coins purchase for {productId}");
            int coinsToAdd = coinPackages[productId];
            
            // Get PlayerInventory and add coins
            if (playerInventory != null)
            {
                bool success = playerInventory.AddCoins(coinsToAdd);
                if (success)
                {
                    Debug.Log($"Coins purchase completed. New total: {playerInventory.CoinCount}");
                    UpdateCoinsDisplay(); // Update the UI immediately
                    
                    // Play purchase complete sound
                    if (SoundEffectManager.Instance != null)
                    {
                        SoundEffectManager.Instance.PlaySound("PurchaseComplete");
                        Debug.Log("Playing purchase complete sound");
                    }
                }
                else
                {
                    Debug.LogError("Failed to add coins to player inventory");
                }
            }
            else
            {
                Debug.LogError("PlayerInventory reference is null!");
            }
        }
        else
        {
            Debug.LogWarning($"Unknown product ID: {productId}");
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"Purchase of {product.definition.id} failed due to {failureReason}");
    }

    public void AddCoins(int coins)
    {
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory reference is null!");
            return;
        }

        Debug.Log($"=== Adding Coins from Market ===");
        Debug.Log($"Current coins before adding: {playerInventory.CoinCount}");
        Debug.Log($"Coins to add: {coins}");

        bool success = playerInventory.AddCoins(coins);
        
        if (success)
        {
            Debug.Log($"New total coins: {playerInventory.CoinCount}");
            UpdateCoinsDisplay();
            
            // Play purchase complete sound for test purchases too
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound("PurchaseComplete");
                Debug.Log("Playing purchase complete sound for test purchase");
            }
        }
        else
        {
            Debug.LogError("Failed to add coins to player inventory");
        }
    }

    public void UpdateCoinsDisplay()
    {
        if (coinsText != null)
        {
            coinsText.text = playerInventory.CoinCount.ToString();
        }
    }

    // Method to initiate purchase
    public void BuyProduct(string productId)
    {
        if (!isInitialized)
        {
            Debug.LogError("Store not initialized. Please wait for initialization to complete.");
            return;
        }

        if (storeController != null && storeController.products.WithID(productId) != null)
        {
            storeController.InitiatePurchase(productId);
            Debug.Log($"Initiating purchase for {productId}");
        }
        else
        {
            Debug.LogError($"Failed to purchase {productId}: Store controller not initialized or product not found");
        }
    }

    public void HideMarketPanel(){
        MarketPanel.SetActive(false);
    }

    public void ShowMarketPanel(){
        MarketPanel.SetActive(true);
        UpdateCoinsDisplay();
        
        // Refresh prices if we're initialized
        if (isInitialized && storeController != null)
        {
            UpdatePriceDisplay();
        }
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }

    // Add this new method to handle the Double Threat pack purchase
    public void OnDoubleThreatPurchase()
    {
        // Check if player inventory exists
        if (playerInventory == null)
        {
            Debug.LogError("Cannot purchase Double Threat: PlayerInventory reference is null!");
            return;
        }

        // Get insect incubator reference
        InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
        if (incubator == null)
        {
            Debug.LogError("Cannot purchase Double Threat: InsectIncubator not found!");
            return;
        }

        // Purchase requires enough coins (set your price)
        int doubleThreatPrice = 300;
        if (playerInventory.CoinCount < doubleThreatPrice)
        {
            Debug.Log($"Not enough coins for Double Threat. Required: {doubleThreatPrice}, Have: {playerInventory.CoinCount}");
            
            // Show message to player
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowInformText($"Not enough coins! Need {doubleThreatPrice} coins.");
            }
            return;
        }

        // Deduct coins
        playerInventory.RemoveCoins(doubleThreatPrice);
        
        // Try to create two scorpion eggs
        bool success = incubator.CreateMultipleEggs("scorpion", 2);
        
        // Play sound and show message
        if (success)
        {
            // Play purchase complete sound
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound("PurchaseComplete");
            }
            
            // Show message to player
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowInformText("Double Threat purchased! Two scorpion eggs created.");
            }
            
            // Update coins display
            UpdateCoinsDisplay();
        }
        else
        {
            // Refund coins if eggs couldn't be created
            playerInventory.AddCoins(doubleThreatPrice);
            Debug.LogError("Failed to create scorpion eggs. Coins refunded.");
            
            // Show error message
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowInformText("Not enough incubation space for Double Threat!");
            }
        }
    }

    // Alternative direct purchase method for testing DoubleThreat
    public void BuyDoubleThreatDirect()
    {
        // This method can be called directly from UI buttons for testing or as an alternative to IAP
        OnDoubleThreatPurchase();
    }
}
