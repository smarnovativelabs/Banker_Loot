using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The level selection screen shown when Play is pressed in the main menu. It is a panel in the main menu scene
/// ("Level Select Panel" under the Main Menu canvas), so its look and layout are changed in the editor. The cards
/// are copies of the "Level Card" prefab, placed in the card container (give it any layout group you like).
/// A card is one of: playable (unlocked level), locked (a level that exists but is not unlocked yet) or
/// coming soon (no level for this slot yet).
/// </summary>
public class LevelSelectScreen : MonoBehaviour
{
    [Tooltip("The Level Card prefab, one copy is made per slot")]
    public LevelCardUI cardPrefab;
    [Tooltip("The parent of the cards, with a layout group (for example a horizontal or grid layout)")]
    public RectTransform cardContainer;
    public Button backButton;
    [Tooltip("Optional: the horizontal scroll view the cards are in (its content is the card container). The content is made at least as wide as the view and scrolled to the newest unlocked level.")]
    public ScrollRect scrollRect;
    [Tooltip("How many cards are shown: the levels that exist and then 'coming soon' cards up to this number")]
    [Min(1)] public int slotCount = 3;

    [Header("Continue / Restart")]
    [Tooltip("Popup shown when the chosen level has saved progress")]
    public GameObject choicePanel;
    public Text choiceTitle;
    public Button continueButton;
    public Button restartButton;
    [Tooltip("Closes the popup without starting the level")]
    public Button choiceCancelButton;

    Action<LevelData> onPlay;

    void Awake()
    {
        if (choiceCancelButton != null)
        {
            choiceCancelButton.onClick.AddListener(CloseChoice);
        }
        if (backButton != null)
        {
            backButton.onClick.AddListener(Hide);
        }
    }

    public void Show(List<LevelData> _levels, Action<LevelData> _onPlay)
    {
        onPlay = _onPlay;
        Fill(_levels);
        gameObject.SetActive(true);
        if (scrollRect != null)
        {
            StartCoroutine(FitScroll());
        }
    }

    /// <summary>
    /// Once the layout has run: the content is at least as wide as the view (so a few cards stay centered) and the
    /// view is scrolled so the newest unlocked level is in the middle.
    /// </summary>
    IEnumerator FitScroll()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        var _content = scrollRect.content;
        var _view = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        float _width = Mathf.Max(LayoutUtility.GetPreferredWidth(_content), _view.rect.width);
        _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

        float _scrollable = _width - _view.rect.width;
        if (_scrollable <= 0f)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
            yield break;
        }
        int _target = Mathf.Clamp(LevelProgress.UnlockedLevel, 1, Mathf.Max(1, _content.childCount)) - 1;
        var _card = _content.GetChild(Mathf.Min(_target, _content.childCount - 1)) as RectTransform;
        float _center = _card.localPosition.x - _content.rect.xMin;
        scrollRect.horizontalNormalizedPosition = Mathf.Clamp01((_center - _view.rect.width * 0.5f) / _scrollable);
    }

    public void Hide()
    {
        CloseChoice();
        gameObject.SetActive(false);
    }

    void Fill(List<LevelData> _levels)
    {
        if (cardPrefab == null || cardContainer == null)
        {
            Debug.LogError("LevelSelectScreen: assign the card prefab and the card container.");
            return;
        }
        for (int i = cardContainer.childCount - 1; i >= 0; i--)
        {
            var _old = cardContainer.GetChild(i).gameObject;
            _old.SetActive(false); // gone from the layout now, destroyed at the end of the frame
            Destroy(_old);
        }
        for (int i = 0; i < slotCount; i++)
        {
            int _number = i + 1;
            var _data = FindLevel(_levels, _number);
            var _card = Instantiate(cardPrefab, cardContainer);
            _card.name = "Level " + _number;
            var _state = _data == null ? LevelCardUI.State.ComingSoon
                : (LevelProgress.IsUnlocked(_number) ? LevelCardUI.State.Playable : LevelCardUI.State.Locked);
            bool _completed = _data != null && LevelProgress.IsCompleted(_number);
            _card.Setup(_number, _state, _completed, () => OnCardPressed(_data));
        }
    }

    /// <summary>A level with saved progress asks Continue or Restart first, a fresh level starts directly.</summary>
    void OnCardPressed(LevelData _data)
    {
        if (_data == null)
            return;
        if (choicePanel == null || continueButton == null || restartButton == null || !LevelProgress.HasProgress(_data.level))
        {
            onPlay?.Invoke(_data);
            return;
        }
        if (choiceTitle != null)
        {
            choiceTitle.text = "Level " + _data.level;
        }
        continueButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() =>
        {
            choicePanel.SetActive(false);
            onPlay?.Invoke(_data);
        });
        restartButton.onClick.AddListener(() =>
        {
            LevelProgress.ResetLevel(_data.level);
            choicePanel.SetActive(false);
            onPlay?.Invoke(_data);
        });
        choicePanel.SetActive(true);
    }

    void CloseChoice()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
    }

    static LevelData FindLevel(List<LevelData> _levels, int _number)
    {
        if (_levels == null)
            return null;
        for (int i = 0; i < _levels.Count; i++)
        {
            if (_levels[i] != null && _levels[i].level == _number && !string.IsNullOrEmpty(_levels[i].SceneName))
                return _levels[i];
        }
        return null;
    }
}
