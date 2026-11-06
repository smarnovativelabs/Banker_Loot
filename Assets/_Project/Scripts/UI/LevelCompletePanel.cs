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

    [Header("Celebration")]
    [Tooltip("Played when the panel opens")]
    public AudioClip celebrationSound;
    [Tooltip("Confetti burst (a particle system prefab), spawned at the left and the right of the screen when the panel opens")]
    public GameObject confettiPrefab;
    [Tooltip("Distance in front of the camera")]
    public float confettiDistance = 4f;
    [Tooltip("Screen position of the left burst (0 = left edge, 1 = right edge); the right burst is mirrored")]
    [Range(0f, 0.5f)] public float confettiScreenX = 0.08f;
    [Tooltip("Screen height of the bursts (0 = bottom, 1 = top)")]
    [Range(0f, 1f)] public float confettiScreenY = 0.35f;
    public float confettiScale = 1.8f;

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
        // the panel object is inactive until it opens, so the always-active HUD runs the routine
        var _runner = UIController.instance != null ? (MonoBehaviour)UIController.instance : this;
        _runner.StartCoroutine(ShowRoutine(_firstTime));
    }

    IEnumerator ShowRoutine(bool _firstTime)
    {
        yield return new WaitForSeconds(showDelay);

        var _data = NoteHeapManager.GetActiveLevelData();
        int _reward = _data != null ? _data.rewardCash : 0;
        bool _paid = _reward > 0;
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
        PlayCelebration();
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

    /// <summary>The level complete sound and the confetti bursts.</summary>
    void PlayCelebration()
    {
        if (celebrationSound != null && SoundController.instance != null)
        {
            SoundController.instance.OnPlayInteractionSound(celebrationSound);
        }
        var _camera = Camera.main;
        if (confettiPrefab == null || _camera == null)
            return;
        for (int _side = 0; _side < 2; _side++)
        {
            var _burst = Instantiate(confettiPrefab, _camera.transform);
            float _x = _side == 0 ? confettiScreenX : 1f - confettiScreenX;
            _burst.transform.position = _camera.ViewportToWorldPoint(new Vector3(_x, confettiScreenY, confettiDistance));
            _burst.transform.localRotation = Quaternion.identity;
            _burst.transform.localScale = Vector3.one * confettiScale;
            // the game is paused while the panel is open, so the particles run on unscaled time
            foreach (var _system in _burst.GetComponentsInChildren<ParticleSystem>(true))
            {
                var _main = _system.main;
                _main.useUnscaledTime = true;
            }
            foreach (var _system in _burst.GetComponentsInChildren<ParticleSystem>(true))
            {
                _system.Play();
            }
            Destroy(_burst, 8f);
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
