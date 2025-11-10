using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotEntryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private GameObject occupiedStateRoot;
    [SerializeField] private GameObject availableStateRoot;
    [SerializeField] private TextMeshProUGUI weaponNameLabel;
    [SerializeField] private TextMeshProUGUI slotIndexLabel;

    private int slotIndex;
    private WeaponSlotState currentState;
    private Action<int, WeaponSlotState> onClicked;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    public void Setup(int index, WeaponSlotState state, string weaponName, Action<int, WeaponSlotState> clickCallback)
    {
        slotIndex = index;
        currentState = state;
        onClicked = clickCallback;

        UpdateVisuals(weaponName);
    }

    private void UpdateVisuals(string weaponName)
    {
        bool isVisible = currentState != WeaponSlotState.Hidden;
        if (gameObject.activeSelf != isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        if (!isVisible)
        {
            return;
        }

        if (slotIndexLabel != null)
        {
            slotIndexLabel.text = (slotIndex + 1).ToString();
        }

        if (weaponNameLabel != null)
        {
            weaponNameLabel.text = weaponName ?? string.Empty;
        }

        if (occupiedStateRoot != null)
        {
            occupiedStateRoot.SetActive(currentState == WeaponSlotState.Occupied);
        }

        if (availableStateRoot != null)
        {
            availableStateRoot.SetActive(currentState == WeaponSlotState.Available);
        }

        if (button != null)
        {
            button.interactable = currentState != WeaponSlotState.Hidden;
        }
    }

    private void OnButtonClicked()
    {
        if (currentState == WeaponSlotState.Hidden) return;
        onClicked?.Invoke(slotIndex, currentState);
    }
}

