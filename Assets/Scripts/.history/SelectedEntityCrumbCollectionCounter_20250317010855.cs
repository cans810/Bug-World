using UnityEngine;
using UnityEngine.UI;

public class Se : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LivingEntity targetEntity;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Transform lookAtTarget; // Usually the camera
    
    [Header("Display Settings")]
    [SerializeField] private bool hideAtFullHealth = true;
    [SerializeField] private float displayDuration = 3f; // How long to show after damage
    [SerializeField] private float fadeSpeed = 2f; // How fast to fade out
    
    // Internal variables
    private CanvasGroup canvasGroup;
    private float displayTimer;
    private Transform entityTransform;
    private bool isDead = false;
    
    private void Awake()
    {
        // Get canvas group or add one
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Auto-find camera if no look target set
        if (lookAtTarget == null)
            lookAtTarget = Camera.main.transform;

        if (targetEntity == null)
            targetEntity = gameObject.GetComponentInParent<LivingEntity>();
    }
    
    private void Start()
    {
        if (targetEntity == null)
        {
            Debug.LogError("HealthBarController: No target entity assigned!");
            enabled = false;
            return;
        }
        
        // Store reference to entity transform
        entityTransform = targetEntity.transform;
        
        // Subscribe to entity events
        targetEntity.OnDamaged.AddListener(OnEntityDamaged);
        targetEntity.OnHealed.AddListener(OnEntityHealed);
        targetEntity.OnDeath.AddListener(OnEntityDeath);
        
        // Initial update
        UpdateHealthBar();
        
        // Initially hide if at full health
        if (hideAtFullHealth && targetEntity.HealthPercentage >= 1f)
            canvasGroup.alpha = 0f;
        else
            displayTimer = displayDuration;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (targetEntity != null)
        {
            targetEntity.OnDamaged.RemoveListener(OnEntityDamaged);
            targetEntity.OnHealed.RemoveListener(OnEntityHealed);
            targetEntity.OnDeath.RemoveListener(OnEntityDeath);
        }
    }
    
    private void Update()
    {
        // Make health bar face camera
        if (lookAtTarget != null)
        {
            // Simplest solution - always face camera directly
            transform.rotation = lookAtTarget.rotation;
        }
        else if (Camera.main != null)
        {
            // Fallback to main camera if lookAtTarget is not set
            lookAtTarget = Camera.main.transform;
        }
        
        // Handle display duration and fading
        if (!isDead && displayTimer > 0f)
        {
            displayTimer -= Time.deltaTime;
            
            // Start fading out when timer runs low
            if (displayTimer <= 1f && hideAtFullHealth && targetEntity.HealthPercentage >= 1f)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
            }
            else
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, fadeSpeed * 2f * Time.deltaTime);
            }
        }
        // Fade out when display timer ends
        else if (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthFillImage != null && targetEntity != null)
        {
            healthFillImage.fillAmount = targetEntity.HealthPercentage;
        }
    }
    
    // Event handlers
    private void OnEntityDamaged()
    {
        UpdateHealthBar();
        displayTimer = displayDuration; // Reset timer to show bar
        canvasGroup.alpha = 1f; // Ensure it's visible immediately
    }
    
    private void OnEntityHealed()
    {
        UpdateHealthBar();
        displayTimer = displayDuration; // Reset timer
    }
    
    private void OnEntityDeath()
    {
        UpdateHealthBar();
        isDead = true;
        // Optional: can hide health bar on death
        StartCoroutine(FadeOutAndDestroy());
    }
    
    private System.Collections.IEnumerator FadeOutAndDestroy()
    {
        float fadeTime = 1f;
        float timer = 0f;
        
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    // Public method to manually set the target entity
    public void SetTargetEntity(LivingEntity entity)
    {
        if (targetEntity != null)
        {
            // Unsubscribe from previous entity
            targetEntity.OnDamaged.RemoveListener(OnEntityDamaged);
            targetEntity.OnHealed.RemoveListener(OnEntityHealed);
            targetEntity.OnDeath.RemoveListener(OnEntityDeath);
        }
        
        targetEntity = entity;
        
        if (targetEntity != null)
        {
            // Subscribe to new entity
            targetEntity.OnDamaged.AddListener(OnEntityDamaged);
            targetEntity.OnHealed.AddListener(OnEntityHealed);
            targetEntity.OnDeath.AddListener(OnEntityDeath);
            
            entityTransform = targetEntity.transform;
            
            // Initial update
            UpdateHealthBar();
        }
    }
} 