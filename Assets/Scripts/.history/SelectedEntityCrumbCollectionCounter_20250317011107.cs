using UnityEngine;
using UnityEngine.UI;

public class SelectedEntityCrumbCollectionCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI crumbCountText;
    [SerializeField] private string crumbCountFormat = "{0}/{1}";

    [Header("Display Settings")]
    [SerializeField] private float displayDuration = 3f; // How long to show after damage
    [SerializeField] private float fadeSpeed = 2f; // How fast to fade out
    
    // Internal variables
    private CanvasGroup canvasGroup;
    private float displayTimer;
    private Transform entityTransform;
    private bool isDead = false;
    
    
} 