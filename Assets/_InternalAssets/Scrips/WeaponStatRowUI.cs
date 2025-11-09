using TMPro;
using UnityEngine;

public class WeaponStatRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI statDeltaText;
    [SerializeField] private TextMeshProUGUI statValueText;

    public void SetData(string statName, string statDelta, string statValue, bool showDelta, Color valueColor, Color deltaColor)
    {
        if (statNameText != null)
        {
            statNameText.text = statName;
        }

        if (statDeltaText != null)
        {
            if (showDelta && !string.IsNullOrEmpty(statDelta))
            {
                statDeltaText.gameObject.SetActive(true);
                statDeltaText.text = statDelta;
                statDeltaText.color = deltaColor;
            }
            else
            {
                statDeltaText.gameObject.SetActive(false);
                statDeltaText.text = string.Empty;
            }
        }

        if (statValueText != null)
        {
            statValueText.text = statValue;
            statValueText.color = valueColor;
        }
    }
}

