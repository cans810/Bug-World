using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private float spawnHeightOffset = 0.5f; // Spawn chitin higher than entity position
    [SerializeField] private float scatterRadius = 0.5f; // How far to scatter multiple chitin pieces
    [SerializeField] private float maxDropForce = 2.0f; // Maximum force to apply to dropped chitin
    
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
        Debug.Log($"Dropping {chitinAmount} chitin from {gameObject.name}");
        
        StartCoroutine(SpawnChitinPieces(chitinAmount));
    }
    
    private IEnumerator SpawnChitinPieces(int amount)
    {
        // Base position for drops
        Vector3 basePosition = transform.position + Vector3.up * spawnHeightOffset;
        
        // Spawn multiple chitin pieces if amount > 1
        for (int i = 0; i < amount; i++)
        {
            // Calculate random offset within scatter radius
            Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPosition = basePosition + new Vector3(randomOffset.x, 0, randomOffset.y);
            
            // Instantiate the chitin
            GameObject chitinInstance = Instantiate(chitinPrefab, spawnPosition, 
                Quaternion.Euler(0, Random.Range(0, 360), 0));
            
            // Add physics forces if it has a rigidbody
            Rigidbody rb = chitinInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply random force to scatter
                Vector3 force = new Vector3(
                    Random.Range(-maxDropForce, maxDropForce),
                    Random.Range(1f, maxDropForce),
                    Random.Range(-maxDropForce, maxDropForce)
                );
                rb.AddForce(force, ForceMode.Impulse);
                
                // Make sure it's active for physics
                rb.isKinematic = false;
            }
            
            Debug.Log($"Spawned chitin piece {i+1}/{amount} at {spawnPosition}");
            
            // Small delay between spawns to avoid physics issues
            yield return new WaitForSeconds(0.05f);
        }
    }
} 