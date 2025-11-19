using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponSlotState
{
    Hidden,
    Available,
    Occupied
}

public class WeaponSlotManager : MonoBehaviour
{
    private static WeaponSlotManager instance;
    private static bool applicationIsQuitting;

    [SerializeField] private List<WeaponRecord> slotRecords = new List<WeaponRecord>();

    public static WeaponSlotManager Instance
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

    public event Action SlotsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (applicationIsQuitting || instance != null) return;

        GameObject host = new GameObject("WeaponSlotManager");
        instance = host.AddComponent<WeaponSlotManager>();
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

        InitializeSlots();
    }

    public int Capacity => slotRecords.Count;

    public int OccupiedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slotRecords.Count; i++)
            {
                if (slotRecords[i] != null)
                {
                    count++;
                }
            }
            return count;
        }
    }

    public WeaponSlotState GetSlotState(int slotIndex)
    {
        if (!IsValidIndex(slotIndex)) return WeaponSlotState.Hidden;

        int occupied = OccupiedCount;

        if (slotIndex < occupied)
        {
            return WeaponSlotState.Occupied;
        }

        if (slotIndex == occupied && slotIndex < Capacity)
        {
            return WeaponSlotState.Available;
        }

        return WeaponSlotState.Hidden;
    }

    public IReadOnlyList<WeaponRecord> GetSlotRecords()
    {
        return slotRecords;
    }
    
    /// <summary>
    /// Get all weapon records (for iteration/searching)
    /// </summary>
    public IEnumerable<WeaponRecord> GetAllRecords()
    {
        foreach (var record in slotRecords)
        {
            if (record != null)
            {
                yield return record;
            }
        }
    }

    public WeaponRecord GetRecord(int slotIndex)
    {
        return IsValidIndex(slotIndex) ? slotRecords[slotIndex] : null;
    }

    public bool TryGetFirstAvailableSlot(out int slotIndex)
    {
        slotIndex = OccupiedCount;
        if (slotIndex < Capacity)
        {
            return true;
        }

        slotIndex = -1;
        return false;
    }

    public bool TryAssignSlot(int slotIndex, WeaponRecord record)
    {
        if (!IsValidIndex(slotIndex) || record == null)
        {
            return false;
        }

        int occupied = OccupiedCount;

        if (slotIndex != occupied)
        {
            Debug.LogWarning($"WeaponSlotManager: Attempted to assign slot {slotIndex}, but next available slot is {occupied}.");
            return false;
        }

        slotRecords[slotIndex] = record;
        RaiseSlotsChanged();
        return true;
    }

    public bool TryAssignNextAvailableSlot(WeaponRecord record, out int assignedIndex)
    {
        assignedIndex = -1;
        if (record == null) return false;

        if (TryGetFirstAvailableSlot(out int slotIndex))
        {
            slotRecords[slotIndex] = record;
            assignedIndex = slotIndex;
            RaiseSlotsChanged();
            return true;
        }

        return false;
    }

    public bool ClearSlot(int slotIndex)
    {
        if (!IsValidIndex(slotIndex)) return false;

        if (slotRecords[slotIndex] == null) return false;

        slotRecords[slotIndex] = null;
        CompactSlots();
        RaiseSlotsChanged();
        return true;
    }

    public int IndexOf(WeaponBody weaponBody)
    {
        if (weaponBody == null) return -1;

        for (int i = 0; i < slotRecords.Count; i++)
        {
            if (slotRecords[i]?.WeaponBody == weaponBody)
            {
                return i;
            }
        }

        return -1;
    }

    public void ForceRebuild()
    {
        InitializeSlots();
        RaiseSlotsChanged();
    }

    private void InitializeSlots()
    {
        slotRecords.Clear();

        int capacity = GameBalanceConfig.Instance != null
            ? Mathf.Max(0, GameBalanceConfig.Instance.WeaponSlotCapacity)
            : 0;

        for (int i = 0; i < capacity; i++)
        {
            slotRecords.Add(null);
        }
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < slotRecords.Count;
    }

    private void CompactSlots()
    {
        int writeIndex = 0;
        for (int readIndex = 0; readIndex < slotRecords.Count; readIndex++)
        {
            WeaponRecord record = slotRecords[readIndex];
            if (record != null)
            {
                if (writeIndex != readIndex)
                {
                    slotRecords[writeIndex] = record;
                    slotRecords[readIndex] = null;
                }
                writeIndex++;
            }
        }

        for (int i = writeIndex; i < slotRecords.Count; i++)
        {
            slotRecords[i] = null;
        }
    }

    private void RaiseSlotsChanged()
    {
        SlotsChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
    
    /// <summary>
    /// Save all weapon records to save data list
    /// If a weapon is currently on the workbench, use its current state instead of the stored record
    /// </summary>
    public List<WeaponSaveData> GetSaveData()
    {
        List<WeaponSaveData> saveDataList = new List<WeaponSaveData>();
        
        // Get workbench to check if any weapon from slots is currently mounted
        Workbench workbench = FindFirstObjectByType<Workbench>();
        WeaponBody mountedWeapon = workbench != null ? workbench.MountedWeapon : null;
        
        foreach (var record in slotRecords)
        {
            if (record != null && record.WeaponBody != null)
            {
                WeaponBody weaponBody = record.WeaponBody;
                
                // Check if this weapon is currently on the workbench
                // If yes, use the workbench weapon's state (it's the same instance but with updated parts)
                // Compare by weapon name to identify the same weapon
                bool isOnWorkbench = mountedWeapon != null && 
                                     mountedWeapon == weaponBody;
                
                if (isOnWorkbench)
                {
                    // Use workbench weapon's current state (has all latest modifications)
                    saveDataList.Add(mountedWeapon.GetSaveData());
                }
                else
                {
                    // Use stored record's weapon state
                    saveDataList.Add(weaponBody.GetSaveData());
                }
            }
            else
            {
                // Null entry indicates empty slot
                saveDataList.Add(null);
            }
        }
        
        return saveDataList;
    }
    
    /// <summary>
    /// Load weapon records from save data list
    /// Note: This method is called by SaveSystemManager, not directly
    /// </summary>
    internal void LoadFromSaveData(List<WeaponSaveData> saveDataList, System.Func<WeaponSaveData, WeaponBody> restoreWeaponFunc)
    {
        if (saveDataList == null || restoreWeaponFunc == null) return;
        
        // Clear existing slots
        ForceRebuild();
        
        // Load weapons into slots
        for (int i = 0; i < saveDataList.Count && i < slotRecords.Count; i++)
        {
            var weaponSaveData = saveDataList[i];
            if (weaponSaveData != null)
            {
                // Restore weapon from save data
                WeaponBody weaponBody = restoreWeaponFunc(weaponSaveData);
                if (weaponBody != null)
                {
                    // Prepare weapon for storage (cache physics state, disable rigidbodies/colliders)
                    // This ensures the weapon is in the correct state for storage and can be properly restored when taken
                    WeaponLockerSystem lockerSystem = WeaponLockerSystem.Instance;
                    if (lockerSystem != null)
                    {
                        // Use reflection to call the private PrepareWeaponForStorage method
                        var prepareMethod = typeof(WeaponLockerSystem).GetMethod("PrepareWeaponForStorage", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (prepareMethod != null)
                        {
                            prepareMethod.Invoke(lockerSystem, new object[] { weaponBody });
                        }
                        else
                        {
                            // Fallback: manually prepare weapon
                            weaponBody.gameObject.SetActive(false);
                            
                            // Disable rigidbodies and colliders manually
                            Rigidbody[] rigidbodies = weaponBody.GetComponentsInChildren<Rigidbody>(true);
                            foreach (Rigidbody rb in rigidbodies)
                            {
                                if (rb != null)
                                {
                                    rb.isKinematic = true;
                                    rb.useGravity = false;
                                }
                            }
                            
                            Collider[] colliders = weaponBody.GetComponentsInChildren<Collider>(true);
                            foreach (Collider collider in colliders)
                            {
                                if (collider != null)
                                {
                                    collider.enabled = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback if WeaponLockerSystem is not available
                        weaponBody.gameObject.SetActive(false);
                    }
                    
                    // Create record and assign to slot
                    WeaponRecord record = new WeaponRecord(
                        weaponSaveData.weaponName,
                        weaponBody,
                        weaponBody.Settings,
                        weaponSaveData.statsSnapshot != null ? weaponSaveData.statsSnapshot.Clone() : null
                    );
                    
                    // Directly set the record in the slot (for loading, bypass validation)
                    slotRecords[i] = record;
                }
            }
        }
        
        // Notify slots changed (auto-save is handled centrally by SaveSystemManager, not here)
        RaiseSlotsChanged();
    }
}

[Serializable]
public class WeaponRecord
{
    [SerializeField] private string weaponName;
    
    public WeaponRecord(string name, WeaponBody weaponBody, WeaponSettings weaponSettings, WeaponStats statsSnapshot = null)
    {
        weaponName = name;
        WeaponBody = weaponBody;
        WeaponSettings = weaponSettings;
        StatsSnapshot = statsSnapshot;
    }
    
    public string WeaponName
    {
        get => weaponName;
        set => weaponName = value;
    }
    
    public WeaponBody WeaponBody { get; set; }
    public WeaponSettings WeaponSettings { get; set; }
    public WeaponStats StatsSnapshot { get; set; }
}

