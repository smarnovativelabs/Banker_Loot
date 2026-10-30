using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CandyCoded.HapticFeedback;

public class MainMenu : MonoBehaviour
{
    public Text CashText;
    public Text rvRewardText;
    public Text qltyInsText;
    public GameObject qualityPanel;
    public Toggle[] qltyToggles;
    [Header("Sound")]
    public AudioClip uiButtonSound;
    public GameObject rvResponseContianer;
    [Header("UI Animations")]
    public GameObject areaLeft;
    public GameObject playButton;
    public GameObject rvButton;
    public GameObject cashContainer;
    public static MainMenu instance;
    private void Awake()
    {
        instance = this;

    }
    private void Start()
    {
        StartCoroutine(GameManager.instance.InitializeGameData());
    }

    public void InitializeMenu()
    {
        CashText.text = PlayerDataManager.instance.playerData.playerCash.ToString() + "$";
        rvResponseContianer.SetActive(false);
     //   rvRewardText.text = "+"+StoreManager.Instance.GetCashRVAmount().ToString();
        GameManager.instance.EnableLoadingScreen(false);
        qualityPanel.SetActive(false);
        if (!PlayerDataManager.instance.playerData.qualityPnlDisplayed)
        {
            DisplayInitialQualityPanel();
        }
        Invoke("AnimateUI", 0.3f);
        Invoke("AnimRVBtn", 0.6f);
        Invoke("CashCntrRV", 0.9f);
    }
    void CashCntrRV()
    {
        cashContainer.GetComponent<Animator>().enabled = true;
        areaLeft.GetComponent<Animator>().enabled = true;
    }
    void AnimRVBtn()
    {
        rvButton.GetComponent<Animator>().enabled = true;
    }
    void AnimateUI()
    {
       // areaLeft.GetComponent<Animator>().enabled = true;
        playButton.GetComponent<Animator>().enabled = true;
       
    }
    public void DisplayInitialQualityPanel()
    {
        PlayerDataManager.instance.playerData.qualityPnlDisplayed = true;
        qualityPanel.SetActive(true);
        qltyInsText.text = "We recommend the " + ((PlayerDataManager.instance.playerData.selectedQualitySettings == 1) ? "Low" :
            ((PlayerDataManager.instance.playerData.selectedQualitySettings == 2) ? "Medium" : "High")) + " quality settings for your device";

        for (int i = 0; i < qltyToggles.Length; i++)
        {
            qltyToggles[i].isOn = (i == (PlayerDataManager.instance.playerData.selectedQualitySettings - 1));
        }
    }
    public void OnCloseQltyPnael()
    {
        qualityPanel.SetActive(false);
        int _qltyIndex = PlayerDataManager.instance.playerData.selectedQualitySettings;
        for (int i = 0; i < qltyToggles.Length; i++)
        {
           if(qltyToggles[i].isOn)
            {
                _qltyIndex = i;
                break;
            }
        }
        if ((_qltyIndex+1) == PlayerDataManager.instance.playerData.selectedQualitySettings)
            return;
        GameManager.instance.OnChangeQualityFromInitialPanel(_qltyIndex);
    }
    public void OnDiscordLinkPressed()
    {
        GameManager.instance.OnDiscordLinkPressed();
    }
    private bool isStartingGame; // by ss
    public void OnPressPlay()
    {
        if (isStartingGame)
            return;

        isStartingGame = true;
        SoundController.instance.OnPlayInteractionSound(uiButtonSound);
        // GameManager.instance.EnableLoadingScreen(true,"Please Wait...");
        // SceneManager.LoadScene(1);
         GameManager.instance.CallFireBase("OnPlay", "Play", 1);
        //AdsMediation.AdsMediationManager.instance.ShowInterstitial();
        var adsManager = AdsMediation.AdsMediationManager.instance;

        if (adsManager != null && adsManager.CanShowInterstitial())
        {
            adsManager.InterstitialClosed = LoadGameScene;
            adsManager.ShowInterstitial();
        }
        else
        {
            LoadGameScene();
        }
    }
    private void LoadGameScene()
    {
        GameManager.instance?.EnableLoadingScreen(true, "Please Wait...");
        SceneManager.LoadScene(1);
    }
    public void OnPressSettingBtn()
    {
        GameManager.instance.OnPressSettings();
    }
    public void OnPressRV()
    {
        SoundController.instance.OnPlayInteractionSound(uiButtonSound);
        AdsMediation.AdsMediationManager.instance.ShowRewardedVideo(OnAdSuccess, OnAdFailed);
        GameManager.instance.CallFireBase("MainScrn_RV", "MainScrn_Rv", 1);
    }
    void OnAdSuccess(string _msg)
    {
        PlayerDataManager.instance.UpdateCash(StoreManager.Instance.GetCashRVAmount());
        if (CashText != null)
        {
            CashText.text = PlayerDataManager.instance.playerData.playerCash.ToString();
        }
        if (rvResponseContianer != null && rvResponseContianer.transform.childCount > 0)
        {
            rvResponseContianer.SetActive(true);

            Text text = rvResponseContianer.transform.GetChild(0).GetComponent<Text>();
            if (text != null)
            {
                rvResponseContianer.transform.GetChild(0).GetComponent<LocalizeText>().UpdateText(_msg);
                text.text = "Reward Granted";
            }
        }
       // rvResponseContianer.SetActive(true);
       // rvResponseContianer.transform.GetChild(0).GetComponent<LocalizeText>().UpdateText(_msg);
        //rvResponseContianer.transform.GetChild(0).GetComponent<Text>().text = "Reward Granted";
        Invoke("DisableRvResponseText", 2f);
        GameManager.instance.CallFireBase("successRVAd_main", "successRVAd", 1);
    }
    void OnAdFailed(string _msg)
    {
        if (rvResponseContianer != null && rvResponseContianer.transform.childCount > 0)
        {
            rvResponseContianer.SetActive(true);

            Text text = rvResponseContianer.transform.GetChild(0).GetComponent<Text>();
            if (text != null)
            {
                rvResponseContianer.transform.GetChild(0).GetComponent<LocalizeText>().UpdateText(_msg);
                text.text = _msg;
            }
        }
       // rvResponseContianer.SetActive(true);
       // rvResponseContianer.transform.GetChild(0).GetComponent<LocalizeText>().UpdateText(_msg);
       // rvResponseContianer.transform.GetChild(0).GetComponent<Text>().text = _msg;
        Invoke(nameof(DisableRvResponseText), 2f);
        GameManager.instance.CallFireBase("failRVAd_main", "failRVAd", 1);
    }
    public void DisplayResponseText(string _msg)
    {
        if (rvResponseContianer != null && rvResponseContianer.transform.childCount > 0)
        {
            rvResponseContianer.SetActive(true);

            Text text = rvResponseContianer.transform.GetChild(0).GetComponent<Text>();
            if (text != null)
            {
                rvResponseContianer.transform.GetChild(0).GetComponent<LocalizeText>().UpdateText(_msg);
                text.text = _msg;
            }
        }
       // rvResponseContianer.SetActive(true);
       // rvResponseContianer.transform.GetChild(0).GetComponent<LocalizeText>().UpdateText(_msg);
       // rvResponseContianer.transform.GetChild(0).GetComponent<Text>().text = _msg;
        Invoke("DisableRvResponseText", 2f);

    }
    void DisableRvResponseText()
    {
        if(this == null|| rvResponseContianer == null)
        {
            return;
        }
        if(instance!=null&&rvResponseContianer!=null)
        {
            rvResponseContianer.SetActive(false);
        }
    }

    public void LightVibration()
    {
        Debug.Log("Light Vibration");
        HapticFeedback.LightFeedback();
    }

    public void MediumVibration()
    {
        Debug.Log("Medium Vibration");
        HapticFeedback.MediumFeedback();
    }

    public void HardVibration()
    {
        Debug.Log("Hard Vibration");
        HapticFeedback.HeavyFeedback();
    }
    public void CrimeGameLink()
    {
        GameManager.instance.CallFireBase("CrimeAdClicked");
#if UNITY_EDITOR
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.peri.games.crime.scene.evidence.cleaner");
#elif UNITY_ANDROID
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.peri.games.crime.scene.evidence.cleaner");
#elif UNITY_IPHONE
        Application.OpenURL("https://apps.apple.com/us/app/crime-scene-cleaner-for-mafia/id6504433331");
#endif
    }
}
