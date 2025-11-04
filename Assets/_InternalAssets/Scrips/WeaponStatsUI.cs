using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponStatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private GameObject statsPanel;
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.1f; // Update UI every 0.1s instead of every frame
    
    private InteractionHandler interactionHandler;
    private float lastUpdateTime;
    private string lastDisplayedText = ""; // Cache to avoid unnecessary updates
    private bool isPanelActive = false; // Track panel state
    
    private void Start()
    {
        // Find InteractionHandler
        interactionHandler = FindFirstObjectByType<InteractionHandler>();
        
        if (interactionHandler == null)
        {
            Debug.LogError("InteractionHandler not found! WeaponStatsUI requires InteractionHandler to work.");
            enabled = false;
            return;
        }
        
        if (statsPanel != null)
        {
            // Warmup: activate panel once to initialize Layout and TMP
            statsPanel.SetActive(true);
            
            if (statsText != null)
            {
                // Trigger TMP mesh generation with dummy text
                statsText.text = "Initializing...\n\nPower: 0\nAccuracy: 0";
                // Force layout rebuild now (not during gameplay)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(statsPanel.GetComponent<RectTransform>());
            }
            
            // Deactivate after warmup
            statsPanel.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Throttle updates for performance
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        UpdateStatsDisplay();
    }
    
    private void UpdateStatsDisplay()
    {
        if (interactionHandler == null) return;
        
        string displayText = "";
        bool showPanel = false;
        
        // Use the same target that InteractionHandler found
        IInteractable currentTarget = interactionHandler.CurrentTarget;
        
        if (currentTarget != null)
        {
            // Try to get WeaponBody from target
            MonoBehaviour targetMB = currentTarget as MonoBehaviour;
            if (targetMB != null)
            {
                WeaponBody weaponBody = targetMB.GetComponent<WeaponBody>();
                if (weaponBody != null)
                {
                    displayText = weaponBody.GetStatsDescription();
                    showPanel = true;
                }
                else
                {
                    // Try to get WeaponPart
                    WeaponPart weaponPart = targetMB.GetComponent<WeaponPart>();
                    if (weaponPart != null)
                    {
                        displayText = weaponPart.GetModifierDescription();
                        showPanel = true;
                    }
                }
            }
        }
        
        // Update UI only if changed (avoid unnecessary Layout rebuilds)
        bool textChanged = displayText != lastDisplayedText;
        bool panelStateChanged = showPanel != isPanelActive;
        
        // Only activate/deactivate if state changed
        if (statsPanel != null && panelStateChanged)
        {
            statsPanel.SetActive(showPanel);
            isPanelActive = showPanel;
        }
        
        // Only update text if it changed
        if (statsText != null && textChanged)
        {
            statsText.text = displayText;
            lastDisplayedText = displayText;
        }
    }
}

