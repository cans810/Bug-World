using UnityEngine;
using System.Collections.Generic;

public class EntityHitbox : MonoBehaviour
{
    [SerializeField] private LivingEntity owner;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private bool showDebugMessages = false;
    
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
    
    // Optional: Visualize the hitbox in the editor
    private void OnDrawGizmos()
    {
        Collider hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider == null)
            return;
            
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        
        if (hitboxCollider is BoxCollider)
        {
            BoxCollider boxCollider = hitboxCollider as BoxCollider;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
        else if (hitboxCollider is SphereCollider)
        {
            SphereCollider sphereCollider = hitboxCollider as SphereCollider;
            Gizmos.DrawSphere(transform.TransformPoint(sphereCollider.center), sphereCollider.radius);
        }
        else if (hitboxCollider is CapsuleCollider)
        {
            // Simplified capsule visualization
            CapsuleCollider capsuleCollider = hitboxCollider as CapsuleCollider;
            Gizmos.DrawSphere(transform.position, capsuleCollider.radius);
        }
    }
} 