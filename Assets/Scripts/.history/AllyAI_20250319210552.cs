using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 2.5f;    // Distance to maintain from player
    [SerializeField] private float moveSpeed = 3.0f;         // Speed multiplier
    [SerializeField] private float rotationSpeed = 10.0f;    // How fast to rotate
    
    [Header("Formation Settings")]
    [SerializeField] private float formationWidth = 2.0f;    // How wide the allies spread out
    [SerializeField] private float moveThreshold = 0.3f;     // How close before stopping movement
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal variables
    private Transform playerTransform;
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
        
        // Determine ally index (for formation position)
        AssignAllyIndex();
    }
    
    private void Update()
    {
        // Reset movement flag each frame
        hasAppliedMovementThisFrame = false;
        
        // Don't proceed if dead or player not found
        if (livingEntity == null || livingEntity.IsDead || playerTransform == null)
            return;
        
        // Calculate target position in formation behind player
        Vector3 targetPosition = CalculateFormationPosition();
        
        // Calculate distance and direction to target
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0; // Keep on horizontal plane
        float distanceToTarget = directionToTarget.magnitude;
        
        // Determine if we need to move
        if (distanceToTarget > moveThreshold)
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
    
    // Calculate a stable formation position behind the player
    private Vector3 CalculateFormationPosition()
    {
        // Get player's backward direction (for following behind)
        Vector3 playerBackward = -playerTransform.forward;
        playerBackward.y = 0;
        playerBackward.Normalize();
        
        // Calculate horizontal offset based on ally index
        float horizontalOffset = 0;
        
        // Calculate formation position with horizontal offset
        if (allyIndex > 0)
        {
            // Determine if this ally should be on the left or right side
            bool isOnLeftSide = (allyIndex % 2 == 1);
            
            // Calculate the horizontal offset magnitude
            int positionInRow = (allyIndex + 1) / 2;
            horizontalOffset = positionInRow * formationWidth * (isOnLeftSide ? -1 : 1);
            
            // For allies further back, reduce the offset
            if (allyIndex > 2)
            {
                horizontalOffset *= 0.8f;
            }
        }
        
        // Calculate right vector for horizontal offset
        Vector3 playerRight = Vector3.Cross(Vector3.up, playerBackward).normalized;
        
        // Calculate target position: behind player + horizontal offset
        Vector3 targetPos = playerTransform.position 
                            + (playerBackward * followDistance)
                            + (playerRight * horizontalOffset);
        
        // Keep y position at the same height as current position
        targetPos.y = transform.position.y;
        
        return targetPos;
    }
    
    // Assign this ally an index for formation positioning
    private void AssignAllyIndex()
    {
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        int livingCount = 0;
        
        for (int i = 0; i < allAllies.Length; i++)
        {
            if (allAllies[i] == this)
            {
                allyIndex = livingCount;
                break;
            }
            
            // Only count living allies
            if (allAllies[i].livingEntity == null || !allAllies[i].livingEntity.IsDead)
            {
                livingCount++;
            }
        }
        
        // Fallback if we couldn't determine index
        if (allyIndex < 0)
            allyIndex = 0;
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
            
            // Draw formation lines
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, CalculateFormationPosition());
            }
        }
    }
}
