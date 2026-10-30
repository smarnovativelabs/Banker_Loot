using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MaxSdkBase;

namespace AdsMediation
{
    public class MaxAdsManager : MonoBehaviour
    {
        private const string MaxSdkKey = "TRg6tE6Jqd63wDA1Egi746kgkf6hBDmP1x3ddKVkxqed_NUoJtPlTFEBu1FT03HiToJf035dbl7ZG8wrgvuCWc";
        [Header("IOS Max Ads Ids")]
        public string iosInterstitialAdUnitId = "ENTER_IOS_INTERSTITIAL_AD_UNIT_ID_HERE";
        public string iosRewardedAdUnitId = "ENTER_IOS_REWARD_AD_UNIT_ID_HERE";
        public string iosRewardedInterstitialAdUnitId = "ENTER_IOS_REWARD_INTER_AD_UNIT_ID_HERE";
        public string iosBannerAdUnitId = "ENTER_IOS_BANNER_AD_UNIT_ID_HERE";
        public string iosMRecAdUnitId = "ENTER_IOS_MREC_AD_UNIT_ID_HERE";

        [Header("Android Max Ads Ids")]
        public string androidInterstitialAdUnitId = "ENTER_ANDROID_INTERSTITIAL_AD_UNIT_ID_HERE";
        public string androidRewardedAdUnitId = "ENTER_ANDROID_REWARD_AD_UNIT_ID_HERE";
        public string androidRewardedInterstitialAdUnitId = "ENTER_ANDROID_REWARD_INTER_AD_UNIT_ID_HERE";
        public string androidBannerAdUnitId = "ENTER_ANDROID_BANNER_AD_UNIT_ID_HERE";
        public string androidMRecAdUnitId = "ENTER_ANDROID_MREC_AD_UNIT_ID_HERE";


        string InterstitialAdUnitId;
        string RewardedAdUnitId;
        string RewardedInterstitialAdUnitId;
        string BannerAdUnitId;
        string MRecAdUnitId;
        private bool isBannerShowing;
        private bool isMRecShowing;

        private int interstitialRetryAttempt;
        private int rewardedRetryAttempt;
        private int rewardedInterstitialRetryAttempt;

        bool rewardedInterReceived = false;
        bool rvRewardReceived = false;
        public bool isInitialized = false;
        bool initiallyAdsLoaded = false;
        AdsMediationManager.RewardedVieoResponse rvSuccessResponse;
        AdsMediationManager.RewardedVieoResponse rvFailureResponse;

        AdsMediationManager.RewardedVieoResponse rewardedInterSuccess;
        AdsMediationManager.RewardedVieoResponse rewardedInterFailure;


        public void InitializeAds(bool _loadAds = true, bool hasTrackingConsent = true)
        {
            print("Initializing Max Ads -" + isInitialized);
            if (isInitialized)
            {
                if (_loadAds && !initiallyAdsLoaded)
                {
                    InitializeInterstitialAds();
                    InitializeRewardedAds();
                    initiallyAdsLoaded = true;
                }
                return;
            }
            // Enable verbose logging
            //   MaxSdk.SetVerboseLogging(true);
            Debug.Log("Initializing MAx Ads-------------");
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                //TenjinManager.instance.SubscibeApplovinImpressions();
                // AppLovin SDK is initialized, configure and start loading ads.
                Debug.Log("MAX SDK Initialized");
                isInitialized = true;
                //     InitializeBannerAds();

                //   MaxSdk.ShowMediationDebugger();
                if (_loadAds)
                {
                    //GameManager.instance.CallFireBase("MaxAdsInitialized");
                    InitializeInterstitialAds();
                    InitializeRewardedAds();
                    initiallyAdsLoaded = true;
                }

            };

            MaxSdk.SetSdkKey(MaxSdkKey);

            // Meta CCPA / Limited Data Use - auto-geo, only restricts data in states that require it
            MetaAdSettings.EnableLimitedDataUseAutoGeo();

