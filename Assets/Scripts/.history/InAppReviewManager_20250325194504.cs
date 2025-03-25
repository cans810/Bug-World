using System.Collections;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_ANDROID
using Google.Play.Review;
#endif

#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class InAppReviewManager : MonoBehaviour
{
    [Header("Review Settings")]
    [Tooltip("Minimum number of words guessed before showing review")]
    [SerializeField] private int requiredWordGuesses = 6;
    
    [Tooltip("Minimum days between review requests")]
    [SerializeField] private int daysBetweenReviewRequests = 30;
    
    [Tooltip("Key to save review information")]
    private const string REVIEW_KEY = "last_review_request";
    private const string WORDS_GUESSED_KEY = "words_guessed_count";
    
    // Event that can be subscribed to when review is requested
    public UnityEvent onReviewRequested = new UnityEvent();

    private static InAppReviewManager _instance;
    public static InAppReviewManager Instance { get { return _instance; } }

    #if UNITY_ANDROID
    private ReviewManager _reviewManager;
    #endif

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        #if UNITY_ANDROID
        _reviewManager = new ReviewManager();
        #endif
    }

    public void IncrementWordGuessCount()
    {
        int currentCount = PlayerPrefs.GetInt(WORDS_GUESSED_KEY, 0);
        currentCount++;
        PlayerPrefs.SetInt(WORDS_GUESSED_KEY, currentCount);
        
        // Check if we should request a review
        if (currentCount >= requiredWordGuesses)
        {
            CheckAndRequestReview();
        }
    }
    
    // Call this when the player correctly guesses a word
    public void OnWordGuessed()
    {
        IncrementWordGuessCount();
    }

    // Manually request a review (can be called from anywhere)
    public void RequestReview()
    {
        StartCoroutine(RequestReviewCoroutine());
    }

    // Check criteria and request review if appropriate
    private void CheckAndRequestReview()
    {
        // Check if we've requested a review recently
        if (!CanRequestReview())
            return;

        // Reset the count after showing review
        PlayerPrefs.SetInt(WORDS_GUESSED_KEY, 0);
        
        // Save the time of this request
        PlayerPrefs.SetString(REVIEW_KEY, System.DateTime.Now.ToString());
        
        // Request the review
        RequestReview();
    }

    // Check if enough time has passed since last review request
    private bool CanRequestReview()
    {
        if (!PlayerPrefs.HasKey(REVIEW_KEY))
            return true;

        string lastReviewStr = PlayerPrefs.GetString(REVIEW_KEY);
        if (System.DateTime.TryParse(lastReviewStr, out System.DateTime lastReview))
        {
            System.TimeSpan timeSinceLastReview = System.DateTime.Now - lastReview;
            return timeSinceLastReview.TotalDays >= daysBetweenReviewRequests;
        }

        return true;
    }

    private IEnumerator RequestReviewCoroutine()
    {
        // Notify any listeners
        onReviewRequested.Invoke();
        
        Debug.Log("Requesting app review...");

        #if UNITY_ANDROID
        // Request the Review Flow
        var requestFlowOperation = _reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;

        if (requestFlowOperation.Error != Google.Play.Review.ReviewErrorCode.NoError)
        {
            Debug.LogError($"Error requesting review: {requestFlowOperation.Error}");
            yield break;
        }

        // Launch the Review Flow
        var playReviewInfo = requestFlowOperation.GetResult();
        var launchFlowOperation = _reviewManager.LaunchReviewFlow(playReviewInfo);
        yield return launchFlowOperation;

        if (launchFlowOperation.Error != Google.Play.Review.ReviewErrorCode.NoError)
        {
            Debug.LogError($"Error launching review: {launchFlowOperation.Error}");
            yield break;
        }
        
        Debug.Log("Review flow completed");
        
        #elif UNITY_IOS
        // For iOS, this opens the review prompt directly
        if (Device.RequestStoreReview())
        {
            Debug.Log("iOS review request sent successfully");
        }
        else
        {
            Debug.LogError("iOS review request failed");
        }
        yield return null;
        #else
        // On other platforms, simply log that review isn't supported
        Debug.Log("In-app review not supported on this platform");
        yield return null;
        #endif
    }
} 