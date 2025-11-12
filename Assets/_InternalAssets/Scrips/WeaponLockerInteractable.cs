using System.Collections.Generic;
using UnityEngine;

public class WeaponLockerInteractable : MonoBehaviour, IInteractable, IInteractionOptionsProvider
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode openKey = KeyCode.E;
    [SerializeField] private KeyCode stashKey = KeyCode.F;

    [Header("References")]
    [SerializeField] private WeaponLockerSystem lockerSystem;

    private bool lockerViewActive;

    private void Awake()
    {
        if (lockerSystem == null)
        {
            lockerSystem = WeaponLockerSystem.Instance;
        }
    }

    public bool Interact(InteractionHandler player)
    {
        if (lockerSystem == null || lockerViewActive) return false;

        lockerSystem.OpenLocker(this);
        return true;
    }

    public bool CanInteract(InteractionHandler player)
    {
        return lockerSystem != null && !lockerViewActive;
    }

    public string GetInteractionPrompt(InteractionHandler player)
    {
        return lockerSystem != null && !lockerViewActive ? "[E] open" : string.Empty;
    }

    public Transform Transform => transform;
    public float InteractionRange => interactionRange;
    public bool ShowOutline => true;

    public void PopulateInteractionOptions(InteractionHandler handler, List<InteractionOption> options)
    {
        if (lockerSystem == null || options == null) return;
        if (lockerViewActive) return;

        options.Add(InteractionOption.Primary(
            id: "locker.open",
            label: "open",
            key: openKey,
            isAvailable: true,
            callback: h => lockerSystem.OpenLocker(this)));

        bool canStash = handler != null && handler.IsHoldingItem && handler.CurrentItem.GetComponent<WeaponBody>() != null;
        options.Add(InteractionOption.Secondary(
            id: "locker.stash",
            label: "stash",
            key: stashKey,
            isAvailable: canStash,
            callback: h =>
            {
                if (canStash)
                {
                    lockerSystem.TryStashHeldWeapon();
                }
            }));
    }

    public void NotifyLockerOpened()
    {
        lockerViewActive = true;
    }

    public void NotifyLockerClosed()
    {
        lockerViewActive = false;
    }
}

