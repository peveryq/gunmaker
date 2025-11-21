using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
#if DOTWEEN_ENABLED
using DG.Tweening;
#endif

/// <summary>
/// Mobile button component with expandable hit area and visual feedback.
/// Supports both tap and hold interactions.
/// </summary>
public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Button Settings")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private bool supportHold = false; // If true, button can be held down
    
    [Header("Hit Area")]
    [Tooltip("Multiplier for hit area size. 1.0 = same as visual size, 1.5 = 50% larger hit area")]
    [SerializeField] private float hitAreaMultiplier = 1.2f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private float pressAnimationDuration = 0.1f;
    
    [Header("Scale Animation")]
    [SerializeField] private bool enableScaleAnimation = true;
    [SerializeField] private float pressedScale = 0.9f;
    
    private RectTransform rectTransform;
    private Vector2 originalHitAreaSize;
    private Vector3 originalScale;
    private bool isPressed = false;
    private bool isEnabled = true;
    private bool isPointerInside = false;
    
    // Events
    public event Action OnPressed;
    public event Action OnReleased;
    public event Action OnClicked; // For tap interactions
    
    // Properties
    public bool IsPressed => isPressed && isEnabled;
    public bool IsEnabled => isEnabled;
    public bool SupportHold 
    { 
        get => supportHold; 
        set => supportHold = value; 
    }
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        
        // Setup hit area
        SetupHitArea();
        
        // Initialize colors
        UpdateVisualState();
    }
    
    private void SetupHitArea()
    {
        // Store original size
        originalHitAreaSize = rectTransform.sizeDelta;
        
        // Expand hit area if multiplier > 1
        if (hitAreaMultiplier > 1f)
        {
            Vector2 expandedSize = originalHitAreaSize * hitAreaMultiplier;
            rectTransform.sizeDelta = expandedSize;
            
            // If we have a button image, keep its size unchanged by adjusting its RectTransform
            if (buttonImage != null && buttonImage.rectTransform != rectTransform)
            {
                // Keep button visual size the same
                buttonImage.rectTransform.sizeDelta = originalHitAreaSize;
            }
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isEnabled) return;
        
        isPressed = true;
        isPointerInside = true;
        
        UpdateVisualState();
        OnPressed?.Invoke();
        
        // For tap buttons, also trigger click immediately
        if (!supportHold)
        {
            OnClicked?.Invoke();
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isEnabled) return;
        
        bool wasPressed = isPressed;
        isPressed = false;
        
        UpdateVisualState();
        
        if (wasPressed)
        {
            OnReleased?.Invoke();
            
            // For hold buttons, trigger click on release if pointer is still inside
            if (supportHold && isPointerInside)
            {
                OnClicked?.Invoke();
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        
        // If we're holding and pointer exits, release the button
        if (isPressed && supportHold)
        {
            isPressed = false;
            UpdateVisualState();
            OnReleased?.Invoke();
        }
    }
    
    private void UpdateVisualState()
    {
        Color targetColor;
        float targetScale;
        
        if (!isEnabled)
        {
            targetColor = disabledColor;
            targetScale = 1f;
        }
        else if (isPressed)
        {
            targetColor = pressedColor;
            targetScale = enableScaleAnimation ? pressedScale : 1f;
        }
        else
        {
            targetColor = normalColor;
            targetScale = 1f;
        }
        
        // Apply color to button image
        if (buttonImage != null)
        {
            if (pressAnimationDuration > 0f)
            {
                // Smooth color transition using DOTween if available, otherwise instant
                #if DOTWEEN_ENABLED
                DG.Tweening.DOTween.Kill(buttonImage);
                buttonImage.DOColor(targetColor, pressAnimationDuration);
                #else
                buttonImage.color = targetColor;
                #endif
            }
            else
            {
                buttonImage.color = targetColor;
            }
        }
        
        // Apply scale animation
        if (enableScaleAnimation)
        {
            Vector3 targetScaleVector = originalScale * targetScale;
            
            if (pressAnimationDuration > 0f)
            {
                #if DOTWEEN_ENABLED
                DG.Tweening.DOTween.Kill(transform);
                transform.DOScale(targetScaleVector, pressAnimationDuration);
                #else
                transform.localScale = targetScaleVector;
                #endif
            }
            else
            {
                transform.localScale = targetScaleVector;
            }
        }
    }
    
    /// <summary>
    /// Enable or disable the button
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        
        if (!enabled && isPressed)
        {
            isPressed = false;
            OnReleased?.Invoke();
        }
        
        UpdateVisualState();
    }
    
    /// <summary>
    /// Set the button icon
    /// </summary>
    public void SetIcon(Sprite icon)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
        }
    }
    
    /// <summary>
    /// Set button visibility
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// Force release the button (useful for external state changes)
    /// </summary>
    public void ForceRelease()
    {
        if (isPressed)
        {
            isPressed = false;
            UpdateVisualState();
            OnReleased?.Invoke();
        }
    }
}
