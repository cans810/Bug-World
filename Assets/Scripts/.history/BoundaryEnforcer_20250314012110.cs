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
    
    // The percentage of the boundary radius where we start enforcing the boundary
    private const float BOUNCE_THRESHOLD = 0.9f; // Start checking farther from edge

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
        
        // Actively check for ALL entities in the trigger, not just ones that register with OnTriggerStay
        var colliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius);
        foreach (var collider in colliders)
        {
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            if (rb == null) continue;
            
            CheckAndBounce(rb);
        }
    }
    
    private void CheckAndBounce(Rigidbody rb)
    {
        // Skip if on cooldown
        if (bounceCooldowns.ContainsKey(rb)) return;
        
        // Check distance to boundary
        float distanceToBoundary = Vector3.Distance(rb.transform.position, transform.position);
        float boundaryRadius = GetComponent<SphereCollider>().radius;
        
        // If entity is approaching the boundary edge
        if (distanceToBoundary >= boundaryRadius * BOUNCE_THRESHOLD)
        {
            // Calculate normal from center to entity
            Vector3 normal = (rb.transform.position - transform.position).normalized;
            normal.y = 0; // Keep movement on the horizontal plane
            normal = normal.normalized;
            
            // Calculate current movement direction
            Vector3 currentDirection = rb.transform.forward;
            currentDirection.y = 0;
            currentDirection = currentDirection.normalized;
            
            // Lower the threshold for outward movement detection
            float dotProduct = Vector3.Dot(currentDirection, normal);
            
            // If the entity is beyond the boundary, force a bounce regardless of direction
            bool isBeyondBoundary = distanceToBoundary > boundaryRadius;
            
            // Only bounce if moving outward or beyond boundary
            if (dotProduct > 0.05f || isBeyondBoundary) // More sensitive detection
            {
                // Simple reflection formula
                Vector3 reflectDirection = Vector3.Reflect(currentDirection, -normal);
                reflectDirection.y = 0;
                reflectDirection = reflectDirection.normalized;
                
                // Add a small random variation to prevent perfectly predictable bounces
                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-20f, 20f), 0);
                reflectDirection = randomRotation * reflectDirection;
                
                // Set the entity's rotation to face the new direction
                rb.transform.forward = reflectDirection;
                
                // FORCE the entity back inside the boundary if it's outside or very close
                if (distanceToBoundary > boundaryRadius * 0.99f)
                {
                    // Calculate safe position well inside the boundary
                    float safeDistance = boundaryRadius * 0.95f;
                    Vector3 safePosition = transform.position + normal * safeDistance;
                    
                    // Maintain Y position
                    safePosition.y = rb.transform.position.y;
                    
                    // Teleport back inside
                    rb.transform.position = safePosition;
                    
                    // Zero out velocity to prevent momentum carrying it back outside
                    rb.velocity = Vector3.zero;
                }
                
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
    
    // Also check in the trigger stay event
    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;
        
        CheckAndBounce(rb);
    }
    
    // Handle entities that somehow got outside
    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;
        
        // If got outside, force it back inside
        float boundaryRadius = GetComponent<SphereCollider>().radius;
        Vector3 directionToCenter = (transform.position - rb.transform.position).normalized;
        directionToCenter.y = 0;
        directionToCenter = directionToCenter.normalized;
        
        // Place at a safe distance inside the boundary
        Vector3 safePosition = transform.position - directionToCenter * (boundaryRadius * 0.9f);
        safePosition.y = rb.transform.position.y; // Maintain Y position
        rb.transform.position = safePosition;
        
        // Stop all momentum
        rb.velocity = Vector3.zero;
        
        // Turn to face inward
        rb.transform.forward = directionToCenter;
        
        // Tell AI to redirect
        AIWandering wandering = rb.GetComponent<AIWandering>();
        if (wandering != null)
        {
            wandering.ForceNewWaypoint();
        }
        
        EnemyAI enemyAI = rb.GetComponent<EnemyAI>();
        if (enemyAI != null && enemyAI.enabled)
        {
            enemyAI.RedirectAfterBounce(directionToCenter);
        }
        
        Debug.Log($"Entity {other.name} forced back inside boundary after escaping");
    }
    
    // Add an additional check for all physics objects
    private void FixedUpdate()
    {
        // Find any entities that are outside the boundary
        Collider[] allColliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius * 1.5f);
        float boundaryRadius = GetComponent<SphereCollider>().radius;
        
        foreach (Collider collider in allColliders)
        {
            float distance = Vector3.Distance(collider.transform.position, transform.position);
            
            // If beyond boundary
            if (distance > boundaryRadius)
            {
                Rigidbody rb = collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Force back inside
                    Vector3 directionToCenter = (transform.position - rb.transform.position).normalized;
                    Vector3 safePosition = transform.position - directionToCenter * (boundaryRadius * 0.9f);
                    safePosition.y = rb.transform.position.y; // Maintain Y position
                    
                    rb.transform.position = safePosition;
                    rb.velocity = Vector3.zero;
                    
                    // Face inward
                    rb.transform.forward = directionToCenter;
                    
                    Debug.Log($"Entity {collider.name} forced back inside boundary in FixedUpdate");
                }
            }
        }
    }
} 