            // Meta ATE flag - bound to the real ATT result resolved upstream by AdsMediationManager
            MetaAdSettings.SetAdvertiserTrackingEnabled(hasTrackingConsent);
            MaxSdk.SetHasUserConsent(hasTrackingConsent);

            MaxSdk.InitializeSdk();

            // StartCoroutine(callSDkDebugger());  
        }

        IEnumerator callSDkDebugger()
        {
            yield return new WaitForSeconds(5f);

            MaxSdk.ShowMediationDebugger();
        }

        private void Awake()
        {
            InterstitialAdUnitId = androidInterstitialAdUnitId;
#if UNITY_IPHONE
            InterstitialAdUnitId = iosInterstitialAdUnitId;
#endif
            RewardedAdUnitId = androidRewardedAdUnitId;
#if UNITY_IPHONE
            RewardedAdUnitId = iosRewardedAdUnitId;
#endif
            BannerAdUnitId = androidBannerAdUnitId;
#if UNITY_IPHONE
            BannerAdUnitId = iosBannerAdUnitId;
#endif
            RewardedInterstitialAdUnitId = androidRewardedInterstitialAdUnitId;
#if UNITY_IPHONE
            RewardedInterstitialAdUnitId = iosRewardedInterstitialAdUnitId;
#endif
        }
        

        #region Interstitial Ad Methods

        private void InitializeInterstitialAds()
        {

            if (string.IsNullOrEmpty(InterstitialAdUnitId) || AdsMediationManager.instance.isAdsRemoved)
            {
                return;

            }
            // Attach callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;

            // Load the first interstitial
            print("Requesting Interstitial");
            //GameManager.instance.CallFireBase("MaxAdsInterReq");
            LoadInterstitial();
        }

        void LoadInterstitial()
        {
            MaxSdk.LoadInterstitial(InterstitialAdUnitId);
        }

        public void ShowInterstitial()
        {
            if (MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
            {
                MaxSdk.ShowInterstitial(InterstitialAdUnitId);
            }
        }
        public bool IsInterstitialLoaded()
        {

            if (!isInitialized)
                return false;

            return MaxSdk.IsInterstitialReady(InterstitialAdUnitId);
        }
        private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
            Debug.Log("Interstitial loaded");
            // Reset retry attempt
            interstitialRetryAttempt = 0;
            //      GameManager.instance.CallFireBase("MaxInterLoded");
        }

        private void OnInterstitialFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
            interstitialRetryAttempt++;
            double retryDelay = Mathf.Pow(2, Mathf.Min(7, interstitialRetryAttempt));

            Debug.Log("Interstitial failed to load with error code: " + errorInfo.Code);

