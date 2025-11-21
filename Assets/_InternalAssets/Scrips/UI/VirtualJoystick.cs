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
    [SerializeField] private float hitAreaMultiplier = 1.2f;
    
    private Vector2 inputVector;
    private bool isDragging = false;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera canvasCamera;
    private CanvasGroup canvasGroup;
    private Vector2 knobStartPosition;
    
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
        
        // Store knob's starting position
        if (knob != null)
        {
            knobStartPosition = knob.anchoredPosition;
        }
        
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
            Vector2 targetPos = knobStartPosition;
            
            if (Vector2.Distance(currentPos, targetPos) > 0.1f)
            {
                knob.anchoredPosition = Vector2.Lerp(currentPos, targetPos, returnSpeed * Time.deltaTime);
                // Don't update input vector during return animation - it's already zero
            }
            else
            {
                // Ensure knob reaches exactly center position
                knob.anchoredPosition = targetPos;
            }
        }
    }
    
    private void SetupHitArea()
    {
        if (background != null && hitAreaMultiplier > 1f)
        {
            // Expand background hit area while keeping visual size
            Vector2 originalSize = background.sizeDelta;
            Vector2 expandedSize = originalSize * hitAreaMultiplier;
            
            // Create invisible hit area
            GameObject hitArea = new GameObject("HitArea");
            hitArea.transform.SetParent(background.transform, false);
            
            RectTransform hitAreaRect = hitArea.AddComponent<RectTransform>();
            hitAreaRect.sizeDelta = expandedSize;
            hitAreaRect.anchoredPosition = Vector2.zero;
            
            // Add invisible image for hit detection
            Image hitImage = hitArea.AddComponent<Image>();
            hitImage.color = Color.clear;
            hitImage.raycastTarget = true;
            
            // Move the hit area to be the main interaction target
            // We'll handle events on this component but apply them to the joystick
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        SetVisualState(true);
        
        // Convert screen position to local position
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, canvasCamera, out localPoint))
        {
            if (snapToPointer && knob != null)
            {
                // Clamp the knob position to the handle range
                Vector2 clampedPosition = Vector2.ClampMagnitude(localPoint, handleRange);
                knob.anchoredPosition = knobStartPosition + clampedPosition;
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
        
        Debug.Log("VirtualJoystick: Finger lifted, movement stopped immediately");
        
        // Knob will return to center in Update() (visual only)
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || knob == null) return;
        
        // Convert screen position to local position
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, canvasCamera, out localPoint))
        {
            // Clamp the knob position to the handle range
            Vector2 clampedPosition = Vector2.ClampMagnitude(localPoint, handleRange);
            knob.anchoredPosition = knobStartPosition + clampedPosition;
        }
        
        UpdateInputVector();
    }
    
    private void UpdateInputVector()
    {
        if (knob == null) return;
        
        // Calculate input vector based on knob position relative to center
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
            knob.anchoredPosition = knobStartPosition;
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
