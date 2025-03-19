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
        if (canvas == null)
        {
            Debug.LogError("OnScreenJoystick: No Canvas found in parent hierarchy!");
            enabled = false;
            return;
        }
        
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        
        // Validate required components
        if (joystickBackground == null)
        {
            Debug.LogError("OnScreenJoystick: joystickBackground is not assigned!");
            enabled = false;
            return;
        }
        
        if (joystickHandle == null)
        {
            Debug.LogError("OnScreenJoystick: joystickHandle is not assigned!");
            enabled = false;
            return;
        }
        
        backgroundImage = joystickBackground.GetComponent<Image>();
        handleImage = joystickHandle.GetComponent<Image>();
        
        if (backgroundImage == null)
        {
            Debug.LogError("OnScreenJoystick: No Image component found on joystickBackground!");
            enabled = false;
            return;
        }
        
        if (handleImage == null)
        {
            Debug.LogError("OnScreenJoystick: No Image component found on joystickHandle!");
            enabled = false;
            return;
        }
        
        // Get the UI camera if the canvas is in Screen Space - Camera or World Space
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
            if (uiCamera == null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                Debug.LogError("OnScreenJoystick: Canvas is in ScreenSpaceCamera mode but no camera is assigned!");
            }
        }
        
        // Hide joystick initially
        SetJoystickVisibility(false);
        
        // Set initial colors
        if (backgroundImage != null) backgroundImage.color = normalColor;
        if (handleImage != null) handleImage.color = normalColor;
        
        if (showDebugInfo)
        {
            Debug.Log("OnScreenJoystick initialized with settings:");
            Debug.Log($"- Handle Range: {handleRange}");
            Debug.Log($"- Dead Zone: {deadZone}");
            Debug.Log($"- Canvas Render Mode: {canvas.renderMode}");
            Debug.Log($"- UI Camera: {(uiCamera != null ? uiCamera.name : "None")}");
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (showDebugInfo)
        {
            Debug.Log($"OnPointerDown detected at screen position: {eventData.position}");
        }
        
        isDragging = true;
        
        // Position the joystick at the touch/click position
        Vector2 touchPosition;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            eventData.position,
            uiCamera,
            out touchPosition
        );
        
        if (!success)
        {
            Debug.LogError($"OnScreenJoystick: Failed to convert screen point to local point! Canvas: {canvas.name}, Camera: {(uiCamera != null ? uiCamera.name : "None")}");
            return;
        }
        
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
            Debug.Log($"Joystick positioned at {touchPosition}, visibility set to true");
            Debug.Log($"Background enabled: {backgroundImage.enabled}, Handle enabled: {handleImage.enabled}");
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
        if (backgroundImage != null) 
        {
            backgroundImage.enabled = visible;
            if (showDebugInfo && visible != backgroundImage.enabled)
            {
                Debug.LogWarning($"Failed to set backgroundImage.enabled to {visible}");
            }
        }
        
        if (handleImage != null) 
        {
            handleImage.enabled = visible;
            if (showDebugInfo && visible != handleImage.enabled)
            {
                Debug.LogWarning($"Failed to set handleImage.enabled to {visible}");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SetJoystickVisibility({visible}) - Background: {(backgroundImage != null ? backgroundImage.enabled.ToString() : "null")}, Handle: {(handleImage != null ? handleImage.enabled.ToString() : "null")}");
        }
    }
    
    // Add this to ensure the joystick is properly visible in the scene
    private void OnEnable()
    {
        if (showDebugInfo)
        {
            Debug.Log("OnScreenJoystick enabled");
        }
    }

    // Add this to check if the joystick is receiving input events
    private void Update()
    {
        if (showDebugInfo && UnityEngine.Input.touchCount > 0 && !isDragging)
        {
            Debug.Log($"Touch detected but joystick not dragging. Touch count: {UnityEngine.Input.touchCount}");
        }
    }
} 