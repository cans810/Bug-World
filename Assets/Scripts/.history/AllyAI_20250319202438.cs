using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float minDistanceToPlayer = 2f;     // Minimum distance to maintain from player
    [SerializeField] private float maxDistanceToPlayer = 5f;     // Distance at which to start following
    [SerializeField] private float moveSpeed = 3f;               // Base move speed
    [SerializeField] private float rotationSpeed = 5f;           // How fast ally rotates
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal states
    private Transform playerTransform;
    private bool isMoving = false;
    private Vector3 lastPlayerPosition;
    private bool playerHasMoved = false;
    
    [Header("Movement Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.2f;
    private Vector3 moveVelocity;
    
    [Header("Ally Avoidance")]
    [SerializeField] private bool avoidOtherAllies = true;
    [SerializeField] private float avoidanceRadius = 1.2f;
    [SerializeField] private LayerMask allyLayer;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo = false;
    
    // For compatibility with AIWandering.cs
    private bool hasAppliedMovementThisFrame = false;
    public bool HasAppliedMovementThisFrame => hasAppliedMovementThisFrame;
    
    private void Start()
    {
        // Find player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogWarning("AllyAI could not find player!");
        }
        else
        {
            lastPlayerPosition = playerTransform.position;
        }
        
        // Get components if not set
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
        
        // Subscribe to death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(HandleDeath);
        }

        // Set up move speed based on player if available
        if (playerTransform != null)
        {
            LivingEntity playerEntity = playerTransform.GetComponent<LivingEntity>();
            if (playerEntity != null && playerEntity.moveSpeed > 0.1f)
            {
                moveSpeed = playerEntity.moveSpeed * 1.1f; // Slightly faster to catch up
                livingEntity.moveSpeed = moveSpeed;
            }
        }
        
        // Start idle animation
        UpdateAnimation(false);
    }
    
    private void Update()
    {
        // Reset the movement flag at the beginning of each frame
        hasAppliedMovementThisFrame = false;
        
        // Skip if dead or no player
        if (livingEntity == null || livingEntity.IsDead || playerTransform == null)
            return;
        
        // Check if player has moved
        Vector3 currentPlayerPos = playerTransform.position;
        float playerMovement = Vector3.Distance(lastPlayerPosition, currentPlayerPos);
        
        if (playerMovement > 0.05f)
        {
            playerHasMoved = true;
            lastPlayerPosition = currentPlayerPos;
        }
        
        // Get distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Determine if we need to move
        if (distanceToPlayer > maxDistanceToPlayer)
        {
            // Too far away, move toward player
            FollowPlayer(true);
        }
        else if (distanceToPlayer < minDistanceToPlayer)
        {
            // Too close, back away slightly
            BackAwayFromPlayer();
        }
        else if (playerHasMoved && distanceToPlayer > minDistanceToPlayer * 1.2f)
        {
            // Within follow range and player has moved, follow
            FollowPlayer(false);
        }
        else
        {
            // At a good distance, stop
            StopMoving();
        }
        
        // Always apply ally avoidance
        ApplyAllyAvoidance();
    }
    
    private void FollowPlayer(bool urgentCatchUp)
    {
        // Calculate direction to player
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0; // Keep on horizontal plane
        
        // If we're urgently catching up, go directly to player
        // Otherwise, aim for a position at the minimum distance
        Vector3 targetPosition;
        
        if (urgentCatchUp)
        {
            // When urgently catching up, go closer to the player but still maintain minimum distance
            targetPosition = playerTransform.position - (directionToPlayer.normalized * minDistanceToPlayer * 0.8f);
        }
        else
        {
            // Normal following, aim for minimum distance
            targetPosition = playerTransform.position - (directionToPlayer.normalized * minDistanceToPlayer);
        }
        
        // Move toward target position
        MoveToPosition(targetPosition);
    }
    
    private void BackAwayFromPlayer()
    {
        // Calculate direction away from player
        Vector3 directionAwayFromPlayer = transform.position - playerTransform.position;
        directionAwayFromPlayer.y = 0; // Keep on horizontal plane
        
        if (directionAwayFromPlayer.magnitude < 0.01f)
        {
            // If we're exactly at the player position, pick a random direction
            directionAwayFromPlayer = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        }
        
        directionAwayFromPlayer.Normalize();
        
        // Calculate a position to move to
        Vector3 targetPosition = transform.position + directionAwayFromPlayer * (minDistanceToPlayer * 0.5f);
        
        // Move toward that position
        MoveToPosition(targetPosition);
    }
    
    private void StopMoving()
    {
        isMoving = false;
        UpdateAnimation(false);
    }
    
    private void MoveToPosition(Vector3 position)
    {
        // Calculate direction to target
        Vector3 directionToTarget = position - transform.position;
        directionToTarget.y = 0; // Keep on horizontal plane
        float distance = directionToTarget.magnitude;
        
        // Only move if we're far enough away
        if (distance > 0.1f)
        {
            // Normalize direction
            Vector3 moveDirection = directionToTarget.normalized;
            
            // Rotate towards movement direction
            if (rotationSpeed > 0 && moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // Move using LivingEntity's movement method if available
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(moveDirection, 1.0f);
                hasAppliedMovementThisFrame = true;
            }
            else
            {
                // Fallback direct movement
                transform.position = Vector3.SmoothDamp(transform.position, position, ref moveVelocity, positionSmoothTime);
                hasAppliedMovementThisFrame = true;
            }
            
            // Set animation
            isMoving = true;
            UpdateAnimation(true);
        }
        else
        {
            // We've reached the target, stop moving
            isMoving = false;
            UpdateAnimation(false);
        }
    }
    
    private void ApplyAllyAvoidance()
    {
        if (!avoidOtherAllies)
            return;
            
        Collider[] nearbyAllies = Physics.OverlapSphere(transform.position, avoidanceRadius, allyLayer);
        Vector3 avoidanceDirection = Vector3.zero;
        int avoidanceCount = 0;
        
        foreach (Collider allyCollider in nearbyAllies)
        {
            // Skip self
            if (allyCollider.gameObject == gameObject)
                continue;
            
            // Get direction away from ally
            Vector3 dirAway = transform.position - allyCollider.transform.position;
            if (dirAway.magnitude < 0.01f)
            {
                // Avoid exact same position
                dirAway = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            }
            else
            {
                dirAway.y = 0; // Keep on horizontal plane
                dirAway.Normalize();
                
                // Scale by how close we are
                float distance = Vector3.Distance(transform.position, allyCollider.transform.position);
                dirAway *= (avoidanceRadius - distance) / avoidanceRadius;
            }
            
            avoidanceDirection += dirAway;
            avoidanceCount++;
        }
        
        // Apply avoidance if needed
        if (avoidanceCount > 0 && avoidanceDirection.magnitude > 0.01f)
        {
            avoidanceDirection /= avoidanceCount;
            avoidanceDirection.Normalize();
            
            // Apply subtle avoidance - just enough to prevent overlap
            if (livingEntity != null)
            {
                livingEntity.MoveInDirection(avoidanceDirection, 0.3f);
            }
            else
            {
                // Fallback
                transform.position += avoidanceDirection * 0.3f * moveSpeed * Time.deltaTime;
            }
        }
    }
    
    private void UpdateAnimation(bool isWalking)
    {
        if (animController == null)
            return;
            
        if (isWalking)
        {
            animController.SetWalking(true);
        }
        else
        {
            animController.SetWalking(false);
        }
    }
    
    private void HandleDeath()
    {
        // Stop all behavior when dead
        isMoving = false;
        enabled = false;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandleDeath);
        }
    }
    
    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // Draw minimum distance
        Gizmos.color = Color.green;
        if (playerTransform != null)
            Gizmos.DrawWireSphere(playerTransform.position, minDistanceToPlayer);
            
        // Draw maximum follow distance
        Gizmos.color = Color.red;
        if (playerTransform != null)
            Gizmos.DrawWireSphere(playerTransform.position, maxDistanceToPlayer);
            
        // Draw ally avoidance radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
    }
}
