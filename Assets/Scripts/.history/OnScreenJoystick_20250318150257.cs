using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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
    
    [Header("Interaction Settings")]
    [SerializeField] private bool ignoreUIElements = true;
    [SerializeField] private LayerMask uiLayerMask = -1; // Default to "UI" layer
    [SerializeField] private string[] touchableUILayers = new string[] { "TouchableUI", "PlayerNest1" }; // UI elements on these layers can be touched through
    [SerializeField] private bool debugLayers = false; // Set to true to log layer info
    
    // Private variables
    private Vector2 input = Vector2.zero;
    private Vector2 touchStartPosition;
    private Image backgroundImage;
    private Image handleImage;
    private Canvas canvas;
    private Camera uiCamera;
    private bool isDragging = false;
    private RectTransform canvasRectTransform;
    private int activeTouchId = -1;
    
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
            enabled = false;
            return;
        }
        
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        
        // Validate required components
        if (joystickBackground == null)
        {
            enabled = false;
            return;
        }
        
        if (joystickHandle == null)
        {
            enabled = false;
            return;
        }
        
        backgroundImage = joystickBackground.GetComponent<Image>();
        handleImage = joystickHandle.GetComponent<Image>();
        
        if (backgroundImage == null)
        {
            enabled = false;
            return;
        }
        
        if (handleImage == null)
        {
            enabled = false;
            return;
        }
        
        // Get the UI camera if the canvas is in Screen Space - Camera or World Space
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
            if (uiCamera == null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
            }
        }
        
        // Hide joystick initially
        SetJoystickVisibility(false);
        
        // Set initial colors
        if (backgroundImage != null) backgroundImage.color = backgroundNormalColor;
        if (handleImage != null) handleImage.color = handleNormalColor;
        
        if (showDebugInfo)
        {

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
            
        }
        
        // Process the initial drag
        OnDrag(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) 
        {
            if (showDebugInfo) 
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
            }
        }
        else
        {
            if (showDebugInfo) 
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (showDebugInfo)
        {
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
            }
        }
        
        if (handleImage != null) 
        {
            // Force enable/disable the GameObject itself, not just the Image component
            joystickHandle.gameObject.SetActive(visible);
            handleImage.enabled = visible;
            
            if (showDebugInfo && visible != handleImage.enabled)
            {
            }
        }
        
        if (showDebugInfo)
        {
        }
    }
    
    // Add this to ensure the joystick is properly visible in the scene
    private void OnEnable()
    {
        if (showDebugInfo)
        {
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
                // Find our active touch if we're already dragging
                if (isDragging && activeTouchId >= 0)
                {
                    bool foundActiveTouch = false;
                    for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                    {
                        Touch touch = UnityEngine.Input.GetTouch(i);
                        if (touch.fingerId == activeTouchId)
                        {
                            foundActiveTouch = true;
                            
                            // Handle ongoing touch
                            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                            {
                                HandleDirectInput(touch.position);
                            }
                            // Handle touch end
                            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                            {
                                if (showDebugInfo)
                                {
                                }
                                
                                // Reset joystick
                                isDragging = false;
                                activeTouchId = -1;
                                input = Vector2.zero;
                                
                                // Hide joystick if not using fixed position
                                if (!useFixedPosition)
                                {
                                    SetJoystickVisibility(false);
                                }
                                else
                                {
                                    // Reset handle to center for fixed joystick
                                    joystickHandle.position = joystickBackground.position;
                                }
                                
                                // Reset colors
                                if (backgroundImage != null) backgroundImage.color = backgroundNormalColor;
                                if (handleImage != null) handleImage.color = handleNormalColor;
                            }
                            break;
                        }
                    }
                    
                    // If we lost our active touch somehow, reset the joystick
                    if (!foundActiveTouch)
                    {
                        isDragging = false;
                        activeTouchId = -1;
                        input = Vector2.zero;
                        
                        // Hide joystick if not using fixed position
                        if (!useFixedPosition)
                        {
                            SetJoystickVisibility(false);
                        }
                        else
                        {
                            // Reset handle to center for fixed joystick
                            joystickHandle.position = joystickBackground.position;
                        }
                        
                        // Reset colors
                        if (backgroundImage != null) backgroundImage.color = backgroundNormalColor;
                        if (handleImage != null) handleImage.color = handleNormalColor;
                    }
                }
                // If we're not dragging, check for new touches
                else if (!isDragging)
                {
                    // Process all touches to find one we can use
                    for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                    {
                        Touch touch = UnityEngine.Input.GetTouch(i);
                        
                        // Only consider new touches
                        if (touch.phase != TouchPhase.Began)
                            continue;
                            
                        // Check if this touch is on a UI element we should ignore
                        bool touchingOtherUI = false;
                        
                        if (ignoreUIElements)
                        {
                            PointerEventData pointerData = new PointerEventData(EventSystem.current);
                            pointerData.position = touch.position;
                            
                            List<RaycastResult> results = new List<RaycastResult>();
                            EventSystem.current.RaycastAll(pointerData, results);
                            
                            foreach (var result in results)
                            {
                                // Skip our own joystick elements
                                if (result.gameObject == gameObject || 
                                    result.gameObject == joystickBackground.gameObject || 
                                    result.gameObject == joystickHandle.gameObject)
                                {
                                    continue;
                                }
                                
                                // Check if this element is on any of the touchable layers
                                bool isOnTouchableLayer = false;
                                string layerName = LayerMask.LayerToName(result.gameObject.layer);
                                
                                foreach (string touchableLayer in touchableUILayers)
                                {
                                    if (layerName == touchableLayer)
                                    {
                                        isOnTouchableLayer = true;
                                        if (debugLayers)
                                        {
                                        }
                                        break;
                                    }
                                }
                                
                                if (isOnTouchableLayer)
                                {
                                    continue; // Skip this UI element, allow joystick to be activated
                                }
                                
                                // Check if this is a UI element we should ignore
                                if (((1 << result.gameObject.layer) & uiLayerMask) != 0)
                                {
                                    if (showDebugInfo)
                                    {
                                        if (debugLayers)
                                        {
                                        }
                                    }
                                    touchingOtherUI = true;
                                    break;
                                }
                            }
                        }
                        
                        // If this touch is valid, start dragging
                        if (!touchingOtherUI)
                        {
                            // Set this as our active touch
                            activeTouchId = touch.fingerId;
                            isDragging = true;
                            
                            // Position joystick
                            joystickBackground.position = touch.position;
                            joystickHandle.position = touch.position;
                            
                            // Show joystick
                            SetJoystickVisibility(true);
                            
                            // Set colors
                            if (backgroundImage != null) backgroundImage.color = backgroundActiveColor;
                            if (handleImage != null) handleImage.color = handleActiveColor;
                            
                            // We found a valid touch, no need to check others
                            break;
                        }
                    }
                }
            }
            
            // Handle mouse input (for testing in editor)
            #if UNITY_EDITOR
            if (UnityEngine.Input.GetMouseButtonDown(0) && !isDragging)
            {
                // Check if clicking on UI element
                bool clickingOtherUI = false;
                
                if (ignoreUIElements)
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current);
                    pointerData.position = UnityEngine.Input.mousePosition;
                    
                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);
                    
                    foreach (var result in results)
                    {
                        // Skip our own joystick elements
                        if (result.gameObject == gameObject || 
                            result.gameObject == joystickBackground.gameObject || 
                            result.gameObject == joystickHandle.gameObject)
                        {
                            continue;
                        }
                        
                        // Check if this element is on any of the touchable layers
                        bool isOnTouchableLayer = false;
                        string layerName = LayerMask.LayerToName(result.gameObject.layer);
                        
                        foreach (string touchableLayer in touchableUILayers)
                        {
                            if (layerName == touchableLayer)
                            {
                                isOnTouchableLayer = true;
                                if (debugLayers)
                                {
                                }
                                break;
                            }
                        }
                        
                        if (isOnTouchableLayer)
                        {
                            continue; // Skip this UI element, allow joystick to be activated
                        }
                        
                        // Check if this is a UI element we should ignore
                        if (((1 << result.gameObject.layer) & uiLayerMask) != 0)
                        {
                            if (showDebugInfo)
                            {
                            }
                            clickingOtherUI = true;
                            break;
                        }
                    }
                }
                
                if (!clickingOtherUI)
                {
                    isDragging = true;
                    
                    // Position joystick
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
                // Reset joystick
                isDragging = false;
                input = Vector2.zero;
                
                // Hide joystick if not using fixed position
                if (!useFixedPosition)
                {
                    SetJoystickVisibility(false);
                }
                else
                {
                    // Reset handle to center for fixed joystick
                    joystickHandle.position = joystickBackground.position;
                }
                
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
                    break;
                }
            }
            
            if (!hitThis)
            {
            }
        }
        
        // Check for touches and mouse input
        if (showDebugInfo)
        {
            if (UnityEngine.Input.touchCount > 0 && !isDragging)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                
                // Try to determine if the touch should be hitting this UI element
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    GetComponent<RectTransform>(),
                    touch.position,
                    uiCamera,
                    out localPoint
                );
            }
            
            if (UnityEngine.Input.GetMouseButton(0) && !isDragging)
            {
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
                foreach (var result in results)
                {
                }
            }
            else
            {
            }
        }
    }

    // Add this method to check if the joystick is properly set up in the scene
    private void OnValidate()
    {
        if (joystickBackground == null || joystickHandle == null)
        {
        }
    }

    // Add this method to check if the EventSystem is properly set up
    private void Start()
    {
        if (EventSystem.current == null)
        {
        }
        else
        {
        }
        
        // Check if this object has a proper RectTransform
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null)
        {
        }
        else
        {
        }
        
        // Check if the Canvas has a GraphicRaycaster
        GraphicRaycaster raycaster = canvas?.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
        }
        else
        {
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
        }
    }

    // Add this helper method to handle direct input
    private void HandleDirectInput(Vector2 inputPosition)
    {
        // For Screen Space Overlay, we need to account for the joystick background's position
        Vector2 joystickCenter = RectTransformUtility.WorldToScreenPoint(null, joystickBackground.position);
        Vector2 direction = inputPosition - joystickCenter;
        
        // Calculate the maximum radius based on the background size
        float maxRadius = joystickBackground.sizeDelta.x * 2f;
        
        // Calculate normalized input (always keep input normalized between -1 and 1)
        if (direction.magnitude > maxRadius)
        {
            // Input is normalized but handle can move beyond the background
            input = direction.normalized;
            
            // Allow the handle to move further than the background edge if desired
            float handleExtension = 1.2f; // Handle can go 20% beyond the visual radius
            joystickHandle.position = joystickCenter + direction.normalized * Mathf.Min(direction.magnitude, maxRadius * handleExtension);
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
        }
    }

    // Add this method for fixed joystick input handling
    private void HandleFixedJoystickInput(Vector2 inputPosition)
    {
        Vector2 joystickCenter = RectTransformUtility.WorldToScreenPoint(null, joystickBackground.position);
        Vector2 direction = inputPosition - joystickCenter;
        
        // Increase the radius to match HandleDirectInput
        float maxRadius = joystickBackground.sizeDelta.x * 0.75f;
        
        if (direction.magnitude > maxRadius)
        {
            input = direction.normalized;
            
            // Allow the handle to move further than the background edge
            float handleExtension = 1.2f;
            joystickHandle.position = joystickCenter + direction.normalized * Mathf.Min(direction.magnitude, maxRadius * handleExtension);
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
        
        // Update only background colors
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

    // Add a method to set handle transparency separately
    public void SetHandleTransparency(float alpha)
    {
        // Clamp alpha between 0 and 1
        alpha = Mathf.Clamp01(alpha);
        
        // Update handle colors
        handleNormalColor.a = alpha;
        handleActiveColor.a = alpha;
        
        // Apply to current state
        if (handleImage != null)
        {
            Color currentColor = handleImage.color;
            currentColor.a = alpha;
            handleImage.color = currentColor;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Handle transparency set to {alpha}");
        }
    }

    // Add this method to help debug sorting layers
    public void LogSortingLayers()
    {
        // Get all sorting layers
        string[] sortingLayers = SortingLayer.layers.Select(l => l.name).ToArray();
        
        Debug.Log("Available sorting layers:");
        foreach (string layer in sortingLayers)
        {
            Debug.Log($"- {layer}");
        }
        
        // Find objects with SpriteRenderers in the scene
        SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>();
        Debug.Log($"Found {renderers.Length} SpriteRenderers in the scene:");
        
        foreach (SpriteRenderer renderer in renderers)
        {
            Debug.Log($"- {renderer.gameObject.name}: Layer={renderer.sortingLayerName}, Order={renderer.sortingOrder}");
        }
    }
} 