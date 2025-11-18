using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Manages testing range gameplay: countdown, door animations, shooting timer, and results screen.
/// </summary>
public class TestingRangeController : MonoBehaviour
{
    [Header("Countdown")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private int countdownStartValue = 5;
    [SerializeField] private float countdownInterval = 1f;
    [SerializeField] private string shootText = "shoot!";
    [SerializeField] private DOTweenAnimation countdownBubbleAnimation; // Animation for each countdown number
    [SerializeField] private DOTweenAnimation shootTextAnimation; // Separate animation for "shoot!" text
    [SerializeField] private Color shootTextColor = Color.yellow; // Color for "shoot!" text
    [SerializeField] private AudioClip countdownTickSound; // Sound for each countdown second
    [SerializeField] private AudioClip countdownEndSound; // Sound when countdown ends (before "shoot!")
    
    [Header("Shooting Timer")]
    [SerializeField] private GameObject shootingTimerRoot;
    [SerializeField] private TextMeshProUGUI shootingTimerText;
    [SerializeField] private float shootingDuration = 60f; // seconds
    [SerializeField] private float warningSecondsThreshold = 10f; // Seconds below which to show warning effects
    [SerializeField] private Color warningTextColor = Color.red; // Color for warning text
    [SerializeField] private DOTweenAnimation shootingTimerBubbleAnimation; // Animation for each second below threshold
    [SerializeField] private AudioClip timerTickSound; // Sound for each second below threshold
    [SerializeField] private AudioClip timerEndSound; // Sound when timer ends
    
    [Header("Door Animations")]
    [SerializeField] private DOTweenAnimation doorOpenAnimation;
    [SerializeField] private DOTweenAnimation doorCloseAnimation;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;
    
    [Header("UI Root")]
    [SerializeField] private GameObject uiRoot;
    
    [Header("References")]
    [SerializeField] private LocationManager locationManager;
    [SerializeField] private ResultsScreenUI resultsScreen;
    [SerializeField] private FadeScreen fadeScreen;
    [SerializeField] private FirstPersonController firstPersonController;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeOutSpeed = 0.5f;
    [SerializeField] private float delayBeforeFade = 1f; // Delay between door close and fade start
    
    private bool isActive = false;
    private Coroutine countdownCoroutine;
    private Coroutine shootingTimerCoroutine;
    private float currentShootingTime;
    private Color originalCountdownColor;
    private Color originalShootingTimerColor;
    private int lastShootingSecond = -1; // Track last second to trigger effects only once per second
    
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
        
        // Hide UI root and elements initially (immediately)
        HideAllUI();
        
        // Save original text colors
        if (countdownText != null)
        {
            originalCountdownColor = countdownText.color;
        }
        
        if (shootingTimerText != null)
        {
            originalShootingTimerColor = shootingTimerText.color;
        }
    }
    
    private void Start()
    {
        // Ensure UI is hidden at start (in case Awake didn't run or objects were enabled after)
        // This is important because UI elements might be on a different canvas that's always active
        if (!isActive)
        {
            HideAllUI();
        }
    }
    
    private void HideAllUI()
    {
        if (uiRoot != null)
        {
            uiRoot.SetActive(false);
        }
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        
        if (shootingTimerRoot != null)
        {
            shootingTimerRoot.SetActive(false);
        }
        
        if (shootingTimerText != null)
        {
            shootingTimerText.gameObject.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // Show UI root when testing range is activated
        if (uiRoot != null)
        {
            uiRoot.SetActive(true);
        }
        
        // Start countdown when testing range is activated
        StartCountdown();
    }
    
    private void OnDisable()
    {
        StopAllCoroutines();
        isActive = false;
        
        // Hide all UI immediately when testing range is deactivated
        // This is critical because the controller might be disabled before OnDisable completes
        HideAllUI();
    }
    
    /// <summary>
    /// Start the countdown sequence
    /// </summary>
    public void StartCountdown()
    {
        if (isActive) return;
        
        isActive = true;
        
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        
        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }
    
    private IEnumerator CountdownRoutine()
    {
        // Show countdown text
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }
        
        // Countdown from countdownStartValue down to 1
        for (int i = countdownStartValue; i >= 1; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                countdownText.color = originalCountdownColor; // Reset to original color
                
                // Play bubble animation
                if (countdownBubbleAnimation != null)
                {
                    countdownBubbleAnimation.CreateTween(false, false);
                    countdownBubbleAnimation.DORestart();
                }
            }
            
            // Play tick sound
            if (countdownTickSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(countdownTickSound, volume: 0.8f);
            }
            
            yield return new WaitForSeconds(countdownInterval);
        }
        
        // Show "shoot!" text
        if (countdownText != null)
        {
            countdownText.text = shootText;
            countdownText.color = shootTextColor; // Set special color for "shoot!"
            
            // Play special animation for "shoot!"
            if (shootTextAnimation != null)
            {
                shootTextAnimation.CreateTween(false, false);
                shootTextAnimation.DORestart();
            }
        }
        
