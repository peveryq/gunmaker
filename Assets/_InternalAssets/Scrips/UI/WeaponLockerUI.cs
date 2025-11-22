using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponLockerUI : MonoBehaviour
{
    [System.Serializable]
    private class EmptyStateLabels
    {
        [Tooltip("Localization key for empty state message. Default: 'locker.empty'")]
        public string emptyStateKey = "locker.empty";
        
        [Header("Fallback Labels (Optional)")]
        [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
        public string emptyState = "";
    }

    [Header("Root")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private GameObject uiContentRoot;
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
    [SerializeField] private string emptyStateText = "No stored weapons"; // Deprecated - use emptyStateLabels instead
    
    [Header("Localization")]
    [SerializeField] private EmptyStateLabels emptyStateLabels = new();
    
    [Header("Actions")]
    [SerializeField] private Button takeButton;
    [SerializeField] private Button sellButton;
    
    [Header("Info")]
    [SerializeField] private TextMeshProUGUI weaponNameLabel;
    
    [Header("Stats")]
    [SerializeField] private WeaponStatsUI statsDisplay;

    private readonly List<WeaponRecord> cachedRecords = new();
    private Action onCloseRequested;
    private Action<WeaponRecord> onTakeRequested;
    private Action<WeaponRecord> onSellRequested;

    private WeaponSlotManager slotManager;
    private int currentIndex = -1;
    private GameObject currentPreviewInstance;
    private bool isVisible;
    private bool subscribedToSlots;
    private Graphic[] emptyStateGraphics;

    private FirstPersonController fpsController;
    private bool fpsWasEnabled;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private bool controlCaptured;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        if (emptyStateRoot != null)
        {
            emptyStateGraphics = emptyStateRoot.GetComponentsInChildren<Graphic>(true);
            SetEmptyStateRaycastTargets(false);
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

        if (statsDisplay != null)
        {
            statsDisplay.ClearManualDisplay();
        }
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

    public void EnsureControlCaptured()
    {
        if (!controlCaptured)
        {
            CapturePlayerControl();
        }
    }

    public void PreparePreviewForOpen()
    {
        if (slotManager == null)
        {
            slotManager = WeaponSlotManager.Instance;
            if (slotManager == null)
            {
                Debug.LogError("WeaponLockerUI: WeaponSlotManager instance not found.");
                return;
            }
        }

        SubscribeToSlots();

        RefreshRecords();

        if (isVisible)
        {
            SetVisible(false);
        }
        else
        {
            ApplyHiddenVisualState();
        }
    }

    public void Show(Action closeCallback, Action<WeaponRecord> takeCallback, Action<WeaponRecord> sellCallback)
    {
        // Block ad timer while locker UI is open
        if (AdManager.Instance != null)
        {
            AdManager.Instance.BlockAdTimer();
        }
        
        onCloseRequested = closeCallback;
        onTakeRequested = takeCallback;
        onSellRequested = sellCallback;

        if (slotManager == null)
        {
            slotManager = WeaponSlotManager.Instance;
            if (slotManager == null)
            {
                Debug.LogError("WeaponLockerUI: WeaponSlotManager instance not found.");
                return;
            }
        }

        SubscribeToSlots();
        EnsureControlCaptured();

        RefreshRecords();
        SetVisible(true);
    }

    public void Hide(bool releaseControl = true, bool clearPreview = true)
    {
        // Unblock ad timer when locker UI closes
        bool wasVisible = isVisible;
        
        if (!isVisible && (!releaseControl || !controlCaptured))
        {
            if (releaseControl && controlCaptured)
            {
                ReleasePlayerControl();
            }

            if (clearPreview)
            {
                ClearPreview();
            }

            return;
        }

        SetVisible(false);

        if (clearPreview)
        {
            ClearPreview();
        }

        UnsubscribeFromSlots();

        onCloseRequested = null;
        onTakeRequested = null;
        onSellRequested = null;

        if (releaseControl && controlCaptured)
        {
            ReleasePlayerControl();
        }
        
        // Unblock ad timer only if UI was actually visible
        if (wasVisible && AdManager.Instance != null)
        {
            AdManager.Instance.UnblockAdTimer();
        }
    }

    public void Hide()
    {
        Hide(true, true);
    }

    public void ReleaseCapturedControl()
    {
        if (controlCaptured)
        {
            ReleasePlayerControl();
        }
    }

    private void HandleCloseClicked()
    {
        onCloseRequested?.Invoke();
    }

    private void HandleTakeClicked()
    {
        WeaponRecord record = GetCurrentRecord();
        if (record == null)
        {
            return;
        }

        onTakeRequested?.Invoke(record);
        onCloseRequested?.Invoke();
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

    private bool RefreshRecords()
    {
        cachedRecords.Clear();

        if (slotManager == null) return false;

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

        bool hasRecords = cachedRecords.Count > 0;

        if (!hasRecords)
        {
            UpdateSelection(-1);
            return false;
        }

        if (currentIndex >= cachedRecords.Count || currentIndex < 0)
        {
            UpdateSelection(0);
        }
        else
        {
            UpdateSelection(currentIndex);
        }

        return true;
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
                // Use localized text with fallback chain
                string localizedText = GetLocalizedLabel(
                    emptyStateLabels.emptyStateKey, 
                    emptyStateLabels.emptyState, 
                    !string.IsNullOrEmpty(emptyStateText) ? emptyStateText : "No stored weapons"
                );
                emptyStateLabel.text = localizedText;
            }

            if (weaponNameLabel != null)
            {
                weaponNameLabel.text = string.Empty;
            }

            SetActionButtonsVisible(false);
            SetActionButtonsInteractable(false);
            SetNavigationButtonsVisible(false);
            SetEmptyStateRaycastTargets(false);
            if (statsDisplay != null)
            {
                statsDisplay.ClearManualDisplay();
            }
            return;
        }

        WeaponRecord record = cachedRecords[currentIndex];

        SetEmptyStateRaycastTargets(true);

        if (weaponNameLabel != null)
        {
            weaponNameLabel.text = record?.WeaponName ?? string.Empty;
        }

        CreatePreview(record);
        UpdateStatsPanel(record);
        SetActionButtonsVisible(true);
        SetActionButtonsInteractable(true);
        SetNavigationButtonsVisible(cachedRecords.Count > 1);
    }

    private void UpdateStatsPanel(WeaponRecord record)
    {
        if (statsDisplay == null)
        {
            return;
        }

        if (record == null)
        {
            statsDisplay.ClearManualDisplay();
            return;
        }

        statsDisplay.DisplayWeaponRecord(record);
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

    public void ClearPreview()
    {
        DestroyPreviewInstance();
        cachedRecords.Clear();
        currentIndex = -1;
        if (statsDisplay != null)
        {
            statsDisplay.ClearManualDisplay();
        }
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (rootPanel != null && !rootPanel.activeSelf)
        {
            rootPanel.SetActive(true);
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        if (uiContentRoot != null)
        {
            uiContentRoot.SetActive(visible);
        }
    }

    private void ApplyHiddenVisualState()
    {
        if (rootPanel != null && !rootPanel.activeSelf)
        {
            rootPanel.SetActive(true);
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (uiContentRoot != null)
        {
            uiContentRoot.SetActive(false);
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

    private void SetActionButtonsVisible(bool visible)
    {
        if (takeButton != null)
        {
            takeButton.gameObject.SetActive(visible);
        }

        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(visible);
        }
    }

    private void SetEmptyStateRaycastTargets(bool enabled)
    {
        if (emptyStateGraphics == null) return;
        for (int i = 0; i < emptyStateGraphics.Length; i++)
        {
            Graphic graphic = emptyStateGraphics[i];
            if (graphic != null)
            {
                graphic.raycastTarget = enabled;
            }
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

        controlCaptured = true;
    }

    private void ReleasePlayerControl()
    {
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;

        if (fpsController != null && fpsWasEnabled)
        {
            fpsController.enabled = true;
        }

        controlCaptured = false;
    }
    
    /// <summary>
    /// Helper method to get localized label with fallback chain:
    /// 1. Try localization by key
    /// 2. Use custom fallback if provided
    /// 3. Use default English fallback
    /// </summary>
    private string GetLocalizedLabel(string key, string customFallback, string defaultFallback)
    {
        if (!string.IsNullOrEmpty(key))
        {
            string localized = LocalizationHelper.Get(key);
            // If localization returned something (and not just the key itself), use it
            if (localized != key || LocalizationManager.Instance != null)
            {
                // If we got a valid translation or LocalizationManager exists, use it
                if (localized != key)
                {
                    return localized;
                }
                // If key was returned, try fallback
            }
        }
        
        // Use custom fallback if provided
        if (!string.IsNullOrEmpty(customFallback))
        {
            return customFallback;
        }
        
        // Use default English fallback
        return defaultFallback;
    }
}

