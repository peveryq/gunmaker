using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Data for a single shop offering (Phase 1: basic info)
/// </summary>
[System.Serializable]
public class ShopOffering
{
    public int rarity; // 1-5 stars
    public int price;
    public Mesh partMesh; // Mesh to apply to universal prefab
    public Sprite partIcon; // Icon sprite for UI display
    public Sprite manufacturerLogo;
    public PartType partType;
    
    // Cached stats (Phase 2: calculated on-demand)
    private Dictionary<StatInfluence.StatType, float> cachedStats;
    private bool statsCalculated = false;
    private string cachedName;
    private bool nameGenerated = false;
    private bool hasCustomStats = false;
    private Dictionary<StatInfluence.StatType, float> customStats;
    private string overrideName;
    
    public Dictionary<StatInfluence.StatType, float> GetStats(ShopPartConfig config)
    {
        if (hasCustomStats && customStats != null)
            return customStats;
        
        if (statsCalculated && cachedStats != null)
            return cachedStats;
        
        cachedStats = CalculateStats(config);
        statsCalculated = true;
        return cachedStats;
    }
    
    /// <summary>
    /// Phase 2: Calculate exact stats based on rarity and price
    /// Formula: x = a + ((b - a) * ((e - c) / (d - c)))
    /// </summary>
    private Dictionary<StatInfluence.StatType, float> CalculateStats(ShopPartConfig config)
    {
        Dictionary<StatInfluence.StatType, float> stats = new Dictionary<StatInfluence.StatType, float>();
        
        PartTypeConfig partConfig = config.GetPartTypeConfig(partType);
        if (partConfig == null)
            return stats;
        
        RarityTier tier = partConfig.GetRarityTier(rarity);
        if (tier == null)
            return stats;
        
        // Get price range for this rarity
        int c = tier.minPrice; // Lower bound of price range
        int d = tier.maxPrice; // Upper bound of price range
        int e = price; // Actual price
        
        // Calculate each influenced stat
        foreach (StatInfluence influence in partConfig.statInfluences)
        {
            float statValue;
            
            if (influence.stat == StatInfluence.StatType.Ammo)
            {
                // Special calculation for ammo
                int a = tier.minAmmo;
                int b = tier.maxAmmo;
                
                if (d == c) // Prevent division by zero
                {
                    statValue = b;
                }
                else
                {
                    statValue = a + ((b - a) * ((e - c) / (float)(d - c)));
                }
                
                statValue = Mathf.Ceil(statValue); // Round up for ammo
            }
            else if (influence.stat == StatInfluence.StatType.Recoil)
            {
                // Recoil is inverted (negative value)
                int a = tier.minStatValue;
                int b = tier.maxStatValue;
                
                if (d == c) // Prevent division by zero
                {
                    statValue = -b;
                }
                else
                {
                    float x = a + ((b - a) * ((e - c) / (float)(d - c)));
                    statValue = -Mathf.Ceil(x); // Negative and round up
                }
            }
            else
            {
                // Standard stat calculation
                int a = tier.minStatValue;
                int b = tier.maxStatValue;
                
                if (d == c) // Prevent division by zero
                {
                    statValue = b;
                }
                else
                {
                    statValue = a + ((b - a) * ((e - c) / (float)(d - c)));
                }
                
                statValue = Mathf.Ceil(statValue); // Round up
            }
            
            stats[influence.stat] = statValue;
        }
        
        return stats;
    }
    
    /// <summary>
    /// Reset cached stats (for refresh)
    /// </summary>
    public void ResetStats()
    {
        cachedStats = null;
        statsCalculated = false;
    }

    /// <summary>
    /// Get or generate a display name for this offering
    /// </summary>
    public string GetGeneratedName(ShopPartConfig config)
    {
        if (!string.IsNullOrWhiteSpace(overrideName))
            return overrideName;
        
        if (nameGenerated && !string.IsNullOrEmpty(cachedName))
            return cachedName;
        
        string firstPart = null;
        if (config != null)
        {
            firstPart = config.GetRandomNameFragment(partType, rarity);
        }
        
        if (string.IsNullOrWhiteSpace(firstPart))
        {
            firstPart = $"{rarity}-star";
        }
        
        string typeLabel = config != null ? config.GetPartTypeLabel(partType) : partType.ToString().ToLowerInvariant();
        string combinedName = string.IsNullOrWhiteSpace(typeLabel) ? firstPart : $"{firstPart} {typeLabel}";
        cachedName = combinedName.Trim();
        nameGenerated = true;
        return cachedName;
    }

