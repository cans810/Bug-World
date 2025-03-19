using UnityEngine;
using System.Collections.Generic;
using System;  // Add this for Action/delegate support

public class EntityHitbox : MonoBehaviour
{
    [Tooltip("The LivingEntity that owns this hitbox")]
    [SerializeField] private LivingEntity owner;
    
    [Tooltip("Layers that can be detected by this hitbox")]
    [SerializeField] private LayerMask targetLayers;
    
    [Tooltip("Enable for debugging collision detection")]
    [SerializeField] private bool showDebugMessages = false;
    
    // Reference to ally AI if owner is an ally
    private AllyAI ownerAllyAI;
    
    // Reference to player controller if owner is player
    private PlayerController playerController;
    
    // Add events for player entering and exiting the hitbox
    public event Action<LivingEntity> OnPlayerEnterHitbox;
    public event Action<LivingEntity> OnPlayerExitHitbox;
    
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
            
            // Check if owner is the player
            if (owner.gameObject.CompareTag("Player"))
            {
                playerController = owner.GetComponent<PlayerController>();
            }
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
        // Check if owner is player or ally and the other object is an insect
        bool ownerIsPlayerOrAlly = playerController != null || ownerAllyAI != null;
        bool otherIsInsect = ((1 << other.gameObject.layer) & LayerMask.GetMask("Insects")) != 0;
        
        if (ownerIsPlayerOrAlly && otherIsInsect)
        {
            // Get the LivingEntity component from the insect
            LivingEntity insectEntity = other.GetComponent<LivingEntity>();
            if (insectEntity == null)
            {
                insectEntity = other.GetComponentInParent<LivingEntity>();
            }
            
            // If the insect has a LivingEntity component and is not dead
            if (insectEntity != null && !insectEntity.IsDead)
            {
                // Trigger the enter event for auto-attack
                OnPlayerEnterHitbox?.Invoke(insectEntity);
                
                // DISCOVERY SYSTEM - REMOVED - We now discover on kill, not on detection
                
                if (showDebugMessages)
                {
                    Debug.Log($"{owner.name}'s hitbox detected insect: {insectEntity.name}");
                }
            }
            return;
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
                
                // Check if this is the player
                if (targetEntity.gameObject.CompareTag("Player"))
                {
                    // Trigger the player enter event
                    OnPlayerEnterHitbox?.Invoke(targetEntity);
                }
                
                if (showDebugMessages)
                {
                    Debug.Log($"{owner.name}'s hitbox detected target: {targetEntity.name}");
                }
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if owner is player or ally and the other object is an insect
        bool ownerIsPlayerOrAlly = playerController != null || ownerAllyAI != null;
        bool otherIsInsect = ((1 << other.gameObject.layer) & LayerMask.GetMask("Insects")) != 0;
        
        if (ownerIsPlayerOrAlly && otherIsInsect)
        {
            // Get the LivingEntity component from the insect
            LivingEntity insectEntity = other.GetComponent<LivingEntity>();
            if (insectEntity == null)
            {
                insectEntity = other.GetComponentInParent<LivingEntity>();
            }
            
            // If the insect has a LivingEntity component
            if (insectEntity != null)
            {
                // Trigger the exit event to stop auto-attack
                OnPlayerExitHitbox?.Invoke(insectEntity);
                
                if (showDebugMessages)
                {
                    Debug.Log($"{owner.name}'s hitbox lost insect: {insectEntity.name}");
                }
            }
            return;
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
                
                // Check if this is the player
                if (targetEntity.gameObject.CompareTag("Player"))
                {
                    // Trigger the player exit event
                    OnPlayerExitHitbox?.Invoke(targetEntity);
                }
                
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
} 