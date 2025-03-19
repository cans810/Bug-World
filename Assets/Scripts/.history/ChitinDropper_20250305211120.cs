using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [Header("Chitin Drop Settings")]
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private float scatterRadius = 0.05f;
    [SerializeField] private float dropHeight = 0.3f;
    [SerializeField] private float maxDropForce = 1.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Reference to the LivingEntity
    private LivingEntity livingEntity;
    
    private void Awake()
    {
        // Get the LivingEntity component
        livingEntity = GetComponent<LivingEntity>();
        
        // Subscribe to the death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(DropChitinOnDeath);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(DropChitinOnDeath);
        }
    }
    
    private void DropChitinOnDeath()
    {
        if (livingEntity == null || chitinPrefab == null)
            return;
            
        int amountToDrop = livingEntity.chitinAmount;
        
        if (showDebugLogs)
            Debug.Log($"Dropping {amountToDrop} chitin pieces from {gameObject.name}");
        
        // Start the coroutine to spawn pieces one by one
        StartCoroutine(SpawnChitinPieces(amountToDrop));
    }
    
    private IEnumerator SpawnChitinPieces(int amount)
    {
        // Safety check
        if (amount <= 0 || chitinPrefab == null)
            yield break;
            
        // Spawn one piece at a time with a tiny delay
        for (int i = 0; i < amount; i++)
        {
            // Random position within scatter radius
            Vector2 randomCircle = Random.insideUnitCircle * scatterRadius;
            Vector3 dropPosition = livingEntity.transform.position + new Vector3(0, dropHeight, 0);
            
            // Create the chitin piece
            GameObject chitinObject = Instantiate(chitinPrefab, dropPosition, Quaternion.Euler(0, Random.Range(0, 360), 0));
            
            // Add physics force if it has a rigidbody
            Rigidbody rb = chitinObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply random force to scatter
                Vector3 force = new Vector3(
                    Random.Range(-maxDropForce, maxDropForce),
                    Random.Range(0.2f, maxDropForce),
                    Random.Range(-maxDropForce, maxDropForce)
                );
                rb.AddForce(force, ForceMode.Impulse);
            }
            
            // Small delay between spawns to avoid physics issues
            yield return new WaitForSeconds(0.05f);
        }
    }
} 