using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Serializable data structures for save system
/// </summary>
[Serializable]
public class WeaponPartSaveData
{
    public PartType partType;
    public string partName = "";
    public int partCost = 0;
    
    // Stat modifiers
    public float powerModifier = 0f;
    public float accuracyModifier = 0f;
    public float rapidityModifier = 0f;
    public float recoilModifier = 0f;
    public float reloadSpeedModifier = 0f;
    public float scopeModifier = 0f;
    public int magazineCapacity = 15;
    
    // Welding state (for barrels)
    public bool isWelded = false;
    public float weldingProgress = 0f;
    
    // Visual data (mesh and material)
    public string meshGUID = ""; // GUID of the mesh asset (Editor only, for reference)
    public string materialGUID = ""; // GUID of the material asset (Editor only, for reference)
    public string meshName = ""; // Name of the mesh (for runtime lookup in ShopPartConfig)
    
    // Lens overlay data (for scopes)
    public string lensOverlayPrefabGUID = ""; // GUID of the lens overlay prefab (for scopes)
    public string lensOverlayName = ""; // Name of the lens overlay child object (for identification)
    
    public WeaponPartSaveData() { }
    
    public WeaponPartSaveData(WeaponPart part)
    {
        if (part == null) return;
        
        partType = part.Type;
        partName = part.PartName;
        partCost = part.PartCost;
        
        // Extract modifiers using reflection (they're private serialized fields)
        var powerField = typeof(WeaponPart).GetField("powerModifier", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var accuracyField = typeof(WeaponPart).GetField("accuracyModifier", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rapidityField = typeof(WeaponPart).GetField("rapidityModifier", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var recoilField = typeof(WeaponPart).GetField("recoilModifier", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var reloadSpeedField = typeof(WeaponPart).GetField("reloadSpeedModifier", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var scopeField = typeof(WeaponPart).GetField("scopeModifier", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var magazineField = typeof(WeaponPart).GetField("magazineCapacity", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (powerField != null) powerModifier = (float)powerField.GetValue(part);
        if (accuracyField != null) accuracyModifier = (float)accuracyField.GetValue(part);
        if (rapidityField != null) rapidityModifier = (float)rapidityField.GetValue(part);
        if (recoilField != null) recoilModifier = (float)recoilField.GetValue(part);
        if (reloadSpeedField != null) reloadSpeedModifier = (float)reloadSpeedField.GetValue(part);
        if (scopeField != null) scopeModifier = (float)scopeField.GetValue(part);
        if (magazineField != null) magazineCapacity = (int)magazineField.GetValue(part);
        
        // Get welding state if this is a barrel
        if (partType == PartType.Barrel)
        {
            WeldingSystem welding = part.GetComponent<WeldingSystem>();
            if (welding != null)
            {
                isWelded = welding.IsWelded;
                weldingProgress = welding.WeldingProgress;
            }
        }
        
        // Save mesh name and GUIDs (GUID for editor reference, name for runtime lookup)
        MeshFilter meshFilter = part.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            // Save mesh name for runtime lookup in ShopPartConfig
            meshName = meshFilter.sharedMesh.name;
            
#if UNITY_EDITOR
            // Save GUID for editor reference (optional)
            string meshPath = UnityEditor.AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
            if (!string.IsNullOrEmpty(meshPath))
            {
                meshGUID = UnityEditor.AssetDatabase.AssetPathToGUID(meshPath);
            }
#endif
        }
        
        MeshRenderer meshRenderer = part.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
#if UNITY_EDITOR
            // Save material GUID for editor reference (optional)
            string materialPath = UnityEditor.AssetDatabase.GetAssetPath(meshRenderer.sharedMaterial);
            if (!string.IsNullOrEmpty(materialPath))
            {
                materialGUID = UnityEditor.AssetDatabase.AssetPathToGUID(materialPath);
            }
#endif
        }
        
        // Save lens overlay data if this is a scope
        if (partType == PartType.Scope)
        {
            // Find lens overlay child (usually named with "lense" or "lens")
            Transform lensChild = null;
            for (int i = 0; i < part.transform.childCount; i++)
            {
                Transform child = part.transform.GetChild(i);
                string childName = child.name.ToLower();
                if (childName.Contains("lense") || childName.Contains("lens") || childName.Contains("overlay"))
                {
                    lensChild = child;
                    lensOverlayName = child.name;
                    break;
                }
            }
            
            // If found, try to get prefab GUID
            if (lensChild != null)
            {
#if UNITY_EDITOR
                // First, try to find the prefab by searching through ShopPartConfig
                // This is more reliable as it uses the same source as PartSpawner
                ShopPartConfig shopConfig = Resources.FindObjectsOfTypeAll<ShopPartConfig>().FirstOrDefault();
                if (shopConfig != null)
                {
                    PartTypeConfig scopeConfig = shopConfig.GetPartTypeConfig(PartType.Scope);
                    if (scopeConfig != null)
                    {
                        // Search through all rarity tiers for lens prefabs
                        foreach (var tier in scopeConfig.rarityTiers)
                        {
                            if (tier != null && tier.partMeshData != null)
                            {
                                foreach (var meshData in tier.partMeshData)
                                {
                                    if (meshData != null && meshData.lensOverlayPrefab != null)
                                    {
                                        // Check if this lens prefab matches the child name
                                        string prefabName = meshData.lensOverlayPrefab.name.ToLower();
                                        string childNameLower = lensOverlayName.ToLower().Replace("(clone)", "").Trim();
                                        
                                        if (prefabName.Contains(childNameLower) || childNameLower.Contains(prefabName))
                                        {
                                            // Found matching prefab, get its GUID
                                            string prefabPath = UnityEditor.AssetDatabase.GetAssetPath(meshData.lensOverlayPrefab);
                                            if (!string.IsNullOrEmpty(prefabPath))
                                            {
                                                lensOverlayPrefabGUID = UnityEditor.AssetDatabase.AssetPathToGUID(prefabPath);
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (!string.IsNullOrEmpty(lensOverlayPrefabGUID)) break;
                            }
                        }
                    }
                }
                
                // Fallback: try to find the prefab by searching for it in the project
                if (string.IsNullOrEmpty(lensOverlayPrefabGUID))
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_InternalAssets/Prefabs/weapon" });
                    foreach (string guid in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                        string lensName = lensOverlayName.ToLower().Replace("(clone)", "").Trim();
                        
                        // Check if prefab name matches lens name or contains similar pattern
                        if (fileName.Contains("lense") || fileName.Contains("lens"))
                        {
                            // Try to match by checking if the prefab name is similar to lens name
                            if (fileName.Contains(lensName) || lensName.Contains(fileName))
                            {
                                lensOverlayPrefabGUID = guid;
                                break;
                            }
                        }
                    }
                }
                
                // Last fallback: try to find by searching all scope lens prefabs
                if (string.IsNullOrEmpty(lensOverlayPrefabGUID))
                {
                    string[] lensGuids = UnityEditor.AssetDatabase.FindAssets("scope*lense t:Prefab", new[] { "Assets/_InternalAssets/Prefabs/weapon" });
                    if (lensGuids.Length > 0)
                    {
                        // Use first available (fallback)
                        lensOverlayPrefabGUID = lensGuids[0];
                    }
                }
#endif
            }
        }
    }
}

[Serializable]
public class WeaponSaveData
{
    public string weaponName = "";
    public WeaponStats statsSnapshot = new WeaponStats();
    
    // Optional parts (null means no part in that slot)
    public WeaponPartSaveData barrelPart = null;
    public WeaponPartSaveData magazinePart = null;
    public WeaponPartSaveData stockPart = null;
    public WeaponPartSaveData scopePart = null;
    
    public WeaponSaveData() { }
    
    public WeaponSaveData(string name, WeaponStats stats, WeaponPart barrel, WeaponPart magazine, WeaponPart stock, WeaponPart scope)
    {
        weaponName = name;
        statsSnapshot = stats != null ? stats.Clone() : new WeaponStats();
        
        if (barrel != null) barrelPart = new WeaponPartSaveData(barrel);
        if (magazine != null) magazinePart = new WeaponPartSaveData(magazine);
        if (stock != null) stockPart = new WeaponPartSaveData(stock);
        if (scope != null) scopePart = new WeaponPartSaveData(scope);
    }
}

[Serializable]
public class WorkbenchSaveData
{
    public WeaponSaveData weaponData = null;
    
    public WorkbenchSaveData() { }
    
    public WorkbenchSaveData(WeaponBody weapon)
    {
        if (weapon == null)
        {
            weaponData = null;
            return;
        }
        
        WeaponPart barrel = weapon.GetPart(PartType.Barrel);
        WeaponPart magazine = weapon.GetPart(PartType.Magazine);
        WeaponPart stock = weapon.GetPart(PartType.Stock);
        WeaponPart scope = weapon.GetPart(PartType.Scope);
        
        weaponData = new WeaponSaveData(
            weapon.WeaponName,
            weapon.CurrentStats,
            barrel,
            magazine,
            stock,
            scope
        );
    }
}

