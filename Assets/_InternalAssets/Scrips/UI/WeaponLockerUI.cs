using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponLockerUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Button closeButton;

    [Header("Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Preview")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private float previewScale = 1f;

    [Header("State")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private TextMeshProUGUI emptyStateLabel;
    [SerializeField] private string emptyStateText = "No stored weapons";

    [Header("Actions")]
    [SerializeField] private Button takeButton;
    [SerializeField] private Button sellButton;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI weaponNameLabel;

    private readonly List<WeaponRecord> cachedRecords = new();
    private Action onCloseRequested;
    private Action<WeaponRecord> onTakeRequested;
    private Action<WeaponRecord> onSellRequested;

    private WeaponSlotManager slotManager;
    private int currentIndex = -1;
    private GameObject currentPreviewInstance;
    private bool isVisible;
    private bool subscribedToSlots;

    private FirstPersonController fpsController;
    private bool fpsWasEnabled;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        if (previousButton != null)
        {
            previousButton.onClick.AddListener(() => Navigate(-1));
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() => Navigate(1));
        }

        if (takeButton != null)
        {
            takeButton.onClick.AddListener(HandleTakeClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.AddListener(HandleSellClicked);
        }

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        if (previousButton != null)
        {
            previousButton.onClick.RemoveAllListeners();
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
        }

        if (takeButton != null)
        {
            takeButton.onClick.RemoveListener(HandleTakeClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(HandleSellClicked);
        }

        UnsubscribeFromSlots();
        DestroyPreviewInstance();
    }

    private void Update()
    {
        if (!isVisible) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleCloseClicked();
        }
    }

    public void Show(Action closeCallback, Action<WeaponRecord> takeCallback, Action<WeaponRecord> sellCallback)
    {
        onCloseRequested = closeCallback;
        onTakeRequested = takeCallback;
        onSellRequested = sellCallback;

        slotManager = WeaponSlotManager.Instance;
        if (slotManager == null)
        {
            Debug.LogError("WeaponLockerUI: WeaponSlotManager instance not found.");
            return;
        }

        SubscribeToSlots();
        CapturePlayerControl();
        SetVisible(true);

        RefreshRecords();
        UpdateSelection(0);
    }

    public void Hide()
    {
        if (!isVisible) return;

        SetVisible(false);
        DestroyPreviewInstance();
        ReleasePlayerControl();
        UnsubscribeFromSlots();

        onCloseRequested = null;
        onTakeRequested = null;
        onSellRequested = null;
        cachedRecords.Clear();
        currentIndex = -1;
    }

    private void HandleCloseClicked()
    {
        onCloseRequested?.Invoke();
    }

    private void HandleTakeClicked()
    {
        WeaponRecord record = GetCurrentRecord();
        if (record == null) return;

        onTakeRequested?.Invoke(record);
    }

    private void HandleSellClicked()
    {
        WeaponRecord record = GetCurrentRecord();
        if (record == null) return;

        onSellRequested?.Invoke(record);
    }

    private void Navigate(int delta)
    {
        if (cachedRecords.Count <= 1) return;
        int nextIndex = Mathf.Clamp(currentIndex + delta, 0, cachedRecords.Count - 1);
        UpdateSelection(nextIndex);
    }

    private void RefreshRecords()
    {
        cachedRecords.Clear();

        if (slotManager == null) return;

        IReadOnlyList<WeaponRecord> records = slotManager.GetSlotRecords();
        if (records != null)
        {
            for (int i = 0; i < records.Count; i++)
            {
                WeaponRecord record = records[i];
                if (record != null)
                {
                    cachedRecords.Add(record);
                }
            }
        }

        if (cachedRecords.Count == 0)
        {
            UpdateSelection(-1);
        }
        else if (currentIndex >= cachedRecords.Count || currentIndex < 0)
        {
            UpdateSelection(0);
        }
        else
        {
            UpdateSelection(currentIndex);
        }
    }

    private void UpdateSelection(int newIndex)
    {
        currentIndex = newIndex;

        DestroyPreviewInstance();

        bool hasWeapons = cachedRecords.Count > 0 && currentIndex >= 0;

        if (emptyStateRoot != null)
        {
            emptyStateRoot.SetActive(!hasWeapons);
        }

        if (!hasWeapons)
        {
            if (emptyStateLabel != null)
            {
                emptyStateLabel.text = emptyStateText;
            }

            if (weaponNameLabel != null)
            {
                weaponNameLabel.text = string.Empty;
            }

            SetActionButtonsInteractable(false);
            SetNavigationButtonsVisible(false);
            return;
        }

        WeaponRecord record = cachedRecords[currentIndex];
        if (weaponNameLabel != null)
        {
            weaponNameLabel.text = record?.WeaponName ?? string.Empty;
        }

        CreatePreview(record);
        SetActionButtonsInteractable(true);
        SetNavigationButtonsVisible(cachedRecords.Count > 1);
    }

    private WeaponRecord GetCurrentRecord()
    {
        if (cachedRecords.Count == 0 || currentIndex < 0 || currentIndex >= cachedRecords.Count)
        {
            return null;
        }

        return cachedRecords[currentIndex];
    }

    private void CreatePreview(WeaponRecord record)
    {
        if (record == null || record.WeaponBody == null || previewAnchor == null)
        {
            return;
        }

        currentPreviewInstance = Instantiate(record.WeaponBody.gameObject, previewAnchor);
        currentPreviewInstance.SetActive(true);
        currentPreviewInstance.transform.localPosition = Vector3.zero;
        currentPreviewInstance.transform.localRotation = Quaternion.identity;
        currentPreviewInstance.transform.localScale = Vector3.one * previewScale;

        foreach (MonoBehaviour behaviour in currentPreviewInstance.GetComponentsInChildren<MonoBehaviour>())
        {
            behaviour.enabled = false;
        }

        foreach (Collider collider in currentPreviewInstance.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        Rigidbody rb = currentPreviewInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        ItemPickup pickup = currentPreviewInstance.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.enabled = false;
        }
    }

    private void DestroyPreviewInstance()
    {
        if (currentPreviewInstance != null)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

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

    private void SetActionButtonsInteractable(bool interactable)
    {
        if (takeButton != null)
        {
            takeButton.interactable = interactable;
        }

        if (sellButton != null)
        {
            sellButton.interactable = interactable;
        }
    }

    private void SetNavigationButtonsVisible(bool visible)
    {
        if (previousButton != null)
        {
            previousButton.gameObject.SetActive(visible);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(visible);
        }
    }

    private void SubscribeToSlots()
    {
        if (subscribedToSlots || slotManager == null) return;
        slotManager.SlotsChanged += OnSlotsChanged;
        subscribedToSlots = true;
    }

    private void UnsubscribeFromSlots()
    {
        if (!subscribedToSlots || slotManager == null) return;
        slotManager.SlotsChanged -= OnSlotsChanged;
        subscribedToSlots = false;
    }

    private void OnSlotsChanged()
    {
        RefreshRecords();
    }

    private void CapturePlayerControl()
    {
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (fpsController == null)
        {
            fpsController = FindFirstObjectByType<FirstPersonController>();
        }

        if (fpsController != null)
        {
            fpsWasEnabled = fpsController.enabled;
            fpsController.enabled = false;
        }
    }

    private void ReleasePlayerControl()
    {
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;

        if (fpsController != null && fpsWasEnabled)
        {
            fpsController.enabled = true;
        }
    }
}

