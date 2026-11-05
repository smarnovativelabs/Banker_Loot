using System;
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
    [Tooltip("How many cards are shown: the levels that exist and then 'coming soon' cards up to this number")]
    [Min(1)] public int slotCount = 3;

    Action<LevelData> onPlay;

    void Awake()
    {
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
    }

    public void Hide()
    {
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
            _card.Setup(_number, _state, _completed, () => onPlay?.Invoke(_data));
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
