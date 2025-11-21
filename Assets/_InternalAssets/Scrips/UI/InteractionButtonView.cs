using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InteractionButtonView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private GameObject keyHintRoot;
    [SerializeField] private Button button;
    // MobileButton removed - using built-in hold logic instead
    [SerializeField] private Image background;
    [SerializeField] private RectTransform contentRow;

    [Header("Primary Style")]
    [SerializeField] private Color primaryBackgroundColor = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color primaryLabelColor = Color.white;
    [SerializeField] private Color primaryKeyColor = Color.white;

    [Header("Secondary Style")]
    [SerializeField] private Color secondaryBackgroundColor = Color.gray;
    [SerializeField] private Color secondaryLabelColor = Color.white;
    [SerializeField] private Color secondaryKeyColor = Color.white;

    private InteractionOption currentOption;
    private InteractionHandler boundHandler;
    private RectTransform cachedRectTransform;
    private bool isMobileDevice = false;
    
    // Hold interaction support
    private bool isHoldInteraction = false;
    private bool isCurrentlyHolding = false;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (cachedRectTransform == null)
        {
            cachedRectTransform = transform as RectTransform;
        }
        
        // Ensure we have an Image component for IPointerDownHandler to work
        Image rootImage = GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = gameObject.AddComponent<Image>();
            rootImage.color = Color.clear; // Make it invisible
            rootImage.raycastTarget = true; // But still receive events
        }
        
    }

    private void OnEnable()
    {
        ForceLayoutUpdate();
    }

    public void Configure(InteractionOption option, InteractionHandler handler)
    {
        
        currentOption = option;
        boundHandler = handler;

        // Check if we're on mobile device
        isMobileDevice = DeviceDetectionManager.Instance != null && DeviceDetectionManager.Instance.IsMobileOrTablet;

        if (labelText != null)
        {
            labelText.text = option.Label;
        }

        if (keyText != null)
        {
            keyText.text = option.Key == KeyCode.None ? string.Empty : option.Key.ToString().ToUpperInvariant();
        }

        // Show/hide key hint root based on availability, key presence, and device type
        if (keyHintRoot != null)
        {
            bool showKeyHint = option.IsAvailable && option.Key != KeyCode.None;
            
            // Hide key hints on mobile/tablet devices
            if (isMobileDevice)
            {
                showKeyHint = false;
            }
            
            keyHintRoot.SetActive(showKeyHint);
        }

        // Configure appropriate button based on device type
        ConfigureButtons(option);

        ApplyStyle(option.Style);
        ForceLayoutUpdate();
    }
    
    private void ConfigureButtons(InteractionOption option)
    {
        // Check if this is a hold interaction
        isHoldInteraction = IsHoldInteraction(option);
        
        if (button != null)
        {
            if (isHoldInteraction)
            {
                // For hold interactions, keep Button enabled but remove onClick listeners
                button.enabled = true;
                button.interactable = option.IsAvailable;
                button.onClick.RemoveAllListeners();
            }
            else
            {
                // For regular interactions, use the Button component
                ConfigureRegularButton(option);
                button.enabled = true;
                button.interactable = option.IsAvailable;
            }
        }
        
        // No mobile button to disable anymore
    }
    
    private void ConfigureRegularButton(InteractionOption option)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
            
            // For hold interactions, don't add onClick listener
            if (!isHoldInteraction)
            {
                button.onClick.AddListener(HandleClicked);
            }
            else
            {
            }
        }
    }
    
    // ConfigureMobileButton removed - using built-in hold logic instead
    
    private bool IsHoldInteraction(InteractionOption option)
    {
        // Use the RequiresHold property from InteractionOption
        return option.RequiresHold;
    }
    
    // HandleMobilePressed and HandleMobileReleased removed - using built-in hold logic instead
    
    /// <summary>
    /// Find the workbench that the player is currently looking at for welding
    /// </summary>
    private Workbench FindWorkbenchForWelding()
    {
        if (boundHandler == null) return null;
        
        // Check if current target is a workbench
        if (boundHandler.CurrentTarget is Workbench workbench)
        {
            return workbench;
        }
        
        return null;
    }

    private void ApplyStyle(InteractionOptionStyle style)
    {
        switch (style)
        {
            case InteractionOptionStyle.Primary:
                ApplyColors(primaryBackgroundColor, primaryLabelColor, primaryKeyColor);
                break;
            case InteractionOptionStyle.Secondary:
                ApplyColors(secondaryBackgroundColor, secondaryLabelColor, secondaryKeyColor);
                break;
            default:
                ApplyColors(primaryBackgroundColor, primaryLabelColor, primaryKeyColor);
                break;
        }
    }

    private void ApplyColors(Color backgroundColor, Color labelColor, Color keyColor)
    {
        if (background != null)
        {
            background.color = backgroundColor;
        }

        if (labelText != null)
        {
            labelText.color = labelColor;
        }

        if (keyText != null)
        {
            keyText.color = keyColor;
        }
    }

    private void ForceLayoutUpdate()
    {
        Canvas.ForceUpdateCanvases();

        if (contentRow != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRow);
        }

        if (cachedRectTransform == null)
        {
            cachedRectTransform = transform as RectTransform;
        }

        if (cachedRectTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(cachedRectTransform);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void HandleClicked()
    {
        if (!currentOption.IsAvailable) return;
        currentOption.Callback?.Invoke(boundHandler);
    }
    
    // IPointerDownHandler implementation
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!currentOption.IsAvailable) return;
        
        if (isHoldInteraction)
        {
            // Start hold interaction (like welding)
            isCurrentlyHolding = true;
            currentOption.Callback?.Invoke(boundHandler);
        }
    }
    
    // IPointerUpHandler implementation
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isCurrentlyHolding && isHoldInteraction)
        {
            // Stop hold interaction
            isCurrentlyHolding = false;
            StopHoldInteraction();
        }
    }
    
    // IPointerExitHandler implementation
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isCurrentlyHolding && isHoldInteraction)
        {
            // Stop hold interaction when pointer leaves the button
            isCurrentlyHolding = false;
            StopHoldInteraction();
        }
    }
    
    private void StopHoldInteraction()
    {
        // For welding, find the workbench and stop welding
        if (currentOption.Id == "workbench.weld")
        {
            var workbench = FindWorkbenchForWelding();
            if (workbench != null)
            {
                workbench.StopWeldingInteraction();
            }
        }
    }
}
