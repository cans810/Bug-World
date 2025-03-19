using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    
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
        animator.SetBool(idleParam, isIdle);
        animator.SetBool(walkingParam, isWalking);
        
        // Set the float parameter for the blend tree
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
        if (isDead) return; // Don't interrupt death
        
        ResetStates();
        isWalking = walking;
        isIdle = !walking; // Idle when not walking
        UpdateAnimatorParameters();
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
        if (isDead) return; // Don't attack if dead
        
        // Only update the attack parameter without resetting other states
        isAttacking = attacking;
        UpdateAnimatorParameters();
    }
    
    public void SetDead()
    {
        ResetStates();
        isDead = true;
        UpdateAnimatorParameters();
        
        // Optional: Disable player controls when dead
        // GetComponent<PlayerController>().enabled = false;
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
    }
}