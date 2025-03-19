using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LootItem
{
    public GameObject lootPrefab;
    [Range(0, 100)]
    public float dropChance = 20f;  // Percentage chance to drop
    public int minQuantity = 1;
    public int maxQuantity = 3;
}

public class InsectLootDropper : MonoBehaviour
{
    [SerializeField] private List<LootItem> possibleLoot = new List<LootItem>();
    [SerializeField] private float dropRadius = 0.5f;
    [SerializeField] private float upwardForce = 2f;
    [SerializeField] private float scatterForce = 1f;
    
    private LivingEntity livingEntity;
    
    private void Start()
    {
        livingEntity = GetComponent<LivingEntity>();
        
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(DropLoot);
        }
    }
    
    private void OnDestroy()
    {
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(DropLoot);
        }
    }
    
    public void DropLoot()
    {
        foreach (LootItem item in possibleLoot)
        {
            // Check if this item should drop based on its chance
            if (Random.Range(0f, 100f) <= item.dropChance)
            {
                // Determine quantity
                int quantity = Random.Range(item.minQuantity, item.maxQuantity + 1);
                
                for (int i = 0; i < quantity; i++)
                {
                    // Instantiate the loot prefab
                    Vector3 dropPosition = transform.position + Random.insideUnitSphere * dropRadius;
                    dropPosition.y = transform.position.y; // Keep on same vertical level initially
                    
                    GameObject lootInstance = Instantiate(item.lootPrefab, dropPosition, Quaternion.identity);
                    
                    // Add some physics to the drop if it has a rigidbody
                    Rigidbody rb = lootInstance.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // Apply a small upward and outward force for a nice scatter effect
                        Vector3 force = Vector3.up * upwardForce;
                        force += Random.insideUnitSphere * scatterForce;
                        rb.AddForce(force, ForceMode.Impulse);
                    }
                }
            }
        }
    }
} 