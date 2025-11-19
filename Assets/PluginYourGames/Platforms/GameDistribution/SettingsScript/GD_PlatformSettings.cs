#if GameDistributionPlatform_yg && UNITY_EDITOR
using UnityEngine;
namespace YG.Insides
{
        public partial class PlatformInfo
        {
            [Platform("GameDistribution")]
            public string gameDistributionId = "";
        }
}
#endif