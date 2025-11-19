#if PLATFORM_WEBGL
namespace YG
{
    using System.Runtime.InteropServices;

    public partial class YG2
    {
        [DllImport("__Internal")]
        private static extern void SetPixelRatioForMobile_js(float ratio);

        [InitYG]
        private static void RedefinePixelRatio()
        {
#if !UNITY_EDITOR
            if (infoYG.Templates.pixelRatioEnable)
                SetPixelRatioForMobile_js(infoYG.Templates.pixelRatioValue);
            else
                SetPixelRatioForMobile_js(2.0f);
#endif
        }
    }
}
#endif