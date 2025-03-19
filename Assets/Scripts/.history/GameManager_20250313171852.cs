using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            
            // Set fixed timestep for physics
            Time.fixedDeltaTime = 0.0167f; // ~60 physics updates per second
            
            // Set maximum allowed timestep
            Time.maximumDeltaTime = 0.0333f; // Prevent physics from going too slow
            
            Debug.Log($"Frame rate limited to {targetFrameRate} FPS, VSync: {(vSyncEnabled ? "Enabled" : "Disabled")}");
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
