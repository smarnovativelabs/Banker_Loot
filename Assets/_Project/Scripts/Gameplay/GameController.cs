using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gameplay scene root. Same pattern as the food game: it initializes the scene, owns the
/// saving actions (auto save + save on pause), the in-game interstitial timer and firebase quarters.
/// Add new gameplay systems by registering a saving action and starting them from InitializeGame.
/// </summary>
public class GameController : MonoBehaviour
{
    public static GameController instance;

    public Camera gameMainCamera;
    public float autoSaveTimeInterval = 180f;
    [Tooltip("Real minutes one in-game day lasts (the HUD calendar shows 24h of game time per day)")]
    public float dayCycleMinutes = 4f;
    public GameData gameData;
    public bool gameStarted = false;

    public delegate void ChangeGameStatus(bool _enable);
    public ChangeGameStatus changeGameStatus;

    /// <summary>Seconds left before the next in-game interstitial, -1 when no countdown is running.</summary>
    public event Action<int> AdsTimerUpdated;
    /// <summary>Raised when a save starts (true) and when it finishes (false).</summary>
    public event Action<bool> SavingStateChanged;
    /// <summary>Raised with the new day number when the in-game calendar moves to the next day.</summary>
    public event Action<int> DayChanged;

    List<UnityAction> savingActions;
    float adsTimer;
    float firebaseTimer;
    float autoSaveTimer;
    int firebaseQuarter;
    bool isDataInitialized;
    bool isSavingData;

