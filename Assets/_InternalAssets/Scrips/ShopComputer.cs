using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Example interactable object - computer that opens shop UI
/// </summary>
public class ShopComputer : MonoBehaviour, IInteractable
{
    [Header("Computer Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private bool requiresWeaponInHand = false;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onShopOpened;
    
    public bool Interact(InteractionHandler player)
    {
        if (player == null) return false;
        
        // Check if weapon is required
        if (requiresWeaponInHand && player.CurrentItem == null)
        {
            return false;
        }
        
        // Open shop UI
        Debug.Log("Opening weapon parts shop");
        onShopOpened?.Invoke();
        
        return true;
    }
    
    public bool CanInteract(InteractionHandler player)
    {
        if (player == null) return false;
        
        if (requiresWeaponInHand && player.CurrentItem == null)
        {
            return false;
        }
        
        return true;
    }
    
    public string GetInteractionPrompt(InteractionHandler player)
    {
        if (requiresWeaponInHand && (player == null || player.CurrentItem == null))
        {
            return "Need weapon in hand";
        }
        
        return "[E] Open Shop";
    }
    
    public Transform Transform => transform;
    public float InteractionRange => interactionRange;
    public bool ShowOutline => true;
}

