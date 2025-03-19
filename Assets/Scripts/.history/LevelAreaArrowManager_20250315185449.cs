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
        
        // Set initial arrow color
        arrowIndicator.SetArrowColor(arrowColor);
        
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
                    uiHelper.ShowInformText($"Level {newLevel} reached! New area '{area.areaName}' unlocked!");
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
        
        // Show the arrow
        arrowIndicator.ShowArrow();
        isArrowActive = true;
        
        // Hide the arrow after duration
        StartCoroutine(HideArrowAfterDelay(arrowDuration, area));
    }
    
    private IEnumerator HideArrowAfterDelay(float delay, LevelAreaTarget area)
    {
        yield return new WaitForSeconds(delay);
        
        // Hide the arrow
        if (isArrowActive)
        {
            arrowIndicator.HideArrow();
            isArrowActive = false;
        }
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
                if (isArrowActive && arrowIndicator.GetTarget() == area.areaTarget)
                {
                    arrowIndicator.HideArrow();
                    isArrowActive = false;
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
                    uiHelper.ShowInformText($"Area '{area.areaName}' is available to explore!");
                }
                
                // Show arrow to this area
                ShowArrowToArea(area);
                
                break; // Only show one arrow at a time
            }
        }
    }
    
    // Add this to detect if a player enters an area trigger
    private void OnTriggerEnter(Collider other)
    {
        // If the player enters the area, mark it as visited
        if (other.CompareTag("Player"))
        {
            foreach (LevelAreaTarget area in levelAreas)
            {
                if (area.areaTarget != null && 
                    area.areaTarget.GetInstanceID() == other.gameObject.GetInstanceID())
                {
                    MarkAreaAsVisited(area.areaName);
                    
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