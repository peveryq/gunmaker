using System;
using UnityEngine;

/// <summary>
/// Game settings data structure for saving/loading
/// </summary>
[Serializable]
public class GameSettings
{
    [Header("Input Settings")]
    [Range(0.1f, 5f)]
    public float mouseSensitivity = 2f;
    
    [Range(0.1f, 5f)]
    public float touchSensitivity = 2f;
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f; // Always 1.0, not user-configurable
    
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    
    /// <summary>
    /// Create default settings
    /// </summary>
    public static GameSettings CreateDefault()
    {
        return new GameSettings
        {
            mouseSensitivity = 2f,
            touchSensitivity = 2f,
            masterVolume = 1f,
            sfxVolume = 1f,
            musicVolume = 1f
        };
    }
    
    /// <summary>
    /// Get current sensitivity based on device type
    /// </summary>
    public float GetCurrentSensitivity()
    {
        bool isMobileDevice = DeviceDetectionManager.Instance != null && 
                             DeviceDetectionManager.Instance.IsMobileOrTablet;
        
        return isMobileDevice ? touchSensitivity : mouseSensitivity;
    }
    
    /// <summary>
    /// Set sensitivity for current device type
    /// </summary>
    public void SetCurrentSensitivity(float sensitivity)
    {
        bool isMobileDevice = DeviceDetectionManager.Instance != null && 
                             DeviceDetectionManager.Instance.IsMobileOrTablet;
        
        if (isMobileDevice)
        {
            touchSensitivity = sensitivity;
        }
        else
        {
            mouseSensitivity = sensitivity;
        }
    }
}
