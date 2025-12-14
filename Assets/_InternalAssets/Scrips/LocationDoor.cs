using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Example interactable object - door that opens location selection
/// </summary>
public class LocationDoor : MonoBehaviour, IInteractable, IInteractionOptionsProvider
{
    [Header("Door Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private string doorName = "Training Range";
    [SerializeField] private bool isLocked = false;
    
    [Header("Localization Keys")]
    [Tooltip("Localization key for 'enter' action when door is unlocked. Default: 'action.enter'")]
    [SerializeField] private string unlockedLabelKey = "action.enter";
    [Tooltip("Localization key for 'locked' action when door is locked. Default: 'action.locked'")]
    [SerializeField] private string lockedLabelKey = "action.locked";
    
    [Header("Fallback Labels (Optional)")]
    [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
    [SerializeField] private string unlockedLabel = "";
    [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
    [SerializeField] private string lockedLabel = "";
    
    [Header("UI Reference")]
    [SerializeField] private LocationSelectionUI locationSelectionUI;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onDoorOpened;
    
    public bool Interact(InteractionHandler player)
    {
        if (isLocked || player == null) return false;
        
        // Open location selection UI
        if (locationSelectionUI != null)
        {
            locationSelectionUI.OpenLocationSelection();
        }
        else
        {
            Debug.LogWarning($"LocationDoor: LocationSelectionUI not assigned for {doorName}");
        }
        
        onDoorOpened?.Invoke();
        
        return true;
    }
    
    public bool CanInteract(InteractionHandler player)
    {
        if (isLocked) return false;
        
        // Block interaction with door until quest 12 is reached
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsQuestBlockingDoor())
        {
            return false;
        }
        
        return true;
    }
    
    public string GetInteractionPrompt(InteractionHandler player)
    {
        if (isLocked)
        {
            return $"[Locked] {doorName}";
        }
        
        return $"[E] Enter {doorName}";
    }
    
    public void PopulateInteractionOptions(InteractionHandler handler, List<InteractionOption> options)
    {
        if (options == null) return;
        
        // Block interaction with door until quest 12 is reached
        // Check this FIRST before other checks to prevent showing the button
        if (TutorialManager.Instance != null)
        {
            if (TutorialManager.Instance.IsQuestBlockingDoor())
            {
                return;
            }
        }
        else
        {
            // If TutorialManager is not available yet, block interaction
            return;
        }
        
        bool available = !isLocked;
        string resolvedLabel;
        
        if (isLocked)
        {
            resolvedLabel = GetLocalizedLabel(lockedLabelKey, lockedLabel, "locked");
        }
        else
        {
            resolvedLabel = GetLocalizedLabel(unlockedLabelKey, unlockedLabel, "enter");
        }
        
        options.Add(InteractionOption.Primary(
            id: $"door.{doorName}",
            label: resolvedLabel,
            key: handler != null ? handler.InteractKey : KeyCode.E,
            isAvailable: available,
            callback: h => h.PerformInteraction(this)));
    }
    
    public Transform Transform => transform;
    public float InteractionRange => interactionRange;
    public bool ShowOutline => true;
    
    // Public methods for controlling door
    public void Lock()
    {
        isLocked = true;
    }
    
    public void Unlock()
    {
        isLocked = false;
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

