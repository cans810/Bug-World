using UnityEngine;
using System.Collections;

public class PlayerHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    [SerializeField] private float damage = 10f;
    
    [Header("Attack Settings")]
    [SerializeField] private string attackAnimationBool = "Attack";
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackDuration = 0.3f; // How long the attack animation stays active
    
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    
    // Reference to the player (owner of this hitbox)
    private GameObject player;
    private Animator playerAnimator;
    private float lastAttackTime = -999f;
    
    // Layer check
    private int insectsLayer;
    
    private void Start()
    {
        // Get reference to the player (parent object)
        player = transform.root.gameObject;
        
        // Get the player's animator
        playerAnimator = player.GetComponent<Animator>();
        if (playerAnimator == null)
        {
            playerAnimator = player.GetComponentInChildren<Animator>();
        }
        
        // Get the Insects layer
        insectsLayer = LayerMask.NameToLayer("Insects");
        
        // Make sure the collider is a trigger
        Collider hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError("PlayerHitbox requires a Collider component!");
        }
        
        if (showDebugMessages)
        {
            Debug.Log($"Player Hitbox initialized on {gameObject.name}");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Skip if this is the player itself or part of the player
        if (other.transform.IsChildOf(player.transform) || other.gameObject == player)
        {
            return;
        }
        
        // Debug output
        if (showDebugMessages)
        {
            Debug.Log($"Player Hitbox collided with: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        }
        
        // Check if the object is on the Insects layer
        if (other.gameObject.layer == insectsLayer)
        {
            // Trigger attack animation if cooldown has passed
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"Insect detected in hitbox! Triggering attack animation.");
                }
                
                // Play attack animation using bool parameter
                if (playerAnimator != null)
                {
                    StartCoroutine(PerformAttackAnimation());
                    lastAttackTime = Time.time;
                }
                else if (showDebugMessages)
                {
                    Debug.LogWarning("No Animator found on player or its children!");
                }
                
                // Check if the insect has a LivingEntity component
                LivingEntity entity = other.GetComponent<LivingEntity>();
                if (entity == null)
                {
                    entity = other.GetComponentInParent<LivingEntity>();
                }
                
                // If we found a LivingEntity, damage it
                if (entity != null)
                {
                    if (showDebugMessages)
                    {
                        Debug.Log($"Dealing {damage} damage to {other.gameObject.name}. Current health: {entity.CurrentHealth}");
                    }
                    
                    entity.TakeDamage(damage, player);
                    
                    if (showDebugMessages)
                    {
                        Debug.Log($"After damage health: {entity.CurrentHealth}");
                    }
                }
                else if (showDebugMessages)
                {
                    Debug.Log($"No LivingEntity component found on {other.gameObject.name}");
                }
            }
            else if (showDebugMessages)
            {
                Debug.Log($"Attack on cooldown. Time remaining: {(lastAttackTime + attackCooldown) - Time.time}");
            }
        }
        else if (showDebugMessages)
        {
            Debug.Log($"Object is not on Insects layer. No attack triggered.");
        }
    }
    
    // Coroutine to handle the attack animation using a bool parameter
    private System.Collections.IEnumerator PerformAttackAnimation()
    {
        // Set the attack bool to true
        playerAnimator.SetBool(attackAnimationBool, true);
        
        // Wait for the attack duration
        yield return new WaitForSeconds(attackDuration);
        
        // Set the attack bool back to false
        playerAnimator.SetBool(attackAnimationBool, false);
    }
    
}