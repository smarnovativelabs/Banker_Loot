using Firebase;
using Firebase.Crashlytics;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager instance;
    public bool isInitialized = false;
    PluginInitializationStatus initializationStatus;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            initializationStatus = PluginInitializationStatus.Loading;
        }
        else
        {
            Destroy(gameObject);
        }

    }

    public void InitializeFirebase()
    {
        try
        {
            if (!instance.isInitialized)
            {
#if !UNITY_EDITOR
                Initialize();
#endif

            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception occured:" + ex);
        }
    }
    void Initialize()
    {
        Debug.Log("Starting Firebase Initialization...");
        // Disable Firebase debug logging
        Firebase.FirebaseApp.LogLevel = Firebase.LogLevel.Assert;
        // Run asynchronously without blocking the main thread
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Firebase dependencies failed: " + task.Exception);
                FailGracefully();
                return;
            }

            var dependencyStatus = task.Result;
            Debug.Log("Firebase Dependency Status: " + dependencyStatus);

            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebaseComponents();
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
            }
            else
            {
                Debug.LogError("Firebase dependencies are not met: " + dependencyStatus);
                FailGracefully();
            }
        });
    }

    private void InitializeFirebaseComponents()
    {
        Debug.Log("Firebase Initialized Successfully!");

        isInitialized = true;

        initializationStatus = PluginInitializationStatus.Loaded;

        //Calling FireBase DB and Auth

        //  FirebaseRealTimeDB.instance.InitializeDataBaseAndAuth();

        // Call Remote Config Initialization here
         Remoteconfig.instance.InitializeRemoteConfig();
    }

    private void FailGracefully()
    {
        Debug.LogWarning("Firebase Initialization Failed. Loading defaults...");
        initializationStatus = PluginInitializationStatus.Failed;

        // Proceed with the game (e.g., load defaults, disable Firebase-dependent features)
        GameManager.instance.EnableLoadingScreen(false);
    }
    public bool GetFirebaseResponse()
    {
#if !UNITY_EDITOR
            return (initializationStatus != PluginInitializationStatus.Loading);
#endif
        return true;

    }
    public void CallFirebasEvent(string eventName)
    {
        if (!isInitialized)
            return;

        string paramName = eventName + "_prm";

        int _paramValue = 1;

#if !UNITY_EDITOR
        Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, paramName, _paramValue);
#endif
    }
    public void CallAdImpressionEvent(string _eventName, Firebase.Analytics.Parameter[] _params)
    {
        if (!isInitialized)
            return;

        Firebase.Analytics.FirebaseAnalytics.LogEvent(_eventName, _params);
    }
}
