using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using Firebase.Extensions;


using UnityEngine.UI;
using Firebase.Analytics;
using Firebase.RemoteConfig;

public class Remoteconfig : MonoBehaviour
{
  
    public static Remoteconfig instance;
    //  public Image adsvariations;
    string tenjinTestDevices = "testDevices";
    string mediation = "mediation";
    bool fetchData;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeRemoteConfig()
    {
        print("Calling to initialize Remote Configurations!");
        // [START set_defaults]
       // Dictionary<string, object> defaults = new Dictionary<string, object>();

        Dictionary<string, object> defaults = SetDefaultValues();
      //  yield return null;
        // These are the values that are used if we haven't fetched data from the
        // server
        // yet, or if we ask for values that the server doesn't have:
//        string _default = "mediation";


//#if UNITY_IPHONE
//        _default = "mediation_ios";

//#endif

       // defaults.Add(_default, 0);
        print("Adding SDefault data");
        Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults)
          .ContinueWithOnMainThread(task =>
          {
              // [END set_defaults]
              Debug.Log("RemoteConfig configured and ready!");
              //  Firebase.RemoteConfig.ConfigSettings=1
              FetchDataAsync();
          });
    }
    private Dictionary<string, object> SetDefaultValues()
    {
        Debug.Log("Setting default RemoteConfig values...");
#if UNITY_IPHONE
        
        tenjinTestDevices = "testDevices_ios";
         mediation = "mediation_ios";
#endif
        return new Dictionary<string, object>
        { 
            {tenjinTestDevices,""},
            {mediation,"" }
        };
    }
    public void OnDataFetched()
    {
        Debug.Log("___Remote Config Data_____");

        Debug.Log("mediation: " +

                 Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue("mediation").LongValue);

        TenjinManager.instance.UpdateTestDevices(FirebaseRemoteConfig.DefaultInstance.GetValue(tenjinTestDevices).StringValue);
        // AdsMediation.AdsMediationManager.instance.InitializeMediationAds((int)FirebaseRemoteConfig.DefaultInstance.GetValue("mediation").LongValue);
    }

    // [START fetch_async]
    // Start a fetch request.
    // FetchAsync only fetches new data if the current data is older than the provided
    // timespan.  Otherwise it assumes the data is "recent enough", and does nothing.
    // By default the timespan is 12 hours, and for production apps, this is a good
    // number. For this example though, it's set to a timespan of zero, so that
    // changes in the console will always show up immediately.
    public System.Threading.Tasks.Task FetchDataAsync()
    {
        Debug.Log("Fetching data...");
        System.Threading.Tasks.Task fetchTask =
        Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(
            TimeSpan.Zero);
        return fetchTask.ContinueWithOnMainThread(FetchComplete);
    }



    private void FetchComplete(System.Threading.Tasks.Task fetchTask)
    {
        if (!fetchTask.IsCompleted)
        {
            Debug.LogError("Retrieval hasn't finished.");
            return;
        }

        var remoteConfig = FirebaseRemoteConfig.DefaultInstance;

        var info = remoteConfig.Info;

        if (info.LastFetchStatus != LastFetchStatus.Success)
        {
            Debug.LogError($"{nameof(FetchComplete)} was unsuccessful\n{nameof(info.LastFetchStatus)}: {info.LastFetchStatus}");

            if (!fetchData)
            {
                FetchDataAsync();
                fetchData = true;
            }
            else
            {
                print("Loading Default Ads!");
                AdsMediation.AdsMediationManager.instance.InitializeMediationAds(0);

            }

            return;
        }

        // Fetch successful. Parameter values must be activated to use.
        remoteConfig.ActivateAsync()
          .ContinueWithOnMainThread(
            task =>
            {
                Debug.Log($"Remote data loaded and ready for use. Last fetch time {info.FetchTime}.");

                OnDataFetched();
            });
    }

}
