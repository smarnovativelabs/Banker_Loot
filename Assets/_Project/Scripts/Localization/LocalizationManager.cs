//using System.Collections;
//using System.Linq;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Networking;
//using UnityEngine.Events;

//public class LocalizationManager : MonoBehaviour
//{
//    public string localizationDataUrl;
//    public static LocalizationManager instance;
//    public List<string> languages;
//    public List<string> arabicStyleLanguages;
//    public Dictionary<string, LocalizedTexts> localizedTexts;
//    public delegate void UpdateLocalizedText();
//    public Dictionary<long, UnityAction> textUpdateCallbacks;
//    public UnityAction uiUpdateAction,fetchingFailedAction;
//    public int curSelectedLanguage;
//    string selectedLanguage;
//    public enum LocalizationDataStatus
//    {
//        waiting,
//        fetching,
//        fetched,
//        failed
//    }
//    LocalizationDataStatus dataStatus = LocalizationDataStatus.waiting;
//    long updateCallbackCounter;
//    bool isUpdatingTexts;

//    private void Awake()
//    {
//        if (instance == null)
//        {
//            instance = this;
//            DontDestroyOnLoad(gameObject);
//            languages = new List<string>();
//            arabicStyleLanguages = new List<string>();
//            localizedTexts = new Dictionary<string, LocalizedTexts>();
//            textUpdateCallbacks = new Dictionary<long, UnityAction>();
//            isUpdatingTexts = false;
//            dataStatus = LocalizationDataStatus.waiting;
//        }
//        else
//        {
//            Destroy(gameObject);
//        }
//    }
//    public  void InitializeLocalization()
//    {
//        try
//        {
//            if (dataStatus == LocalizationDataStatus.fetched || dataStatus == LocalizationDataStatus.fetching)
//                return;
//            StartCoroutine(GetLocalizationData());
//        }
//        catch (System.Exception ex)
//        {
//            Debug.LogError("Exception occured:" + ex);
//        }

//    }
//    public List<string> GetLanguagesList()
//    {
//        if (dataStatus != LocalizationDataStatus.fetched)
//        {
//            return null;
//        }
//        else
//        {
//            return languages;
//        }
//    }

//    IEnumerator GetLocalizationData()
//    {
//       /* dataStatus = LocalizationDataStatus.fetching;
//        if (SerializationManager.Load("Localization") != null)
//        {
//            SortLanguagesData((string)SerializationManager.Load("Localization"));
//        }*/
//        UnityWebRequest _req = UnityWebRequest.Get(localizationDataUrl);
//        _req.timeout = 60;
//        yield return _req.SendWebRequest();
//        if (string.IsNullOrEmpty(_req.error))
//        {
//            SortLanguagesData(_req.downloadHandler.text, dataStatus != LocalizationDataStatus.fetched);
//        }
//        else
//        {
//            if (dataStatus != LocalizationDataStatus.fetched)
//            {
//                dataStatus = LocalizationDataStatus.failed;
//                fetchingFailedAction?.Invoke();
//            }
//        }
//        //string[] _entries=_asset.text.Split()
//    }

//    void SortLanguagesData(string _data,bool _applyLanguages=true)
//    {
//        string[] _texts = _data.Split("\n");
//        if (_texts.Length < 1)
//        {
//            if (dataStatus != LocalizationDataStatus.fetched)
//            {
//                dataStatus = LocalizationDataStatus.failed;

//                fetchingFailedAction?.Invoke();

//            }
//            //No Localization added
//            return;
//        }
//        if (_texts.Length > 0)
//        {
//            languages = _texts[0].Split("\t").ToList();
//            for (int i = 0; i < languages.Count; i++)
//            {
//                string _language = languages[i];
//                if (_language.Contains(" -1"))
//                {
//                    _language = _language.Replace(" -1", "");
//                    arabicStyleLanguages.Add(_language);
//                    languages[i] = _language;
//                }
//            }
//        }
//        int _totalLanguages = languages.Count;
//        if (_texts.Length < 2)
//        {
//            if (dataStatus != LocalizationDataStatus.fetched)
//            {
//                fetchingFailedAction?.Invoke();
//                dataStatus = LocalizationDataStatus.failed;

