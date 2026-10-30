using UnityEngine.Events;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using System.Collections.Generic;
using GoogleMobileAds.Api;

namespace AdsMediation
{
    public class AdMobController : MonoBehaviour
    {
        [Header("Android Ids")]
        public string androidBannerId;
        public string androidInterstitialId;
        public string androidRewardedVideoId;

        [Header("Ios Ids")]
        public string iosBannerId;
        public string iosInterstitialId;
        public string iosRewardedVideoId;

        public bool isInitialized = false;
        bool initiallyAdsLoaded;
        bool loadAdsOnInitialization;
    

        private BannerView bannerView;
        private InterstitialAd interstitialAd;
        private RewardedAd rewardedAd;

        bool isPersonalizedads;
        bool rewardedInterReceived = false;
        bool rvRewardReceived = false;
        bool loadBannerMust;

        int interRetryAttempt;
        int rvRetryAttempt;
        int rvInterRetryAttempy;

        AdsMediationManager.RewardedVieoResponse rvSuccessEvent, rvFailEvent;

        #region UNITY MONOBEHAVIOR METHODS


        public void InitializeAds(/*bool _loadAds = true*/ bool _loadBanner = false)
        {
            if (isInitialized)
                return;
            //RequestConfiguration _config = new RequestConfiguration.Builder.SetTagForChildDirectedTreatment(TagForChildDirectedTreatment.Unspecified).build();
            //MobileAds.SetRequestConfiguration(_config);
            // AdColonyAppOptions.SetGDPRRequired(true);
            //   AdColonyAppOptions.SetGDPRConsentString(AdColonyAppOptions.GetGDPRConsentString());
            //AppLovin.Initialize();
            if (_loadBanner)
                RequestBannerAd();
            loadBannerMust = _loadBanner;
#if UNITY_IPHONE
                      MobileAds.SetiOSAppPauseOnBackground(true);

                    //if (AudiocallManager.PersonalizedAdStatus==0)
                    //{
                    //    //Show Personalized Ads
                    //    isPersonalizedads = true;
                    //}
                    //else
                    //{
                    //    //Show NonPersonalized Ads
                    //    isPersonalizedads = false;
                    //}
#endif
            //  MobileAds.RaiseAdEventsOnUnityMainThread = true;   // copy from other script.. by write this. every events we receive from admob will run on unity main thread
            MobileAds.Initialize(HandleInitCompleteAction);

        }
        //        public void InitializeAds(bool _loadAds = true, bool _loadBanner = false)
        //        {
        //            print("Initializing Admob Ads -" + isInitialized);

        //            if (isInitialized)
        //            {
        //                if (_loadAds && !initiallyAdsLoaded)
        //                {
        //                    initiallyAdsLoaded = true;
        //                    RequestAndLoadRewardedAd();
        //                    RequestAndLoadInterstitialAd();

        //                }
        //                if (_loadBanner)
        //                    RequestBannerAd();
        //                return;
        //            }
        //            Debug.Log("Initializing Admob Ads");
        //            loadAdsOnInitialization = _loadAds;
        //            loadBannerMust = _loadBanner;
        //            //RequestConfiguration _config = new RequestConfiguration.Builder.SetTagForChildDirectedTreatment(TagForChildDirectedTreatment.Unspecified).build();
        //            //MobileAds.SetRequestConfiguration(_config);
        //            // AdColonyAppOptions.SetGDPRRequired(true);
        //            //   AdColonyAppOptions.SetGDPRConsentString(AdColonyAppOptions.GetGDPRConsentString());
        //            //AppLovin.Initialize();
        //#if UNITY_IPHONE
        //              MobileAds.SetiOSAppPauseOnBackground(true);

        //            //if (AudiocallManager.PersonalizedAdStatus==0)
        //            //{
        //            //    //Show Personalized Ads
        //            //    isPersonalizedads = true;
        //            //}
        //            //else
        //            //{
        //            //    //Show NonPersonalized Ads
        //            //    isPersonalizedads = false;
        //            //}
        //#endif
        //            //  MobileAds.RaiseAdEventsOnUnityMainThread = true;   // copy from other script.. by write this. every events we receive from admob will run on unity main thread
        //            MobileAds.Initialize(HandleInitCompleteAction);

