using UnityEngine;
using System;

public class MoneySystem : MonoBehaviour
{
    public static MoneySystem Instance { get; private set; }
    
    [Header("Money Settings")]
    [SerializeField] private int startingMoney = 10000;
    
    private int currentMoney;
    
    // Event for UI updates
    public event Action<int> OnMoneyChanged;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            currentMoney = startingMoney;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Notify initial money amount
        OnMoneyChanged?.Invoke(currentMoney);
    }
    
    /// <summary>
    /// Add money to player's balance
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Cannot add negative money. Use SpendMoney instead.");
            return;
        }
        
        currentMoney += amount;
        OnMoneyChanged?.Invoke(currentMoney);
    }
    
    /// <summary>
    /// Attempt to spend money. Returns true if successful.
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Cannot spend negative money. Use AddMoney instead.");
            return false;
        }
        
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            OnMoneyChanged?.Invoke(currentMoney);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if player has enough money
    /// </summary>
    public bool HasEnoughMoney(int amount)
    {
        return currentMoney >= amount;
    }
    
    /// <summary>
    /// Get current money amount
    /// </summary>
    public int CurrentMoney => currentMoney;
    
    /// <summary>
    /// Reset money to starting amount (for testing)
    /// </summary>
    public void ResetMoney()
    {
        currentMoney = startingMoney;
        OnMoneyChanged?.Invoke(currentMoney);
    }
}

