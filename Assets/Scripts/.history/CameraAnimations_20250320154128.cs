using System.Collections;
using UnityEngine;

public class CameraAnimations : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 2.5f;
    [SerializeField] private float areaViewDuration = 3f;
    [SerializeField] private float returnDuration = 2f;
    [SerializeField] private float heightOffset = 15f;
    [SerializeField] private float lookDownAngle = 50f;
    
    [Header("Camera References")]
    [SerializeField] private CameraController playerCamera;
    [SerializeField] private Transform cameraTransform;
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isAnimating = false;
    private bool isLoadingData = false;
    
    // Add this event at the class level
    public delegate void CameraAnimationCompletedDelegate(GameObject areaTarget, int level, string areaName);
    public event CameraAnimationCompletedDelegate OnCameraAnimationCompleted;
    
    private void Start()
    {
        // Find references if not assigned
        if (playerCamera == null)
            playerCamera = FindObjectOfType<CameraController>();
            
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
            
        // Find and subscribe to the LevelAreaArrowManager
        LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
        if (arrowManager != null)
        {
            // Unsubscribe first to avoid multiple subscriptions
            arrowManager.OnAreaUnlocked -= OnAreaUnlocked;
            // Subscribe again
            arrowManager.OnAreaUnlocked += OnAreaUnlocked;
            Debug.Log("CameraAnimations: Successfully subscribed to LevelAreaArrowManager events");
        }
        else
        {
            Debug.LogWarning("CameraAnimations: Could not find LevelAreaArrowManager!");
        }
        
        // IMPORTANT: Subscribe our method to our own event to show attributes panel after animation
        OnCameraAnimationCompleted += ShowAttributesPanelAfterAnimation;
        
        Debug.Log("CameraAnimations initialized and event handlers connected");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
        if (arrowManager != null)
        {
            arrowManager.OnAreaUnlocked -= OnAreaUnlocked;
        }
        
        // IMPORTANT: Unsubscribe from our own event when destroyed
        OnCameraAnimationCompleted -= ShowAttributesPanelAfterAnimation;
    }
    
    // Event handler for when an area is unlocked
    private void OnAreaUnlocked(GameObject areaTarget, int level, string areaName)
    {
        // Skip animations if we're loading data
        if (isLoadingData)
            return;
        
        if (areaTarget != null)
        {
            StartCoroutine(AnimateCameraToArea(areaTarget.transform, level, areaName));
        }
    }
    
    // Public method to manually trigger camera animation to an area
    public void AnimateToArea(Transform areaTarget)
    {
        if (!isAnimating && areaTarget != null)
        {
            StartCoroutine(AnimateCameraToArea(areaTarget));
        }
    }
    
    private IEnumerator AnimateCameraToArea(Transform areaTarget, int level = 0, string areaName = "")
    {
        if (isAnimating || cameraTransform == null || playerCamera == null)
            yield break;
            
        isAnimating = true;
        
        // Disable the regular camera controller
        bool wasEnabled = playerCamera.enabled;
        playerCamera.enabled = false;
        
        // Store original camera position and rotation
        originalPosition = cameraTransform.position;
        originalRotation = cameraTransform.rotation;
        
        // Calculate position above the area
        Vector3 areaPosition = areaTarget.position;
        Vector3 targetPosition = new Vector3(areaPosition.x, areaPosition.y + heightOffset, areaPosition.z);
        
        // Calculate rotation to look down at the area
        Quaternion targetRotation = Quaternion.Euler(lookDownAngle, cameraTransform.rotation.eulerAngles.y, 0);
        
        // Animate to the area
        float timeElapsed = 0;
        while (timeElapsed < animationDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / animationDuration);
            
            // Use a smooth easing function
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // Move and rotate the camera
            cameraTransform.position = Vector3.Lerp(originalPosition, targetPosition, smoothT);
            cameraTransform.rotation = Quaternion.Slerp(originalRotation, targetRotation, smoothT);
            
            yield return null;
        }
        
        // RIGHT AFTER REACHING THE TARGET POSITION:
        // Find and fade out the matching border visualizers
        FadeBorderVisualizerForArea(areaTarget.gameObject, level);
        
        // Hold at the area view position
        yield return new WaitForSeconds(areaViewDuration);
        
        // Slowly pan around (optional)
        float panStartTime = Time.time;
        float panDuration = areaViewDuration * 0.8f;
        
        while (Time.time - panStartTime < panDuration)
        {
            // Rotate slowly around the area
            cameraTransform.RotateAround(
                areaPosition,
                Vector3.up,
                20f * Time.deltaTime
            );
            
            yield return null;
        }
        
        // Return to the original position
        timeElapsed = 0;
        Vector3 currentPos = cameraTransform.position;
        Quaternion currentRot = cameraTransform.rotation;
        
        while (timeElapsed < returnDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / returnDuration);
            
            // Use a smooth easing function
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // Move and rotate the camera back
            cameraTransform.position = Vector3.Lerp(currentPos, originalPosition, smoothT);
            cameraTransform.rotation = Quaternion.Slerp(currentRot, originalRotation, smoothT);
            
            yield return null;
        }
        
        // Re-enable the camera controller
        playerCamera.enabled = wasEnabled;
        isAnimating = false;
        
        // Fire the completion event - this will now handle showing the attributes panel
        if (OnCameraAnimationCompleted != null && areaTarget != null)
        {
            OnCameraAnimationCompleted(areaTarget.gameObject, level, areaName);
        }
    }
    
    // Public method to show a specific area by level
    public void ShowAreaByLevel(int level)
    {
        LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
        if (arrowManager != null)
        {
            // Access the levelAreas field using reflection
            var areasField = typeof(LevelAreaArrowManager).GetField("levelAreas", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            if (areasField != null)
            {
                var areas = areasField.GetValue(arrowManager) as LevelAreaArrowManager.LevelAreaTarget[];
                if (areas != null)
                {
                    foreach (var area in areas)
                    {
                        if (area.requiredLevel == level && area.areaTarget != null)
                        {
                            StartCoroutine(AnimateCameraToArea(area.areaTarget.transform));
                            break;
                        }
                    }
                }
            }
        }
    }

    public void SetLoadingState(bool loading)
    {
        isLoadingData = loading;
    }

    // Add this method to check if a new area is unlocked at the given level
    public bool IsNewAreaUnlockedAtLevel(int level)
    {
        LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
        if (arrowManager != null)
        {
            // Access the levelAreas field using reflection
            var areasField = typeof(LevelAreaArrowManager).GetField("levelAreas", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            if (areasField != null)
            {
                var areas = areasField.GetValue(arrowManager) as LevelAreaArrowManager.LevelAreaTarget[];
                if (areas != null)
                {
                    foreach (var area in areas)
                    {
                        if (area.requiredLevel == level && area.areaTarget != null)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    // Add this helper method to properly sequence the showing of the attributes panel
    public void ShowAttributesPanelAfterAnimation(GameObject areaTarget, int level, string areaName)
    {
        // Show the attributes panel after the camera animation is complete
        AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
        if (attributeDisplay != null)
        {
            Debug.Log("Camera animation completed, showing attributes panel");
            attributeDisplay.ShowPanel(true);
        }
        else
        {
            // Fallback to directly showing the panel via UIHelper
            UIHelper uiHelper = FindObjectOfType<UIHelper>();
            if (uiHelper != null && uiHelper.attributesPanel != null)
            {
                Debug.Log("Camera animation completed, showing attributes panel via UIHelper");
                uiHelper.attributesPanel.SetActive(true);
                Animator panelAnimator = uiHelper.attributesPanel.GetComponent<Animator>();
                if (panelAnimator != null)
                {
                    panelAnimator.SetBool("ShowUp", true);
                    panelAnimator.SetBool("Hide", false);
                }
            }
        }
    }

    private void FadeBorderVisualizerForArea(GameObject areaTarget, int playerLevel)
    {
        // Try to get the border name from the area target
        string borderName = GetBorderNameFromArea(areaTarget);
        
        // Find all border visualizers in the scene
        BorderVisualizer[] borders = FindObjectsOfType<BorderVisualizer>();
        
        foreach (var border in borders)
        {
            // Case 1: If we found a specific border name from the area, use direct matching
            if (!string.IsNullOrEmpty(borderName) && border.gameObject.name == borderName)
            {
                Debug.Log($"Found exact border match: {borderName} - fading out");
                border.FadeOutAndDisable();
                continue;
            }
            
            // Case 2: Match by required level
            int requiredLevel = border.GetRequiredLevel();
            if (requiredLevel > 0 && requiredLevel <= playerLevel)
            {
                // This is a border the player should be able to cross - fade it out
                Debug.Log($"Fading out border {border.gameObject.name} during area reveal (level requirement: {requiredLevel})");
                border.FadeOutAndDisable();
            }
        }
    }

    // Helper method to determine which border corresponds to an area
    private string GetBorderNameFromArea(GameObject areaTarget)
    {
        if (areaTarget == null) return string.Empty;
        
        // Try to get area name from the area target
        string areaName = areaTarget.name;
        
        // Simple mapping from area name to border name
        // You may need to adjust this based on your specific naming convention
        if (areaName.Contains("Area2")) return "MapBorder2";
        if (areaName.Contains("Area3")) return "MapBorder3";
        if (areaName.Contains("Area4")) return "MapBorder4";
        if (areaName.Contains("Area5")) return "MapBorder5";
        if (areaName.Contains("Area6")) return "MapBorder6";
        if (areaName.Contains("Area7")) return "MapBorder7";
        if (areaName.Contains("Area9")) return "MapBorder9";
        if (areaName.Contains("Area10")) return "MapBorder10";
        if (areaName.Contains("Area11")) return "MapBorder11";
        if (areaName.Contains("Area12")) return "MapBorder12";
        
        // Additional approach: try to find border by proximity
        float closestDistance = float.MaxValue;
        string closestBorder = string.Empty;
        
        foreach (var border in FindObjectsOfType<BorderVisualizer>())
        {
            if (border.gameObject.name.StartsWith("MapBorder"))
            {
                float distance = Vector3.Distance(border.transform.position, areaTarget.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBorder = border.gameObject.name;
                }
            }
        }
        
        // If the closest border is within a reasonable distance (20 units), return it
        if (closestDistance < 20f)
            return closestBorder;
        
        return string.Empty;
    }
} 