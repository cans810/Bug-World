using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [Header("Chitin Drop Settings")]
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private int minChitinToDrop = 1;
    [SerializeField] private int maxChitinToDrop = 3;
    [SerializeField] private float dropHeight = 0.3f;
    
    [Header("Physics Forces")]
    [SerializeField] private float upwardForceMin = 2.0f;
    [SerializeField] private float upwardForceMax = 4.0f;
    [SerializeField] private float horizontalForceMin = 1.0f;
    [SerializeField] private float horizontalForceMax = 3.0f;
    [SerializeField] private float torqueForce = 0.3f;
    
    [Header("Collection Delay")]
    [SerializeField] private float collectionDelay = 0.8f; // Delay before chitins become collectible
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    [Header("Gizmo Settings")]
    [SerializeField] private bool showDropGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0.8f, 0.5f, 0.2f, 0.7f); // Amber color
    [SerializeField] private int gizmoSampleCount = 8; // How many sample trajectories to show
    
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
            Debug.Log($"Dropping {amountToDrop} chitin pieces from {gameObject.name} using physics");
        
        // Drop all chitin pieces at once
        for (int i = 0; i < amountToDrop; i++)
        {
            // Spawn and apply physics
            SpawnChitinWithPhysics();
            
            // Small delay between spawns to prevent explosion effect
            StartCoroutine(DelayBetweenSpawns(0.12f)); // Increased from 0.05f to 0.12f
        }
    }
    
    private IEnumerator DelayBetweenSpawns(float delay)
    {
        yield return new WaitForSeconds(delay);
    }
    
    private void SpawnChitinWithPhysics()
    {
        // Get the exact position of the GameObject including its height
        Vector3 spawnPosition = transform.position;
        
        // Create chitin with random rotation
        GameObject chitin = Instantiate(chitinPrefab, spawnPosition, Quaternion.Euler(
            Random.Range(0, 360),
            Random.Range(0, 360),
            Random.Range(0, 360)
        ));
        
        // Start with a tiny scale
        chitin.transform.localScale = chitin.transform.localScale * 0.01f; // Start at 1% of full size
        
        // Start the scaling animation
        StartCoroutine(ScaleChitinOverTime(chitin, 0.5f)); // Scale up over 0.5 seconds
        
        // Verify it has the ChitinCollectible component
        ChitinCollectible collectible = chitin.GetComponent<ChitinCollectible>();
        if (collectible == null)
        {
            collectible = chitin.AddComponent<ChitinCollectible>();
            Debug.LogWarning("Had to add ChitinCollectible component to spawned chitin");
        }
        
        // Set to non-collectible layer initially
        chitin.layer = LayerMask.NameToLayer("NonpickableLoot");
        
        // Get or add rigidbody
        Rigidbody rb = chitin.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = chitin.AddComponent<Rigidbody>();
            Debug.LogWarning("Had to add Rigidbody to chitin");
        }
        
        // Configure the Rigidbody for better physics
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = 0.5f;
        rb.drag = 0.2f;
        rb.angularDrag = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Apply reduced upward force for gentler arch
        float upwardForce = Random.Range(1.5f, 2.5f); // Reduced from 2.5-4.0 to 1.5-2.5
        
        // Apply extremely minimal horizontal force for almost vertical launch
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector3 horizontalForce = new Vector3(
            randomDirection.x,
            0,
            randomDirection.y
        ) * Random.Range(0.02f, 0.1f); // Further reduced from 0.05-0.2 to 0.02-0.1
        
        // Apply the combined force - more vertical than horizontal
        rb.AddForce(new Vector3(horizontalForce.x, upwardForce, horizontalForce.z), ForceMode.Impulse);
        
        // Apply very gentle torque for subtle spinning
        rb.AddTorque(new Vector3(
            Random.Range(-0.05f, 0.05f), 
            Random.Range(-0.05f, 0.05f),
            Random.Range(-0.05f, 0.05f)
        ), ForceMode.Impulse);
        
        // Get collider
        Collider collider = chitin.GetComponent<Collider>();
        if (collider != null)
        {
            // Make sure it's not a trigger
            collider.isTrigger = false;
            
            // Make sure the collider is not too large (can prevent proper physics)
            if (collider is BoxCollider boxCollider)
            {
                // Ensure the box collider size is reasonable
                if (boxCollider.size.magnitude > 5f)
                {
                    boxCollider.size = Vector3.one * 0.5f;
                    Debug.LogWarning("Adjusted oversized box collider");
                }
            }
        }
        else
        {
            Debug.LogError("Chitin has no collider!");
        }
        
        // Start coroutine to make collectible after delay
        StartCoroutine(MakeCollectibleAfterDelay(chitin, collectionDelay));
    }
    
    private IEnumerator ScaleChitinOverTime(GameObject chitin, float duration)
    {
        // Store the original scale
        Vector3 targetScale = chitin.transform.localScale * 100f; // The full size (100%)
        Vector3 startScale = chitin.transform.localScale; // Current tiny scale
        
        float elapsed = 0f;
        
        while (elapsed < duration && chitin != null)
        {
            // Calculate progress (0 to 1)
            float progress = elapsed / duration;
            
            // Use an ease-out curve for smoother growth
            float smoothProgress = Mathf.SmoothStep(0, 1, progress);
            
            // Set the new scale
            chitin.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothProgress);
            
            // Update elapsed time
            elapsed += Time.deltaTime;
            
            yield return null;
        }
        
        // Ensure final scale is set (if object still exists)
        if (chitin != null)
        {
            chitin.transform.localScale = targetScale;
        }
    }
    
    private IEnumerator MakeCollectibleAfterDelay(GameObject chitin, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Only proceed if the object still exists
        if (chitin != null)
        {
            chitin.layer = LayerMask.NameToLayer("Loot");
            
            // Enable the collectible component
            ChitinCollectible collectible = chitin.GetComponent<ChitinCollectible>();
            if (collectible != null)
            {
                collectible.enabled = true;
            }
        }
    }

    // Simplified visualization - just a red dot
    private void OnDrawGizmosSelected()
    {
        // Get the exact position of the GameObject
        Vector3 spawnPosition = transform.position;
        
        // Draw a red sphere at the spawn position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(spawnPosition, 0.2f);
        
        // Draw label with drop info
        #if UNITY_EDITOR
        UnityEditor.Handles.BeginGUI();
        UnityEditor.Handles.Label(spawnPosition + Vector3.up * 0.5f, 
            $"Chitin Drop: {minChitinToDrop}-{maxChitinToDrop} pieces");
        UnityEditor.Handles.EndGUI();
        #endif
    }
} 