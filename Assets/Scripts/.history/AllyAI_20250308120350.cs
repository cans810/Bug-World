using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 3f; // Distance to maintain from player
    [SerializeField] private float maxFollowRange = 15f; // Max range to follow player
    [SerializeField] private float followSpeed = 1.2f; // Multiplier for movement speed when following

    [Header("Combat Settings")]
    [SerializeField] private float attackDistance = 1.5f; // Distance at which to attack enemies
    [SerializeField] private float attackInterval = 1.5f; // Minimum time between attacks
    [SerializeField] private string[] friendlyTags = new string[] { "Player", "Ally" }; // Tags of entities NOT to attack

    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;

    // Internal states
    private Transform playerTransform;
    private bool isFollowingPlayer = false;
    private bool isAttackingEnemy = false;
    private LivingEntity currentEnemyTarget = null;
    private float lastAttackTime = -999f;
    private AIWandering wanderingBehavior;
    private Vector3 lastPlayerPosition;
    private MapBoundary mapBoundary;

    private void Start()
    {
        // Find player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("AllyAI: Player not found! Make sure player has 'Player' tag.");
        }

        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();

        if (animController == null)
            animController = GetComponent<AnimationController>();

        // Get reference to wandering behavior
        wanderingBehavior = GetComponent<AIWandering>();

        // Subscribe to death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(HandleDeath);
        }

        // Find map boundary
        mapBoundary = FindObjectOfType<MapBoundary>();

        // Start following the player
        StartFollowingPlayer();
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
        // Disable behaviors on death
        if (wanderingBehavior != null)
            wanderingBehavior.enabled = false;

        isFollowingPlayer = false;
        isAttackingEnemy = false;
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

        // Check if there are enemies in range to attack using the hitbox detection system
        if (livingEntity.HasTargetsInRange())
        {
            // Get the closest valid target - we'll filter friendly targets below
            LivingEntity potentialTarget = livingEntity.GetClosestValidTarget();
            
            // If we found a target and it's not friendly
            if (potentialTarget != null && !potentialTarget.IsDead && !IsFriendlyEntity(potentialTarget))
            {
                // Start attacking
                currentEnemyTarget = potentialTarget;
                isAttackingEnemy = true;

                // Disable wandering while attacking
                if (wanderingBehavior != null)
                    wanderingBehavior.SetWanderingEnabled(false);

                // Attack logic
                ChaseAndAttackEnemy();
                return; // Skip the player following logic
            }
        }
            
        // If no enemies to attack, follow player
        isAttackingEnemy = false;
        currentEnemyTarget = null;

        // Check if player is in range
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // If player is within max follow range
        if (distanceToPlayer <= maxFollowRange)
        {
            // Disable wandering while following
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(false);

            // Follow player
            FollowPlayer();
        }
        // If player is too far away
        else
        {
            // Start/resume wandering
            if (wanderingBehavior != null)
                wanderingBehavior.SetWanderingEnabled(true);
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

    private void StartFollowingPlayer()
    {
        isFollowingPlayer = true;
        if (wanderingBehavior != null)
            wanderingBehavior.SetWanderingEnabled(false);
    }

    private void FollowPlayer()
    {
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // If we're too far from player, move closer
        if (distanceToPlayer > followDistance)
        {
            // Update animation
            if (animController != null)
                animController.SetWalking(true);

            // Face player
            FaceTarget(playerTransform.position);

            // Move towards player, but with boundary check
            float currentSpeed = livingEntity.moveSpeed * followSpeed;
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            MoveWithBoundaryCheck(direction, currentSpeed);

            // Save last known player position
            lastPlayerPosition = playerTransform.position;
        }
        // If we're close enough to player, stop
        else
        {
            // Update animation to idle
            if (animController != null)
                animController.SetWalking(false);
        }
    }

    private void ChaseAndAttackEnemy()
    {
        // If target is lost or dead, stop attacking
        if (currentEnemyTarget == null || currentEnemyTarget.IsDead)
        {
            isAttackingEnemy = false;
            return;
        }

        // Get distance to enemy
        float distanceToEnemy = Vector3.Distance(transform.position, currentEnemyTarget.transform.position);

        // If we're close enough to attack
        if (distanceToEnemy <= attackDistance)
        {
            // Stop moving
            if (animController != null)
                animController.SetWalking(false);

            // Face the enemy
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
        // Otherwise, move towards the enemy
        else
        {
            // Update animation
            if (animController != null)
                animController.SetWalking(true);

            // Face and move towards enemy
            FaceTarget(currentEnemyTarget.transform.position);

            // Move with boundary check
            MoveWithBoundaryCheck(transform.forward, livingEntity.moveSpeed);
        }
    }

    private void MoveWithBoundaryCheck(Vector3 direction, float speed)
    {
        // Calculate the next position
        Vector3 nextPosition = transform.position + direction * speed * Time.deltaTime;

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
            transform.position += redirectedDirection * speed * Time.deltaTime;
        }
        else
        {
            // Move normally if within bounds
            transform.position += direction * speed * Time.deltaTime;
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

        // Draw max follow range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxFollowRange);
    }
}
