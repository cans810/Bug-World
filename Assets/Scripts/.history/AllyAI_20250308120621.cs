using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float followDistance = 3f; // Distance to maintain from player
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack enemies
    
    [Header("Attack Settings")]
    [SerializeField] private float attackInterval = 1.5f; // Minimum time between attacks
    [SerializeField] private string[] friendlyTags = new string[] { "Player", "Ally" }; // Tags of entities NOT to attack
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal states
    private bool isMoving = false;
    private float lastAttackTime = -999f;
    private Transform playerTransform;
    private LivingEntity currentEnemyTarget = null;
    private AIWandering wanderingBehavior;
    private bool isChasing = false;
    
    private MapBoundary mapBoundary;
    
    private void Start()
    {
        // Find player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
            
        // Get reference to the wandering behavior
        wanderingBehavior = GetComponent<AIWandering>();
        
        // Ensure entity is set to be destroyed after death
        if (livingEntity != null)
        {
            // Subscribe to death event
            livingEntity.OnDeath.AddListener(HandleDeath);
        }
        
        // Start idle
        UpdateAnimation(false);
        
        // Find the boundary
        mapBoundary = FindObjectOfType<MapBoundary>();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from death event to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandleDeath);
        }
    }
    
    private void HandleDeath()
    {
        if (wanderingBehavior != null)
            wanderingBehavior.enabled = false;
        
        isMoving = false;
        isChasing = false;
        
        enabled = false;
    }
    
    private void Update()
    {
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
            
        // If player reference is lost, try to find it again
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null) return;
        }
        
        // Check if we have any enemy targets in range detected by our hitbox
        if (livingEntity.HasTargetsInRange())
        {
            // Get the closest valid target from the livingEntity
            LivingEntity potentialTarget = livingEntity.GetClosestValidTarget();
            
            // Skip friendly targets (player and other allies)
            if (potentialTarget != null && !potentialTarget.IsDead && !IsFriendlyEntity(potentialTarget))
            {
                // Disable wandering behavior while attacking
                if (wanderingBehavior != null)
                    wanderingBehavior.SetWanderingEnabled(false);
                
                currentEnemyTarget = potentialTarget;
                isChasing = true;
                
                // Attack logic
                ChaseAndAttackEnemy();
                return; // Skip player following logic
            }
        }
        
        // If no enemies or only friendly targets, follow player
        currentEnemyTarget = null;
        
        // If we have a player to follow
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // If we need to follow (too far from player)
            if (distanceToPlayer > followDistance)
            {
                // Disable wandering behavior while following
                if (wanderingBehavior != null)
                    wanderingBehavior.SetWanderingEnabled(false);
                
                isMoving = true;
                UpdateAnimation(true);
                
                // Face and move towards player
                FaceTarget(playerTransform.position);
                MoveWithBoundaryCheck(transform.forward);
            }
            // If we're close enough to player, stop moving and enable wandering
            else
            {
                isMoving = false;
                UpdateAnimation(false);
                
                // Enable wandering if we're just idle
                if (wanderingBehavior != null)
                    wanderingBehavior.SetWanderingEnabled(true);
            }
        }
    }
    
    // Check if an entity is friendly (player or ally)
    private bool IsFriendlyEntity(LivingEntity entity)
    {
        foreach (string tag in friendlyTags)
        {
            if (entity.gameObject.CompareTag(tag))
            {
                return true;
            }
        }
        return false;
    }
    
    private void ChaseAndAttackEnemy()
    {
        // Get distance to target
        float distanceToTarget = Vector3.Distance(transform.position, currentEnemyTarget.transform.position);
        
        // If we're close enough to attack
        if (distanceToTarget <= attackDistance)
        {
            // Stop moving
            isMoving = false;
            UpdateAnimation(false);
            
            // Face the target
            FaceTarget(currentEnemyTarget.transform.position);
            
            // Try to attack if cooldown has passed
            if (Time.time >= lastAttackTime + attackInterval)
            {
                if (livingEntity.TryAttack())
                {
                    lastAttackTime = Time.time;
                }
            }
        }
        // Otherwise, move towards the target
        else
        {
            isMoving = true;
            UpdateAnimation(true);
            
            // Face and move towards target
            FaceTarget(currentEnemyTarget.transform.position);
            
            // Move with boundary check
            MoveWithBoundaryCheck(transform.forward);
        }
    }
    
    // New method to handle movement with boundary checking (same as EnemyAI)
    private void MoveWithBoundaryCheck(Vector3 direction)
    {
        // Calculate the next position
        Vector3 nextPosition = transform.position + direction * livingEntity.moveSpeed * Time.deltaTime;
        
        // Check if the next position is within bounds
        if (mapBoundary != null && !mapBoundary.IsWithinBounds(nextPosition))
        {
            // Get the nearest safe position inside the boundary
            Vector3 safePosition = mapBoundary.GetNearestPointInBounds(nextPosition);
            
            // Calculate new direction along the boundary
            Vector3 redirectedDirection = (safePosition - transform.position).normalized;
            
            // Update facing direction
            transform.forward = redirectedDirection;
            
            // Move in the redirected direction
            transform.position += redirectedDirection * livingEntity.moveSpeed * Time.deltaTime;
        }
        else
        {
            // Move normally if within bounds
            transform.position += direction * livingEntity.moveSpeed * Time.deltaTime;
        }
    }
    
    // Helper method to update animations
    private void UpdateAnimation(bool isWalking)
    {
        if (animController != null)
        {
            animController.SetWalking(isWalking);
        }
    }
    
    private void FaceTarget(Vector3 target)
    {
        Vector3 directionToTarget = target - transform.position;
        directionToTarget.y = 0; // Keep rotation on Y axis only
        
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                livingEntity.rotationSpeed * Time.deltaTime
            );
        }
    }
    
    // Optional debug visuals
    private void OnDrawGizmosSelected()
    {
        // Draw follow range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, followDistance);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
