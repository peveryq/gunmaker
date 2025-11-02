using UnityEngine;

public class PlayerItemHandler : MonoBehaviour
{
    [Header("Item Controls")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    
    [Header("Pickup Detection")]
    [SerializeField] private float maxPickupDistance = 5f;
    [SerializeField] private float aimAssistRadius = 0f; // Additional radius for easier aiming (0 = precise)
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
        
        // Create item hold point if not assigned (intermediate object between camera and items)
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
            
            // Disable Outlinable component
            MonoBehaviour outlinable = lookingAtItem.GetComponent("Outlinable") as MonoBehaviour;
            if (outlinable != null)
            {
                outlinable.enabled = false;
            }
            
            lookingAtItem = null;
        }
        
        // Don't check if already holding item
        if (currentItem != null) return;
        
        if (playerCamera == null) return;
        
        ItemPickup detectedItem = null;
        
        // Primary detection: Raycast from screen center for precise aiming
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)); // Center of screen
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxPickupDistance, interactableLayer))
        {
            // Check if we hit an item directly
            ItemPickup item = hit.collider.GetComponent<ItemPickup>();
            if (item == null)
            {
                item = hit.collider.GetComponentInParent<ItemPickup>();
            }
            
            if (item != null && !item.IsHeld)
            {
                detectedItem = item;
            }
        }
        
        // Secondary detection: Check nearby items if aim assist is enabled and no direct hit
        if (detectedItem == null && aimAssistRadius > 0f)
        {
            // Get point along the ray for proximity check
            Vector3 checkPoint = ray.GetPoint(Mathf.Min(maxPickupDistance * 0.5f, 3f));
            
            // Use OverlapSphere instead of FindObjectsOfType (MUCH faster!)
            Collider[] nearbyColliders = Physics.OverlapSphere(checkPoint, aimAssistRadius, interactableLayer);
            ItemPickup closestItem = null;
            float closestDistance = float.MaxValue;
            
            foreach (Collider col in nearbyColliders)
            {
                ItemPickup item = col.GetComponent<ItemPickup>();
                if (item == null)
                {
                    item = col.GetComponentInParent<ItemPickup>();
                }
                
                if (item != null && !item.IsHeld)
                {
                    float distanceToRay = Vector3.Distance(item.transform.position, checkPoint);
                    
                    if (distanceToRay < closestDistance)
                    {
                        // Check if item is in front of camera
                        Vector3 toItem = item.transform.position - playerCamera.transform.position;
                        float dotProduct = Vector3.Dot(playerCamera.transform.forward, toItem.normalized);
                        
                        if (dotProduct > 0.7f) // Item is roughly in front (within ~45 degrees)
                        {
                            closestItem = item;
                            closestDistance = distanceToRay;
                        }
                    }
                }
            }
            
            detectedItem = closestItem;
        }
        
        // Process detected item
        if (detectedItem != null)
        {
            // Check if within pickup range (distance from player)
            float distance = Vector3.Distance(transform.position, detectedItem.transform.position);
            if (distance <= detectedItem.PickupRange)
            {
                lookingAtItem = detectedItem;
                lookingAtItem.ShowPrompt(true);
                
                // Enable Outlinable component
                MonoBehaviour outlinable = lookingAtItem.GetComponent("Outlinable") as MonoBehaviour;
                if (outlinable != null)
                {
                    outlinable.enabled = true;
                }
            }
        }
    }
    
    private void PickupItem(ItemPickup item)
    {
        if (item == null || itemHoldPoint == null) return;
        
        // Pickup item to hold point (intermediate object, not directly to camera)
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
        
        // Calculate drop rotation relative to camera forward
        Quaternion baseRotation = Quaternion.LookRotation(playerCamera.transform.forward);
        Quaternion finalRotation = baseRotation * Quaternion.Euler(currentItem.DropRotation);
        
        currentItem.Drop(dropPosition, dropForceVector, finalRotation);
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
    
    // Force pickup item (for workbench unmounting)
    public void ForcePickupItem(ItemPickup item)
    {
        if (item == null || itemHoldPoint == null) return;
        
        // Drop current item if holding one
        if (currentItem != null)
        {
            DropItem();
        }
        
        // Pickup the item
        PickupItem(item);
    }
    
    // Clear current item without dropping (for workbench mounting)
    public void ClearCurrentItem()
    {
        currentItem = null;
    }
    
    // Public property to check if holding item
    public bool IsHoldingItem => currentItem != null;
    public ItemPickup CurrentItem => currentItem;
}

