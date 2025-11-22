using System.Collections.Generic;
using YG;

/// <summary>
/// Extends SavesYG with game-specific save data
/// </summary>
namespace YG
{
    public partial class SavesYG
    {
        // Player money
        public int playerMoney = 10000;
        
        // Weapons saved in slots (slot-based, null entries indicate empty slots)
        public List<WeaponSaveData> savedWeapons = new List<WeaponSaveData>();
        
        // Weapon currently on workbench (null if no weapon)
        public WorkbenchSaveData workbenchWeapon = null;
        
        // Game settings (serialized as JSON string)
        public string gameSettings = "";
    }
}

