using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
#if DOTWEEN_ENABLED
using DG.Tweening;
#endif

/// <summary>
/// Virtual joystick for mobile movement input.
/// Consists of a background circle and a movable knob.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick Components")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform knob;
    
    [Header("Settings")]
    [SerializeField] private float handleRange = 50f; // Maximum distance knob can move from center
    [SerializeField] private bool snapToPointer = true; // If true, knob snaps to touch position
    [SerializeField] private float returnSpeed = 10f; // Speed at which knob returns to center
    
    [Header("Visual Feedback")]
    [SerializeField] private float activeAlpha = 1f;
    [SerializeField] private float inactiveAlpha = 0.5f;
    [SerializeField] private float fadeSpeed = 5f;
    
    [Header("Hit Area")]
    [Tooltip("Multiplier for hit area size. 1.0 = same as background size")]
    [SerializeField] private float hitAreaMultiplier = 1.5f;
    
    [Header("Floating Joystick")]
    [Tooltip("If true, joystick moves to touch position on first touch")]
    [SerializeField] private bool floatingJoystick = true;
    [Tooltip("If true, joystick returns to original position when released")]
    [SerializeField] private bool returnToOriginalPosition = true;
    
    private Vector2 inputVector;
    private bool isDragging = false;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera canvasCamera;
    private CanvasGroup canvasGroup;
    private Vector2 knobStartPosition;
    private Vector2 backgroundOriginalPosition;
    private Vector2 joystickOriginalPosition; // Original position of the joystick root
    
    // Events
    public event Action<Vector2> OnValueChanged;
    
    // Properties
    public Vector2 InputVector => inputVector;
    public bool IsDragging => isDragging;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Get camera for screen space calculations
        if (parentCanvas != null)
        {
            canvasCamera = parentCanvas.worldCamera;
            if (canvasCamera == null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvasCamera = null; // Use null for overlay canvas
            }
        }
        
        // Store original positions
        if (knob != null)
        {
            knobStartPosition = knob.anchoredPosition;
        }
        
        if (background != null)
        {
            backgroundOriginalPosition = background.anchoredPosition;
        }
        
        joystickOriginalPosition = rectTransform.anchoredPosition;
        
        // Setup hit area
        SetupHitArea();
        
        // Initialize visual state
        SetVisualState(false);
    }
    
    private void Update()
    {
        // Return knob to center when not dragging (visual only)
        if (!isDragging && knob != null)
        {
            Vector2 currentPos = knob.anchoredPosition;
            Vector2 targetPos = backgroundOriginalPosition; // Always return to center
            
            if (Vector2.Distance(currentPos, targetPos) > 0.1f)
            {
                knob.anchoredPosition = Vector2.Lerp(currentPos, targetPos, returnSpeed * Time.deltaTime);
                // Don't update input vector during return animation - it's already zero
            }
            else
            {
                // Ensure knob reaches exactly center position
                knob.anchoredPosition = targetPos;
                knobStartPosition = targetPos; // Update start position
            }
        }
    }
    
    private void SetupHitArea()
    {
        if (background != null && hitAreaMultiplier > 1f)
        {
            // Expand the main rectTransform hit area while keeping background visual size
            Vector2 originalSize = background.sizeDelta;
            Vector2 expandedSize = originalSize * hitAreaMultiplier;
            
            // Expand the main joystick rectTransform to create larger hit area
            rectTransform.sizeDelta = expandedSize;
            
            // Ensure we have an Image component for raycast detection
            Image hitImage = GetComponent<Image>();
            if (hitImage == null)
            {
                hitImage = gameObject.AddComponent<Image>();
            }
            hitImage.color = Color.clear;
            hitImage.raycastTarget = true;
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        SetVisualState(true);
        
        // Convert screen position to local position in parent canvas
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform, eventData.position, canvasCamera, out localPoint))
        {
            if (floatingJoystick)
            {
                // Move joystick root to touch position
                rectTransform.anchoredPosition = localPoint;
                
                // Reset knob to center (relative to new background position)
                if (knob != null)
                {
                    knob.anchoredPosition = backgroundOriginalPosition; // Center relative to background
                    knobStartPosition = backgroundOriginalPosition; // Update start position
                }
            }
            else
            {
                // Original behavior: convert to background local coordinates
                Vector2 backgroundLocalPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background, eventData.position, canvasCamera, out backgroundLocalPoint))
                {
                    if (snapToPointer && knob != null)
                    {
                        // Clamp the knob position to the handle range
                        Vector2 clampedPosition = Vector2.ClampMagnitude(backgroundLocalPoint, handleRange);
                        knob.anchoredPosition = knobStartPosition + clampedPosition;
                    }
                }
            }
        }
        
        UpdateInputVector();
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        SetVisualState(false);
        
        // Immediately stop movement when finger is lifted
        inputVector = Vector2.zero;
        OnValueChanged?.Invoke(inputVector);
        
        // Return joystick to original position if enabled
        if (floatingJoystick && returnToOriginalPosition)
        {
            rectTransform.anchoredPosition = joystickOriginalPosition;
        }
        
        // Reset knob to center
        if (knob != null)
        {
            knobStartPosition = backgroundOriginalPosition;
        }
        
        // Knob will return to center in Update() (visual only)
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || knob == null || background == null) return;
        
        // Convert screen position to local position relative to background
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, canvasCamera, out localPoint))
        {
            // Calculate offset from center (backgroundOriginalPosition is the center)
            Vector2 offsetFromCenter = localPoint - backgroundOriginalPosition;
            
            // Clamp the knob position to the handle range
            Vector2 clampedOffset = Vector2.ClampMagnitude(offsetFromCenter, handleRange);
            knob.anchoredPosition = knobStartPosition + clampedOffset;
        }
        
        UpdateInputVector();
    }
    
    private void UpdateInputVector()
    {
        if (knob == null) return;
        
        // Calculate input vector based on knob position relative to center
        // knobStartPosition is always the center (backgroundOriginalPosition)
        Vector2 knobOffset = knob.anchoredPosition - knobStartPosition;
        Vector2 newInputVector = knobOffset / handleRange;
        
        // Clamp to unit circle
        newInputVector = Vector2.ClampMagnitude(newInputVector, 1f);
        
        // Apply deadzone
        const float deadzone = 0.1f;
        if (newInputVector.magnitude < deadzone)
        {
            newInputVector = Vector2.zero;
        }
        
        // Update input vector and notify if changed
        if (inputVector != newInputVector)
        {
            inputVector = newInputVector;
            OnValueChanged?.Invoke(inputVector);
        }
    }
    
    private void SetVisualState(bool active)
    {
        float targetAlpha = active ? activeAlpha : inactiveAlpha;
        
        if (canvasGroup != null)
        {
            if (fadeSpeed > 0f)
            {
                #if DOTWEEN_ENABLED
                DG.Tweening.DOTween.Kill(canvasGroup);
                canvasGroup.DOFade(targetAlpha, 1f / fadeSpeed);
                #else
                canvasGroup.alpha = targetAlpha;
                #endif
            }
            else
            {
                canvasGroup.alpha = targetAlpha;
            }
        }
    }
    
    /// <summary>
    /// Set joystick visibility
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// Reset joystick to center position
    /// </summary>
    public void ResetPosition()
    {
        if (knob != null)
        {
            knob.anchoredPosition = backgroundOriginalPosition;
            knobStartPosition = backgroundOriginalPosition;
        }
        
        if (floatingJoystick && returnToOriginalPosition)
        {
            rectTransform.anchoredPosition = joystickOriginalPosition;
        }
        
        inputVector = Vector2.zero;
        isDragging = false;
        SetVisualState(false);
        OnValueChanged?.Invoke(inputVector);
    }
    
    /// <summary>
    /// Set the handle range (maximum distance knob can move)
    /// </summary>
    public void SetHandleRange(float range)
    {
        handleRange = Mathf.Max(0f, range);
    }
}
