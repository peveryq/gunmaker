using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Links a part mesh with its corresponding icon sprite
/// </summary>
[System.Serializable]
public class PartMeshData
{
    [Tooltip("3D mesh for the part (from FBX file)")]
    public Mesh mesh;
    
    [Tooltip("2D icon sprite for UI display")]
    public Sprite icon;
    
    [Tooltip("Optional lens overlay prefab (for scopes). Will be instantiated as child with preserved transform.")]
    public GameObject lensOverlayPrefab;
}

/// <summary>
/// Defines which stats a part type influences
/// </summary>
[System.Serializable]
public class StatInfluence
{
    public enum StatType
    {
        Power,
        Accuracy,
        Rapidity,
        Recoil,
        ReloadSpeed,
        Aim,
        Ammo
    }
    
    public StatType stat;
}

/// <summary>
/// Rarity tier configuration with meshes and price range
/// </summary>
[System.Serializable]
public class RarityTier
{
    [Header("Rarity")]
    [Range(1, 5)] public int rarity = 1;
    
    [Header("Part Meshes & Icons")]
    [Tooltip("Part meshes with their corresponding icon sprites")]
    public List<PartMeshData> partMeshData = new List<PartMeshData>();
    
    [Header("Name Pool")]
    [Tooltip("Unique name fragments for this rarity tier (first part of the generated name)")]
    public List<string> partNamePool = new List<string>();
    
    [Header("Price Range")]
    public int minPrice = 20;
    public int maxPrice = 100;
    
    [Header("Stat Ranges (for non-ammo stats)")]
    [Tooltip("Minimum stat value for this tier (0-100)")]
    [Range(0, 100)] public int minStatValue = 0;
    [Tooltip("Maximum stat value for this tier (0-100)")]
    [Range(0, 100)] public int maxStatValue = 19;
    
    [Header("Ammo Range (for Magazine parts only)")]
    [Tooltip("Minimum ammo capacity for this tier")]
    public int minAmmo = 8;
    [Tooltip("Maximum ammo capacity for this tier")]
    public int maxAmmo = 12;
}

/// <summary>
/// Configuration for a specific part type
/// </summary>
[System.Serializable]
public class PartTypeConfig
{
    [Header("Part Type")]
    public PartType partType;
    [Tooltip("Display name for this part type (localized label). If empty defaults will be used.")]
    public string partTypeDisplayName = string.Empty;
    
    [Header("Rarity Tiers")]
    [Tooltip("Configure 5 rarity tiers (1-5 stars)")]
    public List<RarityTier> rarityTiers = new List<RarityTier>();
    
    [Header("Stat Influences")]
    [Tooltip("Which stats does this part type affect?")]
    public List<StatInfluence> statInfluences = new List<StatInfluence>();
    
    /// <summary>
    /// Get rarity tier by rarity level (1-5)
    /// </summary>
    public RarityTier GetRarityTier(int rarity)
    {
        foreach (RarityTier tier in rarityTiers)
        {
            if (tier.rarity == rarity)
                return tier;
        }
        
        // Fallback to first tier if not found
        return rarityTiers.Count > 0 ? rarityTiers[0] : null;
    }
    
    /// <summary>
    /// Check if this part type influences a specific stat
    /// </summary>
    public bool InfluencesStat(StatInfluence.StatType stat)
    {
        foreach (StatInfluence influence in statInfluences)
        {
            if (influence.stat == stat)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get random name fragment for a specific rarity tier
    /// </summary>
    public string GetRandomNameFragment(int rarity)
    {
        RarityTier tier = GetRarityTier(rarity);
        if (tier != null && tier.partNamePool != null && tier.partNamePool.Count > 0)
        {
            int index = Random.Range(0, tier.partNamePool.Count);
            return tier.partNamePool[index];
        }
        return null;
    }

    /// <summary>
    /// Get display label for this part type
    /// </summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(partTypeDisplayName))
        {
            return partTypeDisplayName;
        }

        switch (partType)
        {
            case PartType.Barrel:
                return "barrel";
            case PartType.Magazine:
                return "magazine";
            case PartType.Stock:
                return "stock";
            case PartType.Scope:
                return "scope";
            default:
                return partType.ToString().ToLowerInvariant();
        }
    }
}

/// <summary>
/// ScriptableObject configuration for shop part generation
/// </summary>
[CreateAssetMenu(fileName = "ShopPartConfig", menuName = "Gunmaker/Shop Part Config")]
public class ShopPartConfig : ScriptableObject
{
    [Header("Universal Part Prefab")]
    [Tooltip("Universal part prefab with empty mesh (will be populated with specific meshes)")]
    public GameObject universalPartPrefab;
    
