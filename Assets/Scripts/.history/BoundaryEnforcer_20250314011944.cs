using UnityEngine;

// This component enforces the boundary by making entities bounce off it
public class BoundaryEnforcer : MonoBehaviour
{
    private MapBoundary mapBoundary;
    
    // Track entities that are near the boundary
    private System.Collections.Generic.Dictionary<Rigidbody, float> bounceCooldowns = 
        new System.Collections.Generic.Dictionary<Rigidbody, float>();
    
    // Cooldown to prevent multiple bounces in quick succession
    private const float BOUNCE_COOLDOWN = 0.5f;
    
    public void Initialize(MapBoundary boundary)
    {
        mapBoundary = boundary;
    }
    
    private void Update()
    {
        // Update cooldowns
        var keysToRemove = new System.Collections.Generic.List<Rigidbody>();
        
        foreach (var key in bounceCooldowns.Keys)
        {
            if (key == null)
            {
                keysToRemove.Add(key);
                continue;
            }
            
            bounceCooldowns[key] -= Time.deltaTime;
            if (bounceCooldowns[key] <= 0)
            {
                keysToRemove.Add(key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            bounceCooldowns.Remove(key);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;
        
        // Skip if on cooldown
        if (bounceCooldowns.ContainsKey(rb)) return;
        
        // Check if near the edge
        float distanceToBoundary = Vector3.Distance(rb.transform.position, transform.position);
        float boundaryRadius = GetComponent<SphereCollider>().radius;
        
        // If entity is very close to boundary edge
        if (distanceToBoundary >= boundaryRadius * 0.95f)
        {
            // Calculate normal from center to entity
            Vector3 normal = (rb.transform.position - transform.position).normalized;
            normal.y = 0; // Keep movement on the horizontal plane
            normal = normal.normalized;
            
            // Calculate current movement direction
            Vector3 currentDirection = rb.transform.forward;
            currentDirection.y = 0;
            currentDirection = currentDirection.normalized;
            
            // Only bounce if moving outward
            float dotProduct = Vector3.Dot(currentDirection, normal);
            if (dotProduct > 0.2f) // Moving somewhat outward
            {
                // Simple reflection formula
                Vector3 reflectDirection = Vector3.Reflect(currentDirection, -normal);
                reflectDirection.y = 0;
                reflectDirection = reflectDirection.normalized;
                
                // Add a small random variation to prevent perfectly predictable bounces
                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-15f, 15f), 0);
                reflectDirection = randomRotation * reflectDirection;
                
                // Set the entity's rotation to face the new direction
                rb.transform.forward = reflectDirection;
                
                // Notify AI systems about the bounce
                AIWandering wandering = rb.GetComponent<AIWandering>();
                if (wandering != null)
                {
                    wandering.ForceNewWaypoint();
                }
                
                EnemyAI enemyAI = rb.GetComponent<EnemyAI>();
                if (enemyAI != null && enemyAI.enabled)
                {
                    // If this is an enemy AI that's chasing the player
                    enemyAI.RedirectAfterBounce(reflectDirection);
                }
                
                // If it's actually outside, push it back slightly
                if (distanceToBoundary > boundaryRadius)
                {
                    float overlapDistance = distanceToBoundary - boundaryRadius;
                    rb.transform.position += -normal * (overlapDistance + 0.1f);
                }
                
                // If player, show notification
                PlayerController player = rb.GetComponent<PlayerController>();
                if (player != null && player.uiHelper != null)
                {
                    player.uiHelper.ShowInformText("You've reached the boundary of the playable area!");
                }
                
                // Set cooldown to prevent multiple bounces
                bounceCooldowns[rb] = BOUNCE_COOLDOWN;
            }
        }
    }
    
    // Handle entities that somehow got outside
    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;
        
        // If somehow got outside, pull back in and point inward
        float boundaryRadius = GetComponent<SphereCollider>().radius;
        Vector3 directionToCenter = (transform.position - rb.transform.position).normalized;
        directionToCenter.y = 0;
        directionToCenter = directionToCenter.normalized;
        
        // Place at boundary edge
        rb.transform.position = transform.position - directionToCenter * (boundaryRadius * 0.95f);
        
        // Turn to face inward
        rb.transform.forward = directionToCenter;
        
        // Tell AI to redirect
        AIWandering wandering = rb.GetComponent<AIWandering>();
        if (wandering != null)
        {
            wandering.ForceNewWaypoint();
        }
    }
} 