//            }
//            return;
//        }
//        for (int i = 1; i < _texts.Length; i++)
//        {
//            string[] _localizedTexts = _texts[i].Split("\t");
//            if (_localizedTexts.Length <= 0)
//                continue;
//            string _key = _localizedTexts[0].ToLower();
//            if (localizedTexts.ContainsKey(_key))
//                continue;
//            LocalizedTexts _locals = new LocalizedTexts();

//            for (int j = 0; j < _totalLanguages; j++)
//            {
//                string _txt = j < _localizedTexts.Length ? _localizedTexts[j] : "";
//                _locals.texts.Add(_txt);
//            }
//            localizedTexts.Add(_key, _locals);
//        }
//        dataStatus = LocalizationDataStatus.fetched;
//        SerializationManager.Save(_data,"Localization");
//        if (_applyLanguages)
//            uiUpdateAction?.Invoke();
//    }

//    public void OnUpdateLocalization(int _selectedLanguage,bool _sceneUpdated=true)
//    {
//        if (isUpdatingTexts)
//            return;
//        if ((_selectedLanguage == curSelectedLanguage) && !_sceneUpdated)
//            return;
//        curSelectedLanguage = _selectedLanguage < languages.Count ? _selectedLanguage : curSelectedLanguage;
//        selectedLanguage = languages[curSelectedLanguage];
//        StartCoroutine(UpdateAllTexts());
//    }
//    IEnumerator UpdateAllTexts()
//    {
//        isUpdatingTexts = true;
//        while (dataStatus == LocalizationDataStatus.fetching)
//        {
//            yield return null;
//        }
//        if (dataStatus == LocalizationDataStatus.failed)
//        {
//            isUpdatingTexts = false;
//            yield break;
//        }
//        int _count = 0;
//        //Should Display Loading Canvas Here
//        GameManager.instance.EnableLoadingScreen(true, "Localizing Language");

//        while (_count < textUpdateCallbacks.Count)
//        {
//            KeyValuePair<long, UnityAction> _callback = textUpdateCallbacks.ElementAt(_count);
//            _callback.Value?.Invoke();
//            _count++;
//            if (_count % 20 == 0)
//                yield return new WaitForSeconds(Time.deltaTime * Time.timeScale);
//        }
//        GameManager.instance.EnableLoadingScreen(false);
//        isUpdatingTexts = false;
//    }
//    public bool IsLocalizationDataFethced()
//    {
//        return (dataStatus == LocalizationDataStatus.fetched);
//    }
//    public long RegisterCallback(UnityAction _callback)
//    {
//        updateCallbackCounter++;
//        textUpdateCallbacks.Add(updateCallbackCounter, _callback);
//        return updateCallbackCounter;
//    }
//    public void UnRegisterCallback(long _counter)
//    {
//        if (textUpdateCallbacks.ContainsKey(_counter))
//            textUpdateCallbacks.Remove(_counter);
//    }

//    public void RegisterUIUpdateCallback(UnityAction _action)
//    {
//        uiUpdateAction = _action;
//    }
//    public void RegisterFailedCallback(UnityAction _action)
//    {
//        fetchingFailedAction = _action;
//    }
//    public void UnRegisterFailedAction()
//    {
//        fetchingFailedAction = null;

//    }
//    public void UnRegisterUIUpdateCallback()
//    {
//        uiUpdateAction = null;
//    }
//    public string LocalizeText(string _textToUpdate)
//    {
//        string _localizedText = "";

//        if (localizedTexts.ContainsKey(_textToUpdate.ToLower()))
//        {
//            _localizedText = (localizedTexts[_textToUpdate.ToLower()].texts[curSelectedLanguage]);
//            //string _invertedText = "";
//            //for(int i = _localizedText.Length - 1; i >= 0; i--)
//            //{
//            //    _invertedText += _localizedText[i];
//            //}
//            //_localizedText = _invertedText;
//        }
//        return _localizedText;
//    }
//    public bool IsArabicStyleLanguage()
//    {
//        bool _isArabicStyled = false;
//        for(int i = 0; i < arabicStyleLanguages.Count; i++)
//        {
//            _isArabicStyled = selectedLanguage == arabicStyleLanguages[i];
//            if (_isArabicStyled)
//                break;
//        }
//        return _isArabicStyled;
//    }
//}
//[System.Serializable]
//public class LocalizedTexts
//{
//    public List<string> texts;
//    public LocalizedTexts()
//    {
//        texts = new List<string>();
//    }
//}

