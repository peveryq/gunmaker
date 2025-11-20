using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject for storing translations of part names (adjectives and part types).
/// Separated from ShopPartConfig to keep it clean and manageable.
/// </summary>
[CreateAssetMenu(fileName = "PartNameLocalization", menuName = "Localization/Part Name Localization")]
public class PartNameLocalization : ScriptableObject
{
    /// <summary>
    /// Translation for part type name (e.g., "barrel" → "Ствол" / "Barrel")
    /// </summary>
    [Serializable]
    public class PartTypeName
    {
        [Tooltip("Part type")]
        public PartType partType;
        
        [Tooltip("Russian translation of part type name")]
        public string russian;
        
        [Tooltip("English translation of part type name")]
        public string english;
    }
    
    /// <summary>
    /// Translation for a single adjective (e.g., "SteelCore" → "Стальной" / "SteelCore")
    /// </summary>
    [Serializable]
    public class AdjectiveTranslation
    {
        [Tooltip("Original adjective name (as stored in ShopPartConfig partNamePool)")]
        public string original; // "SteelCore"
        
        [Tooltip("Russian translation of adjective")]
        public string russian;  // "Стальной"
        
        [Tooltip("English translation of adjective (can be same as original)")]
        public string english;  // "SteelCore"
    }
    
    /// <summary>
    /// Pool of adjective translations for specific part type and rarity tier
    /// </summary>
    [Serializable]
    public class AdjectivePool
    {
        [Header("Part Type & Rarity")]
        [Tooltip("Part type this pool applies to")]
        public PartType partType;
        
        [Tooltip("Rarity tier (1-5) this pool applies to")]
        [Range(1, 5)]
        public int rarity = 1;
        
        [Header("Adjectives")]
        [Tooltip("List of adjective translations for this part type and rarity")]
        public List<AdjectiveTranslation> adjectives = new List<AdjectiveTranslation>();
    }
    
    [Header("Part Type Names")]
    [Tooltip("Translations for part type names (barrel, magazine, stock, scope)")]
    public List<PartTypeName> partTypeNames = new List<PartTypeName>();
    
    [Header("Adjective Pools")]
    [Tooltip("Pools of adjective translations organized by part type and rarity")]
    public List<AdjectivePool> adjectivePools = new List<AdjectivePool>();
    
    /// <summary>
    /// Get localized part type name
    /// </summary>
    /// <param name="partType">Part type</param>
    /// <param name="language">Language code ("ru" or "en")</param>
    /// <returns>Localized part type name or original if not found</returns>
    public string GetLocalizedPartTypeName(PartType partType, string language = null)
    {
        // Use current language if not specified
        if (string.IsNullOrEmpty(language))
        {
            language = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLanguage : "en";
        }
        
        // Find translation
        foreach (var entry in partTypeNames)
        {
            if (entry.partType == partType)
            {
                switch (language)
                {
                    case "ru":
                        return !string.IsNullOrEmpty(entry.russian) ? entry.russian : entry.english;
                    case "en":
                    default:
                        return !string.IsNullOrEmpty(entry.english) ? entry.english : partType.ToString();
                }
            }
        }
        
        // Fallback: return part type as string
        return partType.ToString();
    }
    
    /// <summary>
    /// Get localized adjective
    /// </summary>
    /// <param name="partType">Part type</param>
    /// <param name="rarity">Rarity tier (1-5)</param>
    /// <param name="originalAdjective">Original adjective from partNamePool</param>
    /// <param name="language">Language code ("ru" or "en")</param>
    /// <returns>Localized adjective or original if not found</returns>
    public string GetLocalizedAdjective(PartType partType, int rarity, string originalAdjective, string language = null)
    {
        if (string.IsNullOrEmpty(originalAdjective))
        {
            return originalAdjective;
        }
        
        // Use current language if not specified
        if (string.IsNullOrEmpty(language))
        {
            language = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLanguage : "en";
        }
        
        // Find matching pool
        foreach (var pool in adjectivePools)
        {
            if (pool.partType == partType && pool.rarity == rarity)
            {
                // Find matching adjective translation
                foreach (var adj in pool.adjectives)
                {
                    if (adj.original == originalAdjective)
                    {
                        switch (language)
                        {
                            case "ru":
                                return !string.IsNullOrEmpty(adj.russian) ? adj.russian : adj.original;
                            case "en":
                            default:
                                return !string.IsNullOrEmpty(adj.english) ? adj.english : adj.original;
                        }
                    }
                }
            }
        }
        
        // Fallback: return original adjective
        return originalAdjective;
    }
    
    /// <summary>
    /// Get localized part name (adjective + part type)
    /// </summary>
    /// <param name="partType">Part type</param>
    /// <param name="rarity">Rarity tier (1-5)</param>
    /// <param name="originalAdjective">Original adjective from partNamePool</param>
    /// <param name="language">Language code ("ru" or "en")</param>
    /// <returns>Localized part name (e.g., "Стальной Ствол" or "SteelCore Barrel")</returns>
    public string GetLocalizedPartName(PartType partType, int rarity, string originalAdjective, string language = null)
    {
        string localizedAdjective = GetLocalizedAdjective(partType, rarity, originalAdjective, language);
        string localizedPartType = GetLocalizedPartTypeName(partType, language);
        
        // Combine: "adjective parttype"
        return $"{localizedAdjective} {localizedPartType}";
    }
}

