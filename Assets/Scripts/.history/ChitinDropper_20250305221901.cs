using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [Header("Chitin Drop Settings")]
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private float scatterRadius = 5f;
    [SerializeField] private float dropHeight = 0.2f; // REDUCED from 0.5f - start closer to ground
    
    [Header("Physics Forces")]
    [SerializeField] private float upwardForceMin = 0.2f; 
    [SerializeField] private float upwardForceMax = 0.5f;  
    [SerializeField] private float horizontalForce = 0.3f; 
    [SerializeField] private float explosionRadius = 0.2f; 
    
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
            
            // Use a coroutine for a simple animation instead of physics
            StartCoroutine(SimpleChitinAnimation(chitinObject));
            
            // Small delay between spawns
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator SimpleChitinAnimation(GameObject chitinObject)
    {
        if (chitinObject == null) yield break;
        
        // Get the rigidbody but disable physics
        Rigidbody rb = chitinObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Starting position
        Vector3 startPos = chitinObject.transform.position;
        
        // Calculate a random landing position with MUCH MORE SPREAD
        float angle = Random.Range(0, 360) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * Random.Range(0.2f, 0.8f), // INCREASED from (0.1f, 0.4f)
            0,
            Mathf.Sin(angle) * Random.Range(0.2f, 0.8f)  // INCREASED from (0.1f, 0.4f)
        );
        
        // End position - on the ground with larger random offset
        Vector3 endPos = new Vector3(startPos.x + offset.x, 0.05f, startPos.z + offset.z);
        
        // Calculate peak position with SIGNIFICANTLY HIGHER arc
        float peakHeight = Random.Range(0.3f, 0.6f); // INCREASED from (0.2f, 0.35f)
        Vector3 peakPos = (startPos + endPos) * 0.5f + Vector3.up * peakHeight;
        
        // Animate over time (longer duration for more dramatic effect)
        float duration = 0.5f; // INCREASED from 0.4f
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            
            // Quadratic Bezier curve for a nice arc
            Vector3 m1 = Vector3.Lerp(startPos, peakPos, t);
            Vector3 m2 = Vector3.Lerp(peakPos, endPos, t);
            chitinObject.transform.position = Vector3.Lerp(m1, m2, t);
            
            // Faster rotation for more visual interest
            chitinObject.transform.Rotate(Vector3.up, 120 * Time.deltaTime); // INCREASED from 90
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end exactly at the end position
        chitinObject.transform.position = endPos;
        
        // Enable collection
        ChitinBehavior behavior = chitinObject.GetComponent<ChitinBehavior>();
        if (behavior != null)
        {
            behavior.ForceStick();
        }
    }
} 