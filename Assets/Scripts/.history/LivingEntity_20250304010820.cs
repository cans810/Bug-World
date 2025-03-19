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
    
    [Header("Damage Settings")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 0.5f;
    
    [Header("Events")]
    public UnityEvent OnDamaged;
    public UnityEvent OnHealed;
    public UnityEvent OnDeath;
    
    [Header("Damage Flash Effect")]
    [SerializeField] private bool enableDamageFlash = true;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.15f;
    
    // Internal variables
    private bool isDead = false;
    private float lastAttackTime = -999f;
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
    
    private void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;
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
    
    // New method for dealing damage to another entity
    public bool DealDamage(LivingEntity target, float? customDamage = null)
    {
        // Check if we can attack (cooldown)
        if (Time.time < lastAttackTime + attackCooldown)
            return false;
            
        // Check if target is valid
        if (target == null || target.IsDead)
            return false;
            
        // Update last attack time
        lastAttackTime = Time.time;
        
        // Deal damage to the target
        float damageToApply = customDamage ?? attackDamage;
        target.TakeDamage(damageToApply, this.gameObject);
        
        return true;
    }
    
    // Overload for dealing damage to multiple targets
    public int DealDamageArea(LivingEntity[] targets, float radius, float? customDamage = null)
    {
        // Check if we can attack (cooldown)
        if (Time.time < lastAttackTime + attackCooldown)
            return 0;
            
        // Update last attack time
        lastAttackTime = Time.time;
        
        // Count how many targets were damaged
        int hitCount = 0;
        
        // Deal damage to all valid targets
        float damageToApply = customDamage ?? attackDamage;
        
        foreach (LivingEntity target in targets)
        {
            if (target == null || target.IsDead)
                continue;
                
            // Check distance if radius is specified
            if (radius > 0)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance > radius)
                    continue;
            }
            
            // Apply damage
            target.TakeDamage(damageToApply, this.gameObject);
            hitCount++;
        }
        
        return hitCount;
    }
    
    public virtual void Die(GameObject killer = null)
    {
        // Prevent multiple deaths
        if (isDead)
            return;
            
        // Mark as dead
        isDead = true;
        currentHealth = 0;
        
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
}