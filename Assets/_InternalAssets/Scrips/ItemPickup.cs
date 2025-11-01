using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private string itemName = "Item";
    
    [Header("Held Position")]
    [Tooltip("Position when holding item. X=0 centers weapon horizontally for proper aiming.")]
    [SerializeField] private Vector3 heldPosition = new Vector3(0f, -0.3f, 0.5f);
    [SerializeField] private Vector3 heldRotation = new Vector3(0, 0, 0);
    
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
    
    public void Drop(Vector3 dropPosition, Vector3 dropForce)
    {
        if (!isHeld) return;
        
        // Unparent
        transform.SetParent(null);
        
        // Disable collider temporarily to safely reposition
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
        
        // Set position
        transform.position = dropPosition;
        
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
    
    // Properties
    public bool IsHeld => isHeld;
    public float PickupRange => pickupRange;
    public string ItemName => itemName;
    public Vector3 OriginalLocalPosition => originalLocalPosition;
}

