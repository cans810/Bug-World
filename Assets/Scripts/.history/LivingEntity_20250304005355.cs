using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class LivingEntity : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool invulnerable = false;
    
    [Header("Death Settings")]
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 3f;
    
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
    private Renderer[] renderers;
    private Color[] originalColors;
    private Material[] materials;
    private bool isFlashing = false;
    
    // Properties
    public float MaxHealth { get { return maxHealth; } }
    public float CurrentHealth { get { return currentHealth; } }
    public float HealthPercentage { get { return currentHealth / maxHealth; } }
    public bool IsDead { get { return isDead; } }
    public bool IsInvulnerable { get { return invulnerable; } }
    
    private void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;
    }
    
    private void Start()
    {
        // Cache all renderers and their original colors
        if (enableDamageFlash)
        {
            renderers = GetComponentsInChildren<Renderer>();
            materials = new Material[renderers.Length];
            originalColors = new Color[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                materials[i] = renderers[i].material;
                originalColors[i] = materials[i].color;
            }
        }
    }
    
    public void TakeDamage(float amount, GameObject damager = null)
    {
        // Check if we can take damage
        if (isDead || IsInvulnerable)
            return;
            
        // Apply damage
        currentHealth -= amount;
        
        // Invoke damage event
        OnDamaged?.Invoke();
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
        
        // Flash red when taking damage
        if (enableDamageFlash && !isFlashing)
        {
            StartCoroutine(FlashDamage());
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead)
            return;
            
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        if (currentHealth > oldHealth)
            OnHealed?.Invoke();
    }
    
    public void Die()
    {
        if (isDead)
            return;
            
        isDead = true;
        currentHealth = 0;
        
        // Invoke death event
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
        
        // Change to damage color
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].color = damageFlashColor;
        }
        
        // Wait for the flash duration
        yield return new WaitForSeconds(damageFlashDuration);
        
        // Change back to original color
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].color = originalColors[i];
        }
        
        isFlashing = false;
    }
}