using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Location selection UI with layout similar to ShopUI.
/// Left sidebar (decorative), top bar (exit button), main area (location info, start button).
/// </summary>
public class LocationSelectionUI : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject locationPanel;
    [SerializeField] private GameObject root;
    
    [Header("Top Bar")]
    [SerializeField] private Button exitButton;
    
    [Header("Main Area")]
    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private TextMeshProUGUI locationDescriptionText;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject grabGunFirstNotification;
    
    [Header("References")]
    [SerializeField] private LocationManager locationManager;
    [SerializeField] private InteractionHandler interactionHandler;
    [SerializeField] private FirstPersonController firstPersonController;
    
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    
    private bool hudVisibilityCaptured;
    private bool wasControllerEnabled;
    
    private void Awake()
    {
        // Get root if not assigned
        if (root == null)
        {
            root = gameObject;
        }
        
        // Find references if not assigned
        if (locationManager == null)
        {
            locationManager = FindFirstObjectByType<LocationManager>();
        }
        
        if (interactionHandler == null)
        {
            interactionHandler = FindFirstObjectByType<InteractionHandler>();
        }
        
        if (firstPersonController == null)
        {
            firstPersonController = FindFirstObjectByType<FirstPersonController>();
        }
        
        // Setup button listeners
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(CloseLocationSelection);
        }
        
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }
        
        // Hide panel and root initially
        if (root != null)
        {
            root.SetActive(false);
        }
        
        if (locationPanel != null)
        {
            locationPanel.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Update start button state based on weapon
        UpdateStartButtonState();
        
        // Close on ESC
        if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
        {
            CloseLocationSelection();
        }
    }
    
    /// <summary>
    /// Open location selection UI
    /// </summary>
    public void OpenLocationSelection()
    {
        // Show root first
        if (root != null)
        {
            root.SetActive(true);
        }
        
        // Show panel
        if (locationPanel != null)
        {
            locationPanel.SetActive(true);
        }
        
        // Request HUD hidden
        if (!hudVisibilityCaptured && GameplayUIContext.HasInstance)
        {
            GameplayUIContext.Instance.RequestHudHidden(this);
            hudVisibilityCaptured = true;
        }
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable FirstPersonController
        if (firstPersonController != null)
        {
            wasControllerEnabled = firstPersonController.enabled;
            firstPersonController.enabled = false;
        }
        
        // Update UI state
        UpdateStartButtonState();
    }
    
    /// <summary>
    /// Close location selection UI
    /// </summary>
    public void CloseLocationSelection()
    {
        // Hide panel
        if (locationPanel != null)
        {
            locationPanel.SetActive(false);
        }
        
        // Hide root
        if (root != null)
        {
            root.SetActive(false);
        }
        
        // Release HUD
        if (hudVisibilityCaptured && GameplayUIContext.HasInstance)
        {
            GameplayUIContext.Instance.ReleaseHud(this);
            hudVisibilityCaptured = false;
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable FirstPersonController
        if (firstPersonController != null && wasControllerEnabled)
        {
            firstPersonController.enabled = true;
        }
        
        PlayButtonSound();
    }
    
    private void OnStartClicked()
    {
        // Check if player has weapon
        if (!HasWeapon())
        {
            PlayButtonSound();
            return;
        }
        
        PlayButtonSound();
        
        // Close UI first
        CloseLocationSelection();
        
        // Transition to location (LocationManager will handle loading screen)
        if (locationManager != null)
        {
            locationManager.TransitionToLocation(LocationManager.LocationType.TestingRange);
        }
    }
    
    private bool HasWeapon()
    {
        if (interactionHandler == null || interactionHandler.CurrentItem == null)
        {
            return false;
        }
        
        ItemPickup currentItem = interactionHandler.CurrentItem;
        
        // Check if current item has WeaponBody or WeaponController (not just any item)
        WeaponBody weaponBody = currentItem.GetComponent<WeaponBody>();
        WeaponController weaponController = currentItem.GetComponent<WeaponController>();
        
        return weaponBody != null || weaponController != null;
    }
    
    private void UpdateStartButtonState()
    {
        bool hasWeapon = HasWeapon();
        
        if (startButton != null)
        {
            startButton.interactable = hasWeapon;
        }
        
        if (grabGunFirstNotification != null)
        {
            grabGunFirstNotification.SetActive(!hasWeapon);
        }
    }
    
    private void PlayButtonSound()
    {
        if (buttonClickSound == null) return;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSound, volume: 0.8f);
        }
    }
    
    /// <summary>
    /// Check if location selection UI is open
    /// </summary>
    public bool IsOpen => locationPanel != null && locationPanel.activeSelf;
    
    private void OnDestroy()
    {
        if (hudVisibilityCaptured && GameplayUIContext.HasInstance)
        {
            GameplayUIContext.Instance.ReleaseHud(this);
        }
    }
}

