using UnityEngine;

public class LevelUpSequence : MonoBehaviour
{
    public void ExecuteLevelUpSequence(int newLevel)
    {
        Debug.Log($"Beginning level up sequence for level {newLevel}");
        
        // Play level up effects
        if (levelUpEffect != null)
        {
            levelUpEffect.Play();
        }
        
        // Play sound
        if (soundEffectManager != null)
        {
            soundEffectManager.PlaySound(levelUpSoundName);
        }
        
        // Show level up message
        if (uiHelper != null)
        {
            uiHelper.ShowInformText($"Level up! You are now level {newLevel}");
        }
        
        Debug.Log("Level up sequence completed - attribute panel will NOT be shown automatically");
    }
} 