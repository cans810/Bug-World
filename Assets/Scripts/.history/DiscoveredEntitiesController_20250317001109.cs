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
        
        // Make sure the panel is initialized but inactive
        if (panel != null)
        {
            panel.SetActive(false);
        }
        
        // Make sure the container exists and is active (it will be inside the inactive panel)
        if (container != null)
        {
            container.SetActive(true);
        }
        
        // Clear any existing UI elements that might have been created in the editor
        ClearAllEntityContainers();
        
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
    
    // Add this method to clear all existing entity containers
    private void ClearAllEntityContainers()
    {
        if (container == null) return;
        
        Debug.Log("Clearing all existing entity containers");
        
        // Clear dictionary
        entityContainers.Clear();
        
        // Destroy all children of the container
        foreach (Transform child in container.transform)
        {
            Destroy(child.gameObject);
        }
    }
    
    // Add this method to force refresh the entity UI display
    public void ForceRefreshEntityUI()
    {
        Debug.Log("Force refreshing entity UI");
        
        // Clear existing UI
        ClearAllEntityContainers();
        
        // Recreate UI for discovered entities
        foreach (var definition in entityDefinitions)
        {
            if (definition.isDiscovered)
            {
                Debug.Log($"Recreating UI for discovered entity: {definition.displayName}");
                CreateEntityUI(definition);
            }
        }
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
        if (container == null)
        {
            Debug.LogError("Container not assigned for entity UI creation!");
            return;
        }
        
        if (entityContainerPrefab == null)
        {
            Debug.LogError("Entity container prefab not assigned!");
            return;
        }
        
        // Check if we already have a container for this entity
        if (entityContainers.ContainsKey(definition.entityType))
        {
            Debug.Log($"UI for {definition.displayName} already exists, updating instead of creating");
            UpdateEntityUI(definition);
            return;
        }
        
        Debug.Log($"Creating UI for entity: {definition.displayName}");
        
        // Make sure the container is active
        if (!container.activeInHierarchy)
        {
            // The container might be inside an inactive panel, so we'll just make sure it's active locally
            container.SetActive(true);
            Debug.Log("Activated container for entity UI creation");
        }
        
        // Instantiate new entity container
        GameObject entityContainer = Instantiate(entityContainerPrefab, container.transform);
        
        if (entityContainer == null)
        {
            Debug.LogError($"Failed to instantiate entity container for {definition.displayName}");
            return;
        }
        
        // Store reference
        entityContainers[definition.entityType] = entityContainer;
        
        // Set up the UI elements with correct names based on your prefab
        TextMeshProUGUI nameText = entityContainer.transform.Find("EntityName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = entityContainer.transform.Find("EntityDesc")?.GetComponent<TextMeshProUGUI>();
        Image iconImage = entityContainer.transform.Find("EntityIcon")?.GetComponent<Image>();
        TextMeshProUGUI priceText = entityContainer.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
        
        if (nameText != null)
        {
            nameText.text = definition.displayName;
            Debug.Log($"Set name text for {definition.displayName}");
        }
        else
        {
            Debug.LogWarning($"Could not find EntityName component for {definition.displayName}");
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = definition.description;
        }
        else
        {
            Debug.LogWarning($"Could not find EntityDesc component for {definition.displayName}");
        }
        
        if (iconImage != null && definition.entityIcon != null)
        {
            iconImage.sprite = definition.entityIcon;
        }
        else
        {
            Debug.LogWarning($"Could not set icon for {definition.displayName}. IconImage: {iconImage != null}, IconSprite: {definition.entityIcon != null}");
        }
        
        if (priceText != null)
        {
            priceText.text = definition.isBought ? "Owned" : definition.price;
            Debug.Log($"Set price text for {definition.displayName} to: {priceText.text}");
        }
        else
        {
            Debug.LogWarning($"Price text component not found for {definition.displayName}");
        }
        
        // Ensure the entity container itself is active
        entityContainer.SetActive(true);
        
        Debug.Log($"Successfully created UI for {definition.displayName}");
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
            Debug.LogError("GameManager not found for saving entity discovery!");
            return;
        }
        
        Debug.Log($"Saving discovery of entity: {entityType}");
        
        // Update game data based on entity type
        switch (entityType.ToLower())
        {
            case "ant":
                gameManager.SetEntityDiscovered("ant", true);
                Debug.Log("Set ant as discovered in GameManager");
                break;
            case "fly":
                gameManager.SetEntityDiscovered("fly", true);
                Debug.Log("Set fly as discovered in GameManager");
                break;
            case "ladybug":
                gameManager.SetEntityDiscovered("ladybug", true);
                Debug.Log("Set ladybug as discovered in GameManager");
                break;
            default:
                Debug.LogWarning($"Unknown entity type for saving: {entityType}");
                break;
        }
        
        // Trigger a save to make sure it's persisted
        gameManager.SaveGame();
        Debug.Log($"Saved game after discovering {entityType}");
    }
    
    // Load discovered entities from game data
    public void LoadDiscoveredEntities(GameData gameData)
    {
        if (gameData == null)
        {
            Debug.LogError("Attempted to load discovered entities with null GameData!");
            return;
        }
        
        Debug.Log("Loading discovered entities from GameData");
        Debug.Log($"GameData entity status: Ant={gameData.hasDiscoveredAnt}, Fly={gameData.hasDiscoveredFly}, Ladybug={gameData.hasDiscoveredLadybug}");
        
        // Update the discovery status for each entity type
        UpdateDiscoveryStatus("ant", gameData.hasDiscoveredAnt);
        UpdateDiscoveryStatus("fly", gameData.hasDiscoveredFly);
        UpdateDiscoveryStatus("ladybug", gameData.hasDiscoveredLadybug);
        
        // Force a refresh of the UI after loading
        ForceRefreshEntityUI();
        
        Debug.Log("Finished loading discovered entities data");
    }
    
    // Update discovery status and UI for an entity
    private void UpdateDiscoveryStatus(string entityType, bool isDiscovered)
    {
        Debug.Log($"Updating discovery status for {entityType}: {isDiscovered}");
        
        // Find matching definition
        bool foundMatch = false;
        
        foreach (var definition in entityDefinitions)
        {
            if (string.Equals(definition.entityType, entityType, System.StringComparison.OrdinalIgnoreCase))
            {
                foundMatch = true;
                Debug.Log($"Found matching definition for {entityType}. Current isDiscovered={definition.isDiscovered}, New isDiscovered={isDiscovered}");
                
                // If newly discovered, create UI
                if (isDiscovered && !definition.isDiscovered)
                {
                    Debug.Log($"Entity {entityType} is newly discovered, creating UI");
                    definition.isDiscovered = true;
                    discoveredEntities[definition.entityType] = true;
                    CreateEntityUI(definition);
                }
                // Update existing discovery status
                else
                {
                    Debug.Log($"Updating discovery status for {entityType} to {isDiscovered}");
                    definition.isDiscovered = isDiscovered;
                    discoveredEntities[definition.entityType] = isDiscovered;
                    
                    // If discovered but UI doesn't exist, create it
                    if (isDiscovered && !entityContainers.ContainsKey(definition.entityType))
                    {
                        Debug.Log($"Entity {entityType} is discovered but has no UI, creating it now");
                        CreateEntityUI(definition);
                    }
                    // If not discovered but UI exists, remove it
                    else if (!isDiscovered && entityContainers.ContainsKey(definition.entityType))
                    {
                        Debug.Log($"Entity {entityType} is not discovered but has UI, removing it");
                        Destroy(entityContainers[definition.entityType]);
                        entityContainers.Remove(definition.entityType);
                    }
                }
                
                // Also update the previous discovered state
                previousDiscoveredStates[definition.entityType] = isDiscovered;
                break;
            }
        }
        
        if (!foundMatch)
        {
            Debug.LogWarning($"No entity definition found for type: {entityType}");
        }
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
