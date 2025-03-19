using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class OnScreenJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private float handleRange = 1f;
    [SerializeField] private float deadZone = 0.1f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color backgroundNormalColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color backgroundActiveColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color handleNormalColor = new Color(1f, 1f, 1f, 1f); // Full opacity
    [SerializeField] private Color handleActiveColor = new Color(1f, 1f, 1f, 1f); // Full opacity
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Alternative Input Method")]
    [SerializeField] private bool useDirectInput = true;
    [SerializeField] private RectTransform touchArea;
    
    [Header("Joystick Type")]
    [SerializeField] private bool useFixedPosition = true;
    [SerializeField] private Vector2 fixedJoystickPosition = new Vector2(200, 200); // Position from bottom-left
    
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
    public Vector2 InputVector => input;
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
        if (backgroundImage != null) backgroundImage.color = backgroundNormalColor;
        if (handleImage != null) handleImage.color = handleNormalColor;
        
        if (showDebugInfo)
        {
            Debug.Log("OnScreenJoystick initialized with settings:");
            Debug.Log($"- Handle Range: {handleRange}");
            Debug.Log($"- Dead Zone: {deadZone}");
            Debug.Log($"- Canvas Render Mode: {canvas.renderMode}");
            Debug.Log($"- UI Camera: {(uiCamera != null ? uiCamera.name : "None")}");
        }
        
        if (useFixedPosition)
        {
            // Set the fixed position
            joystickBackground.position = fixedJoystickPosition;
            joystickHandle.position = fixedJoystickPosition;
            
            // Make the joystick always visible
            SetJoystickVisibility(true);
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
        if (backgroundImage != null) backgroundImage.color = backgroundActiveColor;
        if (handleImage != null) handleImage.color = handleActiveColor;
        
        if (showDebugInfo)
        {
            Debug.Log($"Joystick positioned at {touchPosition}, visibility set to true");
            Debug.Log($"Background active: {joystickBackground.gameObject.activeSelf}, enabled: {backgroundImage.enabled}");
            Debug.Log($"Handle active: {joystickHandle.gameObject.activeSelf}, enabled: {handleImage.enabled}");
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
        if (backgroundImage != null) backgroundImage.color = backgroundNormalColor;
        if (handleImage != null) handleImage.color = handleNormalColor;
    }
    
    private void SetJoystickVisibility(bool visible)
    {
        if (backgroundImage != null) 
        {
            // Force enable/disable the GameObject itself, not just the Image component
            joystickBackground.gameObject.SetActive(visible);
            backgroundImage.enabled = visible;
            
            if (showDebugInfo && visible != backgroundImage.enabled)
            {
                Debug.LogWarning($"Failed to set backgroundImage.enabled to {visible}");
            }
        }
        
        if (handleImage != null) 
        {
            // Force enable/disable the GameObject itself, not just the Image component
            joystickHandle.gameObject.SetActive(visible);
            handleImage.enabled = visible;
            
            if (showDebugInfo && visible != handleImage.enabled)
            {
                Debug.LogWarning($"Failed to set handleImage.enabled to {visible}");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SetJoystickVisibility({visible}) - Background active: {joystickBackground.gameObject.activeSelf}, enabled: {backgroundImage.enabled}, Handle active: {joystickHandle.gameObject.activeSelf}, enabled: {handleImage.enabled}");
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
        // Direct input handling - bypass the event system if needed
        if (useDirectInput)
        {
            // Handle touch input
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                
                // Handle touch begin
                if (touch.phase == TouchPhase.Began && !isDragging)
                {
                    // Check if the touch is within our touch area (if specified)
                    bool touchInArea = true;
                    if (touchArea != null)
                    {
                        Vector2 localPoint;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            touchArea,
                            touch.position,
                            uiCamera,
                            out localPoint
                        );
                        
                        // Check if the point is inside the rect
                        touchInArea = touchArea.rect.Contains(localPoint);
                    }
                    
                    if (touchInArea)
                    {
                        // For Screen Space Overlay, we can directly use the touch position
                        if (showDebugInfo)
                        {
                            Debug.Log($"Direct input: Touch began at {touch.position}");
                        }
                        
                        // Set dragging state
                        isDragging = true;
                        
                        // Position joystick directly under the touch point
                        joystickBackground.position = touch.position;
                        joystickHandle.position = touch.position;
                        
                        // Show joystick
                        SetJoystickVisibility(true);
                        
                        // Set colors
                        if (backgroundImage != null) backgroundImage.color = backgroundActiveColor;
                        if (handleImage != null) handleImage.color = handleActiveColor;
                    }
                }
                // Handle ongoing touch
                else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && isDragging)
                {
                    HandleDirectInput(touch.position);
                }
                // Handle touch end
                else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && isDragging)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log("Direct input: Touch ended, resetting joystick");
                    }
                    
                    // Reset joystick
                    isDragging = false;
                    input = Vector2.zero;
                    joystickHandle.anchoredPosition = Vector2.zero;
                    
                    // Hide joystick
                    SetJoystickVisibility(false);
                    
                    // Reset colors
                    if (backgroundImage != null) backgroundImage.color = backgroundNormalColor;
                    if (handleImage != null) handleImage.color = handleNormalColor;
                }
            }
            
            // Handle mouse input (for testing in editor)
            #if UNITY_EDITOR
            if (UnityEngine.Input.GetMouseButtonDown(0) && !isDragging)
            {
                // Check if the click is within our touch area (if specified)
                bool clickInArea = true;
                if (touchArea != null)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        touchArea,
                        UnityEngine.Input.mousePosition,
                        uiCamera,
                        out localPoint
                    );
                    
                    // Check if the point is inside the rect
                    clickInArea = touchArea.rect.Contains(localPoint);
                }
                
                if (clickInArea)
                {
                    // For Screen Space Overlay, we can directly use the mouse position
                    if (showDebugInfo)
                    {
                        Debug.Log($"Direct input: Mouse click at {UnityEngine.Input.mousePosition}");
                    }
                    
                    // Set dragging state
                    isDragging = true;
                    
                    // Position joystick directly under the click point
                    joystickBackground.position = UnityEngine.Input.mousePosition;
                    joystickHandle.position = UnityEngine.Input.mousePosition;
                    
                    // Show joystick
                    SetJoystickVisibility(true);
                    
                    // Set colors
                    if (backgroundImage != null) backgroundImage.color = backgroundActiveColor;
                    if (handleImage != null) handleImage.color = handleActiveColor;
                }
            }
            else if (UnityEngine.Input.GetMouseButton(0) && isDragging)
            {
                HandleDirectInput(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0) && isDragging)
            {
                if (showDebugInfo)
                {
                    Debug.Log("Direct input: Mouse up, resetting joystick");
                }
                
                // Reset joystick
                isDragging = false;
                input = Vector2.zero;
                joystickHandle.anchoredPosition = Vector2.zero;
                
                // Hide joystick
                SetJoystickVisibility(false);
                
                // Reset colors
                if (backgroundImage != null) backgroundImage.color = backgroundNormalColor;
                if (handleImage != null) handleImage.color = handleNormalColor;
            }
            #endif
        }
        
        // Check if this GameObject is receiving raycast hits
        if (showDebugInfo && (UnityEngine.Input.GetMouseButtonDown(0) || (UnityEngine.Input.touchCount > 0 && UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began)))
        {
            Vector2 inputPosition = UnityEngine.Input.touchCount > 0 ? 
                UnityEngine.Input.GetTouch(0).position : 
                UnityEngine.Input.mousePosition;
                
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = inputPosition;
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            bool hitThis = false;
            foreach (var result in results)
            {
                if (result.gameObject == gameObject || 
                    result.gameObject == joystickBackground.gameObject || 
                    result.gameObject == joystickHandle.gameObject)
                {
                    hitThis = true;
                    Debug.Log($"Raycast hit this joystick: {result.gameObject.name}");
                    break;
                }
            }
            
            if (!hitThis)
            {
                Debug.LogWarning("Input detected but raycast did not hit this joystick!");
            }
        }
        
        // Check for touches and mouse input
        if (showDebugInfo)
        {
            if (UnityEngine.Input.touchCount > 0 && !isDragging)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                Debug.Log($"Touch detected but joystick not dragging. Touch position: {touch.position}, phase: {touch.phase}");
                
                // Try to determine if the touch should be hitting this UI element
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    GetComponent<RectTransform>(),
                    touch.position,
                    uiCamera,
                    out localPoint
                );
                Debug.Log($"Touch local point relative to this object: {localPoint}");
            }
            
            if (UnityEngine.Input.GetMouseButton(0) && !isDragging)
            {
                Debug.Log($"Mouse button down but joystick not dragging. Mouse position: {UnityEngine.Input.mousePosition}");
            }
        }
        
        // Debug raycasting
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = UnityEngine.Input.mousePosition;
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            if (results.Count > 0)
            {
                Debug.Log($"UI Raycast hits: {results.Count}");
                foreach (var result in results)
                {
                    Debug.Log($"Hit: {result.gameObject.name}, layer: {result.gameObject.layer}");
                }
            }
            else
            {
                Debug.Log("No UI elements hit by raycast");
            }
        }
    }

    // Add this method to check if the joystick is properly set up in the scene
    private void OnValidate()
    {
        if (joystickBackground == null || joystickHandle == null)
        {
            Debug.LogWarning("OnScreenJoystick: Please assign joystickBackground and joystickHandle in the inspector!");
        }
    }

    // Add this method to check if the EventSystem is properly set up
    private void Start()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("No EventSystem found in the scene! UI interactions won't work.");
        }
        else
        {
            Debug.Log($"Using EventSystem: {EventSystem.current.name}");
        }
        
        // Check if this object has a proper RectTransform
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError("OnScreenJoystick: This component must be attached to a UI GameObject with a RectTransform!");
        }
        else
        {
            Debug.Log($"RectTransform size: {rt.rect.size}, position: {rt.position}");
        }
        
        // Check if the Canvas has a GraphicRaycaster
        GraphicRaycaster raycaster = canvas?.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogError("Canvas does not have a GraphicRaycaster component! UI interactions won't work.");
        }
        else
        {
            Debug.Log("Canvas has GraphicRaycaster: OK");
        }
    }

    // Add this method to test if the joystick can be manually shown
    public void ShowJoystick()
    {
        // Force show the joystick at a fixed position for testing
        joystickBackground.anchoredPosition = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
        SetJoystickVisibility(true);
        
        if (showDebugInfo)
        {
            Debug.Log("ShowJoystick() called - Manually showing joystick");
        }
    }

    // Add this helper method to handle direct input
    private void HandleDirectInput(Vector2 inputPosition)
    {
        // For Screen Space Overlay, we need to account for the joystick background's position
        Vector2 joystickCenter = RectTransformUtility.WorldToScreenPoint(null, joystickBackground.position);
        Vector2 direction = inputPosition - joystickCenter;
        
        // Calculate the maximum radius based on the background size
        float maxRadius = joystickBackground.sizeDelta.x * 0.5f;
        
        // Calculate normalized input
        if (direction.magnitude > maxRadius)
        {
            // Clamp to the edge of the joystick
            input = direction.normalized;
            joystickHandle.position = joystickCenter + direction.normalized * maxRadius;
        }
        else
        {
            // Scale input based on distance from center
            input = direction / maxRadius;
            joystickHandle.position = joystickCenter + direction;
        }
        
        // Apply deadzone
        if (input.magnitude < deadZone)
        {
            input = Vector2.zero;
            // Reset handle to center
            joystickHandle.position = joystickCenter;
        }
        else if (deadZone > 0)
        {
            // Rescale input after deadzone
            input = input.normalized * ((input.magnitude - deadZone) / (1 - deadZone));
        }
        
        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"Direct input: Joystick input: {input}, magnitude: {input.magnitude}");
            Debug.Log($"Joystick center: {joystickCenter}, Input pos: {inputPosition}, Direction: {direction}");
        }
    }

    // Add this method for fixed joystick input handling
    private void HandleFixedJoystickInput(Vector2 inputPosition)
    {
        Vector2 joystickCenter = RectTransformUtility.WorldToScreenPoint(null, joystickBackground.position);
        Vector2 direction = inputPosition - joystickCenter;
        
        float maxRadius = joystickBackground.sizeDelta.x * 0.5f;
        
        if (direction.magnitude > maxRadius)
        {
            input = direction.normalized;
            joystickHandle.position = joystickCenter + direction.normalized * maxRadius;
        }
        else
        {
            input = direction / maxRadius;
            joystickHandle.position = joystickCenter + direction;
        }
        
        // Apply deadzone
        if (input.magnitude < deadZone)
        {
            input = Vector2.zero;
            joystickHandle.position = joystickCenter;
        }
        else if (deadZone > 0)
        {
            input = input.normalized * ((input.magnitude - deadZone) / (1 - deadZone));
        }
    }

    // Add this method to adjust the transparency of the background image
    public void SetBackgroundTransparency(float alpha)
    {
        // Clamp alpha between 0 and 1
        alpha = Mathf.Clamp01(alpha);
        
        // Update colors
        backgroundNormalColor.a = alpha;
        backgroundActiveColor.a = alpha + 0.2f; // Make active slightly more visible
        
        // Apply to current state
        if (backgroundImage != null)
        {
            Color currentColor = backgroundImage.color;
            currentColor.a = isDragging ? backgroundActiveColor.a : backgroundNormalColor.a;
            backgroundImage.color = currentColor;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Background transparency set to {alpha}");
        }
    }
} 