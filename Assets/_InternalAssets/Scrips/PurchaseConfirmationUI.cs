using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Modal UI for confirming weapon part purchases
/// </summary>
public class PurchaseConfirmationUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private GameObject overlay;
    
    [Header("Part Display")]
    [SerializeField] private TextMeshProUGUI modalHeaderText;
    [SerializeField] private Image partIconImage;
    
    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI statsHeaderText;
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statLinePrefab; // Prefab with TextMeshProUGUI for stat name and value
    
    [Header("Purchase Info")]
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;
    
    [Header("Audio")]
    [Tooltip("Optional local AudioSource for fallback (if AudioManager not available). Can be left empty.")]
    [SerializeField] private AudioSource audioSource; // Fallback only
    [SerializeField] private AudioClip purchaseSound;
    [SerializeField] private AudioClip buttonClickSound;
    
    [Header("References")]
    [SerializeField] private ShopOfferingGenerator offeringGenerator;
    
    private ShopOffering currentOffering;
    private PartType currentPartType;
    private int currentOfferingIndex;
    private System.Action onPurchaseComplete;
    private bool listenersInitialized = false;
    private string currentGeneratedName;
    
    private void Awake()
    {
        InitializeListeners();
        
        // Subscribe to language changes to update stats header
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateStatsHeader;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from language changes
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateStatsHeader;
        }
    }
    
    /// <summary>
    /// Update stats header text when language changes
    /// </summary>
    private void UpdateStatsHeader()
    {
        if (statsHeaderText != null && modalPanel != null && modalPanel.activeSelf)
        {
            statsHeaderText.text = LocalizationHelper.Get("stats.header", "STATS");
        }
    }
    
    /// <summary>
    /// Initialize button listeners (safe to call multiple times)
    /// </summary>
    private void InitializeListeners()
    {
        if (listenersInitialized)
            return;
        
        // Setup button listeners
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }
        
        // Setup audio
        // AudioSource is now optional (fallback only)
        // AudioManager will be used if available
        
        listenersInitialized = true;
        
        // Hide modal initially
        HideModal();
    }
    
    /// <summary>
    /// Show purchase confirmation for a specific offering
    /// </summary>
    public void ShowPurchaseConfirmation(PartType partType, int offeringIndex, System.Action onComplete = null)
    {
        // Ensure listeners are initialized (in case Awake wasn't called)
        InitializeListeners();
        
        if (offeringGenerator == null)
        {
            Debug.LogError("ShopOfferingGenerator not assigned!");
            return;
        }
        
        currentPartType = partType;
        currentOfferingIndex = offeringIndex;
        currentOffering = offeringGenerator.GetOffering(partType, offeringIndex);
        onPurchaseComplete = onComplete;
        
        if (currentOffering == null)
        {
            Debug.LogError($"No offering found for {partType} at index {offeringIndex}");
            return;
        }
        
        // Show modal
        if (modalPanel != null)
            modalPanel.SetActive(true);
        
        if (overlay != null)
            overlay.SetActive(true);
        
        // Populate UI
        PopulateUI();
    }
    
    /// <summary>
    /// Populate modal UI with offering data
    /// </summary>
    private void PopulateUI()
    {
        if (currentOffering == null)
        {
            Debug.LogError("Cannot populate UI: currentOffering is null!");
            HideModal();
            return;
        }
        
        // Generate and display part name
        currentGeneratedName = currentOffering.GetGeneratedName(offeringGenerator.Config);
        if (modalHeaderText != null)
        {
            modalHeaderText.text = currentGeneratedName;
        }
        
        // Set part icon
        if (partIconImage != null)
        {
            if (currentOffering.partIcon != null)
            {
                partIconImage.sprite = currentOffering.partIcon;
                partIconImage.enabled = true;
            }
            else
            {
                partIconImage.enabled = false;
            }
        }
        
        // Set cost
        if (costText != null)
        {
            costText.text = $"{currentOffering.price} $";
        }
        
        // Get and display stats (Phase 2: calculated on-demand)
        Dictionary<StatInfluence.StatType, float> stats = offeringGenerator.GetOfferingStats(currentPartType, currentOfferingIndex);
        DisplayStats(stats);
        
        // Update buy button state
        UpdateBuyButton();
    }
    
    /// <summary>
    /// Display calculated stats
    /// </summary>
    private void DisplayStats(Dictionary<StatInfluence.StatType, float> stats)
    {
        // Set header with localization
        if (statsHeaderText != null)
        {
            statsHeaderText.text = LocalizationHelper.Get("stats.header", "STATS");
        }
        
        // Clear existing stat lines (но НЕ удаляем header!)
        if (statsContainer != null)
        {
            // Удаляем только stat lines, не header
            for (int i = statsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = statsContainer.GetChild(i);
                
                // Не удаляем StatsHeader
                if (child.gameObject != statsHeaderText?.gameObject)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        // If no stats, hide stats section
        if (stats == null || stats.Count == 0)
        {
            if (statsContainer != null)
                statsContainer.gameObject.SetActive(false);
            return;
        }
        
        if (statsContainer != null)
            statsContainer.gameObject.SetActive(true);
        
        // Create stat lines
        foreach (var stat in stats)
        {
            CreateStatLine(stat.Key, stat.Value);
        }
    }
    
    /// <summary>
    /// Create a single stat line UI element
    /// </summary>
    private void CreateStatLine(StatInfluence.StatType statType, float value)
    {
        if (statsContainer == null)
            return;
        
        // If we have a prefab, use it
        GameObject statLine;
        if (statLinePrefab != null)
        {
            statLine = Instantiate(statLinePrefab, statsContainer);
        }
        else
        {
            // Create simple stat line
            statLine = new GameObject($"Stat_{statType}");
            statLine.transform.SetParent(statsContainer);
            
            TextMeshProUGUI text = statLine.AddComponent<TextMeshProUGUI>();
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Left;
        }
        
        // Set text content
        // Try to find text in children first (for complex prefabs)
        TextMeshProUGUI statText = statLine.GetComponentInChildren<TextMeshProUGUI>();
        if (statText == null)
        {
            statText = statLine.GetComponent<TextMeshProUGUI>();
        }
        
        if (statText != null)
        {
            string statName = GetStatDisplayName(statType);
            string valueText = GetStatValueText(statType, value);
            
            // Format: white stat name + green value
            statText.text = $"{statName}: <color=#95e76f>{valueText}</color>";
        }
    }
    
    /// <summary>
    /// Get display name for stat type (localized)
    /// </summary>
    private string GetStatDisplayName(StatInfluence.StatType statType)
    {
        string key = statType switch
        {
            StatInfluence.StatType.Power => "stats.power",
            StatInfluence.StatType.Accuracy => "stats.accuracy",
            StatInfluence.StatType.Rapidity => "stats.rapidity",
            StatInfluence.StatType.Recoil => "stats.recoil",
            StatInfluence.StatType.ReloadSpeed => "stats.reload_speed",
            StatInfluence.StatType.Aim => "stats.aim",
            StatInfluence.StatType.Ammo => "stats.ammo",
            _ => null
        };
        
        if (!string.IsNullOrEmpty(key))
        {
            // Provide English fallback for each stat
            string englishFallback = statType switch
            {
                StatInfluence.StatType.Power => "Power",
                StatInfluence.StatType.Accuracy => "Accuracy",
                StatInfluence.StatType.Rapidity => "Rapidity",
                StatInfluence.StatType.Recoil => "Recoil",
                StatInfluence.StatType.ReloadSpeed => "Reload Speed",
                StatInfluence.StatType.Aim => "Aim",
                StatInfluence.StatType.Ammo => "Ammo",
                _ => statType.ToString()
            };
            
            return LocalizationHelper.Get(key, null, englishFallback);
        }
        
        // Fallback to enum name
        return statType.ToString();
    }
    
    /// <summary>
    /// Format stat value for display
    /// </summary>
    private string GetStatValueText(StatInfluence.StatType statType, float value)
    {
        // Ammo has no sign
        if (statType == StatInfluence.StatType.Ammo)
        {
            return ((int)value).ToString();
        }
        
        // Recoil is already negative from calculation
        // Other stats get + sign
        if (value >= 0)
        {
            return $"+{(int)value}";
        }
        else
        {
            return ((int)value).ToString();
        }
    }
    
    /// <summary>
    /// Update buy button interactability based on money
    /// </summary>
    private void UpdateBuyButton()
    {
        if (buyButton != null && MoneySystem.Instance != null && currentOffering != null)
        {
            bool canAfford = MoneySystem.Instance.HasEnoughMoney(currentOffering.price);
            buyButton.interactable = canAfford;
        }
    }
    
    /// <summary>
    /// Handle buy button click
    /// </summary>
    private void OnBuyClicked()
    {
        if (currentOffering == null || MoneySystem.Instance == null || PartSpawner.Instance == null)
        {
            Debug.LogError("Cannot complete purchase: missing systems!");
            return;
        }
        
        // Check if player has enough money
        if (!MoneySystem.Instance.HasEnoughMoney(currentOffering.price))
        {
            Debug.LogWarning("Not enough money!");
            return;
        }
        
        // Get universal part prefab from config
        GameObject universalPrefab = offeringGenerator.Config?.universalPartPrefab;
        if (universalPrefab == null)
        {
            Debug.LogError("Universal part prefab not assigned in ShopPartConfig!");
            return;
        }
        
        // Deduct money
        if (MoneySystem.Instance.SpendMoney(currentOffering.price))
        {
            // Get calculated stats
            Dictionary<StatInfluence.StatType, float> stats = offeringGenerator.GetOfferingStats(currentPartType, currentOfferingIndex);
            string partName = !string.IsNullOrEmpty(currentGeneratedName)
                ? currentGeneratedName
                : currentOffering.GetGeneratedName(offeringGenerator.Config);
            
            // Spawn part with universal prefab, specific mesh, part type, stats, and optional lens overlay
            PartSpawner.Instance.SpawnPart(
                universalPrefab,
                currentOffering.partMesh,
                currentPartType,
                stats,
                partName,
                currentOffering.lensOverlayPrefab,
                currentOffering.price);

            // Remove offering so it disappears from the shop list
            offeringGenerator.RemoveOffering(currentPartType, currentOffering);
            
            // Notify TutorialManager about part purchase
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NotifyPartPurchased(currentPartType);
            }
            
            // Play purchase sound
            PlayPurchaseSound();
            
            // Notify completion
            onPurchaseComplete?.Invoke();
            
            // Close modal
            HideModal();
        }
    }
    
    /// <summary>
    /// Handle close button click
    /// </summary>
    private void OnCloseClicked()
    {
        PlayButtonClickSound();
        HideModal();
    }
    
    /// <summary>
    /// Hide the modal
    /// </summary>
    public void HideModal()
    {
        if (modalPanel != null)
            modalPanel.SetActive(false);
        
        if (overlay != null)
            overlay.SetActive(false);
        
        currentOffering = null;
        currentGeneratedName = null;
    }
    
    /// <summary>
    /// Play purchase sound
    /// </summary>
    private void PlayPurchaseSound()
    {
        if (purchaseSound == null) return;
        
        // Use AudioManager if available, otherwise fallback to local AudioSource
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(purchaseSound, volume: 0.9f);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(purchaseSound);
        }
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickSound == null) return;
        
        // Use AudioManager if available, otherwise fallback to local AudioSource
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSound, volume: 0.8f);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
}

