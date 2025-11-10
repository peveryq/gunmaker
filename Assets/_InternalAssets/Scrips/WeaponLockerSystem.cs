using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponLockerSystem : MonoBehaviour
{
    public static WeaponLockerSystem Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform storageRoot;
    [SerializeField] private WeaponLockerUI lockerUI;
    [SerializeField] private WeaponSellModal sellModal;
    [SerializeField] private LockerCameraController lockerCameraController;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip stashSound;
    [SerializeField] private AudioClip withdrawSound;

    private InteractionHandler interactionHandler;
    private bool isInitialized;
    private WeaponLockerInteractable activeLockerInteractable;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        interactionHandler = FindFirstObjectByType<InteractionHandler>();
        isInitialized = true;
    }

    public InteractionHandler GetInteractionHandler()
    {
        if (interactionHandler == null)
        {
            interactionHandler = FindFirstObjectByType<InteractionHandler>();
        }
        return interactionHandler;
    }

    public bool HasWeaponsStored
    {
        get
        {
            WeaponSlotManager manager = WeaponSlotManager.Instance;
            if (manager == null) return false;
            return manager.OccupiedCount > 0;
        }
    }

    public void OpenLocker(WeaponLockerInteractable source = null)
    {
        if (!isInitialized || lockerUI == null) return;

        if (interactionHandler == null)
        {
            interactionHandler = FindFirstObjectByType<InteractionHandler>();
        }

        lockerCameraController?.EnterLockerView();
        lockerUI.Show(
            HandleLockerClosed,
            HandleTakeRequested,
            HandleSellRequested);

        if (source != null)
        {
            activeLockerInteractable = source;
            activeLockerInteractable.NotifyLockerOpened();
        }
        else
        {
            activeLockerInteractable = null;
        }
    }

    public bool TryStashHeldWeapon()
    {
        if (interactionHandler == null)
        {
            interactionHandler = FindFirstObjectByType<InteractionHandler>();
        }

        if (interactionHandler == null || !interactionHandler.IsHoldingItem) return false;

        ItemPickup heldItem = interactionHandler.CurrentItem;
        if (heldItem == null) return false;

        WeaponBody weaponBody = heldItem.GetComponent<WeaponBody>();
        if (weaponBody == null) return false;

        DetachFromWorkbenchIfMounted(weaponBody);

        WeaponRecord existingRecord = FindRecordForWeapon(weaponBody);
        if (existingRecord == null)
        {
            existingRecord = CreateRecordForWeapon(weaponBody);
            if (!TryAssignRecordToSlot(existingRecord))
            {
                return false;
            }
        }
        else
        {
            existingRecord.WeaponBody = weaponBody;
            existingRecord.StatsSnapshot = weaponBody.CurrentStats.Clone();
        }
        
        interactionHandler.ClearCurrentItem();

        ItemPickup pickup = weaponBody.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.SetHeldState(false);
        }

        PrepareWeaponForStorage(weaponBody);

        PlaySound(stashSound);
        return true;
    }

    public bool RequestTakeWeapon(WeaponRecord record)
    {
        if (record == null) return false;
        if (interactionHandler == null) return false;

        WeaponBody weaponBody = record.WeaponBody;
        if (weaponBody == null)
        {
            Debug.LogWarning("WeaponLockerSystem: Weapon record has no associated WeaponBody.");
            return false;
        }

        ItemPickup pickup = weaponBody.GetComponent<ItemPickup>();
        if (pickup == null)
        {
            Debug.LogWarning("WeaponLockerSystem: WeaponBody has no ItemPickup component.");
            return false;
        }

        DetachFromWorkbenchIfMounted(weaponBody);
        RestoreWeaponFromStorage(weaponBody);
        pickup.SetHeldState(false);
        interactionHandler.ForcePickupItem(pickup);
        PlaySound(withdrawSound);
        return true;
    }

    public void RequestSellWeapon(WeaponRecord record, Action onSellCompleted = null)
    {
        if (record == null || sellModal == null)
        {
            onSellCompleted?.Invoke();
            return;
        }

        sellModal.Show(
            record,
            _ => ConfirmSell(record, onSellCompleted),
            () => onSellCompleted?.Invoke());
    }

    private void ConfirmSell(WeaponRecord record, Action onSellCompleted)
    {
        if (record == null) return;

        WeaponSlotManager manager = WeaponSlotManager.Instance;
        if (manager == null) return;

        int slotIndex = manager.IndexOf(record.WeaponBody);
        if (slotIndex >= 0)
        {
            manager.ClearSlot(slotIndex);
        }

        WeaponSettings weaponSettings = record.WeaponSettings;
        WeaponBody weaponBody = record.WeaponBody;

        int price = weaponSettings != null ? weaponSettings.totalPartCost : 0;
        MoneySystem.Instance?.AddMoney(price);

        if (weaponBody != null)
        {
            Destroy(weaponBody.gameObject);
        }

        if (weaponSettings != null)
        {
            Destroy(weaponSettings);
        }

        onSellCompleted?.Invoke();

        if (manager.OccupiedCount == 0)
        {
            HandleLockerClosed();
        }
    }

    private WeaponRecord CreateRecordForWeapon(WeaponBody weaponBody)
    {
        WeaponSettings settings = weaponBody.Settings;
        WeaponStats stats = weaponBody.CurrentStats != null ? weaponBody.CurrentStats.Clone() : null;
        return new WeaponRecord(weaponBody.WeaponName, weaponBody, settings, stats);
    }

    private bool TryAssignRecordToSlot(WeaponRecord record)
    {
        WeaponSlotManager manager = WeaponSlotManager.Instance;
        if (manager == null) return false;

        if (!manager.TryAssignNextAvailableSlot(record, out int _))
        {
            Debug.LogWarning("WeaponLockerSystem: No available slot for weapon.");
            return false;
        }

        return true;
    }

    private void PrepareWeaponForStorage(WeaponBody weaponBody)
    {
        GameObject go = weaponBody.gameObject;

        DetachFromWorkbenchIfMounted(weaponBody);

        if (storageRoot != null)
        {
            go.transform.SetParent(storageRoot, true);
        }

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            ResetRigidbodyVelocities(rb);
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider[] colliders = go.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        weaponBody.gameObject.SetActive(false);
    }

    private void RestoreWeaponFromStorage(WeaponBody weaponBody)
    {
        GameObject go = weaponBody.gameObject;

        go.transform.SetParent(null, true);

        Collider[] colliders = go.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
        }

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        go.SetActive(true);
    }

    private void DetachFromWorkbenchIfMounted(WeaponBody weaponBody)
    {
        if (weaponBody == null) return;

        Workbench workbench = weaponBody.GetComponentInParent<Workbench>();
        if (workbench != null)
        {
            int restoreLayer = weaponBody.gameObject.layer;
            workbench.DetachMountedWeapon(weaponBody);
            workbench.ResetMountState();
            weaponBody.transform.SetParent(null, true);
            SetLayerRecursively(weaponBody.gameObject, workbench.DefaultInteractableLayer);
            workbench.RecordLastMountedLayer(restoreLayer);
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private WeaponRecord FindRecordForWeapon(WeaponBody weaponBody)
    {
        WeaponSlotManager manager = WeaponSlotManager.Instance;
        if (manager == null || weaponBody == null) return null;

        int index = manager.IndexOf(weaponBody);
        if (index < 0) return null;
        return manager.GetRecord(index);
    }

    private void HandleLockerClosed()
    {
        lockerUI?.Hide();
        lockerCameraController?.ExitLockerView();
        if (activeLockerInteractable != null)
        {
            activeLockerInteractable.NotifyLockerClosed();
            activeLockerInteractable = null;
        }
    }

    private void HandleTakeRequested(WeaponRecord record)
    {
        if (RequestTakeWeapon(record))
        {
            HandleLockerClosed();
        }
    }

    private void HandleSellRequested(WeaponRecord record)
    {
        RequestSellWeapon(record);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ResetRigidbodyVelocities(Rigidbody rb)
    {
        bool wasKinematic = rb.isKinematic;
        if (wasKinematic)
        {
            rb.isKinematic = false;
        }
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;

        if (wasKinematic)
        {
            rb.isKinematic = true;
        }
    }
}

