using UnityEngine;
using UnityEngine.EventSystems; // Add this import for UI detection

public class PlayerController : MonoBehaviour
{
    // Find the method that handles player attacks (could be Update, a custom input method, etc.)
    // and add the UI check there
    
    // For example, if you have a method like this:
    void Attack()
    {
        // Add UI check before allowing attack
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // Cursor is over UI element, don't attack
            return;
        }
        
        // ... existing attack code ...
    }
    
    // Or if the attack is triggered in Update:
    void Update()
    {
        // ... existing code ...
        
        // If attack input is detected (e.g., mouse click)
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            // Add UI check before allowing attack
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // Cursor is over UI element, don't attack
                return;
            }
            
            // Proceed with attack
            // ... existing attack code ...
        }
        
        // ... existing code ...
    }
} 