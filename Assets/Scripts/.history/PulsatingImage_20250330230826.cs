using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PulsatingImage : MonoBehaviour
{
    [Header("Scale Pulsation")]
    [SerializeField] private bool enableScalePulsation = true;
    [SerializeField] private float minScale = 0.9f;
    [SerializeField] private float maxScale = 1.1f;
    
    [Header("Color Pulsation")]
    [SerializeField] private bool enableColorPulsation = false;
    [SerializeField] private Color minColor = new Color(1, 1, 1, 0.7f);
    [SerializeField] private Color maxColor = new Color(1, 1, 1, 1f);
    
    [Header("Timing")]
    [SerializeField] private float pulsationSpeed = 1.0f;
    [SerializeField] [Range(0, 1)] private float startOffset = 0f; // Random offset for coordinated effects
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // References
    public Image targetImage;
    public RectTransform rectTransform;
    private float timeCounter;
    private Vector3 originalScale;

    private void Awake()
    {
        
        // Store original scale for reference
        originalScale = rectTransform.localScale;
        
        // Initialize with random time offset if needed
        timeCounter = startOffset * Mathf.PI * 2;
    }

    private void Start()
    {
        // Make sure we have a valid image to work with
        if (targetImage == null)
        {
            Debug.LogError("PulsatingImage requires an Image component on the same GameObject!");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Increment time counter
        timeCounter += Time.deltaTime * pulsationSpeed;
        
        // Calculate interpolation factor from sine wave (0 to 1)
        float t = (Mathf.Sin(timeCounter) + 1f) * 0.5f;
        
        // Apply animation curve if available
        if (pulseCurve != null && pulseCurve.length > 0)
        {
            t = pulseCurve.Evaluate(t);
        }
        
        // Handle scale pulsation
        if (enableScalePulsation)
        {
            float scaleFactor = Mathf.Lerp(minScale, maxScale, t);
            rectTransform.localScale = originalScale * scaleFactor;
        }
        
        // Handle color pulsation
        if (enableColorPulsation && targetImage != null)
        {
            targetImage.color = Color.Lerp(minColor, maxColor, t);
        }
    }

    // Method to restart the pulsation from the beginning
    public void RestartPulsation()
    {
        timeCounter = 0f;
    }
    
    // Method to stop pulsation and reset to original state
    public void StopPulsation()
    {
        enabled = false;
        
        // Reset scale
        if (enableScalePulsation)
        {
            rectTransform.localScale = originalScale;
        }
        
        // Reset color
        if (enableColorPulsation && targetImage != null)
        {
            targetImage.color = maxColor;
        }
    }
}
