using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChitinUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI chitinCountText;
    [SerializeField] private Image chitinIcon;
    [SerializeField] private Animator uiAnimator; // Optional: for animations
    
    [Header("Display Settings")]
    [SerializeField] private string countFormat = "x{0}";
    [SerializeField] private bool animateChanges = true;
    
    private void Start()
    {
        // Find the ChitinCollector and subscribe to its events
        if (ChitinCollector.Instance != null)
        {
            // Update UI with initial value
            UpdateChitinDisplay(ChitinCollector.Instance.ChitinCount);
            
            // Subscribe to future changes
            ChitinCollector.Instance.OnChitinCountChanged += UpdateChitinDisplay;
        }
        else
        {
            Debug.LogError("ChitinUI couldn't find ChitinCollector instance!");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscription
        if (ChitinCollector.Instance != null)
        {
            ChitinCollector.Instance.OnChitinCountChanged -= UpdateChitinDisplay;
        }
    }
    
    private void UpdateChitinDisplay(int count)
    {
        // Update the text
        if (chitinCountText != null)
        {
            chitinCountText.text = string.Format(countFormat, count);
        }
        
        // Optional: Play animation if count increased
        if (animateChanges && uiAnimator != null)
        {
            uiAnimator.SetTrigger("CountChanged");
        }
    }
} 