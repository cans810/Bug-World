using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool vSyncEnabled = true;
    [SerializeField] private bool limitFrameRate = true;
    
    [Header("Audio Settings")]
    [SerializeField] private string backgroundAmbientSound = "Ambient1";
    [SerializeField] private float ambientFadeInTime = 2.0f;
    [SerializeField] private bool playBackgroundAmbient = true;
    
    private void Awake()
    {
        // Apply frame rate settings
        if (limitFrameRate)
        {
            // Set the target frame rate
            Application.targetFrameRate = targetFrameRate;
            
            // Enable or disable VSync
            QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
            
            // Set fixed timestep for physics (60 Hz is standard)
            Time.fixedDeltaTime = 0.01667f; // Exactly 60 physics updates per second
            
            // Set maximum allowed timestep to prevent large time jumps
            Time.maximumDeltaTime = 0.03333f; // Cap at 33.3ms (30 FPS minimum)
            
            // Set maximum allowed time step between frames for particles
            Time.maximumParticleDeltaTime = 0.03f; // Helps with particle effects
            
            // Improve frame pacing - use UnityEngine namespace explicitly
            Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.Low;
            
            Debug.Log($"Frame rate limited to {targetFrameRate} FPS, VSync: {(vSyncEnabled ? "Enabled" : "Disabled")}");
            Debug.Log($"Fixed timestep: {Time.fixedDeltaTime}s, Max timestep: {Time.maximumDeltaTime}s");
        }
        else
        {
            // If we don't want to limit the frame rate, set it to the maximum
            Application.targetFrameRate = -1;
            Debug.Log("Frame rate not limited");
        }
        
        // Additional platform-specific optimizations
        #if UNITY_ANDROID || UNITY_IOS
        // Mobile-specific optimizations
        Application.targetFrameRate = Mathf.Min(targetFrameRate, 60); // Ensure mobile doesn't exceed 60 FPS
        QualitySettings.antiAliasing = 0; // Disable antialiasing on mobile
        QualitySettings.shadowResolution = ShadowResolution.Low; // Lower shadow quality
        QualitySettings.shadowDistance = 20f; // Reduce shadow distance
        QualitySettings.particleRaycastBudget = 64; // Reduce particle raycast budget
        QualitySettings.asyncUploadTimeSlice = 1; // Reduce time spent on async uploads
        QualitySettings.asyncUploadBufferSize = 4; // Reduce async upload buffer size
        QualitySettings.realtimeReflectionProbes = false; // Disable realtime reflection probes
        QualitySettings.billboardsFaceCameraPosition = false; // Disable billboards facing camera position
        QualitySettings.resolutionScalingFixedDPIFactor = 1.0f; // Don't scale resolution
        
        // Reduce physics simulation quality on mobile
        Physics.defaultSolverIterations = 4; // Default is 6
        Physics.defaultSolverVelocityIterations = 1; // Default is 1
        Physics.sleepThreshold = 0.005f; // Make objects sleep sooner
        Physics.defaultContactOffset = 0.02f; // Increase default contact offset
        Physics.defaultMaxAngularSpeed = 7.0f; // Limit maximum angular velocity
        
        // Optimize memory usage
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        
        Debug.Log("Applied mobile-specific optimizations");
        #endif
        
        // Call this method to optimize all rigidbodies in the scene
        OptimizeAllRigidbodies();
    }
    
    private void Start()
    {
        // Start the background ambient sound if enabled
        if (playBackgroundAmbient && !string.IsNullOrEmpty(backgroundAmbientSound))
        {
            if (SoundEffectManager.Instance != null)
            {
                // Check if the sound effect exists
                if (SoundEffectManager.Instance.HasSoundEffect(backgroundAmbientSound))
                {
                    Debug.Log($"Playing background ambient sound: {backgroundAmbientSound}");
                    SoundEffectManager.Instance.PlayBackgroundSound(backgroundAmbientSound, ambientFadeInTime);
                }
                else
                {
                    Debug.LogWarning($"Background ambient sound '{backgroundAmbientSound}' not found in SoundEffectManager");
                }
            }
            else
            {
                Debug.LogWarning("SoundEffectManager instance not found. Cannot play background ambient.");
            }
        }
    }
    
    // Add this method to optimize all rigidbodies in the scene
    private void OptimizeAllRigidbodies()
    {
        Rigidbody[] allRigidbodies = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in allRigidbodies)
        {
            if (rb == null) continue;
            
            try
            {
                // Important settings for smoother movement
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                
                // Optimize physics behavior
                rb.sleepThreshold = 0.005f;
                rb.maxAngularVelocity = 7f;
                rb.angularDrag = 0.05f;
                
                // Adjust drag based on entity type
                LivingEntity entity = rb.GetComponent<LivingEntity>();
                if (entity != null)
                {
                    if (rb.gameObject.CompareTag("Player"))
                    {
                        rb.mass = 10f;
                        rb.drag = 1f;
                        // Don't freeze rotation for player - needs full control
                        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    }
                    else if (rb.gameObject.CompareTag("Ally"))
                    {
                        rb.mass = 8f;
                        rb.drag = 0.8f;
                        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    }
                    else
                    {
                        // For enemies
                        rb.mass = 5f;
                        rb.drag = 0.5f;
                        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error optimizing rigidbody on {rb.gameObject.name}: {e.Message}");
            }
        }
        
        Debug.Log($"Optimized {allRigidbodies.Length} rigidbodies in the scene");
    }
    
    // Optional: Monitor and log the actual frame rate
    [Header("Debug")]
    [SerializeField] private bool showFpsInLog = false;
    [SerializeField] private float fpsLogInterval = 5f;
    
    private float fpsTimer;
    private float[] fpsBuffer = new float[10]; // Store last 10 FPS values
    private int fpsBufferIndex = 0;
    
    private void Update()
    {
        if (showFpsInLog)
        {
            fpsTimer += Time.deltaTime;
            
            // Store current FPS in buffer
            float currentFps = 1.0f / Time.deltaTime;
            fpsBuffer[fpsBufferIndex] = currentFps;
            fpsBufferIndex = (fpsBufferIndex + 1) % fpsBuffer.Length;
            
            if (fpsTimer >= fpsLogInterval)
            {
                // Calculate average FPS
                float sum = 0;
                foreach (float fps in fpsBuffer)
                {
                    sum += fps;
                }
                float averageFps = sum / fpsBuffer.Length;
                
                Debug.Log($"Current FPS: {currentFps:F1}, Average FPS: {averageFps:F1}");
                fpsTimer = 0f;
            }
        }
    }
}
