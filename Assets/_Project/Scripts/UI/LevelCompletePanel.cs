using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// "Level Completed" panel. Shown by the LockerManager when every locker of the level is complete.
/// Shows the cash reward (first completion only), a Next button when the following level exists
/// (otherwise only Home and Replay), Home and Replay.
/// </summary>
public class LevelCompletePanel : MonoBehaviour
{
    [Tooltip("The animated part of the panel (scaled in when it opens)")]
    public RectTransform window;
    public GameObject rewardRow;
    public Text rewardText;
    public Button nextButton;
    public Button replayButton;
    public Button homeButton;

    [Header("Timing")]
    [Tooltip("Seconds between the last locker completing and the panel opening (the locker door closes meanwhile)")]
    public float showDelay = 1.2f;
    public float popTime = 0.35f;

    LevelData nextLevel;
    bool shown;

    void Awake()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnPressNext);
        if (replayButton != null) replayButton.onClick.AddListener(OnPressReplay);
        if (homeButton != null) homeButton.onClick.AddListener(OnPressHome);
    }

    /// <param name="_firstTime">true when the level was never completed before: only then the reward is paid</param>
    public void Show(bool _firstTime)
    {
        if (shown)
            return;
        shown = true;
        StartCoroutine(ShowRoutine(_firstTime));
    }

    IEnumerator ShowRoutine(bool _firstTime)
    {
        yield return new WaitForSeconds(showDelay);

        var _data = NoteHeapManager.GetActiveLevelData();
        int _reward = _data != null ? _data.rewardCash : 0;
        bool _paid = _firstTime && _reward > 0;
        if (_paid)
        {
            PlayerDataManager.instance.UpdateCash(_reward);
            if (UIController.instance != null)
            {
                UIController.instance.SetPlayerCurrency();
            }
            if (GameController.instance != null)
            {
                GameController.instance.SaveData();
            }
        }
        if (rewardRow != null)
        {
            rewardRow.SetActive(_paid);
        }
        if (rewardText != null)
        {
            rewardText.text = "+" + _reward.ToString("N0");
        }

        nextLevel = FindNextLevel();
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(nextLevel != null);
        }

        gameObject.SetActive(true);
        Time.timeScale = 0f;
        if (window != null)
        {
            float _time = 0f;
            while (_time < popTime)
            {
                _time += Time.unscaledDeltaTime;
                float _t = Mathf.Clamp01(_time / popTime);
                // ease out with a little overshoot
                float _s = 1f + 2.70158f * Mathf.Pow(_t - 1f, 3f) + 1.70158f * Mathf.Pow(_t - 1f, 2f);
                window.localScale = Vector3.one * Mathf.Max(0f, _s);
                yield return null;
            }
            window.localScale = Vector3.one;
        }
    }

    /// <summary>The LevelData of the level after the active one, null when there is none (or it has no scene).</summary>
    static LevelData FindNextLevel()
    {
        var _heap = NoteHeapManager.instance;
        if (_heap == null)
            return null;
        int _next = NoteHeapManager.GetActiveLevel() + 1;
        for (int i = 0; i < _heap.levels.Count; i++)
        {
            var _data = _heap.levels[i];
            if (_data != null && _data.level == _next && !string.IsNullOrEmpty(_data.SceneName))
                return _data;
        }
        return null;
    }

    void OnPressNext()
    {
        if (nextLevel == null)
            return;
        Time.timeScale = 1f;
        LevelProgress.SelectLevel(nextLevel.level);
        LoadScene(nextLevel.SceneName);
    }

    /// <summary>Starts the level again: its saved lockers and note heap are deleted.</summary>
    void OnPressReplay()
    {
        Time.timeScale = 1f;
        // stop the autosave, so nothing writes the old level state again after it was deleted
        if (GameController.instance != null)
        {
            GameController.instance.StopAllCoroutines();
            GameController.instance.enabled = false;
        }
        var _lockers = FindFirstObjectByType<LockerManager>();
        if (_lockers != null) _lockers.ResetSave();
        if (NoteHeapManager.instance != null) NoteHeapManager.instance.ResetSave();
        LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnPressHome()
    {
        if (UIController.instance != null)
        {
            UIController.instance.OnPressHome();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }

    static void LoadScene(string _scene)
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.EnableLoadingScreen(true, "Please Wait...");
        }
        SceneManager.LoadScene(_scene);
    }
}
