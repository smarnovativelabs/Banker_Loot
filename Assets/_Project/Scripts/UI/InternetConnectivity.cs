using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Canvas))]
public class InternetConnectivity : MonoBehaviour
{
    public static InternetConnectivity instance;
    bool canCheckConnectivity = false;
    bool isRoutineRunning = false;
    bool InterNetAvailable;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            canCheckConnectivity = false;
            isRoutineRunning = false;
            DontDestroyOnLoad(gameObject);
            GetComponent<Canvas>().enabled = false;
            StartCoroutine(CheckInternetConnectivity());

        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool isInterNetAvailable()
    {
        return InterNetAvailable;
    }

    IEnumerator CheckInternetConnectivity()
    {
        isRoutineRunning = true;
        bool _isInternetAvailable = false;
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            UnityWebRequest request = new UnityWebRequest("https://www.google.com");
            request.timeout = 60;
            yield return request.SendWebRequest();
            if (string.IsNullOrEmpty(request.error))
            {
                _isInternetAvailable = true;
            }
        }
        if (!_isInternetAvailable)
        {
           // GameManager.instance.CallFireBase("NoInternet");
        }
        InterNetAvailable = _isInternetAvailable;
        if (canCheckConnectivity)
        {
            GetComponent<Canvas>().enabled = !_isInternetAvailable;
        }
        else
        {
            GetComponent<Canvas>().enabled = false;
        }
        yield return new WaitForSecondsRealtime(5f);
        StartCoroutine(CheckInternetConnectivity());

    }
    public void UpateConnectivityCheckStatus(int _val)
    {
        canCheckConnectivity = _val == 1;
        if (!canCheckConnectivity)
        {
            GetComponent<Canvas>().enabled = false;
        }
        else
        {
            if (!isRoutineRunning)
            {
                StartCoroutine(CheckInternetConnectivity());
            }
        }

    }

   
}
