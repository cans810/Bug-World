using UnityEngine;
using UnityEngine.Advertisements;

public class AdManager : MonoBehaviour
{
    [SerializeField] private string _androidGameId;
    [SerializeField] private string _iOSGameId;
    [SerializeField] private bool _testMode = true;
    
    private string _gameId;
    
    public static AdManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeAds();
    }
    
    private void InitializeAds()
    {
        #if UNITY_ANDROID
            _gameId = _androidGameId;
        #elif UNITY_IOS
            _gameId = _iOSGameId;
        #else
            _gameId = "unexpected_platform";
        #endif
        
        if (Advertisement.isInitialized)
        {
            Debug.Log("Ads already initialized.");
            return;
        }
        
        Advertisement.Initialize(_gameId, _testMode, this);
    }
    
    // Implement IUnityAdsInitializationListener
    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
    }
    
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Unity Ads initialization failed: {error.ToString()} - {message}");
    }
} 