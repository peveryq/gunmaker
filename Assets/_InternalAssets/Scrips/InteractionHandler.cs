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
    [Tooltip("Position where items are dropped (relative to camera)")]
    [SerializeField] private Vector3 dropPosition = new Vector3(0f, -0.5f, 1.5f);
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
            
            // Hide workbench preview if leaving workbench
            Workbench prevWorkbench = currentTarget as Workbench;
            if (prevWorkbench != null)
            {
                prevWorkbench.HidePreview();
            }
            
            currentTarget = null;
        }
        
        if (playerCamera == null) return;
        
        IInteractable detected = null;
        
        // Primary: Raycast from screen center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxInteractionDistance, interactableLayer);
        
        // Sort hits by distance (closest first) for accurate selection
        if (hits.Length > 1)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        }
        
        // Find first valid interactable (now guaranteed to be closest)
        foreach (RaycastHit hit in hits)
        {
            // Skip objects that are children of player/camera (held items)
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(playerCamera.transform))
            {
                continue;
            }
            
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }
            
            if (interactable != null)
            {
                detected = interactable;
                break;
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
                // Skip held items
                if (col.transform.IsChildOf(transform) || col.transform.IsChildOf(playerCamera.transform))
                {
                    continue;
                }
                
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
                
                // Show workbench preview if looking at workbench
                Workbench workbench = detected as Workbench;
                if (workbench != null)
                {
                    workbench.ShowPreview();
                }
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
        
        // Automatically drop current item if holding one
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
        
        // Calculate drop position in world space (from camera)
        Vector3 worldDropPosition = playerCamera.transform.position + 
                                    playerCamera.transform.right * dropPosition.x +
                                    playerCamera.transform.up * dropPosition.y +
                                    playerCamera.transform.forward * dropPosition.z;
        
        // Calculate drop rotation
        Quaternion baseRotation = Quaternion.LookRotation(playerCamera.transform.forward);
        Quaternion finalRotation = baseRotation * Quaternion.Euler(currentItem.DropRotation);
        
        // Apply force in camera forward direction
        Vector3 dropForceVector = playerCamera.transform.forward * dropForce;
        
        currentItem.Drop(worldDropPosition, dropForceVector, finalRotation);
        currentItem = null;
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
    public IInteractable CurrentTarget => currentTarget; // For WeaponStatsUI to sync
}

