using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;

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
    
    public delegate void AreaUnlockedDelegate(GameObject areaTarget, int level, string areaName);
    public event AreaUnlockedDelegate OnAreaUnlocked;
    
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
        
        // Check if area targets are properly set up
        Debug.Log("LevelAreaArrowManager - Checking area targets:");
        if (levelAreas != null && levelAreas.Length > 0)
        {
            foreach (LevelAreaTarget area in levelAreas)
            {
                if (area.areaTarget == null)
                {
                    Debug.LogError($"Area '{area.areaName}' has no target GameObject assigned!");
                }
                else
                {
                    Debug.Log($"Area: {area.areaName}, Target: {area.areaTarget.name}, Level: {area.requiredLevel}");
                }
            }
        }
        else
        {
            Debug.LogError("No level areas defined in the inspector!");
        }
        
        // Subscribe to camera animation completion
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null)
        {
            cameraAnimations.OnCameraAnimationCompleted += ShowArrowAfterAnimation;
        }
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
                
                // Don't show arrow right away - it will be triggered by animation completion
                // ShowArrowToArea(area); <- This line is removed/commented
                
                // Just trigger the area unlocked event (which will start the camera animation)
                if (OnAreaUnlocked != null && area.areaTarget != null)
                {
                    OnAreaUnlocked(area.areaTarget, newLevel, area.areaName);
                }
                
                break; // Only handle one area at a time
            }
        }
    }
    
    private void ShowArrowToArea(LevelAreaTarget area)
    {
        Debug.Log($"ShowArrowToArea called for {area.areaName}");
        
        // Make sure the arrow indicator component exists
        if (arrowIndicator == null)
        {
            Debug.LogError("Arrow indicator component is missing!");
            arrowIndicator = gameObject.AddComponent<ArrowIndicator>();
        }
        
        // Hide any existing arrow
        if (isArrowActive)
        {
            arrowIndicator.HideArrow();
            // Stop any existing coroutine
            StopAllCoroutines();
        }
        
        // Set the new target
        arrowIndicator.SetTarget(area.areaTarget);
        currentTarget = area.areaTarget; // Store the current target reference
        
        // Show the arrow
        arrowIndicator.ShowArrow();
        isArrowActive = true;
        Debug.Log($"Arrow is now pointing to {area.areaName}");
        
        // Hide the arrow after 10 seconds
        StartCoroutine(HideArrowAfterDelay(10f));
    }
    
    private IEnumerator HideArrowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Hide the arrow
        if (isArrowActive)
        {
            arrowIndicator.HideArrow();
            isArrowActive = false;
            currentTarget = null;
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
    
    // Instead, add a method to be called from PlayerController when a map border is entered
    public void CheckAreaEntry(string borderName)
    {
        // Debug when this method is called
        Debug.Log($"CheckAreaEntry called with border: {borderName}");
        Debug.Log($"Arrow active: {isArrowActive}, Current target: {(currentTarget != null ? currentTarget.name : "null")}");
        
        // If we don't have an active arrow, nothing to do
        if (!isArrowActive || currentTarget == null)
            return;
        
        // Find the area that corresponds to this border
        bool foundMatch = false;
        
        foreach (LevelAreaTarget area in levelAreas)
        {
            if (area.areaTarget == null) continue;
            
            // Debug each area we're checking
            Debug.Log($"Checking area: {area.areaName}, Target: {area.areaTarget.name}");
            
            // Check for the most common case - border name contains the level number
            bool isMatch = false;
            
            // Try to extract number from border name (e.g., "MapBorder2" -> "2")
            string borderNumber = Regex.Match(borderName, @"\d+").Value;
            string targetNumber = Regex.Match(area.areaTarget.name, @"\d+").Value;
            
            // If both have numbers, compare those
            if (!string.IsNullOrEmpty(borderNumber) && !string.IsNullOrEmpty(targetNumber))
            {
                isMatch = borderNumber == targetNumber;
                Debug.Log($"Comparing numbers: Border={borderNumber}, Target={targetNumber}, Match={isMatch}");
            }
            
            // Also check the original name comparisons
            bool exactMatch = area.areaTarget.name == borderName;
            bool partialMatch = borderName.Contains(area.areaName) || area.areaName.Contains(borderName);
            
            Debug.Log($"Name comparison: ExactMatch={exactMatch}, PartialMatch={partialMatch}");
            
            if (isMatch || exactMatch || partialMatch)
            {
                foundMatch = true;
                Debug.Log($"MATCH FOUND! Area {area.areaName} matches border {borderName}");
                
                // Mark this area as visited
                area.hasBeenVisited = true;
                
                // Hide arrow if it's pointing to this area
                arrowIndicator.HideArrow();
                isArrowActive = false;
                currentTarget = null;
                
                // Show a welcome message
                if (uiHelper != null)
                {
                    uiHelper.ShowInformText($"Welcome to {area.areaName}!");
                }
                
                break;
            }
        }
        
        if (!foundMatch)
        {
            Debug.Log($"No matching area found for border: {borderName}");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerInventory != null)
        {
            playerInventory.OnLevelUp -= OnPlayerLevelUp;
        }
        
        // Unsubscribe from camera animation
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null)
        {
            cameraAnimations.OnCameraAnimationCompleted -= ShowArrowAfterAnimation;
        }
    }
    
    // Add this method to properly handle the camera animation completion event
    private void ShowArrowAfterAnimation(GameObject areaTarget, int level, string areaName)
    {
        // Add debug logs to track execution
        Debug.Log($"ShowArrowAfterAnimation called for area: {areaName}, level: {level}");
        
        // Find the matching area and show the arrow
        foreach (LevelAreaTarget area in levelAreas)
        {
            if (area.areaTarget == areaTarget && area.requiredLevel == level)
            {
                Debug.Log($"Found matching area: {area.areaName}, showing arrow");
                ShowArrowToArea(area);
                return;
            }
        }
        
        // If we get here, we didn't find a matching area
        Debug.LogWarning($"No matching area found for target: {areaTarget.name}, level: {level}");
    }
} 