    /// <summary>
    /// Reset cached name (when refreshing offerings)
    /// </summary>
    public void ResetName()
    {
        cachedName = null;
        nameGenerated = false;
    }
    
    /// <summary>
    /// Assign custom stat overrides (skips automatic calculation)
    /// </summary>
    public void SetCustomStats(Dictionary<StatInfluence.StatType, float> stats)
    {
        customStats = stats != null ? new Dictionary<StatInfluence.StatType, float>(stats) : null;
        hasCustomStats = customStats != null;
    }
    
    /// <summary>
    /// Force specific display name
    /// </summary>
    public void SetOverrideName(string name)
    {
        overrideName = name;
        cachedName = null;
        nameGenerated = false;
    }
}

/// <summary>
/// Generates random shop offerings for each category
/// </summary>
public class ShopOfferingGenerator : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ShopPartConfig shopConfig;
    
    [Header("Settings")]
    [SerializeField] private int offeringsPerCategory = 15;
    
    // Current offerings per part type
    private Dictionary<PartType, List<ShopOffering>> currentOfferings = new Dictionary<PartType, List<ShopOffering>>();
    
    private void Awake()
    {
        // Initialize all categories
        RefreshAllCategories();
    }
    
    /// <summary>
    /// Refresh offerings for all categories
    /// </summary>
    public void RefreshAllCategories()
    {
        RefreshOfferings(PartType.Barrel);
        RefreshOfferings(PartType.Magazine);
        RefreshOfferings(PartType.Stock);
        RefreshOfferings(PartType.Scope);
    }
    
    /// <summary>
    /// Phase 1: Generate random offerings for a specific category
    /// Generates: rarity, price, part mesh, manufacturer logo
    /// </summary>
    public void RefreshOfferings(PartType partType)
    {
        if (shopConfig == null)
        {
            Debug.LogError("ShopPartConfig is not assigned! Cannot generate offerings.");
            currentOfferings[partType] = new List<ShopOffering>();
            return;
        }
        
        PartTypeConfig partConfig = shopConfig.GetPartTypeConfig(partType);
        if (partConfig == null)
        {
            Debug.LogWarning($"No config for part type: {partType}. Creating empty offerings list.");
            currentOfferings[partType] = new List<ShopOffering>();
            return;
        }
        
        List<ShopOffering> offerings = new List<ShopOffering>();
        int targetCount = Mathf.Max(offeringsPerCategory, 1);

        if (RequiresStarterOffering(partType))
        {
            ShopOffering starter = CreateStarterOffering(partType, partConfig);
            if (starter != null)
            {
                offerings.Add(starter);
            }
        }

        int safety = 0;
        int maxAttempts = Mathf.Max(offeringsPerCategory * 5, offeringsPerCategory + 5);
        while (offerings.Count < targetCount && safety < maxAttempts)
        {
            ShopOffering offering = GenerateOffering(partType, partConfig);
            if (offering != null)
            {
                offerings.Add(offering);
            }
            safety++;
        }
        
        currentOfferings[partType] = offerings;
    }

    /// <summary>
    /// Remove a specific offering after purchase
    /// </summary>
    public bool RemoveOffering(PartType partType, ShopOffering offering)
    {
        if (offering == null)
            return false;

        if (currentOfferings.TryGetValue(partType, out List<ShopOffering> offerings) && offerings != null)
        {
            return offerings.Remove(offering);
        }

        return false;
    }
    
    /// <summary>
    /// Generate a single offering (Phase 1)
    /// </summary>
    private ShopOffering GenerateOffering(PartType partType, PartTypeConfig partConfig)
    {
        // Random rarity (1-5)
        int rarity = Random.Range(1, 6);
        
        // Get rarity tier
        RarityTier tier = partConfig.GetRarityTier(rarity);
        if (tier == null || tier.partMeshData.Count == 0)
        {
            Debug.LogWarning($"No mesh data for rarity {rarity} in {partType}");
            return null;
        }
        
        // Random price within rarity range
        int price = Random.Range(tier.minPrice, tier.maxPrice + 1);
        
        // Random part mesh data from rarity tier
        PartMeshData meshData = tier.partMeshData[Random.Range(0, tier.partMeshData.Count)];
        
        // Random manufacturer logo
        Sprite manufacturerLogo = shopConfig.GetRandomManufacturerLogo();
        
        return new ShopOffering
        {
            rarity = rarity,
            price = price,
            partMesh = meshData.mesh,
            partIcon = meshData.icon,
            manufacturerLogo = manufacturerLogo,
            partType = partType
        };
    }

    private bool RequiresStarterOffering(PartType partType)
    {
        return partType == PartType.Barrel || partType == PartType.Magazine;
    }

    private ShopOffering CreateStarterOffering(PartType partType, PartTypeConfig partConfig)
    {
        if (partConfig == null)
            return null;

        RarityTier baseTier = partConfig.GetRarityTier(1);
        if (baseTier == null)
        {
            Debug.LogWarning($"No rarity tier 1 configured for {partType}, starter offering skipped.");
            return null;
        }

        PartMeshData meshData = (baseTier.partMeshData != null && baseTier.partMeshData.Count > 0)
            ? baseTier.partMeshData[0]
            : null;

        if (meshData == null || meshData.mesh == null)
        {
            Debug.LogWarning($"No mesh data available for starter offering in {partType} tier 1.");
            return null;
        }

        ShopOffering starter = new ShopOffering
        {
            rarity = Mathf.Clamp(baseTier.rarity, 1, 5),
            price = 0,
            partMesh = meshData.mesh,
            partIcon = meshData.icon,
            manufacturerLogo = shopConfig != null ? shopConfig.GetRandomManufacturerLogo() : null,
            partType = partType
        };

        Dictionary<StatInfluence.StatType, float> stats = new Dictionary<StatInfluence.StatType, float>();
        foreach (StatInfluence influence in partConfig.statInfluences)
        {
            if (!stats.ContainsKey(influence.stat))
            {
                float value = 0f;
                
                // Magazine starter should have minimum ammo capacity
                if (partType == PartType.Magazine && influence.stat == StatInfluence.StatType.Ammo)
                {
                    value = 8f;
                }
                
                stats[influence.stat] = value;
            }
        }
        starter.SetCustomStats(stats);

        string firstPart = partConfig.GetRandomNameFragment(baseTier.rarity);
        if (string.IsNullOrWhiteSpace(firstPart))
        {
            firstPart = "basic";
        }
        string typeLabel = shopConfig != null ? shopConfig.GetPartTypeLabel(partType) : partType.ToString().ToLowerInvariant();
        string starterName = string.IsNullOrWhiteSpace(typeLabel) ? firstPart : $"{firstPart} {typeLabel}";
        starter.SetOverrideName(starterName.Trim());

        return starter;
    }
    
    /// <summary>
    /// Get all offerings for a specific category
    /// </summary>
    public List<ShopOffering> GetOfferings(PartType partType)
    {
        // Lazy initialization: если offerings не сгенерированы, генерируем сейчас
        if (!currentOfferings.ContainsKey(partType) || currentOfferings[partType] == null || currentOfferings[partType].Count == 0)
        {
            RefreshOfferings(partType);
        }
        
        if (currentOfferings.ContainsKey(partType))
        {
            return currentOfferings[partType];
        }
        
        Debug.LogWarning($"Failed to generate offerings for {partType}!");
        return new List<ShopOffering>();
    }
    
    /// <summary>
    /// Get a specific offering by index
    /// </summary>
    public ShopOffering GetOffering(PartType partType, int index)
    {
        List<ShopOffering> offerings = GetOfferings(partType);
        
        if (index >= 0 && index < offerings.Count)
        {
            return offerings[index];
        }
        
        return null;
    }
    
    /// <summary>
    /// Get stats for a specific offering (Phase 2: on-demand calculation)
    /// </summary>
    public Dictionary<StatInfluence.StatType, float> GetOfferingStats(PartType partType, int index)
    {
        ShopOffering offering = GetOffering(partType, index);
        
        if (offering != null && shopConfig != null)
        {
            return offering.GetStats(shopConfig);
        }
        
        return new Dictionary<StatInfluence.StatType, float>();
    }
    
    // Properties
    public ShopPartConfig Config => shopConfig;
}

