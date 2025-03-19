using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BackgroundScaler : MonoBehaviour
{
    public enum ScaleMode
    {
        FitToScreen,
        FillScreen,
        PreserveWidth,
        PreserveHeight
    }

    [SerializeField] private ScaleMode scaleMode = ScaleMode.FillScreen;
    [SerializeField] private float additionalScale = 1.1f; // Slight overscaling to prevent empty edges

    private Image backgroundImage;
    private RectTransform rectTransform;
    private Vector2 originalImageSize;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        
        if (backgroundImage.sprite != null)
        {
            originalImageSize = new Vector2(backgroundImage.sprite.rect.width, backgroundImage.sprite.rect.height);
        }

        // Initial setup
        SetupBackground();
    }

    private void SetupBackground()
    {
        // Basic RectTransform setup
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        // Calculate screen and image aspects
        float screenAspect = (float)Screen.width / Screen.height;
        float imageAspect = originalImageSize.x / originalImageSize.y;

        switch (scaleMode)
        {
            case ScaleMode.FitToScreen:
                backgroundImage.preserveAspect = true;
                break;

            case ScaleMode.FillScreen:
                backgroundImage.preserveAspect = false;
                // Apply additional scale to prevent empty edges
                rectTransform.localScale = Vector3.one * additionalScale;
                break;

            case ScaleMode.PreserveWidth:
                backgroundImage.preserveAspect = false;
                if (screenAspect < imageAspect)
                {
                    float scale = screenAspect / imageAspect;
                    rectTransform.localScale = new Vector3(1, scale, 1) * additionalScale;
                }
                break;

            case ScaleMode.PreserveHeight:
                backgroundImage.preserveAspect = false;
                if (screenAspect > imageAspect)
                {
                    float scale = imageAspect / screenAspect;
                    rectTransform.localScale = new Vector3(scale, 1, 1) * additionalScale;
                }
                break;
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        SetupBackground();
    }
} 