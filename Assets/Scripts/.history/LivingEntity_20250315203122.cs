using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using CandyCoded.HapticFeedback;

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
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask targetLayers; // Layers that this entity can attack

    [Header("Movement Settings")]
    [SerializeField] public float moveSpeed = 2.0f;
    [SerializeField] public float rotationSpeed = 4f;
    
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
    
    [Header("Health Bar")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] private Transform healthBarAnchor; // Optional specific point to place the health bar
    
    // Add this section for chitin drops
    [Header("Drops")]
    [SerializeField] public int chitinAmount;
    
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
    
    private GameObject healthBarInstance;
    
    // Add these fields to track damage and healing amounts
    private float lastDamageAmount = 0f;
    private float lastHealAmount = 0f;
    
    // Add properties to access these values
    public float LastDamageAmount { get { return lastDamageAmount; } }
    public float LastHealAmount { get { return lastHealAmount; } }
    
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
    public float AttackCooldown => attackCooldown;
    
    // Add property to calculate remaining cooldown time
    public float RemainingAttackCooldown 
    { 
        get 
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            float remainingTime = attackCooldown - timeSinceLastAttack;
            return Mathf.Max(0f, remainingTime); // Never return negative values
        }
    }
    
    [Header("Sound Effects")]
    [SerializeField] private string hitSoundEffectName = "Attack1";
    [SerializeField] private bool useSoundEffectManager = true;
    [SerializeField] private AudioClip hitSound; // Fallback sound
    
    // Add these variables to the LivingEntity class
    private bool rotationLocked = false;
    
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
        
        // Spawn health bar if needed
        if (showHealthBar && healthBarPrefab != null)
        {
            SpawnHealthBar();
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
        // Check if enough time has passed since last attack
        if (Time.time - lastAttackTime < attackCooldown)
        {
            return false; // Still on cooldown
        }
        
        // Record attack time
        lastAttackTime = Time.time;
        
        // Trigger attack animation
        if (animationController != null)
        {
            animationController.SetAttacking(true);
        }
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
        
        return true;
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
                
                // Check if this is the player landing an attack
                if (gameObject.CompareTag("Player"))
                {
                    // Find the player controller and call the OnAttackLanded method
                    PlayerController playerController = GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        playerController.OnAttackLanded();
                    }
                }
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
    
    // Update the TakeDamage method with more detailed player-specific debugging
    public virtual void TakeDamage(float amount, GameObject damageSource = null)
    {
        // Skip if invulnerable or already dead
        if (invulnerable || isDead)
            return;
            
        bool isPlayer = gameObject.CompareTag("Player");
        
        if (isPlayer)
        {
            Debug.LogWarning($"PLAYER taking {amount} damage from {(damageSource ? damageSource.name : "unknown")}");
        }
        else
        {
            Debug.Log($"{gameObject.name} taking {amount} damage from {(damageSource ? damageSource.name : "unknown")}");
        }
        
        // Store the damage amount for UI/feedback
        lastDamageAmount = amount;
        
        // Apply damage
        currentHealth -= amount;
        
        // Play hit sound when taking damage
        if (isPlayer)
        {
            Debug.LogWarning("About to play hit sound for PLAYER");
        }
        PlayHitSound();
        
        // Invoke the OnDamaged event
        OnDamaged.Invoke();
        
        // Flash effect
        if (enableDamageFlash && !isFlashing)
        {
            StartCoroutine(FlashEffect());
        }
        
        // Check for death
        if (currentHealth < 1 && !isDead)
        {
            Die(damageSource);
        }
        
        // Add haptic feedback when player takes damage
        if (this.gameObject.CompareTag("Player"))
        {
            HapticFeedback.MediumFeedback();
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
        
        // Forcefully fix animation transition to death
        StartCoroutine(ForceDeathAnimationByReplacement());
        
        // Trigger death event for other systems to respond
        OnDeath?.Invoke();
        
        // NOTE: ChitinDropper will handle the chitin spawning through the OnDeath event
        // There's no need for additional chitin handling here
        
        // If we should be destroyed after death
        if (destroyOnDeath)
        {
            // Delay destruction to allow for death animation and effects
            Destroy(gameObject, destroyDelay);
        }
    }
    
    private IEnumerator ForceDeathAnimationByReplacement()
    {
        // Try both animation approaches
        
        // 1. Via the animation controller if available
        if (animationController != null)
        {
            animationController.SetWalking(false);
            animationController.SetAttacking(false);
            animationController.SetEating(false);
            animationController.SetDead();
        }
        
        // 2. Via direct animator control for maximum reliability
        if (animator != null)
        {
            // Disable the animator to stop ALL current animations
            animator.enabled = false;
            
            // Wait a frame to ensure it's fully stopped
            yield return null;
            
            // Re-enable it
            animator.enabled = true;
            
            // Reset all parameters that might be causing issues
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name != "Death") // Don't reset Death parameter
                {
                    if (param.type == AnimatorControllerParameterType.Bool)
                        animator.SetBool(param.name, false);
                    else if (param.type == AnimatorControllerParameterType.Trigger)
                        animator.ResetTrigger(param.name);
                    else if (param.type == AnimatorControllerParameterType.Float)
                        animator.SetFloat(param.name, 0f);
                    else if (param.type == AnimatorControllerParameterType.Int)
                        animator.SetInteger(param.name, 0);
                }
            }
            
            // Now FORCE the Death parameter and state
            animator.SetBool("Death", true);
            
            // Force play the death animation directly
            if (animator.HasState(0, Animator.StringToHash("Death")))
            {
                animator.Play("Death", 0, 0f);
            }
        }
        
        // If destroyOnDeath is set, handle object destruction
        if (destroyOnDeath)
        {
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
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
    
    private IEnumerator FlashEffect()
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
        
        // Store the heal amount for UI/feedback
        lastHealAmount = currentHealth - oldHealth;
        
        // Only invoke the event if actual healing occurred
        if (lastHealAmount > 0)
        {
            // Invoke the OnHealed event
            OnHealed.Invoke();
        
        }
    }
    
    // Helper method to set health directly (useful for initialization)
    public void SetHealth(float amount)
    {
        currentHealth = Mathf.Clamp(amount, 0, maxHealth);
        
        if (currentHealth < 1 && !isDead)
        {
            Die();
        }
    }
    
    // Make sure this new method doesn't conflict with the existing float version
    public void SetHealth(int healthValue)
    {
        // Use the existing float SetHealth method to avoid duplicating logic
        SetHealth((float)healthValue);
    }
    
    public void SetMaxHealth(int maxHealthValue)
    {
        // Use the class field directly
        maxHealth = maxHealthValue;
        
        // If current health is higher than new max, adjust it
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        
        // Update the health bar if it exists
        if (healthBarInstance != null)
        {
            // Instead of directly calling UpdateHealthBar(), use the public interface
            HealthBarController controller = healthBarInstance.GetComponent<HealthBarController>();
            if (controller != null)
            {
                // Most likely there's a public method like SetValues or similar
                // Or we might need to destroy and recreate the health bar
                controller.SetTargetEntity(this); // This will trigger a refresh
            }
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
        
        // Check if entity is dead but has health above 0, then revive it
        if (isDead && currentHealth > 1)
        {
            // Calculate health percentage for the revive method
            float healthPercentage = currentHealth / maxHealth;
            Revive(healthPercentage);
            
            // Update animation if we have an animation controller
            if (animationController != null)
            {
                animationController.SyncWithLivingEntity();
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
    
    // Method to cancel an ongoing attack (useful when entity dies mid-attack)
    public void CancelAttack()
    {
        if (isAttacking)
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
        }
    }
    
    private void SpawnHealthBar()
    {
        // Create the health bar
        healthBarInstance = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
        
        // If we have a specific anchor, parent to that, otherwise parent to this transform
        Transform parent = (healthBarAnchor != null) ? healthBarAnchor : transform;
        
        // Don't parent directly to avoid scaling issues with UI
        healthBarInstance.transform.SetParent(null);
        
        // Set up the controller
        HealthBarController controller = healthBarInstance.GetComponent<HealthBarController>();
        if (controller != null)
        {
            controller.SetTargetEntity(this);
        }
        else
        {
            Debug.LogWarning("Health bar prefab missing HealthBarController component!");
        }
    }
    
    // Method to configure destruction settings
    public void SetDestroyOnDeath(bool shouldDestroy, float delay = 3f)
    {
        destroyOnDeath = shouldDestroy;
        destroyDelay = delay;
    }
    
    // Add this method to set the attack cooldown (called from PlayerAttributes)
    public void SetAttackCooldown(float newCooldown)
    {
        attackCooldown = Mathf.Max(0.1f, newCooldown); // Ensure cooldown doesn't go too low
        Debug.Log($"Attack cooldown set to {attackCooldown:F2} seconds");
    }
    
    // Update the PlayHitSound method with more player-specific debugging
    private void PlayHitSound()
    {
        bool isPlayer = gameObject.CompareTag("Player");
        
        if (isPlayer)
        {
            Debug.LogWarning($"PLAYER is playing hit sound: {hitSoundEffectName}");
        }
        else
        {
            Debug.Log($"{gameObject.name} is playing hit sound: {hitSoundEffectName}");
        }
        
        if (useSoundEffectManager && SoundEffectManager.Instance != null)
        {
            bool soundExists = SoundEffectManager.Instance.HasSoundEffect(hitSoundEffectName);
            
            if (isPlayer)
            {
                Debug.LogWarning($"PLAYER: Sound effect '{hitSoundEffectName}' exists in SoundEffectManager: {soundExists}");
                Debug.LogWarning($"PLAYER: useSoundEffectManager = {useSoundEffectManager}");
            }
            else
            {
                Debug.Log($"Sound effect '{hitSoundEffectName}' exists in SoundEffectManager: {soundExists}");
            }
            
            SoundEffectManager.Instance.PlaySound(hitSoundEffectName, transform.position);
        }
        else if (hitSound != null)
        {
            if (isPlayer)
            {
                Debug.LogWarning($"PLAYER: Playing hit sound using AudioSource.PlayClipAtPoint");
            }
            else
            {
                Debug.Log($"Playing hit sound using AudioSource.PlayClipAtPoint");
            }
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
        else
        {
            if (isPlayer)
            {
                Debug.LogWarning($"PLAYER: No sound effect or audio clip assigned for hit sound");
            }
            else
            {
                Debug.LogWarning($"No sound effect or audio clip assigned for hit sound on {gameObject.name}");
            }
        }
    }
    
    // Update the DealDamage method with debug logging
    private void DealDamage()
    {
        Debug.Log($"{gameObject.name} is attempting to deal damage");
        
        // Find targets in range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, targetLayers);
        Debug.Log($"Found {hitColliders.Length} potential targets in range");
        
        foreach (Collider hitCollider in hitColliders)
        {
            // Skip self
            if (hitCollider.gameObject == gameObject)
                continue;
            
            Debug.Log($"Checking target: {hitCollider.gameObject.name}");
            
            // Try to get a LivingEntity component
            LivingEntity targetEntity = hitCollider.GetComponent<LivingEntity>();
            if (targetEntity != null && !targetEntity.IsDead)
            {
                Debug.Log($"{gameObject.name} is dealing {attackDamage} damage to {targetEntity.gameObject.name}");
                
                // Apply damage to the target, passing this entity as the damage source
                targetEntity.TakeDamage(attackDamage, this.gameObject);
                
                // Invoke the OnAttack event with the target
                OnAttack.Invoke(hitCollider.gameObject);
            }
        }
    }
    
    public void RotateTowards(Vector3 direction, float rotationSpeed = 1.0f)
    {
        if (rotationLocked)
            return;
        
        if (direction.magnitude < 0.1f)
            return;
        
        // Ensure we're only rotating on the Y axis
        direction.y = 0;
        direction.Normalize();
        
        // Determine if this is the player - players should rotate faster
        bool isPlayer = gameObject.CompareTag("Player");
        
        // Apply higher rotation speed for player
        float actualRotationSpeed = isPlayer ? 
            rotationSpeed * 2.5f : // Higher rotation speed for player
            rotationSpeed;         // Normal speed for other entities
        
        // Create the target rotation
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        // Get the rigidbody (if any)
        Rigidbody rb = GetComponent<Rigidbody>();
        
        if (rb != null && !rb.isKinematic)
        {
            // Use MoveRotation for physics-based rotation
            Quaternion newRotation = Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                actualRotationSpeed * Time.deltaTime
            );
            
            rb.MoveRotation(newRotation);
        }
        else
        {
            // For non-rigidbody objects, use transform directly
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                actualRotationSpeed * Time.deltaTime
            );
        }
    }
    
    public void MoveInDirection(Vector3 direction, float speedMultiplier = 1.0f)
    {
        if (direction.magnitude < 0.1f)
            return;
        
        // Normalize direction
        direction.Normalize();
        
        // Calculate the velocity - always use transform.forward for movement
        Vector3 targetVelocity = transform.forward * moveSpeed * speedMultiplier;
        
        // Get the rigidbody (if any)
        Rigidbody rb = GetComponent<Rigidbody>();
        
        if (rb != null && !rb.isKinematic)
        {
            // For rigidbody-based movement, set velocity directly
            // This preserves vertical velocity for gravity
            rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
        }
        else
        {
            // For non-rigidbody objects, use transform directly
            transform.position += targetVelocity * Time.deltaTime;
        }
    }
    
    // Add this method to the LivingEntity class
    public void SetRotationLocked(bool locked)
    {
        rotationLocked = locked;
    }
}