using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Ump.Api;
using JetBrains.Annotations;
#if UNITY_IOS
 
using Unity.Advertisement.IosSupport;
 
#endif



namespace AdsMediation
{
    [RequireComponent(typeof(GoogleMobileAdsConsentController))]
    [RequireComponent(typeof(AdMobController))]
    [RequireComponent(typeof(MaxAdsManager))]
    public class AdsMediationManager : MonoBehaviour
    {
        /// <summary>Raised every N interstitials when ads are not removed; hook a remove-ads / promo panel here.</summary>
        public static event System.Action OnPromotionalPanelRequested;
        public System.Action InterstitialClosed; // by ss
        public bool isAdsRemoved;
        [Header("Delay Between 2 conssecutive Interstitial Ads ")]
        public bool displayAdmobBanner;
        public float interstitialAdDelay;
        public static AdsMediationManager instance;
        GoogleMobileAdsConsentController consentController;
        AdMobController admobAds;
        MaxAdsManager maxAds;

        public delegate void RewardedVieoResponse(string _responseMsg);
        int mediationToUse = -1;
        bool hasAttConsent = true; // non-iOS / editor default
        float interAdDelayTimer;
        int interstitialSuccessCounter;

        ConsentFetchStatus consentStatus;
        enum ConsentFetchStatus
        {
            Waiting,
            Fetching,
            Fetched,
            Failed
        }
        // public float interAdDelayTimer1;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                consentController = GetComponent<GoogleMobileAdsConsentController>();
                admobAds = GetComponent<AdMobController>();
                maxAds = GetComponent<MaxAdsManager>();
                interAdDelayTimer = interstitialAdDelay;
                isAdsRemoved = PlayerPrefs.GetInt("RemoveAds", 0) == 1;
                mediationToUse = 0;
                consentStatus = ConsentFetchStatus.Waiting;///by ss
                interstitialSuccessCounter = 1;
            }
            else
            {
                Destroy(gameObject);
            }
            // if (mediationToUse >= 0)
            // {
            //     instance.CheckForConsent();
            // }
        }
        public void CheckForConsent()
        {
            if (consentController.CanRequestAds)
            {
                StartCoroutine(InitializeAdsAfterConsentDelay());
            }
            else
            {
                InitializeGoogleMobileAdsConsent();
            }
        }
        // public void InitializeMediationAds(int _mediationVal)
        // {
        //     mediationToUse = _mediationVal;
        //    // mediationToUse = 1;

        //     CheckForConsent();
        // }


        public void InitializeMediationAds(int _mediationVal = 0)
        {
            try
            {
                mediationToUse = _mediationVal;
                if (consentStatus == ConsentFetchStatus.Waiting || consentStatus == ConsentFetchStatus.Failed)
                {
                    consentStatus = ConsentFetchStatus.Fetching;
                    CheckforTrackingTransparency();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Exception occurred:" + ex);
            }
        }
        //public bool IsAdsInitialized()
        //{
        //    return (admobAds.isInitialized && maxAds.isInitialized);
        //}
        public bool IsAdsInitialized()
        {
            if (mediationToUse == 0)
            {
                return maxAds.isInitialized;
            }

            return admobAds.isInitialized;
        }

        void CheckforTrackingTransparency()   // new add on 6-4
        {
#if UNITY_IOS
            // check with iOS to see if the user has accepted or declined tracking
            var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
 
            if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                ATTrackingStatusBinding.RequestAuthorizationTracking();
                StartCoroutine(WaitForTransparencyResponse());
            }
            else if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED)
            {
                hasAttConsent = true;
                CheckForConsent();
            }
            else
            {
                hasAttConsent = false;
                consentStatus = ConsentFetchStatus.Fetched;
               // StartCoroutine(InitializeAds());
               StartCoroutine(InitializeAdsAfterConsentDelay());
            }

#else
            CheckForConsent();
#endif
        }
        IEnumerator WaitForTransparencyResponse()
        {
#if UNITY_IOS
            var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
 
            while (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                yield return new WaitForSeconds(0.1f);
                status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
            }
            if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED)
            {
                hasAttConsent = true;
                CheckForConsent();
            }
            else
            {
                hasAttConsent = false;
                consentStatus = ConsentFetchStatus.Fetched;
                //StartCoroutine(InitializeAds());
                StartCoroutine(InitializeAdsAfterConsentDelay());
            }
