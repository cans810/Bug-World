using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BackgroundScaler : MonoBehaviour
{
    private Image backgroundImage;
    private RectTransform rectTransform;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        
        // Make sure the image is set to stretch
        backgroundImage.preserveAspect = false;
        
        // Set the RectTransform to stretch in both directions
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }
} 