using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerCoins : MonoBehaviour
{
    [SerializeField] private int currentCoins = 0;
    
    // Event that will be triggered when coins are added or removed
    public UnityEvent<int> OnCoinsChanged;

    private void Awake()
    {
        if (OnCoinsChanged == null)
            OnCoinsChanged = new UnityEvent<int>();
            
        // Load saved coins if you have a save system
        LoadCoins();
    }

    public int GetCoins()
    {
        return currentCoins;
    }

    public void AddCoins(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError("Cannot add negative coins. Use SpendCoins instead.");
            return;
        }

        currentCoins += amount;
        OnCoinsChanged.Invoke(currentCoins);
        SaveCoins();
        
        Debug.Log($"Added {amount} coins. New balance: {currentCoins}");
    }

    public bool SpendCoins(int amount)
    {
        if (amount < 0)
        {
            Debug.LogError("Cannot spend negative coins.");
            return false;
        }

        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            OnCoinsChanged.Invoke(currentCoins);
            SaveCoins();
            
            Debug.Log($"Spent {amount} coins. New balance: {currentCoins}");
            return true;
        }
        else
        {
            Debug.Log($"Not enough coins. Current balance: {currentCoins}, Trying to spend: {amount}");
            return false;
        }
    }

    private void SaveCoins()
    {
        // Save coins to PlayerPrefs or your preferred save system
        PlayerPrefs.SetInt("PlayerCoins", currentCoins);
        PlayerPrefs.Save();
    }

    private void LoadCoins()
    {
        // Load coins from PlayerPrefs or your preferred save system
        if (PlayerPrefs.HasKey("PlayerCoins"))
        {
            currentCoins = PlayerPrefs.GetInt("PlayerCoins");
        }
    }

    // Add this method to sync with PlayerInventory
    public void SyncWithPlayerInventory()
    {
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            // Update our coin count to match PlayerInventory
            currentCoins = playerInventory.CoinCount;
            OnCoinsChanged.Invoke(currentCoins);
            
            Debug.Log($"PlayerCoins synced with PlayerInventory: Coins = {currentCoins}");
        }
    }
} 