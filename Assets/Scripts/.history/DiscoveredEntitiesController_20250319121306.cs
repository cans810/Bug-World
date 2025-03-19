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
    
    // Save purchase status to game data
    private void SavePurchaseStatus(string entityType, bool isPurchased)
    {
        // This would need to be added to GameData and GameManager
        // For now, we'll just save the game to persist any purchases
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SaveGame();
        }
    }
    
    // Add a new method to handle equipping entities
    public void EquipEntity(string entityType)
    {
        Debug.Log($"Equipping entity: {entityType}");
        
        // If this entity is already equipped, just return
        if (currentlyEquippedEntity == entityType)
        {
            Debug.Log($"Entity {entityType} is already equipped");
            return;
        }
        
        // Find the entity definition
        EntityDefinition definition = null;
        foreach (var def in entityDefinitions)
        {
            if (def.entityType.ToLower() == entityType.ToLower())
            {
                definition = def;
                break;
            }
        }
        
        if (definition == null)
        {
            Debug.LogError($"Could not find entity definition for {entityType}");
            return;
        }
        
        // Update the equipped entity
        currentlyEquippedEntity = entityType;
        
        // Find UI helper to create crumb counter
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper == null || uiHelper.selectedEntityCrumbCollectionCounterPrefab == null)
        {
            Debug.LogError("UIHelper or crumb counter prefab not found");
            return;
        }
        
        // Destroy any existing crumb counter
        if (crumbCounter != null)
        {
            crumbCounter.OnCrumbCollectionComplete -= OnCrumbCollectionComplete;
            Destroy(crumbCounter.gameObject);
        }
        
        // Create new counter
        GameObject counterObj = Instantiate(uiHelper.selectedEntityCrumbCollectionCounterPrefab, uiHelper.SafeArea.transform);
        crumbCounter = counterObj.GetComponent<SelectedEntityCrumbCollectionCounter>();
        
        if (crumbCounter != null)
        {
            // Initialize the counter with entity data AND icon
            crumbCounter.Initialize(definition.entityType, definition.crumbCost, definition.entityIcon);
            
            // Subscribe to the completion event
            crumbCounter.OnCrumbCollectionComplete += OnCrumbCollectionComplete;
            
            Debug.Log($"Created crumb collection counter for {definition.displayName} with {definition.crumbCost} required crumbs");
        }
        else
        {
            Debug.LogError("Failed to get SelectedEntityCrumbCollectionCounter component");
        }
    }
    
    // Add this method to handle completed crumb collections
    private void OnCrumbCollectionComplete(string entityType)
    {
        Debug.Log($"Crumb collection complete for entity: {entityType}");
        
        // Make sure we have the ant incubator reference
        if (insectIncubator == null)
        {
            insectIncubator = GameObject.Find("AllyIncubator")?.GetComponent<InsectIncubator>();
            if (insectIncubator == null)
            {
                Debug.LogError("Cannot create egg: InsectIncubator not found!");
                return;
            }
        }
        
        // Find the UIHelper once at the beginning of the method
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        
        // Check if the incubator is full or an egg already exists
        if (insectIncubator.IsIncubatorFull())
        {
            if (uiHelper != null)
            {
                uiHelper.ShowInformText(insectIncubator.GetIncubatorFullMessage());
            }
            return;
        }
        
        // Create the egg using the incubator, passing the entity type
        insectIncubator.CreateInsectEgg(entityType);
        
        // Play a success sound
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("Success", transform.position, false);
        }
        
        // Show a message to the player - use proper capitalization for display
        if (uiHelper != null)
        {
            string displayName = char.ToUpper(entityType[0]) + entityType.Substring(1);
            uiHelper.ShowInformText($"Successfully collected all crumbs! {displayName} egg created.");
        }
        
        // Unequip the entity to reset the crumb counter
        UnequipCurrentEntity();
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

    // Add these methods to DiscoveredEntitiesController to support crumb collection for equipped entities

    // Method to get the currently equipped entity
    public string GetEquippedEntity()
    {
        return currentlyEquippedEntity;
    }

    // Method to update crumb count for the equipped entity
    public void UpdateEquippedEntityCrumbCount(int crumbCount)
    {
        // Make sure we have an equipped entity and a crumb counter
        if (string.IsNullOrEmpty(currentlyEquippedEntity) || crumbCounter == null)
        {
            Debug.LogWarning("Cannot update crumb count: No entity equipped or counter not initialized");
            return;
        }
        
        Debug.Log($"Updating crumb count for {currentlyEquippedEntity} by {crumbCount}");
        
        // Get the entity definition to check if requirements are met
        EntityDefinition definition = null;
        foreach (var def in entityDefinitions)
        {
            if (def.entityType == currentlyEquippedEntity)
            {
                definition = def;
                break;
            }
        }
        
        if (definition == null)
        {
            Debug.LogError($"Could not find definition for equipped entity: {currentlyEquippedEntity}");
            return;
        }
        
        // Update the crumb counter
        crumbCounter.UpdateCrumbCount(crumbCounter.CurrentCrumbs + crumbCount);
        
        // Check if we've collected enough crumbs to complete the collection
        if (crumbCounter.CurrentCrumbs >= definition.crumbCost)
        {
            // We've completed the collection! Trigger appropriate actions
            
            // Show a notification
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowInformText($"Successfully collected all crumbs for {definition.displayName}!");
            }
            
            // Play success sound
            SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
            if (soundManager != null)
            {
                soundManager.PlaySound("Success", transform.position, false);
            }
            
            // Reset the crumb counter
            // Note: The counter should fade out automatically since it detected the completion
        }
    }

    // Method to get the current crumb counter
    public SelectedEntityCrumbCollectionCounter GetCrumbCounter()
    {
        return crumbCounter;
    }

    // Add this method to save discovered entities to GameData
    public void SaveDiscoveredEntities(GameData data)
    {
        if (data == null)
        {
            Debug.LogError("Cannot save discovered entities: GameData is null");
            return;
        }

        
        // Save other entities...
        data.hasDiscoveredAnt = IsEntityDiscovered("Ant");
        data.hasDiscoveredFly = IsEntityDiscovered("Fly");
        data.hasDiscoveredLadybug = IsEntityDiscovered("Ladybug");
        data.hasDiscoveredFlea = IsEntityDiscovered("Flea");
        data.hasDiscoveredLarvae = IsEntityDiscovered("Larvae");
        data.hasDiscoveredMosquito = IsEntityDiscovered("Mosquito");
        data.hasDiscoveredGrasshopper = IsEntityDiscovered("Grasshopper");
        data.hasDiscoveredWasp = IsEntityDiscovered("Wasp");
        data.hasDiscoveredWolfSpider = IsEntityDiscovered("WolfSpider");
        data.hasDiscoveredBeetle = IsEntityDiscovered("Beetle");
        data.hasDiscoveredStickInsect = IsEntityDiscovered("StickInsect");
        data.hasDiscoveredCentipede = IsEntityDiscovered("Centipede");
        data.hasDiscoveredMantis = IsEntityDiscovered("Mantis");
        data.hasDiscoveredTarantula = IsEntityDiscovered("Tarantula");
        data.hasDiscoveredStagBeetle = IsEntityDiscovered("StagBeetle");
        data.hasDiscoveredScorpion = IsEntityDiscovered("Scorpion");
        
        
        Debug.Log("Saved discovered entities state to GameData");
    }

    // Method to get discovered state for an entity
    public bool IsEntityDiscovered(string entityType)
    {
        // First check our dictionary
        if (discoveredEntities.ContainsKey(entityType.ToLower()))
        {
            return discoveredEntities[entityType.ToLower()];
        }
        
        // If not in dictionary, check entity definitions
        foreach (var definition in entityDefinitions)
        {
            if (definition.entityType.ToLower() == entityType.ToLower())
            {
                return definition.isDiscovered;
            }
        }
        
        // If not found at all, return false
        return false;
    }

    // Add a public method to force refresh the UI
    public void ForceRefreshDiscoveredEntitiesUI()
    {
        Debug.Log("Force refreshing discovered entities UI");
        RefreshDiscoveredEntitiesUI();
    }

    // Add this method specifically for kill-based discovery
    public void DiscoverEntityOnKill(string entityType)
    {
        Debug.Log($"Attempting to discover entity through kill: {entityType}");
        
        // Check if valid entity type
        if (string.IsNullOrEmpty(entityType))
        {
            Debug.LogWarning("Empty entity type provided for discovery");
            return;
        }
        
        // Find matching entity definition
        EntityDefinition definition = null;
        foreach (var def in entityDefinitions)
        {
            if (def.entityType.ToLower() == entityType.ToLower())
            {
                definition = def;
                break;
            }
        }
        
        // If not found, try more aggressive matching
        if (definition == null)
        {
            foreach (var def in entityDefinitions)
            {
                if (def.entityType.ToLower().Contains(entityType.ToLower()) || 
                    entityType.ToLower().Contains(def.entityType.ToLower()))
                {
                    definition = def;
                    break;
                }
            }
        }
        
        // If still not found, log error and exit
        if (definition == null)
        {
            Debug.LogError($"Could not find entity definition for: {entityType}");
            return;
        }
        
        // If already discovered, exit
        if (definition.isDiscovered)
        {
            Debug.Log($"Entity {definition.displayName} already discovered");
            return;
        }
        
        // Mark as discovered
        definition.isDiscovered = true;
        discoveredEntities[definition.entityType] = true;
        
        // Show discovery notification panel with this entity
        ShowNewEntityDiscoveredPanel(definition);
        
        // Award XP for discovery and show visual effects
        if (definition.discoveryXPReward > 0)
        {
            PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
            if (inventory != null)
            {
                // Find the position for the XP effect (use player position)
                Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (playerTransform != null)
                {
                    // Get the visual effect manager
                    VisualEffectManager visualEffectManager = FindObjectOfType<VisualEffectManager>();
                    if (visualEffectManager != null)
                    {
                        // Play the XP gain effect from the player position
                        Debug.Log($"Playing XP gain effect for discovery reward: {definition.discoveryXPReward}");
                        visualEffectManager.PlayXPGainEffect(playerTransform.position, definition.discoveryXPReward);
                    }
                    else
                    {
                        Debug.LogWarning("VisualEffectManager not found for XP animation");
                    }
                }
                
                // Add the experience (after showing the visual effect)
                inventory.AddExperience(definition.discoveryXPReward);
                Debug.Log($"Awarded {definition.discoveryXPReward} XP for discovering {definition.displayName}");
            }
        }
        
        // Create UI for the new discovery
        CreateEntityUI(definition);
        
        // Notify the player
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            uiHelper.ShowInformText($"New Discovery: {definition.displayName}!");
        }
        
        // Play a sound effect for discovery
        SoundEffectManager soundManager = FindObjectOfType<SoundEffectManager>();
        if (soundManager != null)
        {
            soundManager.PlaySound("Discovery", transform.position, false);
        }
        
        // Save the discovery
        SaveDiscovery(definition.entityType);
    }

    // Add this method to update discovered entities UI
    public void UpdateDiscoveredEntitiesUI()
    {
        // This should refresh the UI to show all discovered entities
        // Make sure to call this after loading data
        
        Debug.Log("Refreshing Discovered Entities UI panel");
        
        // Refresh the panel if it's currently visible
        if (panel != null && panel.activeInHierarchy)
        {
            // First hide the panel
            panel.SetActive(false);
            
            // Then show it again to refresh
            panel.SetActive(true);
        }
    }

    // Add this method to fix and force discovery of specific entities
    public void ForceDiscoverEntity(string entityType)
    {
        Debug.Log($"FORCE DISCOVERING ENTITY: {entityType}");
        
        // Make sure entity type is normalized (all lowercase for comparison)
        string normalizedType = entityType.ToLower().Trim();
        
        // Find the entity definition
        EntityDefinition entityDef = null;
        foreach (var def in entityDefinitions)
        {
            // Try different match options since Flea might have different capitalization or spelling
            if (def.entityType.ToLower() == normalizedType || 
                def.entityType.ToLower().Contains(normalizedType) || 
                normalizedType.Contains(def.entityType.ToLower()))
            {
                entityDef = def;
                Debug.Log($"Found matching entity definition: {def.entityType}");
                break;
            }
        }
        
        if (entityDef != null)
        {
            // Check if already discovered - IMPORTANT NEW CHECK
            if (entityDef.isDiscovered)
            {
                Debug.Log($"Entity {entityDef.displayName} is already discovered, not showing notification");
                return;
            }
            
            // Mark as discovered
            entityDef.isDiscovered = true;
            discoveredEntities[entityDef.entityType.ToLower()] = true;
            
            // Show the discovery notification
            ShowNewEntityDiscoveredPanel(entityDef);
            
            // Refresh UI
            UpdateDiscoveredEntitiesUI();
            
            Debug.Log($"Entity {entityDef.displayName} is now discovered. UI updated.");
        }
        else
        {
            Debug.LogError($"Could not find entity definition for: {entityType}");
        }
    }

    // Update this method to use animations
    private void ShowNewEntityDiscoveredPanel(EntityDefinition entity)
    {
        if (newEntityDiscoveredPanel == null)
        {
            Debug.LogWarning("New entity discovered panel is not assigned!");
            return;
        }
        
        // Find the EntityIcon child and set its image
        Transform iconTransform = newEntityDiscoveredPanel.transform.Find("EntityIcon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null && entity.entityIcon != null)
            {
                // Set the icon to the discovered entity's sprite
                iconImage.sprite = entity.entityIcon;
                Debug.Log($"Set discovered entity icon to {entity.displayName}");
            }
            else
            {
                Debug.LogWarning("EntityIcon Image component or entity sprite is missing");
            }
        }
        else
        {
            Debug.LogWarning("EntityIcon child not found in newEntityDiscoveredPanel");
        }
        
        // Set text for entity name if there's a text component
        TextMeshProUGUI nameText = newEntityDiscoveredPanel.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = $"New Discovery: {entity.displayName}";
        }
        
        // Make sure the panel is active
        newEntityDiscoveredPanel.SetActive(true);
        
        // Get the animator component
        Animator animator = newEntityDiscoveredPanel.GetComponent<Animator>();
        if (animator != null)
        {
            // Trigger show animation
            animator.SetBool("NotiShowUp", true);
            animator.SetBool("NotiHide", false);
            Debug.Log("Playing NotiShowUp animation");
        }
        else
        {
            Debug.LogWarning("Animator component not found on newEntityDiscoveredPanel");
        }
        
        // Hide after 3 seconds (with animation)
        StartCoroutine(HideDiscoveryPanelAfterDelay(3f));
    }

    // Update the hide coroutine to use animations
    private IEnumerator HideDiscoveryPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (newEntityDiscoveredPanel != null)
        {
            // Get the animator
            Animator animator = newEntityDiscoveredPanel.GetComponent<Animator>();
            if (animator != null)
            {
                // Trigger hide animation
                animator.SetBool("NotiShowUp", false);
                animator.SetBool("NotiHide", true);
                Debug.Log("Playing NotiHide animation");
                
                // Wait for animation to finish before deactivating
                // Assuming animation takes about 1 second
                yield return new WaitForSeconds(1f);
            }
            
            // Deactivate the panel after animation completes
            newEntityDiscoveredPanel.SetActive(false);
            Debug.Log("Hid new entity discovered panel after delay");
        }
    }

    // Helper method to unequip the current entity
    private void UnequipCurrentEntity()
    {
        if (string.IsNullOrEmpty(currentlyEquippedEntity))
            return;
        
        Debug.Log($"Unequipping entity: {currentlyEquippedEntity}");
        
        // If we have a crumb counter, unsubscribe and destroy it
        if (crumbCounter != null)
        {
            crumbCounter.OnCrumbCollectionComplete -= OnCrumbCollectionComplete;
            Destroy(crumbCounter.gameObject);
            crumbCounter = null;
        }
        
        // Reset the currently equipped entity
        currentlyEquippedEntity = "";
    }
}

