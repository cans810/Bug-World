using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiscoveredEntitiesController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel; // this is the panel that will be shown when the player discovers an entity, keep inactive until needed
    public GameObject container; // the prefabs will be instantiated in this container
    public GameObject entityContainerPrefab;

    [Header("Entity Definitions")]
    [SerializeField] private EntityDefinition[] entityDefinitions;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerController playerController;
    
    // Keep track of discovered entities
    private Dictionary<string, bool> discoveredEntities = new Dictionary<string, bool>();
    // Keep track of entity containers that have been instantiated
    private Dictionary<string, GameObject> entityContainers = new Dictionary<string, GameObject>();
    
    // Add dictionary to track previous discovered states
    private Dictionary<string, bool> previousDiscoveredStates = new Dictionary<string, bool>();
    
    // Singleton instance
    public static DiscoveredEntitiesController Instance { get; private set; }
    
    // Add this field at the class level
    private bool isInitialized = false;
    
    [System.Serializable]
    public class EntityDefinition
    {
        public string entityType;
        public string displayName;
        public Sprite entityIcon;
        public string description;
        public string price;
        public bool isBought;
        public bool isDiscovered;
        public int discoveryXPReward = 10; // Default XP reward for discovering this entity
    }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Initialize the panel to be inactive until needed
        if (panel != null)
            panel.SetActive(false);
            
        // Set up initial discovered state from definitions
        foreach (var definition in entityDefinitions)
        {
            discoveredEntities[definition.entityType] = definition.isDiscovered;
            // Also initialize previous states here in Awake
            previousDiscoveredStates[definition.entityType] = definition.isDiscovered;
        }
        
        // Disable Update checks until fully initialized
        isInitialized = false;
    }
    
    private void Start()
    {
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        
        // Debug log all entity definitions
        Debug.Log($"Entity Definitions loaded: {entityDefinitions.Length}");
        foreach (var def in entityDefinitions)
        {
            Debug.Log($"Entity Type: {def.entityType}, Display Name: {def.displayName}, isDiscovered: {def.isDiscovered}");
        }
        
        // Create UI for already discovered entities
        foreach (var definition in entityDefinitions)
        {
            if (definition.isDiscovered)
            {
                CreateEntityUI(definition);
            }
        }
        
        // Use a coroutine to delay enabling the update checks
        StartCoroutine(EnableUpdateChecksAfterDelay());
    }
    
    // Add this coroutine to delay enabling update checks
    private IEnumerator EnableUpdateChecksAfterDelay()
    {
        // Wait for 2 frames to ensure everything is initialized
        yield return null;
        yield return null;
        
        Debug.Log("Discovery system initialization complete - enabling update checks");
        isInitialized = true;
    }
    
    // Method to check and discover an entity by name
    public void CheckAndDiscoverEntity(string entityName)
    {
        Debug.Log($"DISCOVERY CHECK: Checking entity: {entityName}");
        
        if (entityDefinitions == null || entityDefinitions.Length == 0)
        {
            Debug.LogError("No entity definitions found! Check inspector setup.");
            return;
        }
        
        // First try direct/exact match
        foreach (var definition in entityDefinitions)
        {
            if (string.Equals(entityName, definition.entityType, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"DISCOVERY CHECK: Found exact match for {definition.entityType}");
                DiscoverEntity(definition.entityType);
                return;
            }
        }
        
        // Then try contains match
        foreach (var definition in entityDefinitions)
        {
            Debug.Log($"DISCOVERY CHECK: Comparing {entityName.ToLower()} with {definition.entityType.ToLower()}");
            // Check if the entity name contains the definition type OR vice versa
            if (entityName.ToLower().Contains(definition.entityType.ToLower()) || 
                definition.entityType.ToLower().Contains(entityName.ToLower()))
            {
                Debug.Log($"DISCOVERY CHECK: Found matching entity type: {definition.entityType}");
                DiscoverEntity(definition.entityType);
                return;
            }
        }
        
        Debug.LogWarning($"DISCOVERY CHECK: Entity {entityName} does not match any known types");
    }
    
    // Method to mark an entity as discovered and update UI
    public void DiscoverEntity(string entityType)
    {
        Debug.Log($"DISCOVERY SYSTEM: Attempting to discover entity type: {entityType}");
        
        // Check if this entity is already discovered
        if (discoveredEntities.ContainsKey(entityType) && discoveredEntities[entityType])
        {
            Debug.Log($"Entity {entityType} already discovered");
            return;
        }
        
        // Add direct debug to see if we have the definition
        EntityDefinition definition = null;
        for (int i = 0; i < entityDefinitions.Length; i++)
        {
            Debug.Log($"Checking definition [{i}]: {entityDefinitions[i].entityType}");
            if (string.Equals(entityDefinitions[i].entityType, entityType, System.StringComparison.OrdinalIgnoreCase))
            {
                definition = entityDefinitions[i];
                Debug.Log($"Found matching definition: {definition.displayName}");
                break;
            }
        }
        
        if (definition == null)
        {
            Debug.LogError($"No definition found for entity type: {entityType}");
            return;
        }
        
        // Mark as discovered
        discoveredEntities[entityType] = true;
        definition.isDiscovered = true;
        
        Debug.Log($"Entity {entityType} is now discovered!");
        
        // Award XP for discovery
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            // Award XP and show animation
            int xpAwarded = definition.discoveryXPReward;
            playerInventory.AddExperience(xpAwarded);
            
            // If we have a visual effect manager, trigger the XP visual effect
            VisualEffectManager visualEffectManager = FindObjectOfType<VisualEffectManager>();
            if (visualEffectManager != null)
            {
                // Get player position for the effect origin
                Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (playerTransform != null)
                {
                    visualEffectManager.PlayXPGainEffect(playerTransform.position, xpAwarded);
                }
            }
            
            Debug.Log($"Awarded {xpAwarded} XP for discovering {definition.displayName}");
        }
        
        // Create UI element for this entity
        CreateEntityUI(definition);
        
        // Show discovery notification
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            uiHelper.ShowInformText($"New creature discovered: {definition.displayName}! (+{definition.discoveryXPReward} XP)");
        }
        
        // Save the discovery
        SaveDiscovery(entityType);
    }
    
    // Create UI element for a discovered entity
    private void CreateEntityUI(EntityDefinition definition)
    {
        if (container == null || entityContainerPrefab == null)
        {
            Debug.LogError("Container or entity prefab not assigned!");
            return;
        }
        
        // Check if we already have a container for this entity
        if (entityContainers.ContainsKey(definition.entityType))
        {
            // Just update existing container
            UpdateEntityUI(definition);
            return;
        }
        
        // Instantiate new entity container
        GameObject entityContainer = Instantiate(entityContainerPrefab, container.transform);
        entityContainers[definition.entityType] = entityContainer;
        
        // Set up the UI elements with correct names based on your prefab
        TextMeshProUGUI nameText = entityContainer.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = entityContainer.transform.Find("EntityDesc")?.GetComponent<TextMeshProUGUI>();
        Image iconImage = entityContainer.transform.Find("EntityIcon")?.GetComponent<Image>();
        
        if (nameText != null)
            nameText.text = definition.displayName;
            
        if (descriptionText != null)
            descriptionText.text = definition.description;
            
        if (iconImage != null && definition.entityIcon != null)
            iconImage.sprite = definition.entityIcon;
        else
            Debug.LogWarning($"Could not set icon for {definition.displayName}. IconImage: {iconImage != null}, IconSprite: {definition.entityIcon != null}");
        
        // Get and set up purchase-related UI elements
        TextMeshProUGUI priceText = entityContainer.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
        Button buyButton = entityContainer.transform.Find("BuyButton")?.GetComponent<Button>();
        GameObject purchasedLabel = entityContainer.transform.Find("PurchasedLabel")?.gameObject;
        
        // Set up price display
        if (priceText != null)
            priceText.text = definition.price;
        
        // Configure button and purchased status
        if (buyButton != null && purchasedLabel != null)
        {
            // Update UI based on purchase status
            buyButton.gameObject.SetActive(!definition.isBought);
            purchasedLabel.SetActive(definition.isBought);
            
            // Set up button click handler
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => TryPurchaseEntity(definition.entityType));
        }
    }
    
    // Update an existing entity UI element
    private void UpdateEntityUI(EntityDefinition definition)
    {
        if (!entityContainers.ContainsKey(definition.entityType))
            return;
            
        GameObject entityContainer = entityContainers[definition.entityType];
        
        // Update the UI elements with correct component names
        TextMeshProUGUI nameText = entityContainer.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = entityContainer.transform.Find("EntityDesc")?.GetComponent<TextMeshProUGUI>();
        Image iconImage = entityContainer.transform.Find("EntityIcon")?.GetComponent<Image>();
        
        if (nameText != null)
            nameText.text = definition.displayName;
            
        if (descriptionText != null)
            descriptionText.text = definition.description;
            
        if (iconImage != null && definition.entityIcon != null)
            iconImage.sprite = definition.entityIcon;
        else
            Debug.LogWarning($"Could not update icon for {definition.displayName}");
    }
    
    // Show the catalog UI
    public void ShowEntityCatalog()
    {
        if (panel != null)
            panel.SetActive(true);
        
        // Make sure container is active so entity prefabs are visible
        if (container != null && !container.activeSelf)
            container.SetActive(true);
    }
    
    // Hide the catalog UI
    public void HideEntityCatalog()
    {
        if (panel != null)
            panel.SetActive(false);
    }
    
    // Toggle the catalog visibility
    public void ToggleEntityCatalog()
    {
        if (panel != null)
            panel.SetActive(!panel.activeSelf);
        
        // Ensure container is active when panel is active
        if (container != null && panel != null && panel.activeSelf)
            container.SetActive(true);
    }
    
    // Save discovered entity to game data
    private void SaveDiscovery(string entityType)
    {
        // Get GameManager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager not found for saving entity discovery");
            return;
        }
        
        // Update game data based on entity type
        switch (entityType.ToLower())
        {
            case "ant":
                gameManager.SetEntityDiscovered("ant", true);
                break;
            case "fly":
                gameManager.SetEntityDiscovered("fly", true);
                break;
            case "ladybug":
                gameManager.SetEntityDiscovered("ladybug", true);
                break;
            default:
                Debug.LogWarning($"Unknown entity type for saving: {entityType}");
                break;
        }
        
        // Optional: Trigger a save
        gameManager.SaveGame();
    }
    
    // Load discovered entities from game data
    public void LoadDiscoveredEntities(GameData gameData)
    {
        if (gameData == null)
            return;
            
        // Update the discovery status for each entity type
        UpdateDiscoveryStatus("ant", gameData.hasDiscoveredAnt);
        UpdateDiscoveryStatus("fly", gameData.hasDiscoveredFly);
        UpdateDiscoveryStatus("ladybug", gameData.hasDiscoveredLadybug);
    }
    
    // Update discovery status and UI for an entity
    private void UpdateDiscoveryStatus(string entityType, bool isDiscovered)
    {
        // Find matching definition
        foreach (var definition in entityDefinitions)
        {
            if (definition.entityType.ToLower() == entityType.ToLower())
            {
                // If newly discovered, create UI
                if (isDiscovered && !definition.isDiscovered)
                {
                    definition.isDiscovered = true;
                    discoveredEntities[definition.entityType] = true;
                    CreateEntityUI(definition);
                }
                // Update existing discovery status
                else
                {
                    definition.isDiscovered = isDiscovered;
                    discoveredEntities[definition.entityType] = isDiscovered;
                }
                break;
            }
        }
    }
    
    public void TryPurchaseEntity(string entityType)
    {
        // Find the definition
        EntityDefinition definition = System.Array.Find(entityDefinitions, d => d.entityType == entityType);
        
        if (definition == null)
        {
            Debug.LogError($"Cannot purchase: no definition found for {entityType}");
            return;
        }
        
        // Already purchased check
        if (definition.isBought)
        {
            Debug.Log($"{definition.displayName} is already purchased");
            return;
        }
        
        // Get player inventory to check resources
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogError("Cannot purchase: PlayerInventory not found");
            return;
        }
        
        // Parse price (assuming price is stored as string like "50 Chitin")
        int cost = 0;
        bool usesCrumbs = definition.price.ToLower().Contains("crumb");
        
        try
        {
            // Extract number from price string
            string numberPart = System.Text.RegularExpressions.Regex.Match(definition.price, @"\d+").Value;
            cost = int.Parse(numberPart);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing price '{definition.price}': {e.Message}");
            return;
        }
        
        // Check if player has enough resources
        bool canAfford = usesCrumbs ? 
            playerInventory.CrumbCount >= cost : 
            playerInventory.ChitinCount >= cost;
        
        if (!canAfford)
        {
            // Show "cannot afford" message
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                string resourceType = usesCrumbs ? "crumbs" : "chitin";
                uiHelper.ShowInformText($"Not enough {resourceType} to purchase {definition.displayName}");
            }
            return;
        }
        
        // Purchase successful - deduct resources
        if (usesCrumbs)
            playerInventory.RemoveCrumbs(cost);
        else
            playerInventory.RemoveChitin(cost);
        
        // Mark as purchased
        definition.isBought = true;
        
        // Update UI
        UpdateEntityUI(definition);
        
        // Show success message
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            uiHelper.ShowInformText($"Successfully purchased {definition.displayName}!");
        }
        
        // Save the purchase
        SavePurchase(entityType);
    }

    // Add method to save purchase status
    private void SavePurchase(string entityType)
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager not found for saving entity purchase");
            return;
        }
        
        // Update game data based on entity type
        switch (entityType.ToLower())
        {
            case "ant":
                gameManager.SetEntityPurchased("ant", true);
                break;
            case "fly":
                gameManager.SetEntityPurchased("fly", true);
                break;
            case "ladybug":
                gameManager.SetEntityPurchased("ladybug", true);
                break;
            default:
                Debug.LogWarning($"Unknown entity type for saving purchase: {entityType}");
                break;
        }
        
        // Optional: Trigger a save
        gameManager.SaveGame();
    }
    
    private void Update()
    {
        // Skip update checks until fully initialized
        if (!isInitialized)
            return;

        // Check for manual changes in the inspector
        foreach (var definition in entityDefinitions)
        {
            // Get previous state
            bool previousState = false;
            if (previousDiscoveredStates.ContainsKey(definition.entityType))
            {
                previousState = previousDiscoveredStates[definition.entityType];
            }
            
            // If state changed from false to true
            if (definition.isDiscovered && !previousState)
            {
                Debug.Log($"MANUAL DISCOVERY: {definition.displayName} was discovered via inspector");
                
                // Update state tracking
                previousDiscoveredStates[definition.entityType] = true;
                
                // Create UI for this entity
                CreateEntityUI(definition);
                
                // Update dictionary (for potential other code that uses it)
                discoveredEntities[definition.entityType] = true;
                
                // Optionally show discovery notification
                
                // Optional: Save the discovery
                SaveDiscovery(definition.entityType);
            }
            // If state changed from true to false
            else if (!definition.isDiscovered && previousState)
            {
                Debug.Log($"MANUAL UNDISCOVERY: {definition.displayName} was marked as undiscovered via inspector");
                
                // Update state tracking
                previousDiscoveredStates[definition.entityType] = false;
                
                // Remove UI if it exists
                if (entityContainers.ContainsKey(definition.entityType))
                {
                    Destroy(entityContainers[definition.entityType]);
                    entityContainers.Remove(definition.entityType);
                }
                
                // Update dictionary
                discoveredEntities[definition.entityType] = false;
            }
        }
    }
}
