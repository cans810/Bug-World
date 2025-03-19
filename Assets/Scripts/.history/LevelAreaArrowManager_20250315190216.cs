using System.Collections;
using UnityEngine;

public class LevelAreaArrowManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelAreaTarget
    {
        public int requiredLevel;
        public GameObject areaTarget;
        public string areaName = "new area";
        public bool hasBeenVisited = false;
    }

    [Header("Area Targets")]
    [SerializeField] private LevelAreaTarget[] levelAreas;
    
    [Header("Arrow Settings")]
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float arrowDuration = 10f; // How long to show the arrow after level up
    
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private UIHelper uiHelper;
    
    private ArrowIndicator arrowIndicator;
    private bool isArrowActive = false;
    private int previousLevel = 0;
    private GameObject currentTarget; // Track the current target
    
    private void Start()
    {
        // Find the player inventory if not assigned
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory == null)
            {
                Debug.LogError("LevelAreaArrowManager: PlayerInventory not found!");
                return;
            }
        }
        
        // Find UIHelper if not assigned
        if (uiHelper == null)
        {
            uiHelper = FindObjectOfType<UIHelper>();
        }
        
        // Add arrow indicator component
        arrowIndicator = gameObject.AddComponent<ArrowIndicator>();
        
        // Subscribe to level up event
        playerInventory.OnLevelUp += OnPlayerLevelUp;
        
        // Store starting level
        previousLevel = playerInventory.CurrentLevel;
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        // Check if there are any newly unlocked areas
        foreach (LevelAreaTarget area in levelAreas)
        {
            // If this area requires exactly the new level and hasn't been visited yet
            if (area.requiredLevel == newLevel && !area.hasBeenVisited)
            {
                // Show a message about unlocking new area
                if (uiHelper != null)
                {
                    uiHelper.ShowInformText("New Area Unlocked!");
                }
                
                // Show arrow to this area
                ShowArrowToArea(area);
                
                break; // Only show one arrow at a time
            }
        }
    }
    
    private void ShowArrowToArea(LevelAreaTarget area)
    {
        // Hide any existing arrow
        if (isArrowActive)
        {
            arrowIndicator.HideArrow();
        }
        
        // Set the new target
        arrowIndicator.SetTarget(area.areaTarget);
        currentTarget = area.areaTarget; // Store the current target reference
        
        // Show the arrow
        arrowIndicator.ShowArrow();
        isArrowActive = true;
        
        // We no longer call the coroutine to hide the arrow after delay
        // The arrow will stay visible until the player visits the area
    }
    
    // Called when player enters the area target
    public void MarkAreaAsVisited(string areaName)
    {
        foreach (LevelAreaTarget area in levelAreas)
        {
            if (area.areaName == areaName)
            {
                area.hasBeenVisited = true;
                
                // Hide arrow if it's pointing to this area
                if (isArrowActive && currentTarget == area.areaTarget)
                {
                    arrowIndicator.HideArrow();
                    isArrowActive = false;
                    currentTarget = null;
                }
                
                break;
            }
        }
    }
    
    // Method to check for newly available areas when player first enters the game
    public void CheckForNewlyAvailableAreas()
    {
        int currentLevel = playerInventory.CurrentLevel;
        
        // Check if there are any unlocked areas that haven't been visited
        foreach (LevelAreaTarget area in levelAreas)
        {
            if (area.requiredLevel <= currentLevel && !area.hasBeenVisited)
            {
                // Show a message about the available area
                if (uiHelper != null)
                {
                    uiHelper.ShowInformText("New Area Unlocked!");
                }
                
                // Show arrow to this area
                ShowArrowToArea(area);
                
                break; // Only show one arrow at a time
            }
        }
    }
    
    // This method detects when a player enters an area trigger
    private void OnTriggerEnter(Collider other)
    {
        // If the player enters the area, mark it as visited
        if (other.CompareTag("Player"))
        {
            // For each area, check if this is the trigger we're looking for
            foreach (LevelAreaTarget area in levelAreas)
            {
                // Check if this trigger is associated with the area target
                // Note: You should create trigger colliders at area entrances
                // and make sure they have the same GameObject reference as areaTarget
                if (area.areaTarget != null && 
                    area.areaTarget.GetInstanceID() == gameObject.GetInstanceID())
                {
                    // Mark this area as visited
                    area.hasBeenVisited = true;
                    
                    // Hide arrow if it's pointing to this area
                    if (isArrowActive && currentTarget == area.areaTarget)
                    {
                        arrowIndicator.HideArrow();
                        isArrowActive = false;
                        currentTarget = null;
                    }
                    
                    // Show a welcome message
                    if (uiHelper != null)
                    {
                        uiHelper.ShowInformText($"Welcome to {area.areaName}!");
                    }
                    
                    break;
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerInventory != null)
        {
            playerInventory.OnLevelUp -= OnPlayerLevelUp;
        }
    }
} 