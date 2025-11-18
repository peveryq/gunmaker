using UnityEngine;
using System;

/// <summary>
/// Tracks money earned during a shooting session.
/// Subscribes to MoneySystem.OnMoneyChanged when session starts.
/// </summary>
public class EarningsTracker : MonoBehaviour
{
    private int startingMoney;
    private int currentMoney;
    private bool isTracking = false;
    
    /// <summary>
    /// Event fired when earnings are calculated (when tracking ends)
    /// </summary>
    public event Action<int> OnEarningsCalculated;
    
    /// <summary>
    /// Start tracking earnings. Stores current money as starting point.
    /// </summary>
    public void StartTracking()
    {
        if (isTracking)
        {
            Debug.LogWarning("EarningsTracker: Already tracking. Call StopTracking first.");
            return;
        }
        
        if (MoneySystem.Instance == null)
        {
            Debug.LogError("EarningsTracker: MoneySystem.Instance is null!");
            return;
        }
        
        isTracking = true;
        startingMoney = MoneySystem.Instance.CurrentMoney;
        currentMoney = startingMoney;
        
        // Subscribe to money changes
        MoneySystem.Instance.OnMoneyChanged += HandleMoneyChanged;
    }
    
    /// <summary>
    /// Stop tracking and calculate total earnings.
    /// </summary>
    public int StopTracking()
    {
        if (!isTracking)
        {
            Debug.LogWarning("EarningsTracker: Not currently tracking.");
            return 0;
        }
        
        // Unsubscribe from money changes
        if (MoneySystem.Instance != null)
        {
            MoneySystem.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }
        
        isTracking = false;
        
        // Calculate earnings
        int earnings = currentMoney - startingMoney;
        
        // Fire event
        OnEarningsCalculated?.Invoke(earnings);
        
        return earnings;
    }
    
    /// <summary>
    /// Get current earnings without stopping tracking
    /// </summary>
    public int GetCurrentEarnings()
    {
        if (!isTracking)
        {
            return 0;
        }
        
        return currentMoney - startingMoney;
    }
    
    /// <summary>
    /// Check if currently tracking
    /// </summary>
    public bool IsTracking => isTracking;
    
    private void HandleMoneyChanged(int newAmount)
    {
        if (isTracking)
        {
            currentMoney = newAmount;
        }
    }
    
    private void OnDestroy()
    {
        if (isTracking && MoneySystem.Instance != null)
        {
            MoneySystem.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }
    }
}

