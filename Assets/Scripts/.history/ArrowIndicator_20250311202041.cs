using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private GameObject target;
    [SerializeField] private Color arrowColor = Color.red;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private bool destroyWhenReached = false;
    [SerializeField] private float reachDistance = 2f;
    
    private GameObject arrowObject;
    private bool isShowing = false;
    
    private void Start()
    {
        if (showOnStart && target != null)
        {
            ShowArrow();
        }
    }
    
    private void Update()
    {
        if (isShowing && destroyWhenReached && target != null)
        {
            // Check if player has reached the target
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance <= reachDistance)
            {
                HideArrow();
            }
        }
    }
    
    public void ShowArrow()
    {
        if (target == null)
        {
            Debug.LogWarning("Cannot show arrow: No target assigned");
            return;
        }
        
        if (ArrowIndicatorManager.Instance == null)
        {
            Debug.LogError("ArrowIndicatorManager instance not found!");
            return;
        }
        
        arrowObject = ArrowIndicatorManager.Instance.CreateArrowToTarget(target, arrowColor);
        isShowing = true;
    }
    
    public void HideArrow()
    {
        if (ArrowIndicatorManager.Instance != null && isShowing)
        {
            ArrowIndicatorManager.Instance.RemoveArrow(target);
            isShowing = false;
        }
    }
    
    public void SetTarget(GameObject newTarget)
    {
        // If already showing an arrow, hide it first
        if (isShowing)
        {
            HideArrow();
        }
        
        target = newTarget;
        
        // Show arrow to new target if we were already showing one
        if (isShowing)
        {
            ShowArrow();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up arrow when this component is destroyed
        HideArrow();
    }
} 