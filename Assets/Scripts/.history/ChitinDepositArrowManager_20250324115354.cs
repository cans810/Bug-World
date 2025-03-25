using UnityEngine;

public class ChitinDepositArrowManager : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject nestTarget; // Reference to "Player Nest 1"
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float showArrowDelay = 2f; // Delay before showing arrow after inventory is full
    
    private PlayerInventory playerInventory;
    private PlayerFollowingArrowIndicator arrowIndicator;
    private bool isArrowActive = false;
    private float fullInventoryTime = -1f;
    
    private void Start()
    {
        // Find the player inventory
        playerInventory = FindObjectOfType<PlayerInventory>();
        
        if (playerInventory == null)
        {
            Debug.LogError("ChitinDepositArrowManager: PlayerInventory not found!");
            return;
        }
        
        // Find the nest if not assigned
        if (nestTarget == null)
        {
            nestTarget = GameObject.Find("Player Nest 1");
            if (nestTarget == null)
            {
                Debug.LogError("ChitinDepositArrowManager: Could not find 'Player Nest 1' in scene!");
                return;
            }
        }
        
        // Add player following arrow indicator component
        arrowIndicator = gameObject.AddComponent<PlayerFollowingArrowIndicator>();
        arrowIndicator.SetArrowColor(arrowColor);
        arrowIndicator.SetTarget(nestTarget.transform);
        
        // Subscribe to inventory events
        playerInventory.OnChitinCountChanged += CheckChitinCount;
        playerInventory.OnChitinMaxed += OnChitinMaxed;
    }
    
    private void Update()
    {
        // Check if we should show the arrow after delay
        if (fullInventoryTime > 0 && !isArrowActive && Time.time >= fullInventoryTime + showArrowDelay)
        {
            ShowArrow();
        }
    }
    
    private void CheckChitinCount(int chitinCount)
    {
        // If inventory is full, start the timer to show arrow
        if (chitinCount >= playerInventory.MaxChitinCapacity)
        {
            if (fullInventoryTime < 0)
            {
                fullInventoryTime = Time.time;
            }
        }
        else
        {
            // If inventory is no longer full, hide arrow and reset timer
            if (isArrowActive)
            {
                HideArrow();
            }
            fullInventoryTime = -1f;
        }
    }
    
    private void OnChitinMaxed()
    {
        // This is called when player tries to collect more chitin when already full
        // We can immediately show the arrow in this case
        ShowArrow();
    }
    
    private void ShowArrow()
    {
        if (!isArrowActive)
        {
            // Make sure the indicator component exists
            if (arrowIndicator == null)
            {
                Debug.LogWarning("Arrow indicator was null - recreating");
                arrowIndicator = gameObject.AddComponent<PlayerFollowingArrowIndicator>();
                arrowIndicator.SetArrowColor(arrowColor);
                arrowIndicator.SetTarget(nestTarget.transform);
            }
            
            // Show the arrow
            arrowIndicator.ShowArrow();
            isArrowActive = true;
            
            // Force the arrow to be visible for testing
            Debug.Log("FORCING ARROW TO BE VISIBLE");
            
            // Optionally inform the player with UI text
            UIHelper helper = FindObjectOfType<UIHelper>();
            if (helper != null)
            {
                helper.ShowInformText("Chitin inventory full! Return to nest to deposit.");
            }
        }
    }
    
    // Improved version of ForceShowArrow
    public void ForceShowArrow()
    {
        Debug.Log("Force showing chitin deposit arrow - ENHANCED VERSION");
        
        // Find the nest if not assigned
        if (nestTarget == null)
        {
            nestTarget = GameObject.Find("Player Nest 1");
            if (nestTarget == null)
            {
                Debug.LogError("ChitinDepositArrowManager: Could not find 'Player Nest 1' in scene!");
                return;
            }
            else
            {
                Debug.Log($"Found nest target: {nestTarget.name} at position {nestTarget.transform.position}");
            }
        }
        
        // Re-create the arrow indicator component if needed
        if (arrowIndicator == null)
        {
            Debug.Log("Creating new arrow indicator component");
            arrowIndicator = gameObject.AddComponent<PlayerFollowingArrowIndicator>();
            arrowIndicator.SetArrowColor(arrowColor);
            arrowIndicator.SetTarget(nestTarget.transform);
        }
        
        // IMPORTANT: Make sure the arrow is visible
        arrowIndicator.ShowArrow();
        
        // Also call the force visibility method
        arrowIndicator.ForceArrowVisibility();
        
        isArrowActive = true;
        
        Debug.Log("ARROW SHOULD NOW BE VISIBLE - pointing to nest");
        
        // Optionally inform the player with UI text again for redundancy
        UIHelper helper = FindObjectOfType<UIHelper>();
        if (helper != null)
        {
            helper.ShowInformText("Chitin collected! Return to nest to deposit.");
        }
    }
    
    private void HideArrow()
    {
        if (isArrowActive)
        {
            arrowIndicator.HideArrow();
            isArrowActive = false;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (playerInventory != null)
        {
            playerInventory.OnChitinCountChanged -= CheckChitinCount;
            playerInventory.OnChitinMaxed -= OnChitinMaxed;
        }
    }
} 