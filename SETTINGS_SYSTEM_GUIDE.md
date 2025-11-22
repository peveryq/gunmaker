# Settings System Setup Guide

## Overview
Complete settings system with device-specific sensitivity, audio controls, and persistent storage through YG2 save system.

## Features
- **Device-specific sensitivity**: Separate mouse/touch sensitivity automatically selected by device type
- **Audio controls**: Master, SFX, and Music volume with real-time application
- **Persistent storage**: Settings saved to YG2 save system with auto-save
- **Adaptive UI**: Dynamic labels showing current device type and values
- **Keyboard shortcut**: Q key to open/close settings
- **Click outside to close**: Intuitive UI interaction

## System Architecture

### Core Components

#### 1. GameSettings.cs
Data structure for all game settings:
```csharp
public class GameSettings
{
    public float mouseSensitivity = 2f;      // Desktop sensitivity
    public float touchSensitivity = 2f;      // Mobile/tablet sensitivity
    public float masterVolume = 1f;          // Master audio volume (always 1.0, not user-configurable)
    public float sfxVolume = 1f;             // Sound effects volume
    public float musicVolume = 1f;           // Background music volume
    
    // Automatically selects correct sensitivity based on device
    public float GetCurrentSensitivity();
    public void SetCurrentSensitivity(float sensitivity);
}
```

#### 2. SettingsManager.cs
Singleton manager handling settings persistence and application:
- **Loading**: Reads from `YG2.saves.gameSettings` JSON field
- **Saving**: Settings saved automatically by `SaveSystemManager` auto-save system (every 20 seconds)
- **Application**: Applies settings to `AudioManager` and notifies systems via events
- **Events**: `OnSettingsChanged`, `OnSensitivityChanged`, `OnMasterVolumeChanged`, etc.

#### 3. SettingsUI.cs
UI controller for the settings panel:
- **Panel management**: Show/hide with cursor lock/unlock
- **Camera control**: Disables mouse look and mobile camera when settings are open
- **Slider controls**: Real-time value updates with immediate application (saved via auto-save)
- **Dynamic labels**: Show current device type and percentage values with localization support
- **Clear save data**: Button to reset all game progress (for testing only)
- **Input handling**: Q key shortcut and outside-click closing

### Integration Points

#### Camera Control Blocking
When settings panel is open, camera movement is disabled:
```csharp
// In SettingsUI.OpenSettings()
FirstPersonController.Instance.MouseLookEnabled = false;
mobileCameraController.SetEnabled(false);

// In SettingsUI.CloseSettings()
FirstPersonController.Instance.MouseLookEnabled = true;
mobileCameraController.SetEnabled(isMobileDevice);
```

#### Input Systems
Both `FirstPersonController` and `MobileCameraController` get sensitivity directly from `SettingsManager`:

```csharp
// In FirstPersonController.HandleMouseLook()
float currentSensitivity = mouseSensitivity; // Fallback
if (SettingsManager.Instance != null)
{
    currentSensitivity = SettingsManager.Instance.CurrentSettings.GetCurrentSensitivity();
}
```

```csharp
// In MobileCameraController.ApplyCameraRotation()
float touchSensitivity = 2f; // Default fallback
if (SettingsManager.Instance != null)
{
    touchSensitivity = SettingsManager.Instance.CurrentSettings.touchSensitivity;
}
```

#### Audio System
Settings are applied directly to `AudioManager` properties:
```csharp
AudioManager.Instance.MasterVolume = currentSettings.masterVolume;
AudioManager.Instance.SFXVolume = currentSettings.sfxVolume;
AudioManager.Instance.MusicVolume = currentSettings.musicVolume;
```

#### Save System
Settings are stored in the existing YG2 save system:
```csharp
// In GameSaveData.cs (SavesYG extension)
public string gameSettings = ""; // JSON serialized GameSettings
```

#### Clear Save Data Feature
Complete save data clearing functionality:
```csharp
// In SettingsManager.ClearAllSaveData()
YG2.saves.playerMoney = 10000;
YG2.saves.savedWeapons = new List<WeaponSaveData>();
YG2.saves.workbenchWeapon = null;
YG2.saves.gameSettings = "";
YG2.SetDefaultSaves(); // Resets idSave and syncs with cloud
YG2.SaveProgress(); // Force save immediately (as per YG2 docs)
```

## Unity Setup Instructions

### 1. Create Settings UI Hierarchy

Create the following UI structure in your main Canvas:

```
Canvas
├── SettingsPanel (GameObject with SettingsUI script)
│   ├── Panel (Image - vertical panel, anchored left)
│   │   ├── Header (Horizontal Layout Group)
│   │   │   ├── Title (TextMeshPro - "Settings")
│   │   │   └── CloseButton (Button with X icon)
│   │   └── Content (Vertical Layout Group)
│   │       ├── SensitivityGroup (Vertical Layout Group)
│   │       │   ├── SensitivityLabel (TextMeshPro)
│   │       │   └── SensitivitySlider (Slider)
│   │       ├── SFXVolumeGroup (Vertical Layout Group)
│   │       │   ├── SFXVolumeLabel (TextMeshPro)
│   │       │   └── SFXVolumeSlider (Slider)
│   │       ├── MusicVolumeGroup (Vertical Layout Group)
│   │       │   ├── MusicVolumeLabel (TextMeshPro)
│   │       │   └── MusicVolumeSlider (Slider)
│   │       └── ActionButtons (Vertical Layout Group)
│   │           └── ClearSaveDataButton (Button - "Clear All Save Data")
└── OutsideArea (Button - full screen, behind panel)
```

