using UnityEngine;

[CreateAssetMenu(menuName = "Gunmaker/Game Balance Config", fileName = "GameBalanceConfig")]
public class GameBalanceConfig : ScriptableObject
{
    private static GameBalanceConfig instance;

    [Header("Weapon Slots")]
    [SerializeField] private int weaponSlotCapacity = 5;

    public static GameBalanceConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GameBalanceConfig>("GameBalanceConfig");
                if (instance == null)
                {
                    Debug.LogError("GameBalanceConfig asset not found in Resources. Please create one via Assets/_InternalAssets/Resources/GameBalanceConfig.asset");
                }
            }

            return instance;
        }
    }

    public int WeaponSlotCapacity => Mathf.Max(0, weaponSlotCapacity);
}

