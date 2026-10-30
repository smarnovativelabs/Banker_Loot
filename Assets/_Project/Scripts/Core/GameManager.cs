using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    
    public bool isDataLoaded = false;

    [Header("Setting Panel")]
    public GameObject SettingPanel;
    public GameObject languageLoadingTxt;
    public GameObject noInternetPanel;
    public GameObject selectLanguagePanel;
    public GameObject languageBtnCntnr;
    public GameObject languageBtnRef;

    public Dropdown availableLanguages;
    public Dropdown avlQualityStngs;

    public Sprite[] soundBtnSprites;
    public Sprite[] musicBtnSprites;
    public Sprite[] initialLnguageBtnImgs;
    public Color[] lngugBtnColors;
    public Image soundBtn;
    public Image musicBtn;
    public Slider sensitivitySlider;
    public Text versionText;

    [Header("Loading Objects")]
    public Sprite[] loadingImages;
    public string[] gameTips;
    public Image loadingPanel;
    public Text loadingIns;

    [Header("Sound")]
    public AudioClip uiButtonSound;

    public int soundCounter = 0;
    public int musicCounter = 0;
    public int selectedDeliveryMode = 0;
    public float inGameAdsTimer = 105f;
    public bool welcomeBackRewardDisplayed;
    /// <summary>
    public int minimumTheftAmount=50;
    public int theftPercent = 5;
    /// </summary>
    private void Awake()
    {
        InstanceCheck();
    
     
    }

    private void Start()
    {
        Debug.Log(Application.persistentDataPath);
        versionText.text = "v " + Application.version.ToString();
        SettingPanel.SetActive(false);
        soundCounter = PlayerPrefs.GetInt("Sound", 1);
        soundBtn.sprite = soundBtnSprites[soundCounter];
        musicCounter = PlayerPrefs.GetInt("Music", 1);
        sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 8);
        musicBtn.sprite = musicBtnSprites[musicCounter];
        SoundController.instance.OnMusicChanged(musicCounter);
        SoundController.instance.OnSoundChanged(soundCounter);
    }
    public IEnumerator InitializeGameData()
    {
      
            instance.EnableLoadingScreen(true);
            yield return new WaitForSeconds(0.1f);

            if (!isDataLoaded)
            {
                EnableLoadingScreen(true, "Loading Files");
                PlayerDataManager.instance.LoadPlayerData();
                isDataLoaded = true;
                yield return null;
                if (PlayerDataManager.instance.playerData.selectedQualitySettings < 1)
                {
                    DecideQualitySettings();
                    CallFireBase("AutoQlty_" + PlayerDataManager.instance.playerData.selectedQualitySettings.ToString());
                }
                SetGameQuality(PlayerDataManager.instance.playerData.selectedQualitySettings);
                avlQualityStngs.value = (PlayerDataManager.instance.playerData.selectedQualitySettings - 1);
                LocalizationManager.instance.RegisterUIUpdateCallback(SetLocalizationDropdown);
                LocalizationManager.instance.RegisterFailedCallback(OnLanguageFetchingFailed);
            }
            EnableLoadingScreen(true, "Loading Store Data");

            StoreManager.Instance.InitializeStore();
        yield return new WaitForSeconds(0.75f);

        if (!AdsMediation.AdsMediationManager.instance.IsAdsInitialized())
        {
            EnableLoadingScreen(true, "Loading Game Ads..");
            AdsMediation.AdsMediationManager.instance.InitializeMediationAds(0);
            yield return new WaitForSeconds(1f);
        }
        if (!FirebaseManager.instance.isInitialized)
        {
            EnableLoadingScreen(true, "Connecting With Remote..");
            FirebaseManager.instance.InitializeFirebase();
            yield return new WaitForSeconds(0.5f);
        }
        if (!LocalizationManager.instance.IsLocalizationDataFethced())
            {
                LocalizationManager.instance.InitializeLocalization();
            }
            else
            {
                yield return null;
                SetLocalizationDropdown();
            }
            yield return null;
            Welcomebackreward.instance.CheckForRewardPanel();
            yield return null;
            MainMenu.instance.InitializeMenu();

        
        
    }
    public void OnChangeQualityFromInitialPanel(int _val)
    {
        UpdateQualitySettings(_val);
        avlQualityStngs.value = _val;
    }
    public void UpdateQualitySettings(int _val)
    {
        PlayerDataManager.instance.playerData.selectedQualitySettings = _val + 1;
        SetGameQuality(_val + 1);
        CallFireBase("PlyrQlty_" + (_val + 1).ToString());
    }
    void DecideQualitySettings()
    {
        // Check device performance and set quality level accordingly
        //SystemInfo.processorFrequency < 2000 ||
        //SystemInfo.processorFrequency < 3000 ||
        //print("System Specifications------------------");
        //print(SystemInfo.systemMemorySize);
        //print(SystemInfo.graphicsMemorySize);
#if UNITY_IPHONE
        if (SystemInfo.systemMemorySize < 2000 || !IsGraphicsCapable())
        {
            PlayerDataManager.instance.playerData.selectedQualitySettings = 1;
            // Low-end device
        }
        else if (SystemInfo.systemMemorySize < 2700 || !IsGraphicsCapable())
        {
            // Mid-range device
            PlayerDataManager.instance.playerData.selectedQualitySettings = 2;
        }
        else
        {
            // High-end device
            PlayerDataManager.instance.playerData.selectedQualitySettings = 3;
        }
        return;
#endif


        if ( SystemInfo.systemMemorySize < 2000 || !IsGraphicsCapable())
        {
            PlayerDataManager.instance.playerData.selectedQualitySettings = 1;
            // Low-end device
        }
        else if ( SystemInfo.systemMemorySize < 4000 || !IsGraphicsCapable())
        {
            // Mid-range device
            PlayerDataManager.instance.playerData.selectedQualitySettings = 2;
        }
        else
        {
            // High-end device
            PlayerDataManager.instance.playerData.selectedQualitySettings = 3;
        }
    }

    bool IsGraphicsCapable()
    {
        // Check if the graphics card supports necessary features (e.g., shaders, textures)

#if UNITY_IPHONE
        if (SystemInfo.graphicsMemorySize <= 512)
        {
            // Low GPU memory (less than 512 MB)
            return false;
        }
        return true;

#endif

        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2)
        {
            // OpenGL ES 2.0 is typically used by low-end devices
            return false;
        }
        if (SystemInfo.graphicsMemorySize <= 512)
        {
            // Low GPU memory (less than 512 MB)
            return false;
        }
        //if (SystemInfo.graphicsShaderLevel < 30)
        //{
        //    // If the shader level is below 3.0 (directly associated with more modern GPUs)
        //    return false;
        //}

        // If all checks pass, the graphics are considered capable
        return true;
    }

    void SetGameQuality(int _val)
    {
        QualitySettings.SetQualityLevel(_val);  // 0 is typically the lowest quality
        Debug.Log("Low quality settings applied.");
        if (GameController.instance != null)
        {
            GameController.instance.UpdateGameQualitySettings(_val);
        }
        //// Optionally, you can set specific settings for low quality
        //QualitySettings.shadowCascades = 0;   // Disable shadows
        //QualitySettings.masterTextureLimit = 3; // Reduce texture resolution
        //QualitySettings.antiAliasing = 0; // Disable anti-aliasing
    }
    void InstanceCheck()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            loadingPanel.gameObject.SetActive(false);
        }
        else
        {
            AdsMediation.AdsMediationManager.instance.ShowInterstitial();
            Destroy(gameObject);
        }
    }

    #region Localization Implementation

    void SetLocalizationDropdown()
    {
        languageLoadingTxt.SetActive(false);
        availableLanguages.gameObject.SetActive(true);
        List<string> _languages = LocalizationManager.instance.GetLanguagesList();
        availableLanguages.options.Clear();
        availableLanguages.AddOptions(_languages);
        int _selectedLangug = PlayerDataManager.instance.playerData.selectedLanguage;
        _selectedLangug = _selectedLangug >= _languages.Count ? 0 : _selectedLangug;
        availableLanguages.value = _selectedLangug;
        availableLanguages.captionText.GetComponent<ArabicFix>().FixText(availableLanguages.options[_selectedLangug].text);
        PlayerDataManager.instance.playerData.selectedLanguage = _selectedLangug;

        SetLocalization(true);
        if (!PlayerDataManager.instance.playerData.initialLocalizationSet)
        {
            PlayerDataManager.instance.playerData.initialLocalizationSet = true;
            SetInitialLanguageSelectPanel(_languages);
        }
    }
    public void OnLanguageFetchingFailed()
    {
        noInternetPanel.SetActive(true);
    }
    public void OnContinueFailedFetching()
    {
        noInternetPanel.SetActive(false);
        CallFireBase("Lclztn_FlPnl");
    }
    public void OnContinueInitialLanguagePanel()
    {
        selectLanguagePanel.SetActive(false);
        SetLocalization(false);

        availableLanguages.value = PlayerDataManager.instance.playerData.selectedLanguage;
        Invoke("OnChangeLocalizationLanguage", 0.5f);
    }
    public void OnChangeLanguage(int _val)
    {
        if (_val == PlayerDataManager.instance.playerData.selectedLanguage)
            return;
        PlayerDataManager.instance.playerData.selectedLanguage = _val;
        SetLocalization(false);
        availableLanguages.captionText.GetComponent<ArabicFix>().FixText(availableLanguages.options[_val].text);
    }
    void SetLocalization(bool _sceneUpdated)
    {
        LocalizationManager.instance.OnUpdateLocalization(PlayerDataManager.instance.playerData.selectedLanguage, _sceneUpdated);
    }

    void SetInitialLanguageSelectPanel(List<string> _languages)
    {
        selectLanguagePanel.SetActive(true);
        for (int i = 0; i < _languages.Count; i++)
        {
            int _temp = i;
            
            GameObject _btn = Instantiate(languageBtnRef);
            _btn.transform.parent = languageBtnCntnr.transform;
            _btn.transform.GetChild(0).gameObject.GetComponent<LocalizeText>().UpdateText(_languages[i]);
            _btn.transform.localScale = new Vector3(1, 1, 1);
            _btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                for (int i = 0; i < languageBtnCntnr.transform.childCount; i++)
                {
                    languageBtnCntnr.transform.GetChild(i).transform.GetChild(0).GetComponent<Text>().color = lngugBtnColors[0];
                    languageBtnCntnr.transform.GetChild(i).GetComponent<Image>().sprite = initialLnguageBtnImgs[0];
                }
                SoundController.instance.OnPlayInteractionSound(uiButtonSound);
                PlayerDataManager.instance.playerData.selectedLanguage = _temp;
                languageBtnCntnr.transform.GetChild(_temp).transform.GetChild(0).GetComponent<Text>().color = lngugBtnColors[1];
                languageBtnCntnr.transform.GetChild(_temp).GetComponent<Image>().sprite = initialLnguageBtnImgs[1];
                //continue btn interactable
                selectLanguagePanel.transform.GetChild(0).transform.GetChild(3).GetComponent<Button>().interactable = true;
            });
            if (_temp == 0)
            {
                languageBtnCntnr.transform.GetChild(_temp).transform.GetChild(0).GetComponent<Text>().color = lngugBtnColors[1];
                languageBtnCntnr.transform.GetChild(_temp).GetComponent<Image>().sprite = initialLnguageBtnImgs[1];
                selectLanguagePanel.transform.GetChild(0).transform.GetChild(3).GetComponent<Button>().interactable = true;
            }
        }

        int _totalRows = Mathf.CeilToInt(((float)_languages.Count) / 3);
        int _containerSize = (int)(languageBtnCntnr.GetComponent<GridLayoutGroup>().cellSize.y + languageBtnCntnr.GetComponent<GridLayoutGroup>().spacing.y);
        _containerSize = _containerSize * _totalRows;
        _containerSize += (languageBtnCntnr.GetComponent<GridLayoutGroup>().padding.top);
        languageBtnCntnr.GetComponent<RectTransform>().sizeDelta = new Vector2(languageBtnCntnr.GetComponent<RectTransform>().sizeDelta.x, _containerSize);
    }

    public void OnChangeLocalizationLanguage()
    {
        if (LocalizationManager.instance.IsLocalizationDataFethced())
        {
            availableLanguages.captionText.GetComponent<ArabicFix>().FixText(availableLanguages.options[PlayerDataManager.instance.playerData.selectedLanguage].text);
        }        
    }

    #endregion
    #region Settings Panel Implemenatation
    /// <summary>
    /// Setting Panel is here because this panel is used in both scenes
    /// </summary>
    public void OnPressSettings()
    {
        versionText.text = "v " + Application.version.ToString();
        SettingPanel.gameObject.SetActive(true);
        SoundController.instance.OnPlayInteractionSound(uiButtonSound);

        CallFireBase("OnSettingPrs", "Setting", 1);
        if (AdsMediation.AdsMediationManager.instance.CanShowInterstitial())
        {
            if (GameController.instance != null)
            {
                GameController.instance.ResetAdsTimer();
            }
            AdsMediation.AdsMediationManager.instance.ShowInterstitial();
        }
       
        Invoke("OnChangeLocalizationLanguage", 0.1f);

    }
    public void OnPrivacyPress()
    {
        Application.OpenURL("http://perigames.com/privacy");
        SoundController.instance.OnPlayInteractionSound(uiButtonSound);
    }

    //Button Refrence
    public void OnCloseSetting()
    {
        SettingPanel.gameObject.SetActive(false);
        SoundController.instance.OnPlayInteractionSound(uiButtonSound);
        if (AdsMediation.AdsMediationManager.instance.CanShowInterstitial())
        {
            if (GameController.instance != null)
            {
                GameController.instance.ResetAdsTimer();
            }
        }
        AdsMediation.AdsMediationManager.instance.ShowInterstitial();

    }
    public void OnUpdateSlider()
    {
        PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider.value);
        if (GameController.instance != null)
        {
            GameController.instance.UpdatePlayerSensitivity(sensitivitySlider.value);
        }
    }
    //Button Refrence
    public void OnChangeSound()
    {
        soundCounter++;
        soundCounter = soundCounter % 2;
        soundBtn.sprite = soundBtnSprites[soundCounter];
        SoundController.instance.OnSoundChanged(soundCounter);
        SoundController.instance.OnPlayInteractionSound(uiButtonSound);
        if(GamePlaySound.instance!=null)
            GamePlaySound.instance.OnChangeMusicGamePlay();
    }

    //Button Refrence
    public void OnChangeMusic()
    {
        musicCounter++;
        musicCounter = musicCounter % 2;
        musicBtn.sprite = musicBtnSprites[musicCounter];
        SoundController.instance.OnMusicChanged(musicCounter);
        SoundController.instance.OnPlayInteractionSound(uiButtonSound);
        if (GamePlaySound.instance != null)
            GamePlaySound.instance.OnChangeMusicGamePlay();
    }
    public void OnRestorePressed()
    {
        if (StoreManager.Instance != null)
        {
            StoreManager.Instance.OnRequestRestore();
        }
    }
    #endregion

    public void EnableLoadingScreen(bool _enable,string _msg="")
    {
        loadingPanel.sprite = loadingImages[Random.Range(0, loadingImages.Length)];
        //loadingIns.gameObject.GetComponent<Text>().text = (_msg);
        loadingIns.gameObject.GetComponent<LocalizeText>().UpdateText(_msg);
        loadingPanel.gameObject.SetActive(_enable);
    }
    public void CallFireBase(string eventName, string paramName = "", int val = 1)
    {
        if (FirebaseManager.instance != null)
            FirebaseManager.instance.CallFirebasEvent(eventName);
    }
    public void UpdateInGameAdsTimer(long _val)
    {
        inGameAdsTimer = (float)_val;
    }
    public void UpdateDeliveryModeVal(long _val)
    {
      //  selectedDeliveryMode = (int)_val;
    }
    public void OnDiscordLinkPressed()
    {
        Application.OpenURL("https://discord.gg/tnpFCSY33M");
    }

}
[System.Serializable]
public enum PluginInitializationStatus
{
    Loading,
    Loaded,
    Failed
}