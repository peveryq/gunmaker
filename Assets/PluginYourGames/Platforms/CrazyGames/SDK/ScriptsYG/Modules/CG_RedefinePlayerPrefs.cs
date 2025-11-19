#if CrazyGamesPlatform_yg && PlayerStats_yg
using CrazyGames;
using UnityEngine;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        public void SetString(string key, string value)
        {
            if (YG2.player.auth)
                CrazySDK.Data.SetString(key, value);
            else
                PlayerPrefs.SetString(key, value);
        }

        public string GetString(string key)
        {
            if (YG2.player.auth)
                return CrazySDK.Data.GetString(key);
            else
                return PlayerPrefs.GetString(key);
        }

        public string GetString(string key, string defaultValue)
        {
            if (YG2.player.auth)
                return CrazySDK.Data.GetString(key, defaultValue);
            else
                return PlayerPrefs.GetString(key, defaultValue);
        }

        public void SetFloat(string key, float value)
        {
            if (YG2.player.auth)
                CrazySDK.Data.SetFloat(key, value);
            else
                PlayerPrefs.SetFloat(key, value);
        }
         
        public float GetFloat(string key)
        {
            if (YG2.player.auth)
                return CrazySDK.Data.GetFloat(key);
            else
                return PlayerPrefs.GetFloat(key);
        }
         
        public float GetFloat(string key, float defaultValue)
        {
            if (YG2.player.auth)
                return CrazySDK.Data.GetFloat(key, defaultValue);
            else
                return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public void SetInt(string key, int value)
        {
            if (YG2.player.auth)
                CrazySDK.Data.SetInt(key, value);
            else
                PlayerPrefs.SetInt(key, value);
        }
         
        public int GetInt(string key)
        {
            if (YG2.player.auth)
                return CrazySDK.Data.GetInt(key);
            else
                return PlayerPrefs.GetInt(key);
        }
         
        public int GetInt(string key, int defaultValue)
        {
            if (YG2.player.auth)
                return CrazySDK.Data.GetInt(key, defaultValue);
            else
                return PlayerPrefs.GetInt(key, defaultValue);
        }

        public bool HasKey(string key)
        {
            if (YG2.player.auth)
                return CrazySDK.Data.HasKey(key);
            else
                return PlayerPrefs.HasKey(key);
        }
         
        public void DeleteKey(string key)
        {
            if (YG2.player.auth)
                CrazySDK.Data.DeleteKey(key);
            else
                PlayerPrefs.DeleteKey(key);
        }
         
        public void DeleteAll()
        {
            if (YG2.player.auth)
                CrazySDK.Data.DeleteAll();
            else
                PlayerPrefs.DeleteAll();
        }
    }
}
#endif