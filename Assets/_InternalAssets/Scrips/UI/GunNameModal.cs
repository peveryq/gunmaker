using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunNameModal : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private TextMeshProUGUI headerLabel;
    [SerializeField] private TextMeshProUGUI subtitleLabel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button createButton;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private string headerText = "enter gun name";
    [SerializeField] private string subtitleText = "from 1 to 15 characters";
    [SerializeField] private string placeholderText = "gun name";
    [SerializeField] private int maxCharacters = 15;

    private Action<string> onCreate;
    private Action onClose;
    private bool isActive;

    private void Awake()
    {
        if (createButton != null)
        {
            createButton.onClick.AddListener(HandleCreateClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        if (nameInputField != null)
        {
            nameInputField.onValueChanged.AddListener(HandleInputChanged);
        }

        ApplyStaticLabels();
        SetActive(false);
    }

    private void OnDestroy()
    {
        if (createButton != null)
        {
            createButton.onClick.RemoveListener(HandleCreateClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        if (nameInputField != null)
        {
            nameInputField.onValueChanged.RemoveListener(HandleInputChanged);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleCloseClicked();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            HandleCreateClicked();
        }
    }

    public void Show(Action<string> onCreateCallback, Action onCloseCallback)
    {
        onCreate = onCreateCallback;
        onClose = onCloseCallback;
        SetActive(true);
        ResetInputField();
    }

    public void Hide()
    {
        SetActive(false);
    }

    private void SetActive(bool active)
    {
        isActive = active;

        if (contentRoot != null)
        {
            contentRoot.SetActive(active);
        }

        if (!active)
        {
            onCreate = null;
            onClose = null;
        }
    }

    private void ApplyStaticLabels()
    {
        if (headerLabel != null)
        {
            headerLabel.text = headerText;
        }

        if (subtitleLabel != null)
        {
            subtitleLabel.text = subtitleText;
        }

        if (nameInputField != null)
        {
            nameInputField.characterLimit = maxCharacters;
            if (nameInputField.placeholder is TextMeshProUGUI placeholder)
            {
                placeholder.text = placeholderText;
            }
        }
    }

    private void ResetInputField()
    {
        if (nameInputField == null) return;

        nameInputField.text = string.Empty;
        nameInputField.characterLimit = maxCharacters;
        nameInputField.ActivateInputField();
        nameInputField.Select();
        UpdateCreateButtonState();
    }

    private void HandleInputChanged(string value)
    {
        UpdateCreateButtonState();
    }

    private void HandleCreateClicked()
    {
        if (!isActive || nameInputField == null) return;

        string value = nameInputField.text.Trim();
        if (value.Length == 0) return;

        onCreate?.Invoke(value);
    }

    private void HandleCloseClicked()
    {
        if (!isActive) return;

        onClose?.Invoke();
    }

    private void UpdateCreateButtonState()
    {
        if (createButton == null || nameInputField == null) return;

        string value = nameInputField.text.Trim();
        createButton.interactable = value.Length > 0 && value.Length <= maxCharacters;
    }
}

