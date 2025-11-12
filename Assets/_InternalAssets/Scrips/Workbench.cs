using System.Collections.Generic;
using UnityEngine;

public class Workbench : MonoBehaviour, IInteractable, IInteractionOptionsProvider
{
    [Header("Workbench Settings")]
    [SerializeField] private Transform weaponMountPoint;
    [SerializeField] private Vector3 weaponMountRotation = new Vector3(0, -90, 0);
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private string mountedWeaponLayer = "Default"; // Layer for mounted weapons (non-interactable)
    
    [Header("Part Preview")]
    [SerializeField] private GameObject partPreviewPrefab; // Prefab with Outlinable
    
    [Header("Welding")]
    [SerializeField] private WeldingUI weldingUI;
    [SerializeField] private ParticleSystem weldingSparks; // Sparks particle system at welding point
    [SerializeField] private Transform weldingSparkPoint; // Where sparks appear (optional, uses barrel position if null)
    [SerializeField] private Transform blowtorchWorkPosition; // Position where blowtorch moves when welding
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip installSound; // Sound when part/weapon is installed on workbench
    
    [Header("Weapon Creation")]
    [SerializeField] private GameObject emptyWeaponBodyPrefab; // Prefab of empty weapon body
    [SerializeField] private WeaponNameInputUI weaponNameInputUI;
    
    private WeaponBody mountedWeapon;
    private GameObject currentPreview;
    private InteractionHandler interactionHandler;
    private int originalWeaponLayer = 0; // Store original layer to restore later
    [SerializeField] private string interactableLayerName = "Interactable";
    private bool isCreatingWeapon = false; // Flag to prevent input conflicts
    
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
        
        // Stop welding sparks initially
        if (weldingSparks != null)
        {
            weldingSparks.Stop();
        }
        
