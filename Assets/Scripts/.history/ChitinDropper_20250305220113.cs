using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [Header("Chitin Drop Settings")]
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private float scatterRadius = 5f;
    [SerializeField] private float dropHeight = 0.2f; // REDUCED from 0.5f - start closer to ground
    
    [Header("Physics Forces")]
    [SerializeField] private float upwardForceMin = 1.0f;  // REDUCED from 5f - much lower height
    [SerializeField] private float upwardForceMax = 2.0f;  // REDUCED from 7f - much lower height
    [SerializeField] private float horizontalForce = 3f;    // Keep current horizontal force
    [SerializeField] private float explosionRadius = 0.5f;  // Keep current explosion radius
    
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
            // Create the chitin piece
            Vector3 dropPosition = livingEntity.transform.position + new Vector3(0, dropHeight, 0);
            GameObject chitinObject = Instantiate(chitinPrefab, dropPosition, Quaternion.Euler(0, Random.Range(0, 360), 0));
            
            // Add physics force if it has a rigidbody
            Rigidbody rb = chitinObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Calculate direction - explode outward in all directions
                float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(
                    Mathf.Cos(angle) * explosionRadius,
                    0,
                    Mathf.Sin(angle) * explosionRadius
                ).normalized;
                
                // Apply stronger upward force with substantial horizontal movement
                float upForce = Random.Range(upwardForceMin, upwardForceMax);
                float horizontalMagnitude = Random.Range(horizontalForce * 0.5f, horizontalForce);
                
                Vector3 force = new Vector3(
                    direction.x * horizontalMagnitude,
                    upForce,
                    direction.z * horizontalMagnitude
                );
                
                rb.AddForce(force, ForceMode.Impulse);
                
                // Add some torque for spinning (significantly reduced)
                rb.AddTorque(new Vector3(
                    Random.Range(-3f, 3f),
                    Random.Range(-3f, 3f),
                    Random.Range(-3f, 3f)
                ), ForceMode.Impulse);
            }
            
            // Small delay between spawns to avoid physics issues
            yield return new WaitForSeconds(0.05f);
        }
    }
} 