using System.Collections;
using UnityEngine;
using System.Collections.Generic;

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
    
    [Header("Map View Settings")]
    [SerializeField] private float mapViewHeight = 10f; // Reduced from 60f to keep camera closer
    [SerializeField] private float mapViewDuration = 1.5f; // Time to transition to map view
    [SerializeField] private float mapCameraAngle = 15f; // Reduced from 75f for better perspective
    
    [Header("Map View Drag Settings")]
    [SerializeField] private float dragSpeed = 2.0f; // How fast the camera moves when dragged
    [SerializeField] private float mapBoundaryRadius = 100f; // Maximum distance from original position
    [SerializeField] private bool enableMapDragging = true; // Toggle to enable/disable dragging
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isAnimating = false;
    private bool isLoadingData = false;
    
    // Add this event at the class level
    public delegate void CameraAnimationCompletedDelegate(GameObject areaTarget, int level, string areaName);
    public event CameraAnimationCompletedDelegate OnCameraAnimationCompleted;
    
    // Add these fields at the class level
    private Queue<AnimationRequest> animationQueue = new Queue<AnimationRequest>();
    private bool isProcessingQueue = false;

    private Vector3 preMappingCameraPosition; // Store camera position before mapping
    private Quaternion preMappingCameraRotation; // Store camera rotation before mapping
    private bool isInMapView = false; // Track if we're currently in map view

    private Vector3 dragStartPosition; // Mouse/touch position when drag starts
    private Vector3 originalMapPosition; // Original camera position in map view
    private bool isDragging = false;

    // Define a struct to hold animation request data
    private struct AnimationRequest
    {
        public Transform target;
        public int level;
        public string areaName;
        public bool isEgg;

        public AnimationRequest(Transform target, int level = 0, string areaName = "")
        {
            this.target = target;
            this.level = level;
            this.areaName = areaName;
            this.isEgg = false; // Default is false
        }
    }
    
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
            Debug.Log("CameraAnimations: Successfully found LevelAreaArrowManager. Subscribing to OnAreaUnlocked event.");
            arrowManager.OnAreaUnlocked += OnAreaUnlocked;
        }
        else
        {
            Debug.LogError("CameraAnimations: Failed to find LevelAreaArrowManager! Camera animations for new areas won't work.");
        }
        
        Debug.Log("CameraAnimations initialized and event handlers connected");
    }
    
    private void OnEnable()
    {
        // Find and subscribe to the LevelAreaArrowManager (do this in OnEnable instead of Start)
        LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
        if (arrowManager != null)
        {
            Debug.Log("CameraAnimations: Subscribing to OnAreaUnlocked event in OnEnable");
            arrowManager.OnAreaUnlocked += OnAreaUnlocked;
        }
        else
        {
            Debug.LogError("CameraAnimations: Failed to find LevelAreaArrowManager in OnEnable!");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        LevelAreaArrowManager arrowManager = FindObjectOfType<LevelAreaArrowManager>();
        if (arrowManager != null)
        {
            arrowManager.OnAreaUnlocked -= OnAreaUnlocked;
        }
    }
    
    // Event handler for when an area is unlocked
    public void OnAreaUnlocked(GameObject areaTarget, int level, string areaName)
    {
        // Skip animations if we're loading data
        if (isLoadingData)
        {
            Debug.Log($"CameraAnimations: Skipping animation while loading data for area {areaName} (level {level})");
            return;
        }
        
        // We no longer trigger animations automatically here
        // Instead, we'll just log that an area was unlocked, but animation is delayed
        Debug.Log($"CameraAnimations: Area unlocked: {areaName} (level {level}) - waiting for player to claim reward");
        
        // The animation will be triggered from LevelUpPanelHelper when player clicks "Claim" button
    }
    
    // Public method to manually trigger camera animation to an area
    public void AnimateToArea(Transform areaTarget, int level = 0, string areaName = "")
    {
        if (areaTarget == null)
        {
            Debug.LogWarning("Cannot animate to null target");
            return;
        }

        // Create a new animation request
        AnimationRequest request = new AnimationRequest(areaTarget, level, areaName);
        
        // Add to queue
        animationQueue.Enqueue(request);
        Debug.Log($"Added animation request to queue for {(string.IsNullOrEmpty(areaName) ? areaTarget.name : areaName)}");
        
        // Start processing the queue if not already processing
        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessAnimationQueue());
        }
    }
    
    // Add this method to process the animation queue
    private IEnumerator ProcessAnimationQueue()
    {
        // Prevent re-entry if already processing
        if (isProcessingQueue)
        {
            Debug.LogWarning("CameraAnimations: Attempted to process queue while already processing!");
            yield break;
        }
        
        isProcessingQueue = true;
        int safetyCounter = 0; // To prevent infinite loops
        
        while (animationQueue.Count > 0 && safetyCounter < 10) // Safety limit
        {
            // Get next request
            AnimationRequest request = animationQueue.Dequeue();
            
            // Skip if target is null
            if (request.target == null)
            {
                Debug.LogWarning("CameraAnimations: Skipping null target in animation queue");
                continue;
            }
            
            // Process animation
            Debug.Log($"Processing animation request for {(string.IsNullOrEmpty(request.areaName) ? request.target.name : request.areaName)}");
            yield return StartCoroutine(AnimateCameraToArea(request.target, request.level, request.areaName, request.isEgg));
            
            // Add a small delay between animations
            yield return new WaitForSeconds(0.5f);
            
            safetyCounter++;
        }
        
        // Safety check - log warning if we hit the limit
        if (safetyCounter >= 10)
        {
            Debug.LogWarning("CameraAnimations: Hit safety limit in animation queue processing!");
        }
        
        isProcessingQueue = false;
    }
    
    private IEnumerator AnimateCameraToArea(Transform areaTarget, int level = 0, string areaName = "", bool isEgg = false)
    {
        // Safety check to prevent camera freeze if target is null
        if (areaTarget == null)
        {
            Debug.LogWarning("CameraAnimations: Attempted to animate to null target");
            yield break;
        }
        
        if (cameraTransform == null || playerCamera == null)
        {
            Debug.LogWarning($"CameraAnimations: Cannot animate - cameraTransform: {(cameraTransform == null ? "null" : "valid")}, playerCamera: {(playerCamera == null ? "null" : "valid")}");
            yield break;
        }
            
        Debug.Log($"CameraAnimations: Starting camera animation to {(string.IsNullOrEmpty(areaName) ? areaTarget.name : areaName)}");
        
        // If already animating, return
        if (isAnimating)
        {
            Debug.LogWarning("CameraAnimations: Attempted to start animation while already animating");
            yield break;
        }
        
        isAnimating = true;
        
        // Disable the regular camera controller
        bool wasEnabled = playerCamera.enabled;
        playerCamera.enabled = false;
        
        // Store original camera position and rotation
        originalPosition = cameraTransform.position;
        originalRotation = cameraTransform.rotation;
        
        // Use different settings for eggs vs areas
        float useHeightOffset = isEgg ? 2f : heightOffset;  // Even lower for eggs (2 instead of 3)
        float useLookDownAngle = isEgg ? 90f : lookDownAngle; // 90 degrees (straight down) for eggs
        float useAnimationDuration = isEgg ? 1.0f : animationDuration; // Faster for eggs
        float useAreaViewDuration = isEgg ? 3.0f : areaViewDuration; // Slightly longer viewing time
        
        // Calculate position above the target
        Vector3 areaPosition = areaTarget.position;
        Vector3 targetPosition = new Vector3(areaPosition.x, areaPosition.y + useHeightOffset, areaPosition.z);
        
        // Calculate rotation to look down at the target
        Quaternion targetRotation = Quaternion.Euler(useLookDownAngle, cameraTransform.rotation.eulerAngles.y, 0);
        
        // Animate to the target
        float timeElapsed = 0;
        while (timeElapsed < useAnimationDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / useAnimationDuration);
            
            // Use a smooth easing function
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // Move and rotate the camera
            cameraTransform.position = Vector3.Lerp(originalPosition, targetPosition, smoothT);
            cameraTransform.rotation = Quaternion.Slerp(originalRotation, targetRotation, smoothT);
            
            yield return null;
        }
        
        yield return new WaitForSeconds(useAreaViewDuration);
        
        // Return to the original position
        timeElapsed = 0;
        Vector3 currentPos = cameraTransform.position;
        Quaternion currentRot = cameraTransform.rotation;
        
        // Use a faster return for eggs
        float useReturnDuration = isEgg ? returnDuration * 0.7f : returnDuration;
        
        while (timeElapsed < useReturnDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / useReturnDuration);
            
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
        
        // IMPORTANT: Directly notify AttributeDisplay that animation has ended
        AttributeDisplay attributeDisplay = FindObjectOfType<AttributeDisplay>();
        if (attributeDisplay != null)
        {
            attributeDisplay.NotifyCameraAnimationEnded();
        }
        
        // Also notify other subscribers
        if (OnCameraAnimationCompleted != null)
        {
            Debug.Log($"CameraAnimations: Animation completed, invoking OnCameraAnimationCompleted for {areaName}");
            OnCameraAnimationCompleted(areaTarget.gameObject, level, areaName);
        }
        
        yield break;
    }
    
    // Public method to show a specific area by level
    public void ShowAreaByLevel(int level)
    {
        Debug.Log($"CameraAnimations: ShowAreaByLevel called for level {level}");
        
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
                    bool areaFound = false;
                    foreach (var area in areas)
                    {
                        if (area.requiredLevel == level && area.areaTarget != null)
                        {
                            Debug.Log($"CameraAnimations: Found matching area at level {level}: {area.areaName}");
                            areaFound = true;
                            StartCoroutine(AnimateCameraToArea(area.areaTarget.transform, level, area.areaName));
                            break;
                        }
                    }
                    
                    if (!areaFound)
                    {
                        Debug.LogWarning($"CameraAnimations: No area found at level {level}");
                    }
                }
                else
                {
                    Debug.LogError("CameraAnimations: Failed to access areas array through reflection");
                }
            }
            else
            {
                Debug.LogError("CameraAnimations: Failed to access levelAreas field through reflection");
            }
        }
        else
        {
            Debug.LogError("CameraAnimations: LevelAreaArrowManager not found!");
        }
    }

    public void SetLoadingState(bool loading)
    {
        isLoadingData = loading;
    }

    // Add this method to check if a new area is unlocked at the given level
    public bool IsNewAreaUnlockedAtLevel(int level)
    {
        Debug.Log($"CameraAnimations: Checking if new area is unlocked at level {level}");
        
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
                        if (area.requiredLevel == level && area.areaTarget != null && !area.hasBeenVisited)
                        {
                            Debug.Log($"CameraAnimations: Found unvisited area at level {level}: {area.areaName}");
                            return true;
                        }
                    }
                    Debug.Log($"CameraAnimations: No unvisited areas found at level {level}");
                }
            }
        }
        else
        {
            Debug.LogError("CameraAnimations: LevelAreaArrowManager not found!");
        }
        
        return false;
    }

    // Add this helper method to properly sequence the showing of the attributes panel
    public bool IsAnimationInProgress()
    {
        return isAnimating;
    }


    // Add this method to find and remove the border for a newly unlocked area
    private void RemoveBorderForArea(GameObject areaTarget, int level)
    {
        // Find all border visualizers in the scene
        BorderVisualizer[] allBorders = FindObjectsOfType<BorderVisualizer>();
        
        // Pattern to match area names with border names
        // If areaTarget is "Area2" we want to match "MapBorder2", etc.
        string areaName = areaTarget.name;
        string areaNumber = System.Text.RegularExpressions.Regex.Match(areaName, @"\d+").Value;
        
        Debug.Log($"Looking for borders matching area {areaName} (number: {areaNumber})");
        
        bool foundMatchingBorder = false;
        
        foreach (BorderVisualizer border in allBorders)
        {
            string borderName = border.gameObject.name;
            bool isMatch = false;
            
            // Pattern 1: Direct number match (Area2 → MapBorder2)
            if (!string.IsNullOrEmpty(areaNumber))
            {
                string borderNumber = System.Text.RegularExpressions.Regex.Match(borderName, @"\d+").Value;
                if (borderNumber == areaNumber)
                {
                    isMatch = true;
                    Debug.Log($"Found matching border by number: {borderName}");
                }
            }
            
            // Pattern 2: Check if border's required level matches this level
            if (border.GetRequiredLevel() == level)
            {
                isMatch = true;
                Debug.Log($"Found matching border by level: {borderName} (level {level})");
            }
            
            // If we found a match, fade out and disable this border
            if (isMatch)
            {
                foundMatchingBorder = true;
            }
        }
        
        if (!foundMatchingBorder)
        {
            Debug.Log($"No matching border found for area {areaName} at level {level}");
        }
    }

    // Add this method to handle egg-specific camera animation
    public void AnimateToEgg(Transform eggTransform)
    {
        if (eggTransform == null)
        {
            Debug.LogWarning("Cannot animate to null egg");
            return;
        }
        
        // Create a new animation request with special parameters for eggs
        AnimationRequest request = new AnimationRequest(eggTransform, 0, "Egg");
        request.isEgg = true; // Mark as an egg for special handling
        
        // Add to queue
        animationQueue.Enqueue(request);
        Debug.Log($"Added egg-specific animation request to queue for {eggTransform.name}");
        
        // Start processing the queue if not already processing
        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessAnimationQueue());
        }
    }

    // Add this helper method to check if an area is already in the queue
    private bool IsAreaAlreadyInQueue(GameObject areaTarget)
    {
        foreach (var request in animationQueue)
        {
            if (request.target != null && request.target.gameObject == areaTarget)
            {
                return true;
            }
        }
        return false;
    }

    // Modify the method to accept a callback for when camera is in position
    public void AnimateMetamorphosis(Transform playerTransform, System.Action onCameraInPosition = null)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("Cannot animate metamorphosis with null player transform");
            return;
        }

        // Start the metamorphosis animation coroutine with callback
        StartCoroutine(MetamorphosisCameraAnimation(playerTransform, onCameraInPosition));
    }

    // Update the coroutine to disable player controls during animation
    private IEnumerator MetamorphosisCameraAnimation(Transform playerTransform, System.Action onCameraInPosition)
    {
        // If already animating, return
        if (isAnimating)
        {
            Debug.LogWarning("CameraAnimations: Attempted to start metamorphosis animation while already animating");
            yield break;
        }

        isAnimating = true;
        
        // Disable player controls
        PlayerController playerController = playerTransform.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetControlsEnabled(false);
            Debug.Log("Disabled player controls for metamorphosis animation");
        }

        // Disable the regular camera controller
        bool wasEnabled = playerCamera.enabled;
        playerCamera.enabled = false;

        // Store original camera position and rotation
        originalPosition = cameraTransform.position;
        originalRotation = cameraTransform.rotation;

        // CORRECTED: Position camera to see the FRONT of the player
        Vector3 playerForward = playerTransform.forward;
        Vector3 playerUp = Vector3.up;
        
        // Calculate positioning values based on player scale
        float forwardOffset = playerTransform.localScale.z * 5.0f; // Distance for front view
        float heightOffset = playerTransform.localScale.y * 4.0f;  // Height for looking down
        
        // Position camera in front of the player (using positive forward direction)
        // This will position the camera where the ant is facing/looking
        Vector3 horizontalOffset = playerForward * forwardOffset; 
        Vector3 targetPosition = playerTransform.position + horizontalOffset;
        targetPosition.y = playerTransform.position.y + heightOffset;
        
        // Calculate rotation to look back at the player from the front
        Quaternion targetRotation = Quaternion.LookRotation(
            playerTransform.position - targetPosition, // Direction to look at player
            Vector3.up // Keep camera upright
        );

        // Move to the view position over time
        float timeElapsed = 0f;
        float frontViewDuration = 1.5f; // Shorter than regular animations

        // Camera shake parameters
        float shakeIntensity = 0.05f; // Subtle shake
        float shakeFrequency = 10f; // Higher frequency for smoother shake
        Vector3 shakeOffset = Vector3.zero;

        while (timeElapsed < frontViewDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / frontViewDuration);
            
            // Use a smooth easing function
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // Calculate base position and rotation
            Vector3 basePosition = Vector3.Lerp(originalPosition, targetPosition, smoothT);
            Quaternion baseRotation = Quaternion.Slerp(originalRotation, targetRotation, smoothT);
            
            // Add slight camera shake that increases during the transition
            float currentShakeIntensity = shakeIntensity * smoothT; // Gradually increase shake
            
            // Generate smooth random shake offset
            shakeOffset = Vector3.Lerp(
                shakeOffset, 
                new Vector3(
                    (Mathf.PerlinNoise(Time.time * shakeFrequency, 0) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0, Time.time * shakeFrequency) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(Time.time * shakeFrequency, Time.time * shakeFrequency) - 0.5f) * 2f
                ) * currentShakeIntensity,
                Time.deltaTime * 8f // Smooth factor
            );
            
            // Apply position with shake and rotation
            cameraTransform.position = basePosition + shakeOffset;
            cameraTransform.rotation = baseRotation;
            
            yield return null;
        }

        // We've reached the viewing position - trigger the size-up now!
        if (onCameraInPosition != null)
        {
            onCameraInPosition.Invoke();
            Debug.Log("Camera in position - triggering size-up effect");
        }

        // Hold the view for a moment with continued subtle shake
        float holdDuration = 1.5f; // Show the player for this long
        float holdTimer = 0f;
        
        while (holdTimer < holdDuration)
        {
            holdTimer += Time.deltaTime;
            float holdProgress = holdTimer / holdDuration;
            
            // Calculate shake that gradually reduces as we approach the end of hold time
            float currentShakeIntensity = shakeIntensity * (1f - holdProgress * 0.7f);
            
            // Generate smooth random shake offset
            shakeOffset = Vector3.Lerp(
                shakeOffset, 
                new Vector3(
                    (Mathf.PerlinNoise(Time.time * shakeFrequency, 0) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0, Time.time * shakeFrequency) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(Time.time * shakeFrequency, Time.time * shakeFrequency) - 0.5f) * 2f
                ) * currentShakeIntensity,
                Time.deltaTime * 8f
            );
            
            // Apply position with shake
            cameraTransform.position = targetPosition + shakeOffset;
            
            yield return null;
        }

        // Return to original position
        timeElapsed = 0f;
        float returnDuration = 1.5f;
        Vector3 currentPos = cameraTransform.position;
        Quaternion currentRot = cameraTransform.rotation;

        while (timeElapsed < returnDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / returnDuration);
            
            // Use a smooth easing function
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // Calculate base position and rotation
            Vector3 basePosition = Vector3.Lerp(currentPos, originalPosition, smoothT);
            Quaternion baseRotation = Quaternion.Slerp(currentRot, originalRotation, smoothT);
            
            // Add shake that gradually fades out as we return to original position
            float currentShakeIntensity = shakeIntensity * (1f - smoothT);
            
            // Generate smooth random shake offset
            shakeOffset = Vector3.Lerp(
                shakeOffset, 
                new Vector3(
                    (Mathf.PerlinNoise(Time.time * shakeFrequency, 0) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0, Time.time * shakeFrequency) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(Time.time * shakeFrequency, Time.time * shakeFrequency) - 0.5f) * 2f
                ) * currentShakeIntensity,
                Time.deltaTime * 8f
            );
            
            // Apply position with shake and rotation
            cameraTransform.position = basePosition + shakeOffset;
            cameraTransform.rotation = baseRotation;
            
            yield return null;
        }

        // Re-enable the camera controller
        playerCamera.enabled = wasEnabled;
        
        // Re-enable player controls
        if (playerController != null)
        {
            playerController.SetControlsEnabled(true);
            Debug.Log("Re-enabled player controls after metamorphosis animation");
        }
        
        isAnimating = false;
    }

    // Add this method to animate to map view
    public void AnimateToMapView()
    {
        if (isAnimating) return;
        
        Debug.Log("Starting map view camera animation");
        StartCoroutine(AnimateToMapViewCoroutine());
    }

    // Add this method to return from map view
    public void ReturnFromMapView()
    {
        if (!isInMapView || isAnimating) return;
        
        Debug.Log("Returning from map view");
        StartCoroutine(ReturnFromMapViewCoroutine());
    }

    // Implement the map view animation coroutine
    private IEnumerator AnimateToMapViewCoroutine()
    {
        isAnimating = true;
        
        // Store original camera position for return
        preMappingCameraPosition = cameraTransform.position;
        preMappingCameraRotation = cameraTransform.rotation;
        
        // Disable the regular camera controller
        bool wasEnabled = playerCamera.enabled;
        playerCamera.enabled = false;
        
        // Determine player position (center of map view)
        PlayerController player = FindObjectOfType<PlayerController>();
        Vector3 playerPosition = player != null ? player.transform.position : Vector3.zero;
        
        // Calculate target position above player
        Vector3 targetPosition = new Vector3(playerPosition.x, playerPosition.y + mapViewHeight, playerPosition.z);
        
        // Calculate target rotation (looking down)
        Quaternion targetRotation = Quaternion.Euler(mapCameraAngle, 0f, 0f);
        
        // Move to the map view position over time
        float timeElapsed = 0f;
        
        while (timeElapsed < mapViewDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / mapViewDuration);
            
            // Use smooth easing
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            cameraTransform.position = Vector3.Lerp(preMappingCameraPosition, targetPosition, smoothT);
            cameraTransform.rotation = Quaternion.Slerp(preMappingCameraRotation, targetRotation, smoothT);
            
            yield return null;
        }
        
        // Make sure we end at exactly the target
        cameraTransform.position = targetPosition;
        cameraTransform.rotation = targetRotation;
        
        isInMapView = true;
        isAnimating = false;
    }

    // Implement the return from map view animation coroutine
    private IEnumerator ReturnFromMapViewCoroutine()
    {
        isAnimating = true;
        
        // Current camera position (which might be dragged away from original)
        Vector3 currentPosition = cameraTransform.position;
        Quaternion currentRotation = cameraTransform.rotation;
        
        // Move back to the original position over time
        float timeElapsed = 0f;
        
        while (timeElapsed < mapViewDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / mapViewDuration);
            
            // Use smooth easing
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            cameraTransform.position = Vector3.Lerp(currentPosition, preMappingCameraPosition, smoothT);
            cameraTransform.rotation = Quaternion.Slerp(currentRotation, preMappingCameraRotation, smoothT);
            
            yield return null;
        }
        
        // Make sure we end at exactly the original position
        cameraTransform.position = preMappingCameraPosition;
        cameraTransform.rotation = preMappingCameraRotation;
        
        // Re-enable the camera controller
        playerCamera.enabled = true;
        
        isInMapView = false;
        isAnimating = false;
    }

    // Add this to your Update method
    private void Update()
    {
        // Only handle dragging when in map view
        if (isInMapView && enableMapDragging)
        {
            HandleMapViewDragging();
        }
    }

    // Handle dragging logic in map view
    private void HandleMapViewDragging()
    {
        // Handle mouse input for dragging
        if (Input.GetMouseButtonDown(0))
        {
            // Start dragging
            dragStartPosition = Input.mousePosition;
            originalMapPosition = cameraTransform.position;
            isDragging = true;
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            // Calculate the drag delta
            Vector3 dragDelta = Input.mousePosition - dragStartPosition;
            
            // Scale the drag speed based on height to ensure consistent movement regardless of zoom
            float heightFactor = cameraTransform.position.y / 20f; // Adjust divisor as needed
            
            // Move the camera horizontally only (keep the same height)
            Vector3 cameraForward = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
            Vector3 cameraRight = new Vector3(cameraTransform.right.x, 0, cameraTransform.right.z).normalized;
            
            Vector3 movement = 
                -cameraRight * dragDelta.x * dragSpeed * heightFactor * Time.deltaTime -
                cameraForward * dragDelta.y * dragSpeed * heightFactor * Time.deltaTime;
            
            // Update camera position while preserving height
            Vector3 newPosition = cameraTransform.position + movement;
            newPosition.y = cameraTransform.position.y; // Keep the same height
            
            // Enforce boundary limits
            Vector3 mapCenterXZ = new Vector3(originalMapPosition.x, 0, originalMapPosition.z);
            Vector3 newPositionXZ = new Vector3(newPosition.x, 0, newPosition.z);
            
            // If outside the boundary radius, clamp position
            if (Vector3.Distance(mapCenterXZ, newPositionXZ) > mapBoundaryRadius)
            {
                Vector3 directionXZ = (newPositionXZ - mapCenterXZ).normalized;
                newPositionXZ = mapCenterXZ + directionXZ * mapBoundaryRadius;
                
                // Update final position with clamped X and Z, original Y
                newPosition = new Vector3(newPositionXZ.x, newPosition.y, newPositionXZ.z);
            }
            
            // Apply the new position
            cameraTransform.position = newPosition;
            
            // Reset start position for smooth dragging
            dragStartPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // End dragging
            isDragging = false;
        }
    }
} 