using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    [SerializeField] private float damage = 10f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    
    // Reference to the player (owner of this hitbox)
    private GameObject player;
    
    private void Start()
    {
        // Get reference to the player (parent object)
        player = transform.root.gameObject;
        
        // Make sure the collider is a trigger
        Collider hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError("PlayerHitbox requires a Collider component!");
        }
        
        if (showDebugMessages)
        {
            Debug.Log($"[{hitboxName}] Hitbox initialized on {gameObject.name}");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Skip if this is the player itself or part of the player
        if (other.transform.IsChildOf(player.transform) || other.gameObject == player)
        {
            return;
        }
        
        // Debug output
        if (showDebugMessages)
        {
            Debug.Log($"[{}] Hitbox collided with: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        }
        
        // Check if the other object has a LivingEntity component
        LivingEntity entity = other.GetComponent<LivingEntity>();
        if (entity == null)
        {
            entity = other.GetComponentInParent<LivingEntity>();
        }
        
        // If we found a LivingEntity, damage it
        if (entity != null)
        {

        }
        else
        {

        }
    }
    
    // Optional: Visualize the hitbox in the editor
    private void OnDrawGizmos()
    {
        Collider hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider == null)
            return;
            
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        
        if (hitboxCollider is BoxCollider)
        {
            BoxCollider boxCollider = hitboxCollider as BoxCollider;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
        else if (hitboxCollider is SphereCollider)
        {
            SphereCollider sphereCollider = hitboxCollider as SphereCollider;
            Gizmos.DrawSphere(transform.TransformPoint(sphereCollider.center), sphereCollider.radius);
        }
        else if (hitboxCollider is CapsuleCollider)
        {
            // Simplified capsule visualization
            CapsuleCollider capsuleCollider = hitboxCollider as CapsuleCollider;
            Gizmos.DrawSphere(transform.position, capsuleCollider.radius);
        }
    }
}