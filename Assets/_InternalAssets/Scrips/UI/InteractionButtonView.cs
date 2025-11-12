using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionButtonView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Color primaryColor = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color secondaryColor = Color.gray;
    [SerializeField] private Color disabledColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private float disabledAlpha = 0.6f;

    private InteractionOption currentOption;
    private InteractionHandler boundHandler;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
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

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
            button.onClick.AddListener(HandleClicked);
            button.interactable = option.IsAvailable;
        }

        if (background != null)
        {
            Color targetColor = option.Style == InteractionOptionStyle.Primary ? primaryColor : secondaryColor;
            if (!option.IsAvailable)
            {
                targetColor = disabledColor;
                targetColor.a = disabledAlpha;
            }
            background.color = targetColor;
        }
    }

    private void HandleClicked()
    {
        if (!currentOption.IsAvailable) return;
        currentOption.Callback?.Invoke(boundHandler);
    }
}