    [Header("Part Type Configurations")]
    [Tooltip("Configure each part type separately")]
    public List<PartTypeConfig> partTypeConfigs = new List<PartTypeConfig>();
    
    [Header("Manufacturer Logos")]
    [Tooltip("Shared manufacturer logos for all parts")]
    public List<Sprite> manufacturerLogos = new List<Sprite>();
    
    [Header("UI Icons")]
    [Tooltip("Star icon for filled stars")]
    public Sprite filledStarIcon;
    [Tooltip("Star icon for empty stars")]
    public Sprite emptyStarIcon;
    
    [Header("Localization (Optional)")]
    [Tooltip("Part name localization data. If assigned, part names will be localized. If not, original names will be used.")]
    public PartNameLocalization partNameLocalization;
    
    /// <summary>
    /// Get configuration for a specific part type
    /// </summary>
    public PartTypeConfig GetPartTypeConfig(PartType type)
    {
        foreach (PartTypeConfig config in partTypeConfigs)
        {
            if (config.partType == type)
                return config;
        }
        
        Debug.LogWarning($"No configuration found for part type: {type}");
        return null;
    }
    
    /// <summary>
    /// Get a random name fragment for a specific part type and rarity
    /// </summary>
    public string GetRandomNameFragment(PartType type, int rarity)
    {
        PartTypeConfig config = GetPartTypeConfig(type);
        return config != null ? config.GetRandomNameFragment(rarity) : null;
    }
    
    /// <summary>
    /// Get display label for part type (lowercase string)
    /// </summary>
    public string GetPartTypeLabel(PartType type)
    {
        // Try to get localized part type name first
        if (partNameLocalization != null)
        {
            string localized = partNameLocalization.GetLocalizedPartTypeName(type);
            if (!string.IsNullOrEmpty(localized))
            {
                return localized;
            }
        }
        
        // Fallback to PartTypeConfig display name
        PartTypeConfig config = GetPartTypeConfig(type);
        return config != null ? config.GetDisplayName() : type.ToString().ToLowerInvariant();
    }
    
    /// <summary>
    /// Get localized part name (adjective + part type)
    /// </summary>
    /// <param name="type">Part type</param>
    /// <param name="rarity">Rarity tier (1-5)</param>
    /// <param name="originalAdjective">Original adjective from partNamePool</param>
    /// <returns>Localized part name or original if localization not available</returns>
    public string GetLocalizedPartName(PartType type, int rarity, string originalAdjective)
    {
        // Try to get localized name if PartNameLocalization is assigned
        if (partNameLocalization != null && !string.IsNullOrEmpty(originalAdjective))
        {
            return partNameLocalization.GetLocalizedPartName(type, rarity, originalAdjective);
        }
        
        // Fallback to original generation
        string firstPart = !string.IsNullOrEmpty(originalAdjective) ? originalAdjective : $"{rarity}-star";
        string typeLabel = GetPartTypeLabel(type);
        return !string.IsNullOrWhiteSpace(typeLabel) ? $"{firstPart} {typeLabel}" : firstPart;
    }
    
    /// <summary>
    /// Get random manufacturer logo
    /// </summary>
    public Sprite GetRandomManufacturerLogo()
    {
        if (manufacturerLogos.Count == 0)
            return null;
        
        return manufacturerLogos[Random.Range(0, manufacturerLogos.Count)];
    }
}

