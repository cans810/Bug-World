using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private float upwardForce = 2f;
    [SerializeField] private float scatterForce = 1f;
    [SerializeField] private bool disableGravity = true;
    [SerializeField] private float gravityDisableDelay = 0.2f; // Short delay to allow initial bounce
    
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
        if (chitinPrefab != null)
        {
            // Create the chitin at the entity's position
            GameObject chitinInstance = Instantiate(chitinPrefab, transform.position, Quaternion.identity);
            
            // Add physics effect if it has a rigidbody
            Rigidbody rb = chitinInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply a small upward force and random outward force
                Vector3 force = Vector3.up * upwardForce;
                force += Random.insideUnitSphere * scatterForce;
                rb.AddForce(force, ForceMode.Impulse);
                
                // Disable gravity after a short delay (for initial pop effect)
                if (disableGravity)
                {
                    StartCoroutine(DisableGravityAfterDelay(rb, gravityDisableDelay));
                }
            }
        }
        else
        {
            Debug.LogWarning("No Chitin prefab assigned to ChitinDropper on " + gameObject.name);
        }
    }
    
    private System.Collections.IEnumerator DisableGravityAfterDelay(Rigidbody rb, float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);
        
        // Disable gravity
        if (rb != null)
        {
            rb.useGravity = false;
            
            // Optional: zero out the vertical velocity to stop any remaining fall
            Vector3 velocity = rb.velocity;
            velocity.y = 0;
            rb.velocity = velocity;
        }
    }
} 