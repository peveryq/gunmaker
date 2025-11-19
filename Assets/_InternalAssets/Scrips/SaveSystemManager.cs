using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YG;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Singleton manager for save/load system integrated with YG2 Storage module
/// Centralized auto-save system with 20s intervals (workshop only)
/// </summary>
public class SaveSystemManager : MonoBehaviour
{
    private static SaveSystemManager instance;
    private static bool applicationIsQuitting;
    
    [Header("Auto-Save Settings")]
    [SerializeField] private float autoSaveInterval = 20f; // 20 seconds
    
    private LocationManager locationManager;
    private float autoSaveTimer;
    private bool isAutoSaveActive = false;
    private Coroutine autoSaveCoroutine;
    private bool hasLoadedData = false; // Flag to prevent double-loading
    
    public static SaveSystemManager Instance
    {
        get
        {
            if (instance == null && !applicationIsQuitting)
            {
                Bootstrap();
            }
            return instance;
        }
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (applicationIsQuitting || instance != null) return;
        
        GameObject host = new GameObject("SaveSystemManager");
        instance = host.AddComponent<SaveSystemManager>();
        DontDestroyOnLoad(host);
    }
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        
        // Reset timer
        autoSaveTimer = 0f;
    }
    
    private void Start()
    {
        // Find LocationManager
        locationManager = LocationManager.Instance ?? FindFirstObjectByType<LocationManager>();
        
        // Subscribe to location changes
        if (locationManager != null)
        {
            locationManager.OnLocationChangedEvent += HandleLocationChanged;
            
            // Initialize based on current location
            HandleLocationChanged(locationManager.CurrentLocation);
        }
        else
        {
            // If no LocationManager, start auto-save (assume workshop)
            StartAutoSave();
        }
        
        // Explicitly try to load data if YG2 is already initialized
        // This handles cases where onGetSDKData was called before we subscribed
        StartCoroutine(TryLoadDataOnStart());
    }
    
    private System.Collections.IEnumerator TryLoadDataOnStart()
    {
        // Wait a bit for YG2 to fully initialize
        yield return new WaitForSeconds(0.5f);
        
        // Try multiple times in case YG2 takes longer to initialize
        for (int attempt = 0; attempt < 5; attempt++)
        {
            // Check if YG2 is initialized and data exists
            if (YG2.isSDKEnabled && YG2.saves != null)
            {
                if (YG2.saves.idSave > 0 && !hasLoadedData)
                {
                    // Data exists but we might have missed the onGetSDKData event
                    // Try to load it explicitly
                    Debug.Log($"SaveSystemManager: Detected existing save data (attempt {attempt + 1}), loading...");
                    Debug.Log($"SaveSystemManager: Save data - Money: {YG2.saves.playerMoney}, " +
                              $"Weapons: {(YG2.saves.savedWeapons != null ? YG2.saves.savedWeapons.Count : 0)}, " +
                              $"Workbench: {(YG2.saves.workbenchWeapon != null ? "has weapon" : "empty")}, " +
                              $"Save ID: {YG2.saves.idSave}");
                    LoadGameData();
                    yield break; // Exit coroutine after successful load
                }
                else if (YG2.saves.idSave == 0)
                {
                    // No saves yet, but YG2 is initialized - this is fine for first run
                    Debug.Log("SaveSystemManager: No save data found (first run).");
                    hasLoadedData = true; // Mark as loaded to prevent retries
                    yield break;
                }
                else if (hasLoadedData)
                {
                    // Already loaded
                    yield break;
                }
            }
            
            // Wait before next attempt
            if (attempt < 4)
            {
                yield return new WaitForSeconds(0.3f);
            }
        }
        
        // If we get here, YG2 might not be initialized yet
        if (!YG2.isSDKEnabled)
        {
            Debug.LogWarning("SaveSystemManager: YG2 SDK not initialized after 2 seconds, waiting for onGetSDKData event...");
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to YG2 save/load events
        YG2.onGetSDKData += LoadGameData;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from YG2 save/load events
        YG2.onGetSDKData -= LoadGameData;
        
        // Unsubscribe from location changes
        if (locationManager != null)
        {
            locationManager.OnLocationChangedEvent -= HandleLocationChanged;
        }
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
        
        // Stop auto-save coroutine
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
        }
    }
    
    private void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
    
    private void Update()
    {
        // Update timer if auto-save is active and we're in workshop
        if (isAutoSaveActive)
        {
            // Double-check we're in workshop
            if (locationManager != null && locationManager.CurrentLocation == LocationManager.LocationType.Workshop)
            {
                autoSaveTimer += Time.deltaTime;
                
                if (autoSaveTimer >= autoSaveInterval)
                {
                    TriggerAutoSave();
                    autoSaveTimer = 0f;
                }
            }
            else if (locationManager == null)
            {
                // If LocationManager is null, assume workshop and continue auto-save
                autoSaveTimer += Time.deltaTime;
                
                if (autoSaveTimer >= autoSaveInterval)
                {
                    TriggerAutoSave();
                    autoSaveTimer = 0f;
                }
            }
        }
    }
    
    private void HandleLocationChanged(LocationManager.LocationType newLocation)
    {
        if (newLocation == LocationManager.LocationType.Workshop)
        {
            // Reset timer and start auto-save when entering workshop
            ResetAutoSaveTimer();
            StartAutoSave();
        }
        else if (newLocation == LocationManager.LocationType.TestingRange)
        {
            // Stop auto-save when entering testing range
            StopAutoSave();
        }
    }
    
    private void StartAutoSave()
    {
        if (isAutoSaveActive) return;
        
        isAutoSaveActive = true;
        autoSaveTimer = 0f;
        
        Debug.Log("SaveSystemManager: Auto-save started.");
    }
    
    private void StopAutoSave()
    {
        if (!isAutoSaveActive) return;
        
        isAutoSaveActive = false;
        autoSaveTimer = 0f;
        
        Debug.Log("SaveSystemManager: Auto-save stopped.");
    }
    
    private void ResetAutoSaveTimer()
    {
        autoSaveTimer = 0f;
    }
    
    private void TriggerAutoSave()
    {
        if (!YG2.isSDKEnabled)
        {
            Debug.LogWarning("SaveSystemManager: Cannot auto-save - YG2 SDK not initialized yet.");
            return;
        }
        
        // Perform save
        SaveGameData(showUI: true);
    }
    
    /// <summary>
    /// Save current game state to YG2.saves
    /// </summary>
    /// <param name="showUI">Whether to show auto-save UI indicator</param>
    public void SaveGameData(bool showUI = false)
    {
        if (!YG2.isSDKEnabled)
        {
            Debug.LogWarning("SaveSystemManager: Cannot save - YG2 SDK not initialized yet.");
            return;
        }
        
        try
        {
            // Show auto-save UI if requested
            if (showUI && GameplayHUD.Instance != null)
            {
                GameplayHUD.Instance.ShowAutoSaveIndicator();
            }
            
            // Save money
            if (MoneySystem.Instance != null)
            {
                YG2.saves.playerMoney = MoneySystem.Instance.CurrentMoney;
            }
            
            // Save weapon slots
            if (WeaponSlotManager.Instance != null)
            {
                YG2.saves.savedWeapons = WeaponSlotManager.Instance.GetSaveData();
            }
            
            // Save workbench weapon
            Workbench workbench = FindObjectOfType<Workbench>();
            if (workbench != null && workbench.MountedWeapon != null)
            {
                YG2.saves.workbenchWeapon = new WorkbenchSaveData(workbench.MountedWeapon);
            }
            else
            {
                YG2.saves.workbenchWeapon = null;
            }
            
            // Save to YG2 storage
            YG2.SaveProgress();
            
            // Log save details for debugging
            Debug.Log($"SaveSystemManager: Game data saved successfully. " +
                      $"Money: {YG2.saves.playerMoney}, " +
                      $"Weapons: {(YG2.saves.savedWeapons != null ? YG2.saves.savedWeapons.Count : 0)}, " +
                      $"Workbench: {(YG2.saves.workbenchWeapon != null ? "has weapon" : "empty")}, " +
                      $"Save ID: {YG2.saves.idSave}");
            
            if (showUI)
            {
                Debug.Log("SaveSystemManager: Auto-save completed with UI indicator.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystemManager: Error saving game data - {e.Message}");
        }
    }
    
    /// <summary>
    /// Load game state from YG2.saves (called by YG2.onGetSDKData event or explicitly)
    /// </summary>
    private void LoadGameData()
    {
        // Prevent double-loading
        if (hasLoadedData)
        {
            Debug.Log("SaveSystemManager: Data already loaded, skipping...");
            return;
        }
        
        if (!YG2.isSDKEnabled)
        {
            Debug.LogWarning("SaveSystemManager: Cannot load - YG2 SDK not initialized yet.");
            return;
        }
        
        // Check if there's actually data to load
        if (YG2.saves == null || YG2.saves.idSave == 0)
        {
            Debug.Log("SaveSystemManager: No save data to load (first run).");
            hasLoadedData = true; // Mark as loaded to prevent retries
            return;
        }
        
        try
        {
            hasLoadedData = true; // Mark as loading to prevent double-loading
            
            // Load money
            if (MoneySystem.Instance != null)
            {
                // Use a coroutine to ensure MoneySystem is fully initialized
                StartCoroutine(LoadMoneyDelayed());
            }
            
            // Load weapon slots (deferred - after scene loads)
            StartCoroutine(LoadWeaponSlotsDelayed());
            
            // Load workbench weapon (deferred - after scene loads)
            StartCoroutine(LoadWorkbenchDelayed());
            
            Debug.Log("SaveSystemManager: Game data loading initiated.");
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystemManager: Error loading game data - {e.Message}");
            hasLoadedData = false; // Reset flag on error to allow retry
        }
    }
    
    private System.Collections.IEnumerator LoadMoneyDelayed()
    {
        // Wait for MoneySystem to be ready
        yield return null;
        
        if (MoneySystem.Instance != null && YG2.saves != null)
        {
            // Set money directly (bypassing AddMoney to avoid event spam during load)
            MoneySystem.Instance.SetMoneyDirect(YG2.saves.playerMoney);
            
            Debug.Log($"SaveSystemManager: Money loaded - {YG2.saves.playerMoney}");
        }
    }
    
    private System.Collections.IEnumerator LoadWeaponSlotsDelayed()
    {
        // Wait for WeaponSlotManager to be ready and scene to load
        yield return new WaitForSeconds(0.1f);
        
        if (WeaponSlotManager.Instance != null && YG2.saves != null && YG2.saves.savedWeapons != null)
        {
            // Filter out null entries and invalid weapons before loading
            int validWeaponsCount = 0;
            for (int i = 0; i < YG2.saves.savedWeapons.Count; i++)
            {
                var weaponData = YG2.saves.savedWeapons[i];
                if (weaponData != null)
                {
                    // Check if weapon has valid data
                    bool hasValidData = !string.IsNullOrWhiteSpace(weaponData.weaponName) ||
                                       (weaponData.barrelPart != null && !string.IsNullOrWhiteSpace(weaponData.barrelPart.partName)) ||
                                       (weaponData.magazinePart != null && !string.IsNullOrWhiteSpace(weaponData.magazinePart.partName)) ||
                                       (weaponData.stockPart != null && !string.IsNullOrWhiteSpace(weaponData.stockPart.partName)) ||
                                       (weaponData.scopePart != null && !string.IsNullOrWhiteSpace(weaponData.scopePart.partName));
                    
                    if (!hasValidData)
                    {
                        // Replace invalid weapon data with null
                        YG2.saves.savedWeapons[i] = null;
                    }
                    else
                    {
                        validWeaponsCount++;
                    }
                }
            }
            
            // Load weapons into slots using WeaponSlotManager's helper method
            WeaponSlotManager.Instance.LoadFromSaveData(YG2.saves.savedWeapons, RestoreWeaponFromSaveData);
            
            Debug.Log($"SaveSystemManager: Weapon slots loaded - {validWeaponsCount} valid weapons out of {YG2.saves.savedWeapons.Count} total entries");
        }
    }
    
    private System.Collections.IEnumerator LoadWorkbenchDelayed()
    {
        // Wait for Workbench to be ready and scene to load (after weapon slots are loaded)
        yield return new WaitForSeconds(0.3f);
        
        Workbench workbench = FindObjectOfType<Workbench>();
        if (workbench == null) yield break;
        
        // First, clear any existing weapon on workbench (from previous session or leftover)
        if (workbench.MountedWeapon != null)
        {
            WeaponBody existingWeapon = workbench.MountedWeapon;
            workbench.DetachMountedWeapon(existingWeapon);
            workbench.ResetMountState();
            
            // Destroy the old weapon if it's not in any slot
            if (WeaponSlotManager.Instance != null)
            {
                int slotIndex = WeaponSlotManager.Instance.IndexOf(existingWeapon);
                if (slotIndex < 0)
                {
                    // Weapon is not in any slot, destroy it
                    Destroy(existingWeapon.gameObject);
                    Debug.Log("SaveSystemManager: Destroyed leftover weapon from workbench.");
                }
            }
        }
        
        if (YG2.saves != null && YG2.saves.workbenchWeapon != null && YG2.saves.workbenchWeapon.weaponData != null)
        {
            var weaponData = YG2.saves.workbenchWeapon.weaponData;
            
            // Validate workbench weapon data before restoring
            bool hasValidData = !string.IsNullOrWhiteSpace(weaponData.weaponName) ||
                               (weaponData.barrelPart != null && !string.IsNullOrWhiteSpace(weaponData.barrelPart.partName)) ||
                               (weaponData.magazinePart != null && !string.IsNullOrWhiteSpace(weaponData.magazinePart.partName)) ||
                               (weaponData.stockPart != null && !string.IsNullOrWhiteSpace(weaponData.stockPart.partName)) ||
                               (weaponData.scopePart != null && !string.IsNullOrWhiteSpace(weaponData.scopePart.partName));
            
            if (hasValidData)
            {
                WeaponBody weaponBody = null;
                
                // Check if this weapon is already in a slot (same weapon name)
                // If yes, use that weapon instance instead of creating a new one
                if (WeaponSlotManager.Instance != null && !string.IsNullOrWhiteSpace(weaponData.weaponName))
                {
                    // Find weapon in slots by name
                    foreach (var record in WeaponSlotManager.Instance.GetAllRecords())
                    {
                        if (record != null && record.WeaponBody != null && 
                            record.WeaponBody.WeaponName == weaponData.weaponName)
                        {
                            weaponBody = record.WeaponBody;
                            Debug.Log($"SaveSystemManager: Using existing weapon from slot for workbench: {weaponData.weaponName}");
                            break;
                        }
                    }
                }
                
                // If weapon not found in slots, create new one
                if (weaponBody == null)
                {
                    weaponBody = RestoreWeaponFromSaveData(weaponData);
                }
                
                if (weaponBody != null)
                {
                    // Ensure weapon is active and properly positioned before mounting
                    if (!weaponBody.gameObject.activeSelf)
                    {
                        weaponBody.gameObject.SetActive(true);
                    }
                    
                    // Ensure weapon is not parented to anything before mounting
                    if (weaponBody.transform.parent != null)
                    {
                        weaponBody.transform.SetParent(null, true);
                    }
                    
                    // Mount weapon on workbench
                    workbench.MountWeaponForLoad(weaponBody);
                    
                    Debug.Log("SaveSystemManager: Workbench weapon loaded.");
                }
            }
            else
            {
                // Clear invalid workbench weapon data
                YG2.saves.workbenchWeapon = null;
                Debug.Log("SaveSystemManager: Skipped invalid workbench weapon data.");
            }
        }
    }
    
    /// <summary>
    /// Create WeaponSaveData from WeaponBody
    /// </summary>
    private WeaponSaveData CreateWeaponSaveData(WeaponBody weaponBody)
    {
        if (weaponBody == null) return null;
        
        WeaponPart barrel = weaponBody.GetPart(PartType.Barrel);
        WeaponPart magazine = weaponBody.GetPart(PartType.Magazine);
        WeaponPart stock = weaponBody.GetPart(PartType.Stock);
        WeaponPart scope = weaponBody.GetPart(PartType.Scope);
        
        return new WeaponSaveData(
            weaponBody.WeaponName,
            weaponBody.CurrentStats,
            barrel,
            magazine,
            stock,
            scope
        );
    }
    
    /// <summary>
    /// Restore WeaponBody from WeaponSaveData
    /// Note: This requires WeaponBody to have restoration helper methods
    /// </summary>
    private WeaponBody RestoreWeaponFromSaveData(WeaponSaveData saveData)
    {
        if (saveData == null) return null;
        
        // Validate save data - skip empty/invalid weapons
        // A weapon is considered valid if it has at least a barrel part or a non-empty name
        bool hasValidData = false;
        
        if (!string.IsNullOrWhiteSpace(saveData.weaponName))
        {
            hasValidData = true;
        }
        else if (saveData.barrelPart != null && !string.IsNullOrWhiteSpace(saveData.barrelPart.partName))
        {
            hasValidData = true;
        }
        else if (saveData.magazinePart != null && !string.IsNullOrWhiteSpace(saveData.magazinePart.partName))
        {
            hasValidData = true;
        }
        else if (saveData.stockPart != null && !string.IsNullOrWhiteSpace(saveData.stockPart.partName))
        {
            hasValidData = true;
        }
        else if (saveData.scopePart != null && !string.IsNullOrWhiteSpace(saveData.scopePart.partName))
        {
            hasValidData = true;
        }
        
        if (!hasValidData)
        {
            // Skip empty/invalid weapon data
            Debug.Log("SaveSystemManager: Skipping empty/invalid weapon save data.");
            return null;
        }
        
        // Get workbench to access emptyWeaponBodyPrefab
        Workbench workbench = FindObjectOfType<Workbench>();
        if (workbench == null)
        {
            Debug.LogError("SaveSystemManager: Cannot restore weapon - Workbench not found.");
            return null;
        }
        
        // Use reflection to get emptyWeaponBodyPrefab
        var prefabField = typeof(Workbench).GetField("emptyWeaponBodyPrefab", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (prefabField == null)
        {
            Debug.LogError("SaveSystemManager: Cannot restore weapon - emptyWeaponBodyPrefab field not found.");
            return null;
        }
        
        GameObject emptyWeaponBodyPrefab = prefabField.GetValue(workbench) as GameObject;
        if (emptyWeaponBodyPrefab == null)
        {
            Debug.LogError("SaveSystemManager: Cannot restore weapon - emptyWeaponBodyPrefab is null.");
            return null;
        }
        
        // Create weapon body at a safe hidden position (will be positioned by WeaponSlotManager or Workbench)
        // Use a far away position so it doesn't interfere with gameplay
        // Note: If this weapon will be mounted on workbench, it will be repositioned immediately
        Vector3 safePosition = new Vector3(1000f, 1000f, 1000f);
        GameObject weaponObj = Instantiate(emptyWeaponBodyPrefab, safePosition, Quaternion.identity);
        
        // Immediately deactivate to hide it until it's properly positioned
        weaponObj.SetActive(false);
        WeaponBody weaponBody = weaponObj.GetComponent<WeaponBody>();
        if (weaponBody == null)
        {
            Debug.LogError("SaveSystemManager: Cannot restore weapon - WeaponBody component not found.");
            Destroy(weaponObj);
            return null;
        }
        
        // Set name (use default if empty)
        string weaponName = !string.IsNullOrWhiteSpace(saveData.weaponName) 
            ? saveData.weaponName 
            : "Custom Weapon";
        weaponBody.SetWeaponName(weaponName);
        
        // Restore parts using WeaponBody helper method
        if (saveData.barrelPart != null && !string.IsNullOrWhiteSpace(saveData.barrelPart.partName))
        {
            WeaponPart part = RestorePartFromSaveData(saveData.barrelPart);
            if (part != null) weaponBody.InstallPart(part);
        }
        
        if (saveData.magazinePart != null && !string.IsNullOrWhiteSpace(saveData.magazinePart.partName))
        {
            WeaponPart part = RestorePartFromSaveData(saveData.magazinePart);
            if (part != null) weaponBody.InstallPart(part);
        }
        
        if (saveData.stockPart != null && !string.IsNullOrWhiteSpace(saveData.stockPart.partName))
        {
            WeaponPart part = RestorePartFromSaveData(saveData.stockPart);
            if (part != null) weaponBody.InstallPart(part);
        }
        
        if (saveData.scopePart != null && !string.IsNullOrWhiteSpace(saveData.scopePart.partName))
        {
            WeaponPart part = RestorePartFromSaveData(saveData.scopePart);
            if (part != null) weaponBody.InstallPart(part);
        }
        
        // Update stats (they should be calculated from parts, but ensure snapshot is applied)
        weaponBody.UpdateWeaponStats();
        
        return weaponBody;
    }
    
    /// <summary>
    /// Restore WeaponPart from WeaponPartSaveData using Part.prefab
    /// </summary>
    private WeaponPart RestorePartFromSaveData(WeaponPartSaveData saveData)
    {
        if (saveData == null) return null;
        
        // Load Part.prefab from Assets
        GameObject partPrefab = null;
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(saveData.meshGUID))
        {
            // Try to load Part.prefab directly
            partPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_InternalAssets/Prefabs/weapon/Part.prefab");
        }
#endif
        
        // Fallback: Try to get from ShopPartConfig if available
        if (partPrefab == null)
        {
            ShopPartConfig shopConfig = Resources.FindObjectsOfTypeAll<ShopPartConfig>().FirstOrDefault();
            if (shopConfig != null && shopConfig.universalPartPrefab != null)
            {
                partPrefab = shopConfig.universalPartPrefab;
            }
        }
        
        // If still null, create basic GameObject (fallback)
        GameObject partObj = null;
        if (partPrefab != null)
        {
            partObj = Instantiate(partPrefab);
        }
        else
        {
            Debug.LogWarning("SaveSystemManager: Part.prefab not found, creating basic part GameObject.");
            partObj = new GameObject($"Restored_{saveData.partType}_{saveData.partName}");
        }
        
        WeaponPart weaponPart = partObj.GetComponent<WeaponPart>();
        if (weaponPart == null)
        {
            weaponPart = partObj.AddComponent<WeaponPart>();
        }
        
        // Set basic properties
        weaponPart.partType = saveData.partType;
        weaponPart.partName = saveData.partName;
        weaponPart.SetCost(saveData.partCost);
        
        // Set modifiers using reflection
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
        
        if (powerField != null) powerField.SetValue(weaponPart, saveData.powerModifier);
        if (accuracyField != null) accuracyField.SetValue(weaponPart, saveData.accuracyModifier);
        if (rapidityField != null) rapidityField.SetValue(weaponPart, saveData.rapidityModifier);
        if (recoilField != null) recoilField.SetValue(weaponPart, saveData.recoilModifier);
        if (reloadSpeedField != null) reloadSpeedField.SetValue(weaponPart, saveData.reloadSpeedModifier);
        if (scopeField != null) scopeField.SetValue(weaponPart, saveData.scopeModifier);
        if (magazineField != null) magazineField.SetValue(weaponPart, saveData.magazineCapacity);
        
        // Restore mesh and material
        if (!string.IsNullOrEmpty(saveData.meshGUID) || !string.IsNullOrEmpty(saveData.materialGUID))
        {
#if UNITY_EDITOR
            MeshFilter meshFilter = partObj.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = partObj.AddComponent<MeshFilter>();
            }
            
            MeshRenderer meshRenderer = partObj.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = partObj.AddComponent<MeshRenderer>();
            }
            
            // Load mesh from GUID
            if (!string.IsNullOrEmpty(saveData.meshGUID))
            {
                string meshPath = UnityEditor.AssetDatabase.GUIDToAssetPath(saveData.meshGUID);
                if (!string.IsNullOrEmpty(meshPath))
                {
                    Mesh mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (mesh != null)
                    {
                        meshFilter.sharedMesh = mesh;
                    }
                }
            }
            
            // Load material from GUID
            if (!string.IsNullOrEmpty(saveData.materialGUID))
            {
                string materialPath = UnityEditor.AssetDatabase.GUIDToAssetPath(saveData.materialGUID);
                if (!string.IsNullOrEmpty(materialPath))
                {
                    Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material != null)
                    {
                        meshRenderer.sharedMaterial = material;
                    }
                }
            }
            
            // Update collider to match mesh if mesh was loaded
            if (meshFilter.sharedMesh != null)
            {
                UpdatePartCollider(partObj, meshFilter.sharedMesh);
            }
