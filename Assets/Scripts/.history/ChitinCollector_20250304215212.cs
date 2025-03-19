using System;
using UnityEngine;

public class ChitinCollector : MonoBehaviour
{
    // Singleton pattern for easy access
    public static ChitinCollector Instance { get; private set; }
    
    // Event that other scripts can subscribe to
    public event Action<int> OnChitinCountChanged;
    
    [SerializeField] private int startingChitinCount = 0;
    
    private int _chitinCount;
    public int ChitinCount 
    { 
        get => _chitinCount;
        private set
        {
            if (_chitinCount != value)
            {
                _chitinCount = value;
                OnChitinCountChanged?.Invoke(_chitinCount);
            }
        }
    }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: persist between scenes
        
        // Initialize chitin count
        ChitinCount = startingChitinCount;
    }
    
    // Call this method when player collects chitin
    public void CollectChitin(int amount = 1)
    {
        ChitinCount += amount;
        Debug.Log($"Chitin collected! Total: {ChitinCount}");
    }
    
    // Optional: method to spend chitin (for crafting, etc.)
    public bool SpendChitin(int amount)
    {
        if (ChitinCount >= amount)
        {
            ChitinCount -= amount;
            Debug.Log($"Spent {amount} chitin. Remaining: {ChitinCount}");
            return true;
        }
        
        Debug.Log($"Not enough chitin! Have: {ChitinCount}, Need: {amount}");
        return false;
    }
} 