using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

public class LocalizationManager : MonoBehaviour
{
    public string localizationDataUrl;
    public static LocalizationManager instance;
    public List<string> languages;
    public List<string> arabicStyleLanguages;
    public Dictionary<string, LocalizedTexts> localizedTexts;
    public delegate void UpdateLocalizedText();
    public Dictionary<long, UnityAction> textUpdateCallbacks;
    public UnityAction uiUpdateAction, fetchingFailedAction;
    public int curSelectedLanguage;
    string selectedLanguage;
    public enum LocalizationDataStatus
    {
        waiting,
        fetching,
        fetched,
        failed
    }
    LocalizationDataStatus dataStatus = LocalizationDataStatus.waiting;
    long updateCallbackCounter;
    bool isUpdatingTexts;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            languages = new List<string>();
            arabicStyleLanguages = new List<string>();
            localizedTexts = new Dictionary<string, LocalizedTexts>();
            textUpdateCallbacks = new Dictionary<long, UnityAction>();
            isUpdatingTexts = false;
            dataStatus = LocalizationDataStatus.waiting;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void InitializeLocalization()
    {
        try
        {
            if (dataStatus == LocalizationDataStatus.fetched || dataStatus == LocalizationDataStatus.fetching)
                return;
            StartCoroutine(GetLocalizationData());
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception occured:" + ex);
        }

    }
    public List<string> GetLanguagesList()
    {
        if (dataStatus != LocalizationDataStatus.fetched)
        {
            return null;
        }
        else
        {
            return languages;
        }
    }

    IEnumerator GetLocalizationData()
    {
        /* dataStatus = LocalizationDataStatus.fetching;
         if (SerializationManager.Load("Localization") != null)
         {
             SortLanguagesData((string)SerializationManager.Load("Localization"));
         }*/
        UnityWebRequest _req = UnityWebRequest.Get(localizationDataUrl);
        _req.timeout = 60;
        yield return _req.SendWebRequest();
        if (string.IsNullOrEmpty(_req.error))
        {
            SortLanguagesData(_req.downloadHandler.text, dataStatus != LocalizationDataStatus.fetched);
        }
        else
        {
            Debug.Log("Error Here");
            if (dataStatus != LocalizationDataStatus.fetched)
            {
                Debug.Log("Error Here1");
                dataStatus = LocalizationDataStatus.failed;
                fetchingFailedAction?.Invoke();
            }
        }
        //string[] _entries=_asset.text.Split()
    }

    void SortLanguagesData(string _data, bool _applyLanguages = true)
    {
        string[] _texts = _data.Split("\n");
        if (_texts.Length < 1)
        {
            if (dataStatus != LocalizationDataStatus.fetched)
            {
                dataStatus = LocalizationDataStatus.failed;

                fetchingFailedAction?.Invoke();

            }
            //No Localization added
            return;
        }
        if (_texts.Length > 0)
        {
            languages = _texts[0].Split("\t").ToList();
            for (int i = 0; i < languages.Count; i++)
            {
                string _language = languages[i];
                if (_language.Contains(" -1"))
                {
                    _language = _language.Replace(" -1", "");
                    arabicStyleLanguages.Add(_language);
                    languages[i] = _language;
                }
            }
        }
        int _totalLanguages = languages.Count;
        if (_texts.Length < 2)
        {
            if (dataStatus != LocalizationDataStatus.fetched)
            {
                fetchingFailedAction?.Invoke();
                dataStatus = LocalizationDataStatus.failed;

            }
            return;
        }
        for (int i = 1; i < _texts.Length; i++)
        {
            string[] _localizedTexts = _texts[i].Split("\t");
            if (_localizedTexts.Length <= 0)
                continue;
            string _key = _localizedTexts[0].ToLower();
            if (localizedTexts.ContainsKey(_key))
                continue;
            LocalizedTexts _locals = new LocalizedTexts();

            for (int j = 0; j < _totalLanguages; j++)
            {
                string _txt = j < _localizedTexts.Length ? _localizedTexts[j] : "";
                _locals.texts.Add(_txt);
            }
            localizedTexts.Add(_key, _locals);
        }
        dataStatus = LocalizationDataStatus.fetched;
        SerializationManager.Save(_data, "Localization");
        if (_applyLanguages)
            uiUpdateAction?.Invoke();
    }

