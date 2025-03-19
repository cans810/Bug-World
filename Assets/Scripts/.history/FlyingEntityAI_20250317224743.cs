using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEntityAI : EnemyAI
{
    [Header("Flying Settings")]
    [SerializeField] private float flyingHeight = 2f; // Changed from 5f to 2f
    [SerializeField] private float descentSpeed = 2f; // Speed to descend when attacking
    [SerializeField] private float ascentSpeed = 1f; // Speed to ascend after attacking
    [SerializeField] private float hoverHeight = 1.5f; // Height above player when attacking

    private bool isDescending = false;
    private bool isAscending = false;
    private float originalYPosition;

    private bool ShouldAttack(LivingEntity target)
    {
        if (target == null || target.IsDead)
            return false;

        switch (behaviorMode)
        {
            case EnemyBehaviorMode.Aggressive:
                return true;
            case EnemyBehaviorMode.Passive:
                return hasBeenAttacked;
            case EnemyBehaviorMode.UltraPassive:
                return false;
            default:
                return false;
        }
    }

    protected new void Start()
    {
        base.Start();
        // Move directly to Y = 2
        Vector3 newPos = transform.position;
        newPos.y = 2f;
        
        // Use Rigidbody for initial positioning
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = newPos;
            rb.velocity = Vector3.zero;
        }
        else
        {
            transform.position = newPos;
        }
        
        // Force walking animation immediately
        if (animController != null)
        {
            animController.SetWalking(true);
        }
    }

    protected new void Update()
    {
        // Skip base Update since we'll handle our own logic
        if (livingEntity == null || livingEntity.IsDead)
            return;

        // Handle height adjustment
        HandleFlyingHeight();

        // If player is in hitbox, do NOTHING in Update
        if (isPlayerInHitbox)
            return;

        // Run normal AI logic but modified for flying
        RunFlyingAI();
        
        // Ensure walking animation is always playing while wandering
        if (isWandering && animController != null)
        {
            animController.SetWalking(true);
        }
    }

    private void RunFlyingAI()
    {
        // If we're hitting a boundary, handle redirection
        if (isHittingBoundary)
        {
            if (Time.time > boundaryRedirectionTime)
            {
                isHittingBoundary = false;
                ReturnToWandering();
            }
            return;
        }

        // Check for targets
        if (livingEntity.HasTargetsInRange())
        {
            LivingEntity potentialTarget = livingEntity.GetClosestValidTarget();
            if (potentialTarget != null && !potentialTarget.IsDead)
            {
                if (ShouldAttack(potentialTarget))
                {
                    currentTarget = potentialTarget;
                    StartDescending();
                    return;
                }
            }
        }

        // Only run wandering logic if we're not attacking
        if (!isWandering && wanderingBehavior != null && !wanderingBehavior.IsCurrentlyMoving())
        {
            wanderingBehavior.enabled = true;
            wanderingBehavior.SetWanderingEnabled(true);
            isWandering = true;
            
            // Ensure we're at Y = 2 when wandering
            Vector3 newPos = transform.position;
            newPos.y = 2f;
            transform.position = newPos;
        }
    }

    private void HandleFlyingHeight()
    {
        if (isDescending)
        {
            // Move down to player's Y position
            if (currentTarget != null)
            {
                float targetHeight = currentTarget.transform.position.y;
                if (transform.position.y > targetHeight)
                {
                    Vector3 newPos = transform.position;
                    newPos.y = Mathf.MoveTowards(transform.position.y, targetHeight, descentSpeed * Time.deltaTime);
                    
                    // Use Rigidbody for smooth movement
                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = new Vector3(rb.velocity.x, (newPos.y - transform.position.y) / Time.deltaTime, rb.velocity.z);
                    }
                    else
                    {
                        transform.position = newPos;
                    }
                    
                    Debug.Log($"{gameObject.name} descending to {targetHeight}. Current Y: {transform.position.y}");
                }
                else
                {
                    isDescending = false;
                    // Start attacking when we reach the player's height
                    AttackIfInRange();
                }
            }
            else
            {
                // If we lost the target while descending, start ascending
                isDescending = false;
                StartAscending();
            }
        }
        else if (isAscending)
        {
            // Move back up to Y = 2
            if (transform.position.y < 2f)
            {
                Vector3 newPos = transform.position;
                newPos.y = Mathf.MoveTowards(transform.position.y, 2f, ascentSpeed * Time.deltaTime);
                
                // Use Rigidbody for smooth movement
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = new Vector3(rb.velocity.x, (newPos.y - transform.position.y) / Time.deltaTime, rb.velocity.z);
                }
                else
                {
                    transform.position = newPos;
                }
            }
            else
            {
                isAscending = false;
                // Stop vertical movement when reaching target height
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                }
            }
        }
    }

    private void StartDescending()
    {
        isDescending = true;
        isAscending = false;
        
        // Stop wandering
        if (wanderingBehavior != null)
        {
            wanderingBehavior.SetWanderingEnabled(false);
            isWandering = false;
        }
        
        // Set animation to idle when descending to attack
        if (animController != null)
        {
            animController.SetWalking(false);
        }
        
        // Log for debugging
        Debug.Log($"{gameObject.name} is descending to attack player at Y: {currentTarget?.transform.position.y}");
    }

    private void StartAscending()
    {
        isAscending = true;
        isDescending = false;
        
        // Return to wandering after ascending
        if (wanderingBehavior != null)
        {
            wanderingBehavior.SetWanderingEnabled(true);
            isWandering = true;
        }
        
        // Ensure walking animation is playing when ascending
        if (animController != null)
        {
            animController.SetWalking(true);
        }
    }

    protected new void AttackIfInRange()
    {
        // Only attack if we're at the player's height
        if (currentTarget != null && Mathf.Approximately(transform.position.y, currentTarget.transform.position.y))
        {
            base.AttackIfInRange();
            
            // Check if target is dead
            if (currentTarget != null && currentTarget.IsDead)
            {
                // Start ascending after killing the target
                if (!isAscending)
                {
                    StartAscending();
                }
            }
        }
    }

    protected new void HandlePlayerEnterHitbox(LivingEntity playerEntity)
    {
        if (livingEntity == null || livingEntity.IsDead || playerEntity == null || playerEntity.IsDead)
            return;

        if (behaviorMode == EnemyBehaviorMode.UltraPassive)
            return;

        isPlayerInHitbox = true;
        
        // Stop ALL movement states
        isMoving = false;
        isWandering = false;
        inCombatStance = false;
        isHittingBoundary = false;
        
        // Completely disable and stop the AIWandering component
        if (wanderingBehavior != null)
        {
            wanderingBehavior.StopAllCoroutines();
            wanderingBehavior.SetWanderingEnabled(false);
            wanderingBehavior.enabled = false;
        }
        
        // Cancel ALL coroutines on this object
        StopAllCoroutines();
        
        // Handle rotation manually
        if (livingEntity != null)
        {
            livingEntity.SetRotationLocked(true);
        }
        
        // Set animation to idle when battling on ground
        if (animController != null)
        {
            animController.SetWalking(false);
        }
        
        // Start attacking in place after a short delay
        StartCoroutine(StartAttackAfterDelay(0.2f));
        
        // Start the position maintenance coroutine
        StartCoroutine(MaintainPositionWhilePlayerInHitbox());
        
        // Start the rotation coroutine to face the player
        StartCoroutine(FacePlayerWhileInHitbox(playerEntity));
    }

    private void ReturnToWandering()
    {
        currentTarget = null;
        isStandingAfterKill = false;
        
        // Stop moving
        isMoving = false;
        
        // Force animation update to idle immediately
        UpdateAnimation(false);
        
        // Cancel any pending coroutines
        StopAllCoroutines();
        
        // Start the pause-then-wander sequence
        StartCoroutine(PauseAfterChase());
    }

    private IEnumerator PauseAfterChase()
    {
        // Ensure wandering behavior is disabled during the pause
        if (wanderingBehavior != null)
        {
            wanderingBehavior.enabled = false;
            isWandering = false;
        }
        
        // Make sure we're in idle animation
        UpdateAnimation(false);
        
        // Pause for 2 seconds
        float pauseDuration = 2.0f;
        float timer = 0f;
        
        // During this pause, we'll keep the enemy in place
        Vector3 pausePosition = transform.position;
        Quaternion pauseRotation = transform.rotation;
        
        while (timer < pauseDuration)
        {
            // Ensure position and rotation stay fixed
            transform.position = pausePosition;
            transform.rotation = pauseRotation;
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Return to wandering
        if (wanderingBehavior != null)
        {
            wanderingBehavior.enabled = true;
            wanderingBehavior.SetWanderingEnabled(true);
            isWandering = true;
        }
    }

    private void UpdateAnimation(bool isMoving)
    {
        if (animController != null)
        {
            animController.SetWalking(isMoving);
        }
    }

    private void HandleBoundaryRedirection()
    {
        // Logic to handle redirection when reaching the boundary
        if (wanderingBehavior != null)
        {
            // Use the correct method or property to get the spawn position
            Vector3 directionToCenter = (transform.position - wanderingBehavior.GetSpawnPosition()).normalized;
            directionToCenter.y = 0; // Keep rotation on Y axis only

            // Rotate to face away from the boundary
            Quaternion targetRotation = Quaternion.LookRotation(-directionToCenter);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * targetRotationSpeed);

            // Log for debugging
            Debug.Log($"{gameObject.name} redirecting at boundary. New direction: {transform.forward}");
        }
    }
}
