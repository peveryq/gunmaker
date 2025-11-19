using UnityEngine;
using UnityEditor;
using YG;
using System.IO;

/// <summary>
/// Editor tool to clear PlayerPrefs and YG2 saves for testing save system
/// </summary>
public class ClearPlayerPrefsTool
{
    [MenuItem("Tools/Gunmaker/Clear PlayerPrefs")]
    public static void ClearPlayerPrefs()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Clear PlayerPrefs",
            "Are you sure you want to clear all PlayerPrefs? This will delete all local save data.\n\nNote: This only clears local saves. Cloud saves will remain intact.",
            "Yes, Clear All",
            "Cancel"
        );
        
        if (confirmed)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            
            EditorUtility.DisplayDialog(
                "PlayerPrefs Cleared",
                "All PlayerPrefs have been cleared successfully.\n\nRestart the game to see the effect.",
                "OK"
            );
            
            Debug.Log("ClearPlayerPrefsTool: All PlayerPrefs cleared.");
        }
    }
    
    [MenuItem("Tools/Gunmaker/Clear YG2 Saves")]
    public static void ClearYG2Saves()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Clear YG2 Saves",
            "Are you sure you want to clear all YG2 save data? This will reset the game to default state.\n\nNote: This only clears YG2 saves. PlayerPrefs will remain intact.",
            "Yes, Clear All",
            "Cancel"
        );

        if (confirmed)
        {
            // Clear YG2 saves
            if (YG2.saves != null)
            {
                // Reset all custom save fields to defaults
                YG2.saves.playerMoney = 10000;
                YG2.saves.savedWeapons = new System.Collections.Generic.List<WeaponSaveData>();
                YG2.saves.workbenchWeapon = null;
                
                // Set default saves (resets idSave but keeps other defaults)
                YG2.SetDefaultSaves();
                
                // Delete SavesEditorYG2.json file if it exists
                string saveFilePath = Path.Combine(Application.dataPath, "PluginYourGames", "Editor", "SavesEditorYG2.json");
                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                    Debug.Log($"ClearPlayerPrefsTool: Deleted save file: {saveFilePath}");
                }
                
                EditorUtility.DisplayDialog(
                    "YG2 Saves Cleared",
                    "All YG2 save data have been cleared and reset to defaults.\n\nRestart the game to see the effect.",
                    "OK"
                );
                
                Debug.Log("ClearPlayerPrefsTool: YG2 save data cleared and reset to defaults.");
            }
            else
            {
                // Still try to delete save file even if YG2.saves is null
                string saveFilePath = Path.Combine(Application.dataPath, "PluginYourGames", "Editor", "SavesEditorYG2.json");
                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                    Debug.Log($"ClearPlayerPrefsTool: Deleted save file: {saveFilePath}");
                }
                
                EditorUtility.DisplayDialog(
                    "Warning",
                    "YG2.saves is null, but save file deleted.\n\nTry restarting Unity Editor to fully clear.",
                    "OK"
                );
                Debug.LogWarning("ClearPlayerPrefsTool: YG2.saves is null. Save file deleted.");
            }
        }
    }
    
    [MenuItem("Tools/Gunmaker/Clear All Saves")]
    public static void ClearAllSaves()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Clear All Saves",
            "Are you sure you want to clear ALL save data (PlayerPrefs + YG2 saves)? This will reset the game to default state.\n\nThis action cannot be undone.",
            "Yes, Clear All",
            "Cancel"
        );

        if (confirmed)
        {
            // Clear PlayerPrefs
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            
            // Delete SavesEditorYG2.json file if it exists
            string saveFilePath = Path.Combine(Application.dataPath, "PluginYourGames", "Editor", "SavesEditorYG2.json");
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log($"ClearPlayerPrefsTool: Deleted save file: {saveFilePath}");
            }
            
            // Clear YG2 saves
            if (YG2.saves != null)
            {
                // Reset all custom save fields to defaults
                YG2.saves.playerMoney = 10000;
                YG2.saves.savedWeapons = new System.Collections.Generic.List<WeaponSaveData>();
                YG2.saves.workbenchWeapon = null;
                
                // Set default saves (resets idSave but keeps other defaults)
                YG2.SetDefaultSaves();
                
                EditorUtility.DisplayDialog(
                    "All Saves Cleared",
                    "All save data have been cleared (PlayerPrefs + YG2 saves + save file).\n\nRestart the game to see the effect.",
                    "OK"
                );
                
                Debug.Log("ClearPlayerPrefsTool: All save data cleared (PlayerPrefs + YG2 saves + save file).");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "All Saves Cleared",
                    "PlayerPrefs and save file cleared. YG2.saves is null (will be initialized on next play).",
                    "OK"
                );
                Debug.Log("ClearPlayerPrefsTool: PlayerPrefs and save file cleared. YG2.saves is null (will be initialized on next play).");
            }
        }
    }
}

