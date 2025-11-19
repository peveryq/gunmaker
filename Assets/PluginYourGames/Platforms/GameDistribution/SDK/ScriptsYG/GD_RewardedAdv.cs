#if GameDistributionPlatform_yg && RewardedAdv_yg
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
        private static extern void GameDistribution_ShowRewardedAd(
            IntPtr idPtr,
            Action openCallback,
            Action rewardedCallback,
            Action closeCallback,
            Action errorCallback
        );

        private static IntPtr currentRewardIdPtr = IntPtr.Zero;

        [MonoPInvokeCallback(typeof(Action))]
        private static void OnRewardedOpen()
        {
            YGInsides.OpenRewardedAdv();
        }

        [MonoPInvokeCallback(typeof(Action))]
        private static void OnRewarded()
        {
            if (currentRewardIdPtr != IntPtr.Zero)
            {
                string id = Marshal.PtrToStringUTF8(currentRewardIdPtr);
                YGInsides.RewardAdv(id);
            }
            else
            {
                Debug.LogError("[GD_Rewarded] OnRewarded called but currentRewardIdPtr is null!");
            }
        }

        [MonoPInvokeCallback(typeof(Action))]
        private static void OnRewardedClose()
        {
            if (currentRewardIdPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(currentRewardIdPtr);
                currentRewardIdPtr = IntPtr.Zero;
            }
            YGInsides.CloseRewardedAdv();
        }

        [MonoPInvokeCallback(typeof(Action))]
        private static void OnRewardedError()
        {
            if (currentRewardIdPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(currentRewardIdPtr);
                currentRewardIdPtr = IntPtr.Zero;
            }
            YGInsides.ErrorRewardedAdv();
            YGInsides.CloseRewardedAdv();
        }

        public void RewardedAdvShow(string id)
        {
            if (currentRewardIdPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(currentRewardIdPtr);
            }

            currentRewardIdPtr = Marshal.StringToHGlobalAnsi(id);

            GameDistribution_ShowRewardedAd(
                currentRewardIdPtr,
                OnRewardedOpen,
                OnRewarded,
                OnRewardedClose,
                OnRewardedError
            );
        }
    }
}
#endif