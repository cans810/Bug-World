using UnityEngine;

public class 
AnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    
    // Animation parameter names
    private readonly string idleParam = "isIdle";
    private readonly string walkingParam = "isWalking";
    private readonly string eatingParam = "isEating";
    private readonly string attackingParam = "isAttacking";
    private readonly string deadParam = "isDead";
    
    // Track current state
    private bool isIdle = true;
    private bool isWalking = false;
    private bool isEating = false;
    private bool isAttacking = false;
    private bool isDead = false;
    
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
        isAttacking = false;
        // Don't reset isDead here, as it's permanent until respawn
    }
    
    // Update animator with current states
    private void UpdateAnimatorParameters()
    {
        animator.SetBool(idleParam, isIdle);
        animator.SetBool(walkingParam, isWalking);
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
        if (isDead || isAttacking) return; // Don't interrupt death or attack
        
        ResetStates();
        isWalking = walking;
        isIdle = !walking; // Idle when not walking
        UpdateAnimatorParameters();
    }
    
    public void SetEating(bool eating)
    {
        if (isDead || isAttacking) return; // Don't interrupt death or attack
        
        ResetStates();
        isEating = eating;
        isIdle = !eating; // Idle when not eating
        UpdateAnimatorParameters();
    }
    
    public void SetAttacking(bool attacking)
    {
        if (isDead) return; // Don't attack if dead
        
        if (attacking)
        {
            ResetStates();
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
            isIdle = true; // Return to idle after attack
        }
        
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
        SetIdle();
    }
}