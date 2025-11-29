using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton for spawning purchased weapon parts with calculated stats
/// </summary>
public class PartSpawner : MonoBehaviour
{
    public static PartSpawner Instance { get; private set; }
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    
    [Header("Audio")]
    [Tooltip("Optional local AudioSource for fallback (if AudioManager not available). Can be left empty.")]
    [SerializeField] private AudioSource audioSource; // Fallback only
    [SerializeField] private AudioClip spawnSound;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Create spawn point if not assigned
        if (spawnPoint == null)
        {
            GameObject spawnObj = new GameObject("PartSpawnPoint");
            spawnObj.transform.SetParent(transform);
            spawnObj.transform.localPosition = Vector3.up;
            spawnPoint = spawnObj.transform;
        }
        
        // Setup audio source if not assigned
        // AudioSource is now optional (fallback only)
        // AudioManager will be used if available
    }
    
    /// <summary>
    /// Spawn a weapon part with calculated stats and specific mesh
    /// </summary>
    public GameObject SpawnPart(
        GameObject universalPrefab,
        Mesh partMesh,
        PartType partType,
        Dictionary<StatInfluence.StatType, float> stats,
        string partName = null,
        GameObject lensOverlayPrefab = null,
        int partCost = 0)
    {
        if (universalPrefab == null)
        {
            Debug.LogError("Cannot spawn: universal part prefab is null!");
            return null;
        }
        
        if (partMesh == null)
        {
            Debug.LogError("Cannot spawn: part mesh is null!");
            return null;
        }
        
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point is not assigned!");
            return null;
        }
        
        // Instantiate universal part at spawn point
        GameObject spawnedPart = Instantiate(universalPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Apply mesh to MeshFilter
        MeshFilter meshFilter = spawnedPart.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = partMesh;
            
            // Update collider to match new mesh geometry
            UpdateCollider(spawnedPart, partMesh);
            
            // Adjust position so geometry center aligns with spawn point
            // Use mesh local bounds and transform to world space through the MeshFilter's transform
            Vector3 localBoundsCenter = partMesh.bounds.center;
            Vector3 worldBoundsCenter = meshFilter.transform.TransformPoint(localBoundsCenter);
            
            // Calculate how much to move the root object
            Vector3 offset = worldBoundsCenter - spawnPoint.position;
            spawnedPart.transform.position -= offset;
        }
        else
        {
            Debug.LogWarning("Universal part prefab doesn't have MeshFilter component!");
        }
        
        // Apply stats and part type to WeaponPart component
        WeaponPart weaponPart = spawnedPart.GetComponent<WeaponPart>();
        if (weaponPart != null)
        {
            // Set part type
            weaponPart.partType = partType;
            
            if (!string.IsNullOrEmpty(partName))
            {
                weaponPart.partName = partName;
            }

            weaponPart.SetCost(partCost);
            
            // Apply stats
            if (stats != null)
            {
                ApplyStats(weaponPart, stats);
            }
        }
        else
        {
            Debug.LogWarning("Universal part prefab doesn't have WeaponPart component!");
        }
        
        // Instantiate lens overlay if provided (for scopes)
        if (lensOverlayPrefab != null)
        {
            GameObject lensOverlay = Instantiate(lensOverlayPrefab, spawnedPart.transform);
            // Lens overlay prefab should have its local position/rotation pre-configured
            // No need to modify transform here
        }
        
        // Play spawn sound
        PlaySpawnSound();
        
        return spawnedPart;
    }
    
    /// <summary>
    /// Update collider to match new mesh geometry
    /// </summary>
    private void UpdateCollider(GameObject obj, Mesh mesh)
    {
        // Check for MeshCollider first
        MeshCollider meshCollider = obj.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null; // Reset first
            meshCollider.sharedMesh = mesh; // Apply new mesh
            return;
        }
        
        // Check for BoxCollider
        BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            // Calculate bounds from mesh
            Bounds meshBounds = mesh.bounds;
            
            // Update BoxCollider to match mesh bounds
            boxCollider.center = meshBounds.center;
            boxCollider.size = meshBounds.size;
            return;
        }
        
        // Check for CapsuleCollider
        CapsuleCollider capsuleCollider = obj.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            // Calculate bounds from mesh
            Bounds meshBounds = mesh.bounds;
            
            // Update CapsuleCollider to approximate mesh bounds
            capsuleCollider.center = meshBounds.center;
            capsuleCollider.height = meshBounds.size.y;
            capsuleCollider.radius = Mathf.Max(meshBounds.size.x, meshBounds.size.z) * 0.5f;
            return;
        }
        
        // Check for SphereCollider
        SphereCollider sphereCollider = obj.GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            // Calculate bounds from mesh
            Bounds meshBounds = mesh.bounds;
            
            // Update SphereCollider to approximate mesh bounds
            sphereCollider.center = meshBounds.center;
            sphereCollider.radius = Mathf.Max(meshBounds.size.x, meshBounds.size.y, meshBounds.size.z) * 0.5f;
            return;
        }
        
        // No collider found - log warning
        Debug.LogWarning($"No collider found on {obj.name}. Consider adding BoxCollider or MeshCollider to universal part prefab.");
    }
    
    /// <summary>
    /// Apply calculated stats to WeaponPart component using reflection
    /// </summary>
    private void ApplyStats(WeaponPart weaponPart, Dictionary<StatInfluence.StatType, float> stats)
    {
        // Use reflection to access private fields
        System.Type weaponPartType = typeof(WeaponPart);
        
        foreach (var stat in stats)
        {
            float value = stat.Value;
            
            switch (stat.Key)
            {
                case StatInfluence.StatType.Power:
                    SetPrivateField(weaponPartType, weaponPart, "powerModifier", value);
                    break;
                    
                case StatInfluence.StatType.Accuracy:
                    SetPrivateField(weaponPartType, weaponPart, "accuracyModifier", value);
                    break;
                    
                case StatInfluence.StatType.Rapidity:
                    SetPrivateField(weaponPartType, weaponPart, "rapidityModifier", value);
                    break;
                    
                case StatInfluence.StatType.Recoil:
                    SetPrivateField(weaponPartType, weaponPart, "recoilModifier", value);
                    break;
                    
                case StatInfluence.StatType.ReloadSpeed:
                    SetPrivateField(weaponPartType, weaponPart, "reloadSpeedModifier", value);
                    break;
                    
                case StatInfluence.StatType.Aim:
                    SetPrivateField(weaponPartType, weaponPart, "scopeModifier", value);
                    break;
                    
                case StatInfluence.StatType.Ammo:
                    // Ammo is stored as int in magazineCapacity
                    SetPrivateField(weaponPartType, weaponPart, "magazineCapacity", (int)value);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Set private field using reflection
    /// </summary>
    private void SetPrivateField(System.Type type, object instance, string fieldName, float value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(instance, value);
        }
        else
        {
            Debug.LogWarning($"Field '{fieldName}' not found in WeaponPart!");
        }
    }
    
    /// <summary>
    /// Set private field using reflection (int overload)
    /// </summary>
    private void SetPrivateField(System.Type type, object instance, string fieldName, int value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(instance, value);
        }
        else
        {
            Debug.LogWarning($"Field '{fieldName}' not found in WeaponPart!");
        }
    }
    
    /// <summary>
    /// Play spawn sound effect
    /// </summary>
    private void PlaySpawnSound()
    {
        if (spawnSound == null) return;
        
        // Use AudioManager if available, otherwise fallback to local AudioSource
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(spawnSound, volume: 0.7f);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }
    }
    
    // Public property for spawn point position (for debugging/gizmos)
    public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : Vector3.zero;
    
    // Public property for spawn point transform (for tutorial system)
    public Transform SpawnPoint => spawnPoint;
}

