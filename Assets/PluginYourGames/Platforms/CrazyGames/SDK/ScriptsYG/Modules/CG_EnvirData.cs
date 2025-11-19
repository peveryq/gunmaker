#if CrazyGamesPlatform_yg && EnvirData_yg
using CrazyGames;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        public void InitEnirData()
        {
            if (CrazySDK.IsAvailable)
            {
                var systemInfo = CrazySDK.User.SystemInfo;

                YG2.envir.language = systemInfo.countryCode.ToLower();
                YG2.envir.browser = systemInfo.browser.name;
                YG2.envir.platform = systemInfo.os.name;

                string device = systemInfo.device.type;
                YG2.envir.deviceType = device;

                switch (device)
                {
                    case "desktop":
                        YG2.envir.isDesktop = true;
                        YG2.envir.isTablet = false;
                        YG2.envir.isMobile = false;
                        YG2.envir.isTV = false;
                        break;
                    case "tablet":
                        YG2.envir.isDesktop = false;
                        YG2.envir.isTablet = true;
                        YG2.envir.isMobile = false;
                        YG2.envir.isTV = false;
                        break;
                    case "mobile":
                        YG2.envir.isDesktop = false;
                        YG2.envir.isTablet = false;
                        YG2.envir.isMobile = true;
                        YG2.envir.isTV = false;
                        break;
                }
            }
        }

        public void GetEnvirData()
        {
            InitEnirData();
            YG2.GetDataInvoke();
        }
    }
}
#endif
