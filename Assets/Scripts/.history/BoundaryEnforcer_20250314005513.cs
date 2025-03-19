using UnityEngine;
using System.Collections.Generic;

// This component enforces the boundary by preventing movement beyond it
public class BoundaryEnforcer : MonoBehaviour
{
    private MapBoundary mapBoundary;
    
    // Track which players have already been notified (with cooldown)
    private Dictionary<PlayerController, float> notifiedPlayers = new Dictionary<PlayerController, float>();
    private const float MESSAGE_COOLDOWN = 5f; // Only show message once every 5 seconds
    
    public void Initialize(MapBoundary boundary)
    {
        mapBoundary = boundary;
    }
    
    private void Update()
    {
        // Update cooldowns for player notifications
        List<PlayerController> playersToRemove = new List<PlayerController>();
        
        foreach (var player in notifiedPlayers.Keys)
        {
            notifiedPlayers[player] -= Time.deltaTime;
            if (notifiedPlayers[player] <= 0)
            {
                playersToRemove.Add(player);
            }
        }
        
        foreach (var player in playersToRemove)
        {
            notifiedPlayers.Remove(player);
        }
    }
    
    // Called when an object is about to exit the trigger
    private void OnTriggerStay(Collider other)
    {
        // Check if this is a player or entity we need to contain
        PlayerController player = other.GetComponent<PlayerController>();
        LivingEntity entity = other.GetComponent<LivingEntity>();
        
        if (player != null || entity != null)
        {
            // Calculate distance to boundary center
            float distanceToBoundary = Vector3.Distance(other.transform.position, transform.position);
            float boundaryRadius = GetComponent<SphereCollider>().radius;
            
            // If very close to or beyond the boundary
            if (distanceToBoundary >= boundaryRadius * 0.99f)
            {
                // Calculate direction from center to entity
                Vector3 directionFromCenter = (other.transform.position - transform.position).normalized;
                
                // Only cancel velocity component moving outward
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    // Calculate the component of velocity that's moving away from center
                    float outwardVelocity = Vector3.Dot(rb.velocity, directionFromCenter);
                    
                    // If trying to move outward, cancel that component
                    if (outwardVelocity > 0)
                    {
                        Vector3 outwardVelocityVector = directionFromCenter * outwardVelocity;
                        rb.velocity -= outwardVelocityVector;
                        
                        // Notify player if they hit the boundary and are trying to move outward
                        if (player != null && player.uiHelper != null)
                        {
                            // Only show message if we haven't recently
                            if (!notifiedPlayers.ContainsKey(player))
                            {
                                player.uiHelper.ShowInformText("You've reached the boundary of the playable area!");
                                notifiedPlayers[player] = MESSAGE_COOLDOWN;
                            }
                        }
                    }
                }
                
                // If the entity is actually beyond the boundary (can happen due to physics/frame timing)
                if (distanceToBoundary > boundaryRadius)
                {
                    // Move back exactly to the boundary, no more
                    float overlapDistance = distanceToBoundary - boundaryRadius;
                    Vector3 correctionVector = -directionFromCenter * overlapDistance;
                    other.transform.position += correctionVector;
                }
            }
        }
    }
    
    // Catch objects that somehow made it fully outside
    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        LivingEntity entity = other.GetComponent<LivingEntity>();
        
        if (player != null || entity != null)
        {
            // Just move it back to boundary edge
            Vector3 directionToCenter = (transform.position - other.transform.position).normalized;
            float boundaryRadius = GetComponent<SphereCollider>().radius;
            
            // Position exactly at boundary
            Vector3 boundaryPosition = transform.position - directionToCenter * boundaryRadius;
            other.transform.position = boundaryPosition;
            
            // Stop any outward movement
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Calculate the component of velocity that's moving away from center
                float outwardVelocity = Vector3.Dot(rb.velocity, -directionToCenter);
                if (outwardVelocity > 0)
                {
                    Vector3 outwardVelocityVector = -directionToCenter * outwardVelocity;
                    rb.velocity -= outwardVelocityVector;
                }
            }
        }
    }
} 