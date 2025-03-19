using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OnScreenJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private float handleRange = 1f;
    [SerializeField] private float deadZone = 0.1f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color activeColor = new Color(1f, 1f, 1f, 0.8f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Private variables
    private Vector2 input = Vector2.zero;
    private Vector2 touchStartPosition;
    private Image backgroundImage;
    private Image handleImage;
    private Canvas canvas;
    private Camera uiCamera;
    private bool isDragging = false;
    private RectTransform canvasRectTransform;
    
    // Public properties
    public Vector2 Input => input;
    public float Horizontal => input.x;
    public float Vertical => input.y;
    public bool IsDragging => isDragging;
    
    private void Awake()
    {
        // Get references
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        backgroundImage = joystickBackground.GetComponent<Image>();
        handleImage = joystickHandle.GetComponent<Image>();
        
        // Get the UI camera if the canvas is in Screen Space - Camera or World Space
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }
        
        // Hide joystick initially
        SetJoystickVisibility(false);
        
        // Set initial colors
        if (backgroundImage != null) backgroundImage.color = normalColor;
        if (handleImage != null) handleImage.color = normalColor;
        
        if (showDebugInfo)
        {
            Debug.Log("OnScreenJoystick initialized");
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        
        // Position the joystick at the touch/click position
        Vector2 touchPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            eventData.position,
            uiCamera,
            out touchPosition
        );
        
        // Set the joystick position
        joystickBackground.anchoredPosition = touchPosition;
        
        // Reset handle position
        joystickHandle.anchoredPosition = Vector2.zero;
        
        // Show the joystick
        SetJoystickVisibility(true);
        
        // Set active colors
        if (backgroundImage != null) backgroundImage.color = activeColor;
        if (handleImage != null) handleImage.color = activeColor;
        
        if (showDebugInfo)
        {
            Debug.Log($"Joystick OnPointerDown at {touchPosition}, isDragging={isDragging}");
        }
        
        // Process the initial drag
        OnDrag(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) 
        {
            if (showDebugInfo) Debug.Log("OnDrag called but isDragging is false");
            return;
        }
        
        Vector2 touchPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            uiCamera,
            out touchPosition))
        {
            // Calculate the direction from the center of the joystick background
            Vector2 direction = touchPosition - Vector2.zero;
            
            // Calculate input based on the direction and handle range
            input = (direction.magnitude > joystickBackground.sizeDelta.x * handleRange) 
                ? direction.normalized * handleRange
                : direction / (joystickBackground.sizeDelta.x * handleRange);
            
            // Apply deadzone
            if (input.magnitude < deadZone)
            {
                input = Vector2.zero;
            }
            else
            {
                // Rescale input after deadzone
                input = input.normalized * ((input.magnitude - deadZone) / (1 - deadZone));
            }
            
            // Move the handle
            joystickHandle.anchoredPosition = input * (joystickBackground.sizeDelta.x * handleRange);
            
            if (showDebugInfo && Time.frameCount % 30 == 0)
            {
                Debug.Log($"Joystick input: {input}, magnitude: {input.magnitude}");
            }
        }
        else
        {
            if (showDebugInfo) Debug.Log("Failed to get touch position in OnDrag");
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (showDebugInfo)
        {
            Debug.Log("Joystick OnPointerUp, resetting");
        }
        
        isDragging = false;
        input = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
        
        // Hide the joystick
        SetJoystickVisibility(false);
        
        // Reset colors
        if (backgroundImage != null) backgroundImage.color = normalColor;
        if (handleImage != null) handleImage.color = normalColor;
    }
    
    private void SetJoystickVisibility(bool visible)
    {
        if (backgroundImage != null) backgroundImage.enabled = visible;
        if (handleImage != null) handleImage.enabled = visible;
    }
    
    // Add this to ensure the joystick is properly visible in the scene
    private void OnEnable()
    {
        if (showDebugInfo)
        {
            Debug.Log("OnScreenJoystick enabled");
        }
    }
} 