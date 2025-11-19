#if CrazyGamesPlatform_yg && Localization_yg
using CrazyGames;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        public string GetLanguage()
        {
            if (CrazySDK.IsAvailable)
            {
                return CrazySDK.User.SystemInfo.countryCode.ToLower();
            }
            return "en";
        }
    }
}
#endif