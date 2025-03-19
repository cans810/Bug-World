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
            // Use reflection to access the private OnPlayerLevelUp method
            var levelUpEvent = typeof(LevelAreaArrowManager).GetField("OnLevelUp", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            // Alternatively, add a public method to LevelAreaArrowManager for this:
            // arrowManager.RegisterForAreaUnlocked(OnAreaUnlocked);
        }
        
        // Since we can't easily hook into the existing event, let's subscribe to PlayerInventory's level up event
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.OnLevelUp += OnPlayerLevelUp;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.OnLevelUp -= OnPlayerLevelUp;
        }
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        // Find the area that was just unlocked
        LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
        if (arrowManager != null)
        {
            // Access the levelAreas field using reflection since it's private
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
                        if (area.requiredLevel == newLevel && !area.hasBeenVisited && area.areaTarget != null)
                        {
                            // Found the area - animate to it
                            StartCoroutine(AnimateCameraToArea(area.areaTarget.transform));
                            break;
                        }
                    }
                }
            }
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
    
    private IEnumerator AnimateCameraToArea(Transform areaTarget)
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
} 