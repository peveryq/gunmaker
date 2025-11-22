using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotSelectionUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI headerLabel;
    [SerializeField] private TextMeshProUGUI counterLabel;

    [Header("Slot List")]
    [SerializeField] private GameObject slotListRoot;
    [SerializeField] private Transform slotListContainer;
    [SerializeField] private WeaponSlotEntryUI slotEntryPrefab;

    [Header("Name Modal")]
    [SerializeField] private GunNameModal gunNameModal;

    [Header("Audio")]
    [Tooltip("Optional local AudioSource for fallback (if AudioManager not available). Can be left empty.")]
    [SerializeField] private AudioSource uiAudioSource; // Fallback only
    [SerializeField] private AudioClip clickSound;

    [Header("Text")]
    [SerializeField] private string headerText = "choose slot";

    private readonly List<WeaponSlotEntryUI> pooledEntries = new();

    private Action<int, string> onSlotConfirmed;
    private Action<WeaponRecord, int> onSellRequested;
    private Action onCancelled;

    private FirstPersonController fpsController;
    private bool fpsControllerWasEnabled;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    private bool isActive;
    private bool showingNameModal;
    private int pendingSlotIndex = -1;
    private bool isSubscribedToManager;
    private bool hudVisibilityCaptured;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HandleCloseButtonClicked);
        }

        if (headerLabel != null)
        {
            headerLabel.text = headerText;
        }

        ConfigureNameModalAudio();
        HideImmediate();
    }

    private void OnEnable()
    {
        ConfigureNameModalAudio();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
        }

        UnsubscribeFromSlotManager();
    }

    private void Update()
    {
        if (!isActive) return;

        if (!showingNameModal && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelFlow();
        }
    }

    public void Show(Action<int, string> slotConfirmedCallback, Action<WeaponRecord, int> sellRequestedCallback, Action cancelledCallback)
    {
        // Block ad timer while slot selection UI is open
        if (AdManager.Instance != null)
        {
            AdManager.Instance.BlockAdTimer();
        }
        
        onSlotConfirmed = slotConfirmedCallback;
        onSellRequested = sellRequestedCallback;
        onCancelled = cancelledCallback;

        EnsureCursorLockCaptured();
        AcquireFpsController();
        DisablePlayerControl();

        if (!hudVisibilityCaptured)
        {
            GameplayUIContext.Instance.RequestHudHidden(this);
            hudVisibilityCaptured = true;
        }

        pendingSlotIndex = -1;
        showingNameModal = false;
        isActive = true;

        SubscribeToSlotManager();
        SetRootVisibility(true);
        FocusSlotList();
        RefreshSlots();
    }

    public void Hide()
    {
        if (!isActive) return;

        // Unblock ad timer when slot selection UI closes
        if (AdManager.Instance != null)
        {
            AdManager.Instance.UnblockAdTimer();
        }

        isActive = false;
        showingNameModal = false;
        pendingSlotIndex = -1;

        UnsubscribeFromSlotManager();
        RestorePlayerControl();

        if (hudVisibilityCaptured)
        {
            GameplayUIContext.Instance.ReleaseHud(this);
            hudVisibilityCaptured = false;
        }

        if (gunNameModal != null)
        {
            gunNameModal.Hide();
        }

        if (slotListRoot != null)
        {
            slotListRoot.SetActive(false);
        }

        SetRootVisibility(false);

        onSlotConfirmed = null;
        onSellRequested = null;
        onCancelled = null;
    }

    private void HideImmediate()
    {
        isActive = false;
        showingNameModal = false;
        pendingSlotIndex = -1;
        SetRootVisibility(false);

        if (hudVisibilityCaptured)
        {
            GameplayUIContext.Instance.ReleaseHud(this);
            hudVisibilityCaptured = false;
        }

        if (gunNameModal != null)
        {
            gunNameModal.Hide();
        }
    }

    private void CancelFlow()
    {
        if (!isActive) return;

        onCancelled?.Invoke();
        Hide();
    }

    private void HandleCloseButtonClicked()
    {
        PlayClickSound();
        CancelFlow();
    }

    private void RefreshSlots()
    {
        if (!isActive) return;

        WeaponSlotManager manager = WeaponSlotManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("WeaponSlotSelectionUI: WeaponSlotManager instance not found.");
            return;
        }

        EnsureEntryPool(manager.Capacity);

        int occupied = manager.OccupiedCount;
        int capacity = manager.Capacity;

        for (int i = 0; i < pooledEntries.Count; i++)
        {
            WeaponSlotEntryUI entry = pooledEntries[i];
            bool withinCapacity = i < capacity;

            if (!withinCapacity)
            {
                if (entry.gameObject.activeSelf)
                {
                    entry.gameObject.SetActive(false);
                }
                continue;
            }

            WeaponSlotState state = manager.GetSlotState(i);
            WeaponRecord record = manager.GetRecord(i);
            string weaponName = record?.WeaponName ?? string.Empty;

            entry.Setup(i, state, weaponName, HandleSlotClicked, PlayClickSound);
        }

        if (counterLabel != null)
        {
            counterLabel.text = $"{occupied}/{capacity}";
        }
    }

    public void RefreshSlotList()
    {
        RefreshSlots();
    }

    private void HandleSlotClicked(int slotIndex, WeaponSlotState state)
    {
        if (!isActive) return;

        WeaponSlotManager manager = WeaponSlotManager.Instance;
        if (manager == null) return;

        switch (state)
        {
            case WeaponSlotState.Occupied:
                WeaponRecord record = manager.GetRecord(slotIndex);
                if (record != null)
                {
                    onSellRequested?.Invoke(record, slotIndex);
                }
                break;

            case WeaponSlotState.Available:
                BeginNameEntry(slotIndex);
                break;

            case WeaponSlotState.Hidden:
            default:
                break;
        }
    }

    private void BeginNameEntry(int slotIndex)
    {
        if (gunNameModal == null)
        {
            Debug.LogWarning("WeaponSlotSelectionUI: GunNameModal reference not set.");
            return;
        }

        pendingSlotIndex = slotIndex;
        showingNameModal = true;

        if (slotListRoot != null)
        {
            slotListRoot.SetActive(false);
        }

        gunNameModal.Show(OnNameConfirmed, OnNameCancelled);
    }

    private void OnNameConfirmed(string weaponName)
    {
        if (pendingSlotIndex < 0)
        {
            OnNameCancelled();
            return;
        }

        onSlotConfirmed?.Invoke(pendingSlotIndex, weaponName);
        Hide();
    }

    private void OnNameCancelled()
    {
        pendingSlotIndex = -1;
        showingNameModal = false;
        FocusSlotList();
    }

    private void FocusSlotList()
    {
        if (gunNameModal != null)
        {
            gunNameModal.Hide();
        }

        if (slotListRoot != null)
        {
            slotListRoot.SetActive(true);
        }
    }

    private void EnsureEntryPool(int capacity)
    {
        if (slotEntryPrefab == null || slotListContainer == null) return;

        while (pooledEntries.Count < capacity)
        {
            WeaponSlotEntryUI entry = Instantiate(slotEntryPrefab, slotListContainer);
            pooledEntries.Add(entry);
        }
    }

    private void SubscribeToSlotManager()
    {
        WeaponSlotManager manager = WeaponSlotManager.Instance;
        if (manager != null && !isSubscribedToManager)
        {
            manager.SlotsChanged += RefreshSlots;
            isSubscribedToManager = true;
        }
    }

    private void UnsubscribeFromSlotManager()
    {
        if (!isSubscribedToManager) return;

        WeaponSlotManager manager = WeaponSlotManager.Instance;
        if (manager != null)
        {
            manager.SlotsChanged -= RefreshSlots;
        }

        isSubscribedToManager = false;
    }

    private void SetRootVisibility(bool visible)
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        if (rootPanel != null)
        {
            rootPanel.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    private void EnsureCursorLockCaptured()
    {
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void AcquireFpsController()
    {
        if (fpsController == null)
        {
            fpsController = FindFirstObjectByType<FirstPersonController>();
        }
    }

    private void DisablePlayerControl()
    {
        if (fpsController != null)
        {
            fpsControllerWasEnabled = fpsController.enabled;
            fpsController.enabled = false;
        }
    }

    private void RestorePlayerControl()
    {
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;

        if (fpsController != null && fpsControllerWasEnabled)
        {
            fpsController.enabled = true;
        }
    }

    private void ConfigureNameModalAudio()
    {
        if (gunNameModal != null)
        {
            gunNameModal.ConfigureClickAudio(uiAudioSource, clickSound);
        }
    }

    private void PlayClickSound()
    {
        if (clickSound == null) return;

        // Use AudioManager if available, otherwise fallback to local AudioSource
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clickSound, volume: 0.8f);
        }
        else if (uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(clickSound);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clickSound, transform.position);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ConfigureNameModalAudio();
    }
#endif
}