    public void OnUpdateLocalization(int _selectedLanguage, bool _sceneUpdated = true)
    {
        if (isUpdatingTexts)
            return;
        if ((_selectedLanguage == curSelectedLanguage) && !_sceneUpdated)
            return;
        curSelectedLanguage = _selectedLanguage < languages.Count ? _selectedLanguage : curSelectedLanguage;
        selectedLanguage = languages[curSelectedLanguage];
        StartCoroutine(UpdateAllTexts());
    }
    IEnumerator UpdateAllTexts()
    {
        isUpdatingTexts = true;
        while (dataStatus == LocalizationDataStatus.fetching)
        {
            yield return null;
        }
        if (dataStatus == LocalizationDataStatus.failed)
        {
            isUpdatingTexts = false;
            yield break;
        }
        int _count = 0;
        //Should Display Loading Canvas Here
        GameManager.instance.EnableLoadingScreen(true, "Localizing Language");

        while (_count < textUpdateCallbacks.Count)
        {
            KeyValuePair<long, UnityAction> _callback = textUpdateCallbacks.ElementAt(_count);
            _callback.Value?.Invoke();
            _count++;
            if (_count % 20 == 0)
                yield return new WaitForSeconds(Time.deltaTime * Time.timeScale);
        }
        GameManager.instance.EnableLoadingScreen(false);
        isUpdatingTexts = false;
    }
    public bool IsLocalizationDataFethced()
    {
        return (dataStatus == LocalizationDataStatus.fetched);
    }
    public long RegisterCallback(UnityAction _callback)
    {
        updateCallbackCounter++;
        textUpdateCallbacks.Add(updateCallbackCounter, _callback);
        return updateCallbackCounter;
    }
    public void UnRegisterCallback(long _counter)
    {
        if (textUpdateCallbacks.ContainsKey(_counter))
            textUpdateCallbacks.Remove(_counter);
    }

    public void RegisterUIUpdateCallback(UnityAction _action)
    {
        uiUpdateAction = _action;
    }
    public void RegisterFailedCallback(UnityAction _action)
    {
        fetchingFailedAction = _action;
    }
    public void UnRegisterFailedAction()
    {
        fetchingFailedAction = null;

    }
    public void UnRegisterUIUpdateCallback()
    {
        uiUpdateAction = null;
    }
    public string LocalizeText(string _textToUpdate)
    {
        //string _localizedText = "";

        //if (localizedTexts.ContainsKey(_textToUpdate.ToLower()))
        //{
        //    _localizedText = (localizedTexts[_textToUpdate.ToLower()].texts[curSelectedLanguage]);
        //    //string _invertedText = "";
        //    //for(int i = _localizedText.Length - 1; i >= 0; i--)
        //    //{
        //    //    _invertedText += _localizedText[i];
        //    //}
        //    //_localizedText = _invertedText;
        //}
        //return _localizedText;

        if (string.IsNullOrEmpty(_textToUpdate))
            return "";

        if (localizedTexts == null)
            return _textToUpdate;

        string key = _textToUpdate.ToLower();

        if (!localizedTexts.ContainsKey(key))
            return _textToUpdate;

        var entry = localizedTexts[key];
        if (entry == null || entry.texts == null)
            return _textToUpdate;

        if (curSelectedLanguage < 0 ||
            curSelectedLanguage >= entry.texts.Count)
            return entry.texts[0];

        return entry.texts[curSelectedLanguage];
    }
    public bool IsArabicStyleLanguage()
    {
        bool _isArabicStyled = false;
        for (int i = 0; i < arabicStyleLanguages.Count; i++)
        {
            _isArabicStyled = selectedLanguage == arabicStyleLanguages[i];
            if (_isArabicStyled)
                break;
        }
        return _isArabicStyled;
    }
}
[System.Serializable]
public class LocalizedTexts
{
    public List<string> texts;
    public LocalizedTexts()
    {
        texts = new List<string>();
    }
}