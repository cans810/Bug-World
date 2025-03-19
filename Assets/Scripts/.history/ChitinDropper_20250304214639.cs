using UnityEngine;
using System.Collections;

public class ChitinDropper : MonoBehaviour
{
    [SerializeField] private GameObject chitinPrefab;
    [SerializeField] private float spawnHeightOffset = 0.5f; // Spawn chitin higher than entity position
    
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
            // Create the chitin at a position slightly above the entity
            Vector3 spawnPosition = transform.position + Vector3.up * spawnHeightOffset;
            
            // Simply instantiate the chitin and let its own behavior script handle the physics
            GameObject chitinInstance = Instantiate(chitinPrefab, spawnPosition, Quaternion.identity);
            
            // No force application here - ChitinBehavior will handle that
        }
        else
        {
            Debug.LogWarning("No Chitin prefab assigned to ChitinDropper on " + gameObject.name);
        }
    }
} 