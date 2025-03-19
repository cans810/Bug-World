using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntEggController : MonoBehaviour
{
    [Header("Ant Settings")]
    [SerializeField] private GameObject antPrefab;
    
    [Tooltip("Hatching time in seconds. Set to 7200 for 2 hours")]
    [SerializeField] private float hatchTime = 7200f; // 2 hours in seconds
    
    [SerializeField] private float playerDetectionRadius = 1f;

    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem hatchParticles;
    [SerializeField] private AudioClip hatchSound;
    [SerializeField] private GameObject pulsatingEffect;

    // Reference to needed components
    private AntIncubator parentIncubator;
    private UIHelper uiHelper;
    private Transform playerTransform;
    private float hatchingProgress = 0f;
    private bool isPlayerNearby = false;
    private bool isHatching = false;

    private void Awake()
    {

    }

    private void Start()
    {
        // Find references
        parentIncubator = GetComponentInParent<AntIncubator>();
        uiHelper = FindObjectOfType<UIHelper>();
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Start hatching process
        isHatching = true;
        StartCoroutine(HatchEgg());

        // Start checking for player proximity
        StartCoroutine(CheckPlayerProximity());
        
        // Log initial hatch time
        Debug.Log($"Egg started hatching. Will hatch in {FormatTime(hatchTime)}");
        
        // Update egg count in UI
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(1);
        }
    }

    private IEnumerator HatchEgg()
    {
        if (!isHatching)
        {
            Debug.LogError("HatchEgg coroutine started, but isHatching is false!");
            yield break;
        }
        
        // Wait for the specified hatch time
        float elapsedTime = 0f;
        float lastLogTime = -10f; // Log progress every 10 seconds
        
        Debug.Log("Starting egg hatching process...");
        
        while (elapsedTime < hatchTime)
        {
            // Update elapsed time and progress
            float deltaTime = Time.deltaTime;
            elapsedTime += deltaTime;
            hatchingProgress = elapsedTime / hatchTime;
            
            // Log progress periodically for debugging
            if (elapsedTime - lastLogTime > 10f)
            {
                float remaining = hatchTime - elapsedTime;
                Debug.Log($"Egg hatching progress: {hatchingProgress:P2}, time remaining: {FormatTime(remaining)}");
                lastLogTime = elapsedTime;
            }
            
            // Optionally, you could scale the egg or change its appearance based on progress
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, hatchingProgress);
            
            yield return null;
        }
        
        Debug.Log("Egg hatching complete! Spawning ant...");
        
        // Spawn the ant
        SpawnAnt();
        
        // Notify incubator that egg has hatched
        if (parentIncubator != null)
        {
            parentIncubator.OnEggHatched();
        }
        
        // Show hatching message if player is nearby
        if (isPlayerNearby && uiHelper != null && uiHelper.informPlayerText != null)
        {
            uiHelper.ShowInformText("An ant has hatched!");
        }
        
        // Play effects
        if (hatchParticles != null)
        {
            // Detach particle system before destroying egg
            hatchParticles.transform.SetParent(null);
            hatchParticles.Play();
            Destroy(hatchParticles.gameObject, hatchParticles.main.duration);
        }
        
        if (hatchSound != null)
        {
            AudioSource.PlayClipAtPoint(hatchSound, transform.position);
        }
        
        // Destroy egg
        Destroy(gameObject);
    }
    
    private IEnumerator CheckPlayerProximity()
    {
        while (isHatching)
        {
            if (playerTransform != null)
            {
                // Check distance to player
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                bool wasPlayerNearby = isPlayerNearby;
                isPlayerNearby = distanceToPlayer <= playerDetectionRadius;
                
                // If player just entered proximity range, show message
                if (isPlayerNearby && !wasPlayerNearby)
                {
                    UpdatePlayerMessage();
                }
                // If player is staying in proximity, update message periodically
                else if (isPlayerNearby)
                {
                    if (Time.frameCount % 30 == 0) // Update every ~0.5 seconds at 60 FPS
                    {
                        UpdatePlayerMessage();
                    }
                }
                // If player left proximity, clear message
                else if (!isPlayerNearby && wasPlayerNearby)
                {
                    if (uiHelper != null && uiHelper.informPlayerText != null)
                    {
                        uiHelper.ShowInformText("");
                    }
                }
            }
            
            yield return new WaitForSeconds(0.1f); // Check proximity every 0.1 seconds
        }
    }
    
    private void UpdatePlayerMessage()
    {
        if (uiHelper != null && uiHelper.informPlayerText != null)
        {
            float remainingTime = hatchTime * (1 - hatchingProgress);
            
            uiHelper.ShowInformText($"Ant egg hatching in {FormatTime(remainingTime)}");
        }
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int totalSeconds = Mathf.CeilToInt(timeInSeconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        
        return $"{hours}:{minutes:D2}:{seconds:D2}";
    }
    
    private void SpawnAnt()
    {
        if (antPrefab != null)
        {
            // Instantiate the ant slightly above the egg to avoid collision issues
            Vector3 spawnPosition = transform.position + Vector3.up * 0.1f;
            GameObject newAnt = Instantiate(antPrefab, spawnPosition, Quaternion.identity);
            
            // You can add any initialization code for the new ant here
            Debug.Log("Ant hatched from egg!");
        }
        else
        {
            Debug.LogError("No ant prefab assigned to egg!");
        }
    }
    
    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (uiHelper != null && uiHelper.informPlayerText != null)
        {
            uiHelper.ShowInformText("");
        }
    }
    
    private void OnDestroy()
    {
        // Ensure we mark the egg as no longer hatching when destroyed
        // This prevents any potential errors from coroutines
        isHatching = false;
        
        // Reset the static flag in AntIncubator using the public method
        AntIncubator.ResetEggExistsFlag();
        
        // Update egg count in UI when egg is destroyed
        if (uiHelper != null)
        {
            uiHelper.UpdateEggDisplay(0);
        }
    }
}