        //        }
        private void HandleInitCompleteAction(InitializationStatus initstatus)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isInitialized = true;
                if(loadBannerMust)
                {
                    RequestBannerAd();
                }
                print("Admob Initialization Complete");

                initiallyAdsLoaded = true;
                RequestAndLoadRewardedAd();
                RequestAndLoadInterstitialAd();
            });
        }


        private void Update()
        {

        }
        #endregion


        #region BANNER ADS
        // call this function to load the banner Ad
        public void RemoveBanner()
        {
            print("333b");
            if (!isInitialized)
                return;
            DestroyBannerAd();
        }
        public void DisplayBanner()
        {
            if (!isInitialized || !loadBannerMust)
                return;
            RequestBannerAd();
        }
        public void RequestBannerAd()
        {
           

            // These ad units are configured to always serve test ads.
#if UNITY_EDITOR
            string adUnitId = "unused";
#elif UNITY_ANDROID
            string adUnitId = androidBannerId;
#elif UNITY_IPHONE
            string adUnitId = iosBannerId;
#else
        string adUnitId = "unexpected_platform";
#endif

            if (string.IsNullOrEmpty(adUnitId) || AdsMediationManager.instance.isAdsRemoved)
            {
                return;
            }
            // Clean up banner before reusing
            if (bannerView != null)
            {
                DestroyBannerAd();   // delete previous one if present
            }

            // Create a 320x50 banner at top of the screen
            bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Top);   // for banner shape and its position
                                                                                    // Add Event Handlers
            bannerView.OnBannerAdLoaded += () =>
            {
                print("Banner Ad Loaded");
            };
            bannerView.OnBannerAdLoadFailed += (LoadAdError _error) =>
            {
                print("Banner Failed to loaad with error " + _error.GetMessage());
            };

            bannerView.OnAdImpressionRecorded += () =>
            {
                print("Banner ad recorded an impression.");
            };
            bannerView.OnAdClicked += () =>
            {
                print("Banner ad recorded a click.");
            };
            bannerView.OnAdFullScreenContentOpened += () =>
            {
                print("Banner ad opening.");
            };
            bannerView.OnAdFullScreenContentClosed += () =>
            {
                print("Banner ad closed.");
            };
            bannerView.OnAdPaid += (AdValue adValue) =>
            {
                string msg = string.Format("{0} (currency: {1}, value: {2}",
                                            "Banner ad received a paid event.",
                                            adValue.CurrencyCode,
                                            adValue.Value);
                print(msg);
            };

            var _req = new AdRequest();
            // Load a banner ad
            bannerView.LoadAd(_req);
            TenjinManager.instance.SubscribeAdmobBannerImpression(bannerView, adUnitId);
        }

        public void DestroyBannerAd()
        {
           // print("444b");
            if (bannerView != null)
            {
               // print("555b");
                bannerView.Destroy();   // delete previous one if present
                bannerView = null;
            }
        }
        #endregion

        #region INTERSTITIAL ADS
        // call this function to load the Interstitial Ad
        public void RequestAndLoadInterstitialAd()
        {
            print("Requesting Admob Interstitial ad.");

#if UNITY_EDITOR
            string adUnitId = "unused";
#elif UNITY_ANDROID
            string adUnitId = androidInterstitialId;
#elif UNITY_IPHONE
            string adUnitId = iosInterstitialId;
#else
        string adUnitId = "unexpected_platform";
#endif
            if (string.IsNullOrEmpty(adUnitId) || AdsMediationManager.instance.isAdsRemoved)
            {
                return;
            }
            // Clean up interstitial before using it
            DestroyInterstitialAd();

            /*
            * copy from other script
            * this basically tells the admob server that the ads that its requested is going to display inside the unity game, so Admob will choose
            * an Ad accordingly and you can just add more keywords.
            * And it is optional 
           var adRequest = new AdRequest();
           adRequest.Keywords.Add("unity-admob-sample");
           *
           */
            var _req = new AdRequest();
            // Load an interstitial ad
            InterstitialAd.Load(adUnitId, _req,
                (InterstitialAd ad, LoadAdError loadError) =>
                {
                    if (loadError != null)
                    {
                        interRetryAttempt++;
                        double retryDelay = Mathf.Pow(2, Mathf.Min(7, interRetryAttempt));
                        Invoke("RequestAndLoadInterstitialAd", (float)retryDelay);

                        print("Interstitial ad failed to load with error: " +
                            loadError.GetMessage());
                        return;
                    }
                    else if (ad == null)    //Failed To Load Inter
                    {
                        print("Interstitial ad failed to load.");
                        interRetryAttempt++;
                        double retryDelay = Mathf.Pow(2, Mathf.Min(7, interRetryAttempt));
                        Invoke("RequestAndLoadInterstitialAd", (float)retryDelay);

                        return;
                    }

                    print("Interstitial ad loaded.");
                    interstitialAd = ad;    // will call this, ShowInterstitialAd()

                    ad.OnAdFullScreenContentOpened += () => //On Ad Opened Callback
                    {
                        print("Interstitial ad opening.");
                    };
                    ad.OnAdFullScreenContentClosed += () => //OnAd Closed Callback
                    {
                        print("Interstitial ad closed.%%%");
                       // AdsMediation.AdsMediationManager.instance.interAdDelayTimer = AdsMediation.AdsMediationManager.instance.interstitialAdDelay;
                       // GameController.instance.ResetAdsTimer();
                        //print("Bike" + AdsMediation.AdsMediationManager.instance.interAdDelayTimer);
                       
                        RequestAndLoadInterstitialAd();
                        AdsMediationManager.instance.OnInterClosedForNonRewardedAd();
                        // talha.. added
                        //     LoadAdBeforeLoadingTheScene();
                    };
                    ad.OnAdImpressionRecorded += () =>  //On Recorded Impression
                    {
                        print("Interstitial ad recorded an impression.");
                    };
                    ad.OnAdClicked += () => //OnAd Clicked
                    {
                        print("Interstitial ad recorded a click.");
                    };
                    ad.OnAdFullScreenContentFailed += (AdError error) =>    //Inter Ad Failed to show
                    {
                        print("Interstitial ad failed to show with error: " +
                                    error.GetMessage());

                        RequestAndLoadInterstitialAd();

                    };
                    ad.OnAdPaid += (AdValue adValue) => //OnAd Paid
                    {
                        string msg = string.Format("{0} (currency: {1}, value: {2}",
                                                   "Interstitial ad received a paid event.",
                                                   adValue.CurrencyCode,
                                                   adValue.Value);
                        print(msg);
                    };
                });
            TenjinManager.instance.SubscribeAdmobInterstitialImppression(interstitialAd, adUnitId);

        }
        public void ShowInterstitialAd()
        {
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                interstitialAd.Show();
            }
        }

        public void DestroyInterstitialAd()
        {
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }
        }
        public bool IsInterstitialLoaded()
        {
            if (!isInitialized)
                return false;

            return (interstitialAd != null && interstitialAd.CanShowAd());
        }
        #endregion

        #region REWARDED ADS
        // call this function to load the Rewarded Ad
        public void RequestAndLoadRewardedAd()
        {
#if UNITY_EDITOR
            string adUnitId = "unused";
#elif UNITY_ANDROID
        string adUnitId = androidRewardedVideoId;
#elif UNITY_IPHONE
        string adUnitId = iosRewardedVideoId;
#else
        string adUnitId = "unexpected_platform";
#endif
            if (string.IsNullOrEmpty(adUnitId))
            {
                return;
            }
            // Clean up RewardedAd before using it
            DestroyRewardedAd();
            //adRequest.Keywords.Add("unity-admob-sample");
            //Loading A Rewarded Ad with Add Event Handlers
            var _req = new AdRequest();
            RewardedAd.Load(adUnitId, _req,
                (RewardedAd ad, LoadAdError loadError) =>
                {
                    if (loadError != null)
                    {
                        print("Rewarded ad failed to load with error: ");
                        print(loadError.GetMessage() + "--" + loadError.GetCode());

                        rvRetryAttempt++;
                        double retryDelay = Mathf.Pow(2, Mathf.Min(7, rvRetryAttempt));
                        Invoke("RequestAndLoadRewardedAd", (float)retryDelay);

                        return;
                    }
                    else if (ad == null)
                    {
                        rvRetryAttempt++;
                        double retryDelay = Mathf.Pow(2, Mathf.Min(7, rvRetryAttempt));
                        Invoke("RequestAndLoadRewardedAd", (float)retryDelay);
                        return;
                    }

                    print("Rewarded ad loaded.");
                    rewardedAd = ad;    // will call this function ShowRewardedAd()

                    // below are all rewarded Ad events
                    rewardedAd.OnAdFullScreenContentOpened += () =>
                        {
                            print("Rewarded ad opening.");


                        };
                    rewardedAd.OnAdFullScreenContentClosed += () =>
                    {
                        print("Rewarded ad closed.");
                       // AdsMediation.AdsMediationManager.instance.interAdDelayTimer = AdsMediation.AdsMediationManager.instance.interstitialAdDelay;
                       // GameController.instance.ResetAdsTimer();
                        //print("Bike1" + AdsMediation.AdsMediationManager.instance.interAdDelayTimer);
                        StartCoroutine(CheckForRewardEarned());
                    };
                    rewardedAd.OnAdImpressionRecorded += () =>
                    {
                        print("Rewarded ad recorded an impression.");
                    };
                    rewardedAd.OnAdClicked += () =>
                    {
                        print("Rewarded ad recorded a click.");
                    };
                    rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
                    {
                        print("Rewarded ad failed to show with error: ");
                        if (rvFailEvent != null)
                            rvFailEvent?.Invoke("Failed To Show Ad!");
                        RequestAndLoadRewardedAd();
                    };
                    rewardedAd.OnAdPaid += (AdValue adValue) =>
                    {
                        string msg = string.Format("{0} (currency: {1}, value: {2}",
                                                   "Rewarded ad received a paid event.",
                                                   adValue.CurrencyCode,
                                                   adValue.Value);
                        print(msg);
                    };
                });
            TenjinManager.instance.SubscribeAdmobRewardedVideoImpression(rewardedAd, adUnitId);

        }

        public void ShowRewardedAd(AdsMediationManager.RewardedVieoResponse _successAction = null, AdsMediationManager.RewardedVieoResponse _failureAction = null)
        {
            rvSuccessEvent = _successAction;
            rvFailEvent = _failureAction;
            rvRewardReceived = false;

            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                rewardedAd.Show((Reward _reward) =>
                {
                    print("Reward Granted!");   //when user complete watch the video
                    rvRewardReceived = true;
                });
            }
            else
            {
                if (_failureAction != null)
                    _failureAction?.Invoke("Failed To Load Ad!");
            }
        }
        public bool CanShowRewaredVideo()
        {
            if (!isInitialized)
                return false;

            return (rewardedAd != null && rewardedAd.CanShowAd());
        }
        System.Collections.IEnumerator CheckForRewardEarned()
        {
            yield return new WaitForSeconds(0.1f);
            print("Checking Reward Earned");
            if (rvRewardReceived)
            {
                if (rvSuccessEvent != null)
                    rvSuccessEvent?.Invoke("Reward Earned!");
                if (GameController.instance != null)
                {
                    GameController.instance.ResetAdsTimer();
                    AdsMediation.AdsMediationManager.instance.OnInterstitialClosed();

                }
                
            }
            else
            {
                if (rvFailEvent != null)
                    rvFailEvent?.Invoke("Reward Failed!");
            }
            rvRewardReceived = false;
            rvSuccessEvent = null;
            rvFailEvent = null;
            RequestAndLoadRewardedAd();
        }

        public void DestroyRewardedAd()
        {
            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
                rewardedAd = null;
            }
        }
        #endregion

    }
}