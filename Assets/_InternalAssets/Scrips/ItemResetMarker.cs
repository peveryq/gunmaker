using UnityEngine;

/// <summary>
/// Component that marks an item to be reset to its original position when returning to workshop.
/// Auto-registers with LocationManager on Start.
/// </summary>
public class ItemResetMarker : MonoBehaviour
{
    [Header("Reset Settings")]
    [Tooltip("If true, item will be reset even if currently held by player")]
    [SerializeField] private bool alwaysReset = false;
    
    private ItemPickup itemPickup;
    private bool isRegistered = false;
    
    private void Awake()
    {
        itemPickup = GetComponent<ItemPickup>();
        if (itemPickup == null)
        {
            Debug.LogWarning($"ItemResetMarker: No ItemPickup component found on {gameObject.name}. Component will not work.");
        }
    }
    
    private void Start()
    {
        RegisterWithLocationManager();
    }
    
    private void RegisterWithLocationManager()
    {
        if (isRegistered || itemPickup == null) return;
        
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.RegisterItemForReset(itemPickup, alwaysReset);
            isRegistered = true;
        }
        else
        {
            // If LocationManager not ready yet, try again next frame
            Invoke(nameof(RegisterWithLocationManager), 0.1f);
        }
    }
    
    private void OnDestroy()
    {
        if (isRegistered && itemPickup != null && LocationManager.Instance != null)
        {
            LocationManager.Instance.UnregisterItemForReset(itemPickup);
        }
    }
}

