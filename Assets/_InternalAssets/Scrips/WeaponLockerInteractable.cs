using UnityEngine;
using UnityEngine.UI;

public class WeaponLockerInteractable : MonoBehaviour, IInteractable, ICustomInteractionUI
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode openKey = KeyCode.E;
    [SerializeField] private KeyCode stashKey = KeyCode.F;

    [Header("References")]
    [SerializeField] private WeaponLockerSystem lockerSystem;
    [SerializeField] private GameObject openButtonUI;
    [SerializeField] private GameObject stashButtonUI;
    [SerializeField] private Button openButton;
    [SerializeField] private Button stashButton;
    [SerializeField] private string openButtonLabel = "Open (E)";
    [SerializeField] private string stashButtonLabel = "Stash (F)";

    private Text openButtonText;
    private Text stashButtonText;
    private bool lockerViewActive;

    private void Awake()
    {
        if (lockerSystem == null)
        {
            lockerSystem = WeaponLockerSystem.Instance;
        }

        CacheButtonLabels();
        RegisterButtonCallbacks(true);
        HideInteractionUI();
    }

    private void OnEnable()
    {
        CacheButtonLabels();
        RegisterButtonCallbacks(true);
    }

    private void OnDisable()
    {
        RegisterButtonCallbacks(false);
        HideInteractionUI();
        lockerViewActive = false;
    }

    public bool Interact(InteractionHandler player)
    {
        if (lockerSystem == null)
        {
            Debug.LogWarning("WeaponLockerInteractable: Locker system not assigned.");
            return false;
        }

        if (lockerViewActive) return false;

        lockerSystem.OpenLocker(this);
        HideInteractionUI();
        return true;
    }

    public bool CanInteract(InteractionHandler player)
    {
        return !lockerViewActive;
    }

    public string GetInteractionPrompt(InteractionHandler player)
    {
        return string.Empty;
    }

    public Transform Transform => transform;
    public float InteractionRange => interactionRange;
    public bool ShowOutline => true;

    public KeyCode OpenKey => openKey;
    public KeyCode StashKey => stashKey;

    public bool TryStash(InteractionHandler player)
    {
        if (lockerViewActive) return false;
        if (lockerSystem == null) return false;
        if (player == null || !player.IsHoldingItem) return false;

        bool stashed = lockerSystem.TryStashHeldWeapon();
        if (stashed)
        {
            UpdateInteractionUI(player);
        }
        return stashed;
    }

    public void ShowInteractionUI(InteractionHandler handler)
    {
        if (lockerViewActive) return;
        UpdateInteractionUI(handler);
    }

    public void UpdateInteractionUI(InteractionHandler handler)
    {
        if (lockerViewActive)
        {
            HideInteractionUI();
            return;
        }

        bool canOpen = lockerSystem != null;
        bool canStash = handler != null && handler.IsHoldingItem && handler.CurrentItem.GetComponent<WeaponBody>() != null;

        ToggleButtonVisual(openButtonUI, openButton, openButtonText, canOpen, openButtonLabel);
        ToggleButtonVisual(stashButtonUI, stashButton, stashButtonText, canStash, stashButtonLabel);
    }

    public void HideInteractionUI()
    {
        if (openButtonUI != null)
        {
            openButtonUI.SetActive(false);
        }

        if (stashButtonUI != null)
        {
            stashButtonUI.SetActive(false);
        }
    }

    private void ToggleButtonVisual(GameObject root, Button button, Text label, bool visible, string text)
    {
        if (root != null)
        {
            root.SetActive(visible);
        }

        if (button != null)
        {
            button.interactable = visible;
            button.gameObject.SetActive(visible);
        }

        if (label != null)
        {
            label.text = text;
        }
    }

    private void RegisterButtonCallbacks(bool register)
    {
        if (register)
        {
            if (openButton != null)
            {
                openButton.onClick.AddListener(HandleOpenButtonClicked);
            }

            if (stashButton != null)
            {
                stashButton.onClick.AddListener(HandleStashButtonClicked);
            }
        }
        else
        {
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(HandleOpenButtonClicked);
            }

            if (stashButton != null)
            {
                stashButton.onClick.RemoveListener(HandleStashButtonClicked);
            }
        }
    }

    private void HandleOpenButtonClicked()
    {
        lockerSystem?.OpenLocker(this);
        HideInteractionUI();
    }

    private void HandleStashButtonClicked()
    {
        if (lockerViewActive) return;

        if (lockerSystem != null)
        {
            bool result = lockerSystem.TryStashHeldWeapon();
            if (result)
            {
                UpdateInteractionUI(lockerSystem.GetInteractionHandler());
            }
        }
    }

    private void CacheButtonLabels()
    {
        if (openButton != null && openButtonText == null)
        {
            openButtonText = openButton.GetComponentInChildren<Text>(true);
        }

        if (stashButton != null && stashButtonText == null)
        {
            stashButtonText = stashButton.GetComponentInChildren<Text>(true);
        }
    }

    public void NotifyLockerOpened()
    {
        lockerViewActive = true;
        HideInteractionUI();
    }

    public void NotifyLockerClosed()
    {
        lockerViewActive = false;
    }
}

