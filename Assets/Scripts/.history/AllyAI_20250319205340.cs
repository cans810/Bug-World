using UnityEngine;
using System.Collections.Generic;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 3f;     // Distance to maintain from player
    [SerializeField] private float formationSpread = 1.5f;   // How far apart allies should be from each other
    [SerializeField] private float minDistanceBetweenAllies = 1.0f; // Minimum distance between allies
    [SerializeField] private float playerMovementThreshold = 0.05f; // Threshold to detect player movement
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 3f;      // Rotation speed
    [SerializeField] private bool matchPlayerDirection = true; // Whether to match player's facing direction
    private Quaternion targetRotation;                      // Store the target rotation
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal states
    private Transform playerTransform;
    private bool isMoving = false;
    private int allyIndex = -1;
    
    // Player movement detection
    private Vector3 lastPlayerPosition;
    private bool playerIsMoving = false;
    private float playerStationaryTimer = 0f;
    [SerializeField] private float stationaryDelay = 0.5f; // Time before considering player has stopped
    
    // For compatibility with AIWandering.cs
    private bool hasAppliedMovementThisFrame = false;
    public bool HasAppliedMovementThisFrame => hasAppliedMovementThisFrame;
    
    private void Start()
    {
        // Find player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("AllyAI could not find player!");
        }
        else
        {
            lastPlayerPosition = playerTransform.position;
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
        
        // Initialize rotation
        targetRotation = transform.rotation;
        
        // Assign ally index for formation position
        AssignAllyIndex();
    }
    
    private void Update()
    {
        // Reset movement flag
        hasAppliedMovementThisFrame = false;
        
        // Skip if no player or ally is dead
        if (playerTransform == null || livingEntity == null || livingEntity.IsDead)
            return;

        // Check if player is moving
        CheckPlayerMovement();
        
        // Calculate formation position
        Vector3 formationPosition = CalculateFormationPosition();
        
        // Calculate distance to target position
        float distanceToTarget = Vector3.Distance(transform.position, formationPosition);
        
        // Determine if we need to move
        if (playerIsMoving && distanceToTarget > 0.5f)
        {
            // Player is moving and we're not at position - move
            MoveToPosition(formationPosition);
        }
        else if (!playerIsMoving && distanceToTarget > 2.0f)
        {
            // Player has stopped but we're far away - move to position
            MoveToPosition(formationPosition);
        }
        else
        {
            // Either player stopped and we're close, or we're at position
            StopMoving();
        }
        
        // Update rotation to match player's facing direction
        UpdateRotation();
    }

    private void CheckPlayerMovement()
    {
        if (playerTransform == null)
            return;
            
        // Calculate how much player has moved since last frame
        float playerMovement = Vector3.Distance(lastPlayerPosition, playerTransform.position);
        
        if (playerMovement > playerMovementThreshold)
        {
            // Player is moving
            playerIsMoving = true;
            playerStationaryTimer = 0f;
            lastPlayerPosition = playerTransform.position;
        }
        else
        {
            // Player hasn't moved much
            playerStationaryTimer += Time.deltaTime;
            
            // After delay, consider player stopped
            if (playerStationaryTimer >= stationaryDelay)
            {
                playerIsMoving = false;
            }
        }
    }
    
    private void MoveToPosition(Vector3 targetPosition)
    {
        // Calculate movement direction
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        
        // Apply avoidance from other allies
        Vector3 avoidanceDirection = CalculateAvoidanceDirection();
        if (avoidanceDirection.magnitude > 0.01f)
        {
            // Blend movement direction with avoidance (prioritize avoidance)
            moveDirection = Vector3.Lerp(moveDirection, avoidanceDirection, 0.7f);
            moveDirection.Normalize();
        }
        
        // Apply movement using LivingEntity
        if (livingEntity != null)
        {
            livingEntity.MoveInDirection(moveDirection, 1.0f);
            hasAppliedMovementThisFrame = true;
            
            // Update animation
            if (!isMoving)
            {
                isMoving = true;
                if (animController != null)
                    animController.SetWalking(true);
            }
        }
    }
    
    private void StopMoving()
    {
        // Update state and animation
        if (isMoving)
        {
            isMoving = false;
            if (animController != null)
                animController.SetWalking(false);
        }
    }
    
    private void UpdateRotation()
    {
        if (playerTransform == null)
            return;
            
        // Determine target rotation based on setting
        if (matchPlayerDirection)
        {
            // Match player's forward direction
            targetRotation = playerTransform.rotation;
        }
        else
        {
            // Look at player (original behavior)
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            if (directionToPlayer.magnitude > 0.1f)
            {
                directionToPlayer.y = 0;
                targetRotation = Quaternion.LookRotation(directionToPlayer.normalized);
            }
        }
        
        // Apply smooth rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            rotationSpeed * Time.deltaTime
        );
    }
    
    private Vector3 CalculateFormationPosition()
    {
        if (playerTransform == null)
            return transform.position;
            
        // Get all allies to determine formation
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        int allyCount = 0;
        foreach (var ally in allAllies)
        {
            if (ally != null && ally.livingEntity != null && !ally.livingEntity.IsDead)
                allyCount++;
        }
        
        // Base position is behind the player
        Vector3 basePosition = playerTransform.position - (playerTransform.forward * followDistance);
        
        // Formation positioning based on ally count
        if (allyCount <= 1)
        {
            // Only one ally - directly behind player
            return basePosition;
        }
        else if (allyCount <= 3)
        {
            // 2-3 allies - line formation behind player
            float offset = (allyIndex - (allyCount-1)/2.0f) * formationSpread;
            return basePosition + (playerTransform.right * offset);
        }
        else
        {
            // 4+ allies - two rows
            int row = allyIndex / 3; // 0 = first row, 1+ = back rows
            int column = allyIndex % 3; // Position in row
            
            float rowOffset = row * (formationSpread * 0.8f);
            float columnOffset = (column - 1) * formationSpread;
            
            return basePosition 
                - (playerTransform.forward * rowOffset) 
                + (playerTransform.right * columnOffset);
        }
    }
    
    private Vector3 CalculateAvoidanceDirection()
    {
        // Find all nearby allies
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        Vector3 avoidanceDir = Vector3.zero;
        int avoidanceCount = 0;
        
        foreach (var ally in allAllies)
        {
            // Skip self or dead allies
            if (ally == this || ally == null || ally.livingEntity == null || ally.livingEntity.IsDead)
                continue;
                
            // Calculate distance
            float distance = Vector3.Distance(transform.position, ally.transform.position);
            
            // If too close, add avoidance
            if (distance < minDistanceBetweenAllies)
            {
                // Get direction away from ally
                Vector3 awayDir = transform.position - ally.transform.position;
                
                // Avoid zero direction
                if (awayDir.magnitude < 0.01f)
                {
                    awayDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                }
                
                awayDir.y = 0; // Keep on ground plane
                awayDir.Normalize();
                
                // Stronger avoidance for closer allies
                float avoidanceStrength = 1.0f - (distance / minDistanceBetweenAllies);
                avoidanceDir += awayDir * avoidanceStrength;
                avoidanceCount++;
            }
        }
        
        // Average and normalize if needed
        if (avoidanceCount > 0)
        {
            avoidanceDir /= avoidanceCount;
            avoidanceDir.Normalize();
        }
        
        return avoidanceDir;
    }
    
    private void AssignAllyIndex()
    {
        // Find all allies
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        
        // Find our index (position in the list of allies)
        for (int i = 0; i < allAllies.Length; i++)
        {
            if (allAllies[i] == this)
            {
                allyIndex = i;
                break;
            }
        }
        
        // Default to 0 if not found
        if (allyIndex < 0)
            allyIndex = 0;
    }
    
    private void OnDeath()
    {
        // Stop all behavior
        isMoving = false;
        if (animController != null)
            animController.SetWalking(false);
        
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
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Show follow distance
        Gizmos.color = Color.green;
        if (playerTransform != null)
            Gizmos.DrawWireSphere(playerTransform.position, followDistance);
        else if (Application.isEditor && !Application.isPlaying)
        {
            // Try to find player in editor
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                Gizmos.DrawWireSphere(player.transform.position, followDistance);
        }
        
        // Show formation position in play mode
        if (Application.isPlaying && playerTransform != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(CalculateFormationPosition(), 0.3f);
            
            // Show minimum ally distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, minDistanceBetweenAllies);
        }
    }
}
