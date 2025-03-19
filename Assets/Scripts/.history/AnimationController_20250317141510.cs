using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private LivingEntity livingEntity;
    
    // Animation parameter names
    private readonly string idleParam = "Idle";
    private readonly string walkSpeedParam = "WalkSpeed";
    private readonly string walkingParam = "Walk";
    private readonly string eatingParam = "Eat";
    private readonly string attackingParam = "Attack";
    private readonly string deadParam = "Death";
    
    // Track current state
    private bool isIdle = true;
    private bool isWalking = false;
    private bool isEating = false;
    private bool isAttacking = false;
    private bool isDead = false;
    
    // Accessor for the animator
    public Animator Animator => animator;
    
    private void Start()
    {
        // Get the Animator component if not assigned
        if (animator == null)
            animator = GetComponent<Animator>();
            
        // Initialize animation state
        UpdateAnimatorParameters();
        
        if (livingEntity == null)
            livingEntity = GetComponent<LivingEntity>();
        
        // Optimize animator
        OptimizeAnimator();
    }
    
    // Set all states to false except the active one
    private void ResetStates()
    {
        isIdle = false;
        isWalking = false;
        isEating = false;
        // Don't reset isAttacking here, as it can be combined with other states
        // Don't reset isDead here, as it's permanent until respawn
    }
    
    // Update animator with current states
    private void UpdateAnimatorParameters()
    {
        if (animator == null)
            return;

        // Always set these core parameters that should exist on all animators
        animator.SetBool(idleParam, isIdle);
        animator.SetBool(walkingParam, isWalking);
        animator.SetFloat(walkSpeedParam, isWalking ? 1f : 0f);
        animator.SetBool(eatingParam, isEating);
        animator.SetBool(attackingParam, isAttacking);
        animator.SetBool(deadParam, isDead);
        
    }
    
    // Public methods to control animations
    
    public void SetIdle()
    {
        if (isDead) return; // Don't change state if dead
        
        ResetStates();
        isIdle = true;
        UpdateAnimatorParameters();
    }
    
    public void SetWalking(bool walking)
    {
        if (isDead) return; // Don't change state if dead
        
        // Only update if state is changing
        if (walking != isWalking)
        {
            ResetStates();
            isWalking = walking;
            isIdle = !walking; // Idle when not walking
            
            // Use safer animation transitions
            if (animator != null)
            {
                // Check if the animation states exist before trying to transition
                if (animator.HasState(0, Animator.StringToHash("Walk")) && 
                    animator.HasState(0, Animator.StringToHash("Idle")))
                {
                    float transitionTime = 0.15f; // Quick but not instant transition
                    animator.CrossFade(walking ? "Walk" : "Idle", transitionTime);
                }
                else
                {
                    // Fallback to direct parameter setting if states don't exist
                    animator.SetBool(walkingParam, walking);
                    animator.SetBool(idleParam, !walking);
                    Debug.LogWarning($"Animation state not found on {gameObject.name}, using direct parameter setting");
                }
            }
            
            UpdateAnimatorParameters();
        }
    }
    
    public void SetEating(bool eating)
    {
        if (isDead) return; // Don't interrupt death
        
        ResetStates();
        isEating = eating;
        isIdle = !eating; // Idle when not eating
        UpdateAnimatorParameters();
    }
    
    public void SetAttacking(bool attacking)
    {
        // Check if entity is dead before allowing attack
        if (livingEntity != null && livingEntity.IsDead) return;
        
        if (isDead) return; // Don't attack if dead

        // Only update the attack parameter without resetting other states
        isAttacking = attacking;
        UpdateAnimatorParameters();
    }
    
    public void SetDead()
    {
        // Only allow setting dead state if LivingEntity is actually dead
        if (livingEntity != null && !livingEntity.IsDead) return;
        
        ResetStates();
        isDead = true;
        UpdateAnimatorParameters();
    }
    
    // Add a method to sync with LivingEntity state
    public void SyncWithLivingEntity()
    {
        if (livingEntity == null) return;
        
        if (livingEntity.IsDead && !isDead)
        {
            SetDead();
        }
        else if (!livingEntity.IsDead && isDead)
        {
            // Reset death state if entity is alive again
            isDead = false;
            UpdateAnimatorParameters();
        }
    }
    
    // Helper method to check current state
    public bool IsAnimationPlaying(string stateName)
    {
        switch (stateName.ToLower())
        {
            case "idle": return isIdle;
            case "walk": return isWalking;
            case "eat": return isEating;
            case "attack": return isAttacking;
            case "death": return isDead;
            default: return false;
        }
    }
    
    // For attack animations that should automatically return to idle
    public void OnAttackComplete()
    {
        SetAttacking(false);
        

        
        // Ensure animator parameters are reset
        if (animator != null)
        {
            animator.SetBool(attackingParam, false);
        }
        
        if (UnityEngine.Debug.isDebugBuild)
            UnityEngine.Debug.Log("Attack animation complete callback");
    }

    public void SmoothTransition(string fromState, string toState, float transitionTime = 0.25f)
    {
        if (animator == null) return;
        
        // Set the transition time
        animator.CrossFade(toState, transitionTime);
        
        // Update internal state tracking
        switch (toState.ToLower())
        {
            case "idle":
                ResetStates();
                isIdle = true;
                break;
            case "walk":
                ResetStates();
                isWalking = true;
                break;
            case "eat":
                ResetStates();
                isEating = true;
                break;
            case "attack":
                isAttacking = true; // Don't reset other states for attack
                break;
            case "death":
                ResetStates();
                isDead = true;
                break;
        }
        
        UpdateAnimatorParameters();
    }

    public void OptimizeAnimator()
    {
        if (animator == null) return;
        
        // Reduce animation update rate on mobile
        #if UNITY_ANDROID || UNITY_IOS
        animator.updateMode = AnimatorUpdateMode.Normal; // Use Normal instead of AnimatePhysics
        animator.cullingMode = AnimatorCullingMode.CullCompletely; // Don't update animations when not visible
        #endif
        
        // Reduce animation quality on mobile
        #if UNITY_ANDROID || UNITY_IOS
        QualitySettings.skinWeights = SkinWeights.TwoBones; // Reduce bone weights for skinning
        #endif
    }

    // Add this helper method to check if animation states exist
    private bool HasAnimationState(string stateName)
    {
        if (animator == null) return false;
        
        // Convert string to hash for more efficient lookup
        int stateHash = Animator.StringToHash(stateName);
        return animator.HasState(0, stateHash);
    }

    public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            // Trigger the specific animation by name
            animator.SetTrigger(animationName);
        }
    }

    // Add this helper method to safely check if a parameter exists
    private bool HasAnimatorParameter(string paramName)
    {
        if (animator == null)
            return false;
        
        // Check each parameter to see if it matches the name we're looking for
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        
        return false;
    }
}