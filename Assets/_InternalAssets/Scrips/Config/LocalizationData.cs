using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject for storing localization translations.
/// Create an asset from this and assign it to LocalizationManager.
/// </summary>
[CreateAssetMenu(fileName = "LocalizationData", menuName = "Localization/Localization Data")]
public class LocalizationData : ScriptableObject
{
    [Serializable]
    public class TranslationEntry
    {
        [Tooltip("Translation key (e.g., 'shop.buy')")]
        public string key;
        
        [Tooltip("Russian translation")]
        public string russian;
        
        [Tooltip("English translation")]
        public string english;
    }
    
    [Header("Translations")]
    [Tooltip("List of all translations. Key format: 'category.item' (e.g., 'shop.buy', 'location.workshop')")]
    public List<TranslationEntry> translations = new List<TranslationEntry>();
}

