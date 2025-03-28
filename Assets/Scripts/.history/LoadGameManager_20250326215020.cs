using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadGameManager : MonoBehaviour
{
    public GameObject canvas;

    public Image loadingBar;
    public Image loadingBarFill;

    public TextMeshProUGUI loadingText;

    [Header("Loading Settings")]
    [Range(2f, 10f)]
    public float loadingDuration = 5f; // How long the entire loading takes
    [Range(1, 5)]
    public int numberOfPauses = 3; // How many times loading will pause
    [Range(0.1f, 1.5f)]
    public float maxPauseDuration = 0.7f; // Maximum pause duration

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SimulateLoading());
    }

    IEnumerator SimulateLoading()
    {
        float elapsedTime = 0f;
        // Generate random pause points
        List<float> pausePoints = GenerateRandomPausePoints(numberOfPauses);
        List<float> pauseDurations = GenerateRandomPauseDurations(numberOfPauses);
        int currentPauseIndex = 0;

        // Simulate loading
        while (elapsedTime < loadingDuration)
        {
            elapsedTime += Time.deltaTime;
            float fillAmount = Mathf.Clamp01(elapsedTime / loadingDuration);
            
            // Check if we need to pause
            if (currentPauseIndex < pausePoints.Count && fillAmount >= pausePoints[currentPauseIndex])
            {
                // Pause the loading bar
                yield return new WaitForSeconds(pauseDurations[currentPauseIndex]);
                currentPauseIndex++;
            }
            
            // Update the fill amount
            loadingBarFill.fillAmount = fillAmount;
            
            yield return null;
        }
        
        // Loading complete
        loadingBarFill.fillAmount = 1f;
        yield return new WaitForSeconds(0.5f); // Small delay before hiding
        
        // Hide the loading screen
        canvas.SetActive(false);
    }

    private List<float> GenerateRandomPausePoints(int count)
    {
        List<float> points = new List<float>();
        for (int i = 0; i < count; i++)
        {
            // Generate points between 0.1 and 0.9 to avoid pauses at the very beginning or end
            points.Add(Random.Range(0.1f, 0.9f));
        }
        points.Sort(); // Sort points in ascending order
        return points;
    }

    private List<float> GenerateRandomPauseDurations(int count)
    {
        List<float> durations = new List<float>();
        for (int i = 0; i < count; i++)
        {
            durations.Add(Random.Range(0.1f, maxPauseDuration));
        }
        return durations;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
