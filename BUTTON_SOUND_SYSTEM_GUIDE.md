# Button Sound System Guide

## Overview
Universal button sound system using `ButtonSoundComponent` that can be added to any Button GameObject for consistent audio feedback.

## Features
- **Click sounds** - Play when button is pressed
- **Hover sounds** - Play when mouse enters button area (optional)
- **Disabled sounds** - Play when trying to click disabled button (optional)
- **Volume control** - Individual volume setting per button
- **AudioManager integration** - Uses centralized audio system
- **Fallback support** - Works with local AudioSource if AudioManager unavailable
- **Runtime control** - Change sounds and settings via code

## Usage

### Basic Setup

1. **Add Component to Button:**
```
Button GameObject
├── Button (Component)
├── ButtonSoundComponent (Component) ← Add this
├── Image (Component)
└── Text (Child GameObject)
```

2. **Configure in Inspector:**
```
Button Sounds:
├── Click Sound: [AudioClip] (required)
├── Hover Sound: [AudioClip] (optional)
└── Disabled Sound: [AudioClip] (optional)

Sound Settings:
├── Volume: 1.0 (0.0 - 1.0)
├── Play On Hover: ✓ (checkbox)
├── Play On Click: ✓ (checkbox)
└── Play Disabled Sound: ☐ (checkbox)

Audio Source (Optional):
└── Audio Source: [AudioSource] (fallback only)
```

### Component Features

#### **Automatic Integration:**
- `[RequireComponent(typeof(Button))]` - Ensures Button component exists
- Automatically subscribes to `button.onClick` event
- Implements `IPointerEnterHandler` and `IPointerExitHandler` for hover detection

#### **Smart Audio Routing:**
1. **Primary**: Uses `AudioManager.Instance.PlaySFX()` if available
2. **Secondary**: Uses local `AudioSource` if assigned
3. **Fallback**: Creates temporary AudioSource for emergency playback

#### **Button State Awareness:**
- Only plays hover sounds on interactable buttons
- Plays click sounds only when button is interactable
- Optional disabled sound when clicking non-interactable buttons

### Code Examples

#### **Runtime Sound Control:**
```csharp
ButtonSoundComponent buttonSound = button.GetComponent<ButtonSoundComponent>();

// Change sounds at runtime
buttonSound.SetClickSound(newClickSound);
buttonSound.SetHoverSound(newHoverSound);
buttonSound.SetVolume(0.5f);

// Enable/disable sound types
buttonSound.SetHoverEnabled(false);
buttonSound.SetClickEnabled(true);

// Manually trigger sounds
buttonSound.PlayClickSound();
buttonSound.PlayHoverSound();
```

#### **Finding and Configuring Multiple Buttons:**
```csharp
// Configure all buttons in a panel
ButtonSoundComponent[] buttonSounds = panel.GetComponentsInChildren<ButtonSoundComponent>();
foreach (var buttonSound in buttonSounds)
{
    buttonSound.SetClickSound(standardClickSound);
    buttonSound.SetVolume(0.8f);
}
```

## Integration with Existing Systems

### AudioManager Integration
The component automatically uses `AudioManager.Instance.PlaySFX()`:
- Respects SFX volume settings
- Uses centralized audio routing
- Benefits from audio performance optimizations

### Mobile Compatibility
Works seamlessly with mobile input:
- Touch events trigger click sounds
- No hover sounds on touch devices (as expected)
- Consistent behavior across platforms

### Settings Integration
Volume is affected by AudioManager's SFX volume setting:
```csharp
// Final volume = buttonVolume * AudioManager.SFXVolume * AudioManager.MasterVolume
AudioManager.Instance.PlaySFX(clip, buttonVolume);
```

## Best Practices

### Sound Selection
- **Click sounds**: Short, crisp sounds (0.1-0.3 seconds)
- **Hover sounds**: Subtle, quiet sounds (0.05-0.15 seconds)
- **Disabled sounds**: Error-like sounds or muted clicks

### Volume Guidelines
- **UI buttons**: 0.7 - 1.0 volume
- **Action buttons**: 0.8 - 1.0 volume
- **Hover sounds**: 0.3 - 0.6 volume (quieter than clicks)
- **Background buttons**: 0.5 - 0.8 volume

### Performance Considerations
- Component is lightweight (minimal memory footprint)
- Sounds are played through AudioManager's optimized system
- Temporary AudioSource fallback is rare and cleaned up automatically
- No Update() loop - event-driven only

### Accessibility
- Provides audio feedback for visually impaired users
- Consistent sound patterns help with UI navigation
- Can be disabled globally through AudioManager SFX volume

## Common Use Cases

### Settings Menu
```csharp
// Settings buttons with subtle sounds
clickSound = settingsClickSound;  // Soft click
hoverSound = settingsHoverSound;  // Quiet beep
volume = 0.8f;
playOnHover = true;
```

### Action Buttons
```csharp
// Important action buttons with prominent sounds
clickSound = actionClickSound;    // Strong click
hoverSound = null;               // No hover sound
volume = 1.0f;
playOnHover = false;
```

### Danger Buttons
```csharp
// Destructive actions with warning sounds
clickSound = dangerClickSound;    // Ominous click
disabledSound = errorSound;      // Error beep
volume = 0.9f;
playDisabledSound = true;
```

## Troubleshooting

### No Sound Playing
1. Check if AudioClip is assigned
2. Verify AudioManager.Instance exists
3. Check SFX volume in AudioManager
4. Ensure button is interactable (for click sounds)

### Hover Sounds Not Working
1. Verify `playOnHover` is enabled
2. Check if button has Graphic Raycaster in Canvas
3. Ensure button's Image component has "Raycast Target" enabled
4. Test with mouse (hover doesn't work on touch devices)

### Performance Issues
1. Avoid very long audio clips for button sounds
2. Use compressed audio formats (OGG Vorbis recommended)
3. Don't assign AudioSource unless needed (AudioManager is preferred)

## Migration from Manual Sound Implementation

### Before (Manual Implementation):
```csharp
public class SettingsUI : MonoBehaviour
{
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private Button settingsButton;
    
    private void Start()
    {
        settingsButton.onClick.AddListener(() => {
            AudioManager.Instance.PlaySFX(buttonClickSound);
            OpenSettings();
        });
    }
}
```

### After (ButtonSoundComponent):
```csharp
public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Button settingsButton;
    
    private void Start()
    {
        // Sound is handled automatically by ButtonSoundComponent
        settingsButton.onClick.AddListener(OpenSettings);
    }
}
```

The sound is now configured directly on the button GameObject in the inspector, making it more modular and reusable.

## Future Extensions

The system is designed to be easily extensible:
- Add animation triggers alongside sounds
- Implement sound themes/presets
- Add haptic feedback for mobile devices
- Create sound randomization for variety
- Add conditional sound logic based on game state
