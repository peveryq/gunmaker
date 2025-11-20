/// <summary>
/// Static helper class for easy access to localization.
/// Provides convenient methods for getting localized text in code.
/// </summary>
public static class LocalizationHelper
{
    /// <summary>
    /// Get localized text by key
    /// </summary>
    /// <param name="key">Translation key (e.g., "action.open", "shop.buy")</param>
    /// <returns>Localized text or key if translation not found</returns>
    public static string Get(string key)
    {
        return Get(key, null, null);
    }
    
    /// <summary>
    /// Get localized text by key with fallback
    /// </summary>
    /// <param name="key">Translation key</param>
    /// <param name="fallback">Fallback text if translation not found</param>
    /// <param name="defaultEnglish">Default English text if fallback is also not available</param>
    /// <returns>Localized text or fallback</returns>
    public static string Get(string key, string fallback, string defaultEnglish = null)
    {
        if (LocalizationManager.Instance != null)
        {
            string translated = LocalizationManager.Instance.GetText(key);
            
            // If returned key itself (no translation found), use fallback
            if (translated == key)
            {
                if (!string.IsNullOrEmpty(fallback))
                {
                    return fallback;
                }
                if (!string.IsNullOrEmpty(defaultEnglish))
                {
                    return defaultEnglish;
                }
            }
            
            return translated;
        }
        
        // Fallback if LocalizationManager not available
        if (!string.IsNullOrEmpty(fallback))
        {
            return fallback;
        }
        if (!string.IsNullOrEmpty(defaultEnglish))
        {
            return defaultEnglish;
        }
        return key;
    }
}

