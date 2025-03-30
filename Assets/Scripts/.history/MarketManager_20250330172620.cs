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
            // Process Double Threat purchase with real money
            Debug.Log("Processing Double Threat purchase from IAP");
            
            // Get insect incubator reference
            InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
            if (incubator != null)
            {
                // Double-check if there's enough space
                int currentEggCount = incubator.GetCurrentEggCount();
                int maxCapacity = incubator.GetMaxEggCapacity();
                bool eggExists = InsectIncubator.EggExists;
                bool hasSpace = currentEggCount + 2 <= maxCapacity && !eggExists;
                
                // Log detailed space info for debugging
                Debug.Log($"Space check: Current={currentEggCount}, Max={maxCapacity}, " +
                          $"EggExists={eggExists}, HasSpace={hasSpace}");
                
                if (!hasSpace)
                {
                    // Show message about insufficient space
                    UIHelper uiHelper = FindObjectOfType<UIHelper>();
                    if (uiHelper != null)
                    {
                        string message = eggExists ? 
                            "An egg is already incubating! Purchase completed but can't create eggs." : 
                            "Not enough incubation space! Purchase completed but can't create eggs.";
                        
                        uiHelper.ShowInformText(message);
                    }
                    
                    Debug.LogError("Double Threat purchased but failed to create eggs - insufficient space");
                    
                    // Close the market panel
                    HideMarketPanel();
                    
                    // Return Complete to acknowledge purchase but DON'T create eggs
                    return PurchaseProcessingResult.Complete;
                }
                
                // If we have enough space, create the eggs
                Debug.Log("Creating eggs: Space check passed!");
                bool success = incubator.CreateMultipleEggs("scorpion", 2);
                
                if (success)
                {
                    // Try to find an egg to showcase
                    GameObject[] eggs = GameObject.FindGameObjectsWithTag("AllyEgg");
                    GameObject eggToShow = eggs.Length > 0 ? eggs[0] : GameObject.Find("AntEggPile(Clone)");
                    
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
                    
                    // Close the market panel
                    HideMarketPanel();
                    
                    // Show the egg after a short delay (allow market panel to close first)
                    if (eggToShow != null)
                    {
                        StartCoroutine(ShowEggAfterDelay(eggToShow, 0.5f));
                    }
                }
                else
                {
                    // Show error message
                    UIHelper uiHelper = FindObjectOfType<UIHelper>();
                    if (uiHelper != null)
                    {
                        uiHelper.ShowInformText("Purchase completed but egg creation failed!");
                    }
                    
                    Debug.LogError("Double Threat purchase: Failed to create eggs");
                    
                    // Close the market panel
                    HideMarketPanel();
                }
            }
            else
            {
                Debug.LogError("InsectIncubator not found for Double Threat purchase");
            }
            
            return PurchaseProcessingResult.Complete;
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
                    
                    // Close the market panel
                    HideMarketPanel();
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

        // For Double Threat, check incubation space first
        if (productId == "doubleThreat")
        {
            // Check if there's enough incubation space before initiating purchase
            InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
            if (incubator != null)
            {
                int currentEggCount = incubator.GetCurrentEggCount();
                int maxCapacity = incubator.GetMaxEggCapacity();
                bool hasSpace = currentEggCount + 2 <= maxCapacity && !InsectIncubator.EggExists;
                
                if (!hasSpace)
                {
                    // Show message and don't initiate purchase
                    UIHelper uiHelper = FindObjectOfType<UIHelper>();
                    if (uiHelper != null)
                    {
                        uiHelper.ShowInformText("You don't have enough incubation space, purchase failed.");
                    }
                    
                    // Close market panel
                    HideMarketPanel();
                    
                    Debug.Log("Double Threat purchase cancelled: insufficient incubation space");
                    return;
                }
            }
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
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            uiHelper.CloseMarket();
        }
    }

    public bool IsInitialized()
    {
        return isInitialized;
    }

    // For testing purposes, you can keep a direct purchase button method
    public void BuyDoubleThreatDirect()
    {
        // Check if there's enough incubation space before initiating purchase
        InsectIncubator incubator = FindObjectOfType<InsectIncubator>();
        if (incubator != null)
        {
            int currentEggCount = incubator.GetCurrentEggCount();
            int maxCapacity = incubator.GetMaxEggCapacity();
            bool hasSpace = currentEggCount + 2 <= maxCapacity && !InsectIncubator.EggExists;
            
            if (!hasSpace)
            {
                // Show message and don't initiate purchase
                UIHelper uiHelper = FindObjectOfType<UIHelper>();
                if (uiHelper != null)
                {
                    uiHelper.ShowInformText("You don't have enough incubation space, purchase failed.");
                }
                
                // Close market panel
                HideMarketPanel();
                
                Debug.Log("Double Threat direct purchase cancelled: insufficient incubation space");
                return;
            }
        }

        if (storeController != null)
        {
            storeController.InitiatePurchase("doubleThreat");
            Debug.Log("Initiating IAP purchase for doubleThreat");
        }
        else
        {
            Debug.LogError("Cannot purchase doubleThreat: store not initialized");
        }
    }

    // Add this method to show egg animation after delay
    private IEnumerator ShowEggAfterDelay(GameObject egg, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Find camera animations controller
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        
        // Check if there's already an animation in progress
        if (cameraAnimations != null && !cameraAnimations.IsAnimationInProgress())
        {
            // Use the egg-specific animation method
            cameraAnimations.AnimateToEgg(egg.transform);
            Debug.Log($"Camera animating to show newly created scorpion egg: {egg.name}");
        }
        else if (cameraAnimations != null)
        {
            Debug.Log("Camera animation already in progress - skipping egg animation");
        }
    }

    // Add this method to receive the ShowMarketPanel animation event
    public void ShowMarketPanel()
    {
        Debug.Log("ShowMarketPanel animation event received");
        
        // Make sure the market panel is visible
        if (MarketPanel != null)
        {
            MarketPanel.SetActive(true);
        }
        
        // Update the coins display
        UpdateCoinsDisplay();
        
        // Update prices
        if (isInitialized)
        {
            UpdatePriceDisplay();
        }
    }
}
