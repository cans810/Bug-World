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
    [SerializeField] private float _timeBetweenAds = 300f; // 5 minutes (300 seconds)
    [SerializeField] private bool _showDebugMessages = true;
    
    private string _gameId;
    private bool _isInitialized = false;
    private float _timeSinceLastAd = 0f;
    private bool _isAdScheduled = false;
    
    // Add a variable to track when the app was paused
    private float _timeWhenPaused;
    
    public static AdManager Instance { get; private set; }
    
    public bool IsInitialized => _isInitialized;
    
    public event Action OnRewardedAdComplete;
    
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
        // Initialize the timer
        _timeSinceLastAd = 0f;
        
        if (_showDebugMessages)
        {
            Debug.Log($"AdManager: Initialized with {_timeBetweenAds} seconds between ads");
        }
    }
    
    private void Update()
    {
        // Increment the timer
        _timeSinceLastAd += Time.deltaTime;
        
        // Check if it's time to show an ad
        if (_timeSinceLastAd >= _timeBetweenAds && !_isAdScheduled)
        {
            if (_showDebugMessages)
            {
                Debug.Log($"AdManager: Time threshold reached ({_timeSinceLastAd:F1} seconds). Scheduling ad.");
            }
            
            // Schedule the ad
            _isAdScheduled = true;
            StartCoroutine(ShowAdWhenPossible());
        }
    }
    
    // Handle app pausing and resuming to accurately track time
    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            // Store the time when the app was paused
            _timeWhenPaused = Time.realtimeSinceStartup;
        }
        else
        {
            // Calculate elapsed time while paused
            float pauseDuration = Time.realtimeSinceStartup - _timeWhenPaused;
            
            // Add the pause duration to our timer
            _timeSinceLastAd += pauseDuration;
            
            if (_showDebugMessages)
            {
                Debug.Log($"AdManager: App resumed after {pauseDuration:F1} seconds. Total time since last ad: {_timeSinceLastAd:F1}");
            }
            
            // Check if we should show an ad immediately on resume
            if (_timeSinceLastAd >= _timeBetweenAds && !_isAdScheduled)
            {
                _isAdScheduled = true;
                StartCoroutine(ShowAdWhenPossible());
            }
        }
    }
    
    private IEnumerator ShowAdWhenPossible()
    {
        // Wait a short delay to ensure we're not interrupting any important gameplay
        yield return new WaitForSeconds(0.5f);
        
        if (_showDebugMessages)
        {
            Debug.Log("AdManager: Showing interstitial ad based on time threshold");
        }
        
        ShowInterstitialAd();
        
        // Reset the timer
        _timeSinceLastAd = 0f;
        _isAdScheduled = false;
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
            
            // Reset the timer whenever an ad is shown (even if manually triggered)
            _timeSinceLastAd = 0f;
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
            
            // Optionally reset the interstitial timer when a rewarded ad is shown
            // This prevents bombarding users with multiple ads in succession
            _timeSinceLastAd = 0f;
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
    
    public void TriggerRewardedAdCompleted()
    {
        OnRewardedAdComplete?.Invoke();
    }
}