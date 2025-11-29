using System.Collections.Generic;
using UnityEngine;
using YG;

/// <summary>
/// Simple localization manager for Russian/English translations.
/// Loads language from YG2.envir.language and provides translation lookup.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    private static LocalizationManager instance;
    public static LocalizationManager Instance => instance;
    
    [Header("Localization Data")]
    [Tooltip("ScriptableObject containing all translations. If not assigned, will use default dictionary.")]
    [SerializeField] private LocalizationData localizationData;
    
    [Header("Settings")]
    [Tooltip("Default language if YG2 language is not recognized")]
    [SerializeField] private string defaultLanguage = "en";
    
    // Current language
    private string currentLanguage = "en";
    
    // Translation dictionary: key -> language -> text
    private Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>();
    
    // Event for language changes
    public System.Action OnLanguageChanged;
    
    // Property to get current language
    public string CurrentLanguage => currentLanguage;
    
    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Load language from YG2
        LoadLanguageFromYG2();
        
        // Load translations
        LoadTranslations();
    }
    
    /// <summary>
    /// Load language from YG2 environment data
    /// </summary>
    private void LoadLanguageFromYG2()
    {
        if (YG2.isSDKEnabled && YG2.envir != null)
        {
            string yg2Language = YG2.envir.language;
            
            // Map YG2 language codes to our language codes
            if (yg2Language == "ru" || yg2Language == "ru-RU" || yg2Language.StartsWith("ru"))
            {
                currentLanguage = "ru";
            }
            else if (yg2Language == "en" || yg2Language == "en-US" || yg2Language.StartsWith("en"))
            {
                currentLanguage = "en";
            }
            else
            {
                // Unknown language, use default
                currentLanguage = defaultLanguage;
                Debug.LogWarning($"LocalizationManager: Unknown language '{yg2Language}' from YG2, using default '{defaultLanguage}'");
            }
            
            Debug.Log($"LocalizationManager: Language loaded from YG2 - '{currentLanguage}' (YG2: '{yg2Language}')");
        }
        else
        {
            // YG2 not initialized yet, use default
            currentLanguage = defaultLanguage;
            Debug.Log($"LocalizationManager: YG2 not initialized, using default language '{defaultLanguage}'");
        }
    }
    
    /// <summary>
    /// Load translations from LocalizationData or create default dictionary
    /// </summary>
    private void LoadTranslations()
    {
        translations.Clear();
        
        // Always add default translations first (as base)
        AddDefaultTranslations();
        
        // Then load from ScriptableObject if available (will override defaults)
        if (localizationData != null)
        {
            int loadedCount = 0;
            foreach (var entry in localizationData.translations)
            {
                if (!translations.ContainsKey(entry.key))
                {
                    translations[entry.key] = new Dictionary<string, string>();
                }
                
                translations[entry.key]["ru"] = entry.russian;
                translations[entry.key]["en"] = entry.english;
                loadedCount++;
            }
            
            Debug.Log($"LocalizationManager: Loaded {loadedCount} translations from LocalizationData (total: {translations.Count} including defaults).");
        }
        else
        {
            Debug.Log($"LocalizationManager: Using default translations only (no LocalizationData assigned). Total: {translations.Count} translations.");
        }
    }
    
    /// <summary>
    /// Add default translations for common UI elements
    /// </summary>
    private void AddDefaultTranslations()
    {
        // Shop
        AddTranslation("shop.buy", "Купить", "Buy");
        AddTranslation("shop.sell", "Продать", "Sell");
        AddTranslation("shop.money", "Деньги", "Money");
        AddTranslation("shop.refresh", "Обновить", "Refresh");
        AddTranslation("shop.close", "Закрыть", "Close");
        
        // Categories
        AddTranslation("category.stocks", "Приклады", "Stocks");
        AddTranslation("category.barrels", "Стволы", "Barrels");
        AddTranslation("category.magazines", "Магазины", "Magazines");
        AddTranslation("category.scopes", "Прицелы", "Scopes");
        AddTranslation("category.lasers", "Лазеры", "Lasers");
        AddTranslation("category.foregrips", "Рукоятки", "Foregrips");
        
        // Locations
        AddTranslation("location.workshop", "Мастерская", "Workshop");
        AddTranslation("location.testing_range", "Стрельбище", "Testing Range");
        
        // Common
        AddTranslation("common.yes", "Да", "Yes");
        AddTranslation("common.no", "Нет", "No");
        AddTranslation("common.ok", "ОК", "OK");
        AddTranslation("common.cancel", "Отмена", "Cancel");
        
        // Weapon
        AddTranslation("weapon.name", "Название", "Name");
        AddTranslation("weapon.save", "Сохранить", "Save");
        
        // Results
        AddTranslation("results.earnings", "Заработано", "Earnings");
        AddTranslation("results.double_reward", "Удвоить награду", "Double Reward");
        
        // Actions (for interactive buttons)
        AddTranslation("action.open", "Открыть", "Open");
        AddTranslation("action.close", "Закрыть", "Close");
        AddTranslation("action.enter", "Войти", "Enter");
        AddTranslation("action.locked", "Заблокировано", "Locked");
        AddTranslation("action.grab", "Взять", "Grab");
        AddTranslation("action.stash", "Спрятать", "Stash");
        AddTranslation("action.create", "Создать", "Create");
        AddTranslation("action.place", "Поставить", "Place");
        AddTranslation("action.take", "Взять", "Take");
        AddTranslation("action.install", "Установить", "Install");
        AddTranslation("action.weld", "Приварить", "Weld");
        AddTranslation("action.buy", "Купить", "Buy");
        AddTranslation("action.sell", "Продать", "Sell");
        
        // Location selection messages
        AddTranslation("location.no_weapon", "Сначала возьми оружие", "grab a gun first");
        AddTranslation("location.no_barrel", "Прикрепи ствол к оружию", "attach a barrel to the gun");
        AddTranslation("location.no_magazine", "Прикрепи магазин к оружию", "attach a mag to the gun");
        AddTranslation("location.no_barrel_and_magazine", "Прикрепи ствол и магазин к оружию", "attach a barrel and a mag to the gun");
        AddTranslation("location.unwelded_barrel", "Привари ствол к оружию", "weld the barrel to the gun");
        
        // HUD messages
        AddTranslation("hud.unwelded_barrel_warning", "Ствол не приварен", "Barrel not welded");
        AddTranslation("hud.autosave", "автосохранение", "autosave");
        
        // Stats labels
        AddTranslation("stats.header", "ХАРАКТЕРИСТИКИ", "STATS");
        AddTranslation("stats.power", "Мощность", "Power");
        AddTranslation("stats.accuracy", "Точность", "Accuracy");
        AddTranslation("stats.rapidity", "Скорострельность", "Rapidity");
        AddTranslation("stats.recoil", "Отдача", "Recoil");
        AddTranslation("stats.reload_speed", "Скорость перезарядки", "Reload Speed");
        AddTranslation("stats.aim", "Прицел", "Aim");
        AddTranslation("stats.ammo", "Боезапас", "Ammo");
        
        // Tutorial quest texts
        AddTranslation("tutorial.quest.1", "Создай новое оружие на верстаке", "Create a new gun at the workbench");
        AddTranslation("tutorial.quest.2", "Купи ствол в компьютере", "Buy a barrel at the computer");
        AddTranslation("tutorial.quest.3", "Возьми ствол", "Take the barrel");
        AddTranslation("tutorial.quest.4", "Прикрепи ствол к оружию", "Attach the barrel to the gun");
        AddTranslation("tutorial.quest.5", "Возьми горелку", "Take blowtorch");
        AddTranslation("tutorial.quest.6", "Привари ствол горелкой", "Weld the barrel with blowtorch");
        AddTranslation("tutorial.quest.7", "Купи магазин в компьютере", "Buy a mag at the computer");
        AddTranslation("tutorial.quest.8", "Возьми магазин", "Take the mag");
        AddTranslation("tutorial.quest.9", "Прикрепи магазин к оружию", "Attach the mag to the gun");
        AddTranslation("tutorial.quest.10", "Возьми оружие с верстака", "Take the gun from workbench");
        AddTranslation("tutorial.quest.11", "Стреляй по мишеням", "Shoot some targets");
        AddTranslation("tutorial.quest.12", "Подойди к двери и войди на стрельбище", "Go to the door and enter shooting range");
    }
    
    /// <summary>
    /// Add a translation entry
    /// </summary>
    private void AddTranslation(string key, string russian, string english)
    {
        if (!translations.ContainsKey(key))
        {
            translations[key] = new Dictionary<string, string>();
        }
        
        translations[key]["ru"] = russian;
        translations[key]["en"] = english;
    }
    
    /// <summary>
    /// Get translated text by key
    /// </summary>
    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return "";
        }
        
        // Check if translation exists
        if (translations.ContainsKey(key) && translations[key].ContainsKey(currentLanguage))
        {
            return translations[key][currentLanguage];
        }
        
        // Fallback: try default language
        if (translations.ContainsKey(key) && translations[key].ContainsKey(defaultLanguage))
        {
            Debug.LogWarning($"LocalizationManager: Translation for key '{key}' not found in language '{currentLanguage}', using default '{defaultLanguage}'");
            return translations[key][defaultLanguage];
        }
        
        // No translation found, return key
        Debug.LogWarning($"LocalizationManager: Translation key '{key}' not found. Returning key as fallback.");
        return key;
    }
    
    /// <summary>
    /// Set language manually (for testing or language switching)
    /// </summary>
    public void SetLanguage(string language)
    {
        if (language != "ru" && language != "en")
        {
            Debug.LogWarning($"LocalizationManager: Invalid language '{language}'. Supported: 'ru', 'en'");
            return;
        }
        
        if (currentLanguage != language)
        {
            currentLanguage = language;
            Debug.Log($"LocalizationManager: Language changed to '{currentLanguage}'");
            OnLanguageChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// Reload language from YG2 (useful if YG2 initializes after LocalizationManager)
    /// </summary>
    public void ReloadLanguageFromYG2()
    {
        string oldLanguage = currentLanguage;
        LoadLanguageFromYG2();
        
        if (oldLanguage != currentLanguage)
        {
            OnLanguageChanged?.Invoke();
        }
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

