using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private string itemName = "Item";
    
    [Header("Held Position")]
    [SerializeField] private Vector3 heldPosition = new Vector3(0.5f, -0.3f, 0.5f);
    [SerializeField] private Vector3 heldRotation = new Vector3(0, 0, 0);
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject pickupPrompt;
    
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip dropSound;
    
    private Rigidbody rb;
    private Collider itemCollider;
    private AudioSource audioSource;
    private bool isHeld = false;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    
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
        
        // Play sound
        if (dropSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dropSound);
        }
        
        isHeld = false;
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

