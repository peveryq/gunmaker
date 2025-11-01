using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private WeaponSettings settings;
    
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Weapon Position")]
    [SerializeField] private Vector3 heldPosition = new Vector3(0.5f, -0.3f, 0.5f);
    [SerializeField] private Vector3 heldRotation = new Vector3(0, 0, 0);
    
    private bool isEquipped = false;
    private int currentAmmo;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    
    private Vector3 originalLocalPosition;
    private Vector3 recoilOffset = Vector3.zero;
    
    private void Start()
    {
        originalLocalPosition = transform.localPosition;
        
        if (settings == null)
        {
            Debug.LogError("WeaponSettings is not assigned!");
            enabled = false;
            return;
        }
        
        currentAmmo = settings.magSize;
    }
    
    private void Update()
    {
        if (!isEquipped) return;
        
        // Check if cursor is locked (for FPS control)
        bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
        
        // Handle shooting only when cursor is locked
        if (cursorLocked && Input.GetMouseButton(0) && Time.time >= nextFireTime && !isReloading)
        {
            Shoot();
        }
        
        // Handle reload only when cursor is locked
        if (cursorLocked && Input.GetKeyDown(KeyCode.R))
        {
            StartReload();
        }
        
        // Handle recoil recovery (always, even when cursor is unlocked)
        if (recoilOffset.magnitude > 0.01f)
        {
            recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, settings.recoilRecoverySpeed * Time.deltaTime);
            transform.localPosition = originalLocalPosition + recoilOffset;
        }
        else
        {
            recoilOffset = Vector3.zero;
            transform.localPosition = originalLocalPosition;
        }
    }
    
    public void Equip(Camera camera)
    {
        playerCamera = camera;
        isEquipped = true;
        
        // Get local position from ItemPickup if available
        ItemPickup itemPickup = GetComponent<ItemPickup>();
        if (itemPickup != null)
        {
            originalLocalPosition = itemPickup.OriginalLocalPosition;
        }
        else
        {
            originalLocalPosition = transform.localPosition;
        }
    }
    
    public void Unequip()
    {
        isEquipped = false;
        playerCamera = null;
    }
    
    private void Shoot()
    {
        if (currentAmmo <= 0)
        {
            PlaySound(settings.emptySound);
            return;
        }
        
        // Create bullet
        if (bulletPrefab != null && firePoint != null)
        {
            Vector3 direction = GetShootDirection();
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));
            
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(direction, settings.bulletSpeed, settings.bulletDamage);
            }
        }
        
        // Apply recoil
        ApplyRecoil();
        
        // Play effects
        PlaySound(settings.shootSound);
        if (settings.muzzleFlash != null && firePoint != null)
        {
            Instantiate(settings.muzzleFlash, firePoint.position, firePoint.rotation, firePoint);
        }
        
        // Update ammo and fire rate
        currentAmmo--;
        nextFireTime = Time.time + settings.fireRate;
        
        // Auto reload if empty
        if (currentAmmo <= 0)
        {
            StartReload();
        }
    }
    
    private Vector3 GetShootDirection()
    {
        Vector3 direction = playerCamera.transform.forward;
        
        // Add spread
        if (settings.spreadAngle > 0)
        {
            float spread = Random.Range(-settings.spreadAngle, settings.spreadAngle);
            Quaternion spreadRotation = Quaternion.Euler(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                0
            );
            direction = spreadRotation * direction;
        }
        
        return direction;
    }
    
    private void ApplyRecoil()
    {
        // Camera recoil (upward kick) - apply to FirstPersonController
        if (playerCamera != null)
        {
            FirstPersonController fpsController = playerCamera.GetComponentInParent<FirstPersonController>();
            if (fpsController != null)
            {
                fpsController.ApplyCameraRecoil(settings.recoilUpward, 0f);
            }
        }
        
        // Weapon recoil (kickback toward player)
        recoilOffset = -transform.forward * settings.recoilKickback;
        transform.localPosition = originalLocalPosition + recoilOffset;
    }
    
    private void StartReload()
    {
        if (isReloading || currentAmmo >= settings.magSize) return;
        
        isReloading = true;
        PlaySound(settings.reloadSound);
        Invoke(nameof(FinishReload), settings.reloadTime);
    }
    
    private void FinishReload()
    {
        currentAmmo = settings.magSize;
        isReloading = false;
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    public bool IsEquipped => isEquipped;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => settings != null ? settings.magSize : 0;
    public bool IsReloading => isReloading;
}
