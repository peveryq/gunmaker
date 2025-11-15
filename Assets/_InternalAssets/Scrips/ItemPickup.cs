using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable, IInteractionOptionsProvider
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private string itemName = "Item";
    [SerializeField] private string pickupLabel = "grab";
    
    [Header("Auto-Settings (Optional)")]
    [Tooltip("If checked, will automatically apply settings from WeaponPart type")]
    [SerializeField] private bool usePartTypeDefaults = true;
    
    [Header("Held Position")]
    [Tooltip("LOCAL position relative to CAMERA when holding item.\nX: left(-)/right(+), Y: down(-)/up(+), Z: forward(+)\nExample: (0, -0.3, 0.5) = centered, slightly down, in front")]
    [SerializeField] private Vector3 heldPosition = new Vector3(0f, -0.3f, 0.5f);
    [Tooltip("LOCAL rotation when holding item (Euler angles)")]
    [SerializeField] private Vector3 heldRotation = new Vector3(0, 0, 0);
    
    [Header("Drop Settings")]
    [Tooltip("Rotation when dropped (relative to camera direction).\n(0,90,0) = item lies sideways to camera view\n(90,0,0) = item tilted forward")]
    [SerializeField] private Vector3 dropRotation = new Vector3(0, 90, 0);
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject pickupPrompt;
    
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip[] impactSounds; // Sounds when item hits ground
    [SerializeField] private float minImpactVelocity = 1f; // Minimum velocity to play impact sound
    [SerializeField] private float impactSoundCooldown = 0.5f; // Time between impact sounds
    
    private Rigidbody rb;
    private Collider itemCollider;
    [Tooltip("Optional local AudioSource for fallback (if AudioManager not available). Can be left empty.")]
    private AudioSource audioSource; // Fallback only
    private bool isHeld = false;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private float lastImpactSoundTime = -999f;

    // AudioSource is now optional (fallback only)
    // AudioManager will be used if available

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();

        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(false);
        }

        // Apply part type defaults if enabled
        if (usePartTypeDefaults)
        {
            ApplyPartTypeDefaults();
        }
    }

    private void EnsureAudioSource()
    {
        // Only create AudioSource if needed for fallback
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null && AudioManager.Instance == null)
        {
            // Only create if AudioManager is not available
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D sound (fallback)
            audioSource.playOnAwake = false;
        }
    }
    
    public void PopulateInteractionOptions(InteractionHandler handler, List<InteractionOption> options)
    {
        if (handler == null || options == null)
        {
            return;
        }

        bool available = CanInteract(handler);
        if (!available)
        {
            return;
        }

        string resolvedLabel = string.IsNullOrEmpty(pickupLabel) ? "grab" : pickupLabel;
        options.Add(InteractionOption.Primary(
            id: $"pickup.{GetInstanceID()}",
            label: resolvedLabel,
            key: handler.InteractKey,
            isAvailable: true,
            callback: h => h.PerformInteraction(this)));
    }
    
    // Apply default settings based on weapon part type or body
    private void ApplyPartTypeDefaults()
    {
        // Get defaults from PartTypeDefaultSettings
        PartTypeDefaultSettings defaults = PartTypeDefaultSettings.Instance;
        if (defaults == null) return;
        
        // Check if this is a weapon part
        WeaponPart weaponPart = GetComponent<WeaponPart>();
        if (weaponPart != null)
        {
            switch (weaponPart.partType)
            {
                case PartType.Barrel:
                    heldPosition = defaults.barrelHeldPosition;
                    heldRotation = defaults.barrelHeldRotation;
                    dropRotation = defaults.barrelDropRotation;
                    break;
                case PartType.Magazine:
                    heldPosition = defaults.magazineHeldPosition;
                    heldRotation = defaults.magazineHeldRotation;
                    dropRotation = defaults.magazineDropRotation;
                    break;
                case PartType.Stock:
                    heldPosition = defaults.stockHeldPosition;
                    heldRotation = defaults.stockHeldRotation;
                    dropRotation = defaults.stockDropRotation;
                    break;
                case PartType.Scope:
                    heldPosition = defaults.scopeHeldPosition;
                    heldRotation = defaults.scopeHeldRotation;
                    dropRotation = defaults.scopeDropRotation;
                    break;
            }
        }
        // Check if this is a weapon body
        else if (GetComponent<WeaponBody>() != null)
        {
            heldPosition = defaults.bodyHeldPosition;
            heldRotation = defaults.bodyHeldRotation;
            dropRotation = defaults.bodyDropRotation;
        }
    }
    
    public void Pickup(Transform parent)
    {
        if (isHeld) return;
        
        // Parent to camera
        transform.SetParent(parent);
        transform.localPosition = heldPosition;
        transform.localRotation = Quaternion.Euler(heldRotation);
        
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        
        // Disable physics
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Disable collider
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
        
        // Hide prompt
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(false);
        }
        
        // Play sound
        if (pickupSound != null)
        {
            // Use AudioManager if available, otherwise fallback to local AudioSource
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(pickupSound, volume: 0.7f);
            }
            else
            {
                EnsureAudioSource();
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(pickupSound);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
            }
        }
        
        isHeld = true;
    }
    
    public void Drop(Vector3 dropPosition, Vector3 dropForce, Quaternion dropRotationQuat)
    {
        if (!isHeld) return;
        
        // Unparent
        transform.SetParent(null);
        
        // Disable collider temporarily to safely reposition
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
        
        // Adjust drop position to use geometry center instead of pivot
        Vector3 geometryCenter = GetGeometryCenter();
        Vector3 pivotToCenter = transform.position - geometryCenter;
        Vector3 adjustedDropPosition = dropPosition + pivotToCenter;
        
        // Set position and rotation
        transform.position = adjustedDropPosition;
        transform.rotation = dropRotationQuat;
        
        // Enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // Reset velocities first
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            
            // Apply drop force
            rb.AddForce(dropForce, ForceMode.Impulse);
        }
        
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
        }
        
        // AudioSource setup is now optional (fallback only)
        
        isHeld = false;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!isHeld && impactSounds != null && impactSounds.Length > 0)
        {
            if (rb == null || rb.linearVelocity.magnitude < minImpactVelocity)
            {
                return;
            }

            if (Time.time - lastImpactSoundTime < impactSoundCooldown)
            {
                return;
            }

            lastImpactSoundTime = Time.time;

            AudioClip impactClip = impactSounds[Random.Range(0, impactSounds.Length)];
            if (impactClip != null)
            {
                // Use AudioManager if available, otherwise fallback to local AudioSource
                if (AudioManager.Instance != null)
                {
                    float pitch = Random.Range(0.9f, 1.1f); // Slight pitch variation
                    AudioManager.Instance.PlaySFX(impactClip, volume: 0.6f, pitch: pitch);
                }
                else
                {
                    EnsureAudioSource();
                    if (audioSource != null)
                    {
                        audioSource.pitch = Random.Range(0.9f, 1.1f); // Slight pitch variation
                        audioSource.PlayOneShot(impactClip);
                    }
                    else
                    {
                        AudioSource.PlayClipAtPoint(impactClip, transform.position);
                    }
                }
            }
        }
    }
    
    private System.Collections.IEnumerator EnableColliderDelayed()
    {
        // Wait for physics to settle
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
        }
    }
    
    // Calculate geometry center based on renderers bounds
    private Vector3 GetGeometryCenter()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            // Fallback to pivot if no renderers
            return transform.position;
        }
        
        // Calculate combined bounds
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }
        
        return combinedBounds.center;
    }
    
    public void ShowPrompt(bool show)
    {
        if (pickupPrompt != null && !isHeld)
        {
            pickupPrompt.SetActive(show);
        }
    }
    
    // Mark item as held without parenting (for workbench)
    public void SetHeldState(bool held)
    {
        isHeld = held;
    }
    
    // IInteractable implementation
    public bool Interact(InteractionHandler player)
    {
        if (isHeld || player == null) return false;
        return player.PickupItem(this);
    }
    
    public bool CanInteract(InteractionHandler player)
    {
        // Can pick up if not currently held (will auto-drop current item if holding one)
        return !isHeld && player != null;
    }
    
    public string GetInteractionPrompt(InteractionHandler player)
    {
        if (player != null && player.IsHoldingItem)
        {
            return $"[E] Swap for {itemName}";
        }
        return $"[E] Pick up {itemName}";
    }
    
    public Transform Transform => transform;
    public float InteractionRange => pickupRange;
    public bool ShowOutline => true;
    
    // Properties
    public bool IsHeld => isHeld;
    public string ItemName => itemName;
    public Vector3 OriginalLocalPosition => originalLocalPosition;
    public Vector3 DropRotation => dropRotation;
}