            Invoke("LoadInterstitial", (float)retryDelay);
            //      GameManager.instance.CallFireBase("MaxInterFlLod");

        }

        private void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad failed to display. We recommend loading the next ad
            Debug.Log("Interstitial failed to display with error code: " + errorInfo.Code);
            LoadInterstitial();
        }

        private void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad is hidden. Pre-load the next ad
            AdsMediationManager.instance.OnInterstitialClosed();

            Debug.Log("Interstitial dismissed");
            LoadInterstitial();
            //     GameManager.instance.CallFireBase("MaxAdDisplayed");
        }

        private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad revenue paid. Use this callback to track user revenue.
            Debug.Log("Interstitial revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to
            string format = adInfo.AdFormat;

            Firebase.Analytics.Parameter[] impressionParameters = new Firebase.Analytics.Parameter[] {
            new Firebase.Analytics.Parameter("ad_platform", "AppLovin"),
            new Firebase.Analytics.Parameter("ad_source", networkName),
            new Firebase.Analytics.Parameter("ad_unit_name", adUnitIdentifier),
            new Firebase.Analytics.Parameter("ad_format", format),
            new Firebase.Analytics.Parameter("value", revenue),
            new Firebase.Analytics.Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
            };
            FirebaseManager.instance.CallAdImpressionEvent("ad_impression", impressionParameters);

            ApplovinImpressionData _data = new ApplovinImpressionData();
            _data.creative_id = adInfo.CreativeIdentifier;
            _data.placement = adInfo.Placement;
            _data.format = adInfo.AdFormat;
            _data.country = MaxSdk.GetSdkConfiguration().CountryCode;
            _data.ad_revenue_currency = "USD";
            _data.network_placement = adInfo.NetworkPlacement;
            _data.publisher_revenue_decimal = adInfo.Revenue;
            _data.revenue_precision = adInfo.RevenuePrecision;
            _data.ad_unit_id = adInfo.AdUnitIdentifier;
            _data.publisher_revenue_micro = adInfo.Revenue * 1000000;
            _data.revenue = adInfo.Revenue;
            _data.network_name = adInfo.NetworkName;

            Debug.Log($"Received Impression Data - {adInfo.Revenue} - {adUnitIdentifier} - {networkName} - {adInfo.NetworkPlacement}");
            TenjinManager.instance.ApplovinImpressionJson(LitJson.JsonMapper.ToJson(_data));
            TenjinManager.instance.UpdateIAARevenue(adInfo.Revenue);

        }

        #endregion

        #region Rewarded Ad Methods

        private void InitializeRewardedAds()
        {
            if (string.IsNullOrEmpty(RewardedAdUnitId))
                return;
            // Attach callbacks
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;

            // Load the first RewardedAd
            LoadRewardedAd();
        }

        private void LoadRewardedAd()
        {
            MaxSdk.LoadRewardedAd(RewardedAdUnitId);
        }

        public void ShowRewardedAd(AdsMediationManager.RewardedVieoResponse _success = null, AdsMediationManager.RewardedVieoResponse _failure = null)
        {
            rvSuccessResponse = _success;
            rvFailureResponse = _failure;

            if (MaxSdk.IsRewardedAdReady(RewardedAdUnitId))
            {
                MaxSdk.ShowRewardedAd(RewardedAdUnitId);
            }
            else
            {
                rvFailureResponse?.Invoke("Rewarded Ad Not Loaded!");
            }

        }
        public bool CanShowRewardedVideo()
        {
            if (!isInitialized)
                return false;
            return MaxSdk.IsRewardedAdReady(RewardedAdUnitId);
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
            Debug.Log("Rewarded ad loaded");

            // Reset retry attempt
            rewardedRetryAttempt = 0;
        }

        private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Rewarded ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
            rewardedRetryAttempt++;
            double retryDelay = Mathf.Pow(2, Mathf.Min(7, rewardedRetryAttempt));

            Debug.Log("Rewarded ad failed to load with error code: " + errorInfo.Code);

            Invoke("LoadRewardedAd", (float)retryDelay);
        }

        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad failed to display. We recommend loading the next ad
            Debug.Log("Rewarded ad failed to display with error code: " + errorInfo.Code);
            rvFailureResponse?.Invoke("Failed To display Rewarded Ad!");
            LoadRewardedAd();
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Rewarded ad displayed");
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Rewarded ad clicked");
        }

        private void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad is hidden. Pre-load the next ad
            Debug.Log("Rewarded ad dismissed");
            LoadRewardedAd();
            StartCoroutine(CheckRVRewardReceive());
        }

        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad was displayed and user should receive the reward
            Debug.Log("Rewarded ad received reward");
            rvRewardReceived = true;
        }

        IEnumerator CheckRVRewardReceive()
        {
            yield return new WaitForSeconds(0.1f);
            if (rvRewardReceived)
            {
                if (GameController.instance != null)
                {
                    GameController.instance.ResetAdsTimer();
                }
                rvSuccessResponse?.Invoke("Reward Received!");
               // AdsMediationManager.instance.OnRVWatchSuccess();
            }
            //else
            //    rvFailureResponse?.Invoke("Rewarded Video Closed Early!");

            rvRewardReceived = false;
        }

        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad revenue paid. Use this callback to track user revenue.
            Debug.Log("Rewarded ad revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to
            string format = adInfo.AdFormat;

            Firebase.Analytics.Parameter[] impressionParameters = new Firebase.Analytics.Parameter[] {
            new Firebase.Analytics.Parameter("ad_platform", "AppLovin"),
            new Firebase.Analytics.Parameter("ad_source", networkName),
            new Firebase.Analytics.Parameter("ad_unit_name", adUnitIdentifier),
            new Firebase.Analytics.Parameter("ad_format", format),
            new Firebase.Analytics.Parameter("value", revenue),
            new Firebase.Analytics.Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
            };
            FirebaseManager.instance.CallAdImpressionEvent("ad_impression", impressionParameters);

            ApplovinImpressionData _data = new ApplovinImpressionData();
            _data.creative_id = adInfo.CreativeIdentifier;
            _data.placement = adInfo.Placement;
            _data.format = adInfo.AdFormat;
            _data.country = MaxSdk.GetSdkConfiguration().CountryCode;
            _data.ad_revenue_currency = "USD";
            _data.network_placement = adInfo.NetworkPlacement;
            _data.publisher_revenue_decimal = adInfo.Revenue;
            _data.revenue_precision = adInfo.RevenuePrecision;
            _data.ad_unit_id = adInfo.AdUnitIdentifier;
            _data.publisher_revenue_micro = adInfo.Revenue * 1000000;
            _data.revenue = adInfo.Revenue;
            _data.network_name = adInfo.NetworkName;

            Debug.Log($"Received Impression Data - {adInfo.Revenue} - {adUnitIdentifier} - {networkName} - {adInfo.NetworkPlacement}");
            TenjinManager.instance.ApplovinImpressionJson(LitJson.JsonMapper.ToJson(_data));
            TenjinManager.instance.UpdateIAARevenue(adInfo.Revenue);
        }



        #endregion

        #region Banner Ad Methods

        private void InitializeBannerAds()
        {
            if (string.IsNullOrEmpty(BannerAdUnitId) || AdsMediationManager.instance.isAdsRemoved)
                return;
            //// Attach Callbacks
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;

            // Banners are automatically sized to 320x50 on phones and 728x90 on tablets.
            // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments.
            MaxSdk.CreateBanner(BannerAdUnitId, MaxSdkBase.BannerPosition.TopCenter);

            // Set background or background color for banners to be fully functional.
            MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, Color.black);
            DisplayBanner();
        }

        public void DisplayBanner()
        {
            if (!isBannerShowing)
            {
                MaxSdk.ShowBanner(BannerAdUnitId);
            }
            else
            {
                MaxSdk.HideBanner(BannerAdUnitId);
            }
            isBannerShowing = !isBannerShowing;
        }
        public void HideBanner()
        {
            if (isBannerShowing)
                MaxSdk.HideBanner(BannerAdUnitId);
        }
        private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Banner ad is ready to be shown.
            // If you have already called MaxSdk.ShowBanner(BannerAdUnitId) it will automatically be shown on the next ad refresh.
            Debug.Log("Banner ad loaded");
        }

        private void OnBannerAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Banner ad failed to load. MAX will automatically try loading a new ad internally.
            Debug.Log("Banner ad failed to load with error code: " + errorInfo.Code);
        }

        private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Banner ad clicked");
        }

        private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Banner ad revenue paid. Use this callback to track user revenue.
            Debug.Log("Banner ad revenue paid");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

        }

        #endregion

        public class ApplovinImpressionData
        {
            public string creative_id;
            public string placement;
            public string format;
            public string country;
            public string ad_revenue_currency;
            public string network_placement;
            public double publisher_revenue_decimal;
            public string revenue_precision;
            public string ad_unit_id;
            public double publisher_revenue_micro;
            public double revenue;
            public string network_name;
        }
    }
}