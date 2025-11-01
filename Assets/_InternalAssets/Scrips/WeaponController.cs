using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private WeaponSettings settings;
    
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform casingEjectPoint;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Casing Ejection")]
    [SerializeField] private Vector3 casingEjectDirection = new Vector3(1f, 1f, 0f); // Right and up
    [SerializeField] private float casingEjectForce = 3f;
    
    private bool isEquipped = false;
    private int currentAmmo;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    
    private Vector3 originalLocalPosition;
    private Vector3 recoilOffset = Vector3.zero;
    
    private bool hasPlayedEmptySound = false; // Track if empty sound was played
    
    // Aiming
    private bool isAiming = false;
    private float defaultFOV;
    private Vector3 defaultPosition;
    
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
        if (cursorLocked && Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (isReloading)
            {
                // Play empty sound once when trying to shoot during reload
                if (!hasPlayedEmptySound)
                {
                    PlaySound(settings.emptySound);
                    hasPlayedEmptySound = true;
                }
            }
            else if (currentAmmo <= 0)
            {
                // Play empty sound once when magazine is empty
                if (!hasPlayedEmptySound)
                {
                    PlaySound(settings.emptySound);
                    hasPlayedEmptySound = true;
                }
            }
            else
            {
                Shoot();
            }
        }
        
        // Reset empty sound flag when button is released
        if (!Input.GetMouseButton(0))
        {
            hasPlayedEmptySound = false;
        }
        
        // Handle aiming
        if (cursorLocked && settings != null && settings.canAim)
        {
            if (Input.GetMouseButton(1))
            {
                isAiming = true;
            }
            else
            {
                isAiming = false;
            }
            
            HandleAiming();
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
        }
        else
        {
            recoilOffset = Vector3.zero;
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
        
        // Store default values for aiming
        defaultPosition = originalLocalPosition;
        if (playerCamera != null)
        {
            defaultFOV = playerCamera.fieldOfView;
        }
    }
    
    public void Unequip()
    {
        // Restore default FOV before unequipping
        if (playerCamera != null && defaultFOV > 0)
        {
            playerCamera.fieldOfView = defaultFOV;
        }
        
        isEquipped = false;
        isAiming = false;
        playerCamera = null;
    }
    
    private void Shoot()
    {
        // This should not be reached if ammo is 0, but check anyway
        if (currentAmmo <= 0) return;
        
        // Reset empty sound flag when successfully shooting
        hasPlayedEmptySound = false;
        
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
        
        // Eject casing
        EjectCasing();
        
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
        
        // Auto reload if empty and auto reload is enabled
        if (currentAmmo <= 0 && settings.autoReload)
        {
            StartReload();
        }
    }
    
    private void EjectCasing()
    {
        if (casingPrefab == null) return;
        
        // Use casing eject point if assigned, otherwise use fire point
        Vector3 ejectPosition = casingEjectPoint != null ? casingEjectPoint.position : firePoint.position;
        
        // Create casing
        GameObject casing = Instantiate(casingPrefab, ejectPosition, Random.rotation);
        
        // Calculate eject direction in world space
        Vector3 worldEjectDirection = transform.TransformDirection(casingEjectDirection.normalized);
        
        // Get BulletCasing component and eject
        BulletCasing casingScript = casing.GetComponent<BulletCasing>();
        if (casingScript != null)
        {
            casingScript.Eject(worldEjectDirection, casingEjectForce);
        }
    }
    
    private Vector3 GetShootDirection()
    {
        Vector3 direction;
        
        // Raycast from camera center to get accurate aim point
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            // Aim at the hit point
            direction = (hit.point - firePoint.position).normalized;
        }
        else
        {
            // No hit, use camera forward direction adjusted from fire point
            Vector3 targetPoint = playerCamera.transform.position + playerCamera.transform.forward * 1000f;
            direction = (targetPoint - firePoint.position).normalized;
        }
        
        // Add spread
        if (settings.spreadAngle > 0)
        {
            float spread = settings.spreadAngle;
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
        
        // Weapon recoil (kickback toward player) - use local Z axis, always backward
        recoilOffset = Vector3.back * settings.recoilKickback;
        transform.localPosition = originalLocalPosition + recoilOffset;
    }
    
    private void StartReload()
    {
        if (isReloading || currentAmmo >= settings.magSize) return;
        
        isReloading = true;
        
        // Play reload start sound
        PlaySound(settings.reloadStartSound);
        
        // Schedule reload end sound to play just before reload finishes
        if (settings.reloadEndSound != null)
        {
            float endSoundDelay = settings.reloadTime - settings.reloadEndSound.length;
            // Ensure delay is not negative
            endSoundDelay = Mathf.Max(endSoundDelay, 0f);
            Invoke(nameof(PlayReloadEndSound), endSoundDelay);
        }
        
        // Schedule reload finish
        Invoke(nameof(FinishReload), settings.reloadTime);
    }
    
    private void PlayReloadEndSound()
    {
        PlaySound(settings.reloadEndSound);
    }
    
    private void FinishReload()
    {
        currentAmmo = settings.magSize;
        isReloading = false;
        hasPlayedEmptySound = false; // Reset flag after reload
    }
    
    private void HandleAiming()
    {
        if (playerCamera == null || settings == null) return;
        
        // Target values
        float targetFOV = isAiming ? settings.aimFOV : defaultFOV;
        Vector3 targetPosition;
        
        if (isAiming)
        {
            // When aiming, use absolute position relative to itemHoldPoint (parent)
            // This ensures weapon is centered regardless of heldPosition from ItemPickup
            targetPosition = settings.aimPosition;
        }
        else
        {
            // When not aiming, use default position from when weapon was equipped
            targetPosition = defaultPosition;
        }
        
        // Smoothly interpolate FOV
        float lerpSpeed = settings.aimSpeed * Time.deltaTime;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, lerpSpeed);
        
        // Smoothly interpolate weapon position WITH recoil offset
        Vector3 targetPosWithRecoil = targetPosition + recoilOffset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosWithRecoil, lerpSpeed);
        
        // Update original position based on aiming state (for recoil calculations)
        originalLocalPosition = targetPosition;
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