### 2. Configure SettingsUI Component

Assign all UI references in the SettingsUI inspector:
- **Settings Panel**: The main panel GameObject
- **Close Button**: The X button in header
- **Outside Area**: The full-screen button for closing
- **All Sliders**: Sensitivity, SFX Volume, Music Volume
- **All Labels**: (Optional) For dynamic text updates
- **Clear Save Data Button**: Button to reset all game progress

### 3. Configure Sliders

Each slider should be configured as follows:
- **Sensitivity Slider**: Min Value: 0.1, Max Value: 5, Value: 2
- **Volume Sliders**: Min Value: 0, Max Value: 1, Value: 1

### 4. Add Settings Button to HUD

In your `GameplayHUD` prefab:
1. Add a Settings Button (Button component)
2. Assign it to the `settingsButton` field in GameplayHUD
3. The button will automatically open settings when clicked

### 5. Panel Styling

Recommended styling for the settings panel:
- **Position**: Anchored to left side of screen
- **Size**: Fixed width (300-400px), full height or centered
- **Background**: Semi-transparent dark background
- **Animation**: Optional slide-in from left using DOTween

### 6. Slider Styling

For consistent slider appearance:
- **Background**: Dark track with rounded corners
- **Fill Area**: Bright color (blue/green) with rounded corners
- **Handle**: Circular with slight shadow/glow effect
- **Labels**: Clear, readable font with good contrast

## Usage Examples

### Opening Settings Programmatically
```csharp
SettingsUI settingsUI = FindFirstObjectByType<SettingsUI>();
settingsUI?.OpenSettings();
```

### Listening to Settings Changes
```csharp
private void Start()
{
    if (SettingsManager.Instance != null)
    {
        SettingsManager.Instance.OnSensitivityChanged += OnSensitivityChanged;
        SettingsManager.Instance.OnMasterVolumeChanged += OnVolumeChanged;
    }
}

private void OnSensitivityChanged(float newSensitivity)
{
    Debug.Log($"Sensitivity changed to: {newSensitivity}");
}
```

### Getting Current Settings
```csharp
if (SettingsManager.Instance != null)
{
    var settings = SettingsManager.Instance.CurrentSettings;
    float sensitivity = settings.GetCurrentSensitivity(); // Device-specific
    float volume = settings.masterVolume;
}
```

## Device Detection Integration

The system automatically detects device type using `DeviceDetectionManager`:
- **Desktop**: Uses `mouseSensitivity` setting
- **Mobile/Tablet**: Uses `touchSensitivity` setting
- **UI Labels**: Show "Mouse Sensitivity" or "Touch Sensitivity" accordingly

## Troubleshooting

### Settings Not Saving
1. Ensure `SettingsManager` is initialized before use
2. Check that `YG2.isSDKEnabled` is true
3. Verify `SaveSystemManager` is present in scene

### Sliders Not Responding
1. Check all slider references are assigned in SettingsUI
2. Ensure sliders have correct min/max values
3. Verify `SettingsManager.Instance` is not null

### Audio Not Updating
1. Confirm `AudioManager.Instance` exists
2. Check that audio sources are properly configured
3. Verify volume properties are being set correctly

### Sensitivity Not Applied
1. Ensure `FirstPersonController.Instance` exists
2. Check that input systems are getting sensitivity from SettingsManager
3. Verify device detection is working correctly

## Performance Notes

- Settings are applied immediately when changed (no "Apply" button needed)
- **Auto-save integration**: Settings saved every 20 seconds through `SaveSystemManager` (workshop only)
- **No immediate saves**: Changes are NOT saved instantly to avoid save spam
- UI updates are event-driven to minimize performance impact
- Device detection is cached to avoid repeated checks

## Localization Support

The system includes full localization support similar to Workbench:

### Localization Keys
```csharp
// Default localization keys
settings.sensitivity         // Universal sensitivity label (mouse/touch)
settings.sfx_volume         // SFX volume label
settings.music_volume       // Music volume label
settings.clear_save_data    // Clear save data button
```

### Fallback System
1. **Primary**: Localization key lookup via `LocalizationHelper.Get(key)`
2. **Secondary**: Custom fallback text (configurable in inspector)
3. **Tertiary**: Default English text

### Configuration
```csharp
[Header("Localization")]
[SerializeField] private SettingsLabels labels = new SettingsLabels();
```

The `SettingsLabels` class contains:
- **Localization keys** for each label
- **Fallback text** (optional, leave empty for default English)
- **Universal sensitivity key** (works for both mouse and touch)

### Dynamic Updates
Labels update automatically when:
- Language changes (via LocalizationManager)
- Slider values change (percentage display)
- Settings values change (sensitivity, volume)

## Future Extensions

The system is designed to be easily extensible:
- Add new settings by extending `GameSettings` class
- Add new UI controls by extending `SettingsUI`
- Add new audio categories by extending audio settings
- Add graphics settings (resolution, quality, etc.)
