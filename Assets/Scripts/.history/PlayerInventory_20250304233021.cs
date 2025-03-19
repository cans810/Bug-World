using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxChitinCapacity = 999;
    
    [Header("Collection Settings")]
    [SerializeField] private string lootLayerName = "Loot";
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private AudioClip collectionSound;
    
    [Header("Experience Settings")]
    [SerializeField] private int experiencePerChitin = 10;
    
    // Event that UI can subscribe to
    public event Action<int> OnChitinCountChanged;
    public event Action<int> OnExperienceChanged;
    
    // Inventory data
    private int _chitinCount = 0;
    public int ChitinCount 
    { 
        get => _chitinCount; 
        private set 
        {
            int newValue = Mathf.Clamp(value, 0, maxChitinCapacity);
            if (_chitinCount != newValue)
            {
                _chitinCount = newValue;
                OnChitinCountChanged?.Invoke(_chitinCount);
                
                if (showDebugMessages)
                    Debug.Log($"Chitin count updated: {_chitinCount}");
            }
        }
    }
    
    private int _experience = 0;
    public int Experience
    {
        get => _experience;
        private set
        {
            _experience = value;
            OnExperienceChanged?.Invoke(_experience);
        }
    }
    
    private int lootLayerNumber;
    private AudioSource audioSource;
    
    private void Awake()
    {
        // Cache the layer number for more efficient collision checks
        lootLayerNumber = LayerMask.NameToLayer(lootLayerName);
        
        // Get or add audio source for collection sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && collectionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object is on the Loot layer
        if (other.gameObject.layer == lootLayerNumber)
        {
            // Assume it's chitin for now (you can expand this for different item types later)
            CollectChitin(other.gameObject);
        }
    }
    
    private void CollectChitin(GameObject chitinObject)
    {
        // Increase chitin count
        ChitinCount++;
        
        // Play collection sound if assigned
        if (audioSource != null && collectionSound != null)
        {
            audioSource.PlayOneShot(collectionSound);
        }
        
        // Destroy the chitin object
        Destroy(chitinObject);
    }
    
    // Method to use chitin (for crafting, etc.)
    public bool UseChitin(int amount)
    {
        if (ChitinCount >= amount)
        {
            ChitinCount -= amount;
            return true;
        }
        return false;
    }
    
    // Method to add chitin (for testing or rewards)
    public void AddChitin(int amount)
    {
        ChitinCount += amount;
    }
    
    // Remove chitin from inventory
    public bool RemoveChitin(int amount)
    {
        if (amount <= 0 || ChitinCount < amount) return false;
        
        ChitinCount -= amount;
        return true;
    }
    
    // Deposit chitin at the base and gain XP
    public void DepositChitinAtBase(int amount)
    {
        if (amount <= 0 || ChitinCount < amount) return;
        
        // Remove chitin from inventory
        ChitinCount -= amount;
        
        // Add experience
        Experience += amount * experiencePerChitin;
    }
    
    // Deposit all chitin at once
    public void DepositAllChitinAtBase()
    {
        int chitinToDeposit = ChitinCount;
        DepositChitinAtBase(chitinToDeposit);
    }
} 