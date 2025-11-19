#if CrazyGamesPlatform_yg && Storage_yg
using UnityEngine;
using YG.Insides;
#if NJSON_STORAGE_YG2
using Newtonsoft.Json;
#endif
using CrazyGames;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        private const string KEY_SAVE = "CG_SavesYG";

        public void LoadCloud()
        {
            string strSaves = CrazySDK.Data.GetString(KEY_SAVE, null);
            YGInsides.SetLoadSaves(strSaves);
        }

        public void SaveCloud()
        {
#if NJSON_STORAGE_YG2
            CrazySDK.Data.SetString(KEY_SAVE, JsonConvert.SerializeObject(YG2.saves));
#else
            CrazySDK.Data.SetString(KEY_SAVE, JsonUtility.ToJson(YG2.saves));
#endif
        }
    }
}
#endif