using UnityEngine;
using System.Collections;

public class PlayerHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    [SerializeField] private float damage = 10f;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0; // Left mouse button
    
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
        // Check for manual attack input
        if (Input.GetKeyDown(attackKey))
        {
            // Only attack if there's an insect in range and cooldown has passed
            if (insectInHitbox && currentInsect != null && Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
            else if (showDebugMessages && insectInHitbox == false)
            {
                Debug.Log("Attack button pressed, but no insect in range");
            }
        }
        
        // Check if we need to stop the attack animation
        if (animController != null && animController.IsAnimationPlaying("attack"))
        {
            // If the attack animation has been playing for longer than the cooldown,
            // or if there's no insect in range anymore, stop it
            if (Time.time >= lastAttackTime + attackCooldown || !insectInHitbox)
            {
                StopAttacking();
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to an insect
        if (other.gameObject.layer == insectsLayer)
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
                // Store references to the insect
                currentInsect = other.gameObject;
                currentInsectEntity = insectEntity;
                
                // Mark that an insect is in the hitbox
                insectInHitbox = true;
                
                if (showDebugMessages)
                {
                    Debug.Log($"Insect entered hitbox: {other.gameObject.name}");
                }
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting collider is the current insect
        if (currentInsect != null && other.gameObject == currentInsect)
        {
            if (showDebugMessages)
            {
                Debug.Log($"Insect left hitbox: {other.gameObject.name}");
            }
            
            // Clear references to the insect
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