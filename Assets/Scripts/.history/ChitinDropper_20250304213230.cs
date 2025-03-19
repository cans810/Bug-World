using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private float upwardForce = 2f;
    [SerializeField] private float scatterForce = 1f;
    
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
            }
        }
        else
        {
            Debug.LogWarning("No Chitin prefab assigned to ChitinDropper on " + gameObject.name);
        }
    }
} 