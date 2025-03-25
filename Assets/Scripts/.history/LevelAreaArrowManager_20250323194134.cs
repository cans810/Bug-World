using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.Events;

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
    
    private bool isArrowActive = false;
    private int previousLevel = 0;
    private GameObject currentTarget; // Track the current target
    private PlayerFollowingArrowIndicator arrowIndicator; // Changed to use the new arrow type
    
    [System.Serializable]
    public class AreaUnlockedEvent : UnityEvent<GameObject, int, string> { }

    public AreaUnlockedEvent onAreaUnlockedEvent = new AreaUnlockedEvent();
    
    [SerializeField] private CameraAnimations cameraAnimationsRef;
    
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
        
        // Create the arrow indicator component - using PlayerFollowingArrowIndicator
        arrowIndicator = gameObject.AddComponent<PlayerFollowingArrowIndicator>();
        arrowIndicator.SetArrowColor(arrowColor);
        
        // Subscribe to level up event
        playerInventory.OnLevelUp += OnPlayerLevelUp;
        
        // Store starting level
        previousLevel = playerInventory.CurrentLevel;
        
        // Check if area targets are properly set up
        Debug.Log("LevelAreaArrowManager - Checking area targets:");
        foreach (var area in levelAreas)
        {
            if (area.areaTarget != null)
            {
                Debug.Log($"Area: {area.areaName}, Level: {area.requiredLevel}, Target: {area.areaTarget.name}");
            }
            else
            {
                Debug.LogWarning($"Area: {area.areaName}, Level: {area.requiredLevel} - No target assigned!");
            }
        }
        
        // Subscribe to camera animation completion
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null)
        {
            // Subscribe to the delegate event
            cameraAnimations.OnCameraAnimationCompleted += ShowArrowAfterAnimation;
            
            // Also set up the UnityEvent
            onAreaUnlockedEvent.AddListener(cameraAnimations.OnAreaUnlocked);
            
            Debug.Log("Subscribed to camera animation completion event and set up UnityEvent");
        }
        else
        {
            Debug.LogError("CameraAnimations component not found!");
        }
    }
    
    public void OnPlayerLevelUp(int newLevel)
    {
        Debug.Log($"LevelAreaArrowManager: OnPlayerLevelUp called for level {newLevel}");
        
        // Check if there are any newly unlocked areas
        bool foundAreaToUnlock = false;
        LevelAreaTarget areaToUnlock = null;
        
        foreach (LevelAreaTarget area in levelAreas)
        {
            Debug.Log($"Checking area: {area.areaName}, required level: {area.requiredLevel}, visited: {area.hasBeenVisited}");
            
            // If this area requires exactly the new level and hasn't been visited yet
            if (area.requiredLevel == newLevel && !area.hasBeenVisited)
            {
                foundAreaToUnlock = true;
                areaToUnlock = area;
                Debug.Log($"Found area to unlock: {area.areaName}");
                break; // Only handle one area at a time
            }
        }
        
        // Only show message and trigger events if we actually found an area to unlock
        if (foundAreaToUnlock && areaToUnlock != null && areaToUnlock.areaTarget != null)
        {
            Debug.Log($"Starting camera animation sequence for {areaToUnlock.areaName}");
            
            // IMPORTANT: Notify AttributeDisplay that animation is starting
            AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
            if (attributeDisplay != null)
            {
                attributeDisplay.NotifyCameraAnimationStarted();
            }
            
            // Show the message because we're about to animate
            if (uiHelper != null)
            {
                uiHelper.ShowInformText("New Area Unlocked!");
            }
            
            // IMPORTANT: Only use ONE method to trigger the animation!
            // Choose the most reliable method based on your setup
            
            // OPTION 1: Direct method call
            CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
            if (cameraAnimations != null)
            {
                Debug.Log($"Directly calling CameraAnimations.OnAreaUnlocked for {areaToUnlock.areaName}");
                cameraAnimations.OnAreaUnlocked(areaToUnlock.areaTarget, newLevel, areaToUnlock.areaName);
                
                // Mark this area as visited immediately to prevent repeat animations
                areaToUnlock.hasBeenVisited = true;
            }
            else
            {
                Debug.LogWarning($"Found area to unlock ({areaToUnlock.areaName}) but CameraAnimations not found!");
            }
            
            // IMPORTANT: DO NOT use multiple animation triggers!
            // Comment out or remove these additional triggers:
            
            /* REMOVED: Using multiple triggers causes repeat animations
            // Invoke the delegate event
            if (OnAreaUnlocked != null)
            {
                Debug.Log($"Firing OnAreaUnlocked event for {areaToUnlock.areaName}");
                OnAreaUnlocked(areaToUnlock.areaTarget, newLevel, areaToUnlock.areaName);
            }
            
            // Also invoke the Unity Event
            if (onAreaUnlockedEvent != null)
            {
                Debug.Log($"Invoking onAreaUnlockedEvent for {areaToUnlock.areaName}");
                onAreaUnlockedEvent.Invoke(areaToUnlock.areaTarget, newLevel, areaToUnlock.areaName);
            }
            */
        }
        else
        {
            Debug.Log($"No areas to unlock at level {newLevel}");
        }
    }
    
    private void ShowArrowToArea(LevelAreaTarget area)
    {
        if (area.areaTarget == null)
        {
            Debug.LogError($"Cannot show arrow to null area target: {area.areaName}");
            return;
        }
        
        // Set current target
        currentTarget = area.areaTarget;
        
        // Update arrow target
        arrowIndicator.SetTarget(currentTarget.transform);
        
        // Show the arrow
        arrowIndicator.ShowArrow();
        isArrowActive = true;
        
        // Start timer to hide arrow
        StartCoroutine(HideArrowAfterDelay(arrowDuration));
        
        // Show a message
        if (uiHelper != null)
        {
            uiHelper.ShowInformText($"New area unlocked: {area.areaName}!");
        }
        
        Debug.Log($"Showing arrow to area: {area.areaName}");
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
        bool foundArea = false;
        LevelAreaTarget areaToShow = null;
        
        // Check if there are any unlocked areas that haven't been visited
        foreach (LevelAreaTarget area in levelAreas)
        {
            if (area.requiredLevel <= currentLevel && !area.hasBeenVisited && area.areaTarget != null)
            {
                foundArea = true;
                areaToShow = area;
                break; // Only show one arrow at a time
            }
        }
        
        // Only show message if we actually found an area
        if (foundArea && areaToShow != null)
        {
            Debug.Log($"Found available area to show: {areaToShow.areaName}");
            
            // Only show the message right before showing the arrow
            if (uiHelper != null)
            {
                uiHelper.ShowInformText("New Area Unlocked!");
            }
            
            // Show arrow to this area
            ShowArrowToArea(areaToShow);
        }
        else
        {
            Debug.Log("No new areas available to show");
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
    
    // Add this public method to manually connect events
    public void ConnectToCameraAnimations()
    {
        CameraAnimations cameraAnimations = FindObjectOfType<CameraAnimations>();
        if (cameraAnimations != null)
        {
            Debug.Log("Manually connecting CameraAnimations to LevelAreaArrowManager");
            OnAreaUnlocked += cameraAnimations.OnAreaUnlocked;
            
            // Test if the event is now subscribed
            if (OnAreaUnlocked != null)
            {
                Debug.Log("Successfully connected events - OnAreaUnlocked has subscribers");
            }
            else
            {
                Debug.LogError("Failed to connect events - OnAreaUnlocked still has no subscribers");
            }
        }
        else
        {
            Debug.LogError("Cannot find CameraAnimations to connect events");
        }
    }
} 