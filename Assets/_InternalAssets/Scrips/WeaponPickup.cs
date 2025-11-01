using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject pickupPrompt;
    
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    
    private GameObject player;
    private Camera playerCamera;
    private bool isLookingAt = false;
    private WeaponController weaponController;
    private AudioSource audioSource;
    
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found. Make sure player has 'Player' tag.");
        }
        
        weaponController = GetComponent<WeaponController>();
        if (weaponController == null)
        {
            Debug.LogError("WeaponController not found on this object!");
            enabled = false;
            return;
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(false);
        }
    }
    
    private void Update()
    {
        CheckLookingAt();
        
        if (Input.GetKeyDown(pickupKey) && isLookingAt && !weaponController.IsEquipped)
        {
            TryPickup();
        }
    }
    
    private void CheckLookingAt()
    {
        if (player == null) return;
        
        // Get player camera
        if (playerCamera == null)
        {
            FirstPersonController controller = player.GetComponent<FirstPersonController>();
            if (controller != null)
            {
                playerCamera = controller.PlayerCamera;
            }
            else
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    playerCamera = player.GetComponentInChildren<Camera>();
                }
            }
        }
        
        if (playerCamera == null)
        {
            isLookingAt = false;
            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(false);
            }
            return;
        }
        
        // Raycast from camera to check if looking at weapon
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            // Check if we hit this weapon
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                isLookingAt = true;
            }
            else
            {
                isLookingAt = false;
            }
        }
        else
        {
            isLookingAt = false;
        }
        
        // Show/hide pickup prompt
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(isLookingAt && !weaponController.IsEquipped);
        }
    }
    
    private void TryPickup()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Player camera not found!");
            return;
        }
        
        // Equip weapon
        weaponController.Equip(playerCamera);
        
        // Disable collider only (keep renderer enabled for weapon model)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Play pickup sound
        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
        
        // Hide prompt
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(false);
        }
        
        // Disable this component
        enabled = false;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
