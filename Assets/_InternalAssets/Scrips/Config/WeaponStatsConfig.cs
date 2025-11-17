using UnityEngine;

/// <summary>
/// Configuration for weapon stats conversion thresholds.
/// Defines min/max values for converting stats (1-100) to actual weapon parameters.
/// </summary>
[CreateAssetMenu(menuName = "Gunmaker/Weapon Stats Config", fileName = "WeaponStatsConfig")]
public class WeaponStatsConfig : ScriptableObject
{
    private static WeaponStatsConfig instance;

    public static WeaponStatsConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<WeaponStatsConfig>("WeaponStatsConfig");
                if (instance == null)
                {
                    Debug.LogError("WeaponStatsConfig asset not found in Resources. Please create one via Assets > Create > Gunmaker > Weapon Stats Config and place in Resources folder.");
                }
            }
            return instance;
        }
    }

    [Header("Bullet Speed (Power)")]
    [Tooltip("Bullet speed when power stat = 1")]
    public float bulletSpeedMin = 50f;
    [Tooltip("Bullet speed when power stat = 100")]
    public float bulletSpeedMax = 300f;

    [Header("Spread Angle (Accuracy)")]
    [Tooltip("Spread angle (degrees) when accuracy stat = 1")]
    public float spreadAngleMin = 7f;
    [Tooltip("Spread angle (degrees) when accuracy stat = 100")]
    public float spreadAngleMax = 0f;

    [Header("Fire Rate (Rapidity)")]
    [Tooltip("Time between shots (seconds) when rapidity stat = 1")]
    public float fireRateMin = 0.5f;
    [Tooltip("Time between shots (seconds) when rapidity stat = 100")]
    public float fireRateMax = 0.05f;

    [Header("Recoil Upward")]
    [Tooltip("Camera recoil upward (degrees) when recoil stat = 1")]
    public float recoilUpwardMin = 0f;
    [Tooltip("Camera recoil upward (degrees) when recoil stat = 100")]
    public float recoilUpwardMax = 3f;

    [Header("Recoil Kickback")]
    [Tooltip("Weapon kickback when recoil stat = 1")]
    public float recoilKickbackMin = 0.05f;
    [Tooltip("Weapon kickback when recoil stat = 100")]
    public float recoilKickbackMax = 0.15f;

    [Header("Reload Time (Reload Speed)")]
    [Tooltip("Reload time (seconds) when reloadSpeed stat = 1")]
    public float reloadTimeMin = 3f;
    [Tooltip("Reload time (seconds) when reloadSpeed stat = 100")]
    public float reloadTimeMax = 1f;

    [Header("Aim FOV (Scope)")]
    [Tooltip("Aim FOV (degrees) when scope stat = 1")]
    public float aimFOVMin = 45f;
    [Tooltip("Aim FOV (degrees) when scope stat = 100")]
    public float aimFOVMax = 5f;
}

