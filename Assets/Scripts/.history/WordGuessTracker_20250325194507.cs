using UnityEngine;

public class WordGuessTracker : MonoBehaviour
{
    [SerializeField] private int correctGuessesForReview = 6;
    private int correctGuessCount = 0;

    // Call this method whenever a player correctly guesses a word
    public void OnCorrectWordGuessed()
    {
        correctGuessCount++;
        
        // Notify the review manager
        if (InAppReviewManager.Instance != null)
        {
            InAppReviewManager.Instance.OnWordGuessed();
        }
        
        Debug.Log($"Correct guesses: {correctGuessCount}");
    }
} 