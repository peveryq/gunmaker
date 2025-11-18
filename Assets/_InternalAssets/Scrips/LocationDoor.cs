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
    [SerializeField] private string unlockedLabel = "enter";
    [SerializeField] private string lockedLabel = "locked";
    
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
        return !isLocked;
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
        
        bool available = !isLocked;
        string resolvedLabel;
        
        if (isLocked)
        {
            resolvedLabel = string.IsNullOrEmpty(lockedLabel) ? "locked" : lockedLabel;
        }
        else
        {
            resolvedLabel = string.IsNullOrEmpty(unlockedLabel) ? "to testing" : unlockedLabel;
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
}

