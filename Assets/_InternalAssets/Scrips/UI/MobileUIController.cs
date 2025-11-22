using UnityEngine;
using System.Collections;

/// <summary>
/// Main controller for mobile UI elements.
/// Manages visibility and interaction of mobile buttons and joystick based on device type and game state.
/// </summary>
public class MobileUIController : MonoBehaviour
{
    [Header("Mobile UI Elements")]
    [SerializeField] private GameObject mobileUIRoot; // Parent object containing all mobile UI
    [SerializeField] private VirtualJoystick movementJoystick;
    
    [Header("Action Buttons (Bottom Right)")]
    [SerializeField] private MobileButton shootButton;
    [SerializeField] private MobileButton aimButton;
    [SerializeField] private MobileButton reloadButton;
    
    [Header("Additional Action Buttons")]
    [SerializeField] private MobileButton shootButton2; // Second shoot button (e.g., for left side)
    
    [Header("Utility Buttons (Bottom Left)")]
    [SerializeField] private MobileButton dropButton;
    
    private bool isMobileUIActive = false;
    private WeaponController currentWeapon;
    private FirstPersonController playerController;
    private bool hasItemInHands = false;
    private MobileCameraController cameraController;
    
    private void Start()
    {
        // Find player controller
        playerController = FindFirstObjectByType<FirstPersonController>();
        
        // Find or create mobile camera controller
        cameraController = FindFirstObjectByType<MobileCameraController>();
        if (cameraController == null && playerController != null)
        {
            cameraController = playerController.gameObject.AddComponent<MobileCameraController>();
        }
        
        // Setup button events
        SetupButtonEvents();
        
        // Setup joystick events
        SetupJoystickEvents();
        
        // Setup camera controller exclusion areas
        SetupCameraExclusionAreas();
        
        // Check device type and initialize UI
        StartCoroutine(InitializeMobileUI());
    }
    
    private IEnumerator InitializeMobileUI()
    {
        // Wait for DeviceDetectionManager to be ready
        while (DeviceDetectionManager.Instance == null)
        {
            yield return null;
        }
        
        // Subscribe to device type changes
        DeviceDetectionManager.Instance.OnDeviceTypeChanged += OnDeviceTypeChanged;
        
        // Initialize based on current device type
        OnDeviceTypeChanged(DeviceDetectionManager.Instance.CurrentDeviceType);
    }
    
    private void OnDestroy()
    {
        if (DeviceDetectionManager.Instance != null)
        {
            DeviceDetectionManager.Instance.OnDeviceTypeChanged -= OnDeviceTypeChanged;
        }
    }
    
    private void SetupButtonEvents()
    {
        // Shoot button (hold to shoot)
        if (shootButton != null)
        {
            shootButton.OnPressed += () => {
                if (MobileInputManager.Instance != null)
                    MobileInputManager.Instance.SetShootPressed(true);
            };
            shootButton.OnReleased += () => {
                if (MobileInputManager.Instance != null)
                    MobileInputManager.Instance.SetShootPressed(false);
            };
        }
        
        // Second shoot button (same functionality as first)
        if (shootButton2 != null)
        {
            shootButton2.OnPressed += () => {
                if (MobileInputManager.Instance != null)
                    MobileInputManager.Instance.SetShootPressed(true);
            };
            shootButton2.OnReleased += () => {
                if (MobileInputManager.Instance != null)
                    MobileInputManager.Instance.SetShootPressed(false);
            };
        }
        
        // Aim button (hold to aim)
        if (aimButton != null)
        {
            aimButton.OnPressed += () => {
                if (MobileInputManager.Instance != null)
                    MobileInputManager.Instance.SetAimPressed(true);
            };
            aimButton.OnReleased += () => {
                if (MobileInputManager.Instance != null)
                    MobileInputManager.Instance.SetAimPressed(false);
            };
        }
        
        // Reload button (tap to reload)
        if (reloadButton != null)
        {
            reloadButton.OnClicked += () => {
                if (MobileInputManager.Instance != null)
                    MobileInputManager.Instance.TriggerReload();
            };
        }
        
        // Drop button (tap to drop)
        if (dropButton != null)
        {
            dropButton.OnClicked += () => {
                if (MobileInputManager.Instance != null)
                    MobileInputManager.Instance.TriggerDrop();
            };
        }
    }
    
    private void SetupJoystickEvents()
    {
        if (movementJoystick != null)
        {
            movementJoystick.OnValueChanged += (Vector2 input) => {
                if (MobileInputManager.Instance != null)
                    MobileInputManager.Instance.SetMovementInput(input);
            };
        }
    }
    
