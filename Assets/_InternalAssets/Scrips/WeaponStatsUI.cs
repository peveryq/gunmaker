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
        interactionHandler = FindFirstObjectByType<InteractionHandler>();
        
        if (interactionHandler == null)
        {
            Debug.LogError("InteractionHandler not found! WeaponStatsUI requires InteractionHandler to work.");
            enabled = false;
            return;
        }
        
        BootstrapExistingRows();
        
        if (statRowPrefab == null)
        {
            Debug.LogError("WeaponStatsUI requires a statRowPrefab reference. Assign it in the inspector.");
        }
        
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }
    
    private void Update()
    {
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
        WeaponStats previewStats = currentStats;
        
        if (allowPreview && previewPart != null && weaponBody.TryCalculatePreviewStats(previewPart, out WeaponStats calculatedStats))
        {
            previewStats = calculatedStats;
            hasPreview = true;
        }
        
        AddStatEntry("Power", currentStats.power, previewStats.power, hasPreview);
        AddStatEntry("Reload Speed", currentStats.reloadSpeed, previewStats.reloadSpeed, hasPreview);
        AddStatEntry("Accuracy", currentStats.accuracy, previewStats.accuracy, hasPreview);
        AddStatEntry("Rapidity", currentStats.rapidity, previewStats.rapidity, hasPreview);
        AddStatEntry("Recoil", currentStats.recoil, previewStats.recoil, hasPreview);
        AddStatEntry("Ammo", currentStats.ammo, previewStats.ammo, hasPreview, true);
        AddStatEntry("Aim", currentStats.scope, previewStats.scope, hasPreview);
    }
    
    private void BuildEntriesForWeaponPart(WeaponPart weaponPart)
    {
        weaponPart.PopulateModifierEntries((label, value) =>
        {
            bool useInt = string.Equals(label, "Ammo", System.StringComparison.OrdinalIgnoreCase);
            
            Color valueColor = neutralColor;
            if (value > 0f) valueColor = positiveColor;
            else if (value < 0f) valueColor = negativeColor;
            
            statEntriesBuffer.Add(new StatEntry
            {
                label = label,
                deltaText = string.Empty,
                valueText = FormatModifierValue(value, useInt),
                showDelta = false,
                valueColor = valueColor,
                deltaColor = valueColor
            });
        });
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
}

