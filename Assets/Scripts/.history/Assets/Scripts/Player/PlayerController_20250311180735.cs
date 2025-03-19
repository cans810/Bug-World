using UnityEngine;
using UnityEngine.EventSystems;

// This is likely where player attacks are triggered
void HandleAttack()
{
    // Add UI check before allowing attack
    if (EventSystem.current.IsPointerOverGameObject())
    {
        // Cursor is over UI element, don't attack
        return;
    }
    
    // ... existing attack code ...
}

public class PlayerController : MonoBehaviour
{
    // ... existing code ...
} 