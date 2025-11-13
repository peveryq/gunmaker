using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSellModal : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Button closeButton;

    [Header("Content")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI weaponNameLabel;
    [SerializeField] private Transform statsContainer;
    [SerializeField] private WeaponStatRowUI statRowPrefab;
    [SerializeField] private TextMeshProUGUI sellingPriceLabel;
    [SerializeField] private string sellingPriceFormat = "{0} $";

    [Header("Actions")]
    [SerializeField] private Button sellButton;

    [Header("Styling")]
    [SerializeField] private Color statsValueColor = Color.white;
    [SerializeField] private Color statsDeltaColor = Color.white;

    private readonly List<WeaponStatRowUI> activeRows = new();
    private readonly List<WeaponStatRowUI> pooledRows = new();

    private Action<WeaponRecord> onConfirm;
    private Action onCancelled;
    private WeaponRecord currentRecord;
    private bool isVisible;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.AddListener(HandleSellClicked);
        }

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(HandleSellClicked);
        }
    }

    private void Update()
    {
        if (!isVisible) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleCloseClicked();
        }
    }

    public void Show(WeaponRecord record, Action<WeaponRecord> confirmCallback, Action cancelCallback)
    {
        currentRecord = record;
        onConfirm = confirmCallback;
        onCancelled = cancelCallback;

        PopulateContent(record);
        SetVisible(true);
    }

    public void Hide()
    {
        if (!isVisible) return;

        SetVisible(false);
        currentRecord = null;
        onConfirm = null;
        onCancelled = null;
        ClearRows();
    }

    private void PopulateContent(WeaponRecord record)
    {
        if (weaponNameLabel != null)
        {
            weaponNameLabel.text = record?.WeaponName ?? string.Empty;
        }

        PopulateStats(record);
        PopulatePrice(record);
    }

    private void PopulateStats(WeaponRecord record)
    {
        WeaponStats stats = record?.StatsSnapshot ?? record?.WeaponBody?.CurrentStats;

        if (stats == null && record?.WeaponSettings != null)
        {
            stats = BuildStatsFromSettings(record.WeaponSettings);
        }

        if (stats == null)
        {
            ClearRows();
            return;
        }

        var entries = new List<(string label, float value)>
        {
            ("Power", stats.power),
            ("Reload Speed", stats.reloadSpeed),
            ("Accuracy", stats.accuracy),
            ("Rapidity", stats.rapidity),
            ("Recoil", stats.recoil),
            ("Ammo", stats.ammo),
            ("Aim", stats.scope)
        };

        EnsureRowPool(entries.Count);

        for (int i = 0; i < entries.Count; i++)
        {
            WeaponStatRowUI row = activeRows[i];
            (string label, float value) entry = entries[i];
            string valueText = FormatStatValue(entry.value, entry.label == "Ammo");
            row.SetData(entry.label, string.Empty, valueText, false, statsValueColor, statsDeltaColor);
        }
    }

    private void PopulatePrice(WeaponRecord record)
    {
        if (sellingPriceLabel == null) return;

        int price = record?.WeaponSettings != null ? record.WeaponSettings.totalPartCost : 0;
        sellingPriceLabel.text = string.Format(sellingPriceFormat, price);
    }

    private void HandleSellClicked()
    {
        if (!isVisible) return;

        if (currentRecord != null)
        {
            onConfirm?.Invoke(currentRecord);
        }

        Hide();
    }

    private void HandleCloseClicked()
    {
        if (!isVisible) return;

        onCancelled?.Invoke();
        Hide();
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        if (rootPanel != null)
        {
            rootPanel.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    private void EnsureRowPool(int requiredCount)
    {
        if (statsContainer == null || statRowPrefab == null) return;

        while (activeRows.Count < requiredCount)
        {
            WeaponStatRowUI row;
            if (pooledRows.Count > 0)
            {
                row = pooledRows[pooledRows.Count - 1];
                pooledRows.RemoveAt(pooledRows.Count - 1);
                row.gameObject.SetActive(true);
            }
            else
            {
                row = Instantiate(statRowPrefab, statsContainer);
            }

            activeRows.Add(row);
        }

        for (int i = requiredCount; i < activeRows.Count; i++)
        {
            WeaponStatRowUI row = activeRows[i];
            row.gameObject.SetActive(false);
            pooledRows.Add(row);
        }

        if (activeRows.Count > requiredCount)
        {
            activeRows.RemoveRange(requiredCount, activeRows.Count - requiredCount);
        }
    }

    private void ClearRows()
    {
        for (int i = 0; i < activeRows.Count; i++)
        {
            WeaponStatRowUI row = activeRows[i];
            if (row != null)
            {
                row.gameObject.SetActive(false);
                pooledRows.Add(row);
            }
        }

        activeRows.Clear();
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

    private static string FormatStatValue(float value, bool forceInteger)
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
}

