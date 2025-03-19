using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class AdManager : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] private string _androidGameId = "YOUR_ANDROID_GAME_ID";
    [SerializeField] private string _iOSGameId = "YOUR_IOS_GAME_ID";
    [SerializeField] private bool _testMode = true;
    [SerializeField] private bool _enablePerPlacementMode = true;
    
    [Header("Ad Components")]
    [SerializeField] private InterstitialAdExample _interstitialAd;
    [SerializeField] private RewardedAdExample _rewardedAd;
    
    private string _gameId;
    private bool _isInitialized = false;
    
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
    
    public void InitializeAds()
    {
        if (!Advertisement.isInitialized && !string.IsNullOrEmpty(_gameId))
        {
            Advertisement.Initialize(_gameId, _testMode, _enablePerPlacementMode, this);
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
}