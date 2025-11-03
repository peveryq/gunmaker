using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private string itemName = "Item";
    
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
    private AudioSource audioSource;
    private bool isHeld = false;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private float lastImpactSoundTime = -999f;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
        }
        
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(false);
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
        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
        
        isHeld = true;
    }
    
    public void Drop(Vector3 dropPosition, Vector3 dropForce, Quaternion dropRotation)
    {
        if (!isHeld) return;
        
        // Unparent
        transform.SetParent(null);
        
        // Disable collider temporarily to safely reposition
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
        
        // Set position and rotation
        transform.position = dropPosition;
        transform.rotation = dropRotation;
        
        // Enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // Reset velocities first
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Apply force only if not zero
            if (dropForce.magnitude > 0.01f)
            {
                rb.AddForce(dropForce, ForceMode.Impulse);
            }
        }
        
        // Re-enable collider after physics is set up
        StartCoroutine(EnableColliderDelayed());
        
        isHeld = false;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Play impact sound when item hits something with enough force
        if (!isHeld && impactSounds != null && impactSounds.Length > 0)
        {
            // Check cooldown to prevent sound spam
            if (Time.time - lastImpactSoundTime < impactSoundCooldown)
                return;
            
            float impactVelocity = collision.relativeVelocity.magnitude;
            
            if (impactVelocity >= minImpactVelocity)
            {
                AudioClip impactClip = impactSounds[Random.Range(0, impactSounds.Length)];
                
                if (audioSource != null && impactClip != null)
                {
                    // Use 3D sound at the collision point
                    audioSource.pitch = Random.Range(0.9f, 1.1f); // Slight pitch variation
                    audioSource.PlayOneShot(impactClip);
                    
                    // Update last impact time
                    lastImpactSoundTime = Time.time;
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
        return !isHeld && player != null && !player.IsHoldingItem;
    }
    
    public string GetInteractionPrompt(InteractionHandler player)
    {
        return $"[E] Pick up {itemName}";
    }
    
    public Transform Transform => transform;
    public float InteractionRange => pickupRange;
    public bool ShowOutline => true;
    
    // Properties
    public bool IsHeld => isHeld;
    public float PickupRange => pickupRange;
    public string ItemName => itemName;
    public Vector3 OriginalLocalPosition => originalLocalPosition;
    public Vector3 DropRotation => dropRotation;
}

