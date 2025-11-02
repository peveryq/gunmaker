using UnityEngine;

public class Workbench : MonoBehaviour
{
    [Header("Workbench Settings")]
    [SerializeField] private Transform weaponMountPoint;
    [SerializeField] private Vector3 weaponMountRotation = new Vector3(0, -90, 0);
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    
    [Header("Part Preview")]
    [SerializeField] private GameObject partPreviewPrefab; // Prefab with Outlinable
    
    private WeaponBody mountedWeapon;
    private GameObject currentPreview;
    private PlayerItemHandler playerHandler;
    private Camera playerCamera;
    
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
        CheckInteraction();
    }
    
    private void FindPlayer()
    {
        if (playerHandler == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHandler = player.GetComponent<PlayerItemHandler>();
                FirstPersonController fps = player.GetComponent<FirstPersonController>();
                if (fps != null)
                {
                    playerCamera = fps.PlayerCamera;
                }
            }
        }
    }
    
    private void CheckInteraction()
    {
        if (playerHandler == null || playerCamera == null) return;
        
        // Clear preview
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
        
        // Check if looking at workbench
        if (!IsLookingAtWorkbench()) return;
        
        // Get current item in hand
        ItemPickup heldItem = playerHandler.CurrentItem;
        
        if (Input.GetKeyDown(interactionKey))
        {
            if (mountedWeapon == null && heldItem != null)
            {
                // Try to mount weapon body
                WeaponBody weaponBody = heldItem.GetComponent<WeaponBody>();
                if (weaponBody != null)
                {
                    MountWeapon(weaponBody);
                }
            }
            else if (mountedWeapon != null && heldItem == null)
            {
                // Unmount weapon (take back to hands)
                UnmountWeapon();
            }
            else if (mountedWeapon != null && heldItem != null)
            {
                // Try to install part on mounted weapon
                WeaponPart part = heldItem.GetComponent<WeaponPart>();
                if (part != null)
                {
                    InstallPart(part);
                }
            }
        }
        
        // Show preview if holding a part and weapon is mounted
        if (mountedWeapon != null && heldItem != null)
        {
            WeaponPart part = heldItem.GetComponent<WeaponPart>();
            if (part != null)
            {
                ShowPartPreview(part);
            }
        }
    }
    
    private bool IsLookingAtWorkbench()
    {
        if (playerCamera == null) return false;
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            return hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform);
        }
        
        return false;
    }
    
    private void MountWeapon(WeaponBody weaponBody)
    {
        if (weaponBody == null) return;
        
        // Notify player handler that item is being placed (clear currentItem)
        if (playerHandler != null)
        {
            playerHandler.ClearCurrentItem();
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
        if (mountedWeapon == null || playerHandler == null) return;
        
        // Get ItemPickup
        ItemPickup pickup = mountedWeapon.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            // Mark as not held so it can be picked up
            pickup.SetHeldState(false);
            
            // Trigger pickup through player handler (simulates taking weapon)
            playerHandler.ForcePickupItem(pickup);
        }
        
        mountedWeapon = null;
    }
    
    private void InstallPart(WeaponPart part)
    {
        if (mountedWeapon == null || part == null) return;
        
        // Notify player handler that item is being used
        if (playerHandler != null)
        {
            playerHandler.ClearCurrentItem();
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
    
    // Properties
    public bool HasWeapon => mountedWeapon != null;
    public WeaponBody MountedWeapon => mountedWeapon;
}

