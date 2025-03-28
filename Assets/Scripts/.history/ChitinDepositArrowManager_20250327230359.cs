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
    
    // Add a flag to track first-time tutorial arrow
    private bool isShowingFirstTimeArrow = false;
    
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

    }
    
    private void CheckChitinCount(int chitinCount)
    {
        // IMPORTANT: Don't hide arrow if showing first-time tutorial
        if (isShowingFirstTimeArrow)
            return;
            
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
} 