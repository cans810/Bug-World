using UnityEngine;

[RequireComponent(typeof(LivingEntity))]
public class AttackSoundHandler : MonoBehaviour
{
    [Header("Attack Sound Settings")]
    [SerializeField] private string attackSoundEffectName = "Attack1";
    [SerializeField] private bool useSoundEffectManager = true;
    [SerializeField] private AudioClip attackSound; // Fallback sound
    [SerializeField] private float minTimeBetweenSounds = 0.1f; // Prevent sound spam
    
    private LivingEntity livingEntity;
    private float lastSoundTime;
    
    private void Start()
    {
        livingEntity = GetComponent<LivingEntity>();
        
        if (livingEntity != null)
        {
            // Subscribe to the OnAttack event
            livingEntity.OnAttack.AddListener(HandleAttack);
        }
    }
    
    private void HandleAttack(GameObject target)
    {
        // Check cooldown to prevent sound spam
        if (Time.time - lastSoundTime < minTimeBetweenSounds)
            return;
            
        // Play attack sound
        PlayAttackSound();
        
        // Update last sound time
        lastSoundTime = Time.time;
    }
    
    private void PlayAttackSound()
    {
        if (useSoundEffectManager && SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound(attackSoundEffectName, transform.position);
        }
        else if (attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackSound, transform.position);
        }
    }
    
    private void OnDestroy()
    {
        if (livingEntity != null)
        {
            // Unsubscribe from the event
            livingEntity.OnAttack.RemoveListener(HandleAttack);
        }
    }
} 