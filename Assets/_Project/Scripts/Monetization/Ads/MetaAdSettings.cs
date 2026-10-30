using System.Runtime.InteropServices;
using UnityEngine;

namespace AdsMediation
{
    /// <summary>
    /// Meta (Facebook) Audience Network settings wrapper.
    ///
    /// AppLovin MAX ka Meta adapter sirf NATIVE FAN SDK laata hai - uska C# API
    /// nahi aata. Audience Network Unity plugin import karna conflict paida karta
    /// hai (duplicate native SDK), is liye AppLovin khud kehta hai ke native
    /// methods ke liye apna wrapper likho. Ye wohi wrapper hai.
    ///
    /// Android : com.facebook.ads.AdSettings
    /// iOS     : FBAdSettings   (MetaAdSettingsBridge.mm ke zariye)
    /// </summary>
    public static class MetaAdSettings
    {
        // Country codes
        public const int CountryAutoDetect = 0;
        public const int CountryUnitedStates = 1;

        // State codes (0 = Meta khud geolocate kare)
        public const int StateAutoDetect = 0;
        public const int StateCalifornia = 1000;
        public const int StateColorado = 1001;
        public const int StateConnecticut = 1002;
        public const int StateFlorida = 1003;
        public const int StateOregon = 1004;
        public const int StateTexas = 1005;
        public const int StateMontana = 1006;
        public const int StateDelaware = 1007;
        public const int StateNebraska = 1008;
        public const int StateNewHampshire = 1009;
        public const int StateNewJersey = 1010;
        public const int StateMinnesota = 1011;
        public const int StateMaryland = 1012;
        public const int StateRhodeIsland = 1013;

        private static readonly string[] EmptyOptions = new string[0];
        private static readonly string[] LduOptions = new string[] { "LDU" };

#if UNITY_ANDROID && !UNITY_EDITOR
        private const string AndroidAdSettingsClass = "com.facebook.ads.AdSettings";

        // Cached AndroidJavaClass - har call pe naya JNI lookup mehnga hai
        private static AndroidJavaClass _androidAdSettings;

        private static AndroidJavaClass AndroidAdSettings
        {
            get
            {
                if (_androidAdSettings == null)
                {
                    try
                    {
                        _androidAdSettings = new AndroidJavaClass(AndroidAdSettingsClass);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("[MetaAdSettings] FAN SDK class nahi mili: " + e.Message);
                    }
                }
                return _androidAdSettings;
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _MetaSetDataProcessingOptions(string optionsCsv, int country, int state);

        [DllImport("__Internal")]
        private static extern void _MetaSetAdvertiserTrackingEnabled(bool enabled);
#endif

        /// <summary>LDU band - Meta full data use karega.</summary>
        public static void DisableLimitedDataUse()
        {
            SetDataProcessingOptions(EmptyOptions, CountryAutoDetect, StateAutoDetect);
        }

        /// <summary>
        /// LDU chalu, Meta khud geolocate karega. Recommended default:
        /// LDU sirf un US states pe apply hoga jahan legally zaroori hai,
        /// baqi duniya pe full monetization intact rehti hai.
        /// </summary>
        public static void EnableLimitedDataUseAutoGeo()
        {
            SetDataProcessingOptions(LduOptions, CountryAutoDetect, StateAutoDetect);
        }

        /// <summary>LDU chalu, geography khud specify karo.</summary>
        public static void EnableLimitedDataUse(int country, int state)
        {
            SetDataProcessingOptions(LduOptions, country, state);
        }

        public static void SetDataProcessingOptions(string[] options, int country, int state)
        {
            if (options == null) options = EmptyOptions;

#if UNITY_ANDROID && !UNITY_EDITOR
            var adSettings = AndroidAdSettings;
            if (adSettings == null) return;

            try
            {
                // object[] explicitly - warna params string[] splat ho jata hai
                adSettings.CallStatic("setDataProcessingOptions",
                    new object[] { options, country, state });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[MetaAdSettings] setDataProcessingOptions fail: " + e.Message);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            _MetaSetDataProcessingOptions(string.Join(",", options), country, state);
#else
            Debug.Log(string.Format(
                "[MetaAdSettings] (Editor no-op) DPO=[{0}] country={1} state={2}",
                string.Join(",", options), country, state));
#endif
        }

        /// <summary>
        /// Meta ka ATE flag. Sirf iOS 14.5+ pe matlab rakhta hai - ATT ke
        /// ASAL jawab se set karo, hardcoded true se nahi.
        /// Android pe FAN mein ye concept nahi, is liye no-op hai.
        /// </summary>
        public static void SetAdvertiserTrackingEnabled(bool enabled)
        {
#if UNITY_IOS && !UNITY_EDITOR
            _MetaSetAdvertiserTrackingEnabled(enabled);
#else
            Debug.Log("[MetaAdSettings] (no-op) SetAdvertiserTrackingEnabled=" + enabled);
#endif
        }
    }
}
