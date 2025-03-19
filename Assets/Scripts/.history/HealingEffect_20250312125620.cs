using UnityEngine;

public class HealingEffect : MonoBehaviour
{
    public float floatSpeed = 2f;
    public float lifetime = 1f;
    
    private void Start()
    {
        // Destroy the effect after lifetime
        Destroy(gameObject, lifetime);
    }
    
    private void Update()
    {
        // Move upward
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);
        
        // Optional: face camera
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
} 