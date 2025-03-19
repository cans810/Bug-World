using UnityEngine;

// This component enforces the boundary by pushing entities back inside
public class BoundaryEnforcer : MonoBehaviour
{
    private MapBoundary boundary;
    private SphereCollider boundaryCollider;
    
    public void Initialize(MapBoundary parentBoundary)
    {
        boundary = parentBoundary;
        boundaryCollider = GetComponent<SphereCollider>();
    }
    
    // Called when an object exits the trigger
    private void OnTriggerExit(Collider other)
    {
        // Check if the exiting object is an entity we want to keep inside
        LivingEntity entity = other.GetComponent<LivingEntity>();
        if (entity == null)
            entity = other.GetComponentInParent<LivingEntity>();
            
        if (entity != null)
        {
            // Check if this is the player
            PlayerController playerController = entity.GetComponent<PlayerController>();
            
            if (playerController != null)
            {
                // For player, move them back to just inside the boundary in their direction of travel
                
                // Calculate where they crossed the boundary
                Vector3 directionFromCenter = (other.transform.position - transform.position).normalized;
                Vector3 boundaryCrossingPoint = transform.position + directionFromCenter * boundaryCollider.radius;
                
                // Move them slightly inside the boundary
                Vector3 safePosition = transform.position + directionFromCenter * (boundaryCollider.radius * 0.95f);
                
                // Teleport the player to the safe position
                other.transform.position = safePosition;
                
                // Optionally show a message to the player
                UIHelper uiHelper = FindObjectOfType<UIHelper>();
                if (uiHelper != null)
                {
                    uiHelper.ShowInformText("You've reached the boundary of this area!");
                }
            }
            else
            {
                // For non-player entities, use the existing teleport to opposite side logic
                // Push the entity back inside the boundary
                Vector3 direction = (transform.position - other.transform.position).normalized;
                Vector3 newPosition = transform.position + direction * (boundaryCollider.radius * 0.9f);
                
                // Teleport the entity back inside
                other.transform.position = newPosition;
                
                // Use forces if the entity has a rigidbody (for smoother movement)
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.AddForce(direction * 20f, ForceMode.Impulse);
                }
                
                // Notify AI components
                EnemyAI enemyAI = other.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    // TODO: Could add a method to EnemyAI to handle boundary violations
                }
                
                AIWandering wandering = other.GetComponent<AIWandering>();
                if (wandering != null)
                {
                    wandering.ForceNewWaypoint();
                }
            }
            
            Debug.Log($"Entity {other.name} pushed back inside boundary");
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
            float distanceToBoundary = Vector3.Distance(
                new Vector3(other.transform.position.x, 0, other.transform.position.z), 
                new Vector3(transform.position.x, 0, transform.position.z)
            );
            float boundaryRadius = boundaryCollider.radius;
            
            // If the entity is very close to the edge (90% of radius or more)
            if (distanceToBoundary > boundaryRadius * 0.9f)
            {
                // Check if the entity is below the boundary center - it might be falling
                if (other.transform.position.y < transform.position.y - 5f) 
                {
                    // Emergency teleport back inside
                    Vector3 inwardDirection = (transform.position - other.transform.position).normalized;
                    Vector3 newPosition = transform.position + inwardDirection * (boundaryRadius * 0.7f);
                    
                    // Try to find ground below the teleport position
                    RaycastHit hit;
                    if (Physics.Raycast(newPosition + Vector3.up * 10f, Vector3.down, out hit, 20f))
                    {
                        newPosition.y = hit.point.y + 1f;
                    }
                    
                    // Teleport
                    other.transform.position = newPosition;
                    
                    // Reset AI
                    AIWandering wandering = entity.GetComponent<AIWandering>();
                    if (wandering != null)
                    {
                        wandering.ForceNewWaypoint();
                    }
                    
                    Debug.Log($"Entity {entity.name} emergency teleport - was falling");
                }
                else
                {
                    // Normal boundary edge case
                    // Calculate inward direction (ignore Y)
                    Vector3 horizontalPos = new Vector3(other.transform.position.x, 0, other.transform.position.z);
                    Vector3 horizontalCenter = new Vector3(transform.position.x, 0, transform.position.z);
                    Vector3 inwardDirection = (horizontalCenter - horizontalPos).normalized;
                    
                    // Apply gentle force inward
                    Rigidbody rb = other.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(inwardDirection * 8f, ForceMode.Acceleration);
                    }
                }
            }
        }
    }

    // Add this method to handle physics collisions with boundary floor
    private void OnCollisionEnter(Collision collision)
    {
        // Check if we collided with the boundary floor
        if (collision.gameObject.CompareTag("BoundaryFloor"))
        {
            // Check if this is an entity we want to keep inside
            LivingEntity entity = collision.gameObject.GetComponent<LivingEntity>();
            if (entity == null)
                entity = collision.gameObject.GetComponentInParent<LivingEntity>();
                
            if (entity != null)
            {
                // Move the entity back up to a safe position
                Vector3 upDirection = Vector3.up;
                Vector3 currentPos = entity.transform.position;
                
                // Try to find ground position by raycasting downward
                RaycastHit hit;
                if (Physics.Raycast(new Vector3(currentPos.x, transform.position.y + 5f, currentPos.z), Vector3.down, out hit, 100f))
                {
                    // Move to the hit point plus a small offset
                    entity.transform.position = hit.point + Vector3.up * 1f;
                }
                else
                {
                    // If no ground found, move to boundary center with random offset
                    Vector3 randomOffset = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
                    entity.transform.position = new Vector3(
                        transform.position.x + randomOffset.x,
                        transform.position.y,
                        transform.position.z + randomOffset.z
                    );
                }
                
                // Reset velocity if it has a rigidbody
                Rigidbody rb = entity.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                }
                
                // Reset AI wandering if needed
                AIWandering wandering = entity.GetComponent<AIWandering>();
                if (wandering != null)
                {
                    wandering.ForceNewWaypoint();
                }
                
                Debug.Log($"Entity {entity.name} rescued from falling off the map");
            }
        }
    }
} 