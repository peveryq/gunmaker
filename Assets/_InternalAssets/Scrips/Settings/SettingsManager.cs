using UnityEngine;
using System;
using YG;

/// <summary>
/// Manages game settings - loading, saving, and applying settings
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private GameSettings currentSettings;
    
    // Events for settings changes
    public event Action<GameSettings> OnSettingsChanged;
    public event Action<float> OnSensitivityChanged;
    public event Action<float> OnSFXVolumeChanged;
    public event Action<float> OnMusicVolumeChanged;
    
    private const string SETTINGS_SAVE_KEY = "GameSettings";
    
    public GameSettings CurrentSettings => currentSettings;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeSettings()
    {
        LoadSettings();
        ApplyAllSettings();
    }
    
    /// <summary>
    /// Load settings from YG2 save system
    /// </summary>
    public void LoadSettings()
    {
        // Use YG2.saves directly (like other systems do)
        if (YG2.isSDKEnabled && YG2.saves != null && !string.IsNullOrEmpty(YG2.saves.gameSettings))
        {
            try
            {
                currentSettings = JsonUtility.FromJson<GameSettings>(YG2.saves.gameSettings);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load settings: {e.Message}. Using defaults.");
                currentSettings = GameSettings.CreateDefault();
            }
        }
        else
        {
            currentSettings = GameSettings.CreateDefault();
        }
    }
    
    /// <summary>
    /// Save settings to YG2 save system (for manual save only, auto-save handles this automatically)
    /// </summary>
    private void SaveSettings()
    {
        // Note: Settings are now saved automatically by SaveSystemManager.SaveGameData()
        // This method is kept for potential manual save scenarios
        if (YG2.isSDKEnabled && YG2.saves != null)
        {
            YG2.saves.gameSettings = JsonUtility.ToJson(currentSettings);
        }
    }
    
    /// <summary>
    /// Apply all settings to their respective systems
    /// </summary>
    public void ApplyAllSettings()
    {
        ApplySensitivitySettings();
        ApplyAudioSettings();
        
        OnSettingsChanged?.Invoke(currentSettings);
    }
    
    /// <summary>
    /// Apply sensitivity settings to input systems
    /// </summary>
    private void ApplySensitivitySettings()
    {
        // Note: FirstPersonController and MobileCameraController now get sensitivity 
        // directly from SettingsManager in their Update loops, so we don't need to 
        // apply it here. This method is kept for event notification.
        
        OnSensitivityChanged?.Invoke(currentSettings.GetCurrentSensitivity());
    }
    
    /// <summary>
    /// Apply audio settings to AudioManager
    /// </summary>
    private void ApplyAudioSettings()
    {
        if (AudioManager.Instance != null)
        {
            // Use AudioManager properties (not methods)
            AudioManager.Instance.MasterVolume = currentSettings.masterVolume;
            AudioManager.Instance.SFXVolume = currentSettings.sfxVolume;
            AudioManager.Instance.MusicVolume = currentSettings.musicVolume;
        }
        
        OnSFXVolumeChanged?.Invoke(currentSettings.sfxVolume);
        OnMusicVolumeChanged?.Invoke(currentSettings.musicVolume);
    }
    
    /// <summary>
    /// Update sensitivity setting
    /// </summary>
    public void SetSensitivity(float sensitivity)
    {
        currentSettings.SetCurrentSensitivity(sensitivity);
        ApplySensitivitySettings();
        // Settings will be saved by auto-save system (every 20 seconds)
    }
    
    
    /// <summary>
    /// Update SFX volume setting
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        currentSettings.sfxVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
        // Settings will be saved by auto-save system (every 20 seconds)
    }
    
    /// <summary>
    /// Update music volume setting
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        currentSettings.musicVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
        // Settings will be saved by auto-save system (every 20 seconds)
    }
    
    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    public void ResetToDefaults()
    {
        currentSettings = GameSettings.CreateDefault();
        ApplyAllSettings();
        // Settings will be saved by auto-save system (every 20 seconds)
    }
    
    /// <summary>
    /// Clear all save data (reset game to default state)
    /// </summary>
    public void ClearAllSaveData()
    {
        if (!YG2.isSDKEnabled)
        {
            Debug.LogWarning("SettingsManager: Cannot clear saves - YG2 SDK not initialized yet.");
            return;
        }
        
        if (YG2.saves != null)
        {
            // Reset all game data to defaults
            YG2.saves.playerMoney = 10000;
            YG2.saves.savedWeapons = new System.Collections.Generic.List<WeaponSaveData>();
            YG2.saves.workbenchWeapon = null;
            YG2.saves.gameSettings = ""; // Clear settings too
            
            // Reset YG2 save system (this will reset idSave and trigger cloud sync)
            // SetDefaultSaves() resets the save ID, which tells YG2 to sync with cloud
            YG2.SetDefaultSaves();
            
            // Force save immediately (as recommended by YG2 documentation)
            // This will sync the cleared saves to cloud
            YG2.SaveProgress();
            
            // Reset current settings to defaults
            currentSettings = GameSettings.CreateDefault();
            ApplyAllSettings();
            
            Debug.Log("SettingsManager: All save data cleared, reset to defaults, and saved.");
        }
        else
        {
            Debug.LogWarning("SettingsManager: YG2.saves is null, cannot clear save data.");
        }
    }
}
