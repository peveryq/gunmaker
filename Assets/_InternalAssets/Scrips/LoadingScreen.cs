using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Loading screen system with progress bar and fake minimum wait time.
/// Universal system for game entry and location transitions.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    
    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    
    [Header("Progress Bar")]
    [SerializeField] private Image progressBarBackground;
    [SerializeField] private Image progressBarFill;
    
    [Header("Loading Icon")]
    [SerializeField] private GameObject loadingIcon;
    
    [Header("Settings")]
    [Tooltip("Minimum time to show loading screen (fake wait time). If actual load is faster, use this time. If actual load is slower, use actual time.")]
    [SerializeField] private float fakeMinimumWaitTime = 2f;
    
    [Tooltip("Objects that must be active for loading to complete")]
    [SerializeField] private GameObject[] requiredObjects;
    
    private bool isLoading = false;
    private float loadStartTime;
    private Coroutine loadingCoroutine;
    private Tween fadeTween;
    
    private void Awake()
    {
        // Get root if not assigned
        if (root == null)
        {
            root = gameObject;
        }
        
        // Ensure progress bar fill is set up correctly
        if (progressBarFill != null)
        {
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillAmount = 0f;
        }
        
        // Hide by default
        if (root != null)
        {
            root.SetActive(false);
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Start loading screen. Returns coroutine that can be awaited.
    /// </summary>
    public Coroutine StartLoading(System.Action onComplete = null)
    {
        if (isLoading)
        {
            Debug.LogWarning("LoadingScreen: Already loading!");
            return null;
        }
        
        // Stop any existing loading coroutine
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        
        isLoading = true;
        loadStartTime = Time.realtimeSinceStartup;
        
        // Show loading screen root immediately
        if (root != null)
        {
            root.SetActive(true);
        }
        
        // Show loading screen immediately
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
            // Set to fully opaque
            Color c = backgroundImage.color;
            c.a = 1f;
            backgroundImage.color = c;
        }
        
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = 0f;
        }
        
        if (loadingIcon != null)
        {
            loadingIcon.SetActive(true);
        }
        
        loadingCoroutine = StartCoroutine(LoadingRoutine(onComplete));
        return loadingCoroutine;
    }
    
    private IEnumerator LoadingRoutine(System.Action onComplete)
    {
        float elapsedTime = 0f;
        bool allObjectsReady = false;
        
        while (true)
        {
            elapsedTime = Time.realtimeSinceStartup - loadStartTime;
            
            // Check if all required objects are active
            allObjectsReady = CheckRequiredObjects();
            
            float progress = 0f;
            
            // Progress logic:
            // 1. If elapsedTime < fakeMinimumWaitTime: progress = elapsedTime / fakeMinimumWaitTime * 0.8 (0-80%)
            // 2. If elapsedTime >= fakeMinimumWaitTime but objects not ready: progress = 0.8 (80%)
            // 3. When objects ready: progress = 1.0 (100%)
            
            if (elapsedTime < fakeMinimumWaitTime)
            {
                // Progress from 0 to 80% during fake minimum wait time
                progress = Mathf.Clamp01(elapsedTime / fakeMinimumWaitTime) * 0.8f;
            }
            else
            {
                // We've reached minimum wait time
                if (allObjectsReady)
                {
                    // Objects are ready, go to 100%
                    progress = 1f;
                }
                else
                {
                    // Objects not ready yet, stay at 80%
                    progress = 0.8f;
                }
            }
            
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = progress;
            }
            
            // If we've waited at least the minimum time AND all objects are ready
            if (elapsedTime >= fakeMinimumWaitTime && allObjectsReady)
            {
                // Ensure progress is at 100%
                if (progressBarFill != null)
                {
                    progressBarFill.fillAmount = 1f;
                }
                
                // Wait a tiny bit to show 100%
                yield return new WaitForSeconds(0.1f);
                
                break;
            }
            
            yield return null;
        }
        
        isLoading = false;
        loadingCoroutine = null;
        
        onComplete?.Invoke();
    }
    
    private bool CheckRequiredObjects()
    {
        if (requiredObjects == null || requiredObjects.Length == 0)
        {
            // If no required objects specified, assume ready after a short delay
            return Time.realtimeSinceStartup - loadStartTime > 0.1f;
        }
        
        foreach (GameObject obj in requiredObjects)
        {
            if (obj == null || !obj.activeInHierarchy)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Fade out loading screen. Speed is duration in seconds.
    /// </summary>
    public void FadeOut(float speed, System.Action onComplete = null)
    {
        if (backgroundImage == null) return;
        
        // Kill any existing fade
        if (fadeTween != null && fadeTween.IsActive())
        {
            fadeTween.Kill();
        }
        
        fadeTween = backgroundImage.DOFade(0f, speed)
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                fadeTween = null;
                if (backgroundImage != null)
                {
                    backgroundImage.gameObject.SetActive(false);
                }
                // Hide root when fade completes
                if (root != null)
                {
                    root.SetActive(false);
                }
                onComplete?.Invoke();
            });
    }
    
    /// <summary>
    /// Stop loading immediately (for emergency cleanup)
    /// </summary>
    public void StopLoading()
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        
        isLoading = false;
        
        // Hide root
        if (root != null)
        {
            root.SetActive(false);
        }
        
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(false);
        }
        
        if (fadeTween != null && fadeTween.IsActive())
        {
            fadeTween.Kill();
            fadeTween = null;
        }
    }
    
    /// <summary>
    /// Check if currently loading
    /// </summary>
    public bool IsLoading => isLoading;
    
    private void OnDestroy()
    {
        if (fadeTween != null && fadeTween.IsActive())
        {
            fadeTween.Kill();
        }
    }
}

