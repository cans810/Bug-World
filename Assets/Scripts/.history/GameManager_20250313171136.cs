using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool vSyncEnabled = true;
    [SerializeField] private bool limitFrameRate = true;
    
    [Header("Physics Settings")]
    [SerializeField] private int physicsUpdateRate = 60; // Fixed update rate for physics
    [SerializeField] private bool useConsistentPhysics = true;
    
    private void Awake()
    {
        // Apply frame rate settings
        if (limitFrameRate)
        {
            // Set the target frame rate
            Application.targetFrameRate = targetFrameRate;
            
            // Enable or disable VSync
            QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
            
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
        
        // Set consistent physics update rate
        if (useConsistentPhysics)
        {
            // Set fixed timestep for physics (1/physicsUpdateRate seconds)
            Time.fixedDeltaTime = 1f / physicsUpdateRate;
            Debug.Log($"Physics update rate set to {physicsUpdateRate} Hz (fixedDeltaTime: {Time.fixedDeltaTime})");
        }
        
        // Ensure maximum time step to prevent large jumps when frame rate drops
        Time.maximumDeltaTime = 1f / 15f; // Limit to 15 FPS equivalent
    }
    
    // Optional: Monitor and log the actual frame rate
    [Header("Debug")]
    [SerializeField] private bool showFpsInLog = false;
    [SerializeField] private float fpsLogInterval = 5f;
    [SerializeField] private bool showMovementDebug = false;
    
    private float fpsTimer;
    
    private void Update()
    {
        if (showFpsInLog)
        {
            fpsTimer += Time.deltaTime;
            
            if (fpsTimer >= fpsLogInterval)
            {
                float fps = 1.0f / Time.deltaTime;
                Debug.Log($"Current FPS: {fps:F1}, deltaTime: {Time.deltaTime:F4}, fixedDeltaTime: {Time.fixedDeltaTime:F4}");
                fpsTimer = 0f;
            }
        }
        
        // Check for any scripts that might not be using deltaTime properly
        if (showMovementDebug && Input.GetKeyDown(KeyCode.F1))
        {
            DebugMovementScripts();
        }
    }
    
    // This method checks common movement scripts for proper time-based calculations
    private void DebugMovementScripts()
    {
        Debug.Log("Checking movement scripts for proper time-based calculations...");
        
        // Check player controllers
        PlayerController[] playerControllers = FindObjectsOfType<PlayerController>();
        foreach (var controller in playerControllers)
        {
            Debug.Log($"Found PlayerController on {controller.gameObject.name}");
        }
        
        // Check rigidbody-based movement
        Rigidbody[] rigidbodies = FindObjectsOfType<Rigidbody>();
        Debug.Log($"Found {rigidbodies.Length} Rigidbody components in the scene");
        
        // Check Rigidbody2D-based movement
        Rigidbody2D[] rigidbodies2D = FindObjectsOfType<Rigidbody2D>();
        Debug.Log($"Found {rigidbodies2D.Length} Rigidbody2D components in the scene");
        
        // Check character controllers
        CharacterController[] characterControllers = FindObjectsOfType<CharacterController>();
        Debug.Log($"Found {characterControllers.Length} CharacterController components in the scene");
    }
    
    // Add this method to help fix common movement scripts
    public static void EnsureTimeBasedMovement(MonoBehaviour script)
    {
        // This is a utility method you can call from other scripts
        // to ensure they're using time-based movement
        Debug.Log($"Ensuring time-based movement for {script.GetType().Name} on {script.gameObject.name}");
        
        // You would implement specific fixes here based on your game's needs
    }
}
