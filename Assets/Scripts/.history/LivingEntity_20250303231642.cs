using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class LivingEntity : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool invulnerable = false;
    [SerializeField] private float healthRegenRate = 0f;
    [SerializeField] private float healthRegenDelay = 3f;
    
    [Header("Damage Settings")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float invulnerabilityTime = 0.5f;
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 0.7f);
    
    [Header("Death Settings")]
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private AudioClip deathSound;
    
    [Header("Status Effects")]
    [SerializeField] private bool canBePoisoned = true;
    [SerializeField] private bool canBeStunned = true;
    [SerializeField] private bool canBeBurned = true;
    
    [Header("Events")]
    public UnityEvent OnDamaged;
    public UnityEvent OnHealed;
    public UnityEvent OnDeath;
    
    // Internal variables
    private bool isDead = false;
    private bool isInvulnerable = false;
    private float lastDamageTime = -999f;
    private Renderer[] renderers;
    private AudioSource audioSource;
    private Material[] originalMaterials;
    private Material[] damageMaterials;
    
    // Status effect tracking
    private bool isPoisoned = false;
    private bool isStunned = false;
    private bool isBurned = false;
    private Coroutine poisonCoroutine;
    private Coroutine burnCoroutine;
    private Coroutine stunCoroutine;
    
    // Properties
    public float MaxHealth { get { return maxHealth; } }
    public float CurrentHealth { get { return currentHealth; } }
    public float HealthPercentage { get { return currentHealth / maxHealth; } }
    public bool IsDead { get { return isDead; } }
    public bool IsInvulnerable { get { return invulnerable || isInvulnerable; } }
    public bool IsPoisoned { get { return isPoisoned; } }
    public bool IsStunned { get { return isStunned; } }
    public bool IsBurned { get { return isBurned; } }
    
    private void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Get renderers for damage flash effect
        renderers = GetComponentsInChildren<Renderer>();
        
        // Create damage flash materials
        if (flashOnDamage && renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            damageMaterials = new Material[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
                damageMaterials[i] = new Material(originalMaterials[i]);
                damageMaterials[i].color = damageFlashColor;
            }
        }
        
        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (deathSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void Update()
    {
        // Health regeneration
        if (healthRegenRate > 0 && !isDead && currentHealth < maxHealth && Time.time > lastDamageTime + healthRegenDelay)
        {
            Heal(healthRegenRate * Time.deltaTime);
        }
    }
    
    public void TakeDamage(float amount, GameObject damager = null)
    {
        // Check if we can take damage
        if (isDead || IsInvulnerable)
            return;
            
        // Apply damage multiplier
        float actualDamage = amount * damageMultiplier;
        
        // Apply damage
        currentHealth -= actualDamage;
        lastDamageTime = Time.time;
        
        // Invoke damage event
        OnDamaged?.Invoke();
        
        // Flash effect
        if (flashOnDamage)
            StartCoroutine(DamageFlash());
            
        // Temporary invulnerability
        if (invulnerabilityTime > 0)
            StartCoroutine(TemporaryInvulnerability());
            
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
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
        
        // Play death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.clip = deathSound;
            audioSource.Play();
        }
        
        // Destroy if needed
        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
        
        // Clear any status effects
        ClearAllStatusEffects();
    }
    
    public void Revive(float healthPercentage = 1f)
    {
        if (!isDead)
            return;
            
        isDead = false;
        currentHealth = maxHealth * Mathf.Clamp01(healthPercentage);
        
        // Clear any status effects
        ClearAllStatusEffects();
    }
    
    public void SetInvulnerable(bool invulnerable)
    {
        this.invulnerable = invulnerable;
    }
    
    #region Status Effects
    
    public void ApplyPoison(float damage, float duration, float tickInterval = 1f)
    {
        if (!canBePoisoned || isDead)
            return;
            
        // Clear any existing poison
        if (poisonCoroutine != null)
            StopCoroutine(poisonCoroutine);
            
        // Start new poison effect
        poisonCoroutine = StartCoroutine(PoisonEffect(damage, duration, tickInterval));
    }
    
    public void ApplyStun(float duration)
    {
        if (!canBeStunned || isDead)
            return;
            
        // Clear any existing stun
        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);
            
        // Start new stun effect
        stunCoroutine = StartCoroutine(StunEffect(duration));
    }
    
    public void ApplyBurn(float damage, float duration, float tickInterval = 0.5f)
    {
        if (!canBeBurned || isDead)
            return;
            
        // Clear any existing burn
        if (burnCoroutine != null)
            StopCoroutine(burnCoroutine);
            
        // Start new burn effect
        burnCoroutine = StartCoroutine(BurnEffect(damage, duration, tickInterval));
    }
    
    public void ClearAllStatusEffects()
    {
        if (poisonCoroutine != null)
        {
            StopCoroutine(poisonCoroutine);
            isPoisoned = false;
        }
        
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
            isStunned = false;
        }
        
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
            isBurned = false;
        }
    }
    
    private IEnumerator PoisonEffect(float damage, float duration, float tickInterval)
    {
        isPoisoned = true;
        float elapsed = 0;
        
        while (elapsed < duration && !isDead)
        {
            TakeDamage(damage);
            
            elapsed += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }
        
        isPoisoned = false;
    }
    
    private IEnumerator StunEffect(float duration)
    {
        isStunned = true;
        
        // Notify other components about stun
        BroadcastMessage("OnStunned", duration, SendMessageOptions.DontRequireReceiver);
        
        yield return new WaitForSeconds(duration);
        
        isStunned = false;
        
        // Notify other components about stun end
        BroadcastMessage("OnStunEnded", SendMessageOptions.DontRequireReceiver);
    }
    
    private IEnumerator BurnEffect(float damage, float duration, float tickInterval)
    {
        isBurned = true;
        float elapsed = 0;
        
        while (elapsed < duration && !isDead)
        {
            TakeDamage(damage);
            
            elapsed += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }
        
        isBurned = false;
    }
    
    #endregion
    
    #region Visual Effects
    
    private IEnumerator DamageFlash()
    {
        if (renderers.Length == 0)
            yield break;
            
        // Apply damage material
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = damageMaterials[i];
        }
        
        // Wait a short time
        yield return new WaitForSeconds(0.1f);
        
        // Restore original material
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }
    }
    
    private IEnumerator TemporaryInvulnerability()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        isInvulnerable = false;
    }
    
    #endregion
}