    private void OnDeviceTypeChanged(DeviceDetectionManager.DeviceType deviceType)
    {
        bool shouldShowMobileUI = deviceType == DeviceDetectionManager.DeviceType.Mobile || 
                                  deviceType == DeviceDetectionManager.DeviceType.Tablet;
        
        SetMobileUIActive(shouldShowMobileUI);
    }
    
    private void SetMobileUIActive(bool active)
    {
        isMobileUIActive = active;
        
        if (mobileUIRoot != null)
        {
            mobileUIRoot.SetActive(active);
        }
        
        // Enable/disable mobile input manager
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.SetMobileInputEnabled(active);
        }
        
        // Update button visibility based on current state
        if (active)
        {
            UpdateButtonVisibility();
        }
    }
    
    /// <summary>
    /// Update button visibility based on current weapon and item state
    /// </summary>
    private void UpdateButtonVisibility()
    {
        if (!isMobileUIActive) return;
        
        bool hasWeapon = currentWeapon != null;
        bool hasWeaponWithMagazine = hasWeapon && currentWeapon.GetComponent<WeaponBody>()?.HasMagazine == true;
        
        // Weapon-related buttons (bottom right)
        if (shootButton != null)
            shootButton.SetVisible(hasWeapon);
        
        if (aimButton != null)
            aimButton.SetVisible(hasWeapon);
        
        if (reloadButton != null)
            reloadButton.SetVisible(hasWeaponWithMagazine);
        
        // Additional action buttons
        if (shootButton2 != null)
            shootButton2.SetVisible(hasWeapon);
        
        // Drop button (bottom left) - visible when holding anything
        if (dropButton != null)
            dropButton.SetVisible(hasItemInHands);
        
        // Movement joystick is always visible when mobile UI is active
        if (movementJoystick != null)
            movementJoystick.SetVisible(true);
    }
    
    /// <summary>
    /// Called when player equips a weapon
    /// </summary>
    public void OnWeaponEquipped(WeaponController weapon)
    {
        currentWeapon = weapon;
        hasItemInHands = weapon != null;
        UpdateButtonVisibility();
    }
    
    /// <summary>
    /// Called when player unequips weapon
    /// </summary>
    public void OnWeaponUnequipped()
    {
        currentWeapon = null;
        hasItemInHands = false;
        UpdateButtonVisibility();
    }
    
    /// <summary>
    /// Called when player picks up any item (weapon, part, tool, etc.)
    /// </summary>
    public void OnItemPickedUp()
    {
        hasItemInHands = true;
        UpdateButtonVisibility();
    }
    
    /// <summary>
    /// Called when player drops/places any item
    /// </summary>
    public void OnItemDropped()
    {
        hasItemInHands = false;
        UpdateButtonVisibility();
    }
    
    /// <summary>
    /// Force update button visibility (useful for external state changes)
    /// </summary>
    public void RefreshButtonVisibility()
    {
        UpdateButtonVisibility();
    }
    
    /// <summary>
    /// Enable/disable mobile UI temporarily (e.g., during cutscenes)
    /// </summary>
    public void SetTemporaryEnabled(bool enabled)
    {
        if (mobileUIRoot != null)
        {
            mobileUIRoot.SetActive(enabled && isMobileUIActive);
        }
    }
    
    /// <summary>
    /// Check if mobile UI is currently active and visible
    /// </summary>
    public bool IsMobileUIActive()
    {
        return isMobileUIActive && mobileUIRoot != null && mobileUIRoot.activeInHierarchy;
    }
    
    /// <summary>
    /// Setup camera exclusion areas to prevent camera movement when touching UI elements
    /// </summary>
    private void SetupCameraExclusionAreas()
    {
        if (cameraController == null) return;
        
        // Add joystick area as exclusion
        if (movementJoystick != null)
        {
            RectTransform joystickRect = movementJoystick.GetComponent<RectTransform>();
            if (joystickRect != null)
            {
                cameraController.AddExclusionArea(joystickRect);
            }
        }
        
        // Add all action buttons as exclusions
        AddButtonExclusionArea(shootButton);
        AddButtonExclusionArea(aimButton);
        AddButtonExclusionArea(reloadButton);
        AddButtonExclusionArea(dropButton);
        
        // Add additional action buttons
        AddButtonExclusionArea(shootButton2);
    }
    
    private void AddButtonExclusionArea(MobileButton button)
    {
        if (button != null && cameraController != null)
        {
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                cameraController.AddExclusionArea(buttonRect);
            }
        }
    }
}
