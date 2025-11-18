using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Universal fullscreen fade screen system with configurable fade speed.
/// Can be reused for multiple fade scenarios (fade in/out).
/// </summary>
public class FadeScreen : MonoBehaviour
{
    [Header("Fade Image")]
    [SerializeField] private Image fadeImage;
    
    private Tween currentFadeTween;
    
    private void Awake()
    {
        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
        }
        
        if (fadeImage == null)
        {
            Debug.LogError("FadeScreen: Image component not found!");
            enabled = false;
            return;
        }
        
        // Ensure image is fullscreen and starts transparent
        if (fadeImage.color.a > 0f)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }
    
    /// <summary>
    /// Fade in (from transparent to opaque). Speed is duration in seconds.
    /// </summary>
    public void FadeIn(float speed)
    {
        if (fadeImage == null) return;
        
        // Kill any existing tween
        if (currentFadeTween != null && currentFadeTween.IsActive())
        {
            currentFadeTween.Kill();
        }
        
        // Ensure image is active
        if (!fadeImage.gameObject.activeSelf)
        {
            fadeImage.gameObject.SetActive(true);
        }
        
        Color startColor = fadeImage.color;
        startColor.a = 0f;
        fadeImage.color = startColor;
        
        Color endColor = startColor;
        endColor.a = 1f;
        
        currentFadeTween = fadeImage.DOColor(endColor, speed)
            .SetEase(Ease.Linear)
            .OnComplete(() => currentFadeTween = null);
    }
    
    /// <summary>
    /// Fade out (from opaque to transparent). Speed is duration in seconds.
    /// </summary>
    public void FadeOut(float speed)
    {
        if (fadeImage == null) return;
        
        // Kill any existing tween
        if (currentFadeTween != null && currentFadeTween.IsActive())
        {
            currentFadeTween.Kill();
        }
        
        // Ensure image is active
        if (!fadeImage.gameObject.activeSelf)
        {
            fadeImage.gameObject.SetActive(true);
        }
        
        Color startColor = fadeImage.color;
        startColor.a = 1f;
        fadeImage.color = startColor;
        
        Color endColor = startColor;
        endColor.a = 0f;
        
        currentFadeTween = fadeImage.DOColor(endColor, speed)
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                currentFadeTween = null;
                // Optionally hide image when fully transparent
                if (fadeImage.color.a <= 0.01f)
                {
                    fadeImage.gameObject.SetActive(false);
                }
            });
    }
    
    /// <summary>
    /// Instantly set fade state (0 = transparent, 1 = opaque)
    /// </summary>
    public void SetFade(float alpha)
    {
        if (fadeImage == null) return;
        
        // Kill any existing tween
        if (currentFadeTween != null && currentFadeTween.IsActive())
        {
            currentFadeTween.Kill();
            currentFadeTween = null;
        }
        
        Color c = fadeImage.color;
        c.a = Mathf.Clamp01(alpha);
        fadeImage.color = c;
        
        fadeImage.gameObject.SetActive(c.a > 0.01f);
    }
    
    /// <summary>
    /// Check if fade is currently in progress
    /// </summary>
    public bool IsFading => currentFadeTween != null && currentFadeTween.IsActive();
    
    private void OnDestroy()
    {
        if (currentFadeTween != null && currentFadeTween.IsActive())
        {
            currentFadeTween.Kill();
        }
    }
}

