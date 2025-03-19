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
                wandering.ForceNewWaypoint(); // This will make it pick a new destination inside
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
            float distanceToBoundary = Vector3.Distance(other.transform.position, transform.position);
            float boundaryRadius = boundaryCollider.radius;
            
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