using UnityEngine;

// This component enforces the boundary by preventing movement beyond it
public class BoundaryEnforcer : MonoBehaviour
{
    private MapBoundary mapBoundary;
    
    // Store all entities that have been detected
    private System.Collections.Generic.List<Rigidbody> trackedEntities = new System.Collections.Generic.List<Rigidbody>();
    
    public void Initialize(MapBoundary boundary)
    {
        mapBoundary = boundary;
    }
    
    private void Update()
    {
        // Actively check each entity every frame
        for (int i = trackedEntities.Count - 1; i >= 0; i--)
        {
            Rigidbody rb = trackedEntities[i];
            
            // Skip null references (destroyed entities)
            if (rb == null)
            {
                trackedEntities.RemoveAt(i);
                continue;
            }
            
            // Check if too close to boundary
            float distanceToBoundary = Vector3.Distance(rb.transform.position, transform.position);
            float boundaryRadius = GetComponent<SphereCollider>().radius;
            
            // If entity is about to go beyond boundary (within 99% of radius)
            if (distanceToBoundary >= boundaryRadius * 0.99f)
            {
                // Get boundary normal (direction from center to entity)
                Vector3 normal = (rb.transform.position - transform.position).normalized;
                
                // If the entity has a velocity component moving outward
                float outwardVelocity = Vector3.Dot(rb.velocity, normal);
                if (outwardVelocity > 0)
                {
                    // Calculate the reflection direction
                    Vector3 reflectedVelocity = Vector3.Reflect(rb.velocity, normal);
                    
                    // Set velocity to the reflected direction
                    rb.velocity = reflectedVelocity;
                    
                    // If entity is actually outside, push it back in
                    if (distanceToBoundary > boundaryRadius)
                    {
                        // Move back exactly to the boundary
                        float overlapDistance = distanceToBoundary - boundaryRadius;
                        Vector3 correctionVector = -normal * (overlapDistance + 0.1f);
                        rb.transform.position += correctionVector;
                    }
                    
                    // If it's a player, notify them
                    PlayerController player = rb.GetComponent<PlayerController>();
                    if (player != null && player.uiHelper != null)
                    {
                        player.uiHelper.ShowInformText("You've reached the boundary of the playable area!");
                    }
                    
                    // Attempt to rotate the entity to face the new direction
                    EnemyAI enemyAI = rb.GetComponent<EnemyAI>();
                    AIWandering wandering = rb.GetComponent<AIWandering>();
                    
                    if (enemyAI != null || wandering != null)
                    {
                        // Force entity to face the reflection direction
                        rb.transform.forward = reflectedVelocity.normalized;
                    }
                }
            }
        }
    }
    
    // Track entities entering the trigger
    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null && !trackedEntities.Contains(rb))
        {
            trackedEntities.Add(rb);
        }
    }
    
    // Stop tracking entities that leave
    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            trackedEntities.Remove(rb);
            
            // Immediately push entity back if it somehow exited
            float boundaryRadius = GetComponent<SphereCollider>().radius;
            Vector3 normal = (other.transform.position - transform.position).normalized;
            
            // Place at boundary edge plus a small buffer
            Vector3 correctionPosition = transform.position + normal * (boundaryRadius * 0.99f);
            other.transform.position = correctionPosition;
            
            // Zero out any outward velocity
            Vector3 outwardVelocity = Vector3.Project(rb.velocity, normal);
            rb.velocity -= outwardVelocity;
            
            // Turn entity around
            other.transform.forward = -normal;
            
            // Re-add to tracking list
            trackedEntities.Add(rb);
        }
    }
} 