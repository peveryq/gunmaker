using System.Collections;
using UnityEngine;
using YG;

/// <summary>
/// Manages interstitial and rewarded ads integration with YG2 SDK.
/// Handles ad timing, location-based ad control, and ad display coordination.
/// Uses YG2's built-in timer system.
/// </summary>
public class AdManager : MonoBehaviour
{
    private static AdManager instance;
    public static AdManager Instance => instance;
    
    [Header("Ad Settings")]
    [Tooltip("Show ads only in workshop location")]
    [SerializeField] private bool showAdsOnlyInWorkshop = true;
    
    [Header("Next Button Ad Settings")]
    [Tooltip("Show interstitial ad on Next button every N times. Set to 1 to show every time, 2 to show every 2nd time, etc.")]
    [SerializeField] private int nextButtonAdFrequency = 2;
    
    [Tooltip("PlayerPrefs key for storing Next button ad counter. Change if you want separate counters for different builds.")]
    [SerializeField] private string nextButtonAdCounterKey = "NextButtonAdCounter";
    
    [Header("UI References")]
    [Tooltip("UI panel for ad warning with countdown timer (3-2-1). Leave empty to show ads immediately.")]
    [SerializeField] private AdTimerWarningUI adTimerWarningUI;
    
    [Header("Ad Warning Settings")]
    [Tooltip("Show countdown warning before ads")]
    [SerializeField] private bool showCountdownWarning = true;
    
    [Tooltip("Countdown duration in seconds")]
    [SerializeField] private float countdownDuration = 3f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // State
    private bool isWaitingForAd = false;
    private Coroutine adCheckCoroutine;
    private LocationManager.LocationType lastLocation = LocationManager.LocationType.Workshop;
    private int fullscreenUIBlockCount = 0; // Count of open fullscreen UIs blocking ads
    private int nextButtonAdCounter = 0; // Counter for Next button ad frequency
    private float lastAdClosedTime = -1f; // Time when last ad was closed (to prevent immediate re-show)
    private const float AD_COOLDOWN_AFTER_CLOSE = 3f; // Minimum seconds to wait after ad closes before checking again
    
    // Events
    public System.Action OnAdTimerStarted;
    public System.Action OnAdTimerStopped;
    public System.Action OnAdShown;
    
    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            Debug.LogWarning("AdManager: Another instance exists. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        
        // Move to root if parented (DontDestroyOnLoad only works for root objects)
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        DontDestroyOnLoad(gameObject);
        
        // Find AdTimerWarningUI if not assigned
        if (adTimerWarningUI == null)
        {
            adTimerWarningUI = FindFirstObjectByType<AdTimerWarningUI>();
        }
    }
    
    private void Start()
    {
        // Subscribe to YG2 events
        YG2.onOpenInterAdv += OnInterstitialAdOpened;
        YG2.onCloseInterAdv += OnInterstitialAdClosed;
        YG2.onOpenRewardedAdv += OnRewardedAdOpened;
        YG2.onCloseRewardedAdv += OnRewardedAdClosed;
        YG2.onRewardAdv += OnRewardReceived;
        YG2.onPauseGame += OnPauseGameChanged; // Subscribe to pause events
        
        // Subscribe to location changes
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChangedEvent += OnLocationChanged;
            lastLocation = LocationManager.Instance.CurrentLocation;
        }
        
        // Load Next button ad counter from PlayerPrefs
        LoadNextButtonAdCounter();
        
        // Block ad timer during initial game loading
        // Will be unblocked when game is fully initialized
        BlockAdTimer();
        
