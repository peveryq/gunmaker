using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// UI component for displaying tutorial quest text on GameplayHUD
/// Shows quest text, icon, and background with animations
/// </summary>
public class TutorialQuestUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private TextMeshProUGUI questText;
    [SerializeField] private Image questIcon;
    [SerializeField] private Image backgroundImage;
    
    [Header("Animation Settings")]
    [SerializeField] private float completeAnimationDuration = 0.5f;
    [SerializeField] private float switchAnimationDuration = 0.3f;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color completeTextColor = Color.green;
    [SerializeField] private float bubbleScale = 1.2f;
    
    private bool isAnimating = false;
    private Tween currentTween;
    
    private void Awake()
    {
        // Hide initially
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show quest text
    /// </summary>
    public void ShowQuest(string text)
    {
        if (questText != null)
        {
            questText.text = text;
            questText.color = normalTextColor;
        }
        
        if (rootPanel != null && !rootPanel.activeSelf)
        {
            rootPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide quest UI
    /// </summary>
    public void Hide()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
        
        StopAnimations();
    }
    
    /// <summary>
    /// Animate quest completion (green color + bubble effect)
    /// </summary>
    public void CompleteQuestAnimation(System.Action onComplete = null)
    {
        if (isAnimating || questText == null) return;
        
        isAnimating = true;
        StopAnimations();
        
        // Change color to green
        questText.DOColor(completeTextColor, completeAnimationDuration * 0.3f)
            .OnComplete(() =>
            {
                // Bubble effect (scale up then down)
                Vector3 originalScale = questText.transform.localScale;
                questText.transform.DOScale(originalScale * bubbleScale, completeAnimationDuration * 0.35f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        questText.transform.DOScale(originalScale, completeAnimationDuration * 0.35f)
                            .SetEase(Ease.InQuad)
                            .OnComplete(() =>
                            {
                                isAnimating = false;
                                onComplete?.Invoke();
                            });
                    });
            });
    }
    
    /// <summary>
    /// Animate quest text switch (fade out old, fade in new)
    /// </summary>
    public void SwitchQuestAnimation(string newText, System.Action onComplete = null)
    {
        if (isAnimating || questText == null) return;
        
        isAnimating = true;
        StopAnimations();
        
        // Fade out current text
        questText.DOFade(0f, switchAnimationDuration * 0.5f)
            .OnComplete(() =>
            {
                // Update text and reset color
                questText.text = newText;
                questText.color = normalTextColor;
                
                // Fade in new text
                questText.DOFade(1f, switchAnimationDuration * 0.5f)
                    .OnComplete(() =>
                    {
                        isAnimating = false;
                        onComplete?.Invoke();
                    });
            });
    }
    
    /// <summary>
    /// Stop all animations
    /// </summary>
    private void StopAnimations()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
        
        if (questText != null)
        {
            questText.DOKill();
            questText.transform.DOKill();
        }
        
        isAnimating = false;
    }
    
    private void OnDestroy()
    {
        StopAnimations();
    }
}

