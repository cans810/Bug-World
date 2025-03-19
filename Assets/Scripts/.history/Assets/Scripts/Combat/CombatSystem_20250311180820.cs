using UnityEngine.EventSystems;

public class CombatSystem : MonoBehaviour
{
    public void TryAttack()
    {
        // Add UI check before allowing attack
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // Cursor is over UI element, don't attack
            return;
        }
        
        // ... existing attack code ...
    }
} 