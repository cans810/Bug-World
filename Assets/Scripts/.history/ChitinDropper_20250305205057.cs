using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private float spawnHeightOffset = 0.2f; // Reduced height to start closer to insect
    [SerializeField] private float scatterRadius = 0.3f; // Reduced scatter radius
    [SerializeField] private bool usePhysicsScatter = false; // New option to control behavior
    [SerializeField] private float maxDropForce = 1.0f; // Reduced drop force
    
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
            
            // If we're using physics scatter, apply force here
            if (usePhysicsScatter)
            {
                Rigidbody rb = chitinInstance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Apply very mild force, mostly upward
                    Vector3 force = new Vector3(
                        Random.Range(-maxDropForce * 0.3f, maxDropForce * 0.3f),
                        Random.Range(0.5f, maxDropForce),
                        Random.Range(-maxDropForce * 0.3f, maxDropForce * 0.3f)
                    );
                    rb.AddForce(force, ForceMode.Impulse);
                    rb.isKinematic = false;
                }
            }
            else
            {
                // If not using physics scatter, disable the ChitinBehavior component
                // if it exists to prevent additional physics application
                ChitinBehavior behavior = chitinInstance.GetComponent<ChitinBehavior>();
                if (behavior != null)
                {
                    behavior.enabled = false;
                }
            }
            
            Debug.Log($"Spawned chitin piece {i+1}/{amount} at {spawnPosition}");
            
            // Small delay between spawns
            yield return new WaitForSeconds(0.05f);
        }
    }
} 