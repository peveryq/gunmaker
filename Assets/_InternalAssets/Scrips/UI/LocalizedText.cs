using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Component that automatically localizes TextMeshProUGUI or Text components.
/// Add this to any text element and specify the translation key.
/// Works with either TextMeshProUGUI or Unity Text component.
/// </summary>
public class LocalizedText : MonoBehaviour
{
    [Header("Translation Settings")]
    [Tooltip("Translation key (e.g., 'shop.buy', 'location.workshop')")]
    [SerializeField] private string translationKey = "";
    
    [Header("Fallback Text")]
    [Tooltip("Text to show if translation key is not found. If empty, will use translation key.")]
    [SerializeField] private string fallbackText = "";
    
    // Component references
    private TextMeshProUGUI tmpText;
    private Text unityText;
    
    private void Awake()
    {
        // Get text components (try both)
        tmpText = GetComponent<TextMeshProUGUI>();
        unityText = GetComponent<Text>();
        
        // If neither component exists, log warning
        if (tmpText == null && unityText == null)
        {
            Debug.LogWarning($"LocalizedText: No TextMeshProUGUI or Text component found on {gameObject.name}. Component will not work.");
        }
    }
    
    private void Start()
    {
        // Subscribe to language changes
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            UpdateText();
        }
        else
        {
            // LocalizationManager not ready yet, try again next frame
            Invoke(nameof(DelayedUpdate), 0.1f);
        }
    }
    
    private void DelayedUpdate()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            UpdateText();
        }
    }
    
    /// <summary>
    /// Update text with current translation
    /// </summary>
    private void UpdateText()
    {
        if (string.IsNullOrEmpty(translationKey))
        {
            Debug.LogWarning($"LocalizedText: Translation key is empty on {gameObject.name}");
            return;
        }
        
        string translatedText = "";
        
        if (LocalizationManager.Instance != null)
        {
            translatedText = LocalizationManager.Instance.GetText(translationKey);
        }
        else
        {
            // LocalizationManager not available, use fallback
            translatedText = !string.IsNullOrEmpty(fallbackText) ? fallbackText : translationKey;
        }
        
        // Apply to text component
        if (tmpText != null)
        {
            tmpText.text = translatedText;
        }
        else if (unityText != null)
        {
            unityText.text = translatedText;
        }
    }
    
    /// <summary>
    /// Set translation key programmatically
    /// </summary>
    public void SetTranslationKey(string key)
    {
        translationKey = key;
        UpdateText();
    }
    
    /// <summary>
    /// Get current translation key
    /// </summary>
    public string GetTranslationKey() => translationKey;
    
    private void OnDestroy()
    {
        // Unsubscribe from language changes
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }
}

