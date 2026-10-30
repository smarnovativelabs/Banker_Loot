using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gameplay HUD: cash, blitz, level + xp bar, calendar (day / time), interact button, hover prompt,
/// instruction popup and tutorial quest text. Other systems talk to the HUD only through this class.
/// </summary>
public class UIController : MonoBehaviour
{
    public static UIController instance;

    [Header("-----------------------> Currency")]
    public Text CashText;
    public Text CashTextAnimation;
    public Text BlitzText;
    public Text BlitzTextAnimation;

    [Header("-----------------------> Level")]
    public Image xpBar;
    public Text XPText;
    public Text XPTextAnimation;
    public Text cargoLevelText;

    [Header("-----------------------> Calendar")]
    public Text dayDisplay;
    public Text timeDisplay;
    public string[] daysOfWeek = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    [Header("-----------------------> Interaction")]
    public GameObject pickButton;
    public Sprite[] interactButtons;
    public Sprite[] hoverInsImgs;
    [SerializeField] private Text hoverItemNameText;

    [Header("-----------------------> Pause")]
    public GameObject pausePanel;
    public AudioClip uiButtonSound;

    [Header("-----------------------> Saving Notification")]
    public GameObject savingNotification;
    public Text savingNotificationText;
    public Image savingNotificationImg;
    public Sprite[] savingNotificationImgs;

    [Header("-----------------------> Notification / Quest")]
    public GameObject notificationPopupPanel;
    public Text notificationText;
    public bool questDisplayed = false;