        RefreshWeldingUI(null);
    }
    
    private void Update()
    {
        FindPlayer();
        HandleWelding();
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
    
    private void HandleWelding()
    {
        if (interactionHandler == null) return;
        
        // Check if player is holding blowtorch
        ItemPickup heldItem = interactionHandler.CurrentItem;
        Blowtorch blowtorch = heldItem != null ? heldItem.GetComponent<Blowtorch>() : null;
        
        // Check if player is looking at this workbench
        bool isLookingAtWorkbench = ReferenceEquals(interactionHandler.CurrentTarget, this);
        
        // Find unwelded barrel on mounted weapon
        WeldingSystem unweldedBarrel = FindUnweldedBarrel();
        RefreshWeldingUI(unweldedBarrel);
        
        // Show welding UI if holding blowtorch and looking at workbench with unwelded barrel
        if (blowtorch != null && isLookingAtWorkbench && unweldedBarrel != null)
        {
            // Start welding on LMB
            if (Input.GetMouseButton(0))
            {
                blowtorch.StartWorking(blowtorchWorkPosition);
                
                if (blowtorch.IsWorking)
                {
                    float progressAdded = blowtorch.WeldingSpeed * Time.deltaTime;
                    unweldedBarrel.AddWeldingProgress(progressAdded);
                    
                    // Show sparks at welding point
                    ShowWeldingSparks(unweldedBarrel);
                }
            }
            else
            {
                blowtorch.StopWorking();
                HideWeldingSparks();
            }
        }
        else
        {
            // Stop blowtorch if not looking at workbench
            if (blowtorch != null)
            {
                blowtorch.StopWorking();
            }
            
            // Hide sparks
            HideWeldingSparks();
        }
    }
    
    private void ShowWeldingSparks(WeldingSystem weldingTarget)
    {
        if (weldingSparks == null || mountedWeapon == null) return;
        
        // Determine position
        Vector3 targetPosition;
        Quaternion targetRotation;
        
        if (weldingSparkPoint != null)
        {
            // Use custom spark point if assigned
            targetPosition = weldingSparkPoint.position;
            targetRotation = weldingSparkPoint.rotation;
        }
        else
        {
            // Use barrel position as spark point
            WeaponPart barrel = mountedWeapon.GetPart(PartType.Barrel);
            if (barrel != null)
            {
                targetPosition = barrel.transform.position;
                targetRotation = barrel.transform.rotation;
            }
            else
            {
                return; // No valid position
            }
        }
        
        // Position sparks (only if changed significantly or not playing)
        if (!weldingSparks.isPlaying || Vector3.Distance(weldingSparks.transform.position, targetPosition) > 0.01f)
        {
            weldingSparks.transform.position = targetPosition;
            weldingSparks.transform.rotation = targetRotation;
            
            // Restart particle system to apply new position
            if (weldingSparks.isPlaying)
            {
                weldingSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            weldingSparks.Play();
        }
    }
    
    private void HideWeldingSparks()
    {
        if (weldingSparks != null && weldingSparks.isPlaying)
        {
            weldingSparks.Stop();
        }
    }
    
    private WeldingSystem FindUnweldedBarrel()
    {
        if (mountedWeapon == null) return null;
        
        // Check if barrel is installed
        WeaponPart barrel = mountedWeapon.GetPart(PartType.Barrel);
        if (barrel == null) return null;
        
        WeldingSystem weldingSystem = barrel.GetComponent<WeldingSystem>();
        if (weldingSystem != null && weldingSystem.RequiresWelding && !weldingSystem.IsWelded)
        {
            return weldingSystem;
        }
        
        return null;
    }
    
    private void PlayInstallSound()
    {
        if (audioSource != null && installSound != null)
        {
            audioSource.PlayOneShot(installSound);
        }
    }
    
    private void StartWeaponCreation()
    {
        if (emptyWeaponBodyPrefab == null || weaponNameInputUI == null)
        {
            Debug.LogError("Cannot create weapon: emptyWeaponBodyPrefab or weaponNameInputUI not assigned!");
            return;
        }
        
        if (isCreatingWeapon) return;
        
        isCreatingWeapon = true;
        weaponNameInputUI.BeginWeaponCreation(this);
    }
    
    internal void CompleteWeaponCreation(int slotIndex, string weaponName)
    {
        isCreatingWeapon = false;
        
        if (emptyWeaponBodyPrefab == null)
        {
            Debug.LogError("Workbench: emptyWeaponBodyPrefab is missing.");
            return;
        }
        
        WeaponSlotManager slotManager = WeaponSlotManager.Instance;
        if (slotManager == null)
        {
            Debug.LogError("Workbench: WeaponSlotManager instance not found.");
            return;
        }
        
        if (slotManager.GetSlotState(slotIndex) != WeaponSlotState.Available)
        {
            Debug.LogWarning("Workbench: Selected slot is not available for creation.");
            return;
        }
        
        GameObject newWeaponObj = Instantiate(emptyWeaponBodyPrefab);
        WeaponBody newWeaponBody = newWeaponObj.GetComponent<WeaponBody>();
        
        if (newWeaponBody == null)
        {
            Debug.LogError("emptyWeaponBodyPrefab does not have WeaponBody component!");
            Destroy(newWeaponObj);
            return;
    }
    
        newWeaponBody.SetWeaponName(weaponName);
        newWeaponBody.UpdateWeaponStats();
        
        WeaponRecord record = new WeaponRecord(
            newWeaponBody.WeaponName,
            newWeaponBody,
            newWeaponBody.Settings,
            newWeaponBody.CurrentStats != null ? newWeaponBody.CurrentStats.Clone() : null);
        
        if (!slotManager.TryAssignSlot(slotIndex, record))
        {
            Debug.LogWarning("Workbench: Unable to assign weapon to the selected slot.");
            Destroy(newWeaponObj);
            return;
        }
        
        MountWeapon(newWeaponBody);
    }
    
    internal void CancelWeaponCreation()
    {
        isCreatingWeapon = false;
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
        
        if (interactionHandler == null) return;
        
        ItemPickup heldItem = interactionHandler.CurrentItem;
        if (heldItem == null) return;
        
        // Show preview for weapon body (mounting)
        if (mountedWeapon == null)
        {
            WeaponBody weaponBody = heldItem.GetComponent<WeaponBody>();
            if (weaponBody != null)
            {
                ShowWeaponBodyPreview(weaponBody);
            }
        }
        // Show preview for part (installing)
        else
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
        weaponBody.transform.SetParent(weaponMountPoint);
        weaponBody.transform.localPosition = Vector3.zero;
        weaponBody.transform.localRotation = Quaternion.Euler(weaponMountRotation);
        
        ItemPickup pickup = weaponBody.GetComponent<ItemPickup>();
        if (pickup != null)
        {
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
        
        // Store original layer and change to non-interactable layer
        originalWeaponLayer = weaponBody.gameObject.layer;
        SetLayerRecursively(weaponBody.gameObject, LayerMask.NameToLayer(mountedWeaponLayer));
        
        mountedWeapon = weaponBody;
        
        // Play install sound
        PlayInstallSound();
        
        RefreshWeldingUI();
    }

    public void DetachMountedWeapon(WeaponBody weaponBody)
    {
        if (mountedWeapon != null && mountedWeapon == weaponBody)
        {
            mountedWeapon = null;
            RefreshWeldingUI(null);
        }
    }

    public void ResetMountState()
    {
        if (mountedWeapon != null)
        {
            mountedWeapon = null;
            RefreshWeldingUI(null);
        }
    }

    public int DefaultInteractableLayer => LayerMask.NameToLayer(interactableLayerName);

    public void RecordLastMountedLayer(int layer)
    {
        originalWeaponLayer = layer;
    }
    
    private void UnmountWeapon()
    {
        if (mountedWeapon == null || interactionHandler == null) return;
        
        // Restore original layer before unmounting
        SetLayerRecursively(mountedWeapon.gameObject, originalWeaponLayer);
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
        RefreshWeldingUI(null);
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
            
            // Play install sound
            PlayInstallSound();
        }
        
        RefreshWeldingUI();
    }
    
    private void ShowWeaponBodyPreview(WeaponBody weaponBody)
    {
        if (weaponMountPoint == null) return;
        
        // Create ghost - clone the entire weapon (body + all parts)
        currentPreview = new GameObject("WeaponBodyPreview");
        currentPreview.transform.SetParent(weaponMountPoint);
        currentPreview.transform.localPosition = Vector3.zero;
        currentPreview.transform.localRotation = Quaternion.Euler(weaponMountRotation);
        
        // Copy all renderers from weapon body and its children
        CopyRenderersRecursively(weaponBody.transform, currentPreview.transform);
        
        // Copy Outlinable settings from part preview prefab if available
        if (partPreviewPrefab != null)
        {
            MonoBehaviour prefabOutlinable = partPreviewPrefab.GetComponent("Outlinable") as MonoBehaviour;
            if (prefabOutlinable != null)
            {
                // Add Outlinable and copy settings
                System.Type outlinableType = prefabOutlinable.GetType();
                MonoBehaviour previewOutlinable = currentPreview.AddComponent(outlinableType) as MonoBehaviour;
                
                if (previewOutlinable != null)
                {
                    // Setup renderers FIRST (before copying properties)
                    AutoOutline autoOutline = currentPreview.AddComponent<AutoOutline>();
                    autoOutline.SetupOutlinable();
                    
                    // Copy outline properties using reflection AFTER setup
                    CopyOutlinableProperties(prefabOutlinable, previewOutlinable);
                    
                    previewOutlinable.enabled = true;
                }
            }
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
    
    // Recursively copy all renderers from source to target hierarchy
    private void CopyRenderersRecursively(Transform source, Transform target)
    {
        // Copy renderer from source if exists
        MeshFilter sourceMF = source.GetComponent<MeshFilter>();
        MeshRenderer sourceMR = source.GetComponent<MeshRenderer>();
        
        if (sourceMF != null && sourceMR != null)
        {
            MeshFilter targetMF = target.gameObject.AddComponent<MeshFilter>();
            MeshRenderer targetMR = target.gameObject.AddComponent<MeshRenderer>();
            
            targetMF.sharedMesh = sourceMF.sharedMesh;
            targetMR.sharedMaterials = sourceMR.sharedMaterials;
            
            // Copy MeshRenderer settings from prefab if available
            if (partPreviewPrefab != null)
            {
                MeshRenderer prefabMR = partPreviewPrefab.GetComponent<MeshRenderer>();
                if (prefabMR != null)
                {
                    CopyMeshRendererSettings(prefabMR, targetMR);
                }
            }
        }
        
        // Recursively copy children
        foreach (Transform child in source)
        {
            GameObject childCopy = new GameObject(child.name);
            childCopy.transform.SetParent(target);
            childCopy.transform.localPosition = child.localPosition;
            childCopy.transform.localRotation = child.localRotation;
            childCopy.transform.localScale = child.localScale;
            
            CopyRenderersRecursively(child, childCopy.transform);
        }
    }
    
    // Copy MeshRenderer settings (shadows, lighting, etc.) from source to target
    private void CopyMeshRendererSettings(MeshRenderer source, MeshRenderer target)
    {
        if (source == null || target == null) return;
        
        // Shadow settings
        target.shadowCastingMode = source.shadowCastingMode;
        target.receiveShadows = source.receiveShadows;
        
        // Lighting
        target.lightProbeUsage = source.lightProbeUsage;
        target.reflectionProbeUsage = source.reflectionProbeUsage;
        
        // Rendering
        target.motionVectorGenerationMode = source.motionVectorGenerationMode;
        target.allowOcclusionWhenDynamic = source.allowOcclusionWhenDynamic;
        
        // Other settings
        target.renderingLayerMask = source.renderingLayerMask;
        target.rendererPriority = source.rendererPriority;
    }
    
    // Copy Outlinable properties from source to target using reflection
    private void CopyOutlinableProperties(MonoBehaviour source, MonoBehaviour target)
    {
        if (source == null || target == null) return;
        
        System.Type outlinableType = source.GetType();
        
        // Get the private outlineParameters field directly
        var outlineParametersField = outlinableType.GetField("outlineParameters", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (outlineParametersField != null)
        {
            object sourceParameters = outlineParametersField.GetValue(source);
            object targetParameters = outlineParametersField.GetValue(target);
            
            if (sourceParameters != null && targetParameters != null)
            {
                System.Type parametersType = sourceParameters.GetType();
                
                // Copy color field
                var colorField = parametersType.GetField("color", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colorField != null)
                {
                    Color sourceColor = (Color)colorField.GetValue(sourceParameters);
                    colorField.SetValue(targetParameters, sourceColor);
                }
                
                // Copy dilateShift field
                var dilateField = parametersType.GetField("dilateShift", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dilateField != null)
                {
                    float sourceValue = (float)dilateField.GetValue(sourceParameters);
                    dilateField.SetValue(targetParameters, sourceValue);
                }
                
                // Copy blurShift field
                var blurField = parametersType.GetField("blurShift", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (blurField != null)
                {
                    float sourceValue = (float)blurField.GetValue(sourceParameters);
                    blurField.SetValue(targetParameters, sourceValue);
                }
                
                // Copy fillPass (for fill color)
                var fillPassField = parametersType.GetField("fillPass", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fillPassField != null)
                {
                    object sourceFillPass = fillPassField.GetValue(sourceParameters);
                    if (sourceFillPass != null)
                    {
                        // Serialize and deserialize to copy the entire SerializedPass
                        string json = JsonUtility.ToJson(sourceFillPass);
                        object targetFillPass = fillPassField.GetValue(targetParameters);
                        if (targetFillPass != null)
                        {
                            JsonUtility.FromJsonOverwrite(json, targetFillPass);
                        }
                    }
                }
            }
        }
    }
    
    public void PopulateInteractionOptions(InteractionHandler handler, List<InteractionOption> options)
    {
        if (handler == null || options == null)
        {
            return;
        }

        ItemPickup heldItem = handler.CurrentItem;
        string label = null;

        if (mountedWeapon == null && heldItem == null)
        {
            label = "create new gun";
        }
        else if (mountedWeapon == null && heldItem != null && heldItem.GetComponent<WeaponBody>() != null)
        {
            label = "place gun";
        }
        else if (mountedWeapon != null && heldItem == null)
        {
            label = "take gun";
        }
        else if (mountedWeapon != null && heldItem != null && heldItem.GetComponent<WeaponPart>() != null)
        {
            label = "install part";
        }
        else if (heldItem != null && heldItem.GetComponent<Blowtorch>() != null)
        {
            if (FindUnweldedBarrel() != null)
            {
                label = "weld";
            }
        }

        if (!string.IsNullOrEmpty(label))
        {
            bool available = CanInteract(handler);
            options.Add(InteractionOption.Primary(
                id: $"workbench.{label.Replace(" ", string.Empty).ToLowerInvariant()}",
                label: label,
                key: handler.InteractKey,
                isAvailable: available,
                callback: h => h.PerformInteraction(this)));
        }
    }
    
    // IInteractable implementation
    public bool Interact(InteractionHandler player)
    {
        if (player == null) return false;
        
        ItemPickup heldItem = player.CurrentItem;
        
        // Check if holding blowtorch - don't block interaction, just return false to allow welding
        if (heldItem != null && heldItem.GetComponent<Blowtorch>() != null)
        {
            // Don't interact with E key when holding blowtorch
            // Welding is handled by HandleWelding() with LMB
            return false;
        }
        
        // Create new weapon if empty workbench and empty hands
        if (mountedWeapon == null && heldItem == null)
        {
            StartWeaponCreation();
            return true;
        }
        
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
        // 1. No weapon mounted and empty hands (to create new weapon)
        // 2. No weapon mounted and holding a weapon body (to mount)
        // 3. Weapon mounted and not holding anything (to unmount)
        // 4. Weapon mounted and holding a part (to install)
        // 5. Holding blowtorch and there's unwelded barrel (for welding)
        
        // Check for blowtorch - allow interaction for welding
        if (heldItem != null && heldItem.GetComponent<Blowtorch>() != null)
        {
            // Can interact if there's an unwelded barrel
            return FindUnweldedBarrel() != null;
        }
        
        // Allow creating new weapon if workbench is empty and hands are empty
        if (mountedWeapon == null && heldItem == null)
        {
            return emptyWeaponBodyPrefab != null;
        }
        
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
        
        // Check for blowtorch first
        if (heldItem != null)
        {
            Blowtorch blowtorch = heldItem.GetComponent<Blowtorch>();
            if (blowtorch != null && FindUnweldedBarrel() != null)
            {
                return "Weld Barrel (Hold LMB)";
            }
        }
        
        // Create new weapon if empty workbench and empty hands
        if (mountedWeapon == null && heldItem == null && emptyWeaponBodyPrefab != null)
        {
            return "[E] Create New Weapon";
        }
        
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
    
    private void RefreshWeldingUI()
    {
        RefreshWeldingUI(FindUnweldedBarrel());
    }
    
    private void RefreshWeldingUI(WeldingSystem target)
    {
        if (weldingUI != null)
        {
            weldingUI.SetTarget(target);
        }
    }
    
    // For WeaponStatsUI to display mounted weapon stats
    public WeaponBody GetMountedWeapon()
    {
        return mountedWeapon;
    }
    
    public Transform Transform => transform;
    public float InteractionRange => interactionRange;
    public bool ShowOutline => true;
    
    // Helper method to change layer recursively (weapon + all parts)
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        
        obj.layer = layer;
        
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    // Properties
    public bool HasWeapon => mountedWeapon != null;
    public WeaponBody MountedWeapon => mountedWeapon;
}

