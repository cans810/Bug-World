using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiscoveredEntitiesController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject container;
    public GameObject entityContainerPrefab;
    
    [Header("Entity Definitions")]
    [SerializeField] private EntityDefinition[] entityDefinitions;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerController playerController;
    
    // Keep track of discovered entities
    private Dictionary<string, bool> discoveredEntities = new Dictionary<string, bool>();
    // Keep track of entity containers that have been instantiated
    private Dictionary<string, GameObject> entityContainers = new Dictionary<string, GameObject>();
    
    // Singleton instance
    public static DiscoveredEntitiesController Instance { get; private set; }
    
    [System.Serializable]
    public class EntityDefinition
    {
        public string entityType;
        public string displayName;
        public Sprite entityIcon;
        public string description;
        public bool isDiscovered;
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
        
        // Initialize the container to be inactive until needed
        if (container != null)
            container.SetActive(false);
            
        // Set up initial discovered state from definitions
        foreach (var definition in entityDefinitions)
        {
            discoveredEntities[definition.entityType] = definition.isDiscovered;
        }
    }
    
    private void Start()
    {
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
    }
    
    // Method to check and discover an entity by name
    public void CheckAndDiscoverEntity(string entityName)
    {
        Debug.Log($"Checking entity: {entityName}");
        
        // Check against known entity types
        foreach (var definition in entityDefinitions)
        {
            if (entityName.ToLower().Contains(definition.entityType.ToLower()))
            {
                DiscoverEntity(definition.entityType);
                return;
            }
        }
        
        Debug.Log($"Entity {entityName} does not match any known types");
    }
    
    // Method to mark an entity as discovered and update UI
    public void DiscoverEntity(string entityType)
    {
        // Check if this entity is already discovered
        if (discoveredEntities.ContainsKey(entityType) && discoveredEntities[entityType])
        {
            Debug.Log($"Entity {entityType} already discovered");
            return;
        }
        
        Debug.Log($"Discovering entity: {entityType}");
        
        // Find the corresponding definition
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
            Debug.LogWarning($"No definition found for entity type: {entityType}");
            return;
        }
        
        // Mark as discovered
        discoveredEntities[entityType] = true;
        definition.isDiscovered = true;
        
        // Create UI element for this entity
        CreateEntityUI(definition);
        
        // Show discovery notification
        UIHelper uiHelper = FindObjectOfType<UIHelper>();
        if (uiHelper != null)
        {
            uiHelper.ShowInformText($"New creature discovered: {definition.displayName}!");
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
        
        // Set up the UI elements
        TextMeshProUGUI nameText = entityContainer.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = entityContainer.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        Image iconImage = entityContainer.transform.Find("EntityIcon")?.GetComponent<Image>();
        
        if (nameText != null)
            nameText.text = definition.displayName;
            
        if (descriptionText != null)
            descriptionText.text = definition.description;
            
        if (iconImage != null && definition.entityIcon != null)
            iconImage.sprite = definition.entityIcon;
    }
    
    // Update an existing entity UI element
    private void UpdateEntityUI(EntityDefinition definition)
    {
        if (!entityContainers.ContainsKey(definition.entityType))
            return;
            
        GameObject entityContainer = entityContainers[definition.entityType];
        
        // Update the UI elements
        TextMeshProUGUI nameText = entityContainer.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI descriptionText = entityContainer.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        Image iconImage = entityContainer.transform.Find("EntityIcon")?.GetComponent<Image>();
        
        if (nameText != null)
            nameText.text = definition.displayName;
            
        if (descriptionText != null)
            descriptionText.text = definition.description;
            
        if (iconImage != null && definition.entityIcon != null)
            iconImage.sprite = definition.entityIcon;
    }
    
    // Show the catalog UI
    public void ShowEntityCatalog()
    {
        if (container != null)
            container.SetActive(true);
    }
    
    // Hide the catalog UI
    public void HideEntityCatalog()
    {
        if (container != null)
            container.SetActive(false);
    }
    
    // Toggle the catalog visibility
    public void ToggleEntityCatalog()
    {
        if (container != null)
            container.SetActive(!container.activeSelf);
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
}
