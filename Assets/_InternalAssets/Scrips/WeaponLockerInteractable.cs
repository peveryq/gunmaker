using System.Collections.Generic;
using UnityEngine;

public class WeaponLockerInteractable : MonoBehaviour, IInteractable, IInteractionOptionsProvider
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode openKey = KeyCode.E;
    [SerializeField] private KeyCode stashKey = KeyCode.F;

    [Header("Localization Keys")]
    [Tooltip("Localization key for 'open' action. Default: 'action.open'")]
    [SerializeField] private string openLabelKey = "action.open";
    [Tooltip("Localization key for 'stash' action. Default: 'action.stash'")]
    [SerializeField] private string stashLabelKey = "action.stash";
    
    [Header("Fallback Labels (Optional)")]
    [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
    [SerializeField] private string openLabel = "";
    [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
    [SerializeField] private string stashLabel = "";

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
        if (lockerSystem != null && !lockerViewActive)
        {
            return $"[{openKey}] {openLabel}";
        }

        return string.Empty;
    }

    public Transform Transform => transform;
    public float InteractionRange => interactionRange;
    public bool ShowOutline => true;

    public void PopulateInteractionOptions(InteractionHandler handler, List<InteractionOption> options)
    {
        if (lockerSystem == null || options == null) return;
        if (lockerViewActive) return;

        // Use localization with fallback chain
        string resolvedOpenLabel = GetLocalizedLabel(openLabelKey, openLabel, "open");
        string resolvedStashLabel = GetLocalizedLabel(stashLabelKey, stashLabel, "stash");

        options.Add(InteractionOption.Primary(
            id: "locker.open",
            label: resolvedOpenLabel,
            key: openKey,
            isAvailable: true,
            callback: h => lockerSystem.OpenLocker(this)));

        bool canStash = handler != null && handler.IsHoldingItem && handler.CurrentItem.GetComponent<WeaponBody>() != null;
        if (canStash)
        {
            options.Add(InteractionOption.Secondary(
                id: "locker.stash",
                label: resolvedStashLabel,
                key: stashKey,
                isAvailable: true,
                callback: h => lockerSystem.TryStashHeldWeapon()));
        }
    }

    public void NotifyLockerOpened()
    {
        lockerViewActive = true;
    }

    public void NotifyLockerClosed()
    {
        lockerViewActive = false;
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

