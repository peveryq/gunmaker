using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

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
    
    [Header("Barrel Ejection")]
    [Tooltip("Force applied when unwelded barrel is ejected during shooting")]
    [SerializeField] private float barrelEjectForce = 5f;
    [Tooltip("Torque applied to barrel for random rotation")]
    [SerializeField] private float barrelEjectTorque = 2f;
    
    private bool isEquipped = false;
    private int currentAmmo;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private Coroutine reloadCoroutine;
    private float reloadProgress;
    
    private Vector3 originalLocalPosition;
    private Vector3 recoilOffset = Vector3.zero;
    
    private bool hasPlayedEmptySound = false; // Track if empty sound was played
    
    // Aiming
    private bool isAiming = false;
    private float defaultFOV;
    private Vector3 defaultPosition;
    
    // Weapon Sway
    private Vector3 swayOffset = Vector3.zero;
    private float swayTimer = 0f;

    public event Action<int, int> AmmoChanged;
    public event Action<bool> ReloadStateChanged;
    public event Action<float> ReloadProgressChanged;

    private void NotifyAmmoChanged()
    {
        AmmoChanged?.Invoke(currentAmmo, MaxAmmo);
    }
    
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
        NotifyAmmoChanged();
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
        
        // Handle weapon sway when moving
        if (settings != null && settings.enableWeaponSway)
        {
            HandleWeaponSway();
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
        CancelReload();

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
        // Check if weapon can shoot (has barrel)
        WeaponBody weaponBody = GetComponent<WeaponBody>();
        if (weaponBody != null && !weaponBody.CanShoot())
        {
            // Play empty sound once (same logic as empty magazine)
            if (!hasPlayedEmptySound)
            {
                PlaySound(settings.emptySound);
                hasPlayedEmptySound = true;
            }
            return;
        }
        
        // Check if barrel is welded (if barrel exists)
        if (weaponBody != null)
        {
            WeaponPart barrel = weaponBody.GetPart(PartType.Barrel);
            if (barrel != null)
            {
                WeldingSystem weldingSystem = barrel.GetComponent<WeldingSystem>();
                if (weldingSystem != null && weldingSystem.RequiresWelding && !weldingSystem.IsWelded)
                {
                    // Barrel is not welded - eject it!
                    EjectBarrel(barrel, weaponBody);
                    return;
                }
            }
        }
        
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
        
        // Trigger crosshair animation
        if (GameplayHUD.Instance != null)
        {
            GameplayHUD.Instance.TriggerCrosshairShot();
        }
        
        // Update ammo and fire rate
        currentAmmo--;
        NotifyAmmoChanged();
        nextFireTime = Time.time + settings.fireRate;
        
        // Auto reload if empty and auto reload is enabled
        if (currentAmmo <= 0 && settings.autoReload)
        {
            StartReload();
        }
    }

    public void SetSettings(WeaponSettings newSettings)
    {
        CancelReload();
        settings = newSettings;

        if (settings == null)
        {
            Debug.LogError($"WeaponController on {name} received null WeaponSettings reference.");
            enabled = false;
            return;
        }

        enabled = true;
        currentAmmo = settings.magSize;
        NotifyAmmoChanged();
    }
    
    private void EjectBarrel(WeaponPart barrel, WeaponBody weaponBody)
    {
        // Play single shot sound
        PlaySound(settings.shootSound);
        
        // Use barrel's current position or firepoint as eject position
        Vector3 ejectPosition = barrel.transform.position;
        Vector3 ejectDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        
        // Manually remove barrel (set to null and destroy link)
        // We'll use reflection-like approach: detach barrel from parent
        barrel.transform.SetParent(null);
        
        // Re-enable physics and interaction
        Rigidbody barrelRb = barrel.GetComponent<Rigidbody>();
        if (barrelRb != null)
        {
            barrelRb.isKinematic = false;
            barrelRb.useGravity = true;
            
            // Apply ejection force
            barrelRb.AddForce(ejectDirection * barrelEjectForce, ForceMode.Impulse);
            barrelRb.AddTorque(Random.insideUnitSphere * barrelEjectTorque, ForceMode.Impulse);
        }
        
        // Re-enable collider
        Collider barrelCol = barrel.GetComponent<Collider>();
        if (barrelCol != null)
        {
            barrelCol.enabled = true;
        }
        
        // Re-enable ItemPickup
        ItemPickup barrelPickup = barrel.GetComponent<ItemPickup>();
        if (barrelPickup != null)
        {
            barrelPickup.enabled = true;
            barrelPickup.SetHeldState(false);
        }
        
        // Remove barrel from weapon body slot
        weaponBody.RemovePart(PartType.Barrel);
        
        // Weapon is now in "no barrel" state
        // Empty sound will play on next shot attempt
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
        // Start raycast slightly forward to avoid hitting own weapon
        Vector3 rayStart = playerCamera.transform.position + playerCamera.transform.forward * 0.1f;
        Ray ray = new Ray(rayStart, playerCamera.transform.forward);
        RaycastHit hit;
        
        // Ignore own weapon layer or use longer raycast start
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            // Check if we hit ourselves (weapon/player)
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(playerCamera.transform))
            {
                // Hit own weapon, use camera forward instead
                Vector3 targetPoint = playerCamera.transform.position + playerCamera.transform.forward * 1000f;
                direction = (targetPoint - firePoint.position).normalized;
            }
            else
            {
                // Aim at the hit point
                direction = (hit.point - firePoint.position).normalized;
            }
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
        WeaponBody weaponBody = GetComponent<WeaponBody>();
        if (weaponBody != null && !weaponBody.CanReload())
        {
            PlaySound(settings.emptySound);
            return;
        }

        if (!isEquipped || isReloading || settings == null || currentAmmo >= settings.magSize)
        {
            return;
        }

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        isReloading = true;
        hasPlayedEmptySound = false;

        ReloadStateChanged?.Invoke(true);
        UpdateReloadProgress(0f);

        PlaySound(settings.reloadStartSound);

        float duration = Mathf.Max(settings.reloadTime, 0f);
        if (duration <= Mathf.Epsilon)
        {
            FinishReload();
            return;
        }

        reloadCoroutine = StartCoroutine(ReloadRoutine(duration));
    }

    private IEnumerator ReloadRoutine(float duration)
    {
        float elapsed = 0f;
        bool playedEndSound = settings.reloadEndSound == null;
        float endSoundTriggerTime = 0f;

        if (settings.reloadEndSound != null)
        {
            endSoundTriggerTime = Mathf.Max(duration - settings.reloadEndSound.length, 0f);
        }

        while (elapsed < duration)
        {
            if (!isEquipped)
            {
                reloadCoroutine = null;
                ResetReloadState(false);
                yield break;
            }

            elapsed += Time.deltaTime;
            UpdateReloadProgress(elapsed / duration);

            if (!playedEndSound && elapsed >= endSoundTriggerTime)
            {
                PlaySound(settings.reloadEndSound);
                playedEndSound = true;
            }

            yield return null;
        }

        reloadCoroutine = null;
        FinishReload();
    }

    public void CancelReload()
    {
        if (!isReloading)
        {
            return;
        }

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        ResetReloadState(false);
    }

    private void FinishReload()
    {
        if (!isReloading)
        {
            return;
        }

        currentAmmo = settings.magSize;
        NotifyAmmoChanged();
        ResetReloadState(true);
    }

    private void ResetReloadState(bool completed)
    {
        isReloading = false;
        hasPlayedEmptySound = false;
        reloadCoroutine = null;
        UpdateReloadProgress(completed ? 1f : 0f);
        ReloadStateChanged?.Invoke(false);
    }

    private void UpdateReloadProgress(float value)
    {
        reloadProgress = Mathf.Clamp01(value);
        ReloadProgressChanged?.Invoke(reloadProgress);
    }
    
    private void HandleWeaponSway()
    {
        // Get movement info from FirstPersonController
        FirstPersonController fpsController = playerCamera.GetComponentInParent<FirstPersonController>();
        bool isMoving = false;
        bool isRunning = false;
        
        if (fpsController != null)
        {
            isMoving = fpsController.IsMoving;
            isRunning = fpsController.IsRunning;
        }
        
        if (isMoving && !isAiming) // Sway only when moving and not aiming
        {
            // Increase timer for sway animation
            float swaySpeed = isRunning ? settings.swaySpeed * 1.5f : settings.swaySpeed;
            swayTimer += swaySpeed * Time.deltaTime;
            
            // Calculate sway offset using sine waves
            float swayX = Mathf.Sin(swayTimer) * settings.swayAmount;
            float swayY = Mathf.Sin(swayTimer * 2f) * settings.swayAmount * 0.5f; // Vertical sway at double frequency
            
            Vector3 targetSway = new Vector3(swayX, swayY, 0f);
            swayOffset = Vector3.Lerp(swayOffset, targetSway, settings.swaySmoothing * Time.deltaTime);
        }
        else
        {
            // Smoothly return to zero when not moving or when aiming
            swayOffset = Vector3.Lerp(swayOffset, Vector3.zero, settings.swaySmoothing * Time.deltaTime);
            if (!isMoving)
            {
                swayTimer = 0f;
            }
        }
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
        
        // Smoothly interpolate weapon position WITH recoil offset AND sway offset
        Vector3 targetPosWithOffsets = targetPosition + recoilOffset + swayOffset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosWithOffsets, lerpSpeed);
        
        // Update original position based on aiming state (for recoil calculations)
        originalLocalPosition = targetPosition;
    }

    private void OnDisable()
    {
        CancelReload();
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
    
    public void RefreshAmmo()
    {
        // Called when weapon parts change
        if (settings != null)
        {
            currentAmmo = Mathf.Min(currentAmmo, settings.magSize);
            NotifyAmmoChanged();
        }
    }
    
    public bool IsEquipped => isEquipped;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => settings != null ? settings.magSize : 0;
    public bool IsReloading => isReloading;
    public float ReloadProgress => reloadProgress;
}
