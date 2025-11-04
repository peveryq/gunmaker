using UnityEngine;

/// <summary>
/// Singleton ScriptableObject that stores default settings for each weapon part type
/// </summary>
[CreateAssetMenu(fileName = "PartTypeDefaultSettings", menuName = "Weapon/Part Type Default Settings")]
public class PartTypeDefaultSettings : ScriptableObject
{
    private static PartTypeDefaultSettings instance;
    
    public static PartTypeDefaultSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<PartTypeDefaultSettings>("PartTypeDefaultSettings");
                
                if (instance == null)
                {
                    Debug.LogWarning("PartTypeDefaultSettings not found in Resources folder! Create it via Assets > Create > Weapon > Part Type Default Settings and place in Resources folder.");
                }
            }
            return instance;
        }
    }
    
    [Header("Body (Weapon) Settings")]
    [Tooltip("Position when holding weapon body")]
    public Vector3 bodyHeldPosition = new Vector3(0f, -0.3f, 0.5f);
    public Vector3 bodyHeldRotation = new Vector3(0, 0, 0);
    public Vector3 bodyDropRotation = new Vector3(0, 90, 0);
    
    [Header("Barrel Settings")]
    [Tooltip("Position when holding barrel")]
    public Vector3 barrelHeldPosition = new Vector3(0.3f, -0.3f, 0.5f);
    public Vector3 barrelHeldRotation = new Vector3(0, 0, 0);
    public Vector3 barrelDropRotation = new Vector3(0, 0, 0);
    
    [Header("Magazine Settings")]
    [Tooltip("Position when holding magazine")]
    public Vector3 magazineHeldPosition = new Vector3(0f, -0.4f, 0.4f);
    public Vector3 magazineHeldRotation = new Vector3(0, 0, 0);
    public Vector3 magazineDropRotation = new Vector3(0, 90, 0);
    
    [Header("Stock Settings")]
    [Tooltip("Position when holding stock/butt")]
    public Vector3 stockHeldPosition = new Vector3(0f, -0.2f, 0.6f);
    public Vector3 stockHeldRotation = new Vector3(0, 0, 0);
    public Vector3 stockDropRotation = new Vector3(90, 0, 0);
    
    [Header("Scope Settings")]
    [Tooltip("Position when holding scope")]
    public Vector3 scopeHeldPosition = new Vector3(0f, -0.3f, 0.4f);
    public Vector3 scopeHeldRotation = new Vector3(0, 0, 0);
    public Vector3 scopeDropRotation = new Vector3(0, 90, 0);
}

