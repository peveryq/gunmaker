#if GameDistributionPlatform_yg
using System.Runtime.InteropServices;
using UnityEngine;
namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        [DllImport("__Internal")]
        private static extern bool GameDistribution_CheckInit();

        
        public void InitAwake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (GameDistribution_CheckInit())
            {
                YG2.SyncInitialization();
            }
            Debug.Log("GameDistribution_CheckInit false");
#else
            YG2.SyncInitialization();
#endif
        }
    }
}

namespace YG.Insides
{
    public partial class YGSendMessage
    {
        public void GDSDKReady()
        {
            YG2.SyncInitialization();
        }
    }
}
#endif