        // Play end sound
        if (countdownEndSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(countdownEndSound, volume: 0.8f);
        }
        
        // Open door and start shooting timer immediately when "shoot!" appears
        OpenDoor();
        StartShootingTimer();
        
        // Wait for countdown interval, then hide countdown text
        yield return new WaitForSeconds(countdownInterval);
        
        // Hide countdown text
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        
        countdownCoroutine = null;
    }
    
    private void OpenDoor()
    {
        if (doorOpenAnimation != null)
        {
            // Ensure tween is created
            doorOpenAnimation.CreateTween(false, false);
            doorOpenAnimation.DORestart();
        }
        
        // Play door open sound
        if (doorOpenSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(doorOpenSound, volume: 0.8f);
        }
    }
    
    private void StartShootingTimer()
    {
        // Start earnings tracking
        if (locationManager != null)
        {
            locationManager.StartEarningsTracking();
        }
        
        // Show shooting timer root and text
        if (shootingTimerRoot != null)
        {
            shootingTimerRoot.SetActive(true);
        }
        
        if (shootingTimerText != null)
        {
            shootingTimerText.gameObject.SetActive(true);
            shootingTimerText.color = originalShootingTimerColor; // Reset to original color
        }
        
        currentShootingTime = shootingDuration;
        lastShootingSecond = -1; // Reset tracking
        
        if (shootingTimerCoroutine != null)
        {
            StopCoroutine(shootingTimerCoroutine);
        }
        
        shootingTimerCoroutine = StartCoroutine(ShootingTimerRoutine());
    }
    
    private IEnumerator ShootingTimerRoutine()
    {
        while (currentShootingTime > 0f)
        {
            // Update timer display (format: 00:00 - minutes:seconds)
            int minutes = Mathf.FloorToInt(currentShootingTime / 60f);
            int seconds = Mathf.FloorToInt(currentShootingTime % 60f);
            int totalSeconds = Mathf.FloorToInt(currentShootingTime);
            
            if (shootingTimerText != null)
            {
                shootingTimerText.text = $"{minutes:00}:{seconds:00}";
                
                // Check if below warning threshold
                if (currentShootingTime <= warningSecondsThreshold)
                {
                    // Change color to warning color
                    shootingTimerText.color = warningTextColor;
                    
                    // Trigger bubble animation and sound on each new second
                    if (totalSeconds != lastShootingSecond)
                    {
                        lastShootingSecond = totalSeconds;
                        
                        // Play bubble animation
                        if (shootingTimerBubbleAnimation != null)
                        {
                            shootingTimerBubbleAnimation.CreateTween(false, false);
                            shootingTimerBubbleAnimation.DORestart();
                        }
                        
                        // Play tick sound
                        if (timerTickSound != null && AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlaySFX(timerTickSound, volume: 0.8f);
                        }
                    }
                }
                else
                {
                    // Reset to original color if above threshold
                    shootingTimerText.color = originalShootingTimerColor;
                    lastShootingSecond = totalSeconds; // Update tracking
                }
            }
            
            currentShootingTime -= Time.deltaTime;
            yield return null;
        }
        
        // Timer ended
        currentShootingTime = 0f;
        if (shootingTimerText != null)
        {
            shootingTimerText.text = "00:00";
            shootingTimerText.color = warningTextColor; // Keep warning color
        }
        
        // Play end sound
        if (timerEndSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(timerEndSound, volume: 0.8f);
        }
        
        // Stop earnings tracking
        int earnings = 0;
        if (locationManager != null)
        {
            earnings = locationManager.StopEarningsTracking();
        }
        
        // Close door
        CloseDoor();
        
        // Wait for delay before starting fade
        yield return new WaitForSeconds(delayBeforeFade);
        
        // Fade out
        if (fadeScreen != null)
        {
            fadeScreen.FadeIn(fadeOutSpeed);
        }
        
        // Wait for fade
        yield return new WaitForSeconds(fadeOutSpeed);
        
        // Hide shooting timer root and text
        if (shootingTimerRoot != null)
        {
            shootingTimerRoot.SetActive(false);
        }
        
        if (shootingTimerText != null)
        {
            shootingTimerText.gameObject.SetActive(false);
        }
        
        // Show results screen
        if (resultsScreen != null)
        {
            resultsScreen.ShowResults(earnings);
        }
        
        shootingTimerCoroutine = null;
    }
    
    private void CloseDoor()
    {
        if (doorCloseAnimation != null)
        {
            // Ensure tween is created
            doorCloseAnimation.CreateTween(false, false);
            doorCloseAnimation.DORestart();
        }
        
        // Play door close sound
        if (doorCloseSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(doorCloseSound, volume: 0.8f);
        }
    }
    
    /// <summary>
    /// Get remaining shooting time
    /// </summary>
    public float RemainingTime => currentShootingTime;
    
    /// <summary>
    /// Check if shooting is active
    /// </summary>
    public bool IsShootingActive => isActive && currentShootingTime > 0f;
}

