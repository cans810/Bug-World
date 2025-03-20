using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 2.5f;    // Distance to maintain from player
    [SerializeField] private float positionSmoothTime = 0.2f; // Smoothing time for movement
    [SerializeField] private float minMoveDistance = 0.1f;   // Minimum distance to trigger movement
    [SerializeField] private float maxFollowDistance = 8f;   // Maximum distance before teleporting
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal variables
    private Transform playerTransform;
    private Vector3 velocity = Vector3.zero;
    private bool isMoving = false;
    private int allyIndex = -1;
    
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
        
        // Determine ally index for positioning
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        for (int i = 0; i < allAllies.Length; i++)
        {
            if (allAllies[i] == this)
            {
                allyIndex = i;
                break;
            }
        }
        
        // If we didn't find an index, use a random one
        if (allyIndex < 0)
            allyIndex = Random.Range(0, 100);
    }
    
    private void Update()
    {
        // Reset movement flag each frame
        hasAppliedMovementThisFrame = false;
        
        // Don't proceed if dead or player not found
        if (livingEntity == null || livingEntity.IsDead || playerTransform == null)
            return;
        
        // Get current distance to player
        float currentDistanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Teleport if extremely far away
        if (currentDistanceToPlayer > maxFollowDistance)
        {
            Vector3 teleportPos = GetBestFollowPosition();
            transform.position = teleportPos;
            return;
        }
        
        // Calculate target position using a better algorithm
        Vector3 targetPosition = GetBestFollowPosition();
        
        // Calculate distance to target
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // Determine if we need to move
        if (distanceToTarget > minMoveDistance)
        {
            // Use SmoothDamp for smooth movement
            Vector3 newPosition = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref velocity, 
                positionSmoothTime, 
                livingEntity != null ? livingEntity.moveSpeed : 3f
            );
            
            // Apply movement
            if (livingEntity != null)
            {
                // Use LivingEntity's movement if available (physics-based)
                Vector3 moveDirection = (targetPosition - transform.position).normalized;
                moveDirection.y = 0; // Keep on horizontal plane
                livingEntity.MoveInDirection(moveDirection, 1.0f);
            }
            else
            {
                // Direct position update as fallback
                transform.position = newPosition;
            }
            
            hasAppliedMovementThisFrame = true;
            
            // Look at the direction we're moving
            Vector3 lookDirection = targetPosition - transform.position;
            if (lookDirection.magnitude > 0.01f)
            {
                lookDirection.y = 0;  // Keep on horizontal plane
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
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
            Vector3 dirToPlayer = playerTransform.position - transform.position;
            dirToPlayer.y = 0;
            if (dirToPlayer.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }
    
    private Vector3 GetBestFollowPosition()
    {
        // Calculate a position behind the player, with an offset based on ally index
        float angleOffset = 30f * (allyIndex % 3) - 30f;  // Spread allies: -30, 0, 30 degrees
        
        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(0, angleOffset, 0);
        Vector3 followDirection = rotation * -playerTransform.forward;
        
        // Calculate final position
        Vector3 targetPosition = playerTransform.position + followDirection * followDistance;
        
        // Ensure we're on the same Y level as the player
        targetPosition.y = playerTransform.position.y;
        
        return targetPosition;
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
        if (!Application.isPlaying || playerTransform == null) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerTransform.position, followDistance);
        
        // Draw the target position
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(GetBestFollowPosition(), 0.2f);
    }
}
