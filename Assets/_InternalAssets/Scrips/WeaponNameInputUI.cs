using UnityEngine;

public class WeaponNameInputUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private WeaponSlotSelectionUI slotSelectionUI;

    private Workbench activeWorkbench;

    private void Awake()
    {
        if (slotSelectionUI == null)
        {
            slotSelectionUI = FindFirstObjectByType<WeaponSlotSelectionUI>(FindObjectsInactive.Include);
        }
    }

    public void BeginWeaponCreation(Workbench workbench)
    {
        if (slotSelectionUI == null)
        {
            Debug.LogError("WeaponNameInputUI: Slot selection UI is not assigned.");
            return;
        }

        activeWorkbench = workbench;

        slotSelectionUI.Show(
            HandleSlotConfirmed,
            HandleSlotSellRequested,
            HandleSlotSelectionCancelled);
    }

    private void HandleSlotConfirmed(int slotIndex, string weaponName)
    {
        Workbench targetWorkbench = activeWorkbench;
        activeWorkbench = null;

        if (targetWorkbench == null)
        {
            return;
        }

        targetWorkbench.CompleteWeaponCreation(slotIndex, weaponName);
    }

    private void HandleSlotSellRequested(WeaponRecord record, int slotIndex)
    {
        if (record == null)
        {
            return;
        }

        WeaponLockerSystem.Instance?.RequestSellWeapon(record, () =>
        {
            slotSelectionUI.RefreshSlotList();
        });
    }

    private void HandleSlotSelectionCancelled()
    {
        activeWorkbench?.CancelWeaponCreation();
        activeWorkbench = null;
    }
}
