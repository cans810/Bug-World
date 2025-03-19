using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEntityAI : EnemyAI
{
    [Header("Flying Settings")]
    [SerializeField] private float flyingHeight = 5f; // Height above ground to maintain
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
        // Store the original Y position (ground level)
        originalYPosition = transform.position.y;
        
        // Move up to flying height
        Vector3 newPos = transform.position;
        newPos.y = originalYPosition + flyingHeight;
        transform.position = newPos;
        
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
        }
    }

    private void HandleFlyingHeight()
    {
        if (isDescending)
        {
            // Move down toward hover height above player
            if (currentTarget != null)
            {
                float targetHeight = currentTarget.transform.position.y + hoverHeight;
                if (transform.position.y > targetHeight)
                {
                    Vector3 newPos = transform.position;
                    newPos.y = Mathf.MoveTowards(transform.position.y, targetHeight, descentSpeed * Time.deltaTime);
                    transform.position = newPos;
                }
                else
                {
                    isDescending = false;
                    AttackIfInRange();
                }
            }
        }
        else if (isAscending)
        {
            // Move back up to flying height
            float targetHeight = originalYPosition + flyingHeight;
            if (transform.position.y < targetHeight)
            {
                Vector3 newPos = transform.position;
                newPos.y = Mathf.MoveTowards(transform.position.y, targetHeight, ascentSpeed * Time.deltaTime);
                transform.position = newPos;
            }
            else
            {
                isAscending = false;
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
        base.AttackIfInRange();
        
        // After attacking, start ascending
        if (!isAscending)
        {
            StartAscending();
        }
        
        // Ensure walking animation is playing when ascending
        if (isAscending && animController != null)
        {
            animController.SetWalking(true);
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
}
