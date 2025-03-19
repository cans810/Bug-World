using UnityEngine;
using System.Collections;

public class PlayerHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    [SerializeField] private float damage = 10f;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    
    // Reference to the player (owner of this hitbox)
    private GameObject player;
    private AnimationController animController;
    private Animator animator;
    private float lastAttackTime = -999f;
    private bool insectInHitbox = false;
    
    // Layer check
    private int insectsLayer;
    
    // Reference to the current insect in the hitbox
    private GameObject currentInsect;
    private LivingEntity currentInsectEntity;
    
    private void Start()
    {
        // Get reference to the player (parent object)
        player = transform.root.gameObject;
        
        // Get the AnimationController
        animController = player.GetComponent<AnimationController>();
        if (animController == null)
        {
            animController = player.GetComponentInChildren<AnimationController>();
        }
        
        // Get the Animator directly as backup
        if (animController != null)
        {
            animator = animController.Animator;
        }
        else
        {
            animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                animator = player.GetComponentInChildren<Animator>();
            }
            
            if (animator == null && showDebugMessages)
            {
                Debug.LogError("No Animator component found!");
            }
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
    
    private void Update()
    {
        // If there's an insect in the hitbox, keep attacking
        if (insectInHitbox && currentInsect != null)
        {
            // Check if we can attack again (cooldown passed)
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }
        else if (animController != null && animController.IsAnimationPlaying("attack"))
        {
            // Stop attacking if no insect is in the hitbox
            StopAttacking();
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
            if (showDebugMessages)
            {
                Debug.Log($"Insect entered hitbox: {other.gameObject.name}");
            }
            
            // Set the current insect
            currentInsect = other.gameObject;
            
            // Get the LivingEntity component
            currentInsectEntity = other.GetComponent<LivingEntity>();
            if (currentInsectEntity == null)
            {
                currentInsectEntity = other.GetComponentInParent<LivingEntity>();
            }
            
            // Mark that an insect is in the hitbox
            insectInHitbox = true;
            
            // Start attacking immediately
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if the leaving object is our current insect
        if (other.gameObject == currentInsect)
        {
            if (showDebugMessages)
            {
                Debug.Log($"Insect left hitbox: {other.gameObject.name}");
            }
            
            // Clear the current insect
            currentInsect = null;
            currentInsectEntity = null;
            
            // Mark that no insect is in the hitbox
            insectInHitbox = false;
            
            // Stop attacking
            StopAttacking();
        }
    }
    
    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        
        // Start attack animation
        if (animController != null)
        {
            // Use the AnimationController's method to set attacking
            animController.SetAttacking(true);
            
            if (showDebugMessages)
            {
                Debug.Log("Started attack animation via AnimationController");
            }
        }
        else if (animator != null)
        {
            // Direct animator control as fallback
            animator.SetBool("Attack", true);
            
            if (showDebugMessages)
            {
                Debug.Log("Started attack animation via direct Animator control");
            }
        }
        
        // Deal damage to the insect
        if (currentInsectEntity != null && !currentInsectEntity.IsDead)
        {
            if (showDebugMessages)
            {
                Debug.Log($"Dealing {damage} damage to {currentInsect.name}. Current health: {currentInsectEntity.CurrentHealth}");
            }
            
            currentInsectEntity.TakeDamage(damage, player);
            
            if (showDebugMessages)
            {
                Debug.Log($"After damage health: {currentInsectEntity.CurrentHealth}");
            }
        }
    }
    
    private void StopAttacking()
    {
        if (animController != null)
        {
            // Use the AnimationController's method to stop attacking
            animController.SetAttacking(false);
            
            if (showDebugMessages)
            {
                Debug.Log("Stopped attack animation via AnimationController");
            }
        }
        else if (animator != null)
        {
            // Direct animator control as fallback
            animator.SetBool("Attack", false);
            
            if (showDebugMessages)
            {
                Debug.Log("Stopped attack animation via direct Animator control");
            }
        }
    }
}