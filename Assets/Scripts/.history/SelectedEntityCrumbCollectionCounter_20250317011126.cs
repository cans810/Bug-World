using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectedEntityCrumbCollectionCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI crumbCountText;
    [SerializeField] private string crumbCountFormat = "{0}/{1}";
    
    // Internal variables
    private CanvasGroup canvasGroup;

    
    
} 