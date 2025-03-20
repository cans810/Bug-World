using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyAI : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followDistance = 3f;      // Target distance from player when following
    [SerializeField] private float formationSpread = 60f;    // Angle spread for formation
    [SerializeField] private float moveSpeed = 3f;           // Base move speed
    [SerializeField] private float rotationSpeed = 5f;       // How fast ally rotates
    [SerializeField] private float stoppingDistance = 0.3f;  // How close to get before stopping
    
    [Header("Random Movement")]
    [SerializeField] private float randomMovementRadius = 1f;  // Random movement radius when following
    [SerializeField] private float positionUpdateFrequency = 1.5f;  // How often to update random position
    [SerializeField] private float movementThreshold = 0.1f;  // Player speed to trigger following
    
    [Header("References")]
    [SerializeField] private AnimationController animController;
    [SerializeField] private LivingEntity livingEntity;
    
    // Internal states
    private Transform playerTransform;
    private Rigidbody playerRigidbody;
    private int allyIndex = -1;
    private bool isMoving = false;
    private Vector3 targetPosition;
    private float nextPositionUpdateTime;
    
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
        if (playerTransform != null)
        {
            playerRigidbody = playerTransform.GetComponent<Rigidbody>();
        }
        else
        {
            Debug.LogWarning("AllyAI could not find player!");
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

        // Set up move speed to match player
        if (playerTransform != null)
        {
            LivingEntity playerEntity = playerTransform.GetComponent<LivingEntity>();
            if (playerEntity != null && playerEntity.moveSpeed > 0.1f)
            {
                moveSpeed = playerEntity.moveSpeed * 1.1f; // Slightly faster to catch up
                livingEntity.moveSpeed = moveSpeed;
            }
        }
        
        // Find ally index for formation
        AssignAllyIndex();
        
        // Start idle animation
        UpdateAnimation(false);
        
        // Initialize target position and next update time
        if (playerTransform != null)
        {
            targetPosition = CalculateFormationPosition();
            nextPositionUpdateTime = Time.time + Random.Range(0f, positionUpdateFrequency);
        }
    }
    
    private void Update()
    {
        // Reset the movement flag at the beginning of each frame
        hasAppliedMovementThisFrame = false;
        
        // Skip if dead or no player
        if (livingEntity == null || livingEntity.IsDead || playerTransform == null)
            return;
        
        // Get player's current velocity (either from rigidbody or approximating from position)
        Vector3 playerVelocity = GetPlayerVelocity();
        float playerSpeed = playerVelocity.magnitude;
        
        // Determine if player is moving
        bool playerIsMoving = playerSpeed > movementThreshold;
        
        if (playerIsMoving)
        {
            // Player is moving - follow with some randomness
            if (Time.time >= nextPositionUpdateTime)
            {
                // Calculate base position in formation
                Vector3 basePosition = CalculateFormationPosition();
                
                // Add random offset
                Vector3 randomOffset = Random.insideUnitSphere * randomMovementRadius;
                randomOffset.y = 0; // Keep on horizontal plane
                
                // Set new target and next update time
                targetPosition = basePosition + randomOffset;
                nextPositionUpdateTime = Time.time + Random.Range(0.8f, 1.2f) * positionUpdateFrequency;
                
                if (showDebugInfo)
                    Debug.Log($"Ally {gameObject.name}: New random position set: {targetPosition}");
            }
            
            // Move toward the target with random variation
            MoveToPosition(targetPosition);
        }
        else
        {
            // Player is stationary - stop and idle
            isMoving = false;
            UpdateAnimation(false);
        }
        
        // Always apply ally avoidance
        ApplyAllyAvoidance();
    }
    
    private Vector3 GetPlayerVelocity()
    {
        if (playerRigidbody != null)
        {
            return playerRigidbody.velocity;
        }
        else
        {
            // Approximate velocity by checking position change
            PlayerController controller = playerTransform.GetComponent<PlayerController>();
            if (controller != null)
            {
                return controller.GetMovementDirection() * controller.MoveSpeed;
            }
            else
            {
                // Just check if the player position is changing
                return Vector3.zero;
            }
        }
    }
    
    private void MoveToPosition(Vector3 position)
    {
        // Calculate distance to target
        Vector3 directionToTarget = position - transform.position;
        directionToTarget.y = 0; // Keep movement on horizontal plane
        float distance = directionToTarget.magnitude;
        
        // Only move if we're far enough away
        if (distance > stoppingDistance)
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
    
    private Vector3 CalculateFormationPosition()
    {
        if (playerTransform == null)
            return transform.position;
            
        // Get number of allies for better spacing
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        int aliveAlliesCount = 0;
        
        foreach (AllyAI ally in allAllies)
        {
            if (ally != null && ally.livingEntity != null && !ally.livingEntity.IsDead)
            {
                aliveAlliesCount++;
            }
        }
        
        // Default to a single ally if something went wrong
        if (aliveAlliesCount <= 0) aliveAlliesCount = 1;
        
        // Calculate angle based on ally index and total count
        float angleStep = formationSpread / aliveAlliesCount;
        float angle = -formationSpread/2 + (allyIndex * angleStep) + (angleStep/2);
        
        // Add some random variation to the angle
        angle += Random.Range(-5f, 5f);
        
        // Calculate direction based on angle (negative Z is behind player)
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Vector3 direction = rotation * Vector3.back;
        
        // Add some random variation to the distance
        float actualDistance = followDistance + Random.Range(-0.3f, 0.3f);
        
        // Return position at specified distance from player
        return playerTransform.position + (direction * actualDistance);
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
    
    private void AssignAllyIndex()
    {
        // Find all ally AIs and determine this one's index
        AllyAI[] allAllies = FindObjectsOfType<AllyAI>();
        int aliveIndex = 0;
        
        for (int i = 0; i < allAllies.Length; i++)
        {
            if (allAllies[i] == this)
            {
                allyIndex = aliveIndex;
                break;
            }
            
            // Only count living allies for the index
            if (!allAllies[i].livingEntity.IsDead)
            {
                aliveIndex++;
            }
        }
        
        if (allyIndex == -1) allyIndex = 0; // Fallback
        
        if (showDebugInfo)
            Debug.Log($"Ally {gameObject.name} assigned index: {allyIndex}");
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
        
        // Draw follow distance
        Gizmos.color = Color.green;
        if (playerTransform != null)
            Gizmos.DrawWireSphere(playerTransform.position, followDistance);
            
        // Draw ally avoidance radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
        
        // Draw target position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(targetPosition, 0.2f);
        }
    }
}
