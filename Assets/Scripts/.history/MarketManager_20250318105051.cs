using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For UI components

public class MarketManager : MonoBehaviour
{
    [Header("Coin Buttons")]
    [SerializeField] private Button coin250Button;
    [SerializeField] private Button coin500Button;
    [SerializeField] private Button coin750Button;
    [SerializeField] private Button coin1000Button;

    [Header("Coin Values")]
    [SerializeField] private int smallPackAmount = 250;
    [SerializeField] private int mediumPackAmount = 500;
    [SerializeField] private int largePackAmount = 750;
    [SerializeField] private int extraLargePackAmount = 1000;

    // Reference to player's coin manager/wallet
    [SerializeField] private PlayerCoins playerCoins;

    // Start is called before the first frame update
    void Start()
    {
        // Add listeners to buttons
        if (coin250Button != null)
            coin250Button.onClick.AddListener(() => PurchaseCoins(smallPackAmount));
        
        if (coin500Button != null)
            coin500Button.onClick.AddListener(() => PurchaseCoins(mediumPackAmount));
        
        if (coin750Button != null)
            coin750Button.onClick.AddListener(() => PurchaseCoins(largePackAmount));
        
        if (coin1000Button != null)
            coin1000Button.onClick.AddListener(() => PurchaseCoins(extraLargePackAmount));
    }

    // Method to handle coin purchases
    public void PurchaseCoins(int amount)
    {
        // Here you would normally implement actual in-app purchase logic
        // For now, we'll just add the coins directly

        Debug.Log($"Purchased {amount} coins");
        
        if (playerCoins != null)
        {
            playerCoins.AddCoins(amount);
        }
        else
        {
            Debug.LogError("PlayerCoins reference is missing!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
