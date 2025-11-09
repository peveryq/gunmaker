using UnityEngine;
using System.Collections.Generic;

public class WeaponBody : MonoBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] private string weaponName = "Custom Weapon";
    
    [Header("Base Stats")]
    [SerializeField] private WeaponStats baseStats = new WeaponStats();
    
    [Header("Installed Parts")]
    [SerializeField] private WeaponPart barrelPart;
    [SerializeField] private WeaponPart magazinePart;
    [SerializeField] private WeaponPart stockPart;
    [SerializeField] private WeaponPart scopePart;
    
    [Header("References")]
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private WeaponSettings weaponSettings;
    
    private WeaponStats currentStats;
    
    // Properties
    public string WeaponName => weaponName;
    
    private void Start()
    {
        // Get references if not assigned
        if (weaponController == null)
        {
            weaponController = GetComponent<WeaponController>();
        }
        
        UpdateWeaponStats();
    }
    
    // Install a part on the weapon
    public bool InstallPart(WeaponPart part)
    {
        if (part == null) return false;
        
        WeaponPart oldPart = null;
        
        // Replace part based on type
        switch (part.Type)
        {
            case PartType.Barrel:
                oldPart = barrelPart;
                barrelPart = part;
                break;
            case PartType.Magazine:
                oldPart = magazinePart;
                magazinePart = part;
                break;
            case PartType.Stock:
                oldPart = stockPart;
                stockPart = part;
                break;
            case PartType.Scope:
                oldPart = scopePart;
                scopePart = part;
                break;
        }
        
        // Destroy old part if exists
        if (oldPart != null)
        {
            Destroy(oldPart.gameObject);
        }
        
        // Parent new part to weapon body
        part.transform.SetParent(transform);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        
        // Disable physics and interaction on installed part
        Rigidbody rb = part.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        Collider col = part.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Disable ItemPickup on part
        ItemPickup pickup = part.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.enabled = false;
        }
        
        // Update weapon stats
        UpdateWeaponStats();
        
        // Update outline to include new part
        AutoOutline autoOutline = GetComponent<AutoOutline>();
        if (autoOutline != null)
        {
            autoOutline.RefreshOutline();
        }
        
        return true;
    }
    
    // Calculate current stats based on installed parts
    public void UpdateWeaponStats()
    {
        currentStats = CalculateCombinedStats(barrelPart, magazinePart, stockPart, scopePart);
        
        // Apply stats to weapon settings if available
        if (weaponSettings != null)
        {
            currentStats.ApplyToSettings(weaponSettings);
        }
        
        // Update weapon controller ammo
        if (weaponController != null)
        {
            weaponController.RefreshAmmo();
        }
    }
    
    // Check if weapon can function
    public bool CanShoot()
    {
        return barrelPart != null; // Need barrel to shoot
    }
    
    public bool CanReload()
    {
        return magazinePart != null; // Need magazine to reload
    }
    
    // Get part in slot
    public WeaponPart GetPart(PartType type)
    {
        switch (type)
        {
            case PartType.Barrel: return barrelPart;
            case PartType.Magazine: return magazinePart;
            case PartType.Stock: return stockPart;
            case PartType.Scope: return scopePart;
            default: return null;
        }
    }
    
    // Remove part from slot (without destroying it)
    public void RemovePart(PartType type)
    {
        switch (type)
        {
            case PartType.Barrel: barrelPart = null; break;
            case PartType.Magazine: magazinePart = null; break;
            case PartType.Stock: stockPart = null; break;
            case PartType.Scope: scopePart = null; break;
        }
        
        // Update stats after removal
        UpdateWeaponStats();
    }
    
    // Get stats description for UI
    public string GetStatsDescription()
    {
        if (currentStats == null) return "";
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"<b>{weaponName}</b>");
        sb.AppendLine();
        sb.AppendLine($"Power: {currentStats.power}");
        sb.AppendLine($"Accuracy: {currentStats.accuracy}");
        sb.AppendLine($"Rapidity: {currentStats.rapidity}");
        sb.AppendLine($"Recoil: {currentStats.recoil}");
        sb.AppendLine($"Reload Speed: {currentStats.reloadSpeed}");
        sb.AppendLine($"Scope: {currentStats.scope}");
        sb.AppendLine($"Ammo: {currentStats.ammo}");
        
        return sb.ToString();
    }
    
    // Properties
    public WeaponStats CurrentStats => currentStats;
    public bool HasBarrel => barrelPart != null;
    public bool HasMagazine => magazinePart != null;

    public bool TryCalculatePreviewStats(WeaponPart candidatePart, out WeaponStats previewStats)
    {
        previewStats = null;
        if (candidatePart == null) return false;
        
        WeaponPart previewBarrel = barrelPart;
        WeaponPart previewMagazine = magazinePart;
        WeaponPart previewStock = stockPart;
        WeaponPart previewScope = scopePart;
        bool partApplied = false;
        
        switch (candidatePart.Type)
        {
            case PartType.Barrel:
                previewBarrel = candidatePart;
                partApplied = true;
                break;
            case PartType.Magazine:
                previewMagazine = candidatePart;
                partApplied = true;
                break;
            case PartType.Stock:
                previewStock = candidatePart;
                partApplied = true;
                break;
            case PartType.Scope:
                previewScope = candidatePart;
                partApplied = true;
                break;
        }
        
        if (!partApplied)
        {
            return false;
        }
        
        previewStats = CalculateCombinedStats(previewBarrel, previewMagazine, previewStock, previewScope);
        return previewStats != null;
    }
    
    private WeaponStats CalculateCombinedStats(WeaponPart barrel, WeaponPart magazine, WeaponPart stock, WeaponPart scope)
    {
        WeaponStats combinedStats = baseStats.Clone();
        
        if (barrel != null) barrel.ApplyModifiers(combinedStats);
        if (magazine != null) magazine.ApplyModifiers(combinedStats);
        if (stock != null) stock.ApplyModifiers(combinedStats);
        if (scope != null) scope.ApplyModifiers(combinedStats);
        
        return combinedStats;
    }
}

