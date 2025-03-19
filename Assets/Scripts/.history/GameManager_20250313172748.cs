using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class GameManager : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool vSyncEnabled = true;
    [SerializeField] private bool limitFrameRate = true;
    
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
            Time.maximumDeltaTime = 0.1f; // Cap at 100ms to prevent extreme slowdowns
            
            // Set maximum allowed time step between frames
            Time.maximumParticleDeltaTime = 0.03f; // Helps with particle effects
            
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
        
        // Reduce physics simulation quality on mobile
        Physics.defaultSolverIterations = 4; // Default is 6
        Physics.defaultSolverVelocityIterations = 1; // Default is 1
        
        Debug.Log("Applied mobile-specific optimizations");
        #endif
    }
    
    // Optional: Monitor and log the actual frame rate
    [Header("Debug")]
    [SerializeField] private bool showFpsInLog = false;
    [SerializeField] private float fpsLogInterval = 5f;
    
    private float fpsTimer;
    
    private void Update()
    {
        if (showFpsInLog)
        {
            fpsTimer += Time.deltaTime;
            
            if (fpsTimer >= fpsLogInterval)
            {
                float fps = 1.0f / Time.deltaTime;
                Debug.Log($"Current FPS: {fps:F1}");
                fpsTimer = 0f;
            }
        }
    }
}