        // Subscribe to GameManager initialization event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameInitialized += OnGameInitialized;
        }
        else
        {
            // GameManager might not be ready yet, try to find it later
            StartCoroutine(WaitForGameManager());
        }
    }
    
    /// <summary>
    /// Wait for GameManager to be ready and subscribe to initialization event
    /// </summary>
    private IEnumerator WaitForGameManager()
    {
        while (GameManager.Instance == null)
        {
            yield return null;
        }
        
        GameManager.Instance.OnGameInitialized += OnGameInitialized;
        
        // If game is already initialized, unblock immediately
        if (GameManager.Instance.IsInitialized)
        {
            OnGameInitialized();
        }
    }
    
    /// <summary>
    /// Called when game is fully initialized
    /// </summary>
    private void OnGameInitialized()
    {
        if (enableDebugLogs)
        {
            Debug.Log("AdManager: Game initialized, starting ad timer check");
        }
        
        // Unblock ad timer now that game is fully loaded
        UnblockAdTimer();
        
        // Start ad timer check if in workshop
        if (ShouldShowAds())
        {
            StartAdTimerCheck();
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        YG2.onOpenInterAdv -= OnInterstitialAdOpened;
        YG2.onCloseInterAdv -= OnInterstitialAdClosed;
        YG2.onOpenRewardedAdv -= OnRewardedAdOpened;
        YG2.onCloseRewardedAdv -= OnRewardedAdClosed;
        YG2.onRewardAdv -= OnRewardReceived;
        YG2.onPauseGame -= OnPauseGameChanged;
        
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChangedEvent -= OnLocationChanged;
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameInitialized -= OnGameInitialized;
        }
        
        if (instance == this)
        {
            instance = null;
        }
    }
    
    /// <summary>
    /// Check if ads should be shown based on current location
    /// </summary>
    private bool ShouldShowAds()
    {
        if (!showAdsOnlyInWorkshop)
        {
            return true;
        }
        
        if (LocationManager.Instance == null)
        {
            return false;
        }
        
        return LocationManager.Instance.CurrentLocation == LocationManager.LocationType.Workshop;
    }
    
    /// <summary>
    /// Handle location change - reset timer when returning to workshop
    /// </summary>
    private void OnLocationChanged(LocationManager.LocationType newLocation)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"AdManager: Location changed to {newLocation} (previous: {lastLocation})");
        }
        
        // If returning to workshop, reset ad timer
        if (newLocation == LocationManager.LocationType.Workshop && 
            lastLocation == LocationManager.LocationType.TestingRange)
        {
            if (enableDebugLogs)
            {
                Debug.Log("AdManager: Returning to workshop - resetting ad timer");
            }
            
            // Force reset block count in case it got stuck
            // (LoadingScreen should handle unblock, but this is a safety net)
            if (fullscreenUIBlockCount > 0)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"AdManager: Warning - block count is {fullscreenUIBlockCount} when returning to workshop, resetting");
                }
                fullscreenUIBlockCount = 0;
            }
            
            // Reset ad timer to start fresh countdown
            ResetAdTimer();
        }
        
        lastLocation = newLocation;
        
        // Start or stop ad timer check based on location
        if (ShouldShowAds())
        {
            // Start timer check
            // Note: It won't actually check if blocked by LoadingScreen or UI
            // LoadingScreen will unblock when transition completes via UnblockAdTimer()
            StartAdTimerCheck();
            
            if (enableDebugLogs)
            {
                Debug.Log($"AdManager: Started ad timer check for {newLocation}");
            }
        }
        else
        {
            StopAdTimerCheck();
            
            if (enableDebugLogs)
            {
                Debug.Log($"AdManager: Stopped ad timer check (not in workshop)");
            }
        }
    }
    
    /// <summary>
    /// Start checking for ad timer completion
    /// </summary>
    private void StartAdTimerCheck()
    {
        if (adCheckCoroutine != null)
        {
            return; // Already running
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("AdManager: Starting ad timer check");
        }
        
        adCheckCoroutine = StartCoroutine(AdTimerCheckCoroutine());
    }
    
    /// <summary>
    /// Stop checking for ad timer completion
    /// </summary>
    private void StopAdTimerCheck()
    {
        if (adCheckCoroutine != null)
        {
            StopCoroutine(adCheckCoroutine);
            adCheckCoroutine = null;
        }
        
        // Also stop any active timer warning
        if (isWaitingForAd && adTimerWarningUI != null)
        {
            adTimerWarningUI.Hide();
            isWaitingForAd = false;
        }
    }
    
    /// <summary>
    /// Coroutine that checks if ad timer is ready and shows ad
    /// </summary>
    private IEnumerator AdTimerCheckCoroutine()
    {
        while (true)
        {
            // Check every second
            yield return new WaitForSeconds(1f);
            
            // Only check if we should show ads and timer is not already active
            // Also check if any fullscreen UI is blocking ads
            if (!ShouldShowAds() || isWaitingForAd || YG2.nowAdsShow || fullscreenUIBlockCount > 0)
            {
                if (enableDebugLogs && fullscreenUIBlockCount > 0)
                {
                    Debug.Log($"AdManager: Ad timer check skipped (blockCount: {fullscreenUIBlockCount})");
                }
                continue;
            }
            
            // Check cooldown after last ad closed (prevent immediate re-show)
            // This gives YG2 SDK time to reset its internal timer
            if (lastAdClosedTime > 0f && Time.time - lastAdClosedTime < AD_COOLDOWN_AFTER_CLOSE)
            {
                float timeSinceClose = Time.time - lastAdClosedTime;
                if (enableDebugLogs)
                {
                    Debug.Log($"AdManager: Ad timer check skipped (cooldown: {AD_COOLDOWN_AFTER_CLOSE - timeSinceClose:F1}s remaining)");
                }
                continue;
            }
            
            // Check if YG2 timer is ready
            bool timerReady = false;
            
            if (YG2.isSDKEnabled)
            {
                // Use YG2 timer
                timerReady = YG2.isTimerAdvCompleted;
                
                if (enableDebugLogs)
                {
                    float timeLeft = YG2.timerInterAdv;
                    Debug.Log($"AdManager: Checking timer (ready: {timerReady}, timeLeft: {timeLeft:F1}s)");
                }
            }
            else
            {
                // In editor without SDK, skip timer check
                continue;
            }
            
            // Only show ad if timer is ready AND we're not in cooldown period
            // Also double-check that YG2 timer was actually reset (not still at 0 from previous ad)
            if (timerReady && !isWaitingForAd)
            {
                // Additional safety check: if timer shows 0 seconds, it might be from previous ad
                // Wait a bit more to ensure YG2 SDK has reset the timer
                if (YG2.isSDKEnabled && YG2.timerInterAdv <= 0.1f && lastAdClosedTime > 0f && Time.time - lastAdClosedTime < AD_COOLDOWN_AFTER_CLOSE + 1f)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"AdManager: Timer shows ready but might be from previous ad, waiting a bit more");
                    }
                    continue;
                }
                
                if (enableDebugLogs)
                {
                    Debug.Log("AdManager: Timer ready, showing ad with warning");
                }
                // Show ad warning with countdown
                ShowAdWithWarning();
            }
        }
    }
    
    /// <summary>
    /// Show ad with warning countdown (3-2-1)
    /// </summary>
    private void ShowAdWithWarning()
    {
        if (isWaitingForAd || YG2.nowAdsShow)
        {
            return;
        }
        
        isWaitingForAd = true;
        
        if (enableDebugLogs)
        {
            Debug.Log("AdManager: Showing ad warning with countdown");
        }
        
        OnAdTimerStarted?.Invoke();
        
        // Show warning UI with countdown if enabled
        if (showCountdownWarning && adTimerWarningUI != null)
        {
            adTimerWarningUI.Show(countdownDuration, () => {
                // Callback when countdown finishes - show ad
                ShowInterstitialAd();
            });
        }
        else
        {
            // No warning UI or disabled - show ad immediately
            if (enableDebugLogs && showCountdownWarning)
            {
                Debug.LogWarning("AdManager: Countdown warning enabled but no AdTimerWarningUI assigned - showing ad immediately");
            }
            ShowInterstitialAd();
        }
    }
    
    /// <summary>
    /// Check if Next button ad will be shown (for UI logic)
    /// This simulates what will happen when ShowInterstitialAdImmediate() is called
    /// </summary>
    public bool WillShowNextButtonAd()
    {
        if (YG2.nowAdsShow)
        {
            return false; // Ad already showing
        }
        
        // Simulate increment to check what would happen
        int simulatedCounter = nextButtonAdCounter + 1;
        
        if (nextButtonAdFrequency <= 0)
        {
            return false; // Disabled
        }
        
        if (nextButtonAdFrequency == 1)
        {
            return true; // Show every time
        }
        
        // Check if simulated counter would trigger ad
        return (simulatedCounter % nextButtonAdFrequency) == 0;
    }
    
    /// <summary>
    /// Show interstitial ad immediately (without warning)
    /// Used for Next button - checks frequency before showing
    /// Forces ad to show even if YG2 timer is not ready
    /// </summary>
    public void ShowInterstitialAdImmediate()
    {
        if (YG2.nowAdsShow)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("AdManager: Ad already showing, cannot show another");
            }
            return;
        }
        
        // Increment counter FIRST (before checking frequency)
        IncrementNextButtonAdCounter();
        
        // Check if we should show ad based on frequency (after increment)
        bool shouldShow = ShouldShowNextButtonAd();
        
        if (!shouldShow)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"AdManager: Skipping Next button ad (counter: {nextButtonAdCounter}, frequency: {nextButtonAdFrequency}, next show at: {GetNextShowCounter()})");
            }
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AdManager: Showing interstitial ad immediately (Next button, counter: {nextButtonAdCounter}, frequency: {nextButtonAdFrequency})");
        }
        
        // Force show ad by temporarily setting timer to 0
        // This bypasses YG2's timer check for Next button ads
        if (YG2.isSDKEnabled && !YG2.isTimerAdvCompleted)
        {
            // Temporarily set timer to 0 to force ad show
            var ygInsidesType = System.Type.GetType("YG.Insides.YGInsides");
            if (ygInsidesType != null)
            {
                var setTimerMethod = ygInsidesType.GetMethod("SetTimerInterAdv", new System.Type[] { typeof(int) });
                if (setTimerMethod != null)
                {
                    // Set timer to 0 to make it ready
                    setTimerMethod.Invoke(null, new object[] { 0 });
                    if (enableDebugLogs)
                    {
                        Debug.Log("AdManager: Temporarily set YG2 timer to 0 to force Next button ad");
                    }
                }
            }
        }
        
        ShowInterstitialAd();
        
        // Note: YG2 will automatically reset timer to full interval after ad is shown/closed
        // When returning to workshop, ResetAdTimer() will also reset timer to full interval
        // This ensures timer is always reset to full interval on return, regardless of how ad was shown
    }
    
    /// <summary>
    /// Check if we should show ad on Next button based on frequency
    /// Counter is incremented BEFORE this check, so:
    /// - frequency=1: show every time
    /// - frequency=2: show on 2nd, 4th, 6th... (when counter % 2 == 0)
    /// - frequency=3: show on 3rd, 6th, 9th... (when counter % 3 == 0)
    /// </summary>
    private bool ShouldShowNextButtonAd()
    {
        if (nextButtonAdFrequency <= 0)
        {
            return false; // Disabled
        }
        
        if (nextButtonAdFrequency == 1)
        {
            return true; // Show every time
        }
        
        // Show when counter is a multiple of frequency (after increment)
        // Counter is incremented BEFORE this check, so:
        // - First click: counter becomes 1, 1 % 2 != 0, don't show
        // - Second click: counter becomes 2, 2 % 2 == 0, show
        // - Third click: counter becomes 3, 3 % 2 != 0, don't show
        // - Fourth click: counter becomes 4, 4 % 2 == 0, show
        bool shouldShow = (nextButtonAdCounter % nextButtonAdFrequency) == 0;
        
        if (enableDebugLogs && shouldShow)
        {
            Debug.Log($"AdManager: Next button ad should show (counter: {nextButtonAdCounter}, frequency: {nextButtonAdFrequency})");
        }
        
        return shouldShow;
    }
    
    /// <summary>
    /// Get the next counter value when ad will show (for debugging)
    /// </summary>
    private int GetNextShowCounter()
    {
        if (nextButtonAdFrequency <= 0)
        {
            return -1;
        }
        
        if (nextButtonAdFrequency == 1)
        {
            return nextButtonAdCounter + 1;
        }
        
        // Find next multiple of frequency
        int nextMultiple = ((nextButtonAdCounter / nextButtonAdFrequency) + 1) * nextButtonAdFrequency;
        return nextMultiple;
    }
    
    /// <summary>
    /// Increment Next button ad counter and save to PlayerPrefs
    /// </summary>
    private void IncrementNextButtonAdCounter()
    {
        nextButtonAdCounter++;
        SaveNextButtonAdCounter();
        
        if (enableDebugLogs)
        {
            Debug.Log($"AdManager: Next button ad counter incremented to {nextButtonAdCounter}");
        }
    }
    
    /// <summary>
    /// Load Next button ad counter from PlayerPrefs
    /// </summary>
    private void LoadNextButtonAdCounter()
    {
        nextButtonAdCounter = PlayerPrefs.GetInt(nextButtonAdCounterKey, 0);
        
        if (enableDebugLogs)
        {
            Debug.Log($"AdManager: Loaded Next button ad counter: {nextButtonAdCounter} (frequency: {nextButtonAdFrequency})");
        }
    }
    
    /// <summary>
    /// Save Next button ad counter to PlayerPrefs
    /// </summary>
    private void SaveNextButtonAdCounter()
    {
        PlayerPrefs.SetInt(nextButtonAdCounterKey, nextButtonAdCounter);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Internal method to show interstitial ad
    /// </summary>
    private void ShowInterstitialAd()
    {
        if (YG2.nowAdsShow)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("AdManager: Ad already showing");
            }
            return;
        }
        
        // YG2 will handle pause automatically via PauseGame()
        YG2.InterstitialAdvShow();
        
        OnAdShown?.Invoke();
    }
    
    /// <summary>
    /// Show rewarded ad with callback
    /// </summary>
    public void ShowRewardedAd(string rewardId, System.Action onRewardCallback)
    {
        if (YG2.nowAdsShow)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("AdManager: Ad already showing, cannot show rewarded ad");
            }
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AdManager: Showing rewarded ad with ID: {rewardId}");
        }
        
        // YG2 will handle pause automatically via PauseGame()
        YG2.RewardedAdvShow(rewardId, onRewardCallback);
    }
    
    /// <summary>
    /// Reset ad timer state (called when returning to workshop)
    /// Resets YG2 timer to full interval to start fresh countdown
    /// This ensures ads show at the configured interval (e.g., 30 seconds) from return moment
    /// </summary>
    private void ResetAdTimer()
    {
        // Reset our state
        isWaitingForAd = false;
        
        if (adTimerWarningUI != null)
        {
            adTimerWarningUI.Hide();
        }
        
        // Reset YG2 timer to full interval when returning to workshop
        // This ensures ads show at the configured interval from return moment, not immediately
        // Even if ad was shown via Next button and YG2 already reset timer, this is safe
        // (it will just reset to the same interval)
        if (YG2.isSDKEnabled)
        {
            // Use reflection to call YGInsides.SetTimerInterAdv() without parameters
            // This sets timer to full interval (interAdvInterval, e.g., 30 seconds)
            var ygInsidesType = System.Type.GetType("YG.Insides.YGInsides");
            if (ygInsidesType != null)
            {
                // Try method without parameters first (sets to full interval)
                var setTimerMethod = ygInsidesType.GetMethod("SetTimerInterAdv", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null, System.Type.EmptyTypes, null);
                
                if (setTimerMethod != null)
                {
                    setTimerMethod.Invoke(null, null);
                    if (enableDebugLogs)
                    {
                        Debug.Log($"AdManager: YG2 timer reset to full interval ({YG2.interAdvInterval}s) via SetTimerInterAdv()");
                    }
                }
                else
                {
                    // Fallback: use method with parameter (set to interAdvInterval)
                    var setTimerMethodWithParam = ygInsidesType.GetMethod("SetTimerInterAdv", 
                        new System.Type[] { typeof(int) });
                    if (setTimerMethodWithParam != null)
                    {
                        setTimerMethodWithParam.Invoke(null, new object[] { YG2.interAdvInterval });
                        if (enableDebugLogs)
                        {
                            Debug.Log($"AdManager: YG2 timer reset to full interval ({YG2.interAdvInterval}s) via SetTimerInterAdv(int)");
                        }
                    }
                }
            }
        }
        
        if (enableDebugLogs)
        {
            float timeLeft = YG2.isSDKEnabled ? YG2.timerInterAdv : 0f;
            Debug.Log($"AdManager: Ad timer reset - will start fresh countdown from return moment (YG2 timer: {timeLeft:F1}s remaining)");
        }
    }
    
    // YG2 Event Handlers
    
    private void OnInterstitialAdOpened()
    {
        if (enableDebugLogs)
        {
            Debug.Log("AdManager: Interstitial ad opened");
        }
        
        isWaitingForAd = false;
        
        // Hide warning UI if still showing
        if (adTimerWarningUI != null)
        {
            adTimerWarningUI.Hide();
        }
        
        // YG2 automatically pauses game via PauseGame()
    }
    
    private void OnInterstitialAdClosed()
    {
        if (enableDebugLogs)
        {
            Debug.Log("AdManager: Interstitial ad closed");
        }
        
        OnAdTimerStopped?.Invoke();
        
        // Reset waiting flag
        isWaitingForAd = false;
        
        // Record time when ad closed to prevent immediate re-show
        // This gives YG2 SDK time to reset its internal timer
        lastAdClosedTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log($"AdManager: Ad closed, starting cooldown period ({AD_COOLDOWN_AFTER_CLOSE}s)");
        }
        
        // Ensure warning UI is hidden and controllers are restored
        if (adTimerWarningUI != null)
        {
            adTimerWarningUI.Hide();
        }
        
        // YG2 automatically resumes game via PauseGame(false)
        // YG2 timer is reset automatically when ad closes
        // The timer will start counting again from 0
        // However, YG2 SDK might not immediately update isTimerAdvCompleted,
        // so we use a cooldown period to prevent immediate re-show
        
        // Ensure ad timer check is running if we're in workshop
        if (ShouldShowAds() && adCheckCoroutine == null)
        {
            if (enableDebugLogs)
            {
                Debug.Log("AdManager: Restarting ad timer check after ad closed");
            }
            StartAdTimerCheck();
        }
    }
    
    /// <summary>
    /// Handle pause game events from YG2 to restore controllers when pause is released
    /// </summary>
    private void OnPauseGameChanged(bool isPaused)
    {
        if (!isPaused)
        {
            // When pause is released, ensure controllers are restored
            // This handles the case when ad closes and YG2 releases pause
            if (adTimerWarningUI != null)
            {
                adTimerWarningUI.EnsureControllersRestored();
            }
            
            // Also ensure FirstPersonController and InteractionHandler are enabled
            // (in case they were disabled and not restored properly)
            RestorePlayerControllers();
        }
    }
    
    /// <summary>
    /// Restore player controllers (fallback method)
    /// </summary>
    private void RestorePlayerControllers()
    {
        // Find and enable FirstPersonController if it exists and should be enabled
        FirstPersonController fps = FindFirstObjectByType<FirstPersonController>();
        if (fps != null && !fps.enabled)
        {
            // Only enable if game is not paused and we're in gameplay
            if (!YG2.isPauseGame && LocationManager.Instance != null && 
                LocationManager.Instance.CurrentLocation == LocationManager.LocationType.Workshop)
            {
                fps.enabled = true;
            }
        }
        
        // Find and enable InteractionHandler if it exists and should be enabled
        InteractionHandler interaction = FindFirstObjectByType<InteractionHandler>();
        if (interaction != null && !interaction.enabled)
        {
            // Only enable if game is not paused
            if (!YG2.isPauseGame)
            {
                interaction.enabled = true;
            }
        }
    }
    
    private void OnRewardedAdOpened()
    {
        if (enableDebugLogs)
        {
            Debug.Log("AdManager: Rewarded ad opened");
        }
        
        // YG2 automatically pauses game via PauseGame()
    }
    
    private void OnRewardedAdClosed()
    {
        if (enableDebugLogs)
        {
            Debug.Log("AdManager: Rewarded ad closed");
        }
        
        // YG2 automatically resumes game via PauseGame(false)
        // Note: Reward callback is called separately via onRewardAdv
    }
    
    private void OnRewardReceived(string rewardId)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"AdManager: Reward received with ID: {rewardId}");
        }
        
        // Reward is handled by callback passed to RewardedAdvShow
    }
    
    /// <summary>
    /// Block ad timer (called when fullscreen UI opens)
    /// Prevents ads from showing while UI is open
    /// </summary>
    public void BlockAdTimer()
    {
        fullscreenUIBlockCount++;
        
        if (enableDebugLogs)
        {
            Debug.Log($"AdManager: Ad timer blocked (count: {fullscreenUIBlockCount})");
        }
        
        // Stop any active warning UI
        if (isWaitingForAd && adTimerWarningUI != null)
        {
            adTimerWarningUI.Hide();
            isWaitingForAd = false;
        }
    }
    
    /// <summary>
    /// Unblock ad timer (called when fullscreen UI closes)
    /// Resumes ad timer if in workshop
    /// </summary>
    public void UnblockAdTimer()
    {
        if (fullscreenUIBlockCount > 0)
        {
            fullscreenUIBlockCount--;
        }
        
        if (fullscreenUIBlockCount < 0)
        {
            fullscreenUIBlockCount = 0; // Safety
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AdManager: Ad timer unblocked (count: {fullscreenUIBlockCount})");
        }
        
        // Resume ad timer check if in workshop and no other UIs are blocking
        if (fullscreenUIBlockCount == 0 && ShouldShowAds())
        {
            if (enableDebugLogs)
            {
                Debug.Log($"AdManager: All blocks cleared, resuming ad timer check (in workshop: {ShouldShowAds()})");
            }
            
            // Ensure timer check is running
            if (adCheckCoroutine == null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("AdManager: Starting ad timer check after unblock");
                }
                StartAdTimerCheck();
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log("AdManager: Ad timer check already running, will resume checking");
                }
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log($"AdManager: Cannot resume ad timer (blockCount: {fullscreenUIBlockCount}, shouldShow: {ShouldShowAds()})");
            }
        }
    }
    
    /// <summary>
    /// Force reset block count (for debugging/fixing stuck states)
    /// </summary>
    public void ForceResetBlockCount()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"AdManager: Force resetting block count from {fullscreenUIBlockCount} to 0");
        }
        fullscreenUIBlockCount = 0;
        
        // Resume ad timer check if in workshop
        if (ShouldShowAds())
        {
            if (adCheckCoroutine == null)
            {
                StartAdTimerCheck();
            }
        }
    }
    
    /// <summary>
    /// Check if ad timer is currently blocked by fullscreen UI
    /// </summary>
    public bool IsAdTimerBlocked => fullscreenUIBlockCount > 0;
}
