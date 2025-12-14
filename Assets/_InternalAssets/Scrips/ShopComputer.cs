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
    
    [Header("Localization Keys")]
    [Tooltip("Localization key for 'open shop' action. Default: 'action.open'")]
    [SerializeField] private string interactLabelKey = "action.open";
    
    [Header("Fallback Labels (Optional)")]
    [Tooltip("Fallback text if localization fails. Leave empty to use default English.")]
    [SerializeField] private string interactLabel = "";
    
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
        
        // Block interaction with shop computer until quest 1 is reached
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsQuestBlockingShopComputer())
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
        
        // Block interaction with shop computer until quest 1 is reached
        // Check this FIRST before CanInteract to prevent showing the button
        if (TutorialManager.Instance != null)
        {
            if (TutorialManager.Instance.IsQuestBlockingShopComputer())
            {
                return;
            }
        }
        else
        {
            // If TutorialManager is not available yet, block interaction
            return;
        }
        
        bool available = CanInteract(handler);
        if (!available)
        {
            return;
        }

        // Use localization with fallback chain
        string resolvedLabel = GetLocalizedLabel(interactLabelKey, interactLabel, "shop");
        
        options.Add(InteractionOption.Primary(
            id: "shop.open",
            label: resolvedLabel,
            key: handler != null ? handler.InteractKey : KeyCode.E,
            isAvailable: true,
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

