using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AlliesPanelDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttributes playerAttributes;
    [SerializeField] private PlayerInventory playerInventory;
    
    [Header("Panel Settings")]
    [SerializeField] private bool showOnBreadCrumbsFilled = true;
    [SerializeField] private GameObject alliesPanel;
    [SerializeField] private Animator panelAnimator; // Optional
    
    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI antPointsText;
    
    [Header("Buttons")]
    [SerializeField] private Button addPointsOnAnt;
    [SerializeField] private Button closeButton; // Optional close button
    
    [Header("Ant Egg Spawning")]
    [SerializeField] private GameObject antEggPrefab;
    [SerializeField] private int crumbsRequiredForEgg = 5;
    [SerializeField] private Transform eggSpawnPoint; // Optional - defaults to this transform
    [SerializeField] private UIHelper uiHelper; // To display messages
    
    // Track crumb collection internally
    private int lastCrumbCount = 0;
    
    private void Start()
    {
        // Find references if not assigned
        if (playerAttributes == null)
            playerAttributes = FindObjectOfType<PlayerAttributes>();
            
        if (playerInventory == null)
            playerInventory = FindObjectOfType<PlayerInventory>();
            
        if (uiHelper == null)
            uiHelper = FindObjectOfType<UIHelper>();
            
        // If no spawn point specified, use this transform
        if (eggSpawnPoint == null)
            eggSpawnPoint = transform;
            
        // Subscribe to attribute changes and level up events
        if (playerAttributes != null)
            playerAttributes.OnAttributesChanged += UpdateDisplay;
            
        if (playerInventory != null)
        {
            playerInventory.OnLevelUp += OnPlayerLevelUp;
            playerInventory.OnCrumbCountChanged += OnCrumbCountChanged;
            
            // Initialize tracking
            lastCrumbCount = playerInventory.CrumbCount;
        }
            
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);
            
        // Initial update
        UpdateDisplay();
        
        // Hide panel initially unless player has points to spend
        if (alliesPanel != null && (playerAttributes == null || playerAttributes.AvailablePoints <= 0))
        {
            alliesPanel.SetActive(false);
        }
    }
    
    private void OnCrumbCountChanged(int newCrumbCount)
    {
        // Check if we've reached or passed a threshold of collected crumbs
        int crumbsCollectedSinceLastCheck = newCrumbCount - lastCrumbCount;
        
        // If crumbs were added (not removed/used)
        if (crumbsCollectedSinceLastCheck > 0)
        {
            // Calculate how many eggs to spawn based on crumbs collected
            int eggsToSpawn = crumbsCollectedSinceLastCheck / crumbsRequiredForEgg;
            
            // If we need to spawn eggs
            if (eggsToSpawn > 0)
            {
                for (int i = 0; i < eggsToSpawn; i++)
                {
                    SpawnAntEgg();
                }
                
                // Notify the player
                if (uiHelper != null && uiHelper.informPlayerText != null)
                {
                    string message = eggsToSpawn == 1 ? 
                        "An ant egg has been created!" : 
                        $"{eggsToSpawn} ant eggs have been created!";
                    
                    uiHelper.informPlayerText.text = message;
                    StartCoroutine(ClearMessageAfterDelay(3f));
                }
            }
        }
        
        // Update our tracking
        lastCrumbCount = newCrumbCount;
    }
    
    private System.Collections.IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (uiHelper != null && uiHelper.informPlayerText != null)
        {
            uiHelper.informPlayerText.text = "";
        }
    }
    
    private void SpawnAntEgg()
    {
        if (antEggPrefab != null)
        {
            // Instantiate the ant egg at the spawn position
            GameObject newEgg = Instantiate(antEggPrefab, eggSpawnPoint.position, Quaternion.identity);
            
            // Optional: Make the egg a child of this transform or a specific container
            newEgg.transform.SetParent(eggSpawnPoint);
            
            Debug.Log("Ant egg spawned successfully!");
            
            // Show the allies panel when an egg is spawned
            if (showOnBreadCrumbsFilled)
            {
                ShowPanel();
            }
        }
        else
        {
            Debug.LogError("Cannot spawn ant egg: antEggPrefab is not assigned!");
        }
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        // Show the panel when player levels up
        if (showOnBreadCrumbsFilled && alliesPanel != null)
        {
            ShowPanel();
        }
    }
    
    private void ShowPanel()
    {
        if (alliesPanel != null)
        {
            alliesPanel.SetActive(true);
            
            // If using an animator, trigger show animation
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger("Show");
            }
        }
    }
    
    private void HidePanel()
    {
        if (alliesPanel != null)
        {
            // If using an animator, trigger hide animation
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger("Hide");
                // Panel will be deactivated by animation event
            }
            else
            {
                alliesPanel.SetActive(false);
            }
        }
    }
    
    private void UpdateDisplay()
    {
        if (playerAttributes != null)
        {
            // Update available points
            if (antPointsText != null)
                antPointsText.text = $"Ally Points: {playerAttributes.AvailablePoints}";
        }
    }
    
    private void OnDestroy()
    {
        if (playerAttributes != null)
            playerAttributes.OnAttributesChanged -= UpdateDisplay;
            
        if (playerInventory != null)
        {
            playerInventory.OnLevelUp -= OnPlayerLevelUp;
            playerInventory.OnCrumbCountChanged -= OnCrumbCountChanged;
        }
            
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HidePanel);
    }
} 