using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 2.5f;    // Distance to maintain from player
    [SerializeField] private float moveSpeed = 3.0f;         // Speed multiplier (will be overridden by livingEntity if available)
    [SerializeField] private float rotationSpeed = 10.0f;    // How fast to rotate
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal variables
    private Transform playerTransform;
    private bool isMoving = false;
    
    // For compatibility with other systems
    private bool hasAppliedMovementThisFrame = false;
    public bool HasAppliedMovementThisFrame => hasAppliedMovementThisFrame;
    
    private void Start()
    {
        // Find the player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogWarning("AllyAI: Could not find player!");
        }
        
        // Get required components
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
            
        if (animController == null)
            animController = GetComponent<AnimationController>();
        
        // Set the movement speed based on the LivingEntity
        if (livingEntity != null)
        {
            moveSpeed = livingEntity.moveSpeed;
        }
        
        // Subscribe to death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(OnDeath);
        }
    }
    
    private void Update()
    {
        // Reset movement flag each frame
        hasAppliedMovementThisFrame = false;
        
        // Don't proceed if dead or player not found
        if (livingEntity == null || livingEntity.IsDead || playerTransform == null)
            return;
        
        // Calculate ideal target position behind the player
        Vector3 targetPosition = GetTargetPositionBehindPlayer();
        
        // Calculate distance and direction to target
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0; // Keep on horizontal plane
        float distanceToTarget = directionToTarget.magnitude;
        
        // Determine if we need to move
        if (distanceToTarget > 0.3f) // Only move if more than 0.3 units away
        {
            // Normalize the direction
            Vector3 normalizedDirection = directionToTarget.normalized;
            
            // Move using the LivingEntity component
            if (livingEntity != null)
            {
                // Use LivingEntity's movement system
                livingEntity.MoveInDirection(normalizedDirection, 1.0f);
                hasAppliedMovementThisFrame = true;
            }
            else
            {
                // Fallback direct movement with Time.deltaTime
                transform.position += normalizedDirection * moveSpeed * Time.deltaTime;
                hasAppliedMovementThisFrame = true;
            }
            
            // Rotation - smoothly look in the direction of movement
            if (directionToTarget.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // Update animation state
            if (!isMoving)
            {
                isMoving = true;
                UpdateAnimation(true);
            }
        }
        else
        {
            // We're close enough, stop moving
            if (isMoving)
            {
                isMoving = false;
                UpdateAnimation(false);
            }
            
            // Look at the player when idle
            Vector3 lookDir = playerTransform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    // Calculate a position behind the player based on current positions
    private Vector3 GetTargetPositionBehindPlayer()
    {
        // First get direction from player to ally
        Vector3 directionFromPlayer = transform.position - playerTransform.position;
        
        // If we're at or very near the player position, use player's backward direction
        if (directionFromPlayer.magnitude < 0.1f)
        {
            directionFromPlayer = -playerTransform.forward;
        }
        
        // Keep on horizontal plane and normalize
        directionFromPlayer.y = 0;
        directionFromPlayer.Normalize();
        
        // Return position at follow distance away from player in that direction
        return playerTransform.position + directionFromPlayer * followDistance;
    }
    
    private void UpdateAnimation(bool walking)
    {
        if (animController != null)
        {
            animController.SetWalking(walking);
        }
    }
    
    private void OnDeath()
    {
        // Disable AI when dead
        enabled = false;
    }
    
    private void OnDestroy()
    {
        // Clean up event listeners
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(OnDeath);
        }
    }
    
    // Visualize the follow distance in the editor
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, followDistance);
        }
    }
}
