using UnityEngine;
using TMPro;

public class WeldingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject weldingPanel;
    [SerializeField] private TextMeshProUGUI weldingText;
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.05f; // Update UI every 50ms
    
    private float lastUpdateTime;
    private float lastDisplayedProgress = -1f;
    
    private void Start()
    {
        if (weldingPanel != null)
        {
            weldingPanel.SetActive(false);
        }
    }
    
    public void ShowWeldingUI(float progress)
    {
        if (weldingPanel != null && !weldingPanel.activeSelf)
        {
            weldingPanel.SetActive(true);
        }
        
        // Throttle updates for performance
        if (Time.time - lastUpdateTime < updateInterval && Mathf.Approximately(lastDisplayedProgress, progress))
        {
            return;
        }
        
        lastUpdateTime = Time.time;
        lastDisplayedProgress = progress;
        
        if (weldingText != null)
        {
            weldingText.text = $"Welding {Mathf.RoundToInt(progress)}%";
        }
    }
    
    public void HideWeldingUI()
    {
        if (weldingPanel != null && weldingPanel.activeSelf)
        {
            weldingPanel.SetActive(false);
        }
        
        lastDisplayedProgress = -1f;
    }
}

