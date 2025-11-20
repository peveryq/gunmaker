using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class WeaponStatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private RectTransform statsRowsContainer;
    [SerializeField] private WeaponStatRowUI statRowPrefab;
    [SerializeField] private GameObject statsPanel;
    
    [Header("Display Settings")]
    [SerializeField] private Workbench targetWorkbench;
    [SerializeField] private bool requireWorkbenchFocusForComparison = true;
    [SerializeField] private float updateInterval = 0.1f;
    
    [Header("Behaviour")]
    [SerializeField] private bool manualMode = false;
    
    [Header("Colours")]
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color positiveColor = new Color(0.36f, 0.84f, 0.44f);
    [SerializeField] private Color negativeColor = new Color(0.91f, 0.35f, 0.35f);
    
    private InteractionHandler interactionHandler;
    private float lastUpdateTime;
    private bool isPanelActive;
    private string lastDisplayedName = string.Empty;
    private string lastStatsSignature = string.Empty;
    
    private readonly List<WeaponStatRowUI> activeRows = new List<WeaponStatRowUI>();
    private readonly List<WeaponStatRowUI> pooledRows = new List<WeaponStatRowUI>();
    private readonly List<StatEntry> statEntriesBuffer = new List<StatEntry>(8);
    private readonly StringBuilder signatureBuilder = new StringBuilder(256);
    
    private struct StatEntry
    {
        public string label;
        public string deltaText;
        public string valueText;
        public bool showDelta;
        public Color valueColor;
        public Color deltaColor;
    }
    
    private void Start()
    {
        if (!manualMode)
        {
            interactionHandler = FindFirstObjectByType<InteractionHandler>();

            if (interactionHandler == null)
            {
                Debug.LogError("InteractionHandler not found! WeaponStatsUI requires InteractionHandler to work.");
                enabled = false;
                return;
            }
        }

        BootstrapExistingRows();

        if (statRowPrefab == null)
        {
            Debug.LogError("WeaponStatsUI requires a statRowPrefab reference. Assign it in the inspector.");
        }

        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
            isPanelActive = false;
        }
    }
    
    private void Update()
    {
        if (manualMode) return;

        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        UpdateStatsDisplay();
    }
    
    private void UpdateStatsDisplay()
    {
        if (interactionHandler == null) return;
        
        string itemName = string.Empty;
        bool showPanel = false;
        
        statEntriesBuffer.Clear();
        
        WeaponPart heldPart = GetHeldWeaponPart();
        
        if (targetWorkbench != null)
        {
            WeaponBody mountedWeapon = targetWorkbench.GetMountedWeapon();
            if (mountedWeapon != null)
            {
                itemName = mountedWeapon.WeaponName;
                
                bool allowPreview = heldPart != null &&
                                    (!requireWorkbenchFocusForComparison ||
                                     ReferenceEquals(interactionHandler.CurrentTarget, targetWorkbench));
                
                BuildEntriesForWeaponBody(mountedWeapon, allowPreview ? heldPart : null, allowPreview);
                showPanel = true;
            }
        }
        else
        {
            IInteractable currentTarget = interactionHandler.CurrentTarget;
            
            if (currentTarget != null)
            {
                MonoBehaviour targetBehaviour = currentTarget as MonoBehaviour;
                if (targetBehaviour != null)
                {
                    WeaponBody weaponBody = targetBehaviour.GetComponent<WeaponBody>();
                    if (weaponBody != null)
                    {
                        itemName = weaponBody.WeaponName;
                        BuildEntriesForWeaponBody(weaponBody, null, false);
                        showPanel = true;
                    }
                    else
                    {
                        WeaponPart weaponPart = targetBehaviour.GetComponent<WeaponPart>();
                        if (weaponPart != null)
                        {
                            itemName = weaponPart.PartName;
                            BuildEntriesForWeaponPart(weaponPart);
                            showPanel = statEntriesBuffer.Count > 0;
                        }
                    }
                }
            }
        }
        
        PresentEntries(itemName, showPanel);
    }
    
    private WeaponPart GetHeldWeaponPart()
    {
        ItemPickup heldItem = interactionHandler.CurrentItem;
        if (heldItem == null) return null;
        return heldItem.GetComponent<WeaponPart>();
    }
    
    private void BuildEntriesForWeaponBody(WeaponBody weaponBody, WeaponPart previewPart, bool allowPreview)
    {
        WeaponStats currentStats = weaponBody.CurrentStats;
        if (currentStats == null) return;
        
        bool hasPreview = false;
        WeaponStats previewStats = null;
        
        if (allowPreview && previewPart != null && weaponBody.TryCalculatePreviewStats(previewPart, out WeaponStats calculatedStats))
        {
            previewStats = calculatedStats;
            hasPreview = true;
        }
        
        AddEntriesFromStats(currentStats, hasPreview ? previewStats : null);
    }
    
    private void BuildEntriesForWeaponPart(WeaponPart weaponPart)
    {
        weaponPart.PopulateModifierEntries((label, value) =>
        {
            // Try to localize the label
            string localizedLabel = GetLocalizedStatNameFromLabel(label);
            bool useInt = string.Equals(localizedLabel, GetLocalizedStatName("stats.ammo", "Ammo"), System.StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(label, "Ammo", System.StringComparison.OrdinalIgnoreCase);
            
            Color valueColor = neutralColor;
            if (value > 0f) valueColor = positiveColor;
            else if (value < 0f) valueColor = negativeColor;
            
            statEntriesBuffer.Add(new StatEntry
            {
                label = localizedLabel,
                deltaText = string.Empty,
                valueText = FormatModifierValue(value, useInt),
                showDelta = false,
                valueColor = valueColor,
                deltaColor = valueColor
            });
        });
    }
    
    private void AddEntriesFromStats(WeaponStats currentStats, WeaponStats previewStats = null)
    {
        if (currentStats == null) return;

        bool hasPreview = previewStats != null;

        float previewPower = hasPreview ? previewStats.power : currentStats.power;
        float previewReload = hasPreview ? previewStats.reloadSpeed : currentStats.reloadSpeed;
        float previewAccuracy = hasPreview ? previewStats.accuracy : currentStats.accuracy;
        float previewRapidity = hasPreview ? previewStats.rapidity : currentStats.rapidity;
        float previewRecoil = hasPreview ? previewStats.recoil : currentStats.recoil;
        float previewAmmo = hasPreview ? previewStats.ammo : currentStats.ammo;
        float previewScope = hasPreview ? previewStats.scope : currentStats.scope;

        AddStatEntry(GetLocalizedStatName("stats.power", "Power"), currentStats.power, previewPower, hasPreview);
        AddStatEntry(GetLocalizedStatName("stats.reload_speed", "Reload Speed"), currentStats.reloadSpeed, previewReload, hasPreview);
        AddStatEntry(GetLocalizedStatName("stats.accuracy", "Accuracy"), currentStats.accuracy, previewAccuracy, hasPreview);
        AddStatEntry(GetLocalizedStatName("stats.rapidity", "Rapidity"), currentStats.rapidity, previewRapidity, hasPreview);
        AddStatEntry(GetLocalizedStatName("stats.recoil", "Recoil"), currentStats.recoil, previewRecoil, hasPreview);
        AddStatEntry(GetLocalizedStatName("stats.ammo", "Ammo"), currentStats.ammo, previewAmmo, hasPreview, true);
        AddStatEntry(GetLocalizedStatName("stats.aim", "Aim"), currentStats.scope, previewScope, hasPreview);
    }

    private static WeaponStats BuildStatsFromSettings(WeaponSettings settings)
    {
        if (settings == null) return null;

        return new WeaponStats
        {
            power = Mathf.Clamp01((settings.bulletSpeed - 50f) / (300f - 50f)) * 99f + 1f,
            accuracy = Mathf.Clamp01((7f - settings.spreadAngle) / 7f) * 99f + 1f,
            rapidity = Mathf.Clamp01((0.5f - settings.fireRate) / (0.5f - 0.05f)) * 99f + 1f,
            recoil = Mathf.Clamp01(settings.recoilUpward / 3f) * 99f + 1f,
            reloadSpeed = Mathf.Clamp01((3f - settings.reloadTime) / 2f) * 99f + 1f,
            scope = Mathf.Clamp01((45f - settings.aimFOV) / 40f) * 99f + 1f,
            ammo = settings.magSize,
            totalPartCost = settings.totalPartCost
        };
    }

    private void AddStatEntry(string label, float currentValue, float previewValue, bool previewAvailable, bool forceInteger = false)
    {
        float finalValue = previewAvailable ? previewValue : currentValue;
        float difference = previewAvailable ? (previewValue - currentValue) : 0f;
        
        bool hasDelta = previewAvailable && !Mathf.Approximately(difference, 0f);
        string deltaText = hasDelta ? $"({FormatModifierValue(difference, forceInteger)})" : string.Empty;
        string valueText = FormatStatValue(finalValue, forceInteger);
        
        Color valueColor = neutralColor;
        Color deltaColor = neutralColor;
        
        if (hasDelta)
        {
            if (difference > 0f)
            {
                valueColor = positiveColor;
                deltaColor = positiveColor;
            }
            else
            {
                valueColor = negativeColor;
                deltaColor = negativeColor;
            }
        }
        
        statEntriesBuffer.Add(new StatEntry
        {
            label = label,
            deltaText = deltaText,
            valueText = valueText,
            showDelta = hasDelta,
            valueColor = valueColor,
            deltaColor = deltaColor
        });
    }
    
    private string FormatStatValue(float value, bool forceInteger)
    {
        if (forceInteger)
        {
            return Mathf.RoundToInt(value).ToString();
        }
        
        int rounded = Mathf.RoundToInt(value);
        if (Mathf.Approximately(value, rounded))
        {
            return rounded.ToString();
        }
        
        return value.ToString("0.##");
    }
    
    private string FormatModifierValue(float value, bool forceInteger = false)
    {
        if (forceInteger)
        {
            int intValue = Mathf.RoundToInt(value);
            return intValue >= 0 ? $"+{intValue}" : intValue.ToString();
        }
        
        if (Mathf.Approximately(value, 0f))
        {
            return "0";
        }
        
        bool isWhole = Mathf.Approximately(value, Mathf.Round(value));
        string formatted = isWhole ? Mathf.RoundToInt(value).ToString() : value.ToString("0.##");
        
        if (value > 0f)
        {
            return "+" + formatted;
        }
        
        return formatted;
    }
    
    private string BuildSignature(List<StatEntry> entries)
    {
        signatureBuilder.Length = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            StatEntry entry = entries[i];
            signatureBuilder.Append(entry.label);
            signatureBuilder.Append(':');
            signatureBuilder.Append(entry.deltaText);
            signatureBuilder.Append('|');
            signatureBuilder.Append(entry.valueText);
            signatureBuilder.Append(entry.showDelta ? '1' : '0');
            signatureBuilder.Append(';');
        }
        return signatureBuilder.ToString();
    }
    
    private void ApplyStatEntries(List<StatEntry> entries)
    {
        if (statsRowsContainer == null || statRowPrefab == null) return;
        
        while (activeRows.Count < entries.Count)
        {
            WeaponStatRowUI row = GetRowFromPool();
            if (row == null)
            {
                return;
            }
            activeRows.Add(row);
        }
        
        for (int i = entries.Count; i < activeRows.Count; i++)
        {
            ReleaseRow(activeRows[i]);
            activeRows.RemoveAt(i);
            i--;
        }
        
        for (int i = 0; i < entries.Count; i++)
        {
            StatEntry entry = entries[i];
            activeRows[i].SetData(entry.label, entry.deltaText, entry.valueText, entry.showDelta, entry.valueColor, entry.deltaColor);
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(statsRowsContainer);
    }
    
    private WeaponStatRowUI GetRowFromPool()
    {
        WeaponStatRowUI row;
        if (pooledRows.Count > 0)
        {
            int lastIndex = pooledRows.Count - 1;
            row = pooledRows[lastIndex];
            pooledRows.RemoveAt(lastIndex);
            row.gameObject.SetActive(true);
        }
        else
        {
            if (statRowPrefab == null)
            {
                Debug.LogError("WeaponStatsUI cannot create stat rows because statRowPrefab is not assigned.");
                return null;
            }
            row = Instantiate(statRowPrefab, statsRowsContainer);
        }
        
        row.transform.SetAsLastSibling();
        return row;
    }
    
    private void ReleaseRow(WeaponStatRowUI row)
    {
        if (row == null) return;
        row.gameObject.SetActive(false);
        pooledRows.Add(row);
    }
    
    private void ResetCache()
    {
        lastDisplayedName = string.Empty;
        lastStatsSignature = string.Empty;
    }
    
    private void BootstrapExistingRows()
    {
        if (statsRowsContainer == null) return;
        
        List<WeaponStatRowUI> rowsToRelease = new List<WeaponStatRowUI>();
        for (int i = 0; i < statsRowsContainer.childCount; i++)
        {
            Transform child = statsRowsContainer.GetChild(i);
            WeaponStatRowUI row = child.GetComponent<WeaponStatRowUI>();
            if (row != null)
            {
                if (statRowPrefab == null)
                {
                    statRowPrefab = row;
                }
                rowsToRelease.Add(row);
            }
        }
        
        for (int i = 0; i < rowsToRelease.Count; i++)
        {
            ReleaseRow(rowsToRelease[i]);
        }
    }

    private void PresentEntries(string itemName, bool showPanel)
    {
        bool panelStateChanged = showPanel != isPanelActive;

        if (statsPanel != null && panelStateChanged)
        {
            statsPanel.SetActive(showPanel);
            isPanelActive = showPanel;

            if (!showPanel)
            {
                ResetCache();
            }
        }

        if (!showPanel) return;

        bool nameChanged = itemName != lastDisplayedName;
        string signature = BuildSignature(statEntriesBuffer);
        bool statsChanged = signature != lastStatsSignature;

        if (itemNameText != null && nameChanged)
        {
            itemNameText.text = itemName;
            lastDisplayedName = itemName;
        }

        if (statsChanged)
        {
            ApplyStatEntries(statEntriesBuffer);
            lastStatsSignature = signature;
        }
    }

    public void DisplayWeaponRecord(WeaponRecord record)
    {
        if (!manualMode)
        {
            Debug.LogWarning("WeaponStatsUI.DisplayWeaponRecord called while manual mode is disabled.");
            return;
        }

        string itemName = string.Empty;
        WeaponStats currentStats = null;

        if (record != null)
        {
            itemName = !string.IsNullOrEmpty(record.WeaponName)
                ? record.WeaponName
                : record.WeaponBody != null ? record.WeaponBody.WeaponName : string.Empty;

            currentStats = record.StatsSnapshot ?? record.WeaponBody?.CurrentStats;

            if (currentStats == null && record.WeaponSettings != null)
            {
                currentStats = BuildStatsFromSettings(record.WeaponSettings);
            }
        }

        DisplayStats(itemName, currentStats);
    }

    public void DisplayStats(string itemName, WeaponStats currentStats, WeaponStats previewStats = null)
    {
        if (!manualMode)
        {
            Debug.LogWarning("WeaponStatsUI.DisplayStats called while manual mode is disabled.");
            return;
        }

        statEntriesBuffer.Clear();

        string displayName = string.IsNullOrEmpty(itemName) ? string.Empty : itemName;

        if (currentStats == null)
        {
            PresentEntries(displayName, false);
            return;
        }

        AddEntriesFromStats(currentStats, previewStats);
        PresentEntries(displayName, true);
    }

    public void ClearManualDisplay()
    {
        if (!manualMode)
        {
            return;
        }

        statEntriesBuffer.Clear();
        PresentEntries(string.Empty, false);
    }
    
    /// <summary>
    /// Get localized stat name by key
    /// </summary>
    private string GetLocalizedStatName(string key, string fallback)
    {
        return LocalizationHelper.Get(key, null, fallback);
    }
    
    /// <summary>
    /// Get localized stat name from English label (for backward compatibility)
    /// </summary>
    private string GetLocalizedStatNameFromLabel(string englishLabel)
    {
        if (string.IsNullOrEmpty(englishLabel))
            return englishLabel;
        
        // Map English labels to localization keys
        string key = englishLabel switch
        {
            "Power" => "stats.power",
            "Accuracy" => "stats.accuracy",
            "Rapidity" => "stats.rapidity",
            "Recoil" => "stats.recoil",
            "Reload Speed" => "stats.reload_speed",
            "Aim" => "stats.aim",
            "Ammo" => "stats.ammo",
            _ => null
        };
        
        if (!string.IsNullOrEmpty(key))
        {
            return LocalizationHelper.Get(key, englishLabel);
        }
        
        // Return original label if no mapping found
        return englishLabel;
    }
}

