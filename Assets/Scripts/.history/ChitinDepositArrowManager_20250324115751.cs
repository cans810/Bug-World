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
    
    // Improved version of ForceShowArrow with extreme visibility measures
    public void ForceShowArrow()
    {
        Debug.Log("Force showing chitin deposit arrow - EXTREME VERSION");
        
        // Make sure this GameObject is active
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        // Find the nest if not assigned
        if (nestTarget == null)
        {
            nestTarget = GameObject.Find("Player Nest 1");
            if (nestTarget == null)
            {
                Debug.LogError("ChitinDepositArrowManager: Could not find 'Player Nest 1' in scene!");
                
                // Fallback: try to find any nest-like object
                GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.ToLower().Contains("nest"))
                    {
                        nestTarget = obj;
                        Debug.Log($"Found fallback nest: {nestTarget.name}");
                        break;
                    }
                }
                
                // If still null, create a dummy target object at a known position
                if (nestTarget == null)
                {
                    GameObject dummyTarget = new GameObject("DummyNestTarget");
                    
                    // Try to position it near the player
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        dummyTarget.transform.position = player.transform.position + player.transform.forward * 10f;
                    }
                    else
                    {
                        dummyTarget.transform.position = new Vector3(0, 0, 10);
                    }
                    
                    nestTarget = dummyTarget;
                    Debug.Log("Created dummy target object for arrow to point to");
                }
            }
            else
            {
                Debug.Log($"Found nest target: {nestTarget.name} at position {nestTarget.transform.position}");
            }
        }
        
        // BRUTE FORCE: Delete any existing arrow indicator and create a new one
        if (arrowIndicator != null)
        {
            Destroy(arrowIndicator);
            arrowIndicator = null;
        }
        
        // Create a new arrow indicator component
        Debug.Log("Creating brand new arrow indicator component");
        arrowIndicator = gameObject.AddComponent<PlayerFollowingArrowIndicator>();
        arrowIndicator.SetArrowColor(Color.yellow);
        arrowIndicator.SetTarget(nestTarget.transform);
        
        // Force showing the arrow with extreme visibility settings
        arrowIndicator.ShowArrow();
        
        // Also create a HIGHLY VISIBLE debug sphere to confirm rendering
        CreateDebugSphere();
        
        isArrowActive = true;
        
        // Informative message with more details
        Debug.Log($"ARROW CREATION COMPLETE - GameObject active: {gameObject.activeSelf}, " +
                  $"Parent active: {transform.parent?.gameObject.activeSelf}, " +
                  $"Arrow component: {arrowIndicator != null}");
        
        // Optionally inform the player with UI text again for redundancy
        UIHelper helper = FindObjectOfType<UIHelper>();
        if (helper != null)
        {
            helper.ShowInformText("Chitin collected! Return to nest to deposit.");
        }
    }
    
    // Add debug helper method to create a bright sphere
    private void CreateDebugSphere()
    {
        // Create a bright debug sphere at the player's position to confirm rendering is working
        GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugSphere.name = "DebugVisibilitySphere";
        
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Position above player's head
            debugSphere.transform.position = player.transform.position + Vector3.up * 3f;
        }
        else
        {
            // Fallback position
            debugSphere.transform.position = new Vector3(0, 3, 0);
        }
        
        // Make it big and obvious
        debugSphere.transform.localScale = new Vector3(1f, 1f, 1f);
        
        // Bright material
        Renderer renderer = debugSphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material brightMat = new Material(Shader.Find("Standard"));
            brightMat.color = Color.magenta;
            brightMat.EnableKeyword("_EMISSION");
            brightMat.SetColor("_EmissionColor", Color.magenta * 2f);
            renderer.material = brightMat;
        }
        
        // Destroy it after 10 seconds
        Destroy(debugSphere, 10f);
        
        Debug.Log($"Created debug sphere at position {debugSphere.transform.position}");
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