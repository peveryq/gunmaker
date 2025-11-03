using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Example interactable object - door that opens location selection
/// </summary>
public class LocationDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private string doorName = "Training Range";
    [SerializeField] private bool isLocked = false;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onDoorOpened;
    
    public bool Interact(InteractionHandler player)
    {
        if (isLocked || player == null) return false;
        
        // Open location selection UI
        Debug.Log($"Opening location selection for: {doorName}");
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

