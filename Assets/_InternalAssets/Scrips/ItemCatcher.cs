using UnityEngine;

/// <summary>
/// Item catcher - teleports pickable items to a specified location when they enter the trigger
/// </summary>
public class ItemCatcher : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Transform where items will be teleported. If null, uses this transform's position.")]
    [SerializeField] private Transform teleportTarget;
    
    [Tooltip("If true, preserves item's rotation when teleporting. If false, uses teleport target's rotation.")]
    [SerializeField] private bool preserveRotation = false;
    
    [Tooltip("If true, only catches items that are NOT currently held by player.")]
    [SerializeField] private bool ignoreHeldItems = true;
    
    [Header("Physics Settings")]
    [Tooltip("If true, resets item's velocity and angular velocity after teleporting.")]
    [SerializeField] private bool resetPhysics = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private void Start()
    {
        // Ensure collider is set as trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"ItemCatcher: Collider on {gameObject.name} is not set as trigger. Setting isTrigger = true.");
            col.isTrigger = true;
        }
        else if (col == null)
        {
            Debug.LogWarning($"ItemCatcher: No collider found on {gameObject.name}. ItemCatcher requires a trigger collider to work.");
        }
        
        // If no teleport target specified, use this transform
        if (teleportTarget == null)
        {
            teleportTarget = transform;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object has ItemPickup component
        ItemPickup itemPickup = other.GetComponent<ItemPickup>();
        if (itemPickup == null)
        {
            // Also check in parent (in case collider is on child object)
            itemPickup = other.GetComponentInParent<ItemPickup>();
        }
        
        if (itemPickup == null)
        {
            return; // Not a pickable item
        }
        
        // Skip if item is held and we're ignoring held items
        if (ignoreHeldItems && itemPickup.IsHeld)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"ItemCatcher: Skipping {itemPickup.ItemName} - item is currently held by player.");
            }
            return;
        }
        
        // Teleport the item
        TeleportItem(itemPickup);
    }
    
    /// <summary>
    /// Teleport item to target location
    /// </summary>
    private void TeleportItem(ItemPickup item)
    {
        if (item == null || teleportTarget == null)
        {
            return;
        }
        
        // Get the item's Rigidbody
        Rigidbody rb = item.GetComponent<Rigidbody>();
        
        // If item has Rigidbody and it's kinematic, we need to temporarily make it non-kinematic
        // to set position, then restore its state
        bool wasKinematic = false;
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            if (wasKinematic)
            {
                rb.isKinematic = false;
            }
            
            // Reset physics if requested
            if (resetPhysics)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
#endif
            }
        }
        
        // Teleport position
        item.transform.position = teleportTarget.position;
        
        // Teleport rotation
        if (preserveRotation)
        {
            // Keep item's current rotation
            // (rotation is already set, no change needed)
        }
        else
        {
            // Use teleport target's rotation
            item.transform.rotation = teleportTarget.rotation;
        }
        
        // Restore kinematic state if needed
        if (rb != null && wasKinematic)
        {
            rb.isKinematic = true;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"ItemCatcher: Teleported {item.ItemName} to {teleportTarget.position}");
        }
    }
    
    /// <summary>
    /// Manually teleport an item to the target location (public method for external calls)
    /// </summary>
    public void TeleportItemManually(ItemPickup item)
    {
        if (item == null)
        {
            Debug.LogWarning("ItemCatcher: Cannot teleport - item is null.");
            return;
        }
        
        TeleportItem(item);
    }
    
    /// <summary>
    /// Set the teleport target at runtime
    /// </summary>
    public void SetTeleportTarget(Transform target)
    {
        teleportTarget = target != null ? target : transform;
    }
}

