using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InteractionHandler : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    [SerializeField] private float maxInteractionDistance = 5f;
    [SerializeField] private float aimAssistRadius = 0f;
    [SerializeField] private LayerMask interactableLayer = -1;
    
    [Header("Drop Settings")]
    [SerializeField] private float dropDistance = 2f;
    [SerializeField] private float dropForce = 5f;
    
    [Header("UI")]
    [SerializeField] private Text interactionPromptText;
    
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform itemHoldPoint;
    
    private IInteractable currentTarget;
    private ItemPickup currentItem;
    
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
        
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }
    
    private void Update()
    {
        DetectInteractable();
        
        // Handle interaction
        if (Input.GetKeyDown(interactKey) && currentTarget != null)
        {
            currentTarget.Interact(this);
        }
        
        // Handle drop
        if (Input.GetKeyDown(dropKey) && currentItem != null)
        {
            DropCurrentItem();
        }
    }
    
    private void DetectInteractable()
    {
        // Clear previous target
        if (currentTarget != null)
        {
            DisableOutline(currentTarget);
            HidePrompt();
            currentTarget = null;
        }
        
        if (playerCamera == null) return;
        
        IInteractable detected = null;
        
        // Primary: Raycast from screen center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxInteractionDistance, interactableLayer))
        {
            detected = hit.collider.GetComponent<IInteractable>();
            if (detected == null)
            {
                detected = hit.collider.GetComponentInParent<IInteractable>();
            }
        }
        
        // Secondary: Aim assist if enabled and no direct hit
        if (detected == null && aimAssistRadius > 0f)
        {
            Vector3 checkPoint = ray.GetPoint(Mathf.Min(maxInteractionDistance * 0.5f, 3f));
            Collider[] nearbyColliders = Physics.OverlapSphere(checkPoint, aimAssistRadius, interactableLayer);
            float closestDistance = float.MaxValue;
            
            foreach (Collider col in nearbyColliders)
            {
                IInteractable interactable = col.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    interactable = col.GetComponentInParent<IInteractable>();
                }
                
                if (interactable != null)
                {
                    float dist = Vector3.Distance(interactable.Transform.position, checkPoint);
                    if (dist < closestDistance)
                    {
                        Vector3 toTarget = interactable.Transform.position - playerCamera.transform.position;
                        float dotProduct = Vector3.Dot(playerCamera.transform.forward, toTarget.normalized);
                        
                        if (dotProduct > 0.7f)
                        {
                            detected = interactable;
                            closestDistance = dist;
                        }
                    }
                }
            }
        }
        
        // Process detected interactable
        if (detected != null)
        {
            float distance = Vector3.Distance(transform.position, detected.Transform.position);
            if (distance <= detected.InteractionRange && detected.CanInteract(this))
            {
                currentTarget = detected;
                
                if (detected.ShowOutline)
                {
                    EnableOutline(detected);
                }
                
                ShowPrompt(detected.GetInteractionPrompt(this));
            }
        }
    }
    
    private void EnableOutline(IInteractable interactable)
    {
        MonoBehaviour mb = interactable as MonoBehaviour;
        if (mb != null)
        {
            MonoBehaviour outlinable = mb.GetComponent("Outlinable") as MonoBehaviour;
            if (outlinable != null)
            {
                outlinable.enabled = true;
            }
        }
    }
    
    private void DisableOutline(IInteractable interactable)
    {
        MonoBehaviour mb = interactable as MonoBehaviour;
        if (mb != null)
        {
            MonoBehaviour outlinable = mb.GetComponent("Outlinable") as MonoBehaviour;
            if (outlinable != null)
            {
                outlinable.enabled = false;
            }
        }
    }
    
    private void ShowPrompt(string text)
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.text = text;
            interactionPromptText.gameObject.SetActive(true);
        }
    }
    
    private void HidePrompt()
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }
    
    // Item management (for ItemPickup compatibility)
    public bool PickupItem(ItemPickup item)
    {
        if (item == null || itemHoldPoint == null) return false;
        
        // Drop current item if holding one
        if (currentItem != null)
        {
            DropCurrentItem();
        }
        
        item.Pickup(itemHoldPoint);
        currentItem = item;
        
        // Enable weapon controller if item has one
        WeaponController weapon = item.GetComponent<WeaponController>();
        if (weapon != null)
        {
            weapon.Equip(playerCamera);
        }
        
        return true;
    }
    
    public void DropCurrentItem()
    {
        if (currentItem == null || playerCamera == null) return;
        
        // Disable weapon controller if item has one
        WeaponController weapon = currentItem.GetComponent<WeaponController>();
        if (weapon != null)
        {
            weapon.Unequip();
        }
        
        // Find safe drop position
        Vector3 dropPosition = FindSafeDropPosition();
        Vector3 dropForceVector = playerCamera.transform.forward * dropForce;
        
        // Calculate drop rotation
        Quaternion baseRotation = Quaternion.LookRotation(playerCamera.transform.forward);
        Quaternion finalRotation = baseRotation * Quaternion.Euler(currentItem.DropRotation);
        
        currentItem.Drop(dropPosition, dropForceVector, finalRotation);
        currentItem = null;
    }
    
    private Vector3 FindSafeDropPosition()
    {
        if (currentItem == null) return playerCamera.transform.position + playerCamera.transform.forward * dropDistance;
        
        Collider itemCollider = currentItem.GetComponent<Collider>();
        float itemRadius = 0.5f;
        
        if (itemCollider != null)
        {
            Bounds bounds = itemCollider.bounds;
            itemRadius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        }
        
        Vector3 startPos = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;
        RaycastHit hit;
        
        if (Physics.SphereCast(startPos, itemRadius, direction, out hit, dropDistance))
        {
            float safeDistance = hit.distance - itemRadius - 0.1f;
            safeDistance = Mathf.Max(safeDistance, 0.5f);
            return startPos + direction * safeDistance;
        }
        
        return startPos + direction * dropDistance;
    }
    
    public void ClearCurrentItem()
    {
        currentItem = null;
    }
    
    public void ForcePickupItem(ItemPickup item)
    {
        PickupItem(item);
    }
    
    // Properties
    public bool IsHoldingItem => currentItem != null;
    public ItemPickup CurrentItem => currentItem;
    public Camera PlayerCamera => playerCamera;
    public Transform ItemHoldPoint => itemHoldPoint;
}

