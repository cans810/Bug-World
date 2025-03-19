using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack
    
    [Header("Attack Settings")]
    [SerializeField] private float attackInterval = 1.5f; // Minimum time between attacks
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal states
    private bool isMoving = false;
    private float lastAttackTime = -999f;
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
    }
    
    private void Update()
    {
        // Don't do anything if dead
        if (livingEntity.IsDead)
            return;
            
        LivingEntity target = livingEntity.CurrentTarget;
        
        // If we have a target in range (set by EntityHitbox)
        if (target != null && !target.IsDead)
        {
            // Get distance to target
            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            
            // If we're close enough to attack
            if (distanceToTarget <= attackDistance)
            {
                // Stop moving
                isMoving = false;
                
                // Face the target
                FaceTarget(target.transform.position);
                
                // Update animation
                if (animController != null)
                {
                    animController.SetWalking(false);
                }
                
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
                
                // Face and move towards target
                FaceTarget(target.transform.position);
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
                
                // Update animation
                if (animController != null)
                {
                    animController.SetWalking(true);
                }
            }
        }
        // No target in range
        else
        {
            // Stop moving
            isMoving = false;
            
            // Update animation
            if (animController != null)
            {
                animController.SetWalking(false);
            }
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
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw attack radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
} 