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
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private bool useAimAssist = true;
    [SerializeField] private float aimAssistRadius = 0.5f; // Same as InteractionHandler by default
    
    private Camera playerCamera;
    private InteractionHandler interactionHandler;
    private float lastUpdateTime;
    
    private void Start()
    {
        // Find player camera - try multiple ways
        if (playerCamera == null)
        {
            // Try FirstPersonController
            FirstPersonController fps = FindFirstObjectByType<FirstPersonController>();
            if (fps != null)
            {
                playerCamera = fps.PlayerCamera;
                interactionHandler = fps.GetComponent<InteractionHandler>();
            }
            
            // Fallback to Camera.main
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
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
        
        WeaponBody foundWeaponBody = null;
        WeaponPart foundWeaponPart = null;
        
        // Primary: Raycast from screen center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            foundWeaponBody = hit.collider.GetComponent<WeaponBody>();
            if (foundWeaponBody == null)
            {
                foundWeaponBody = hit.collider.GetComponentInParent<WeaponBody>();
            }
            
            if (foundWeaponBody == null)
            {
                foundWeaponPart = hit.collider.GetComponent<WeaponPart>();
                if (foundWeaponPart == null)
                {
                    foundWeaponPart = hit.collider.GetComponentInParent<WeaponPart>();
                }
            }
        }
        
        // Secondary: Aim assist if enabled and no direct hit
        if (useAimAssist && foundWeaponBody == null && foundWeaponPart == null && aimAssistRadius > 0f)
        {
            Vector3 checkPoint = ray.GetPoint(Mathf.Min(maxDistance * 0.5f, 3f));
            Collider[] nearbyColliders = Physics.OverlapSphere(checkPoint, aimAssistRadius);
            float closestDistance = float.MaxValue;
            
            foreach (Collider col in nearbyColliders)
            {
                // Skip held items (children of camera)
                if (playerCamera != null && col.transform.IsChildOf(playerCamera.transform))
                {
                    continue;
                }
                
                WeaponBody weaponBody = col.GetComponent<WeaponBody>();
                if (weaponBody == null)
                {
                    weaponBody = col.GetComponentInParent<WeaponBody>();
                }
                
                WeaponPart weaponPart = null;
                if (weaponBody == null)
                {
                    weaponPart = col.GetComponent<WeaponPart>();
                    if (weaponPart == null)
                    {
                        weaponPart = col.GetComponentInParent<WeaponPart>();
                    }
                }
                
                if (weaponBody != null || weaponPart != null)
                {
                    Vector3 targetPos = weaponBody != null ? weaponBody.transform.position : weaponPart.transform.position;
                    float dist = Vector3.Distance(targetPos, checkPoint);
                    
                    if (dist < closestDistance)
                    {
                        Vector3 toTarget = targetPos - playerCamera.transform.position;
                        float dotProduct = Vector3.Dot(playerCamera.transform.forward, toTarget.normalized);
                        
                        if (dotProduct > 0.7f)
                        {
                            foundWeaponBody = weaponBody;
                            foundWeaponPart = weaponPart;
                            closestDistance = dist;
                        }
                    }
                }
            }
        }
        
        // Display stats
        string displayText = "";
        bool showPanel = false;
        
        if (foundWeaponBody != null)
        {
            displayText = foundWeaponBody.GetStatsDescription();
            showPanel = true;
        }
        else if (foundWeaponPart != null)
        {
            displayText = foundWeaponPart.GetModifierDescription();
            showPanel = true;
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

