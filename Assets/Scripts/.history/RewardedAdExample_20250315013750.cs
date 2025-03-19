using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;

public class RewardAdExample : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] private Button _showAdButton;
    [SerializeField] private string _androidAdUnitId = "Rewarded_Android";
    [SerializeField] private string _iOSAdUnitId = "Rewarded_iOS";
    
    private string _adUnitId;
    
    private void Awake()
    {
        // Get the Ad Unit ID for the current platform
        #if UNITY_ANDROID
            _adUnitId = _androidAdUnitId;
        #elif UNITY_IOS
            _adUnitId = _iOSAdUnitId;
        #else
            _adUnitId = "unexpected_platform";
        #endif
        
        // Disable the button until the ad is ready to show
        _showAdButton.interactable = false;
    }
    
    private void Start()
    {
        LoadAd();
    }
    
    // Load content to the Ad Unit
    public void LoadAd()
    {
        Debug.Log($"Loading Ad: {_adUnitId}");
        Advertisement.Load(_adUnitId, this);
    }
    
    // Show the loaded content in the Ad Unit
    public void ShowAd()
    {
        // Disable the button until the ad is ready
        _showAdButton.interactable = false;
        Advertisement.Show(_adUnitId, this);
    }
    
    // Implement IUnityAdsLoadListener interface methods
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log($"Ad Loaded: {adUnitId}");
        
        if (adUnitId.Equals(_adUnitId))
        {
            // Enable the button for users to watch the ad
            _showAdButton.interactable = true;
        }
    }
    
    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Error loading Ad Unit {adUnitId}: {error.ToString()} - {message}");
        // Use the error details to determine whether to try again
    }
    
    // Implement IUnityAdsShowListener interface methods
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
        {
            Debug.Log("Unity Ads Rewarded Ad Completed");
            // Grant a reward to the player
            // Example: AddCoins(100);
            
            // Load another ad
            LoadAd();
        }
    }
    
    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Error showing Ad Unit {adUnitId}: {error.ToString()} - {message}");
    }
    
    public void OnUnityAdsShowStart(string adUnitId) 
    {
        Debug.Log($"Ad Show Start: {adUnitId}");
    }
    
    public void OnUnityAdsShowClick(string adUnitId) 
    {
        Debug.Log($"Ad Show Click: {adUnitId}");
    }
}