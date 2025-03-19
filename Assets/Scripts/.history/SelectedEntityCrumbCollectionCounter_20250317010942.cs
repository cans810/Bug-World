using UnityEngine;
using UnityEngine.UI;

public class Se : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LivingEntity targetEntity;
    [SerializeField] private Image fillI;
    
    [Header("Display Settings")]
    [SerializeField] private bool hideAtFullHealth = false;
    [SerializeField] private bool alwaysVisible = true;
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

        if (targetEntity == null)
            targetEntity = gameObject.GetComponentInParent<LivingEntity>();
            
        // Make visible immediately if alwaysVisible is true
        if (alwaysVisible)
            canvasGroup.alpha = 1f;
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
        
        // Set visibility based on settings
        if (alwaysVisible)
        {
            canvasGroup.alpha = 1f;
        }
        else if (hideAtFullHealth && targetEntity.HealthPercentage >= 1f)
        {
            canvasGroup.alpha = 0f;
        }
        else
        {
            displayTimer = displayDuration;
            canvasGroup.alpha = 1f;
        }
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
        // If always visible is enabled, keep alpha at 1
        if (alwaysVisible)
        {
            canvasGroup.alpha = 1f;
            return;
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
    
    public void UpdateHealthBar()
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
        
        // Only set alpha if not always visible
        if (!alwaysVisible)
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