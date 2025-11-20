using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class WeaponLockerSystem : MonoBehaviour
{
    public static WeaponLockerSystem Instance { get; private set; }

    [SerializeField] private Transform storageRoot;
    [SerializeField] private WeaponLockerUI lockerUI;
    [SerializeField] private WeaponSellModal sellModal;
    [SerializeField] private LockerCameraController lockerCameraController;
    [Tooltip("Optional local AudioSource for fallback (if AudioManager not available). Can be left empty.")]
    [SerializeField] private AudioSource audioSource; // Fallback only
    [SerializeField] private AudioClip stashSound;
    [SerializeField] private AudioClip withdrawSound;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("Locker Animations (DOTween)")]
    [Tooltip("DOTweenAnimation components that animate locker doors from closed to open.\nEach element typically corresponds to a single door leaf.\nTweens should be configured from closed (0) to open (+/- angle) with autoPlay = false.")]
    [SerializeField] private DOTweenAnimation[] doorOpenTweens;
    [Tooltip("DOTweenAnimation components that animate locker doors from open back to closed.\nShould mirror doorOpenTweens by index (same doors), but with their own easing/bounce and autoPlay = false.")]
    [SerializeField] private DOTweenAnimation[] doorCloseTweens;

    [Header("Locker Light")]
    [Tooltip("Optional light inside the locker that is toggled when doors open/close.")]
    [SerializeField] private Light lockerLight;
    [Tooltip("Delay before enabling the locker light after opening.")]
    [SerializeField] private float lightOnDelay = 0.1f;
    [Tooltip("Delay before disabling the locker light after closing.")]
    [SerializeField] private float lightOffDelay = 0f;

    private InteractionHandler interactionHandler;
    private bool isInitialized;
    private WeaponLockerInteractable activeLockerInteractable;
    private readonly Dictionary<WeaponBody, List<RigidbodyState>> rigidbodyStateCache = new();
    private readonly Dictionary<WeaponBody, List<ColliderState>> colliderStateCache = new();

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

        // Ensure locker light starts disabled by default
        if (lockerLight != null)
        {
            lockerLight.enabled = false;
        }
    }

    private void Start()
    {
        EnsureLockerUI();
        EnsureSellModal();
        EnsureCameraController();

        interactionHandler = FindFirstObjectByType<InteractionHandler>();
        isInitialized = true;
    }

    private bool EnsureLockerUI()
    {
        if (lockerUI == null)
        {
            lockerUI = FindFirstObjectByType<WeaponLockerUI>(FindObjectsInactive.Include);
        }

        return lockerUI != null;
    }

    private bool EnsureSellModal()
    {
        if (sellModal == null)
        {
            sellModal = FindFirstObjectByType<WeaponSellModal>(FindObjectsInactive.Include);
        }

        return sellModal != null;
    }

    private bool EnsureCameraController()
    {
        if (lockerCameraController == null)
        {
            lockerCameraController = FindFirstObjectByType<LockerCameraController>(FindObjectsInactive.Include);
        }

        return lockerCameraController != null;
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
        if (!isInitialized)
        {
            return;
        }

        if (!EnsureLockerUI())
        {
            Debug.LogError("WeaponLockerSystem: Locker UI reference missing.");
            return;
        }

        EnsureCameraController();

        if (lockerCameraController != null)
        {
            if (lockerCameraController.IsTransitionInProgress || lockerCameraController.IsInLockerView)
            {
                return;
            }
        }

        if (interactionHandler == null)
        {
            interactionHandler = FindFirstObjectByType<InteractionHandler>();
        }

        activeLockerInteractable = source;
        activeLockerInteractable?.NotifyLockerOpened();

        GameplayUIContext.Instance.RequestHudHidden(this);
        lockerUI.EnsureControlCaptured();
        lockerUI.PreparePreviewForOpen();
        PlaySound(openSound);
        PlayLockerAnimation(true);

        Action showLockerUI = () =>
        {
            lockerUI.Show(
                HandleLockerClosed,
                HandleTakeRequested,
                HandleSellRequested);
        };

        if (lockerCameraController != null)
        {
            lockerCameraController.EnterLockerView(showLockerUI);
        }
        else
        {
            showLockerUI();
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

        return TryStashWeapon(weaponBody);
    }

    private bool TryStashWeapon(WeaponBody weaponBody)
    {
        if (weaponBody == null) return false;

        WeaponSlotManager slotManager = WeaponSlotManager.Instance;
        if (slotManager == null) return false;

        WeaponRecord record = FindRecordForWeapon(weaponBody);
        if (record == null)
        {
            return false;
        }

        ItemPickup pickup = weaponBody.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.SetHeldState(false);

            if (interactionHandler != null && interactionHandler.CurrentItem == pickup)
            {
                WeaponController weaponController = pickup.GetComponent<WeaponController>();
                if (weaponController != null)
                {
                    weaponController.Unequip();
                }

                weaponBody.transform.SetParent(null, true);
                interactionHandler.ClearCurrentItem();
            }
        }

        if (PrepareWeaponForStorage(weaponBody))
        {
            PlaySound(stashSound);
            weaponBody.gameObject.SetActive(false);
            return true;
        }

        return false;
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
        if (record == null)
        {
            onSellCompleted?.Invoke();
            return;
        }

        if (!EnsureSellModal())
        {
            Debug.LogWarning("WeaponLockerSystem: Sell modal not found in scene.");
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
    }

    private bool PrepareWeaponForStorage(WeaponBody weaponBody)
    {
        GameObject go = weaponBody.gameObject;

        DetachFromWorkbenchIfMounted(weaponBody);

        if (storageRoot != null)
        {
            go.transform.SetParent(storageRoot, true);
        }

        CacheAndDisableRigidbodies(weaponBody, go);
        CacheAndDisableColliders(weaponBody, go);

        weaponBody.gameObject.SetActive(false);
        return true;
    }

    private void RestoreWeaponFromStorage(WeaponBody weaponBody)
    {
        GameObject go = weaponBody.gameObject;

        go.transform.SetParent(null, true);

        RestoreRigidbodies(weaponBody);
        RestoreColliders(weaponBody);

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }

        go.SetActive(true);
    }

    private void DetachFromWorkbenchIfMounted(WeaponBody weaponBody)
    {
        if (weaponBody == null) return;

        // Try to find workbench in parent hierarchy
        Workbench workbench = weaponBody.GetComponentInParent<Workbench>();
        
        // Also try to find workbench by searching for it in the scene
        // This handles cases where the parent hierarchy might not be set correctly
        if (workbench == null)
        {
            workbench = FindFirstObjectByType<Workbench>();
        }
        
        if (workbench == null) return;
        
        // Check if this weapon is currently mounted on the workbench
        // Compare by instance ID to identify the same weapon
        if (workbench.MountedWeapon != null)
        {
            bool isMounted = workbench.MountedWeapon == weaponBody;
            
            // Also check by weapon name as fallback (in case instance ID doesn't match)
            if (!isMounted && !string.IsNullOrEmpty(weaponBody.WeaponName))
            {
                isMounted = workbench.MountedWeapon.WeaponName == weaponBody.WeaponName;
            }
            
            if (isMounted)
            {
                int restoreLayer = weaponBody.gameObject.layer;
                
                // First, ensure weapon is detached from parent (before calling DetachMountedWeapon)
                if (weaponBody.transform.parent != null)
                {
                    weaponBody.transform.SetParent(null, true);
                }
                
                // Then call workbench methods to fully clean up
                workbench.DetachMountedWeapon(weaponBody);
                workbench.ResetMountState();
                
                // Double-check that weapon is detached (in case it was re-attached)
                if (weaponBody.transform.parent != null)
                {
                    weaponBody.transform.SetParent(null, true);
                }
                
                SetLayerRecursively(weaponBody.gameObject, workbench.DefaultInteractableLayer);
                workbench.RecordLastMountedLayer(restoreLayer);
            }
            else
            {
                // This weapon is NOT the mounted weapon, but there IS a mounted weapon
                // This means we're taking a different weapon from locker while another is on workbench
                // We should destroy the mounted weapon if it's not in any slot
                WeaponBody mountedWeapon = workbench.MountedWeapon;
                if (mountedWeapon != null && mountedWeapon != weaponBody)
                {
                    // Check if mounted weapon is in any slot
                    bool isInSlot = false;
                    if (WeaponSlotManager.Instance != null)
                    {
                        int slotIndex = WeaponSlotManager.Instance.IndexOf(mountedWeapon);
                        isInSlot = slotIndex >= 0;
                    }
                    
                    if (!isInSlot)
                    {
                        // Mounted weapon is not in any slot, destroy it
                        workbench.DetachMountedWeapon(mountedWeapon);
                        workbench.ResetMountState();
                        Destroy(mountedWeapon.gameObject);
                        Debug.Log("WeaponLockerSystem: Destroyed orphaned weapon from workbench.");
                    }
                }
            }
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
        if (lockerCameraController != null && lockerCameraController.IsTransitionInProgress)
        {
            return;
        }

        PlaySound(closeSound);
        PlayLockerAnimation(false);

        Action onExitCompleted = () =>
        {
            lockerUI?.ClearPreview();
            lockerUI?.ReleaseCapturedControl();
            GameplayUIContext.Instance.ReleaseHud(this);
            if (activeLockerInteractable != null)
            {
                activeLockerInteractable.NotifyLockerClosed();
                activeLockerInteractable = null;
            }
        };

        if (lockerCameraController != null)
        {
            lockerUI?.Hide(false, false);
            lockerCameraController.ExitLockerView(onExitCompleted);
        }
        else
        {
            lockerUI?.Hide();
            onExitCompleted();
        }
    }

    private void HandleTakeRequested(WeaponRecord record)
    {
        if (record == null) return;

        RequestTakeWeapon(record);
    }

    private void HandleSellRequested(WeaponRecord record)
    {
        if (record == null) return;

        RequestSellWeapon(record);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        
        // Use AudioManager if available, otherwise fallback to local AudioSource
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, volume: 0.8f);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayLockerAnimation(bool isOpen)
    {
        // Prefer DOTween-based door animations if configured
        DOTweenAnimation[] tweensToPlay = null;
        if (isOpen)
        {
            if (doorOpenTweens != null && doorOpenTweens.Length > 0)
            {
                tweensToPlay = doorOpenTweens;
            }
        }
        else
        {
            if (doorCloseTweens != null && doorCloseTweens.Length > 0)
            {
                tweensToPlay = doorCloseTweens;
            }
        }

        if (tweensToPlay != null && tweensToPlay.Length > 0)
        {
            foreach (DOTweenAnimation anim in tweensToPlay)
    {
                if (anim == null) continue;

                // Ensure tween is created (if autoGenerate is false)
                anim.CreateTween(false, false);
                anim.DORestart();
            }

            // Handle locker light with delay
            if (lockerLight != null)
            {
                if (isOpen)
                {
                    if (lightOnDelay > 0f)
                    {
                        DOVirtual.DelayedCall(lightOnDelay, () =>
                        {
                            if (lockerLight != null) lockerLight.enabled = true;
                        });
                    }
                    else
                    {
                        lockerLight.enabled = true;
                    }
                }
                else
                {
                    if (lightOffDelay > 0f)
                    {
                        DOVirtual.DelayedCall(lightOffDelay, () =>
                        {
                            if (lockerLight != null) lockerLight.enabled = false;
                        });
                    }
                    else
                    {
                        lockerLight.enabled = false;
                    }
                }
            }

            return;
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
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
    }

    private void CacheAndDisableRigidbodies(WeaponBody weaponBody, GameObject root)
    {
        var states = new List<RigidbodyState>();
        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rb in rigidbodies)
        {
            states.Add(new RigidbodyState
            {
                Rigidbody = rb,
                IsKinematic = rb.isKinematic,
                UseGravity = rb.useGravity
            });

            if (!rb.isKinematic)
            {
                ResetRigidbodyVelocities(rb);
            }

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        rigidbodyStateCache[weaponBody] = states;
    }

    private void CacheAndDisableColliders(WeaponBody weaponBody, GameObject root)
    {
        var states = new List<ColliderState>();
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
        {
            states.Add(new ColliderState
            {
                Collider = collider,
                Enabled = collider.enabled
            });

            collider.enabled = false;
        }

        colliderStateCache[weaponBody] = states;
    }

    private void RestoreRigidbodies(WeaponBody weaponBody)
    {
        if (!rigidbodyStateCache.TryGetValue(weaponBody, out var states))
        {
            // No cache found - this weapon was loaded from save and never went through PrepareWeaponForStorage
            // Initialize Rigidbodies properly for weapons loaded from save
            // IMPORTANT: Only restore physics on the weapon body itself, not on installed parts
            // Installed parts should remain kinematic (they're attached via parent transform, not physics)
            Rigidbody weaponBodyRb = weaponBody.GetComponent<Rigidbody>();
            if (weaponBodyRb != null)
            {
                // Set default values for weapon body Rigidbody (non-kinematic, use gravity)
                weaponBodyRb.isKinematic = false;
                weaponBodyRb.useGravity = true;
                ResetRigidbodyVelocities(weaponBodyRb);
            }
            
            // Also check for Rigidbodies in children that are NOT weapon parts
            // Weapon parts should remain kinematic
            Rigidbody[] allRigidbodies = weaponBody.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody rb in allRigidbodies)
            {
                if (rb == null || rb == weaponBodyRb) continue;
                
                // Check if this Rigidbody belongs to a weapon part
                WeaponPart part = rb.GetComponent<WeaponPart>();
                if (part == null)
                {
                    // Not a weapon part, restore physics (e.g., lens overlay, other child objects)
                    // But make it kinematic if it's a visual-only object
                    // For now, keep lens overlays kinematic (they're visual only)
                    if (rb.gameObject.name.ToLower().Contains("lense") || 
                        rb.gameObject.name.ToLower().Contains("lens") ||
                        rb.gameObject.name.ToLower().Contains("overlay"))
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }
                    else
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        ResetRigidbodyVelocities(rb);
                    }
                }
                // Weapon parts remain kinematic (handled by WeaponBody.InstallPart)
            }
            return;
        }

        foreach (var state in states)
        {
            if (state.Rigidbody == null) continue;

            state.Rigidbody.isKinematic = state.IsKinematic;
            state.Rigidbody.useGravity = state.UseGravity;

            if (!state.IsKinematic)
            {
                ResetRigidbodyVelocities(state.Rigidbody);
            }
        }

        rigidbodyStateCache.Remove(weaponBody);
    }

    private void RestoreColliders(WeaponBody weaponBody)
    {
        if (!colliderStateCache.TryGetValue(weaponBody, out var states))
        {
            // No cache found - this weapon was loaded from save and never went through PrepareWeaponForStorage
            // Enable colliders properly for weapons loaded from save
            // IMPORTANT: Only enable collider on the weapon body itself, not on installed parts
            // Installed parts should have colliders disabled (they're attached via parent transform, not physics)
            Collider weaponBodyCollider = weaponBody.GetComponent<Collider>();
            if (weaponBodyCollider != null)
            {
                weaponBodyCollider.enabled = true;
            }
            
            // Also check for Colliders in children that are NOT weapon parts
            // Weapon parts should have colliders disabled
            Collider[] allColliders = weaponBody.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in allColliders)
            {
                if (collider == null || collider == weaponBodyCollider) continue;
                
                // Check if this Collider belongs to a weapon part
                WeaponPart part = collider.GetComponent<WeaponPart>();
                if (part == null)
                {
                    // Not a weapon part, enable collider if it's not a visual-only object
                    // Lens overlays should have colliders disabled (they're visual only)
                    if (!collider.gameObject.name.ToLower().Contains("lense") && 
                        !collider.gameObject.name.ToLower().Contains("lens") &&
                        !collider.gameObject.name.ToLower().Contains("overlay"))
                    {
                        collider.enabled = true;
                    }
                }
                // Weapon parts keep colliders disabled (handled by WeaponBody.InstallPart)
            }
            return;
        }

        foreach (var state in states)
        {
            if (state.Collider == null) continue;
            state.Collider.enabled = state.Enabled;
        }

        colliderStateCache.Remove(weaponBody);
    }

    private struct RigidbodyState
    {
        public Rigidbody Rigidbody;
        public bool IsKinematic;
        public bool UseGravity;
    }

    private struct ColliderState
    {
        public Collider Collider;
        public bool Enabled;
    }
}