    void Awake()
    {
#if UNITY_EDITOR
        // persistent managers (GameManager, PlayerDataManager, ...) are created by the main menu scene,
        // so always boot through it when this scene is played directly from the editor
        if (GameManager.instance == null)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            return;
        }
#endif
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (GameManager.instance == null)
            return;
        Time.timeScale = 1;
        StartCoroutine(InitializeGame());
        if (AdsMediation.AdsMediationManager.instance != null)
        {
            AdsMediation.AdsMediationManager.instance.HideBannerAdd();
        }
    }

    IEnumerator InitializeGame()
    {
        changeGameStatus += UpdateGameStatus;
        if (PlayerInteraction.instance != null)
        {
            changeGameStatus += PlayerInteraction.instance.StartGame;
        }
        savingActions = new List<UnityAction>();
        AddSavingAction(SaveGameData);
        AddSavingAction(PlayerDataManager.instance.SavePlayerData);

        GameManager.instance.EnableLoadingScreen(true, "Loading Game Data");
        yield return null;

        gameData = (GameData)SerializationManager.LoadFile("_gameData");
        if (gameData == null)
        {
            gameData = new GameData();
        }
        yield return null;

        // tutorial loads its saved progress and registers its own saving action
        if (GameTutorialManager.instance != null)
        {
            yield return GameTutorialManager.instance.InitializeTutorial();
        }

        // register new gameplay systems here (each one: load its data, then yield)

        GameManager.instance.EnableLoadingScreen(true, "Setting Up Game");
        if (UIController.instance != null)
        {
            yield return UIController.instance.InitializeUIController();
            UIController.instance.UpdateGameTime(gameData.currentTime, gameData.currentDay);
        }
        UpdateGameQualitySettings(PlayerDataManager.instance.playerData.selectedQualitySettings);
        UpdatePlayerSensitivity(PlayerPrefs.GetFloat("Sensitivity", 8));
        yield return StoreManager.Instance.CheckForPendingPurchases();
        yield return new WaitForSeconds(0.1f);

        isDataInitialized = true;
        GameManager.instance.EnableLoadingScreen(false);
        changeGameStatus?.Invoke(true);

        if (GameTutorialManager.instance != null)
        {
            GameTutorialManager.instance.CheckToDisplayTutorial();
        }
    }

    public void UpdatePlayerSensitivity(float _val)
    {
        if (Controlsmanager.instance != null)
        {
            Controlsmanager.instance.UpdateSensitivity(_val);
        }
    }

    public void UpdateGameQualitySettings(int _val)
    {
        if (gameMainCamera == null)
            return;
        switch (_val)
        {
            case 1:
                gameMainCamera.fieldOfView = 45;
                gameMainCamera.farClipPlane = 60;
                break;
            case 2:
                gameMainCamera.fieldOfView = 55;
                gameMainCamera.farClipPlane = 75;
                break;
            case 3:
                gameMainCamera.fieldOfView = 60;
                gameMainCamera.farClipPlane = 300;
                break;
        }
    }

    void UpdateGameStatus(bool _started)
    {
        gameStarted = _started;
    }

    void Update()
    {
        if (gameStarted)
        {
            UpdateGameTime();
            UpdateAdsTimer();
            UpdateFirebaseQuarter();
            UpdateAutoSavingTimer();
        }
    }

    #region Calendar
    /// <summary>
    /// gameData.currentTime counts game minutes of the current day (0 - 1440) and is saved with the game.
    /// One day (1440 game minutes) lasts dayCycleMinutes real minutes.
    /// </summary>
    void UpdateGameTime()
    {
        const float minutesPerDay = 24f * 60f;
        gameData.currentTime += Time.deltaTime * (minutesPerDay / (dayCycleMinutes * 60f));
        if (gameData.currentTime >= minutesPerDay)
        {
            gameData.currentTime -= minutesPerDay;
            gameData.currentDay++;
            DayChanged?.Invoke(gameData.currentDay);
        }
        if (UIController.instance != null)
        {
            UIController.instance.UpdateGameTime(gameData.currentTime, gameData.currentDay);
        }
    }
    #endregion

    #region Ads Timer
    void UpdateAdsTimer()
    {
        var _ads = AdsMediation.AdsMediationManager.instance;
        if (_ads == null)
            return;

        int _difference = Mathf.CeilToInt(GameManager.instance.inGameAdsTimer - adsTimer);
        if (_difference > 5)
        {
            adsTimer += Time.deltaTime;
            AdsTimerUpdated?.Invoke(-1);
        }
        else if (_ads.CanShowInterstitial())
        {
            adsTimer += Time.deltaTime;
            AdsTimerUpdated?.Invoke(_difference);
            if (_difference < 1)
            {
                _ads.ShowInterstitial();
                adsTimer = 0f;
            }
        }
        else
        {
            AdsTimerUpdated?.Invoke(-1);
        }
    }

    public void ResetAdsTimer()
    {
        adsTimer = 0f;
        AdsTimerUpdated?.Invoke(-1);
    }
    #endregion

    void UpdateFirebaseQuarter()
    {
        firebaseTimer += Time.deltaTime;
        if (firebaseTimer >= 240f)
        {
            firebaseTimer = 0f;
            firebaseQuarter++;
            GameManager.instance.CallFireBase("Q_" + firebaseQuarter.ToString(), "quarter", firebaseQuarter);
        }
    }

    #region Saving Implementation
    void UpdateAutoSavingTimer()
    {
        if (isSavingData)
            return;

        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveTimeInterval)
        {
            autoSaveTimer = 0;
            SaveData(false);
        }
    }

    public void AddSavingAction(UnityAction _action)
    {
        savingActions.Add(_action);
    }

    public void SaveData(bool _SavingOnLeaving = false)
    {
        if (!isDataInitialized)
            return;

        if (_SavingOnLeaving)
        {
            for (int i = 0; i < savingActions.Count; i++)
            {
                savingActions[i].Invoke();
            }
            autoSaveTimer = 0f;
            return;
        }
        if (isSavingData)
            return;

        isSavingData = true;
        StartCoroutine(SavePeriodicData());
    }

    IEnumerator SavePeriodicData()
    {
        SavingStateChanged?.Invoke(true);
        for (int i = 0; i < savingActions.Count; i++)
        {
            savingActions[i].Invoke();
            if (i % 5 == 4)
            {
                yield return null;
            }
        }
        yield return new WaitForSecondsRealtime(0.5f);
        SavingStateChanged?.Invoke(false);

        autoSaveTimer = 0f;
        isSavingData = false;
    }

    public bool IsSavingInProgress()
    {
        return isSavingData;
    }

    void SaveGameData()
    {
        if (!isDataInitialized)
            return;
        if (gameData != null)
        {
            SerializationManager.Save(gameData, "_gameData");
        }
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveData(true);
        }
    }
    #endregion
}

[Serializable]
public class GameData
{
    public int currentLevel;
    public int tutorialBuild;
    public int currentDay;
    public float currentTime = 9f * 60f;
    public GameData()
    {
        currentLevel = 1;
        currentDay = 0;
        currentTime = 9f * 60f; // new games start at 9:00 AM
    }
}
