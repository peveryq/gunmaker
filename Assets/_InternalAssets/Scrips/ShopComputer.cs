using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Interactable computer that opens the weapon parts shop UI
/// </summary>
public class ShopComputer : MonoBehaviour, IInteractable, IInteractionOptionsProvider
{
    [Header("Computer Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private bool requiresWeaponInHand = false;
    
    [Header("Shop UI")]
    [SerializeField] private ShopUI shopUI;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onShopOpened;
    
    private void Start()
    {
        // Find ShopUI if not assigned
        if (shopUI == null)
        {
            shopUI = FindFirstObjectByType<ShopUI>();
        }
    }
    
    public bool Interact(InteractionHandler player)
    {
        if (player == null) return false;
        
        // Check if weapon is required
        if (requiresWeaponInHand && player.CurrentItem == null)
        {
            return false;
        }
        
        // Open shop UI
        OpenShop();
        
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
    
    public void PopulateInteractionOptions(InteractionHandler handler, List<InteractionOption> options)
    {
        if (options == null) return;
        bool available = CanInteract(handler);
        options.Add(InteractionOption.Primary(
            id: "shop.open",
            label: "shop",
            key: handler != null ? handler.InteractKey : KeyCode.E,
            isAvailable: available,
            callback: h => h.PerformInteraction(this)));
    }
    
    private void OpenShop()
    {
        if (shopUI != null)
        {
            shopUI.OpenShop();
            onShopOpened?.Invoke();
        }
        else
        {
            Debug.LogError("ShopUI not found! Make sure ShopUI component exists in the scene.");
        }
    }
    
    public Transform Transform => transform;
    public float InteractionRange => interactionRange;
    public bool ShowOutline => true;
}

