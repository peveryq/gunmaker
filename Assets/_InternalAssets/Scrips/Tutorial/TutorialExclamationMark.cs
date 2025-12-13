using UnityEngine;
using DG.Tweening;

/// <summary>
/// 3D exclamation mark that animates up and down to indicate quest locations
/// </summary>
public class TutorialExclamationMark : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDistance = 0.3f; // Distance to move up/down
    [SerializeField] private float animationDuration = 1f; // Duration of one cycle (up + down)
    [SerializeField] private Ease animationEase = Ease.InOutSine;
    
    private Vector3 originalPosition;
    private Tween animationTween;
    private bool isVisible = false;
    
    private void Awake()
    {
        originalPosition = transform.localPosition;
    }
    
    private void OnEnable()
    {
        // If already visible, restart animation
        if (isVisible)
        {
            StartAnimation();
        }
    }
    
    private void OnDisable()
    {
        StopAnimation();
    }
    
    /// <summary>
    /// Show the exclamation mark and start animation
    /// </summary>
    public void Show()
    {
        if (isVisible) return;
        
        isVisible = true;
        gameObject.SetActive(true);
        StartAnimation();
    }
    
    /// <summary>
    /// Hide the exclamation mark and stop animation
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        StopAnimation();
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Start the up/down animation
    /// </summary>
    private void StartAnimation()
    {
        StopAnimation();
        
        // Reset to original position
        transform.localPosition = originalPosition;
        
        // Create looping animation: move up, then down, repeat
        Vector3 upPosition = originalPosition + Vector3.up * animationDistance;
        
        // Create a sequence that moves up then down, and loops infinitely
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOLocalMoveY(upPosition.y, animationDuration * 0.5f).SetEase(animationEase));
        sequence.Append(transform.DOLocalMoveY(originalPosition.y, animationDuration * 0.5f).SetEase(animationEase));
        sequence.SetLoops(-1, LoopType.Restart);
        
        animationTween = sequence;
    }
    
    /// <summary>
    /// Stop the animation
    /// </summary>
    private void StopAnimation()
    {
        if (animationTween != null && animationTween.IsActive())
        {
            animationTween.Kill();
            animationTween = null;
        }
        
        // Reset to original position
        transform.localPosition = originalPosition;
    }
    
    private void OnDestroy()
    {
        StopAnimation();
    }
}

