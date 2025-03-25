using UnityEngine;

public class LevelUpSequence : MonoBehaviour
{
    public float levelUpEffectDuration = 1f;
    public AudioClip levelUpSound;
    public string levelUpSoundName = "LevelUpSound";
    public UnityEvent onLevelUpCompleted;
    public UIHelper uiHelper;
    public AttributeDisplay attributeDisplay;
    public SoundEffectManager soundEffectManager;

    private float levelUpEffectTimer = 0f;
    private bool levelUpEffectPlaying = false;

    void Update()
    {
        if (levelUpEffectPlaying)
        {
            levelUpEffectTimer += Time.deltaTime;
            if (levelUpEffectTimer >= levelUpEffectDuration)
            {
                levelUpEffectPlaying = false;
                levelUpEffectTimer = 0f;
                levelUpEffect.Stop();
            }
        }
    }

    public void ExecuteLevelUpSequence(int newLevel)
    {
        Debug.Log($"Beginning level up sequence for level {newLevel}");
        
        // Play level up effects
        if (levelUpEffect != null)
        {
            Debug.Log("Playing level up effect");
            levelUpEffect.Play();
            levelUpEffectPlaying = true;
        }
        
        // Play sound
        if (soundEffectManager != null)
        {
            Debug.Log("Playing level up sound");
            soundEffectManager.PlaySound(levelUpSoundName);
        }
        
        // Invoke level up callback
        if (onLevelUpCompleted != null)
        {
            Debug.Log("Calling level up completed callbacks");
            onLevelUpCompleted.Invoke(newLevel);
        }
        
        // Show level up message
        if (uiHelper != null)
        {
            uiHelper.ShowInformText($"Level up! You are now level {newLevel}");
        }
        
        Debug.Log("Level up sequence completed. Attribute panel will NOT be shown automatically.");
    }
} 