using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Settings UI controller - manages the settings panel and sliders
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [System.Serializable]
    private class SettingsLabels
    {
        [Header("Localization Keys")]
        [Tooltip("Localization key for sensitivity label. Default: 'settings.sensitivity'")]
        public string sensitivityKey = "settings.sensitivity";
        [Tooltip("Localization key for SFX volume label. Default: 'settings.sfx_volume'")]
        public string sfxVolumeKey = "settings.sfx_volume";
        [Tooltip("Localization key for music volume label. Default: 'settings.music_volume'")]
        public string musicVolumeKey = "settings.music_volume";
        [Tooltip("Localization key for clear save data button. Default: 'settings.clear_save_data'")]
        public string clearSaveDataKey = "settings.clear_save_data";
        
        [Header("Fallback Labels (Optional)")]
        [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
        public string sensitivity = "";
        [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
        public string sfxVolume = "";
        [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
        public string musicVolume = "";
        [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
        public string clearSaveData = "";
    }
    [Header("UI References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button outsideArea; // Area outside settings panel for closing
    
    [Header("Settings Sliders")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    
    [Header("Settings Labels (Optional)")]
    [SerializeField] private TextMeshProUGUI sensitivityLabel;
    [SerializeField] private TextMeshProUGUI sfxVolumeLabel;
    [SerializeField] private TextMeshProUGUI musicVolumeLabel;
    
    [Header("Action Buttons")]
    [SerializeField] private Button clearSaveDataButton;
    
    [Header("Localization")]
    [SerializeField] private SettingsLabels labels = new SettingsLabels();
    
    private bool isOpen = false;
    private bool isInitialized = false;
    
    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
        
        // Close panel by default
        CloseSettings();
    }
    
    private void Update()
    {
        // Handle keyboard shortcut (Q key)
        // Don't open settings if player is in a fullscreen window
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!IsFullscreenBlocked())
        {
            ToggleSettings();
        }
        }
    }
    
    /// <summary>
    /// Проверка: сейчас открыт какой-либо полноэкранный UI, перекрывающий HUD?
    /// </summary>
    private bool IsFullscreenBlocked()
    {
        // На стрельбище AdManager блокируется, но это не полноэкранное окно
        // Проверяем только если мы в мастерской
        bool isInWorkshop = LocationManager.Instance != null && 
                           LocationManager.Instance.CurrentLocation == LocationManager.LocationType.Workshop;
        
        // 1) Блок от AdManager (используется для таймера рекламы и полноэкранных окон)
        // Но только если мы в мастерской (на стрельбище блок не означает полноэкранное окно)
        if (isInWorkshop && AdManager.Instance != null && AdManager.Instance.IsAdTimerBlocked)
        {
            return true;
        }
        
        // 2) Общий контекст HUD — если кто-то запросил скрытие HUD, значит на экране полноэкранный UI
        if (GameplayUIContext.HasInstance && GameplayUIContext.Instance.IsHudHidden)
        {
            return true;
        }
        
        return false;
    }
    
    private void InitializeUI()
    {
        if (SettingsManager.Instance == null) return;
        
        var settings = SettingsManager.Instance.CurrentSettings;
        
        // Initialize sliders with current settings
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 5f;
            sensitivitySlider.value = settings.GetCurrentSensitivity();
        }
        
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = settings.sfxVolume;
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = settings.musicVolume;
        }
        
        UpdateLabels();
        UpdateButtonLabels();
        isInitialized = true;
    }
    
    private void SetupEventListeners()
    {
        // Close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSettings);
        }
        
        // Outside area button
        if (outsideArea != null)
        {
            outsideArea.onClick.AddListener(CloseSettings);
        }
        
        // Slider events
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        // Action buttons
        if (clearSaveDataButton != null)
        {
            clearSaveDataButton.onClick.AddListener(OnClearSaveDataClicked);
        }
        
        // Subscribe to settings manager events
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += OnSettingsUpdated;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= OnSettingsUpdated;
        }
    }
    
    public void ToggleSettings()
    {
        if (isOpen)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }
    
    public void OpenSettings()
    {
        // Block ad timer while settings UI is open
        if (AdManager.Instance != null)
        {
            AdManager.Instance.BlockAdTimer();
        }
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            isOpen = true;
            
            // Refresh settings in case they changed
            if (!isInitialized)
            {
                InitializeUI();
            }
            else
            {
                RefreshUI();
            }
            
            // Unlock cursor for interaction and disable camera movement
            if (FirstPersonController.Instance != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                FirstPersonController.Instance.MouseLookEnabled = false;
            }
            
            // Disable mobile camera controller if present
            MobileCameraController mobileCameraController = FindFirstObjectByType<MobileCameraController>();
            if (mobileCameraController != null)
            {
                mobileCameraController.SetEnabled(false);
            }
        }
    }
    
    public void CloseSettings()
    {
        // Unblock ad timer when settings UI closes
        if (AdManager.Instance != null)
        {
            AdManager.Instance.UnblockAdTimer();
        }
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isOpen = false;
            
            // Lock cursor back and re-enable camera movement
            if (FirstPersonController.Instance != null)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                FirstPersonController.Instance.MouseLookEnabled = true;
            }
            
            // Re-enable mobile camera controller if present and on mobile device
            MobileCameraController mobileCameraController = FindFirstObjectByType<MobileCameraController>();
            if (mobileCameraController != null)
            {
                // Re-enable based on device type
                bool isMobileDevice = DeviceDetectionManager.Instance != null && 
                                     DeviceDetectionManager.Instance.IsMobileOrTablet;
                mobileCameraController.SetEnabled(isMobileDevice);
            }
        }
    }
    
    private void RefreshUI()
    {
        if (SettingsManager.Instance == null) return;
        
        var settings = SettingsManager.Instance.CurrentSettings;
        
        // Update sliders without triggering events
        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(settings.GetCurrentSensitivity());
        }
        
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
        }
        
        UpdateLabels();
    }
    
    private void UpdateLabels()
    {
        if (sensitivityLabel != null && sensitivitySlider != null)
        {
            // Universal sensitivity label (works for both mouse and touch)
            string labelText = GetLocalizedLabel(labels.sensitivityKey, labels.sensitivity, "Sensitivity");
            sensitivityLabel.text = $"{labelText}: {sensitivitySlider.value:F1}";
        }
        
        
        if (sfxVolumeLabel != null && sfxVolumeSlider != null)
        {
            string labelText = GetLocalizedLabel(labels.sfxVolumeKey, labels.sfxVolume, "SFX Volume");
            sfxVolumeLabel.text = $"{labelText}: {(sfxVolumeSlider.value * 100):F0}%";
        }
        
        if (musicVolumeLabel != null && musicVolumeSlider != null)
        {
            string labelText = GetLocalizedLabel(labels.musicVolumeKey, labels.musicVolume, "Music Volume");
            musicVolumeLabel.text = $"{labelText}: {(musicVolumeSlider.value * 100):F0}%";
        }
    }
    
    private void UpdateButtonLabels()
    {
        // Update clear save data button text
        if (clearSaveDataButton != null)
        {
            TextMeshProUGUI buttonText = clearSaveDataButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                string labelText = GetLocalizedLabel(labels.clearSaveDataKey, labels.clearSaveData, "Clear Save Data");
                buttonText.text = labelText;
            }
        }
    }
    
    // Slider event handlers
    private void OnSensitivityChanged(float value)
    {
        if (!isInitialized) return;
        
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetSensitivity(value);
        }
        
        UpdateLabels();
    }
    
    
    private void OnSFXVolumeChanged(float value)
    {
        if (!isInitialized) return;
        
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetSFXVolume(value);
        }
        
        UpdateLabels();
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        if (!isInitialized) return;
        
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetMusicVolume(value);
        }
        
        UpdateLabels();
    }
    
    // Settings manager event handler
    private void OnSettingsUpdated(GameSettings settings)
    {
        RefreshUI();
    }
    
    /// <summary>
    /// Public method to open settings (for UI button)
    /// </summary>
    public void OnSettingsButtonClicked()
    {
        OpenSettings();
    }
    
    /// <summary>
    /// Handle clear save data button click (for testing only)
    /// </summary>
    private void OnClearSaveDataClicked()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ClearAllSaveData();
            
            // Refresh UI to show default values
            RefreshUI();
            
            Debug.Log("SettingsUI: All save data cleared (testing mode).");
        }
    }
    
    /// <summary>
    /// Helper method to get localized label with fallback chain:
    /// 1. Try localization by key
    /// 2. Use custom fallback if provided
    /// 3. Use default English fallback
    /// </summary>
    private string GetLocalizedLabel(string key, string customFallback, string defaultFallback)
    {
        if (!string.IsNullOrEmpty(key))
        {
            string localized = LocalizationHelper.Get(key);
            // If localization returned something (and not just the key itself), use it
            if (localized != key || LocalizationManager.Instance != null)
            {
                // If we got a valid translation or LocalizationManager exists, use it
                if (localized != key)
                {
                    return localized;
                }
                // If key was returned, try fallback
            }
        }
        
        // Use custom fallback if provided
        if (!string.IsNullOrEmpty(customFallback))
        {
            return customFallback;
        }
        
        // Use default English fallback
        return defaultFallback;
    }
}
