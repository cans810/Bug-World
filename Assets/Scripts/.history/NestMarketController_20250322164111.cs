using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NestMarketController : MonoBehaviour
{
    public GameObject nestPanel;

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

    public TextMeshProUGUI playerCoinsText;
    
    // Prices
    private int nest2Price = 500;
    private int nest3Price = 750;
    
    // Player inventory reference
    private PlayerInventory playerInventory;

    [Header("Camera Animation")]
    [SerializeField] private CameraAnimations cameraAnimations;
    [SerializeField] private Transform nest2CameraTarget;  // Transform to focus on for Nest 2
    [SerializeField] private Transform nest3CameraTarget;  // Transform to focus on for Nest 3

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
        
        // Initialize coin display
        UpdateCoinDisplay();
        
        // Subscribe to coin changed event to update the display
        if (playerInventory != null)
        {
            playerInventory.OnCoinCountChanged += UpdateCoinDisplay;
        }
    }
    
    private void UpdateCoinDisplay(int coinCount = -1)
    {
        if (playerCoinsText != null && playerInventory != null)
        {
            // If coinCount parameter is provided and valid, use it
            // Otherwise get it from the playerInventory
            int coinsToDisplay = coinCount >= 0 ? coinCount : playerInventory.CoinCount;
            playerCoinsText.text = $"{coinsToDisplay}";
        }
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
            
            // Update coin display
            UpdateCoinDisplay();
            
            // Mark as purchased
            isNest2Purchased = true;
            
            // Update UI
            UpdateNestStatus();
            
            // Save purchase
            SaveNestPurchases();
            
            // Play purchase complete sound
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound("PurchaseComplete");
                Debug.Log("Playing purchase complete sound for Nest 2");
            }

            playerNest2GameObject.GetComponent<Animator>().SetBool("ShowUp", false);
            playerNest2GameObject.GetComponent<Animator>().SetBool("Hide", true);
            
            // Show the new nest using camera animation
            ShowNewNestAnimation(2);
            
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
            
            // Update coin display
            UpdateCoinDisplay();
            
            // Mark as purchased
            isNest3Purchased = true;
            
            // Update UI
            UpdateNestStatus();
            
            // Save purchase
            SaveNestPurchases();
            
            // Play purchase complete sound
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound("PurchaseComplete");
                Debug.Log("Playing purchase complete sound for Nest 3");
            }

            playerNest3GameObject.GetComponent<Animator>().SetBool("ShowUp", false);
            playerNest3GameObject.GetComponent<Animator>().SetBool("Hide", true);
            
            // Show the new nest using camera animation after successful purchase
            ShowNewNestAnimation(3);
            
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
        // Get the base interaction component
        BaseInteraction baseInteraction = nestObject.GetComponent<BaseInteraction>();
        if (baseInteraction != null)
        {
            // Use the new SetPurchased method
            baseInteraction.SetPurchased(enable);
            Debug.Log($"Set {nestObject.name} purchase state to {enable}");
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

    // Added to ensure we clean up event subscriptions
    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnCoinCountChanged -= UpdateCoinDisplay;
        }
    }

    public void MakeNestPanelActive(){
        nestPanel.SetActive(true);
    }

    public void MakeNestPanelInactive(){
        nestPanel.SetActive(false);
    }

    private void ShowNewNestAnimation(int nestNumber)
    {
        // Get the target transform based on which nest was purchased
        Transform targetTransform = null;
        
        if (nestNumber == 2)
        {
            targetTransform = nest2CameraTarget;
            if (targetTransform == null && playerNest2GameObject != null)
            {
                targetTransform = playerNest2GameObject.transform;
            }
        }
        else if (nestNumber == 3)
        {
            targetTransform = nest3CameraTarget;
            if (targetTransform == null && playerNest3GameObject != null)
            {
                targetTransform = playerNest3GameObject.transform;
            }
        }
        
        if (targetTransform == null)
        {
            Debug.LogWarning($"No target transform found for Nest {nestNumber}, cannot show animation.");
            return;
        }
        
        // Start our own camera animation coroutine
        StartCoroutine(AnimateCameraToTarget(targetTransform));
        
        // Play a sound effect for the camera movement/reveal
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("Unlock", transform.position, false);
        }
        
        Debug.Log($"Playing camera animation to show newly purchased Nest {nestNumber}");
    }

    // Fix camera animation to properly show the nest
    private IEnumerator AnimateCameraToTarget(Transform target)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera not found, cannot animate camera movement.");
            yield break;
        }
        
        // Save starting camera position
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        
        // Calculate a better target position that clearly shows the nest
        // Position the camera higher up and further back
        Vector3 targetPos = target.position + new Vector3(0, 5, -10);
        
        // Make the camera look directly at the nest
        Vector3 directionToTarget = target.position - targetPos;
        Quaternion targetRot = Quaternion.LookRotation(directionToTarget);
        
        // Disable player control during animation
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // First temporarily disable any camera follow script
        MonoBehaviour[] cameraScripts = mainCamera.GetComponents<MonoBehaviour>();
        List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
        
        foreach (MonoBehaviour script in cameraScripts)
        {
            // Don't disable this script
            if (script.GetType() != typeof(NestMarketController) && script.enabled)
            {
                script.enabled = false;
                disabledScripts.Add(script);
                Debug.Log($"Temporarily disabled camera script: {script.GetType().Name}");
            }
        }
        
        // Animation parameters
        float moveDuration = 2.0f;
        float elapsed = 0f;
        
        // Move to target position
        Debug.Log($"Starting camera movement to show nest at {target.position}");
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            
            // Use easing for smoother movement
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // Move camera directly
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            
            yield return null;
        }
        
        // Make sure we're exactly at the target
        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;
        
        Debug.Log("Camera is now at target position, holding...");
        
        // Play the ShowNest sound when camera is viewing the nest
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("ShowNest", transform.position, false);
            Debug.Log("Playing ShowNest sound effect");
        }
        
        // Show a UI message about the nest
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            string message = target.name.Contains("2") ? 
                "Nest 2 purchased! You can now deposit resources here." : 
                "Nest 3 purchased! You can now deposit resources here.";
            uiHelper.ShowInformText(message);
        }
        
        // Hold at the target for a longer moment
        yield return new WaitForSeconds(3.5f);
        
        // Animate back to original position
        elapsed = 0f;
        float returnDuration = 2.0f;
        
        Debug.Log("Returning camera to player");
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            
            // Use easing for smoother movement
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // Move camera back
            mainCamera.transform.position = Vector3.Lerp(targetPos, startPos, smoothT);
            mainCamera.transform.rotation = Quaternion.Slerp(targetRot, startRot, smoothT);
            
            yield return null;
        }
        
        // Ensure we're exactly back at the start
        mainCamera.transform.position = startPos;
        mainCamera.transform.rotation = startRot;
        
        // Re-enable the camera scripts we disabled
        foreach (MonoBehaviour script in disabledScripts)
        {
            script.enabled = true;
        }
        
        // Re-enable player control
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        Debug.Log("Camera animation completed");
    }
}
