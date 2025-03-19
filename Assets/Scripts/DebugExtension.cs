// Simple helper class for visualization
using UnityEngine;

public static class DebugExtension
{
    // Draw a wire sphere that persists in the Scene view
    public static void DebugWireSphere(Vector3 position, Color color, float radius, float duration)
    {
        // Only do this in editor
        #if UNITY_EDITOR
        // Get 12 points on the sphere
        for (int i = 0; i < 12; i++)
        {
            float angle1 = (i / 12f) * Mathf.PI * 2;
            float angle2 = ((i+1) / 12f) * Mathf.PI * 2;
            
            // X circle
            Vector3 pos1 = position + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 pos2 = position + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
            Debug.DrawLine(pos1, pos2, color, duration);
            
            // Y circle
            pos1 = position + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
            pos2 = position + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);
            Debug.DrawLine(pos1, pos2, color, duration);
            
            // Z circle
            pos1 = position + new Vector3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
            pos2 = position + new Vector3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);
            Debug.DrawLine(pos1, pos2, color, duration);
        }
        #endif
    }
} 