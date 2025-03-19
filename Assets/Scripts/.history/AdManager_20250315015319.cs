using UnityEngine;
using UnityEngine.Advertisements;
using System;

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
    
    // Reference to player inventory
    private PlayerInventory _playerInventory;
    
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
        // Find player inventory if not assigned
        if (_playerInventory == null)
        {
            _playerInventory = FindObjectOfType<PlayerInventory>();
            
            if (_playerInventory != null)
            {
                // Subscribe to chitin deposit events
                _playerInventory.OnChitinCountChanged += OnChitinCountChanged;
            }
            else if (_showDebugMessages)
            {
                Debug.LogWarning("AdManager: PlayerInventory not found. Chitin-based ads won't work.");
            }
        }
    }
    
    private void OnChitinCountChanged(int newChitinCount)
    {
        // This method is called whenever chitin count changes
        // We need to detect when chitin is deposited (count decreases)
        
        // Get the previous chitin count (before the change)
        int previousCount = _playerInventory.ChitinCount;
        
        // If the new count is less than the previous count, chitin was deposited
        if (newChitinCount < previousCount)
        {
            int depositedAmount = previousCount - newChitinCount;
            
            if (_showDebugMessages)
            {
                Debug.Log($"AdManager: Detected {depositedAmount} chitin deposited");
            }
            
            // Add to our counter
            _chitinDepositedSinceLastAd += depositedAmount;
            
            // Check if we've reached the threshold
            if (_chitinDepositedSinceLastAd >= _chitinThresholdForAd)
            {
                if (_showDebugMessages)
                {
                    Debug.Log($"AdManager: Chitin threshold reached ({_chitinDepositedSinceLastAd}). Showing interstitial ad.");
                }
                
                // Show the ad
                ShowInterstitialAd();
                
                // Reset the counter (subtract the threshold)
                _chitinDepositedSinceLastAd -= _chitinThresholdForAd;
            }
        }
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
        if (_playerInventory != null)
        {
            _playerInventory.OnChitinCountChanged -= OnChitinCountChanged;
        }
    }
}