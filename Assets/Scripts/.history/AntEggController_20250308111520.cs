using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntEggController : MonoBehaviour
{
    [Header("Ant Settings")]
    [SerializeField] private GameObject antPrefab;
    [SerializeField] private float hatchTime = 5f;
    [SerializeField] private float playerDetectionRadius = 3f;
    
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

    // Start is called before the first frame update
    void Start()
    {
        // Find references
        parentIncubator = GetComponentInParent<AntIncubator>();
        uiHelper = FindObjectOfType<UIHelper>();
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Start hatching process
        StartCoroutine(HatchEgg());

        // Start checking for player proximity
        StartCoroutine(CheckPlayerProximity());
    }

    private IEnumerator HatchEgg()
    {
        // Wait for the specified hatch time
        float elapsedTime = 0f;
        
        while (elapsedTime < hatchTime)
        {
            elapsedTime += Time.deltaTime;
            hatchingProgress = elapsedTime / hatchTime;
            
            // Optionally, you could scale the egg or change its appearance based on progress
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.2f, hatchingProgress);
            
            yield return null;
        }
        
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
            uiHelper.informPlayerText.text = "An ant has hatched!";
            StartCoroutine(ClearMessageAfterDelay(2f));
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
        while (true)
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
                        uiHelper.informPlayerText.text = "";
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
            uiHelper.informPlayerText.text = $"Ant egg hatching in {Mathf.CeilToInt(remainingTime)} seconds!";
        }
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
            uiHelper.informPlayerText.text = "";
        }
    }
    
    // Optionally, visualize the detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
