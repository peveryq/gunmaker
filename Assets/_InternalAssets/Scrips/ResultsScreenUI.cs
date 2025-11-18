using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Results screen UI showing earnings and options to continue or get x2 reward.
/// </summary>
public class ResultsScreenUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    
    [Header("Main Panel")]
    [SerializeField] private GameObject resultsPanel;
    
    [Header("Header")]
    [SerializeField] private GameObject timeIsUpHeader;
    [SerializeField] private GameObject timeIsUpBackground;
    
    [Header("Earnings Display")]
    [SerializeField] private TextMeshProUGUI earningsLabel;
    [SerializeField] private TextMeshProUGUI dollarSignText;
    [SerializeField] private TextMeshProUGUI earningsAmountText;
    [SerializeField] private RectTransform earningsContainer; // Parent with HorizontalLayoutGroup
    [SerializeField] private HorizontalLayoutGroup earningsLayoutGroup; // HorizontalLayoutGroup on earningsContainer
    
    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button getX2Button;
    [SerializeField] private DOTweenAnimation getX2HighlightAnimation;
    [SerializeField] private float highlightAnimationDelay = 0.5f;
    
    [Header("References")]
    [SerializeField] private LocationManager locationManager;
    [SerializeField] private FadeScreen fadeScreen;
    [SerializeField] private FirstPersonController firstPersonController;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeOutSpeed = 0.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    
    private bool hudVisibilityCaptured;
    private bool wasControllerEnabled;
    private int currentEarnings;
    private Coroutine highlightAnimationCoroutine;
    private Vector3 highlightInitialPosition;
    private RectTransform highlightRectTransform;
    
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
        
        // Get root if not assigned
        if (root == null)
        {
            root = gameObject;
        }
        
        // Setup button listeners
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }
        
        if (getX2Button != null)
        {
            getX2Button.onClick.AddListener(OnGetX2Clicked);
        }
        
        // Save initial position of highlight for reset
        if (getX2HighlightAnimation != null && getX2HighlightAnimation.target != null)
        {
            // Get RectTransform for UI elements
            highlightRectTransform = getX2HighlightAnimation.target.GetComponent<RectTransform>();
            if (highlightRectTransform == null)
            {
                // Fallback to Transform if not UI element
                Transform highlightTransform = getX2HighlightAnimation.target.GetComponent<Transform>();
                if (highlightTransform != null)
                {
                    highlightInitialPosition = highlightTransform.localPosition;
                }
            }
            else
            {
                highlightInitialPosition = highlightRectTransform.localPosition;
            }
        }
        
        // Hide root and panel initially
        if (root != null)
        {
            root.SetActive(false);
        }
        
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Close on ESC
        if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
        {
            OnNextClicked();
        }
    }
    
    /// <summary>
    /// Show results screen with earnings
    /// </summary>
    public void ShowResults(int earnings)
    {
        currentEarnings = earnings;
        
        // Show root first
        if (root != null)
        {
            root.SetActive(true);
        }
        
        // Show panel
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
        }
        
        // Show header
        if (timeIsUpHeader != null)
        {
            timeIsUpHeader.SetActive(true);
        }
        
        if (timeIsUpBackground != null)
        {
            timeIsUpBackground.SetActive(true);
        }
        
        // Update earnings display
        UpdateEarningsDisplay(earnings);
        
        // Request HUD hidden
        if (!hudVisibilityCaptured && GameplayUIContext.HasInstance)
        {
            GameplayUIContext.Instance.RequestHudHidden(this);
            hudVisibilityCaptured = true;
        }
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable FirstPersonController
        if (firstPersonController != null)
        {
            wasControllerEnabled = firstPersonController.enabled;
            firstPersonController.enabled = false;
        }
        
        // Start highlight animation loop
        StartHighlightAnimation();
    }
    
    private void UpdateEarningsDisplay(int earnings)
    {
        // Update dollar sign (separate text element)
        if (dollarSignText != null)
        {
            dollarSignText.text = "$";
        }
        
        // Update amount (separate text element with different color/size)
        if (earningsAmountText != null)
        {
            earningsAmountText.text = earnings.ToString("n0");
        }
        
        // Force HorizontalLayoutGroup to recalculate positions
        // Use coroutine to update on next frame to ensure TextMeshPro has recalculated preferred width
        StartCoroutine(ForceLayoutGroupUpdate());
    }
    
    private IEnumerator ForceLayoutGroupUpdate()
    {
        // Wait for end of frame to ensure TextMeshPro has recalculated text size
        yield return null;
        
        // Force TextMeshPro to recalculate preferred width
        if (earningsAmountText != null)
        {
            earningsAmountText.ForceMeshUpdate();
        }
        
        // Force canvas update
        Canvas.ForceUpdateCanvases();
        
        // Force HorizontalLayoutGroup to recalculate positions
        if (earningsLayoutGroup != null)
        {
            // Temporarily disable and re-enable to force recalculation
            earningsLayoutGroup.enabled = false;
            yield return null;
            earningsLayoutGroup.enabled = true;
            
            // Also explicitly call layout methods
            earningsLayoutGroup.SetLayoutHorizontal();
            earningsLayoutGroup.SetLayoutVertical();
        }
        
        // Force layout rebuild
        if (earningsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(earningsContainer);
        }
        
        Canvas.ForceUpdateCanvases();
    }
    
    private void OnNextClicked()
    {
        PlayButtonSound();
        
        // Close results screen
        CloseResults();
        
        // Return to workshop
        ReturnToWorkshop();
    }
    
    private void OnGetX2Clicked()
    {
        PlayButtonSound();
        
        // Add earnings again (future: show ad, then reward)
        if (MoneySystem.Instance != null && currentEarnings > 0)
        {
            MoneySystem.Instance.AddMoney(currentEarnings);
        }
        
        // Close results screen
        CloseResults();
        
        // Return to workshop
        ReturnToWorkshop();
    }
    
    private void CloseResults()
    {
        // Stop highlight animation
        StopHighlightAnimation();
        
        // Hide panel
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }
        
        // Hide root
        if (root != null)
        {
            root.SetActive(false);
        }
        
        // Release HUD
        if (hudVisibilityCaptured && GameplayUIContext.HasInstance)
        {
            GameplayUIContext.Instance.ReleaseHud(this);
            hudVisibilityCaptured = false;
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable FirstPersonController
        if (firstPersonController != null && wasControllerEnabled)
        {
            firstPersonController.enabled = true;
        }
    }
    
    private void StartHighlightAnimation()
    {
        if (getX2HighlightAnimation == null) return;
        
        // Stop any existing animation
        StopHighlightAnimation();
        
        // Start animation loop
        highlightAnimationCoroutine = StartCoroutine(HighlightAnimationLoop());
    }
    
    private void StopHighlightAnimation()
    {
        if (highlightAnimationCoroutine != null)
        {
            StopCoroutine(highlightAnimationCoroutine);
            highlightAnimationCoroutine = null;
        }
        
        // Kill any active tween
        if (getX2HighlightAnimation != null)
        {
            getX2HighlightAnimation.DOKill();
        }
        
        // Reset position to initial
        if (highlightRectTransform != null)
        {
            highlightRectTransform.localPosition = highlightInitialPosition;
        }
        else if (getX2HighlightAnimation != null && getX2HighlightAnimation.target != null)
        {
            Transform highlightTransform = getX2HighlightAnimation.target.GetComponent<Transform>();
            if (highlightTransform != null)
            {
                highlightTransform.localPosition = highlightInitialPosition;
            }
        }
    }
    
    private IEnumerator HighlightAnimationLoop()
    {
        if (getX2HighlightAnimation == null || getX2HighlightAnimation.target == null) yield break;
        
        // Ensure we have the transform reference
        if (highlightRectTransform == null && getX2HighlightAnimation.target != null)
        {
            highlightRectTransform = getX2HighlightAnimation.target.GetComponent<RectTransform>();
        }
        
        while (IsOpen)
        {
            // Reset position to initial before each animation cycle
            if (highlightRectTransform != null)
            {
                highlightRectTransform.localPosition = highlightInitialPosition;
            }
            else if (getX2HighlightAnimation.target != null)
            {
                Transform highlightTransform = getX2HighlightAnimation.target.GetComponent<Transform>();
                if (highlightTransform != null)
                {
                    highlightTransform.localPosition = highlightInitialPosition;
                }
            }
            
            // Ensure tween is created
            getX2HighlightAnimation.CreateTween(false, false);
            
            // Restart animation
            getX2HighlightAnimation.DORestart();
            
            // Wait for animation to complete
            if (getX2HighlightAnimation.tween != null && getX2HighlightAnimation.tween.IsActive())
            {
                yield return getX2HighlightAnimation.tween.WaitForCompletion();
            }
            else
            {
                // Fallback: wait a bit if tween is not available
                yield return new WaitForSeconds(1f);
            }
            
            // Wait for delay between cycles
            if (highlightAnimationDelay > 0f)
            {
                yield return new WaitForSeconds(highlightAnimationDelay);
            }
        }
        
        highlightAnimationCoroutine = null;
    }
    
    private void ReturnToWorkshop()
    {
        // Fade out
        if (fadeScreen != null)
        {
            fadeScreen.FadeIn(fadeOutSpeed);
        }
        
        // Transition to workshop
        if (locationManager != null)
        {
            locationManager.TransitionToLocation(LocationManager.LocationType.Workshop);
        }
        
        // Fade in after transition
        if (fadeScreen != null)
        {
            // Fade in will be handled by LocationManager after transition
        }
    }
    
    private void PlayButtonSound()
    {
        if (buttonClickSound == null) return;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSound, volume: 0.8f);
        }
    }
    
    /// <summary>
    /// Check if results screen is open
    /// </summary>
    public bool IsOpen => resultsPanel != null && resultsPanel.activeSelf;
    
    private void OnDestroy()
    {
        if (hudVisibilityCaptured && GameplayUIContext.HasInstance)
        {
            GameplayUIContext.Instance.ReleaseHud(this);
        }
    }
}

