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
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.15f;
    
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
        StartAttackAnimation();
        return true;
    }
    
    // Start the attack animation
    private void StartAttackAnimation()
    {
        lastAttackTime = Time.time;
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
    
    // Called by animation event when the attack should deal damage
    public void OnDamageFrame()
    {
        // Find the best target if we don't have one
        if (currentTarget == null || currentTarget.IsDead)
        {
            currentTarget = GetBestTargetInRange();
        }
        
        // Deal damage to the target
        if (currentTarget != null && !currentTarget.IsDead)
        {
            currentTarget.TakeDamage(attackDamage, gameObject);
            OnAttack?.Invoke(currentTarget.gameObject);
        }
    }
    
    // Called by animation event with a damage multiplier
    public void OnDamageFrame(float damageMultiplier)
    {
        // Find the best target if we don't have one
        if (currentTarget == null || currentTarget.IsDead)
        {
            currentTarget = GetBestTargetInRange();
        }
        
        // Deal damage to the target
        if (currentTarget != null && !currentTarget.IsDead)
        {
            currentTarget.TakeDamage(attackDamage * damageMultiplier, gameObject);
            OnAttack?.Invoke(currentTarget.gameObject);
        }
    }
    
    // Called by animation event when the attack animation ends
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
        
        // Reset animation bool if using bool mode
        if (animator != null && !useAttackTrigger)
        {
            animator.SetBool(attackBoolName, false);
        }
    }
    
    // Find the best target in range (closest by default)
    private LivingEntity GetBestTargetInRange()
    {
        // If we have targets in our tracked list, find the closest one
        if (targetsInRange.Count > 0)
        {
            LivingEntity bestTarget = null;
            float closestDistance = float.MaxValue;
            
            foreach (LivingEntity target in targetsInRange)
            {
                if (target == null || target.IsDead)
                    continue;
                    
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance <= attackRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = target;
                }
            }
            
            return bestTarget;
        }
        
        // Fallback: use a physics check to find targets
        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange, targetLayers);
        
        LivingEntity closestEntity = null;
        float minDistance = float.MaxValue;
        
        foreach (Collider collider in colliders)
        {
            LivingEntity entity = collider.GetComponent<LivingEntity>();
            if (entity == null)
            {
                entity = collider.GetComponentInParent<LivingEntity>();
            }
            
            if (entity != null && entity != this && !entity.IsDead)
            {
                float distance = Vector3.Distance(transform.position, entity.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEntity = entity;
                }
            }
        }
        
        return closestEntity;
    }
    
    // Add a target to the tracked list
    public void AddTargetInRange(LivingEntity target)
    {
        if (target != null && target != this && !targetsInRange.Contains(target))
        {
            targetsInRange.Add(target);
            
            // If we don't have a current target, set this as our target
            if (currentTarget == null || currentTarget.IsDead)
            {
                currentTarget = target;
            }
        }
    }
    
    // Remove a target from the tracked list
    public void RemoveTargetInRange(LivingEntity target)
    {
        if (target != null && targetsInRange.Contains(target))
        {
            targetsInRange.Remove(target);
            
            // If this was our current target, find a new one
            if (currentTarget == target)
            {
                currentTarget = GetBestTargetInRange();
            }
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
        
        // Destroy if needed
        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
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
    
    // Visualize attack range in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}