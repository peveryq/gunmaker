using UnityEngine;
using TMPro;

public class WeldingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject weldingPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private UnityEngine.UI.Image progressFill;
    
    [Header("Settings")]
    [SerializeField] private string needsWeldingLabel = "Needs Welding";
    [SerializeField] private float updateInterval = 0.05f; // Update UI every 50ms
    
    private WeldingSystem currentTarget;
    private float lastUpdateTime;
    private float lastDisplayedProgress = -1f;
    
    private void Start()
    {
        if (statusText != null)
        {
            statusText.text = needsWeldingLabel;
        }
        
        SetPanelActive(false);
    }
    
    private void Update()
    {
        Refresh(false);
    }
    
    public void SetTarget(WeldingSystem target)
    {
        if (currentTarget == target)
        {
            Refresh(true);
            return;
        }
        
        currentTarget = target;
        lastDisplayedProgress = -1f;
        Refresh(true);
    }
    
    private void Refresh(bool force)
    {
        if (currentTarget == null || currentTarget.IsWelded || !currentTarget.RequiresWelding)
        {
            if (force)
            {
                SetPanelActive(false);
            }
            return;
        }
        
        SetPanelActive(true);
        
        if (!force && Time.time - lastUpdateTime < updateInterval)
        {
            return;
        }
        
        lastUpdateTime = Time.time;
        
        float rawProgress = Mathf.Clamp(currentTarget.WeldingProgress, 0f, 100f);
        float normalized = rawProgress / 100f;
        
        if (!force && Mathf.Approximately(lastDisplayedProgress, rawProgress))
        {
            return;
        }
        
        lastDisplayedProgress = rawProgress;
        
        if (progressFill != null)
        {
            progressFill.fillAmount = normalized;
        }
        
        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(rawProgress)}%";
        }
    }
    
    private void SetPanelActive(bool active)
    {
        if (weldingPanel == null) return;
        
        if (weldingPanel.activeSelf != active)
        {
            weldingPanel.SetActive(active);
        }
    }
}

