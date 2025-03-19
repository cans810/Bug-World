using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class LivingEntity : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool invulnerable = false;
    
    [Header("Death Settings")]
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 3f;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask targetLayers; // Layers that this entity can attack
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string attackBoolName = "Attack";
    [SerializeField] private bool useAttackTrigger = false; // If true, uses trigger; if false, uses bool
    
    [Header("Events")]
    public UnityEvent OnDamaged;
    public UnityEvent OnHealed;
    public UnityEvent OnDeath;
    public UnityEvent<GameObject> OnAttack;
    
    [Header("Damage Flash Effect")]
    [SerializeField] private bool enableDamageFlash = true;
    [SerializeField] private Color damageFlashColor = new Color(0.5f, 0f, 0f);
    [SerializeField] private float damageFlashDuration = 1f;
    [SerializeField] private int flashCount = 1;
    [SerializeField] private float flashInterval = 0.1f;
    
    [Header("Animation Controller")]
    [SerializeField] private AnimationController animationController;
    
    // Internal variables
    private bool isDead = false;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;
    
    // Target tracking
    private List<LivingEntity> targetsInRange = new List<LivingEntity>();
    private LivingEntity currentTarget;
    
    // Damage flash effect
    private Renderer[] renderers;
    private Dictionary<Renderer, Material[]> originalMaterialsMap = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> flashMaterialsMap = new Dictionary<Renderer, Material[]>();
    private bool isFlashing = false;
    
    // Properties
    public float MaxHealth { get { return maxHealth; } }
    public float CurrentHealth { get { return currentHealth; } }
    public float HealthPercentage { get { return currentHealth / maxHealth; } }
    public bool IsDead { get { return isDead; } }
    public bool IsInvulnerable { get { return invulnerable; } }
    public float AttackDamage => attackDamage;
    public float AttackRange => attackRange;
    public bool IsAttacking => isAttacking;
    public LivingEntity CurrentTarget => currentTarget;
    
    private void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Find animator if not set
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        // Find AnimationController if not set
        if (animationController == null)
        {
            animationController = GetComponent<AnimationController>();
            if (animationController == null)
            {
                animationController = GetComponentInChildren<AnimationController>();
            }
        }
    }
    
    private void Start()
    {
        // Cache all renderers and create flash materials
        if (enableDamageFlash)
        {
            renderers = GetComponentsInChildren<Renderer>();
            
            foreach (Renderer renderer in renderers)
            {
                // Get all materials on this renderer
                Material[] originalMaterials = renderer.materials;
                Material[] flashMaterials = new Material[originalMaterials.Length];
                
                // Create flash versions of each material
                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    flashMaterials[i] = new Material(originalMaterials[i]);
                    flashMaterials[i].color = damageFlashColor;
                }
                
                // Store both original and flash materials
                originalMaterialsMap[renderer] = originalMaterials;
                flashMaterialsMap[renderer] = flashMaterials;
            }
            
            Debug.Log($"Found {renderers.Length} renderers with a total of {CountTotalMaterials()} materials");
        }
    }
    
    private int CountTotalMaterials()
    {
        int count = 0;
        foreach (var materials in originalMaterialsMap.Values)
        {
            count += materials.Length;
        }
        return count;
    }
    
    private void OnDestroy()
    {
        // Clean up the flash materials to prevent memory leaks
        if (flashMaterialsMap != null)
        {
            foreach (Material[] materials in flashMaterialsMap.Values)
            {
                foreach (Material mat in materials)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
            }
        }
    }
    
    // Called by input system or AI to initiate an attack
    public bool TryAttack()
    {
        // Don't attack if dead or already attacking
        if (isDead || isAttacking)
            return false;
            
        // Check cooldown
        if (Time.time < lastAttackTime + attackCooldown)
            return false;
            
        // Start attack animation
        StartAttack();
        lastAttackTime = Time.time;
        return true;
    }
    
    private void StartAttack()
    {
        isAttacking = true;
        
        // Update animation controller if available
        if (animationController != null)
        {
            animationController.SetAttacking(true);
        }
        // Or use animator directly
        else if (animator != null)
        {
            if (useAttackTrigger)
            {
                animator.SetTrigger(attackTriggerName);
            }
            else
            {
                animator.SetBool(attackBoolName, true);
            }
        }
    }
    
    // This method must be called by animation event at the end of attack animation
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        
        // Reset animation state
        if (animationController != null)
        {
            animationController.SetAttacking(false);
        }
        else if (animator != null && !useAttackTrigger)
        {
            animator.SetBool(attackBoolName, false);
        }
        
        if (UnityEngine.Debug.isDebugBuild)
            UnityEngine.Debug.Log("Attack animation ended");
    }
    
    // This method can be called by animation event when the attack should apply damage
    public void OnAttackHitFrame()
    {
        // Find a target in range and deal damage
        if (targetsInRange.Count > 0)
        {
            LivingEntity target = GetClosestTarget();
            if (target != null && !target.IsDead)
            {
                target.TakeDamage(attackDamage, gameObject);
                OnAttack?.Invoke(target.gameObject);
            }
        }
    }
    
    private LivingEntity GetClosestTarget()
    {
        if (targetsInRange.Count == 0)
            return null;
            
        float closestDistance = float.MaxValue;
        LivingEntity closestTarget = null;
        
        foreach (LivingEntity target in targetsInRange)
        {
            if (target == null || target.IsDead)
                continue;
                
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }
        
        return closestTarget;
    }
    
    // These methods are used by EntityHitbox to track targets
    public void AddTargetInRange(LivingEntity target)
    {
        if (!targetsInRange.Contains(target))
        {
            targetsInRange.Add(target);
        }
    }
    
    public void RemoveTargetInRange(LivingEntity target)
    {
        targetsInRange.Remove(target);
        
        // If this was our current target, clear it
        if (currentTarget == target)
        {
            currentTarget = null;
        }
    }
    
    // Take damage from a source
    public void TakeDamage(float damageAmount, GameObject damageSource = null)
    {
        // Don't take damage if already dead
        if (isDead)
            return;
            
        // Apply damage
        currentHealth -= damageAmount;
        
        // Flash red when taking damage
        if (enableDamageFlash && !isFlashing)
        {
            StartCoroutine(FlashDamage());
        }
        
        // Trigger damage event
        OnDamaged?.Invoke();
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die(damageSource);
        }
    }
    
    public virtual void Die(GameObject killer = null)
    {
        // Prevent multiple deaths
        if (isDead)
            return;
            
        // Mark as dead
        isDead = true;
        currentHealth = 0;
        
        // Update animation
        if (animationController != null)
        {
            animationController.SetDead();
        }
        else if (animator != null)
        {
            animator.SetBool("Death", true);
        }
        
        // Trigger death event
        OnDeath?.Invoke();
        
        // Note: We're no longer handling destruction here
        // This is now managed by whoever subscribes to the OnDeath event
        // (like the EnemyAI component)
    }
    
    public void Revive(float healthPercentage = 1f)
    {
        if (!isDead)
            return;
            
        isDead = false;
        currentHealth = maxHealth * Mathf.Clamp01(healthPercentage);
    }
    
    public void SetInvulnerable(bool invulnerable)
    {
        this.invulnerable = invulnerable;
    }
    
    public void SetMaxHealth(float newMaxHealth, bool adjustCurrentHealth = true)
    {
        float oldMaxHealth = maxHealth;
        maxHealth = Mathf.Max(1, newMaxHealth);
        
        if (adjustCurrentHealth)
        {
            // Adjust current health proportionally
            float healthPercentage = currentHealth / oldMaxHealth;
            currentHealth = maxHealth * healthPercentage;
        }
        else
        {
            // Cap current health at new max
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
    }
    
    private IEnumerator FlashDamage()
    {
        isFlashing = true;
        
        // For a single flash
        if (flashCount <= 1)
        {
            // Switch to flash materials
            foreach (Renderer renderer in renderers)
            {
                if (flashMaterialsMap.ContainsKey(renderer))
                {
                    renderer.materials = flashMaterialsMap[renderer];
                }
            }
            
            // Wait for the flash duration
            yield return new WaitForSeconds(damageFlashDuration);
            
            // Switch back to original materials
            foreach (Renderer renderer in renderers)
            {
                if (originalMaterialsMap.ContainsKey(renderer))
                {
                    renderer.materials = originalMaterialsMap[renderer];
                }
            }
        }
        // For multiple flashes (creates a pulsing effect)
        else
        {
            for (int i = 0; i < flashCount; i++)
            {
                // Switch to flash materials
                foreach (Renderer renderer in renderers)
                {
                    if (flashMaterialsMap.ContainsKey(renderer))
                    {
                        renderer.materials = flashMaterialsMap[renderer];
                    }
                }
                
                // Wait for half the interval
                yield return new WaitForSeconds(damageFlashDuration / (flashCount * 2));
                
                // Switch back to original materials
                foreach (Renderer renderer in renderers)
                {
                    if (originalMaterialsMap.ContainsKey(renderer))
                    {
                        renderer.materials = originalMaterialsMap[renderer];
                    }
                }
                
                // Wait for half the interval before next flash (if not the last flash)
                if (i < flashCount - 1)
                    yield return new WaitForSeconds(flashInterval);
            }
        }
        
        isFlashing = false;
    }
    
    // Helper method to heal the entity
    public void Heal(float amount)
    {
        if (isDead)
            return;
            
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        if (currentHealth > oldHealth)
            OnHealed?.Invoke();
    }
    
    // Helper method to set health directly (useful for initialization)
    public void SetHealth(float amount)
    {
        currentHealth = Mathf.Clamp(amount, 0, maxHealth);
        
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    
    
    private void Update()
    {
        // Add this check to force reset attack state if animation finished playing
        if (isAttacking && animator != null)
        {
            // Check if we're in the attack state and the normalized time is past the end
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Attack") && stateInfo.normalizedTime >= 0.95f)
            {
                OnAttackAnimationEnd();
            }
        }
    }
    
    public bool HasTargetsInRange()
    {
        return targetsInRange.Count > 0;
    }
    
    public LivingEntity GetClosestValidTarget()
    {
        return GetClosestTarget();
    }
}