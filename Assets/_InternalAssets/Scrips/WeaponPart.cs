using UnityEngine;

public enum PartType
{
    Barrel,
    Magazine,
    Stock,  // Butt
    Scope
}

public class WeaponPart : MonoBehaviour
{
    [Header("Part Info")]
    public PartType partType;
    public string partName = "Part";
    
    [Header("Stat Modifiers")]
    [Tooltip("Change in power (-100 to +100)")]
    [Range(-100, 100)] [SerializeField] private float powerModifier = 0f;
    
    [Tooltip("Change in accuracy (-100 to +100)")]
    [Range(-100, 100)] [SerializeField] private float accuracyModifier = 0f;
    
    [Tooltip("Change in rapidity (-100 to +100)")]
    [Range(-100, 100)] [SerializeField] private float rapidityModifier = 0f;
    
    [Tooltip("Change in recoil (-100 to +100)")]
    [Range(-100, 100)] [SerializeField] private float recoilModifier = 0f;
    
    [Tooltip("Change in reload speed (-100 to +100)")]
    [Range(-100, 100)] [SerializeField] private float reloadSpeedModifier = 0f;
    
    [Tooltip("Change in scope zoom (-100 to +100)")]
    [Range(-100, 100)] [SerializeField] private float scopeModifier = 0f;
    
    [Tooltip("Magazine capacity (only for Magazine parts)")]
    [SerializeField] private int magazineCapacity = 15;
    
    // Apply this part's modifiers to weapon stats
    public void ApplyModifiers(WeaponStats stats)
    {
        if (stats == null) return;
        
        stats.power = Mathf.Clamp(stats.power + powerModifier, 1f, 100f);
        stats.accuracy = Mathf.Clamp(stats.accuracy + accuracyModifier, 1f, 100f);
        stats.rapidity = Mathf.Clamp(stats.rapidity + rapidityModifier, 1f, 100f);
        stats.recoil = Mathf.Clamp(stats.recoil + recoilModifier, 1f, 100f);
        stats.reloadSpeed = Mathf.Clamp(stats.reloadSpeed + reloadSpeedModifier, 1f, 100f);
        stats.scope = Mathf.Clamp(stats.scope + scopeModifier, 1f, 100f);
        
        // Magazine sets ammo capacity
        if (partType == PartType.Magazine)
        {
            stats.ammo = magazineCapacity;
        }
    }
    
    // Get description of modifiers for UI
    public string GetModifierDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"<b>{partName}</b>");
        sb.AppendLine();
        
        if (powerModifier != 0) 
            sb.AppendLine($"Power: {(powerModifier > 0 ? "+" : "")}{powerModifier}");
        if (accuracyModifier != 0) 
            sb.AppendLine($"Accuracy: {(accuracyModifier > 0 ? "+" : "")}{accuracyModifier}");
        if (rapidityModifier != 0) 
            sb.AppendLine($"Rapidity: {(rapidityModifier > 0 ? "+" : "")}{rapidityModifier}");
        if (recoilModifier != 0) 
            sb.AppendLine($"Recoil: {(recoilModifier > 0 ? "+" : "")}{recoilModifier}");
        if (reloadSpeedModifier != 0) 
            sb.AppendLine($"Reload Speed: {(reloadSpeedModifier > 0 ? "+" : "")}{reloadSpeedModifier}");
        if (scopeModifier != 0) 
            sb.AppendLine($"Scope: {(scopeModifier > 0 ? "+" : "")}{scopeModifier}");
        if (partType == PartType.Magazine) 
            sb.AppendLine($"Ammo: {magazineCapacity}");
        
        return sb.ToString();
    }
    
    // Properties
    public PartType Type => partType;
    public string PartName => partName;
}

