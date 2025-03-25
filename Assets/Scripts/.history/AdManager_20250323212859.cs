using UnityEngine;
using UnityEngine.Advertisements;
using System;
using System.Collections;

public class AdManager : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] private string _androidGameId = "YOUR_ANDROID_GAME_ID";
    [SerializeField] private string _iOSGameId = "YOUR_IOS_GAME_ID";
    [SerializeField] private bool _testMode = true;
    
    [Header("Ad Components")]
    [SerializeField] private InterstitialAdExample _interstitialAd;
    [SerializeField] private RewardedAdExample _rewardedAd;
    
    [Header("Ad Settings")]
    [SerializeField] private int _chitinThresholdForAd = 5;
    [SerializeField] private bool _showDebugMessages = true;
    
    private string _gameId;
    private bool _isInitialized = false;
    private int _chitinDepositedSinceLastAd = 0;
    
    // Reference to base interaction
    private BaseInteraction[] _baseInteractions;
    
    public static AdManager Instance { get; private set; }
    
    public bool IsInitialized => _isInitialized;
    
    private void Awake()
    {
        // Implement singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Set the game ID based on platform
        #if UNITY_IOS
            _gameId = _iOSGameId;
        #elif UNITY_ANDROID
            _gameId = _androidGameId;
        #else
            _gameId = "unexpected_platform";
        #endif
        
        InitializeAds();
    }
    
    private void Start()
    {
        // Find all BaseInteraction objects in the scene
        _baseInteractions = FindObjectsOfType<BaseInteraction>();
        
        if (_baseInteractions.Length > 0)
        {
            // Subscribe to chitin deposit events by adding our method to each BaseInteraction
            foreach (BaseInteraction baseInteraction in _baseInteractions)
            {
                // Add a method to be called after chitin deposit
                // We'll need to add this method to BaseInteraction
                baseInteraction.OnChitinDeposited += OnChitinDeposited;
            }
            
            if (_showDebugMessages)
            {
                Debug.Log($"AdManager: Found {_baseInteractions.Length} BaseInteraction objects");
            }
        }
        else if (_showDebugMessages)
        {
            Debug.LogWarning("AdManager: No BaseInteraction found. Chitin-based ads won't work.");
        }
    }
    
    // This will be called by BaseInteraction when chitin is deposited
    public void OnChitinDeposited(int amount)
    {
        if (_showDebugMessages)
        {
            Debug.Log($"AdManager: Detected {amount} chitin deposited");
        }
        
        // Add to our counter
        _chitinDepositedSinceLastAd += amount;
        
        // Check if we've reached the threshold
        if (_chitinDepositedSinceLastAd >= _chitinThresholdForAd)
        {
            if (_showDebugMessages)
            {
                Debug.Log($"AdManager: Chitin threshold reached ({_chitinDepositedSinceLastAd}). Scheduling ad after animation.");
            }
            
            // Get the VisualEffectManager to determine animation duration
            VisualEffectManager visualEffectManager = FindObjectOfType<VisualEffectManager>();
            
            if (visualEffectManager != null)
            {
                // Get the chitin fly duration from the VisualEffectManager
                float animationDuration = visualEffectManager.ChitinFlyDuration;
                
                // Add a small buffer to ensure animation completes
                float delayTime = animationDuration + 0.5f;
                
                // Schedule the ad to show after the animation completes
                StartCoroutine(ShowAdAfterDelay(delayTime));
            }
            else
            {
                // If we can't find the VisualEffectManager, use a default delay
                StartCoroutine(ShowAdAfterDelay(2.0f));
            }
            
            // Reset the counter (subtract the threshold)
            _chitinDepositedSinceLastAd -= _chitinThresholdForAd;
        }
    }
    
    private IEnumerator ShowAdAfterDelay(float delay)
    {
        if (_showDebugMessages)
        {
            Debug.Log($"AdManager: Waiting {delay} seconds for animation to complete before showing ad.");
        }
        
        yield return new WaitForSeconds(delay);
        
        if (_showDebugMessages)
        {
            Debug.Log("AdManager: Animation complete, showing interstitial ad now.");
        }
        
        ShowInterstitialAd();
    }
    
    public void InitializeAds()
    {
        if (!Advertisement.isInitialized && !string.IsNullOrEmpty(_gameId))
        {
            Advertisement.Initialize(_gameId, _testMode, this);
        }
    }
    
    // IUnityAdsInitializationListener implementation
    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
        _isInitialized = true;
        
        // Load ads after initialization
        LoadAds();
    }
    
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Unity Ads initialization failed: {error.ToString()} - {message}");
    }
    
    private void LoadAds()
    {
        if (_interstitialAd != null)
        {
            _interstitialAd.LoadAd();
        }
        
        if (_rewardedAd != null)
        {
            _rewardedAd.LoadAd();
        }
    }
    
    // Public methods to show ads
    public void ShowInterstitialAd()
    {
        if (_isInitialized && _interstitialAd != null)
        {
            _interstitialAd.ShowAd();
            
            // Reload the ad for next time
            _interstitialAd.LoadAd();
        }
        else
        {
            Debug.LogWarning("Interstitial ad not ready to show.");
        }
    }
    
    public void ShowRewardedAd()
    {
        if (_isInitialized && _rewardedAd != null)
        {
            _rewardedAd.ShowAd();
            
            // Reload the ad for next time
            _rewardedAd.LoadAd();
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready to show.");
        }
    }
    
    // Method to reload ads (call this after showing an ad)
    public void ReloadAds()
    {
        LoadAds();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (_baseInteractions != null)
        {
            foreach (BaseInteraction baseInteraction in _baseInteractions)
            {
                if (baseInteraction != null)
                {
                    baseInteraction.OnChitinDeposited -= OnChitinDeposited;
                }
            }
        }
    }
}