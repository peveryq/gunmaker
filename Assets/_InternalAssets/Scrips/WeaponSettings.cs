using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponSettings", menuName = "Weapon/Weapon Settings")]
public class WeaponSettings : ScriptableObject
{
    [Header("Shooting")]
    public float fireRate = 0.1f; // Time between shots
    public float bulletSpeed = 50f;
    public float bulletDamage = 10f;
    public float spreadAngle = 2f; // Degrees
    
    [Header("Recoil")]
    public float recoilUpward = 0.5f; // Camera kick up
    public float recoilKickback = 0.1f; // Weapon kick back toward player
    public float recoilRecoverySpeed = 5f; // How fast weapon returns to position
    
    [Header("Ammo")]
    public int magSize = 15;
    public float reloadTime = 2f;
    public bool autoReload = false; // Auto reload when magazine is empty
    
    [Header("Aiming")]
    public bool canAim = true;
    public float aimFOV = 40f; // Field of view when aiming
    public float aimSpeed = 8f; // How fast to zoom in/out
    [Tooltip("Weapon position when aiming. Use same X as heldPosition for consistent centering. Y: up/down, Z: closer/farther")]
    public Vector3 aimPosition = new Vector3(0f, -0.2f, 0.4f); // Position when aiming (X should match heldPosition.x)
    
    [Header("Effects")]
    public GameObject muzzleFlash;
    public AudioClip shootSound;
    public AudioClip reloadStartSound;
    public AudioClip reloadEndSound;
    public AudioClip emptySound;
}