#endif
        }
        
        // Restore WeldingSystem for barrels if needed
        if (saveData.partType == PartType.Barrel)
        {
            WeldingSystem welding = partObj.GetComponent<WeldingSystem>();
            if (welding == null)
            {
                welding = partObj.AddComponent<WeldingSystem>();
            }
            
            // Restore welding state using reflection
            var weldingProgressField = typeof(WeldingSystem).GetField("weldingProgress", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isWeldedField = typeof(WeldingSystem).GetField("isWelded", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (weldingProgressField != null) weldingProgressField.SetValue(welding, saveData.weldingProgress);
            if (isWeldedField != null) isWeldedField.SetValue(welding, saveData.isWelded);
        }
        
        // Restore lens overlay for scopes if needed
        // Only restore lens if it was actually present in the original scope (lensOverlayName is not empty)
        if (saveData.partType == PartType.Scope && !string.IsNullOrEmpty(saveData.lensOverlayName))
        {
            GameObject lensPrefab = null;
            
#if UNITY_EDITOR
            // First, try to load lens overlay prefab from GUID
            if (!string.IsNullOrEmpty(saveData.lensOverlayPrefabGUID))
            {
                string lensPath = UnityEditor.AssetDatabase.GUIDToAssetPath(saveData.lensOverlayPrefabGUID);
                if (!string.IsNullOrEmpty(lensPath))
                {
                    lensPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(lensPath);
                }
            }
            
            // Fallback: try to find lens prefab through ShopPartConfig
            if (lensPrefab == null)
            {
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
                                        // Check if this lens prefab matches the saved name
                                        string prefabName = meshData.lensOverlayPrefab.name.ToLower();
                                        string savedName = saveData.lensOverlayName.ToLower().Replace("(clone)", "").Trim();
                                        
                                        if (prefabName.Contains(savedName) || savedName.Contains(prefabName))
                                        {
                                            lensPrefab = meshData.lensOverlayPrefab;
                                            break;
                                        }
                                    }
                                }
                                if (lensPrefab != null) break;
                            }
                        }
                    }
                }
            }
