using UnityEngine;
using System.Collections.Generic;

public class EntityHitbox : MonoBehaviour
{
    [Tooltip("The LivingEntity that owns this hitbox")]
    [SerializeField] private LivingEntity owner;
    
    [Tooltip("Layers that can be detected by this hitbox")]
    [SerializeField] private LayerMask targetLayers;
    
    [Tooltip("Layer that contains loot objects")]
    [SerializeField] private LayerMask lootLayer;
    
    [Tooltip("Enable for debugging collision detection")]
    [SerializeField] private bool showDebugMessages = false;
    
    // Reference to ally AI if owner is an ally
    private AllyAI ownerAllyAI;
    
    private void Awake()
    {
        // Find owner if not set
        if (owner == null)
        {
            owner = GetComponentInParent<LivingEntity>();
            if (owner == null)
            {
                Debug.LogError("EntityHitbox requires a LivingEntity owner!");
            }
        }
        
        // Check if owner is an Ally
        if (owner != null)
        {
            ownerAllyAI = owner.GetComponent<AllyAI>();
        }
        
        // Make sure the collider is a trigger
        Collider hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError("EntityHitbox requires a Collider component!");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider is a loot object and owner is an Ally
        if (owner != null && IsInLootLayer(other.gameObject) && ownerAllyAI != null)
        {
            // Notify the AllyAI that loot has been detected
            ownerAllyAI.LootDetected(other.transform);
            
            if (showDebugMessages)
            {
                Debug.Log($"{owner.name}'s hitbox detected loot: {other.name}");
            }
            
            return; // Skip the rest of the method since we've handled the loot
        }
        
        // Check if the collider belongs to a valid target
        if (owner != null && IsInTargetLayers(other.gameObject))
        {
            // Get the LivingEntity component from the target
            LivingEntity targetEntity = other.GetComponent<LivingEntity>();
            if (targetEntity == null)
            {
                targetEntity = other.GetComponentInParent<LivingEntity>();
            }
            
            // If the target has a LivingEntity component, is not dead, and is not the owner
            if (targetEntity != null && !targetEntity.IsDead && targetEntity != owner)
            {
                // Add the target to the owner's list of targets in range
                owner.AddTargetInRange(targetEntity);
                
                if (showDebugMessages)
                {
                    Debug.Log($"{owner.name}'s hitbox detected target: {targetEntity.name}");
                }
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if the collider is a loot object and owner is an Ally
        if (owner != null && IsInLootLayer(other.gameObject) && ownerAllyAI != null)
        {
            // Notify the AllyAI that loot is no longer in range if needed
            // This is optional based on your game design
            
            if (showDebugMessages)
            {
                Debug.Log($"{owner.name}'s hitbox lost loot: {other.name}");
            }
            
            return; // Skip the rest of the method since we've handled the loot
        }
        
        // Check if the collider belongs to a valid target
        if (owner != null && IsInTargetLayers(other.gameObject))
        {
            // Get the LivingEntity component from the target
            LivingEntity targetEntity = other.GetComponent<LivingEntity>();
            if (targetEntity == null)
            {
                targetEntity = other.GetComponentInParent<LivingEntity>();
            }
            
            // If the target has a LivingEntity component
            if (targetEntity != null)
            {
                // Remove the target from the owner's list of targets in range
                owner.RemoveTargetInRange(targetEntity);
                
                if (showDebugMessages)
                {
                    Debug.Log($"{owner.name}'s hitbox lost target: {targetEntity.name}");
                }
            }
        }
    }
    
    private bool IsInTargetLayers(GameObject obj)
    {
        return ((1 << obj.layer) & targetLayers) != 0;
    }
    
    private bool IsInLootLayer(GameObject obj)
    {
        return ((1 << obj.layer) & lootLayer) != 0;
    }
} 