#if CrazyGamesPlatform_yg && RewardedAdv_yg
using CrazyGames;
using YG.Insides;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        public void RewardedAdvShow(string id)
        {
            if (!CrazySDK.IsAvailable) return;

            CrazySDK.Ad.RequestAd(CrazyAdType.Rewarded, () =>
            {
                YGInsides.OpenRewardedAdv();
            }, (error) =>
            {
                YGInsides.ErrorRewardedAdv();
                YGInsides.CloseRewardedAdv();
            }, () =>
            {
                YGInsides.RewardAdv(id);
                YGInsides.CloseRewardedAdv();
            });
        }
    }
}
#endif