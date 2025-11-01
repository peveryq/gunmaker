using UnityEngine;

public class PlayerItemHandler : MonoBehaviour
{
    [Header("Item Controls")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    
    [Header("Pickup Detection")]
    [SerializeField] private float maxPickupDistance = 5f;
    [SerializeField] private float detectionRadius = 0.3f; // Radius for sphere cast (easier to aim)
    [SerializeField] private LayerMask interactableLayer = -1; // All layers by default
    
    [Header("Drop Settings")]
    [SerializeField] private float dropDistance = 2f;
    [SerializeField] private float dropForce = 5f;
    
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform itemHoldPoint;
    
    private ItemPickup currentItem;
    private ItemPickup lookingAtItem;
    
    private void Start()
    {
        // Get camera from FirstPersonController if not assigned
        if (playerCamera == null)
        {
            FirstPersonController fpsController = GetComponent<FirstPersonController>();
            if (fpsController != null)
            {
                playerCamera = fpsController.PlayerCamera;
            }
            else
            {
                playerCamera = Camera.main;
            }
        }
        
        // Create item hold point if not assigned
        if (itemHoldPoint == null && playerCamera != null)
        {
            GameObject holdPointObj = new GameObject("ItemHoldPoint");
            holdPointObj.transform.SetParent(playerCamera.transform);
            holdPointObj.transform.localPosition = Vector3.zero;
            holdPointObj.transform.localRotation = Quaternion.identity;
            itemHoldPoint = holdPointObj.transform;
        }
    }
    
    private void Update()
    {
        CheckLookingAtItem();
        
        // Pickup item
        if (Input.GetKeyDown(pickupKey) && lookingAtItem != null && currentItem == null)
        {
            PickupItem(lookingAtItem);
        }
        
        // Drop item
        if (Input.GetKeyDown(dropKey) && currentItem != null)
        {
            DropItem();
        }
    }
    
    private void CheckLookingAtItem()
    {
        // Clear previous looking at item
        if (lookingAtItem != null)
        {
            lookingAtItem.ShowPrompt(false);
            lookingAtItem = null;
        }
        
        // Don't check if already holding item
        if (currentItem != null) return;
        
        if (playerCamera == null) return;
        
        // Use SphereCast for easier aiming (more forgiving than raycast)
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        // SphereCast with configurable radius
        if (Physics.SphereCast(ray, detectionRadius, out hit, maxPickupDistance, interactableLayer))
        {
            // Check if we hit an item
            ItemPickup item = hit.collider.GetComponent<ItemPickup>();
            if (item == null)
            {
                item = hit.collider.GetComponentInParent<ItemPickup>();
            }
            
            if (item != null && !item.IsHeld)
            {
                // Check if within pickup range
                float distance = Vector3.Distance(transform.position, item.transform.position);
                if (distance <= item.PickupRange)
                {
                    lookingAtItem = item;
                    lookingAtItem.ShowPrompt(true);
                }
            }
        }
    }
    
    private void PickupItem(ItemPickup item)
    {
        if (item == null || itemHoldPoint == null) return;
        
        item.Pickup(itemHoldPoint);
        currentItem = item;
        
        // Enable weapon controller if item has one
        WeaponController weapon = item.GetComponent<WeaponController>();
        if (weapon != null)
        {
            weapon.Equip(playerCamera);
        }
    }
    
    private void DropItem()
    {
        if (currentItem == null || playerCamera == null) return;
        
        // Disable weapon controller if item has one
        WeaponController weapon = currentItem.GetComponent<WeaponController>();
        if (weapon != null)
        {
            weapon.Unequip();
        }
        
        // Find safe drop position using sphere cast to avoid placing inside colliders
        Vector3 dropPosition = FindSafeDropPosition();
        Vector3 dropForceVector = playerCamera.transform.forward * dropForce;
        
        currentItem.Drop(dropPosition, dropForceVector);
        currentItem = null;
    }
    
    private Vector3 FindSafeDropPosition()
    {
        // Get item's collider size for proper spacing
        Collider itemCollider = currentItem.GetComponent<Collider>();
        float itemRadius = 0.5f; // Default radius
        
        if (itemCollider != null)
        {
            // Calculate approximate radius from bounds
            Bounds bounds = itemCollider.bounds;
            itemRadius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        }
        
        // Cast a sphere from camera forward to find clear space
        Vector3 startPos = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;
        RaycastHit hit;
        
        // Use SphereCast to check if there's enough space for the item
        if (Physics.SphereCast(startPos, itemRadius, direction, out hit, dropDistance))
        {
            // Found obstacle, place item just before it with some padding
            float safeDistance = hit.distance - itemRadius - 0.1f;
            safeDistance = Mathf.Max(safeDistance, 0.5f); // Minimum 0.5m in front of camera
            return startPos + direction * safeDistance;
        }
        else
        {
            // No obstacle, use full drop distance
            return startPos + direction * dropDistance;
        }
    }
    
    // Public property to check if holding item
    public bool IsHoldingItem => currentItem != null;
    public ItemPickup CurrentItem => currentItem;
}

