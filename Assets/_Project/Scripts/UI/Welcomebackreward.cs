using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Welcomebackreward : MonoBehaviour
{
    public static Welcomebackreward instance;
    public int offlineRewardQuantity;
    public int welcomeBackRewardQuantity;
    public int adtoWatchWelcomeBack;
    public int adtoWatchOffline;
    public Image welcomeBacPanel;
    public Text welcomeBackPanelHeading;
    public Text welcomeBackPanelRewardQuantity;
    public Text adtoWatchQuantity;
    public GameObject closebutton;
    public int totalAdWatched;
    public RewardType rewardType;

    private void Awake()
    {
        instance = this;
    }
    public enum RewardType
    {
        None,
        WelcomeBack,
        Offline
    }
    /// <summary>
    /// Enable and set data on Reward panel
    /// </summary>
    public void ShowWelcomeRewardPanel()
    {
       // GameManager.instance.CallFireBase("welcomerewardshow");
        welcomeBacPanel.gameObject.SetActive(true);
        welcomeBackPanelHeading.GetComponent<LocalizeText>().UpdateText("Welcome Back");
        welcomeBackPanelRewardQuantity.text ="$" +welcomeBackRewardQuantity;
        adtoWatchQuantity.text= adtoWatchWelcomeBack.ToString();
        StartCoroutine(ShowCloseOption());
        GameManager.instance.welcomeBackRewardDisplayed = true;
    }
    /// <summary>
    /// Enable and set data on Reward panel
    /// </summary>
    /// ShowOfflineRewardPanel
    /// ShowWelcomeRewardPanel
    public void ShowOfflineRewardPanel()
    {
       // GameManager.instance.CallFireBase("offlinerewardshow");
        welcomeBacPanel.gameObject.SetActive(true);
        welcomeBackPanelHeading.GetComponent<LocalizeText>().UpdateText("Offline Reward");
        welcomeBackPanelRewardQuantity.text = "$" +offlineRewardQuantity;
        adtoWatchQuantity.text = adtoWatchOffline.ToString();
        StartCoroutine(ShowCloseOption());
        GameManager.instance.welcomeBackRewardDisplayed = true;
    }

    public void HideWelcomeBackPanel()
    {
        welcomeBacPanel.gameObject.SetActive(false);
        SaveUTC();

    }

    public void closeFirebaseWelcomebackPanel()
    {
        if (rewardType == RewardType.Offline)
        {
           // GameManager.instance.CallFireBase("offlinerewardpanelclosed");
        }
        else
        {
           // GameManager.instance.CallFireBase("welcomerewardpanelclosed");
        }
    }

    IEnumerator ShowCloseOption()
    {
        yield return new WaitForSeconds(2f);
        closebutton.gameObject.SetActive(true);

    }

    public void OnWatchAdClick()
    {
        if (rewardType == RewardType.Offline)
        {
            GameManager.instance.CallFireBase("watchadclickoffine");
        }
        else
        {
            GameManager.instance.CallFireBase("watchadclickwelcome");
        }
        AdsMediation.AdsMediationManager.instance.ShowRewardedVideo(OnWatchSuccessRV, OnWatchFailureRV);
    }

    public void OnWatchSuccessRV(string message)
    {
        totalAdWatched++;

        if (rewardType == RewardType.WelcomeBack)
        {
            if (totalAdWatched >= adtoWatchWelcomeBack)
            {
                PlayerDataManager.instance.UpdateCash(welcomeBackRewardQuantity);
                if (MainMenu.instance != null && MainMenu.instance.CashText != null)
                {
                    MainMenu.instance.CashText.text = PlayerDataManager.instance.playerData.playerCash.ToString();
                    MainMenu.instance.rvResponseContianer.SetActive(true);
                    MainMenu.instance.rvResponseContianer.transform.GetChild(0).GetComponent<LocalizeText>().UpdateText(message);
                    HideWelcomeBackPanel();
                    Invoke("DisableRvResponseText", 2f);
                }
               // MainMenu.instance.CashText.text = PlayerDataManager.instance.playerData.playerCash.ToString();
               
               

               // GameManager.instance.CallFireBase("claimedwelcomebackreward");
            }
            else
            {
                int required = adtoWatchWelcomeBack - totalAdWatched;
                if (adtoWatchQuantity != null)
                {
                    adtoWatchQuantity.text = required.ToString();
                }
            }
        }
        if (rewardType == RewardType.Offline)
        {
            if (totalAdWatched >= adtoWatchOffline)
            {
                PlayerDataManager.instance.UpdateCash(offlineRewardQuantity);
                if (MainMenu.instance != null && MainMenu.instance.CashText != null)
                {
                    MainMenu.instance.CashText.text = PlayerDataManager.instance.playerData.playerCash.ToString();
                    MainMenu.instance.rvResponseContianer.SetActive(true);
                    MainMenu.instance.rvResponseContianer.transform.GetChild(0).GetComponent<LocalizeText>().UpdateText(message);
                    HideWelcomeBackPanel();
                    Invoke("DisableRvResponseText", 2f);
                }
               // MainMenu.instance.CashText.text = PlayerDataManager.instance.playerData.playerCash.ToString();
               
               
                //GameManager.instance.CallFireBase("claimedofflinereward");
            }
            else
            {
                int required = adtoWatchOffline - totalAdWatched;
                // adtoWatchQuantity.text = required.ToString();
                if (adtoWatchQuantity != null)
                {
                    adtoWatchQuantity.text = required.ToString();
                }

            }
        }

    }

    public void OnWatchFailureRV(string message)
    {
        if (rewardType == RewardType.Offline)
        {
           // GameManager.instance.CallFireBase("failetowatchofflinereward");
        }
        else
        {
           // GameManager.instance.CallFireBase("failetowatchwelcomebackreward");
        }
        if(MainMenu.instance!=null)
        {
            MainMenu.instance.rvResponseContianer.SetActive(true);
            MainMenu.instance.rvResponseContianer.transform.GetChild(0).GetComponent<LocalizeText>().UpdateText(message);
            Invoke(nameof(DisableRvResponseText), 2f);
        }
       
    }

    void DisableRvResponseText()
    {
        if (MainMenu.instance != null)
        {
            MainMenu.instance.rvResponseContianer.SetActive(false);
        }
    }

    public  void CheckForRewardPanel()
    {
#if !UNITY_EDITOR
        if (GameManager.instance.welcomeBackRewardDisplayed) return;

        string utcBefore = PlayerDataManager.instance.playerData.utcforwelcomebackreward;
        DateTime utcNow = System.DateTime.Now;

        if (!string.IsNullOrEmpty(utcBefore))
        {
            long temp = Convert.ToInt64(utcBefore);
            DateTime oldTime = DateTime.FromBinary(temp);
            TimeSpan difference = utcNow.Subtract(oldTime);
            double timediffrenceseconds = difference.TotalSeconds;
            double timediffrencMinutes = difference.TotalMinutes;
            double timediffrencHours = difference.TotalHours;
            print("Time diffrence in Hours:" + timediffrencHours + "Hours");
            print("Time diffrence in Minutes:" + timediffrencMinutes + "Minutes");
            if (timediffrencHours >=6)
            {
                rewardType = RewardType.Offline;
                ShowOfflineRewardPanel();
                SaveUTC();
            }
            else if (timediffrencMinutes >= 5f)
            {
                rewardType = RewardType.WelcomeBack;
                ShowWelcomeRewardPanel();
                SaveUTC();
            }
        }
        else
        {
            SaveUTC();
        }

#endif
    }

    void SaveUTC()
    {
        if (PlayerDataManager.instance.playerData != null)
        {
            PlayerDataManager.instance.playerData.utcforwelcomebackreward = System.DateTime.Now.ToBinary().ToString();

        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveUTC();
        }
    }


}
