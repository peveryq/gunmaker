using UnityEngine;
using UnityEngine.UI;

public class WeaponStatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text statsText;
    [SerializeField] private GameObject statsPanel;
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.1f; // Update UI every 0.1s instead of every frame
    
    private Camera playerCamera;
    private float lastUpdateTime;
    
    private void Start()
    {
        // Find player camera
        FirstPersonController fps = FindFirstObjectByType<FirstPersonController>();
        if (fps != null)
        {
            playerCamera = fps.PlayerCamera;
        }
        
        if (statsPanel != null)
        {
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
        if (playerCamera == null) return;
        
        // Raycast from center of screen
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        string displayText = "";
        bool showPanel = false;
        
        if (Physics.Raycast(ray, out hit, 10f))
        {
            // Check for weapon body
            WeaponBody weaponBody = hit.collider.GetComponent<WeaponBody>();
            if (weaponBody == null)
            {
                weaponBody = hit.collider.GetComponentInParent<WeaponBody>();
            }
            
            if (weaponBody != null)
            {
                displayText = weaponBody.GetStatsDescription();
                showPanel = true;
            }
            else
            {
                // Check for weapon part
                WeaponPart part = hit.collider.GetComponent<WeaponPart>();
                if (part == null)
                {
                    part = hit.collider.GetComponentInParent<WeaponPart>();
                }
                
                if (part != null)
                {
                    displayText = part.GetModifierDescription();
                    showPanel = true;
                }
            }
        }
        
        // Update UI
        if (statsPanel != null)
        {
            statsPanel.SetActive(showPanel);
        }
        
        if (statsText != null)
        {
            statsText.text = displayText;
        }
    }
}

