#if CrazyGamesPlatform_yg && InterstitialAdv_yg
using CrazyGames;
using YG.Insides;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        public void InterstitialAdvShow()
        {
            if (!CrazySDK.IsAvailable) return;

            CrazySDK.Ad.RequestAd(CrazyAdType.Midgame, () =>
            {
                YGInsides.OpenInterAdv();
            }, (error) =>
            {
                YGInsides.CloseInterAdv();
                YGInsides.ErrorInterAdv();
            }, () =>
            {
                YGInsides.CloseInterAdv();
            });
        }

        public void FirstInterAdvShow()
        {
            OptionalPlatform.FirstInterAdvShow_RealizationSkip();
        }

        public void OtherInterAdvShow() { }
    }
}
#endif