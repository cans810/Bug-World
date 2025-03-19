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
            // Calculate how far the entity is from the boundary center
            Vector3 directionToCenter = (transform.position - other.transform.position).normalized;
            float distanceOutside = Vector3.Distance(other.transform.position, transform.position) - GetComponent<SphereCollider>().radius;
            
            // Push back by a very small amount - just enough to be inside
            Vector3 pushBackPosition = other.transform.position + directionToCenter * (distanceOutside + 0.2f);
            
            // Move the entity back inside the boundary by this small amount
            other.transform.position = pushBackPosition;
            
            // If it's a player, show a notification
            if (player != null && player.uiHelper != null)
            {
                player.uiHelper.ShowInformText("You've reached the boundary of the playable area!");
            }
            
            // If the entity has a rigidbody, reduce its outward velocity
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Calculate the component of velocity that's moving away from center
                Vector3 awayFromCenterDir = -directionToCenter;
                float awayVelocity = Vector3.Dot(rb.velocity, awayFromCenterDir);
                
                if (awayVelocity > 0)
                {
                    // Cancel out the outward velocity component
                    Vector3 outwardVelocity = awayFromCenterDir * awayVelocity;
                    rb.velocity -= outwardVelocity;
                }
            }
        }
    }
    
    // Called each frame for objects that are inside the trigger
    private void OnTriggerStay(Collider other)
    {
        // Check if the object is near the edge
        LivingEntity entity = other.GetComponent<LivingEntity>();
        PlayerController player = other.GetComponent<PlayerController>();
        
        if (entity != null || player != null)
        {
            // Calculate distance to boundary center
            float distanceToBoundary = Vector3.Distance(other.transform.position, transform.position);
            float boundaryRadius = GetComponent<SphereCollider>().radius;
            
            // If the entity is very close to the edge (95% of radius or more)
            if (distanceToBoundary > boundaryRadius * 0.95f)
            {
                // Calculate inward direction
                Vector3 inwardDirection = (transform.position - other.transform.position).normalized;
                
                // Apply strong force inward when very close to edge
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Calculate the component of velocity that's moving away from center
                    Vector3 awayFromCenterDir = -inwardDirection;
                    float awayVelocity = Vector3.Dot(rb.velocity, awayFromCenterDir);
                    
                    // Scale force based on how close to the edge and outward velocity
                    float proximityFactor = (distanceToBoundary / boundaryRadius);
                    float resistanceFactor = 8f + (awayVelocity * 2f); // Base resistance + velocity-based component
                    
                    float forceMagnitude = resistanceFactor * proximityFactor * proximityFactor;
                    rb.AddForce(inwardDirection * forceMagnitude, ForceMode.Acceleration);
                }
            }
        }
    }
} 