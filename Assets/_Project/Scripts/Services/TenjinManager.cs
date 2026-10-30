using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using LitJson;
using System.Globalization;
using UnityEngine.Networking;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class TenjinManager : MonoBehaviour
{
    public static TenjinManager instance;

    public string tenjinAndroidKey;
    public string tenjinIosKey;
    public string revenueDataURL;
    [HideInInspector]
    public string tenjinKey;
    bool isSessionEventFired = false;
    public List<string> testDeviceIds;
    string deviceId;
    double iAARevenue;
    double iAPRevenue;
    int postCallbackVal;
    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            tenjinKey = tenjinAndroidKey;
#if UNITY_IPHONE
            tenjinKey = tenjinIosKey;
#endif
            if (PlayerPrefs.HasKey("Device_ID"))
            {
                deviceId = PlayerPrefs.GetString("Device_ID");
            }
            else
            {
                StartCoroutine(GetAdvertisingId());
            }
#if UNITY_IOS && !UNITY_EDITOR
            StartCoroutine(instance.ConnectWithDelay());
            LoadSavedRevenue();
            StartCoroutine(GetRevenueData());
#else
            instance.TenjinConnect();
#endif
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public IEnumerator ConnectWithDelay()
    {
        yield return new WaitForSeconds(3f); // Wait 3 seconds (iOS only)
        TenjinConnect();
    }

    private IEnumerator GetAdvertisingId()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        yield return null;
        using (var advertisingIdClientClass = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient"))
        {
            try
            {
                AndroidJavaObject activity = GetUnityActivity(); 
                AndroidJavaObject adInfo = advertisingIdClientClass.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", activity);

                string advertisingId = adInfo.Call<string>("getId");
                bool isLimitAdTrackingEnabled = adInfo.Call<bool>("isLimitAdTrackingEnabled");

                if (!string.IsNullOrEmpty(advertisingId))
                {
                    deviceId = advertisingId;
                    PlayerPrefs.SetString("Device_ID", deviceId);

                    Debug.Log($" Advertising ID: {advertisingId}");
                    Debug.Log($" Limit Ad Tracking: {isLimitAdTrackingEnabled}");
                }
                else
                {
                    deviceId = SystemInfo.deviceUniqueIdentifier;
                    Debug.LogWarning(" Advertising ID unavailable, using SystemInfo.deviceUniqueIdentifier.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($" Failed to get Advertising ID: {ex.Message}");
                deviceId = SystemInfo.deviceUniqueIdentifier;
            }
        }
#elif UNITY_IPHONE
            yield return null;
            BaseTenjin _instance = Tenjin.getInstance(instance.tenjinKey);
            deviceId = SystemInfo.deviceUniqueIdentifier;
            _instance.RequestTrackingAuthorizationWithCompletionHandler((status) =>
           {
               switch (status)
               {
                   case 0:
                       break;
                   case 1:
                       break;
                   case 2:
                       break;
                   case 3:
                       deviceId = Device.advertisingIdentifier;
                       PlayerPrefs.SetString("Device_ID", deviceId);
                       break;
                   default:
                       break;
               }
           });
#else
        deviceId = SystemInfo.deviceUniqueIdentifier;
        print("Called To Set Device ID");
        yield return null;
#endif
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    // Must be inside the same class and within the Android define
    private AndroidJavaObject GetUnityActivity()
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }
    }
#endif
    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus && instance != null)
        {
            TenjinConnect();
        }
        else if (instance != null)
        {
#if UNITY_IOS
            SaveRevenueData();
#endif
        }
    }


    public BaseTenjin TenjinConnect()
    {
        BaseTenjin _instance = Tenjin.getInstance(instance.tenjinKey);
        bool _canConnect = true;
#if UNITY_IPHONE
        _instance.RegisterAppForAdNetworkAttribution();

        string _version = UnityEngine.iOS.Device.systemVersion;
        int _len = _version.IndexOf(".");
        //   print(_version);
        string _initial = _len > 0 ? _version.Substring(0, _len) : "14";
        float iOSVersion = float.Parse(_initial);
        if (iOSVersion >= 14.0f)
        {
            print("IDFA-"+ Device.advertisingIdentifier);
            print("Requesting Tenjin");
            //Tenjin wrapper for requestTrackingAuthorization
            
           _instance.RequestTrackingAuthorizationWithCompletionHandler((status) =>
           {
               Debug.Log("===> App Tracking Transparency Authorization Status: " + status);
               switch (status)
               {
                   case 0:
                       Debug.Log("ATTrackingManagerAuthorizationStatusNotDetermined case");
                       Debug.Log("Not Determined");
                       Debug.Log("Unknown consent");
                       break;
                   case 1:
                       Debug.Log("ATTrackingManagerAuthorizationStatusRestricted case");
                       Debug.Log(@"Restricted");
                       Debug.Log(@"Device has an MDM solution applied");
                       break;
                   case 2:
                       Debug.Log("ATTrackingManagerAuthorizationStatusDenied case");
                       Debug.Log("Denied");
                       Debug.Log("Denied consent");
                       break;
                   case 3:
                       Debug.Log("ATTrackingManagerAuthorizationStatusAuthorized case");
                       Debug.Log("Authorized");
                       Debug.Log("Granted consent");
                       print("IDFA-" + Device.advertisingIdentifier);
                       _canConnect = true;

                       break;
                   default:
                       Debug.Log("Unknown");
                       break;
               }
           });
        }
        else
        {
            print("Directly Called Connect");
            _instance.Connect();

        }
#elif UNITY_ANDROID
        _instance.SetAppStoreType(AppStoreType.googleplay);
        // Sends install/open event to Tenjin
        _canConnect = true;
#endif
        if (_canConnect)
        {

            _instance.Connect();
            return _instance;
        }
        return null;
    }
    public void UpdateTestDevices(string _json)
    {
        if (string.IsNullOrEmpty(_json))
        {
            return;
        }

        TestDeviceIds _devices = JsonMapper.ToObject<TestDeviceIds>(_json);
        if (_devices != null)
        {
            if (testDeviceIds == null)
            {
                testDeviceIds = new List<string>();
            }
            for (int i = 0; i < _devices.deviceIds.Length; i++)
            {
                testDeviceIds.Add(_devices.deviceIds[i]);
            }
        }
    }
    public string GetDeviceId()
    {
        return deviceId;
    }
    public bool IsTestDevice()
    {
        if (testDeviceIds == null)
        {
            return false;
        }
        if (string.IsNullOrEmpty(deviceId))
        {
            return false;
        }
        return (testDeviceIds.Contains(deviceId));
    }
    public void SubscibeApplovinImpressions()
    {
        if (IsTestDevice())
        {
            return;
        }
        BaseTenjin _instance = TenjinConnect();
        if (_instance != null)
        {
            _instance.SubscribeAppLovinImpressions();

            //print("Sending Tenjin Applovin Event");
            //CallTenjinEvent("SendigApplovin");
        }
    }
    public void ApplovinImpressionJson(string _json)
    {
        //if (IsTestDevice())
        //{
        //    return;
        //}
        BaseTenjin _instance = TenjinConnect();
        if (_instance != null)
        {
            print(_json);
            _instance.AppLovinImpressionFromJSON(_json);
#if UNITY_IOS
            // _instance.UpdatePostbackConversionValue(2, "medium");
            // _instance.UpdatePostbackConversionValue(3, "medium");
#endif


        }
    }
    public void AdmobImpressionJson(string _json)
    {
        if (IsTestDevice())
        {
            return;
        }
        BaseTenjin _instance = TenjinConnect();
        if (_instance != null)
        {
            print(_json);
            _instance.AdMobImpressionFromJSON(_json);
#if UNITY_IOS
            // _instance.UpdatePostbackConversionValue(2, "medium");
            // _instance.UpdatePostbackConversionValue(3, "medium");
#endif

        }
    }
    public void SubscribeAdmobInterstitialImppression(InterstitialAd _inter, string _adUnitId)
    {
        if (IsTestDevice())
        {
            return;
        }
        BaseTenjin _instance = TenjinConnect();
        if (_instance != null)
        {
            _instance.SubscribeAdMobInterstitialAdImpressions(_inter, _adUnitId);
        }
    }
    public void SubscribeAdmobRewardedVideoImpression(RewardedAd _rv, string _adUnitId)
    {
        if (IsTestDevice())
        {
            return;
        }
        BaseTenjin _instance = TenjinConnect();
        if (_instance != null)
        {
            _instance.SubscribeAdMobRewardedAdImpressions(_rv, _adUnitId);
        }
    }
    public void SubscribeAdmobBannerImpression(BannerView _banner, string _adUnitId)
    {
        if (IsTestDevice())
        {
            return;
        }
        BaseTenjin _instance = TenjinConnect();
        if (_instance != null)
        {
            _instance.SubscribeAdMobBannerViewImpressions(_banner, _adUnitId);
        }
    }

    public void CallSessionEvent()
    {
        if (IsTestDevice())
        {
            print("Test Device Skipped!");

            return;
        }
        if (isSessionEventFired)
        {
            return;
        }
        BaseTenjin _instance = TenjinConnect();
        if (_instance != null)
        {
            _instance.SendEvent("Session");
            _instance.UpdatePostbackConversionValue(1, "low");
            isSessionEventFired = true;

        }
    }
    public void OnAndroidPurcahaseTransaction(string _productId, string _currencyCode, int _quantity, double _unitPrice, string _receipt, string _signature)
    {
        if (IsTestDevice())
        {
            print("Test Device Skipped!");

            return;
        }
        BaseTenjin _instance = Tenjin.getInstance(instance.tenjinKey);
        _instance.Transaction(_productId, _currencyCode, _quantity, _unitPrice, null, _receipt, _signature);

    }
    public void OnIOSPurcahseTransaction(string _productId, string _currencyCode, int _quantity, double _unitPrice, string _transactionId, string _receipt)
    {
        if (IsTestDevice())
        {
            print("Test Device Skipped!");
            return;
        }
        BaseTenjin _instance = Tenjin.getInstance(instance.tenjinKey);
        _instance.Transaction(_productId, _currencyCode, _quantity, _unitPrice, _transactionId, _receipt, null);
        // _instance.UpdatePostbackConversionValue(6, "high");
        // _instance.UpdatePostbackConversionValue(7, "high");
        iAPRevenue += _unitPrice;
        CheckToSendCallback();
    }
    #region Handle IOS PostCallBack Revenue
    List<PostCallbackRevenueData> postCallBackList;
    void LoadSavedRevenue()
    {
        iAARevenue = double.Parse(PlayerPrefs.GetString("IAARevenue", "0"), CultureInfo.InvariantCulture);
        iAPRevenue = double.Parse(PlayerPrefs.GetString("IAPRevenue", "0"), CultureInfo.InvariantCulture);
        postCallbackVal = PlayerPrefs.GetInt("PostCallbackVal", 0);
    }
    void SaveRevenueData()
    {
        PlayerPrefs.SetString("IAARevenue", iAARevenue.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetString("IAPRevenue", iAPRevenue.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetInt("PostCallbackVal", postCallbackVal);
    }

    IEnumerator GetRevenueData()
    {
        postCallBackList = new List<PostCallbackRevenueData>();
        UnityWebRequest _req = UnityWebRequest.Get(revenueDataURL);
        _req.timeout = 60;
        yield return _req.SendWebRequest();
        if (string.IsNullOrEmpty(_req.error))
        {
            SortRevenueData(_req.downloadHandler.text, false);

        }
        else
        {
            GetSavedRevenueData();
        }
    }
    void GetSavedRevenueData()
    {
        string _data = (string)SerializationManager.LoadFile("PostCallbackData");
        if (!string.IsNullOrEmpty(_data))
        {
            SortRevenueData(_data, true);
        }
    }
    void SortRevenueData(string _data, bool _localData)
    {
        print("Callback Data to sort is===============");
        print(_data);
        if (postCallBackList == null)
        {
            postCallBackList = new List<PostCallbackRevenueData>();
        }
        postCallBackList.Clear();
        string[] _texts = _data.Split("\n");
        if (_texts.Length < 2)
        {
            if (!_localData)
            {
                GetSavedRevenueData();
            }
            return;
        }
        for (int i = 1; i < _texts.Length; i++)
        {
            string[] _values = _texts[i].Split(",");
            if (_values.Length < 3)
            {
                continue;
            }
            PostCallbackRevenueData _callBackData = new PostCallbackRevenueData();
            int.TryParse(_values[0], out _callBackData.callbackVal);
            double.TryParse(_values[1], out _callBackData.minVal);
            double.TryParse(_values[2], out _callBackData.maxVal);
            postCallBackList.Add(_callBackData);
        }
        print("Callback List Updated=====" + postCallBackList.Count);
        if (!_localData)
        {
            SerializationManager.Save(_data, "PostCallbackData");
        }
    }

    public void UpdateIAARevenue(double _val)
    {
#if UNITY_IOS
        iAARevenue+=_val;
        CheckToSendCallback();
#endif
    }
    void CheckToSendCallback()
    {
        if (postCallBackList == null)
        {
            return;
        }
        double _totalVal = iAARevenue + iAPRevenue;
        int _index = -1;
        for (int i = 0; i < postCallBackList.Count; i++)
        {
            if (iAPRevenue > 0 && postCallBackList[i].callbackVal < 41)
            {
                continue;
            }
            if (iAPRevenue <= 0 && postCallBackList[i].callbackVal > 40)
            {
                break;
            }
            if (_totalVal >= postCallBackList[i].minVal)
            {
                _index = i;
            }
            else
            {
                break;
            }
        }
        if (_index > -1)
        {
            if (postCallBackList[_index].callbackVal > postCallbackVal)
            {
                postCallbackVal = postCallBackList[_index].callbackVal;
                BaseTenjin _instance = Tenjin.getInstance(instance.tenjinKey);
                string _coarseVal = "low";
                if (_totalVal >= 0.1)
                {
                    _coarseVal = "medium";
                }
                if (_totalVal >= 0.99)
                {
                    _coarseVal = "high";
                }
                _instance.UpdatePostbackConversionValue(postCallbackVal, _coarseVal);
            }
        }
    }
    #endregion
}

[System.Serializable]
public class TestDeviceIds
{
    public string[] deviceIds;
}
[System.Serializable]
public class PostCallbackRevenueData
{
    public int callbackVal;
    public double minVal;
    public double maxVal;
}