#endif
            yield return null;
            //yield break;
        }

        private void InitializeGoogleMobileAdsConsent()
        {
            Debug.Log("Google Mobile Ads gathering consent.");

            consentController.GatherConsent((string error) =>
            {
                if (error != null)
                {
                    Debug.LogError("Failed to gather consent with error: " +
                        error);
                    consentStatus = ConsentFetchStatus.Failed;//by ss
#if UNITY_ANDROID && !UNITY_EDITOR
                    Invoke("RetryConsentRequest", 5f); // by ss
#endif
                }
                else
                {
                    Debug.Log("Google Mobile Ads consent updated: "
                        + ConsentInformation.ConsentStatus);
                }
                consentStatus = ConsentFetchStatus.Fetched;//by ss
                if (consentController.CanRequestAds)
                {
                    // print("Initializing Ads!!!!!");
                    StartCoroutine(InitializeAdsAfterConsentDelay());
                }
            });
        }
        void RetryConsentRequest()//by ss
        {
            if (InternetConnectivity.instance.isInterNetAvailable())
            {
                CheckForConsent();
                return;
            }

            Invoke("RetryConsentRequest", 5f);
        }
        private IEnumerator InitializeAdsAfterConsentDelay()
        {
            yield return null;
            yield return new WaitForSeconds(0.5f);

            InitializeAds();
        }
        public void InitializeAds()
        {
            if (mediationToUse == 0)
            {
                maxAds.InitializeAds(true, hasAttConsent);
            }
            else
            {
                //maxAds.InitializeAds();

                admobAds.InitializeAds();
            }
        }
        public void ShowInterstitial()
        {
            if (interAdDelayTimer > 0f)
                return;
            interAdDelayTimer = interstitialAdDelay;
            if (isAdsRemoved)
                return;
#if UNITY_IOS || UNITY_IPHONE
    //  if (GameTutorialManager.instance != null)
    //     {
    //         if (!GameTutorialManager.instance.isTutorialComplete)
    //         {
    //             return;
    //         }
    //     }
        //if(!Remoteconfig.instance.adsEnables)return;
#endif
            if (mediationToUse == 0)
            {
                if (maxAds.IsInterstitialLoaded())
                {
                    maxAds.ShowInterstitial();
                }
            }
            else
            {
                if (admobAds.IsInterstitialLoaded())
                {
                    admobAds.ShowInterstitialAd();
                }
            }

        }

        public void ShowTestInterstitial()
        {
            if (mediationToUse == 0)
            {
                if (maxAds.IsInterstitialLoaded())
                {
                    maxAds.ShowInterstitial();
                }
            }
            else
            {
                if (admobAds.IsInterstitialLoaded())
                {
                    admobAds.ShowInterstitialAd();
                }
            }

        }

        public bool CanShowInterstitial()
        {
            if (isAdsRemoved)
                return false;
            bool _canShow = false;
            _canShow = (interAdDelayTimer <= 0);
            if (_canShow)
            {
                if (mediationToUse == 0)
                {
                    _canShow = maxAds.IsInterstitialLoaded();
                }
                else
                {
                    _canShow = admobAds.IsInterstitialLoaded();
                }
            }
            return _canShow;
        }
        public void RemoveBannerAd()
        {
            admobAds.RemoveBanner();
        }
        public void DisplayBanner()
        {
            if (displayAdmobBanner)
            {
                admobAds.DisplayBanner();
            }
        }
        public void ShowRewardedVideo(RewardedVieoResponse _success = null, RewardedVieoResponse _failure = null)
        {
            if (mediationToUse == 0)
            {
                if (maxAds.CanShowRewardedVideo())
                {
                    maxAds.ShowRewardedAd(_success, _failure);

                }
                else
                {
                    print("If MediATION == 0 Not Available!++++++++=");
                    _failure?.Invoke("Ad Not Available!");

                }
            }
            else
            {
                if (admobAds.CanShowRewaredVideo())
                {
                    admobAds.ShowRewardedAd(_success, _failure);

                }
                else
                {
                    print("else MediATION  Not Available!++++++++=");
                    _failure?.Invoke("Ad Not Available!");


                }
            }
        }
        public void OnRemoveAds()
        {
            isAdsRemoved = true;
            PlayerPrefs.SetInt("RemoveAds", 1);
            if (admobAds.isInitialized)
                admobAds.DestroyBannerAd();
            if (maxAds.isInitialized)
                maxAds.HideBanner();

            Debug.Log("remove ad purchased");
        }
        public void HideBannerAdd()
        {
            //print("Hid Banner");
            admobAds.DestroyBannerAd();
        }
        public void OnInterstitialClosed()
        {
            InterstitialClosed?.Invoke();
            InterstitialClosed = null;
            interAdDelayTimer = interstitialAdDelay;
            if (GameTutorialManager.instance != null && !GameTutorialManager.instance.isTutorialComplete)
            {
                return;
            }

            interstitialSuccessCounter--;
            if (interstitialSuccessCounter <= 0)
            {
                if (!isAdsRemoved)
                {
                    OnPromotionalPanelRequested?.Invoke();
                    interstitialSuccessCounter = 5;
                }
            }

        }
        public void OnInterClosedForNonRewardedAd()
        {
            print("Happy");
            interAdDelayTimer = interstitialAdDelay;
            if (GameController.instance != null) GameController.instance.ResetAdsTimer();
        }
        // Start is called before the first frame update

        // Update is called once per frame
        void Update()
        {

            interAdDelayTimer -= Time.deltaTime;
            // interAdDelayTimer1 = interAdDelayTimer;
        }
    }
}

