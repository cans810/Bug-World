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
    [SerializeField] private bool hideJoystickOnRelease = false;
    [SerializeField] private bool centerJoystickOnPress = true;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color activeColor = new Color(1f, 1f, 1f, 0.8f);
    
    // Private variables
    private Vector2 input = Vector2.zero;
    private Vector2 touchStartPosition;
    private Vector2 backgroundStartPosition;
    private Image backgroundImage;
    private Image handleImage;
    private Canvas canvas;
    private Camera uiCamera;
    private bool isDragging = false;
    
    // Public properties
    public Vector2 Input => input;
    public float Horizontal => input.x;
    public float Vertical => input.y;
    public bool IsDragging => isDragging;
    
    private void Awake()
    {
        // Get references
        canvas = GetComponentInParent<Canvas>();
        backgroundImage = joystickBackground.GetComponent<Image>();
        handleImage = joystickHandle.GetComponent<Image>();
        
        // Store the initial position of the joystick background
        backgroundStartPosition = joystickBackground.anchoredPosition;
        
        // Get the UI camera if the canvas is in Screen Space - Camera or World Space
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }
        
        // Initialize joystick state
        if (hideJoystickOnRelease)
        {
            SetJoystickVisibility(false);
        }
        
        // Set initial colors
        if (backgroundImage != null) backgroundImage.color = normalColor;
        if (handleImage != null) handleImage.color = normalColor;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        
        // Store the touch/click position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            uiCamera,
            out touchStartPosition
        );
        
        // Center the joystick at the touch position if enabled
        if (centerJoystickOnPress)
        {
            Vector2 touchPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                eventData.position,
                uiCamera,
                out touchPosition
            );
            joystickBackground.anchoredPosition = touchPosition;
        }
        
        // Show the joystick if it was hidden
        if (hideJoystickOnRelease)
        {
            SetJoystickVisibility(true);
        }
        
        // Set active colors
        if (backgroundImage != null) backgroundImage.color = activeColor;
        if (handleImage != null) handleImage.color = activeColor;
        
        // Process the initial drag
        OnDrag(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
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
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        input = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
        
        // Reset joystick position if it was centered on press
        if (centerJoystickOnPress)
        {
            joystickBackground.anchoredPosition = backgroundStartPosition;
        }
        
        // Hide the joystick if enabled
        if (hideJoystickOnRelease)
        {
            SetJoystickVisibility(false);
        }
        
        // Reset colors
        if (backgroundImage != null) backgroundImage.color = normalColor;
        if (handleImage != null) handleImage.color = normalColor;
    }
    
    private void SetJoystickVisibility(bool visible)
    {
        if (backgroundImage != null) backgroundImage.enabled = visible;
        if (handleImage != null) handleImage.enabled = visible;
    }
} 