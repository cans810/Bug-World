using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack
    [SerializeField] private float detectionFollowRange = 10f; // Max range to follow player after detection
    
    [Header("Attack Settings")]
    [SerializeField] private float attackInterval = 1.5f; // Minimum time between attacks
    [SerializeField] private float minAttackDistance = 1.0f; // Minimum distance required to attack player
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal states
    private bool isMoving = false;
    private float lastAttackTime = -999f;
    private LivingEntity currentTarget = null;
    private AIWandering wanderingBehavior;
    private Vector3 lastKnownPlayerPosition;
    private bool isChasing = false;
    
    private void Start()
    {
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
            
        // Get reference to the wandering behavior
        wanderingBehavior = GetComponent<AIWandering>();
        
        // Subscribe to death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(HandleDeath);
        }
        
        // Start idle
        UpdateAnimation(false);
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
        // Disable AI scripts
        if (wanderingBehavior != null)
            wanderingBehavior.enabled = false;
        
        // Ensure we stop all movement
        isMoving = false;
        isChasing = false;
        
        // Make sure we force interrupt any current animations, especially attack animations
        if (animController != null)
        {
            // Completely reset all animation states before playing death
            ResetAllAnimations();
            
            // Play death animation with a slight delay to ensure other animations are fully stopped
            StartCoroutine(PlayDeathAnimationWithPriority());
        }
        
        // Get the LivingEntity component and disable it
        if (livingEntity != null)
        {
            // Cancel any ongoing attack
            livingEntity.CancelAttack();
            
            // We don't disable it right away to allow death flash effects to complete
            StartCoroutine(DisableLivingEntityAfterEffects());
        }
        
        // Disable this script
        enabled = false;
        
        // Start coroutine to destroy the gameobject after 5 seconds
        StartCoroutine(DestroyAfterDelay(5f));
    }
    
    private void ResetAllAnimations()
    {
        // Reset all possible animation states to ensure nothing is playing
        if (animController != null)
        {
            animController.SetWalking(false);
            animController.SetAttacking(false);
            animController.SetEating(false);
            
            // If needed, you can also directly access the animator
            Animator animator = animController.GetComponent<Animator>();
            if (animator != null)
            {
                // Reset all animator parameters that might keep animations playing
                animator.SetBool("Walk", false);
                animator.SetBool("Attack", false);
                animator.SetBool("Eat", false);
                animator.SetBool("Idle", false);
                animator.ResetTrigger("AttackTrigger");
                
                // Optional: Force the animator to a neutral state
                animator.Play("Idle", 0, 0f);
            }
        }
    }
    
    private IEnumerator PlayDeathAnimationWithPriority()
    {
        // Wait for two frames to ensure animations have truly stopped and reset
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        // Now set the death state with highest priority
        if (animController != null)
        {
            // Get direct reference to animator to ensure our death animation takes priority
            Animator animator = animController.GetComponent<Animator>();
            if (animator != null)
            {
                // Force immediate transition to death animation with high priority
                animator.SetBool("Death", true);
                
                // Optional: Directly play death animation (if you have direct access to state name)
                // animator.Play("Death", 0, 0f);
            }
            
            animController.SetDead();
        }
    }
    
    private IEnumerator DisableLivingEntityAfterEffects()
    {
        // Wait for any death effects (like flash) to complete
        yield return new WaitForSeconds(1f);
        
        // Now disable the LivingEntity component
        if (livingEntity != null)
            livingEntity.enabled = false;
    }
    
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Destroy the gameobject
        Destroy(gameObject);
    }
    
    private void Update()
    {
        // Don't do anything if dead
        if (livingEntity == null || livingEntity.IsDead)
            return;
        
        // Check if we have any targets in range detected by our hitbox
        if (livingEntity.HasTargetsInRange())
        {
            // Get the closest valid target from the livingEntity
            currentTarget = livingEntity.GetClosestValidTarget();
            
            // If we found a target, start chasing
            if (currentTarget != null && !currentTarget.IsDead)
            {
                // Disable wandering behavior while chasing
                if (wanderingBehavior != null)
                    wanderingBehavior.SetWanderingEnabled(false);
                
                isChasing = true;
                lastKnownPlayerPosition = currentTarget.transform.position;
                
                // Chase and attack logic
                ChaseAndAttackTarget();
            }
            else
            {
                ReturnToWandering();
            }
        }
        // If we were chasing but lost the target, check if we should continue to last known position
        else if (isChasing)
        {
            float distanceToLastKnown = Vector3.Distance(transform.position, lastKnownPlayerPosition);
            
            // If we're still within follow range of the last known position, continue moving there
            if (distanceToLastKnown > attackDistance && distanceToLastKnown < detectionFollowRange)
            {
                isMoving = true;
                UpdateAnimation(true);
                
                // Face and move towards last known position
                FaceTarget(lastKnownPlayerPosition);
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
                
                // If we've reached the last known position and still don't see the player, return to wandering
                if (distanceToLastKnown <= attackDistance)
                {
                    ReturnToWandering();
                }
            }
            else
            {
                ReturnToWandering();
            }
        }
    }
    
    private void ChaseAndAttackTarget()
    {
        // Get distance to target
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        
        // If we're close enough to attack
        if (distanceToTarget <= minAttackDistance)
        {
            // Stop moving
            isMoving = false;
            UpdateAnimation(false);
            
            // Face the target
            FaceTarget(currentTarget.transform.position);
            
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
            FaceTarget(currentTarget.transform.position);
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
    }
    
    private void ReturnToWandering()
    {
        isChasing = false;
        currentTarget = null;
        
        // Stop moving
        isMoving = false;
        UpdateAnimation(false);
        
        // Re-enable wandering behavior
        if (wanderingBehavior != null)
            wanderingBehavior.SetWanderingEnabled(true);
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
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
} 