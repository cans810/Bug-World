using UnityEngine;

// This component enforces the boundary by pushing entities back inside
public class BoundaryEnforcer : MonoBehaviour
{
    private MapBoundary mapBoundary;
    
    public void Initialize(MapBoundary boundary)
    {
        mapBoundary = boundary;
    }
    
    // Called when an object exits the trigger
    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting object is a player or another entity we want to constrain
        PlayerController player = other.GetComponent<PlayerController>();
        LivingEntity entity = other.GetComponent<LivingEntity>();
        
        if (player != null || entity != null)
        {
            // Get the nearest point inside the boundary
            Vector3 safePosition = mapBoundary.GetNearestPointInBounds(other.transform.position);
            
            // Move the entity back inside the boundary
            other.transform.position = safePosition;
            
            // If it's a player, show a notification
            if (player != null && player.uiHelper != null)
            {
                player.uiHelper.ShowInformText("You've reached the boundary of the playable area!");
            }
            
            // If the entity has a rigidbody, zero out its velocity to prevent it from immediately exiting again
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Preserve vertical velocity but zero out horizontal movement
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
    }
    
    // Called each frame for objects that are inside the trigger
    private void OnTriggerStay(Collider other)
    {
        // Check if the object is near the edge and push it inward
        LivingEntity entity = other.GetComponent<LivingEntity>();
        if (entity == null)
            entity = other.GetComponentInParent<LivingEntity>();
            
        if (entity != null)
        {
            // Calculate distance to boundary center
            float distanceToBoundary = Vector3.Distance(other.transform.position, transform.position);
            float boundaryRadius = GetComponent<SphereCollider>().radius;
            
            // If the entity is very close to the edge (95% of radius or more)
            if (distanceToBoundary > boundaryRadius * 0.95f)
            {
                // Calculate inward direction
                Vector3 inwardDirection = (transform.position - other.transform.position).normalized;
                
                // Apply gentle force inward
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(inwardDirection * 5f, ForceMode.Acceleration);
                }
            }
        }
    }
} 