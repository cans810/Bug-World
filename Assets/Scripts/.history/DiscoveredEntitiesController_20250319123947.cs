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
    
    // Add these fields to the DiscoveredEntitiesController class
    private string currentlyEquippedEntity = "";
    private SelectedEntityCrumbCollectionCounter crumbCounter;

    public GameObject newEntityDiscoveredPanel; 
    // this is the panel that will be shown when the player discovers an entity, keep inactive until needed,
    // it will have a close button and a text component that will display the name of the entity discovered
    // it will also have an icon image that will display the icon of the entity
    
    // Add this field to keep track of the insect incubator
    private InsectIncubator insectIncubator;
    
    [System.Serializable]
    public class EntityDefinition
    {
        public string entityType;
        public string displayName;
        public Sprite entityIcon;
        public string description;
        public string price;
        public int coinCost = 100; // Actual numerical cost in coins
        public bool isBought;
        public bool isDiscovered;
        public bool isEquipped; // Add new field to track equipped state
        public int crumbCost = 10; // Default crumb cost
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
        // Make sure the new entity discovered panel is initially inactive
        if (newEntityDiscoveredPanel != null)
        {
            newEntityDiscoveredPanel.SetActive(false);
        }
        
        // Ensure the controller is properly initialized before showing anything
        StartCoroutine(DelayedInitialization());

        // Find the insect incubator in the scene
        insectIncubator = GameObject.Find("AllyIncubator")?.GetComponent<InsectIncubator>();
        if (insectIncubator == null)
        {
            Debug.LogWarning("InsectIncubator not found in the scene!");
        }
    }
    
    private IEnumerator DelayedInitialization()
    {
        // Wait for one frame to ensure all other systems are initialized
        yield return null;
        
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        
        // Debug log all entity definitions
        Debug.Log($"Entity Definitions loaded: {entityDefinitions.Length}");
        foreach (var def in entityDefinitions)
        {
            Debug.Log($"Entity Type: {def.entityType}, Display Name: {def.displayName}, isDiscovered: {def.isDiscovered}");
        }
        
        // Refresh UI for all discovered entities - this will create the UI for any entities
        // that were discovered in previous game sessions
        RefreshDiscoveredEntitiesUI();
        
        // Now we're fully initialized
        isInitialized = true;
        Debug.Log("DiscoveredEntitiesController initialized");
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
        TextMeshProUGUI nameText = entityContainer.transform.Find("EntityName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = entityContainer.transform.Find("EntityDesc")?.GetComponent<TextMeshProUGUI>();
        Image iconImage = entityContainer.transform.Find("EntityIcon")?.GetComponent<Image>();
        // Add price text component
        TextMeshProUGUI priceText = entityContainer.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
        
        if (nameText != null)
            nameText.text = definition.displayName;
            
        if (descriptionText != null)
            descriptionText.text = definition.description;
            
        if (iconImage != null && definition.entityIcon != null)
            iconImage.sprite = definition.entityIcon;
        else
            Debug.LogWarning($"Could not set icon for {definition.displayName}. IconImage: {iconImage != null}, IconSprite: {definition.entityIcon != null}");
        
        // Set price text if available
        if (priceText != null)
        {
            priceText.text = definition.isBought ? "Owned" : definition.price;
            Debug.Log($"Set price text for {definition.displayName} to: {priceText.text}");
        }
        else
        {
            Debug.LogWarning($"Price text component not found for {definition.displayName}");
        }
        
        // Set up purchase button - skip for flea and larvae
        Button purchaseButton = entityContainer.transform.Find("PurchaseButton")?.GetComponent<Button>();
        if (purchaseButton != null)
        {
            // Disable purchase button for flea and larvae
            if (definition.entityType.ToLower() == "flea" || definition.entityType.ToLower() == "larvae")
            {
                purchaseButton.gameObject.SetActive(false);
            }
            else
            {
                // Update button text based on purchase status
                TextMeshProUGUI buttonText = purchaseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = definition.isBought ? "Owned" : "Purchase";
                }
                
                // Set button interactability based on purchase status
                purchaseButton.interactable = !definition.isBought;
                
                // Store the entity type with the button for the purchase method
                string entityTypeForButton = definition.entityType;
                
                // Add click listener
                purchaseButton.onClick.RemoveAllListeners(); // Clear any existing listeners
                purchaseButton.onClick.AddListener(() => PurchaseEntity(entityTypeForButton));
            }
        }
        else
        {
            Debug.LogWarning($"Purchase button not found for {definition.displayName}");
        }
        
        // Set up equip button - only show when entity is bought and not flea or larvae
        Button equipButton = entityContainer.transform.Find("EquipButton")?.GetComponent<Button>();
        if (equipButton != null)
        {
            // Set button visibility based on purchase status and entity type
            equipButton.gameObject.SetActive(definition.isBought && 
                definition.entityType.ToLower() != "flea" && 
                definition.entityType.ToLower() != "larvae");
            
            // Only add listener if not flea or larvae
            if (definition.entityType.ToLower() != "flea" && 
                definition.entityType.ToLower() != "larvae" && 
                definition.isBought)
            {
                // Update button text based on equipped status
                TextMeshProUGUI buttonText = equipButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = definition.isEquipped ? "Equipped" : "Equip";
                }
                
                // Store the entity type with the button for the equip method
                string entityTypeForButton = definition.entityType;
                
                // Add click listener for equipping
                equipButton.onClick.RemoveAllListeners();
                equipButton.onClick.AddListener(() => EquipEntity(entityTypeForButton));
            }
        }
        else
        {
            Debug.LogWarning($"Equip button not found for {definition.displayName}");
        }
    }
    
    // Update an existing entity UI element
    private void UpdateEntityUI(EntityDefinition definition)
    {
        if (!entityContainers.ContainsKey(definition.entityType))
            return;
            
        GameObject entityContainer = entityContainers[definition.entityType];
        
        // Update the UI elements with correct component names
        TextMeshProUGUI nameText = entityContainer.transform.Find("EntityName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = entityContainer.transform.Find("EntityDesc")?.GetComponent<TextMeshProUGUI>();
        Image iconImage = entityContainer.transform.Find("EntityIcon")?.GetComponent<Image>();
        // Add price text component update
        TextMeshProUGUI priceText = entityContainer.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
        
        if (nameText != null)
            nameText.text = definition.displayName;
            
        if (descriptionText != null)
            descriptionText.text = definition.description;
            
        if (iconImage != null && definition.entityIcon != null)
            iconImage.sprite = definition.entityIcon;
        else
            Debug.LogWarning($"Could not update icon for {definition.displayName}");
        
        // Update price text if available
        if (priceText != null)
        {
            priceText.text = definition.isBought ? "Owned" : definition.price;
        }
        
        // Update purchase button - skip for flea and larvae
        Button purchaseButton = entityContainer.transform.Find("PurchaseButton")?.GetComponent<Button>();
        if (purchaseButton != null)
        {
            // Disable purchase button for flea and larvae
            if (definition.entityType.ToLower() == "flea" || definition.entityType.ToLower() == "larvae")
            {
                purchaseButton.gameObject.SetActive(false);
            }
            else
            {
                // Update button text based on purchase status
                TextMeshProUGUI buttonText = purchaseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = definition.isBought ? "Owned" : "Purchase";
                }
                
                // Set button interactability based on purchase status
                purchaseButton.interactable = !definition.isBought;
            }
        }
        
        // Update equip button visibility and listener - skip for flea and larvae
        Button equipButton = entityContainer.transform.Find("EquipButton")?.GetComponent<Button>();
        if (equipButton != null)
        {
            // Set button visibility based on purchase status and entity type
            equipButton.gameObject.SetActive(definition.isBought && 
                definition.entityType.ToLower() != "flea" && 
                definition.entityType.ToLower() != "larvae");
            
            // Only update listener if not flea or larvae
            if (definition.entityType.ToLower() != "flea" && 
                definition.entityType.ToLower() != "larvae" && 
                definition.isBought)
            {
                // Update button text based on equipped status
                TextMeshProUGUI buttonText = equipButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = definition.isEquipped ? "Equipped" : "Equip";
                }
                
                string entityTypeForButton = definition.entityType;
                equipButton.onClick.RemoveAllListeners();
                equipButton.onClick.AddListener(() => EquipEntity(entityTypeForButton));
            }
        }
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
    public void SaveDiscovery(string entityType)
    {
        // First update our local tracking
        discoveredEntities[entityType.ToLower()] = true;
        previousDiscoveredStates[entityType.ToLower()] = true;
        
        // Update the definition
        foreach (var definition in entityDefinitions)
        {
            if (definition.entityType.ToLower() == entityType.ToLower())
            {
                definition.isDiscovered = true;
                break;
            }
        }
        
        // Then save to GameData via GameManager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            // Let GameManager know this entity is discovered
            gameManager.SetEntityDiscovered(entityType, true);
            
            // Save the game
            gameManager.SaveGame();
            
            Debug.Log($"Saved discovery of {entityType} to game data");
        }
        else
        {
            Debug.LogWarning("Could not find GameManager to save entity discovery");
        }
    }
    
    // Load discovered entities from game data
    public void LoadDiscoveredEntities(GameData data)
    {
        if (data == null)
        {
            Debug.LogError("Cannot load discovered entities: GameData is null");
            return;
        }

        Debug.Log($"Loading discovered entities - Ant: {data.hasDiscoveredAnt}, Fly: {data.hasDiscoveredFly}, Ladybug: {data.hasDiscoveredLadybug}");
        
        // Update entity definitions and dictionaries
        UpdateEntityDiscovery("flea", data.hasDiscoveredFlea);
        UpdateEntityDiscovery("larvae", data.hasDiscoveredLarvae);
        UpdateEntityDiscovery("ant", data.hasDiscoveredAnt);
        UpdateEntityDiscovery("fly", data.hasDiscoveredFly);
        UpdateEntityDiscovery("ladybug", data.hasDiscoveredLadybug);
        UpdateEntityDiscovery("mosquito", data.hasDiscoveredMosquito);
        UpdateEntityDiscovery("grasshopper", data.hasDiscoveredGrasshopper);
        UpdateEntityDiscovery("wasp", data.hasDiscoveredWasp);
        UpdateEntityDiscovery("wolfspider", data.hasDiscoveredWolfSpider);
        UpdateEntityDiscovery("beetle", data.hasDiscoveredBeetle);
        UpdateEntityDiscovery("stickinsect", data.hasDiscoveredStickInsect);
        UpdateEntityDiscovery("centipede", data.hasDiscoveredCentipede);
        UpdateEntityDiscovery("mantis", data.hasDiscoveredMantis);
        UpdateEntityDiscovery("tarantula", data.hasDiscoveredTarantula);
        UpdateEntityDiscovery("stagbeetle", data.hasDiscoveredStagBeetle);
        UpdateEntityDiscovery("scorpion", data.hasDiscoveredScorpion);
        
        // Refresh UI for all discovered entities
        RefreshDiscoveredEntitiesUI();
    }
    
    // Helper method to update entity discovery state
    private void UpdateEntityDiscovery(string entityType, bool isDiscovered)
    {
        // Normalize entity type to lowercase for consistency
        entityType = entityType.ToLower();
        
        // Update dictionary
        discoveredEntities[entityType] = isDiscovered;
        previousDiscoveredStates[entityType] = isDiscovered;
        
        // Update entity definition
        foreach (var definition in entityDefinitions)
        {
            if (definition.entityType.ToLower() == entityType)
            {
                definition.isDiscovered = isDiscovered;
                break;
            }
        }
        
        Debug.Log($"Updated discovery status for {entityType}: {isDiscovered}");
    }
    
    // Helper method to refresh UI for all discovered entities
    private void RefreshDiscoveredEntitiesUI()
    {
        // Clear existing UI containers
        foreach (var container in entityContainers.Values)
        {
            Destroy(container);
        }
        entityContainers.Clear();
        
        // Recreate UI for discovered entities
        int createdCount = 0;
        foreach (var definition in entityDefinitions)
        {
            if (definition.isDiscovered)
            {
                CreateEntityUI(definition);
                createdCount++;
                Debug.Log($"Created UI for discovered entity: {definition.entityType}");
            }
        }
        
        Debug.Log($"Refreshed discovered entities UI - Created {createdCount} entity UI elements");
    }
    
    // New method to handle entity purchases
    public void PurchaseEntity(string entityType)
    {
        Debug.Log($"Attempting to purchase entity: {entityType}");
        
        // Find the entity definition
        EntityDefinition definition = null;
        foreach (var def in entityDefinitions)
        {
            if (def.entityType == entityType)
            {
                definition = def;
                break;
            }
        }
        
        if (definition == null)
        {
            Debug.LogError($"Could not find definition for entity: {entityType}");
            return;
        }
        
        // Skip if already purchased
        if (definition.isBought)
        {
            Debug.Log($"Entity {entityType} is already purchased");
            return;
        }
        
        // Check if player has enough coins
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogError("Could not find PlayerInventory");
            return;
        }
        
        // Parse the coin cost from the definition
        int cost = definition.coinCost;
        if (playerInventory.CoinCount < cost)
        {
            // Not enough coins
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowInformText($"Not enough coins to purchase {definition.displayName}. Need {cost} coins.");
            }
            Debug.Log($"Not enough coins to purchase {entityType}. Have: {playerInventory.CoinCount}, Need: {cost}");
            return;
        }
        
        // Deduct coins
        if (playerInventory.RemoveCoins(cost))
        {
            // Mark as purchased
            definition.isBought = true;
            
            // Update UI
            UpdateEntityUI(definition);
            
            // Show the equip button for this entity
            if (entityContainers.ContainsKey(entityType))
            {
                GameObject entityContainer = entityContainers[entityType];
                Button equipButton = entityContainer.transform.Find("EquipButton")?.GetComponent<Button>();
                if (equipButton != null)
                {
                    equipButton.gameObject.SetActive(true);
                    
                    // Make sure the click listener is set up for equipping
                    string entityTypeForButton = definition.entityType;
                    equipButton.onClick.RemoveAllListeners();
                    equipButton.onClick.AddListener(() => EquipEntity(entityTypeForButton));
                }
            }
            
            // Show purchase message
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowInformText($"Successfully purchased {definition.displayName}!");
            }
            
            // Save purchase status
            SavePurchaseStatus(entityType, true);
            
            Debug.Log($"Successfully purchased entity {entityType} for {cost} coins");
            
            // Play purchase sound (if available)
            SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
            if (soundManager != null)
            {
                soundManager.PlaySound("Purchase", transform.position, false);
            }
        }
        else
        {
            Debug.LogError($"Failed to deduct coins for entity purchase: {entityType}");
        }
    }
    
    // Update SavePurchaseStatus method to handle null GameManager
    private void SavePurchaseStatus(string entityType, bool isPurchased)
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found - purchase status will not be saved");
            return;
        }

        // Set the entity purchase status
        SetEntityPurchased(entityType, isPurchased);
        
        try
        {
            // Save the game
            gameManager.SaveGame();
            Debug.Log($"Saved purchase status for {entityType}: {isPurchased}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving purchase status: {e.Message}\n{e.StackTrace}");
        }
    }
    
    // Add this method to set entity purchase status
    private void SetEntityPurchased(string entityType, bool isPurchased)
    {
        if (string.IsNullOrEmpty(entityType))
        {
            Debug.LogWarning("Cannot set purchase status: entityType is null or empty");
            return;
        }
        
        // Find the entity definition
        bool foundEntity = false;
        foreach (var def in entityDefinitions)
        {
            if (def != null && def.entityType.ToLower() == entityType.ToLower())
            {
                // Set the purchased status
                def.isBought = isPurchased;
                foundEntity = true;
                Debug.Log($"Set {entityType} purchased status to {isPurchased}");
                break;
            }
        }
        
        if (!foundEntity)
        {
            Debug.LogWarning($"Could not find entity definition for {entityType}");
                }
            }
        }
        
        // Make sure to update the UI after loading
        UpdateDiscoveredEntitiesUI();
    }

    // Modify the panel show method to update the UI
    public void ShowPanel()
    {
        if (panel != null)
        {
            // Update the UI before showing the panel
            UpdateDiscoveredEntitiesUI();
            
            panel.SetActive(true);
        }
    }
}

