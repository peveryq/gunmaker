using System;
using System.Collections;
using UnityEngine;
using TMPro;
using YG;

/// <summary>
/// Fullscreen UI panel that shows countdown timer (3-2-1) before showing ad.
/// Pauses game and shows countdown, then calls callback to show ad.
/// Uses YG2.PauseGame() for proper game pause.
/// </summary>
public class AdTimerWarningUI : MonoBehaviour
{
    [System.Serializable]
    private class AdTimerLabels
    {
        [Tooltip("Localization key for 'ad starts in' message. Default: 'ad.starts_in'")]
        public string startsInKey = "ad.starts_in";
        
        [Header("Fallback Label (Optional)")]
        [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
        public string startsIn = "";
    }
    
    [Header("UI References")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private TextMeshProUGUI countdownLabelText; // Text for "ad starts in"
    [SerializeField] private TextMeshProUGUI countdownNumberText; // Text for number (3, 2, 1)
    
    [Header("Countdown Settings")]
    [Tooltip("Total countdown time in seconds (default: 3). Can be overridden by AdManager.")]
    [SerializeField] private float countdownDuration = 3f; // Total countdown time (3 seconds)
    
    [Header("Animation")]
    [SerializeField] private float textScaleAnimation = 1.2f;
    [SerializeField] private float animationDuration = 0.3f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip countdownSound;
    [SerializeField] private AudioClip finalCountdownSound;
    
    [Header("Localization")]
    [SerializeField] private AdTimerLabels labels = new AdTimerLabels();
    
    private bool isShowing = false;
    private Coroutine countdownCoroutine;
    private Action onCountdownComplete;
    
    // Store controller states for restoration
    private FirstPersonController fpsController;
    private bool wasFpsControllerEnabled;
    private bool controllersWereDisabled = false; // Track if we disabled controllers
    private InteractionHandler interactionHandler;
    private bool wasInteractionHandlerEnabled;
    
    private void Awake()
    {
        // Hide initially
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
        
        if (countdownLabelText != null)
        {
            countdownLabelText.text = "";
        }
        
        if (countdownNumberText != null)
        {
            countdownNumberText.text = "";
        }
    }
    
    /// <summary>
    /// Show countdown timer and pause game
    /// </summary>
    public void Show(Action onComplete)
    {
        Show(countdownDuration, onComplete);
    }
    
    /// <summary>
    /// Show countdown timer with custom duration
    /// </summary>
    public void Show(float duration, Action onComplete)
    {
        if (isShowing)
        {
            Debug.LogWarning("AdTimerWarningUI: Already showing, ignoring show request");
            return;
        }
        
        onCountdownComplete = onComplete;
        isShowing = true;
        countdownDuration = duration; // Update duration
        
        // Show panel
        if (rootPanel != null)
        {
            rootPanel.SetActive(true);
        }
        
        // Pause game using YG2.PauseGame() for proper pause (time, audio, cursor, event system)
        // This ensures complete game pause including camera control
        YG2.PauseGame(true);
        
        // Disable FirstPersonController to prevent any input processing
        DisablePlayerControllers();
        
        // Start countdown
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }
    
    /// <summary>
    /// Hide countdown timer and resume game
    /// </summary>
    public void Hide()
    {
        if (!isShowing)
        {
            return;
        }
        
        isShowing = false;
        
        // Stop countdown
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        
        // Hide panel
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
        
        if (countdownLabelText != null)
        {
            countdownLabelText.text = "";
        }
        
        if (countdownNumberText != null)
        {
            countdownNumberText.text = "";
        }
        
        // Re-enable player controllers
        EnablePlayerControllers();
        
        // Resume game only if ad is not showing (YG2 will handle resume when ad closes)
        // If ad is showing, YG2 will handle resume automatically
        if (!YG2.isSDKEnabled || !YG2.nowAdsShow)
        {
            YG2.PauseGame(false);
        }
        // If ad is still showing, YG2 will handle pause/resume
        // Controllers will be restored via EnsureControllersRestored() when pause is released
    }
    
    /// <summary>
    /// Disable player controllers to prevent any input during warning
    /// </summary>
    private void DisablePlayerControllers()
    {
        controllersWereDisabled = true;
        
        // Disable FirstPersonController
        if (fpsController == null)
        {
            fpsController = FindFirstObjectByType<FirstPersonController>();
        }
        
        if (fpsController != null)
        {
            wasFpsControllerEnabled = fpsController.enabled;
            if (wasFpsControllerEnabled)
            {
                fpsController.enabled = false;
            }
        }
        
        // Disable InteractionHandler
        if (interactionHandler == null)
        {
            interactionHandler = FindFirstObjectByType<InteractionHandler>();
        }
        
        if (interactionHandler != null)
        {
            wasInteractionHandlerEnabled = interactionHandler.enabled;
            if (wasInteractionHandlerEnabled)
            {
                interactionHandler.enabled = false;
            }
        }
    }
    
    /// <summary>
    /// Re-enable player controllers after warning
    /// </summary>
    private void EnablePlayerControllers()
    {
        if (!controllersWereDisabled)
        {
            return; // Controllers weren't disabled by us, don't restore
        }
        
        controllersWereDisabled = false;
        
        // Re-enable FirstPersonController
        if (fpsController == null)
        {
            fpsController = FindFirstObjectByType<FirstPersonController>();
        }
        
        if (fpsController != null && wasFpsControllerEnabled)
        {
            fpsController.enabled = true;
        }
        
        // Re-enable InteractionHandler
        if (interactionHandler == null)
        {
            interactionHandler = FindFirstObjectByType<InteractionHandler>();
        }
        
        if (interactionHandler != null && wasInteractionHandlerEnabled)
        {
            interactionHandler.enabled = true;
        }
    }
    
    /// <summary>
    /// Public method to ensure controllers are restored (called from AdManager when pause is released)
    /// </summary>
    public void EnsureControllersRestored()
    {
        // Always restore controllers if they were disabled, regardless of isShowing state
        // This ensures controllers are restored even if ad closes before Hide() is called
        if (controllersWereDisabled)
        {
            EnablePlayerControllers();
        }
    }
    
    /// <summary>
    /// Countdown coroutine (3-2-1)
    /// </summary>
    private IEnumerator CountdownCoroutine()
    {
        int countdownValue = Mathf.CeilToInt(countdownDuration);
        
        // Update label text with localized "ad starts in"
        if (countdownLabelText != null)
        {
            string labelText = GetLocalizedLabel(labels.startsInKey, labels.startsIn, "ad starts in");
            countdownLabelText.text = labelText;
        }
        
        for (int i = countdownValue; i > 0; i--)
        {
            // Update countdown number
            if (countdownNumberText != null)
            {
                countdownNumberText.text = i.ToString();
                
                // Animate number text scale
                StartCoroutine(AnimateTextScale());
            }
            
            // Play sound
            if (i == 1 && finalCountdownSound != null)
            {
                // Final countdown sound
                PlaySound(finalCountdownSound);
            }
            else if (countdownSound != null)
            {
                // Regular countdown sound
                PlaySound(countdownSound);
            }
            
            // Wait for 1 second (using unscaled time since game is paused)
            yield return new WaitForSecondsRealtime(1f);
        }
        
        // Countdown complete - clear texts
        if (countdownLabelText != null)
        {
            countdownLabelText.text = "";
        }
        
        if (countdownNumberText != null)
        {
            countdownNumberText.text = "";
        }
        
        // Hide panel
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
        
        isShowing = false;
        countdownCoroutine = null;
        
        // Call callback to show ad (YG2 will handle pause for ad)
        onCountdownComplete?.Invoke();
        onCountdownComplete = null;
    }
    
    /// <summary>
    /// Animate text scale for visual feedback (animates number text only)
    /// </summary>
    private IEnumerator AnimateTextScale()
    {
        if (countdownNumberText == null) yield break;
        
        Vector3 originalScale = countdownNumberText.transform.localScale;
        Vector3 targetScale = originalScale * textScaleAnimation;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            
            // Scale up then down
            if (t < 0.5f)
            {
                countdownNumberText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t * 2f);
            }
            else
            {
                countdownNumberText.transform.localScale = Vector3.Lerp(targetScale, originalScale, (t - 0.5f) * 2f);
            }
            
            yield return null;
        }
        
        countdownNumberText.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// Play sound effect
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, volume: 0.8f);
        }
        else
        {
            // Fallback: play at point
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, 0.8f);
        }
    }
    
    /// <summary>
    /// Check if countdown is currently showing
    /// </summary>
    public bool IsShowing => isShowing;
    
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
