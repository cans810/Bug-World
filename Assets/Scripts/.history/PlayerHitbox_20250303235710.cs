using UnityEngine;
using System.Collections;

public class PlayerHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    [SerializeField] private float damage = 10f;
    
    [Header("Attack Settings")]
    [SerializeField] private string attackAnimationBool = "Attack";
    [SerializeField] private float attackCooldown = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    
    // Reference to the player (owner of this hitbox)
    private GameObject player;
    private Animator playerAnimator;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;
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
    
    private void Update()
    {
        // If there's an insect in the hitbox, keep attacking
        if (insectInHitbox && currentInsect != null)
        {
            // Check if we can attack again (cooldown passed)
            if (Time.time >= lastAttackTime + attackCooldown && !isAttacking)
            {
                StartCoroutine(PerformAttack());
            }
        }
        else if (isAttacking)
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
            if (!isAttacking && Time.time >= lastAttackTime + attackCooldown)
            {
                StartCoroutine(PerformAttack());
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
    
    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Set the attack animation bool to true
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(attackAnimationBool, true);
            
            if (showDebugMessages)
            {
                Debug.Log("Started attack animation");
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
        
        // Wait for the cooldown
        yield return new WaitForSeconds(attackCooldown);
        
        // If the insect is still in the hitbox, we'll start another attack in the Update method
        isAttacking = false;
    }
    
    private void StopAttacking()
    {
        // Set the attack animation bool to false
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(attackAnimationBool, false);
            
            if (showDebugMessages)
            {
                Debug.Log("Stopped attack animation");
            }
        }
        
        isAttacking = false;
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