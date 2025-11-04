using UnityEngine;

public class Workbench : MonoBehaviour, IInteractable
{
    [Header("Workbench Settings")]
    [SerializeField] private Transform weaponMountPoint;
    [SerializeField] private Vector3 weaponMountRotation = new Vector3(0, -90, 0);
    [SerializeField] private float interactionRange = 3f;
    
    [Header("Part Preview")]
    [SerializeField] private GameObject partPreviewPrefab; // Prefab with Outlinable
    
    private WeaponBody mountedWeapon;
    private GameObject currentPreview;
    private InteractionHandler interactionHandler;
    
    private void Start()
    {
        // Create weapon mount point if not assigned
        if (weaponMountPoint == null)
        {
            GameObject mountObj = new GameObject("WeaponMountPoint");
            mountObj.transform.SetParent(transform);
            mountObj.transform.localPosition = Vector3.up; // 1m above workbench
            mountObj.transform.localRotation = Quaternion.identity;
            weaponMountPoint = mountObj.transform;
        }
    }
    
    private void Update()
    {
        FindPlayer();
    }
    
    private void FindPlayer()
    {
        if (interactionHandler == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                interactionHandler = player.GetComponent<InteractionHandler>();
            }
        }
    }
    
    // Called by InteractionHandler when player can interact (looking at workbench)
    public void ShowPreview()
    {
        // Clear old preview
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
        
        if (interactionHandler == null || mountedWeapon == null) return;
        
        // Show preview if holding a part
        ItemPickup heldItem = interactionHandler.CurrentItem;
        if (heldItem != null)
        {
            WeaponPart part = heldItem.GetComponent<WeaponPart>();
            if (part != null)
            {
                ShowPartPreview(part);
            }
        }
    }
    
    // Called when player stops looking at workbench
    public void HidePreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }
    
    private void MountWeapon(WeaponBody weaponBody)
    {
        if (weaponBody == null) return;
        
        // Notify interaction handler that item is being placed (clear currentItem)
        if (interactionHandler != null)
        {
            interactionHandler.ClearCurrentItem();
        }
        
        // Unequip weapon from player
        WeaponController weaponController = weaponBody.GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.Unequip();
        }
        
        // Mount weapon on workbench
        ItemPickup pickup = weaponBody.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            // Unparent from player
            pickup.transform.SetParent(weaponMountPoint);
            pickup.transform.localPosition = Vector3.zero;
            pickup.transform.localRotation = Quaternion.Euler(weaponMountRotation);
            
            // Mark as held to prevent pickup system from detecting it
            pickup.SetHeldState(true);
        }
        
        // Setup physics
        Rigidbody rb = weaponBody.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Enable collider for part installation detection
        Collider col = weaponBody.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
        
        mountedWeapon = weaponBody;
    }
    
    private void UnmountWeapon()
    {
        if (mountedWeapon == null || interactionHandler == null) return;
        
        // Get ItemPickup
        ItemPickup pickup = mountedWeapon.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            // Mark as not held so it can be picked up
            pickup.SetHeldState(false);
            
            // Trigger pickup through interaction handler (simulates taking weapon)
            interactionHandler.ForcePickupItem(pickup);
        }
        
        mountedWeapon = null;
    }
    
    private void InstallPart(WeaponPart part)
    {
        if (mountedWeapon == null || part == null) return;
        
        // Notify interaction handler that item is being used
        if (interactionHandler != null)
        {
            interactionHandler.ClearCurrentItem();
        }
        
        // Install part on weapon
        bool installed = mountedWeapon.InstallPart(part);
        
        if (installed)
        {
            // Part is now child of weapon, handled by WeaponBody.InstallPart
        }
    }
    
    private void ShowPartPreview(WeaponPart part)
    {
        if (partPreviewPrefab == null || mountedWeapon == null) return;
        
        // Create preview if not exists
        if (currentPreview == null)
        {
            currentPreview = Instantiate(partPreviewPrefab, mountedWeapon.transform);
            currentPreview.transform.localPosition = Vector3.zero;
            currentPreview.transform.localRotation = Quaternion.identity;
        }
        
        // Copy mesh from part to preview
        MeshFilter partMesh = part.GetComponent<MeshFilter>();
        MeshFilter previewMesh = currentPreview.GetComponent<MeshFilter>();
        
        if (partMesh != null && previewMesh != null)
        {
            previewMesh.mesh = partMesh.sharedMesh;
        }
        
        // Copy materials
        MeshRenderer partRenderer = part.GetComponent<MeshRenderer>();
        MeshRenderer previewRenderer = currentPreview.GetComponent<MeshRenderer>();
        
        if (partRenderer != null && previewRenderer != null)
        {
            previewRenderer.materials = partRenderer.sharedMaterials;
        }
        
        // Enable Outlinable on preview
        MonoBehaviour outlinable = currentPreview.GetComponent("Outlinable") as MonoBehaviour;
        if (outlinable != null)
        {
            outlinable.enabled = true;
        }
    }
    
    // IInteractable implementation
    public bool Interact(InteractionHandler player)
    {
        if (player == null) return false;
        
        ItemPickup heldItem = player.CurrentItem;
        
        if (mountedWeapon == null && heldItem != null)
        {
            // Try to mount weapon body
            WeaponBody weaponBody = heldItem.GetComponent<WeaponBody>();
            if (weaponBody != null)
            {
                MountWeapon(weaponBody);
                return true;
            }
        }
        else if (mountedWeapon != null && heldItem == null)
        {
            // Unmount weapon (take back to hands)
            UnmountWeapon();
            return true;
        }
        else if (mountedWeapon != null && heldItem != null)
        {
            // Try to install part on mounted weapon
            WeaponPart part = heldItem.GetComponent<WeaponPart>();
            if (part != null)
            {
                InstallPart(part);
                return true;
            }
        }
        
        return false;
    }
    
    public bool CanInteract(InteractionHandler player)
    {
        if (player == null) return false;
        
        ItemPickup heldItem = player.CurrentItem;
        
        // Can interact if:
        // 1. No weapon mounted and holding a weapon body
        // 2. Weapon mounted and not holding anything (to unmount)
        // 3. Weapon mounted and holding a part (to install)
        
        if (mountedWeapon == null && heldItem != null)
        {
            return heldItem.GetComponent<WeaponBody>() != null;
        }
        
        if (mountedWeapon != null && heldItem == null)
        {
            return true;
        }
        
        if (mountedWeapon != null && heldItem != null)
        {
            return heldItem.GetComponent<WeaponPart>() != null;
        }
        
        return false;
    }
    
    public string GetInteractionPrompt(InteractionHandler player)
    {
        if (player == null) return "";
        
        ItemPickup heldItem = player.CurrentItem;
        
        if (mountedWeapon == null && heldItem != null)
        {
            WeaponBody weaponBody = heldItem.GetComponent<WeaponBody>();
            if (weaponBody != null)
            {
                return "[E] Place weapon on workbench";
            }
        }
        else if (mountedWeapon != null && heldItem == null)
        {
            return "[E] Take weapon";
        }
        else if (mountedWeapon != null && heldItem != null)
        {
            WeaponPart part = heldItem.GetComponent<WeaponPart>();
            if (part != null)
            {
                return $"[E] Install {part.partName}";
            }
        }
        
        return "[E] Workbench";
    }
    
    public Transform Transform => transform;
    public float InteractionRange => interactionRange;
    public bool ShowOutline => true;
    
    // Properties
    public bool HasWeapon => mountedWeapon != null;
    public WeaponBody MountedWeapon => mountedWeapon;
}

