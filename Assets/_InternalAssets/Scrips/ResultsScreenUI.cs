using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Results screen UI showing earnings and options to continue or get x2 reward.
/// </summary>
public class ResultsScreenUI : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject resultsPanel;
    
    [Header("Header")]
    [SerializeField] private GameObject timeIsUpHeader;
    [SerializeField] private GameObject timeIsUpBackground;
    
    [Header("Earnings Display")]
    [SerializeField] private TextMeshProUGUI earningsLabel;
    [SerializeField] private TextMeshProUGUI dollarSignText;
    [SerializeField] private TextMeshProUGUI earningsAmountText;
    
    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button getX2Button;
    
    [Header("References")]
    [SerializeField] private LocationManager locationManager;
    [SerializeField] private FadeScreen fadeScreen;
    [SerializeField] private FirstPersonController firstPersonController;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeOutSpeed = 0.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    
    private bool hudVisibilityCaptured;
    private bool wasControllerEnabled;
    private int currentEarnings;
    
    private void Awake()
    {
        // Find references if not assigned
        if (locationManager == null)
        {
            locationManager = FindFirstObjectByType<LocationManager>();
        }
        
        if (firstPersonController == null)
        {
            firstPersonController = FindFirstObjectByType<FirstPersonController>();
        }
        
        // Setup button listeners
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }
        
        if (getX2Button != null)
        {
            getX2Button.onClick.AddListener(OnGetX2Clicked);
        }
        
        // Hide panel initially
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Close on ESC
        if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
        {
            OnNextClicked();
        }
    }
    
    /// <summary>
    /// Show results screen with earnings
    /// </summary>
    public void ShowResults(int earnings)
    {
        currentEarnings = earnings;
        
        // Show panel
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
        }
        
        // Show header
        if (timeIsUpHeader != null)
        {
            timeIsUpHeader.SetActive(true);
        }
        
        if (timeIsUpBackground != null)
        {
            timeIsUpBackground.SetActive(true);
        }
        
        // Update earnings display
        UpdateEarningsDisplay(earnings);
        
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
    }
    
    private void UpdateEarningsDisplay(int earnings)
    {
        // Update dollar sign (separate text element)
        if (dollarSignText != null)
        {
            dollarSignText.text = "$";
        }
        
        // Update amount (separate text element with different color/size)
        if (earningsAmountText != null)
        {
            earningsAmountText.text = earnings.ToString("n0");
        }
    }
    
    private void OnNextClicked()
    {
        PlayButtonSound();
        
        // Close results screen
        CloseResults();
        
        // Return to workshop
        ReturnToWorkshop();
    }
    
    private void OnGetX2Clicked()
    {
        PlayButtonSound();
        
        // Add earnings again (future: show ad, then reward)
        if (MoneySystem.Instance != null && currentEarnings > 0)
        {
            MoneySystem.Instance.AddMoney(currentEarnings);
        }
        
        // Close results screen
        CloseResults();
        
        // Return to workshop
        ReturnToWorkshop();
    }
    
    private void CloseResults()
    {
        // Hide panel
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
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
    }
    
    private void ReturnToWorkshop()
    {
        // Fade out
        if (fadeScreen != null)
        {
            fadeScreen.FadeIn(fadeOutSpeed);
        }
        
        // Transition to workshop
        if (locationManager != null)
        {
            locationManager.TransitionToLocation(LocationManager.LocationType.Workshop);
        }
        
        // Fade in after transition
        if (fadeScreen != null)
        {
            // Fade in will be handled by LocationManager after transition
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
    /// Check if results screen is open
    /// </summary>
    public bool IsOpen => resultsPanel != null && resultsPanel.activeSelf;
    
    private void OnDestroy()
    {
        if (hudVisibilityCaptured && GameplayUIContext.HasInstance)
        {
            GameplayUIContext.Instance.ReleaseHud(this);
        }
    }
}

