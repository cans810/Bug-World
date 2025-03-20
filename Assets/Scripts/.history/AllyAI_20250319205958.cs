using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 2.5f;    // Distance to maintain from player
    [SerializeField] private float positionSmoothTime = 0.2f; // Smoothing time for movement
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal variables
    private Transform playerTransform;
    private Vector3 velocity = Vector3.zero;
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
        
        // Calculate target position - behind the player at specified distance
        Vector3 directionFromPlayer = (transform.position - playerTransform.position).normalized;
        if (directionFromPlayer.magnitude < 0.01f)
        {
            // If we're at the same position as the player, pick a position behind them
            directionFromPlayer = -playerTransform.forward;
        }
        
        Vector3 targetPosition = playerTransform.position + directionFromPlayer * followDistance;
        
        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // Determine if we need to move
        if (distanceToTarget > 0.2f)
        {
            // Use SmoothDamp for smooth movement
            Vector3 newPosition = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref velocity, 
                positionSmoothTime, 
                livingEntity.moveSpeed
            );
            
            // Apply movement
            transform.position = newPosition;
            hasAppliedMovementThisFrame = true;
            
            // Look at the direction we're moving
            Vector3 lookDirection = targetPosition - transform.position;
            if (lookDirection.magnitude > 0.01f)
            {
                lookDirection.y = 0;  // Keep on horizontal plane
                transform.LookAt(transform.position + lookDirection);
            }
            
            // Set animation to walking
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
            transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
        }
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
    
    // Optional: visualize the follow distance in the editor
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, followDistance);
        }
    }
}
