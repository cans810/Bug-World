using UnityEngine;

public class BaseInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    
    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            
            // Show UI prompt (you might want to implement this)
            Debug.Log("Press " + interactionKey + " to deposit chitin");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            playerInventory = null;
            
            // Hide UI prompt
        }
    }
    
    private void Update()
    {
        if (playerInRange && playerInventory != null && Input.GetKeyDown(interactionKey))
        {
            // Deposit all chitin at the base
            playerInventory.DepositAllChitinAtBase();
            
            // Show feedback
            Debug.Log("Chitin deposited! XP gained!");
        }
    }
} 