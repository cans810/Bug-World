using UnityEngine;

public class Billboard : MonoBehaviour
{
    public bool preserveAspect = true;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Make the sprite face the camera
            transform.forward = mainCamera.transform.forward;
            
            if (preserveAspect)
            {
                // Preserve the original scale proportions
                Vector3 originalScale = transform.localScale;
                float uniformScale = (originalScale.x + originalScale.y + originalScale.z) / 3f;
                transform.localScale = new Vector3(uniformScale, uniformScale, uniformScale);
            }
        }
    }
} 