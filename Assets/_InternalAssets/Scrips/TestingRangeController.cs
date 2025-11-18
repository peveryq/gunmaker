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
    [SerializeField] private float countdownInterval = 1f;
    [SerializeField] private string shootText = "shoot!";
    
    [Header("Shooting Timer")]
    [SerializeField] private TextMeshProUGUI shootingTimerText;
    [SerializeField] private float shootingDuration = 60f; // seconds
    
    [Header("Door Animations")]
    [SerializeField] private DOTweenAnimation doorOpenAnimation;
    [SerializeField] private DOTweenAnimation doorCloseAnimation;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;
    
    [Header("References")]
    [SerializeField] private LocationManager locationManager;
    [SerializeField] private ResultsScreenUI resultsScreen;
    [SerializeField] private FadeScreen fadeScreen;
    [SerializeField] private FirstPersonController firstPersonController;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeOutSpeed = 0.5f;
    
    private bool isActive = false;
    private Coroutine countdownCoroutine;
    private Coroutine shootingTimerCoroutine;
    private float currentShootingTime;
    
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
        
        // Hide UI initially
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        
        if (shootingTimerText != null)
        {
            shootingTimerText.gameObject.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // Start countdown when testing range is activated
        StartCountdown();
    }
    
    private void OnDisable()
    {
        StopAllCoroutines();
        isActive = false;
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
        
        // Countdown: 5, 4, 3, 2, 1
        for (int i = 5; i >= 1; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
            }
            
            yield return new WaitForSeconds(countdownInterval);
        }
        
        // Show "shoot!" text
        if (countdownText != null)
        {
            countdownText.text = shootText;
        }
        
        yield return new WaitForSeconds(countdownInterval);
        
        // Hide countdown text
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        
        // Open door
        OpenDoor();
        
        // Start shooting timer
        StartShootingTimer();
        
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
        
        // Show shooting timer
        if (shootingTimerText != null)
        {
            shootingTimerText.gameObject.SetActive(true);
        }
        
        currentShootingTime = shootingDuration;
        
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
            // Update timer display
            if (shootingTimerText != null)
            {
                int minutes = Mathf.FloorToInt(currentShootingTime / 60f);
                int seconds = Mathf.FloorToInt(currentShootingTime % 60f);
                shootingTimerText.text = $"{minutes:00}:{seconds:00}";
            }
            
            currentShootingTime -= Time.deltaTime;
            yield return null;
        }
        
        // Timer ended
        currentShootingTime = 0f;
        if (shootingTimerText != null)
        {
            shootingTimerText.text = "00:00";
        }
        
        // Stop earnings tracking
        int earnings = 0;
        if (locationManager != null)
        {
            earnings = locationManager.StopEarningsTracking();
        }
        
        // Close door
        CloseDoor();
        
        // Fade out
        if (fadeScreen != null)
        {
            fadeScreen.FadeIn(fadeOutSpeed);
        }
        
        // Wait for fade
        yield return new WaitForSeconds(fadeOutSpeed);
        
        // Hide shooting timer
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

