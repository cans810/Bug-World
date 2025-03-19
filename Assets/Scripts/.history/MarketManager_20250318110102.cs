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

    // Example point packages
    private readonly Dictionary<string, int> pointPackages = new Dictionary<string, int>
    {
        { "250coins", 250 },
        { "600_points", 600 },
        { "1500_points", 1500 },
        { "3500_points", 3500 },
        { "8000_points", 8000 },
    };

    private Coroutine pointAnimationCoroutine;

    // Start is called before the first frame update
    private void Start()
    {
        Debug.Log("MarketManager Start");
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        InitializePurchasing();
        
    }

    private void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        
        // Add products
        builder.AddProduct("200_points", ProductType.Consumable);
        builder.AddProduct("600_points", ProductType.Consumable);
        builder.AddProduct("1500_points", ProductType.Consumable);
        builder.AddProduct("3500_points", ProductType.Consumable);
        builder.AddProduct("8000_points", ProductType.Consumable);


        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
        
        
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

        if (pointPackages.ContainsKey(productId))
        {
            Debug.Log($"Processing points purchase for {productId}");
            int pointsToAdd = pointPackages[productId];
            AddPoints(pointsToAdd);
            Debug.Log($"Points purchase completed. New total: {GameManager.Instance.CurrentPoints}");
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

    private void AddPoints(int points)
    {
        Debug.Log($"=== Adding Points from Market ===");
        Debug.Log($"Current points before adding: {GameManager.Instance.CurrentPoints}");
        Debug.Log($"Points to add: {points}");

        int startPoints = GameManager.Instance.CurrentPoints;
        
        // Directly modify GameManager's points
        GameManager.Instance.CurrentPoints += points;
        
        Debug.Log($"New total points: {GameManager.Instance.CurrentPoints}");
        
        // Save immediately
        SaveManager.Instance.SaveGame();
        Debug.Log("Points saved to SaveManager");
        
        // Update display
        UpdatePointsDisplay();
        
        // Animate the change
        if (pointAnimationCoroutine != null)
        {
            StopCoroutine(pointAnimationCoroutine);
        }
        pointAnimationCoroutine = StartCoroutine(AnimatePointsChange(startPoints, GameManager.Instance.CurrentPoints));
    }

    private IEnumerator AnimatePointsChange(int startPoints, int endPoints)
    {
        float elapsedTime = 0f;
        float animationDuration = 1.1f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            t = t * t * (3 - 2 * t); // Smooth interpolation
            
            int currentPoints = Mathf.RoundToInt(Mathf.Lerp(startPoints, endPoints, t));
            
            if (pointsText != null)
            {
                pointsText.text = currentPoints.ToString();
                pointsText.color = Color.green;
            }
            
            yield return null;
        }
        
        // Ensure we end at exact value
        if (pointsText != null)
        {
            pointsText.text = endPoints.ToString();
            pointsText.color = Color.white;
        }
    }

    private void UpdatePointsDisplay()
    {
        if (pointsText != null)
        {
            pointsText.text = GameManager.Instance.CurrentPoints.ToString();
        }
    }


    // Method to initiate purchase
    public void BuyProduct(string productId)
    {
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

    public void OnReturnButtonPressed()
    {
        animator.SetBool("DeLoad", true);
    }

    public void OnReturnButtonClickedSetDeactive()
    {
        gameObject.GetComponent<Canvas>().sortingLayerName = "BackgroundImage";
        gameObject.GetComponent<Canvas>().sortingOrder = -2;
        gameObject.SetActive(false);
    }

    public void ShowMarketScreen()
    {
        Debug.Log("=== ShowMarketScreen ===");
        gameObject.SetActive(true);
        gameObject.GetComponent<Canvas>().sortingLayerName = "UI";
        gameObject.GetComponent<Canvas>().sortingOrder = 3;
    
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        UpdatePointsDisplay();
        
        // Make sure we initialize purchasing before updating the button
        InitializePurchasing();
        
        Debug.Log("Market screen shown and initialized");
    }
}
