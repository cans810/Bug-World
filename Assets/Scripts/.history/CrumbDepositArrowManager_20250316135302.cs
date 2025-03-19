using UnityEngine;

public class CrumbDepositArrowManager : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject nestTarget; // Reference to "Player Nest 1"
    [SerializeField] private GameObject arrowPrefab; // Add this field for your arrow prefab
    [SerializeField] private Color arrowColor = Color.yellow; // Yellow for crumbs
    [SerializeField] private float showArrowDelay = 2f; // Delay before showing arrow after inventory is full
    
    private PlayerInventory playerInventory;
    private ArrowIndicator arrowIndicator;
    private bool isArrowActive = false;
    private float fullInventoryTime = -1f;
    
    private void Start()
    {
        // Find the player inventory
        playerInventory = FindObjectOfType<PlayerInventory>();
        
        if (playerInventory == null)
        {
            Debug.LogError("CrumbDepositArrowManager: PlayerInventory not found!");
            return;
        }
        
        // Find the nest if not assigned
        if (nestTarget == null)
        {
            nestTarget = GameObject.Find("Player Nest 1");
            if (nestTarget == null)
            {
                Debug.LogError("CrumbDepositArrowManager: Could not find 'Player Nest 1' in scene!");
                return;
            }
        }
        
        // Add arrow indicator component
        arrowIndicator = gameObject.AddComponent<ArrowIndicator>();
        
        // Set the arrow prefab
        arrowIndicator.SetArrowPrefab(arrowPrefab);
        arrowIndicator.SetArrowColor(arrowColor);
        arrowIndicator.SetTarget(nestTarget);
        
        // Subscribe to inventory events
        playerInventory.OnCrumbCountChanged += CheckCrumbCount;
        playerInventory.OnCrumbMaxed += OnCrumbMaxed;
    }
    
    private void Update()
    {
        // Check if we should show the arrow after delay
        if (fullInventoryTime > 0 && !isArrowActive && Time.time >= fullInventoryTime + showArrowDelay)
        {
            ShowArrow();
        }
    }
    
    private void CheckCrumbCount(int crumbCount)
    {
        // If inventory is full, start the timer to show arrow
        if (crumbCount >= playerInventory.MaxCrumbCapacity)
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
    
    private void OnCrumbMaxed()
    {
        // This is called when player tries to collect more crumbs when already full
        // We can immediately show the arrow in this case
        ShowArrow();
    }
    
    private void ShowArrow()
    {
        if (!isArrowActive)
        {
            arrowIndicator.ShowArrow();
            isArrowActive = true;
            
            // Optionally inform the player with UI text
            UIHelper helper = FindObjectOfType<UIHelper>();
            if (helper != null)
            {
                helper.ShowInformText("Crumb inventory full! Return to nest to deposit.");
            }
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
            playerInventory.OnCrumbCountChanged -= CheckCrumbCount;
            playerInventory.OnCrumbMaxed -= OnCrumbMaxed;
        }
    }
} 