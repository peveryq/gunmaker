#if GameDistributionPlatform_yg && InterstitialAdv_yg
namespace YG
{
    using System;
    using System.Runtime.InteropServices;
    using YG.Insides;
    using AOT;
    using UnityEngine;

    public partial class PlatformYG2 : IPlatformsYG2
    {
        [DllImport("__Internal")]
        private static extern void GameDistribution_ShowInterstitialAd(
            Action openCallback,
            Action closeCallback,
            Action errorCallback
        );

        [MonoPInvokeCallback(typeof(Action))]
        private static void OnInterstitialOpen()
        {
            YGInsides.OpenInterAdv();
        }

        [MonoPInvokeCallback(typeof(Action))]
        private static void OnInterstitialClose()
        {
            YGInsides.CloseInterAdv();
        }

        [MonoPInvokeCallback(typeof(Action))]
        private static void OnInterstitialError()
        {
            YGInsides.CloseInterAdv();
            YGInsides.ErrorInterAdv();
        }

        public void InterstitialAdvShow()
        {
            GameDistribution_ShowInterstitialAd(
                OnInterstitialOpen,
                OnInterstitialClose,
                OnInterstitialError
            );
        }
    }
}
#endif