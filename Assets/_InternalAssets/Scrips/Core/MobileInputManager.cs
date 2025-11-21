using UnityEngine;
using System;

/// <summary>
/// Singleton manager for mobile input handling.
/// Provides virtual input events that can be consumed by other systems.
/// </summary>
public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager Instance { get; private set; }
    
    [Header("Input Settings")]
    [SerializeField] private bool enableMobileInput = true;
    
    // Movement input (from virtual joystick)
    public Vector2 MovementInput { get; private set; }
    public bool IsMoving => MovementInput.magnitude > 0.1f;
    
    // Button states
    public bool IsShootPressed { get; private set; }
    public bool IsAimPressed { get; private set; }
    public bool IsReloadPressed { get; private set; }
    public bool IsDropPressed { get; private set; }
    
    // Events for button presses (one-time events)
    public event Action OnShootPressed;
    public event Action OnShootReleased;
    public event Action OnAimPressed;
    public event Action OnAimReleased;
    public event Action OnReloadPressed;
    public event Action OnDropPressed;
    
    // Events for continuous input
    public event Action<Vector2> OnMovementChanged;
    
    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Move to root if parented (safety for DontDestroyOnLoad)
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
    }
    
    private void Start()
    {
        // MobileInputManager initialized
    }
    
    /// <summary>
    /// Set movement input from virtual joystick
    /// </summary>
    public void SetMovementInput(Vector2 input)
    {
        if (!enableMobileInput) return;
        
        Vector2 previousInput = MovementInput;
        MovementInput = input;
        
        if (previousInput != MovementInput)
        {
            OnMovementChanged?.Invoke(MovementInput);
        }
    }
    
    /// <summary>
    /// Set shoot button state
    /// </summary>
    public void SetShootPressed(bool pressed)
    {
        if (!enableMobileInput) return;
        
        bool wasPressed = IsShootPressed;
        IsShootPressed = pressed;
        
        if (!wasPressed && pressed)
        {
            OnShootPressed?.Invoke();
        }
        else if (wasPressed && !pressed)
        {
            OnShootReleased?.Invoke();
        }
    }
    
    /// <summary>
    /// Set aim button state
    /// </summary>
    public void SetAimPressed(bool pressed)
    {
        if (!enableMobileInput) return;
        
        bool wasPressed = IsAimPressed;
        IsAimPressed = pressed;
        
        if (!wasPressed && pressed)
        {
            OnAimPressed?.Invoke();
        }
        else if (wasPressed && !pressed)
        {
            OnAimReleased?.Invoke();
        }
    }
    
    /// <summary>
    /// Trigger reload button press (one-time action)
    /// </summary>
    public void TriggerReload()
    {
        if (!enableMobileInput) return;
        
        IsReloadPressed = true;
        OnReloadPressed?.Invoke();
        
        // Reset after one frame
        Invoke(nameof(ResetReloadPressed), 0.1f);
    }
    
    private void ResetReloadPressed()
    {
        IsReloadPressed = false;
    }
    
    /// <summary>
    /// Trigger drop button press (one-time action)
    /// </summary>
    public void TriggerDrop()
    {
        if (!enableMobileInput) return;
        
        IsDropPressed = true;
        OnDropPressed?.Invoke();
        
        // Reset after one frame
        Invoke(nameof(ResetDropPressed), 0.1f);
    }
    
    private void ResetDropPressed()
    {
        IsDropPressed = false;
    }
    
    /// <summary>
    /// Enable or disable mobile input processing
    /// </summary>
    public void SetMobileInputEnabled(bool enabled)
    {
        enableMobileInput = enabled;
        
        if (!enabled)
        {
            // Reset all inputs when disabled
            MovementInput = Vector2.zero;
            IsShootPressed = false;
            IsAimPressed = false;
            IsReloadPressed = false;
            IsDropPressed = false;
        }
    }
    
    /// <summary>
    /// Check if mobile input is currently enabled and device supports it
    /// </summary>
    public bool IsMobileInputActive()
    {
        return enableMobileInput && 
               DeviceDetectionManager.Instance != null && 
               DeviceDetectionManager.Instance.IsMobileOrTablet;
    }
}
