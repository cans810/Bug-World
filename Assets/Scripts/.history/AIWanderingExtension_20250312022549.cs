using UnityEngine;

// This script should be added to the same GameObject that has AIWandering
public class AIWanderingExtension : MonoBehaviour
{
    private AIWandering wanderingBehavior;
    
    [SerializeField] private float minWanderDistance = 3f;
    [SerializeField] private float maxWanderDistance = 8f;
    
    private void Awake()
    {
        wanderingBehavior = GetComponent<AIWandering>();
    }
    
    public void ForceNewWanderTarget()
    {
        if (wanderingBehavior == null)
            return;
            
        // Calculate a random position away from the current position
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        Vector3 newTarget = transform.position + randomDirection * Random.Range(minWanderDistance, maxWanderDistance);
        
        // Try to use reflection to set the new target and reset the timer
        try
        {
            System.Reflection.FieldInfo targetField = wanderingBehavior.GetType().GetField("currentWanderTarget", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            System.Reflection.FieldInfo timeField = wanderingBehavior.GetType().GetField("currentWanderTime", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            System.Reflection.FieldInfo isWanderingField = wanderingBehavior.GetType().GetField("isWandering", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (targetField != null)
                targetField.SetValue(wanderingBehavior, newTarget);
                
            if (timeField != null)
                timeField.SetValue(wanderingBehavior, 0f);
                
            if (isWanderingField != null)
                isWanderingField.SetValue(wanderingBehavior, true);
                
            Debug.Log($"{gameObject.name} forced to new wander target at {newTarget}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not force new wander target: {e.Message}");
            
            // Fallback: just move the ally away from its current position
            transform.position += randomDirection * 1.0f;
        }
    }
} 