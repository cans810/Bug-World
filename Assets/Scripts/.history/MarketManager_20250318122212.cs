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

    // Example point packages
    private readonly Dictionary<string, int> coinPackages = new Dictionary<string, int>
    {
        { "250coins", 250 },
        { "500coins", 500 },
        { "750coins", 750 },
        { "1000coins", 1000 },
    };

    // Add this field to track initialization status
    private bool isInitialized = false;

    // Start is called before the first frame update
    private void Start()
    {
        InitializePurchasing();
    }

    public void InitializePurchasing()
    {
        // If already initialized, return
        if (isInitialized) return;

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        
        // Add products
        builder.AddProduct("250coins", ProductType.Consumable);
        builder.AddProduct("500coins", ProductType.Consumable);
        builder.AddProduct("750coins", ProductType.Consumable);
        builder.AddProduct("1000coins", ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
        isInitialized = true; // Set initialized flag
        
        Debug.Log("IAP Initialization successful!");
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

        if (coinPackages.ContainsKey(productId))
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
                }
                else
                {
                    Debug.LogError("Failed to add coins to player inventory");
            AddCoins(coinsToAdd);
            Debug.Log($"Coins purchase completed. New total: {playerInventory.currentCoinCount}");
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
        Debug.Log($"=== Adding Coins from Market ===");
        Debug.Log($"Current coins before adding: {playerInventory.currentCoinCount}");
        Debug.Log($"Coins to add: {coins}");

        int startCoins = playerInventory.currentCoinCount;
        
        // Directly modify GameManager's coins
        playerInventory.currentCoinCount += coins;
        
        Debug.Log($"New total coins: {playerInventory.currentCoinCount}");
        
        // Save immediately
        
        // Update display
        UpdateCoinsDisplay();
    }

    public void UpdateCoinsDisplay()
    {
        if (coinsText != null)
        {
            coinsText.text = playerInventory.currentCoinCount.ToString();
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
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }
}
