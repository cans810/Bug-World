using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSize = 0.18f;
    [SerializeField] private Color arrowColor = Color.yellow;
    [SerializeField] private float pulseSpeed = 2.0f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    [Header("Target Settings")]
    [SerializeField] private GameObject target;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private bool destroyWhenReached = false;
    [SerializeField] private float reachDistance = 2f;
    
    [Header("Position Settings")]
    [SerializeField] private Vector2 screenPosition = new Vector2(0.5f, 0.8f);
    [SerializeField] private float smoothingFactor = 5.0f;
    
    private GameObject arrowObject;
    private bool isShowing = false;
    private Transform playerTransform;
    private Camera mainCamera;
    private Vector3 targetLastPosition;
    private Vector3 currentArrowPosition;
    
    private void Awake()
    {
        // Find the player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("ArrowIndicator: Player not found! Make sure the player has the 'Player' tag.");
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("ArrowIndicator: Main camera not found!");
        }
    }
    
    private void Start()
    {
        if (showOnStart && target != null)
        {
            ShowArrow();
        }
    }
    
    private void Update()
    {
        if (isShowing && arrowObject != null && target != null && playerTransform != null)
        {
            if (HasTargetMovedSignificantly() || HasCameraMovedSignificantly())
            {
                UpdateArrowPosition();
                targetLastPosition = target.transform.position;
            }
        }
    }
    
    private bool HasTargetMovedSignificantly()
    {
        if (targetLastPosition == Vector3.zero)
        {
            targetLastPosition = target.transform.position;
            return true;
        }
        
        return Vector3.Distance(targetLastPosition, target.transform.position) > 0.5f;
    }
    
    private bool HasCameraMovedSignificantly()
    {
        return false;
    }
    
    private void UpdateArrowPosition()
    {
        Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(target.transform.position);
        
        float distance = Vector3.Distance(playerTransform.position, target.transform.position);
        
        if (distance > reachDistance)
        {
            arrowObject.SetActive(true);
            
            bool isBehind = targetScreenPos.z < 0;
            
            bool isOnScreen = IsOnScreen(targetScreenPos) && !isBehind;
            
            Vector3 targetArrowWorldPos;
            float angle;
            
            if (isOnScreen)
            {
                Vector3 offsetTargetScreenPos = new Vector3(
                    targetScreenPos.x, 
                    targetScreenPos.y + 100f,
                    targetScreenPos.z);
                    
                targetArrowWorldPos = mainCamera.ScreenToWorldPoint(
                    new Vector3(offsetTargetScreenPos.x, offsetTargetScreenPos.y, 1f));
                    
                angle = 0;
            }
            else
            {
                Vector2 fixedScreenPos = new Vector2(Screen.width * screenPosition.x, Screen.height * screenPosition.y);
                
                Vector2 directionToTarget = new Vector2(targetScreenPos.x - fixedScreenPos.x, 
                                                       targetScreenPos.y - fixedScreenPos.y);
                
                if (directionToTarget.magnitude > 0)
                {
                    directionToTarget.Normalize();
                }
                
                angle = (Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg) + 180f;
                
                if (isBehind)
                {
                    angle += 180f;
                }
                
                targetArrowWorldPos = mainCamera.ScreenToWorldPoint(
                    new Vector3(fixedScreenPos.x, fixedScreenPos.y, 1f));
            }
            
            if (currentArrowPosition == Vector3.zero)
            {
                currentArrowPosition = targetArrowWorldPos;
            }
            else
            {
                currentArrowPosition = Vector3.Lerp(currentArrowPosition, targetArrowWorldPos, Time.deltaTime * smoothingFactor);
            }
            
            arrowObject.transform.position = currentArrowPosition;
            
            arrowObject.transform.LookAt(
                arrowObject.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
                                    
            arrowObject.transform.Rotate(0, 0, angle, Space.Self);
            
            float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
            float baseScale = arrowSize * pulse;
            arrowObject.transform.localScale = new Vector3(baseScale, baseScale, baseScale);
        }
        else
        {
            arrowObject.SetActive(false);
            
            if (distance <= reachDistance && destroyWhenReached)
            {
                HideArrow();
                Debug.Log("Player reached target, hiding arrow");
            }
        }
    }
    
    private bool IsOnScreen(Vector3 screenPos)
    {
        float padding = 50f;
        
        return screenPos.x > padding && 
               screenPos.x < Screen.width - padding && 
               screenPos.y > padding && 
               screenPos.y < Screen.height - padding &&
               screenPos.z > 0;
    }
    
    public void ShowArrow()
    {
        Debug.Log("ShowArrow called");
        
        if (target == null)
        {
            Debug.LogError("ArrowIndicator: Cannot show arrow - target is null!");
            return;
        }
        
        if (arrowPrefab == null)
        {
            Debug.LogError("ArrowIndicator: Arrow prefab is null!");
            return;
        }
        
        if (arrowObject == null)
        {
            Debug.Log($"Creating new arrow from prefab: {arrowPrefab.name}");
            arrowObject = Instantiate(arrowPrefab);
            
            SpriteRenderer renderer = arrowObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = arrowColor;
                Debug.Log($"Set arrow color to: {arrowColor}");
                renderer.sortingOrder = 1000;
            }
            else
            {
                Debug.LogWarning("Arrow prefab does not have a SpriteRenderer component!");
            }
            
            arrowObject.transform.localScale = Vector3.one * arrowSize;
            
            Vector3 fixedScreenPos = new Vector3(
                Screen.width * screenPosition.x, 
                Screen.height * screenPosition.y, 
                1f);
            Vector3 initialPos = mainCamera.ScreenToWorldPoint(fixedScreenPos);
            arrowObject.transform.position = initialPos;
            currentArrowPosition = initialPos;
        }
        
        arrowObject.SetActive(true);
        isShowing = true;
        Debug.Log($"Arrow is now visible and pointing to {target.name}");
    }
    
    public void HideArrow()
    {
        if (arrowObject != null)
        {
            arrowObject.SetActive(false);
        }
        isShowing = false;
        Debug.Log("Arrow is now hidden");
    }
    
    public void SetTarget(GameObject newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogError("ArrowIndicator: Cannot set null target!");
            return;
        }
        
        Debug.Log($"Setting arrow target to: {newTarget.name}");
        target = newTarget;
        
        if (isShowing)
        {
            HideArrow();
            ShowArrow();
        }
    }
    
    private void OnDestroy()
    {
        if (arrowObject != null)
        {
            Destroy(arrowObject);
        }
    }
    
    public void SetArrowPrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            arrowPrefab = prefab;
            
            if (arrowObject != null)
            {
                Destroy(arrowObject);
                arrowObject = null;
            }
        }
    }
    
    public void SetArrowColor(Color color)
    {
        arrowColor = color;
        
        if (arrowObject != null)
        {
            SpriteRenderer renderer = arrowObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = arrowColor;
            }
        }
    }
    
    public void SetScreenPosition(Vector2 newPosition)
    {
        screenPosition = newPosition;
        
        if (arrowObject != null && isShowing)
        {
            Vector3 fixedScreenPos = new Vector3(
                Screen.width * screenPosition.x, 
                Screen.height * screenPosition.y, 
                1f);
            Vector3 newPos = mainCamera.ScreenToWorldPoint(fixedScreenPos);
            arrowObject.transform.position = newPos;
        }
    }
} 