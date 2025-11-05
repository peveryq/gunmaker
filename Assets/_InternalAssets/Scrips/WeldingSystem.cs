using UnityEngine;

/// <summary>
/// Manages welding state for weapon parts on the workbench
/// </summary>
public class WeldingSystem : MonoBehaviour
{
    [Header("Welding Settings")]
    [SerializeField] private float weldingProgress = 0f; // 0 to 100
    [SerializeField] private bool isWelded = false;
    
    private WeaponPart weaponPart;
    
    private void Awake()
    {
        weaponPart = GetComponent<WeaponPart>();
    }
    
    /// <summary>
    /// Add welding progress
    /// </summary>
    /// <param name="amount">Amount to add (0-100)</param>
    /// <returns>Current welding progress</returns>
    public float AddWeldingProgress(float amount)
    {
        if (isWelded) return 100f;
        
        weldingProgress = Mathf.Clamp(weldingProgress + amount, 0f, 100f);
        
        if (weldingProgress >= 100f)
        {
            isWelded = true;
            weldingProgress = 100f;
        }
        
        return weldingProgress;
    }
    
    /// <summary>
    /// Reset welding state (when part is removed from weapon)
    /// </summary>
    public void ResetWelding()
    {
        weldingProgress = 0f;
        isWelded = false;
    }
    
    /// <summary>
    /// Is this part fully welded?
    /// </summary>
    public bool IsWelded => isWelded;
    
    /// <summary>
    /// Current welding progress (0-100)
    /// </summary>
    public float WeldingProgress => weldingProgress;
    
    /// <summary>
    /// Does this part require welding?
    /// </summary>
    public bool RequiresWelding => weaponPart != null && weaponPart.Type == PartType.Barrel;
}

