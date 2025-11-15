using UnityEngine;

[System.Serializable]
public class WeaponStats
{
    [Header("Base Stats (1-100)")]
    [Range(1, 100)] public float power = 1f;
    [Range(1, 100)] public float accuracy = 1f;
    [Range(1, 100)] public float rapidity = 1f;
    [Range(1, 100)] public float recoil = 100f;
    [Range(1, 100)] public float reloadSpeed = 1f;
    [Range(1, 100)] public float scope = 1f;
    public int ammo = 0;
    [Tooltip("Base damage dealt by weapon. Can be configured in weapon body prefab.")]
    public float damage = 10f;

    [Header("Economy")]
    [Tooltip("Accumulated cost of all installed parts contributing to these stats.")]
    public int totalPartCost = 0;
    
    // Convert stats to weapon settings values
    public float GetBulletSpeed()
    {
        // power: 1->30, 100->300
        return Mathf.Lerp(50f, 300f, (power - 1f) / 99f);
    }
    
    public float GetSpreadAngle()
    {
        // accuracy: 1->7, 100->0
        return Mathf.Lerp(7f, 0f, (accuracy - 1f) / 99f);
    }
    
    public float GetFireRate()
    {
        // rapidity: 1->0.5, 100->0.05
        return Mathf.Lerp(0.5f, 0.05f, (rapidity - 1f) / 99f);
    }
    
    public float GetRecoilUpward()
    {
        // recoil: 1->0, 100->3
        return Mathf.Lerp(0f, 3f, (recoil - 1f) / 99f);
    }
    
    public float GetRecoilKickback()
    {
        // recoil: 1->0.01, 100->0.4
        return Mathf.Lerp(0.05f, 0.15f, (recoil - 1f) / 99f);
    }
    
    public float GetReloadTime()
    {
        // reloadSpeed: 1->3, 100->1
        return Mathf.Lerp(3f, 1f, (reloadSpeed - 1f) / 99f);
    }
    
    public float GetAimFOV()
    {
        // scope: 1->40, 100->10
        return Mathf.Lerp(45f, 5f, (scope - 1f) / 99f);
    }
    
    public int GetMagSize()
    {
        return ammo;
    }
    
    // Apply these stats to WeaponSettings
    public void ApplyToSettings(WeaponSettings settings)
    {
        if (settings == null) return;
        
        settings.bulletSpeed = GetBulletSpeed();
        settings.spreadAngle = GetSpreadAngle();
        settings.fireRate = GetFireRate();
        settings.recoilUpward = GetRecoilUpward();
        settings.recoilKickback = GetRecoilKickback();
        settings.reloadTime = GetReloadTime();
        settings.aimFOV = GetAimFOV();
        settings.magSize = GetMagSize();
        settings.bulletDamage = GetDamage();
        settings.totalPartCost = totalPartCost;
    }
    
    public float GetDamage()
    {
        return damage;
    }
    
    // Clone stats
    public WeaponStats Clone()
    {
        return new WeaponStats
        {
            power = this.power,
            accuracy = this.accuracy,
            rapidity = this.rapidity,
            recoil = this.recoil,
            reloadSpeed = this.reloadSpeed,
            scope = this.scope,
            ammo = this.ammo,
            damage = this.damage,
            totalPartCost = this.totalPartCost
        };
    }
}

