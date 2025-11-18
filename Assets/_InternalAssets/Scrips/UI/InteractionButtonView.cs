using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionButtonView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private GameObject keyHintRoot;
    [SerializeField] private Button button;
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
    }

    private void OnEnable()
    {
        ForceLayoutUpdate();
    }

    public void Configure(InteractionOption option, InteractionHandler handler)
    {
        currentOption = option;
        boundHandler = handler;

        if (labelText != null)
        {
            labelText.text = option.Label;
        }

        if (keyText != null)
        {
            keyText.text = option.Key == KeyCode.None ? string.Empty : option.Key.ToString().ToUpperInvariant();
        }

        // Show/hide key hint root based on availability and key presence
        if (keyHintRoot != null)
        {
            bool showKeyHint = option.IsAvailable && option.Key != KeyCode.None;
            keyHintRoot.SetActive(showKeyHint);
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
            button.onClick.AddListener(HandleClicked);
            button.interactable = option.IsAvailable;
        }

        ApplyStyle(option.Style);
        ForceLayoutUpdate();
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
}
