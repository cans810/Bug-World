using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private float spawnHeightOffset = 0.2f; // Height above entity to spawn
    [SerializeField] private float scatterRadius = 0.3f; // Horizontal scatter radius
    [SerializeField] private bool usePhysicsScatter = true; // Changed to true by default
    [SerializeField] private float maxDropForce = 2.0f; // Increased back to 2.0
    
    private LivingEntity livingEntity;
    
    private void Start()
    {
        livingEntity = GetComponent<LivingEntity>();
        
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(DropChitin);
        }
        else
        {
            Debug.LogError("ChitinDropper requires a LivingEntity component on the same GameObject!");
        }
    }
    
    private void OnDestroy()
    {
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(DropChitin);
        }
    }
    
    private void DropChitin()
    {
        if (chitinPrefab == null || livingEntity == null)
            return;
            
        int chitinAmount = livingEntity.chitinAmount;
        Debug.Log($"Dropping {chitinAmount} chitin from {gameObject.name} at position {transform.position}");
        
        StartCoroutine(SpawnChitinPieces(chitinAmount));
    }
    
    private IEnumerator SpawnChitinPieces(int amount)
    {
        // Use the actual position to ensure correct drop location
        Vector3 basePosition = transform.position;
        
        // Spawn multiple chitin pieces if amount > 1
        for (int i = 0; i < amount; i++)
        {
            // Calculate random offset within scatter radius (only X and Z)
            Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPosition = basePosition + new Vector3(randomOffset.x, spawnHeightOffset, randomOffset.y);
            
            // Instantiate the chitin
            GameObject chitinInstance = Instantiate(chitinPrefab, spawnPosition, 
                Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
            
            Rigidbody rb = chitinInstance.GetComponent<Rigidbody>();
            ChitinBehavior behavior = chitinInstance.GetComponent<ChitinBehavior>();
            
            // Handle physics differently based on setting
            if (usePhysicsScatter)
            {
                // If using physics scatter and we have a ChitinBehavior, just let it handle physics
                if (behavior != null)
                {
                    // Make sure behavior is enabled
                    behavior.enabled = true;
                    
                    // We'll let ChitinBehavior handle the forces
                    // Ensure it wakes up
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.WakeUp();
                    }
                }
                // If no ChitinBehavior, apply our own forces
                else if (rb != null)
                {
                    // Apply upward and outward force
                    Vector3 force = new Vector3(
                        Random.Range(-maxDropForce * 0.5f, maxDropForce * 0.5f),
                        Random.Range(1.5f, maxDropForce * 1.2f), // More upward force
                        Random.Range(-maxDropForce * 0.5f, maxDropForce * 0.5f)
                    );
                    rb.AddForce(force, ForceMode.Impulse);
                    rb.isKinematic = false;
                    
                    // Add some spin
                    rb.AddTorque(new Vector3(
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 1f)
                    ), ForceMode.Impulse);
                }
            }
            else
            {
                // If not using physics scatter, disable the ChitinBehavior component
                if (behavior != null)
                {
                    behavior.enabled = false;
                }
                
                // Apply minimal forces
                if (rb != null)
                {
                    Vector3 force = new Vector3(
                        Random.Range(-0.5f, 0.5f),
                        0.5f, // Small upward bump
                        Random.Range(-0.5f, 0.5f)
                    );
                    rb.AddForce(force, ForceMode.Impulse);
                }
            }
            
            Debug.Log($"Spawned chitin piece {i+1}/{amount} at {spawnPosition}");
            
            // Small delay between spawns
            yield return new WaitForSeconds(0.05f);
        }
    }
} 