    bool isPlayingCurrencyAnim;
    bool isPlayingBlitzAnim;
    bool isPlayingXPAnim;
    bool notificationRoutineRunning;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        GameTutorialManager.QuestInstructionRequested += DisplayQuestInstruction;
        GameTutorialManager.QuestInstructionRemoved += RemoveQuestInstruction;
    }

    void OnDisable()
    {
        GameTutorialManager.QuestInstructionRequested -= DisplayQuestInstruction;
        GameTutorialManager.QuestInstructionRemoved -= RemoveQuestInstruction;
    }

    public IEnumerator InitializeUIController()
    {
        if (pickButton != null)
        {
            pickButton.SetActive(false);
        }
        if (hoverItemNameText != null)
        {
            hoverItemNameText.transform.parent.gameObject.SetActive(false);
        }
        if (notificationPopupPanel != null)
        {
            notificationPopupPanel.SetActive(false);
        }
        SetPlayerCurrency();
        BlitzText.text = PlayerDataManager.instance.playerData.playerBlitz.ToString();
        UpdateLevelAndXPBar();
        yield return null;
    }

    static void SetText(Text _text, string _value)
    {
        if (_text == null)
            return;
        var _localize = _text.GetComponent<LocalizeText>();
        if (_localize != null)
        {
            _localize.UpdateText(_value);
        }
        else
        {
            _text.text = _value;
        }
    }

    #region Saving Notification
    /// <summary>Slides the "Saving Progress" popup in / out.</summary>
    public void EnableSavingProgressNotification(bool _enable)
    {
        if (savingNotification == null)
            return;
        savingNotification.GetComponent<Animator>().SetBool("Display", _enable);
    }

    /// <param name="_typeIndex">0 = in progress (spinner), 1 = not saved, 2 = completed</param>
    public void UpdateSavingProgressText(string _msg, int _typeIndex)
    {
        if (savingNotificationText == null)
            return;
        SetText(savingNotificationText, _msg);
        if (savingNotificationImg != null && _typeIndex < savingNotificationImgs.Length)
        {
            savingNotificationImg.sprite = savingNotificationImgs[_typeIndex];
            savingNotificationImg.transform.GetChild(0).gameObject.SetActive(_typeIndex == 0);
        }
    }
    #endregion

    #region Pause
    public bool IsPaused
    {
        get { return pausePanel != null && pausePanel.activeSelf; }
    }

    void PlayButtonSound()
    {
        if (SoundController.instance != null && uiButtonSound != null)
        {
            SoundController.instance.OnPlayInteractionSound(uiButtonSound);
        }
    }

    public void OnPressPause()
    {
        PlayButtonSound();
        pausePanel.SetActive(true);
        Time.timeScale = 0;
        GameManager.instance.CallFireBase("OnPausePrs", "Setting", 1);
    }

    public void OnPressResume()
    {
        PlayButtonSound();
        pausePanel.SetActive(false);
        Time.timeScale = 1;
        var _ads = AdsMediation.AdsMediationManager.instance;
        if (_ads != null && _ads.CanShowInterstitial())
        {
            if (GameController.instance != null)
            {
                GameController.instance.ResetAdsTimer();
            }
            _ads.ShowInterstitial();
        }
        GameManager.instance.CallFireBase("OnResumePrs", "Setting", 1);
    }

    /// <summary>"Save Progress" button of the pause panel.</summary>
    public void SaveGameData()
    {
        GameController.instance.SaveData();
    }

    /// <summary>Opens the shared settings panel owned by GameManager (sound, music, language, quality).</summary>
    public void OnPressSetting()
    {
        PlayButtonSound();
        GameManager.instance.OnPressSettings();
    }

    /// <summary>"Main Menu" button: saves, then returns to the menu scene.</summary>
    public void OnPressHome()
    {
        Time.timeScale = 1;
        GameManager.instance.EnableLoadingScreen(true, "Saving Progress...");
        GameController.instance.SaveData();
        PlayButtonSound();
        GameManager.instance.CallFireBase("OnHomePrs", "EndPanel", 1);
        StartCoroutine(LoadHome());
    }

    IEnumerator LoadHome()
    {
        yield return null;
        while (GameController.instance.IsSavingInProgress())
        {
            yield return new WaitForSeconds(0.1f);
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    #endregion

    #region Calendar
    /// <param name="_time">minutes elapsed in the current day (0 - 24h)</param>
    /// <param name="_day">days passed since the game started</param>
    public void UpdateGameTime(float _time, int _day)
    {
        int _totalMinutes = Mathf.FloorToInt(_time);
        int _hours = _totalMinutes / 60;
        int _remMinutes = _totalMinutes % 60;

        if (timeDisplay != null)
        {
            timeDisplay.text = string.Format("{0:00}:{1:00} {2}", _hours % 12 == 0 ? 12 : _hours % 12, _remMinutes, _hours >= 12 ? "PM" : "AM");
        }
        if (dayDisplay != null)
        {
            string _dayName = daysOfWeek[_day % daysOfWeek.Length];
            dayDisplay.text = _dayName;
            SetText(dayDisplay, _dayName);
        }
    }
    #endregion

    #region Interaction
    public void DisplayHoverObjectName(string _name, bool _enableParent = true, HoverInstructionType _type = HoverInstructionType.Unknown)
    {
        if (hoverItemNameText == null)
            return;
        Transform _panel = hoverItemNameText.transform.parent;
        _panel.gameObject.SetActive(_enableParent);
        SetText(hoverItemNameText, _name);
        Transform _icon = _panel.GetChild(0);
        if (_type == HoverInstructionType.Unknown || (int)_type >= hoverInsImgs.Length)
        {
            _icon.gameObject.SetActive(false);
            return;
        }
        _icon.gameObject.SetActive(true);
        _icon.GetComponent<Image>().sprite = hoverInsImgs[(int)_type];
    }

    public void OnChangeInteraction(int _index, bool _enable)
    {
        if (pickButton == null)
            return;
        pickButton.SetActive(_enable);
        if (_enable && _index < interactButtons.Length)
        {
            pickButton.transform.GetChild(0).GetComponent<Image>().sprite = interactButtons[_index];
        }
    }

    /// <summary>Hooked to the interact button and to a quick screen tap (EnvInteract).</summary>
    public void OnInteractBtnPressed()
    {
        if (PlayerInteraction.instance != null)
        {
            PlayerInteraction.instance.OnInteract();
        }
    }

    public void DisplayInstructions(string _instruction)
    {
        EnablePopupNotification(_instruction, 3f);
    }

    public void EnablePopupNotification(string _msg, float _waitTime = 2f)
    {
        if (notificationPopupPanel == null || notificationRoutineRunning || questDisplayed)
            return;
        notificationPopupPanel.SetActive(true);
        SetText(notificationText, _msg);
        notificationRoutineRunning = true;
        StartCoroutine(DisableNotification(_waitTime));
    }

    IEnumerator DisableNotification(float _waitTime)
    {
        yield return new WaitForSeconds(_waitTime);
        if (!questDisplayed)
        {
            notificationPopupPanel.SetActive(false);
        }
        notificationRoutineRunning = false;
    }
    #endregion

    #region Currency
    public void SetPlayerCurrency()
    {
        CashText.text = "$" + PlayerDataManager.instance.playerData.playerCash;
    }

    /// <summary>Call after PlayerDataManager.UpdateCash to animate the change on the HUD.</summary>
    public void UpdateCurrency(int _amount)
    {
        CashTextAnimation.text = (_amount < 0 ? "-$" : "+$") + Math.Abs(_amount);
        if (isPlayingCurrencyAnim)
            return;
        isPlayingCurrencyAnim = true;
        StartCoroutine(AddCurrancy());
    }

    IEnumerator AddCurrancy()
    {
        yield return PlayAddAnimation(CashTextAnimation, () => SetPlayerCurrency());
        isPlayingCurrencyAnim = false;
    }

    /// <summary>Call after PlayerDataManager.UpdateBlitz to animate the change on the HUD.</summary>
    public void UpdateBlitz(int _amount)
    {
        BlitzTextAnimation.text = (_amount > 0 ? "+" : "") + _amount;
        if (isPlayingBlitzAnim)
            return;
        isPlayingBlitzAnim = true;
        StartCoroutine(AddBlitz());
    }

    IEnumerator AddBlitz()
    {
        yield return PlayAddAnimation(BlitzTextAnimation, () => BlitzText.text = PlayerDataManager.instance.playerData.playerBlitz.ToString());
        isPlayingBlitzAnim = false;
    }

    static IEnumerator PlayAddAnimation(Text _animText, Action _applyValue)
    {
        _animText.gameObject.SetActive(true);
        var _animator = _animText.GetComponent<Animator>();
        if (_animator != null)
        {
            _animator.SetInteger("fade", 1);
        }
        yield return new WaitForSeconds(1f);
        _applyValue();
        if (_animator != null)
        {
            _animator.SetInteger("fade", 0);
        }
        yield return null;
        _animText.gameObject.SetActive(false);
    }
    #endregion

    #region Level
    /// <summary>Call after PlayerDataManager.UpdateXP (LevelManager.AddXP does both).</summary>
    public void UpdateXP(int _amount)
    {
        XPTextAnimation.text = (_amount > 0 ? "+" : "") + _amount;
        if (isPlayingXPAnim)
            return;
        isPlayingXPAnim = true;
        StartCoroutine(AddXP());
    }

    IEnumerator AddXP()
    {
        yield return PlayAddAnimation(XPTextAnimation, () => { });
        isPlayingXPAnim = false;
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CheckForLevelUp();
        }
        else
        {
            UpdateLevelAndXPBar();
        }
    }

    /// <summary>Level text, xp bar fill and "current/required" xp text.</summary>
    public void UpdateLevelAndXPBar()
    {
        if (LevelManager.Instance != null)
        {
            xpBar.fillAmount = LevelManager.Instance.GetLevelBarFillValue();
            XPText.text = LevelManager.Instance.GetCurrentAndNextLevelRequiredXPS();
        }
        cargoLevelText.text = "LEVEL " + PlayerDataManager.instance.playerData.playerLevel;
    }
    #endregion

    #region Quest / Tutorial Instructions
    /// <summary>Tutorial instructions use the notification popup and stay until removed.</summary>
    public void DisplayQuestInstruction(string _instruction, string _heading, bool _show, bool _displayOnTop, bool _instantDisplay)
    {
        if (notificationPopupPanel == null)
            return;
        questDisplayed = true;
        notificationPopupPanel.SetActive(true);
        notificationText.text = string.IsNullOrEmpty(_heading) ? _instruction : _heading + "\n" + _instruction;
    }

    public void RemoveQuestInstruction()
    {
        questDisplayed = false;
        if (notificationPopupPanel != null && !notificationRoutineRunning)
        {
            notificationPopupPanel.SetActive(false);
        }
    }
    #endregion
}

[Serializable]
public enum HoverInstructionType
{
    Unknown = -1,
    General = 0,
    Warning = 1,
    Tools = 2,
    GameStore = 3
}
