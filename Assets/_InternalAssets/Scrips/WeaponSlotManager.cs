using System;
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

