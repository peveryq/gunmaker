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
    
    [Header("Effects")]
    public GameObject muzzleFlash;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
}
