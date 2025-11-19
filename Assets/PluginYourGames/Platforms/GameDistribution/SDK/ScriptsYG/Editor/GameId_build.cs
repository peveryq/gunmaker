#if GameDistributionPlatform_yg
using UnityEngine;

namespace YG.EditorScr.BuildModify
{
    public partial class ModifyBuild
    {
        public static void SetGameId()
        {
            if (string.IsNullOrEmpty(infoYG.platformInfo.gameDistributionId))
            {
                Debug.LogError("GameDistribution Game ID is not set! Please set it in YG2 settings.");
                return;
            }

            var placeholder = "\"gameId\": \"YOUR_GD_GAME_ID\"";
            var actualGameId = $"\"gameId\": \"{infoYG.platformInfo.gameDistributionId}\"";
            
            indexFile = indexFile.Replace(placeholder, actualGameId);
            
            Debug.Log($"GameDistribution Game ID set to: {infoYG.platformInfo.gameDistributionId}");
        }
    }
}
#endif