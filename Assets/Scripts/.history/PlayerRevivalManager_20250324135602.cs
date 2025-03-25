using UnityEngine;
using System.Collections;

public class PlayerRevivalManager : MonoBehaviour
{
    [SerializeField] private UIHelper uiHelper;
    [SerializeField] private LivingEntity livingEntity;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float revivalDelay = 5f;
    
    // Add references to all components that need to be disabled/enabled
    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private Collider playerCollider;
    private Animator animator;
    
    private void Awake()
    {
        // Cache all the components we need to manipulate
        if (livingEntity == null) livingEntity = GetComponent<LivingEntity>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        
        characterController = GetComponent<CharacterController>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        animator = GetComponent<Animator>();
        
        // Subscribe to death event
        if (livingEntity != null)
        {
            livingEntity.OnDeath.AddListener(HandlePlayerDeath);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (livingEntity != null)
        {
            livingEntity.OnDeath.RemoveListener(HandlePlayerDeath);
        }
    }
    
    private void HandlePlayerDeath()
    {
        StartCoroutine(ReviveSequence());
    }
    
    private IEnumerator ReviveSequence()
    {
        // Disable all movement and control
        DisablePlayerComponents();
        
        // Show initial death message
        if (uiHelper != null)
            uiHelper.ShowInformText($"You are dead. Revive in {revivalDelay} seconds.");
        
        // Countdown logic
        float remainingTime = revivalDelay;
        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
            
            if (uiHelper != null)
                uiHelper.ShowInformText($"Reviving in {Mathf.CeilToInt(remainingTime)} seconds...");
        }
        
        // Move player to spawn point if available
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // CRITICAL: Hard reset ALL components
        DestroyAndRecreateComponents();
        
        // Show revival message
        if (uiHelper != null)
            uiHelper.ShowInformText("You have been revived!");
    }
    
    private void DisablePlayerComponents()
    {
        // Disable all movement and physics components
        if (playerController != null) playerController.enabled = false;
        if (characterController != null) characterController.enabled = false;
        if (playerRigidbody != null) playerRigidbody.isKinematic = true;
        if (playerCollider != null) playerCollider.enabled = false;
        
        // Reset input
        Input.ResetInputAxes();
        
        // Reset all joysticks
        foreach (OnScreenJoystick joystick in FindObjectsOfType<OnScreenJoystick>())
        {
            joystick.ResetJoystick();
        }
    }
    
    private void DestroyAndRecreateComponents()
    {
        // The nuclear option: completely destroy and recreate the player controller
        
        // 1. Store the original values we need to preserve
        float originalHealth = livingEntity.CurrentHealth;
        float originalMaxHealth = livingEntity.MaxHealth;
        
        // 2. Set isDead to false directly in LivingEntity
        System.Reflection.FieldInfo isDead = livingEntity.GetType().GetField("isDead", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (isDead != null) isDead.SetValue(livingEntity, false);
        
        // 3. Enable all the disabled components
        if (characterController != null) characterController.enabled = true;
        if (playerRigidbody != null) 
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
        if (playerCollider != null) playerCollider.enabled = true;
        
        // 4. Reset the animator
        if (animator != null)
        {
            // Reset all animation parameters
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Bool)
                    animator.SetBool(param.name, false);
                else if (param.type == AnimatorControllerParameterType.Float)
                    animator.SetFloat(param.name, 0f);
                else if (param.type == AnimatorControllerParameterType.Int)
                    animator.SetInteger(param.name, 0);
                else if (param.type == AnimatorControllerParameterType.Trigger)
                    animator.ResetTrigger(param.name);
            }
            
            // Force idle animation
            animator.Play("Idle", 0, 0f);
            
            // Ensure animator is enabled
            animator.enabled = true;
        }
        
        // 5. Destroy and recreate player controller (the nuclear option)
        if (playerController != null)
        {
            // Store relevant values
            Transform targetSpawnPoint = playerController.spawnPoint;
            UIHelper targetUiHelper = playerController.uiHelper;
            
            // Store the component type to recreate it with the same type
            System.Type controllerType = playerController.GetType();
            
            // Destroy the component
            Destroy(playerController);
            
            // Wait a frame to ensure destruction
            StartCoroutine(RecreateController(controllerType, targetSpawnPoint, targetUiHelper));
        }
        else
        {
            // If we can't destroy/recreate, just re-enable
            if (playerController != null) playerController.enabled = true;
        }
        
        // 6. Restore health
        livingEntity.SetCurrentHealth(originalMaxHealth); // Restore to full health
        
        // 7. Invoke the OnRevive event
        livingEntity.OnRevive?.Invoke();
    }
    
    private IEnumerator RecreateController(System.Type controllerType, Transform targetSpawnPoint, UIHelper targetUiHelper)
    {
        // Wait to ensure previous component is destroyed
        yield return null;
        
        // Recreate the component
        PlayerController newController = (PlayerController)gameObject.AddComponent(controllerType);
        
        // Restore references
        newController.spawnPoint = targetSpawnPoint;
        newController.uiHelper = targetUiHelper;
        
        // Force enable
        newController.enabled = true;
    }
} 