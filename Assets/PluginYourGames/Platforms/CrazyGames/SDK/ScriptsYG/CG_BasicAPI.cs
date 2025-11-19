#if CrazyGamesPlatform_yg
using CrazyGames;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        public PortalUser User = null;

        public void InitAwake()
        {
            if (CrazySDK.IsAvailable)
            {
                CrazySDK.Init(() =>
                {
                    if (CrazySDK.User.IsUserAccountAvailable)
                    {
                        CrazySDK.User.GetUser(user =>
                        {
                            if (user != null)
                            {
                                User = user;
#if Authorization_yg
                                YG2.player.auth = true;
                                YG2.player.name = user.username;
                                YG2.player.photo = user.profilePictureUrl;
#endif
                                YG2.SyncInitialization();
                            }
                            else
                            {
                                YG2.SyncInitialization();
                            }
                        });
                    }
                    else
                    {
                        YG2.SyncInitialization();
                    }
                });
            }
            else
            {
                YG2.SyncInitialization();
            }
        }

        public void InitStart() { }
        public void InitComplete() { }
        public void GameplayStart() => CrazySDK.Game.GameplayStart();
        public void GameplayStop() => CrazySDK.Game.GameplayStop();
        public void HappyTime() => CrazySDK.Game.HappyTime();
    }
}
#endif