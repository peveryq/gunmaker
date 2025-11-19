#if CrazyGamesPlatform_yg && Authorization_yg
using UnityEngine;
using CrazyGames;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        public void InitAuth()
        { 
            // Initialized in script CG_BasicAPI
        }

        public void GetAuth()
        {
            if (CrazySDK.IsAvailable && CrazySDK.User.IsUserAccountAvailable)
            {
                CrazySDK.User.GetUser(user =>
                {
                    User = user;

                    if (user != null)
                    {
                        YG2.player.auth = true;
                        YG2.player.name = user.username;
                        YG2.player.photo = user.profilePictureUrl;
                    }
                    else
                    {
                        NotAuthorized();
                    }
                });
            }
            else
            {
                NotAuthorized();
            }
            YG2.GetDataInvoke();
        }

        public void OpenAuthDialog()
        {
            if (!CrazySDK.IsAvailable) return;

            CrazySDK.User.ShowAuthPrompt((error, user) =>
            {
                if (error != null)
                {
                    Debug.LogError("Show auth prompt error: " + error);
                    return;
                }

                User = user;

                if (user != null)
                {
                    YG2.player.auth = true;
                    YG2.player.name = user.username;
                    YG2.player.photo = user.profilePictureUrl;
                }
                else
                {
                    NotAuthorized();
                }

                YG2.GetDataInvoke();
            });
        }

        private void NotAuthorized()
        {
            YG2.player.auth = false;
            YG2.player.name = "unauthorized";
            YG2.player.photo = string.Empty;
        }
    }
}
#endif