#else
            // Runtime: try to find lens prefab through ShopPartConfig
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
                                    // Check if this lens prefab matches the saved name
                                    string prefabName = meshData.lensOverlayPrefab.name.ToLower();
                                    string savedName = saveData.lensOverlayName.ToLower().Replace("(clone)", "").Trim();
                                    
                                    if (prefabName.Contains(savedName) || savedName.Contains(prefabName))
                                    {
                                        lensPrefab = meshData.lensOverlayPrefab;
                                        break;
                                    }
                                }
                            }
                            if (lensPrefab != null) break;
                        }
                    }
                }
            }
#endif
            
            // Instantiate lens overlay if found
            if (lensPrefab != null)
            {
                GameObject lensOverlay = Instantiate(lensPrefab, partObj.transform);
                
                // Disable physics on lens overlay to prevent it from affecting weapon physics
                // Lens overlay should be purely visual and not interact with physics
                Rigidbody[] lensRigidbodies = lensOverlay.GetComponentsInChildren<Rigidbody>(true);
                foreach (Rigidbody rb in lensRigidbodies)
                {
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }
                }
                
                Collider[] lensColliders = lensOverlay.GetComponentsInChildren<Collider>(true);
                foreach (Collider collider in lensColliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }
                }
                
                // Lens overlay prefab should have its local position/rotation pre-configured
                Debug.Log($"SaveSystemManager: Restored lens overlay for scope: {saveData.partName}");
            }
            else if (!string.IsNullOrEmpty(saveData.lensOverlayPrefabGUID))
            {
                Debug.LogWarning($"SaveSystemManager: Could not restore lens overlay for scope: {saveData.partName}. GUID: {saveData.lensOverlayPrefabGUID}, Name: {saveData.lensOverlayName}");
            }
        }
        
        return weaponPart;
    }
    
    /// <summary>
    /// Update collider to match mesh geometry (similar to PartSpawner.UpdateCollider)
    /// </summary>
    private void UpdatePartCollider(GameObject obj, Mesh mesh)
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
    }
    
    /// <summary>
    /// Public method to trigger save (can be called from other systems)
    /// </summary>
    public static void Save()
    {
        if (Instance != null)
        {
            Instance.SaveGameData(showUI: false);
        }
    }
    
    /// <summary>
    /// Trigger auto-save with UI indicator (called when returning from testing range)
    /// </summary>
    public void TriggerAutoSaveOnReturn()
    {
        // Reset timer to prevent immediate save after this one
        ResetAutoSaveTimer();
        
        // Trigger save with UI
        TriggerAutoSave();
    }
}

