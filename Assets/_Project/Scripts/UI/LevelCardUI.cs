using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One card of the level select screen (the "Level Card" prefab). It only holds references to the parts of the card
/// and sets their text and colors for the state of the level, so the layout of the card can be changed freely in
/// the prefab: move, resize, restyle or replace the images and texts, just keep them assigned here.
/// </summary>
public class LevelCardUI : MonoBehaviour
{
    public enum State
    {
        Playable,
        Locked,
        ComingSoon
    }

    [Header("Parts")]
    public Image border;
    public Image background;
    public Image preview;
    public Text symbolText;
    public Text nameText;
    public Text statusText;
    [Tooltip("Shown when the level can be played")]
    public Button playButton;
    [Tooltip("Shown instead of the play button when the level cannot be played")]
    public GameObject lockedBox;

    [Header("Playable colors")]
    public Color openBorder = new Color(0.96f, 0.77f, 0.26f);
    public Color openBackground = new Color(0.12f, 0.31f, 0.55f);
    public Color openPreview = new Color(0.18f, 0.42f, 0.71f);
    public Color openSymbol = new Color(0.96f, 0.77f, 0.26f);
    public Color openName = Color.white;
    public Color openStatus = new Color(0.62f, 0.88f, 0.8f);

    [Header("Locked / coming soon colors")]
    public Color closedBorder = new Color(0.2f, 0.267f, 0.373f);
    public Color closedBackground = new Color(0.106f, 0.169f, 0.271f);
    public Color closedPreview = new Color(0.141f, 0.212f, 0.325f);
    public Color closedText = new Color(0.42f, 0.486f, 0.588f);

    [Header("Texts")]
    public string playableSymbol = "$";
    public string closedSymbol = "?";
    public string readyStatus = "Ready";
    public string completedStatus = "Completed";
    public string lockedStatus = "Locked";
    public string comingSoonStatus = "Coming soon";

    /// <summary>Fills the card. _onPlay is called when the play button of a playable card is pressed.</summary>
    public void Setup(int _number, State _state, bool _completed, Action _onPlay)
    {
        bool _open = _state == State.Playable;
        if (border != null) border.color = _open ? openBorder : closedBorder;
        if (background != null) background.color = _open ? openBackground : closedBackground;
        if (preview != null) preview.color = _open ? openPreview : closedPreview;
        if (symbolText != null)
        {
            symbolText.text = _open ? playableSymbol : closedSymbol;
            symbolText.color = _open ? openSymbol : closedText;
        }
        if (nameText != null)
        {
            nameText.text = "Level " + _number;
            nameText.color = _open ? openName : closedText;
        }
        if (statusText != null)
        {
            statusText.text = _open ? (_completed ? completedStatus : readyStatus)
                : (_state == State.Locked ? lockedStatus : comingSoonStatus);
            statusText.color = _open ? openStatus : closedText;
        }
        if (playButton != null)
        {
            playButton.gameObject.SetActive(_open);
            playButton.onClick.RemoveAllListeners();
            if (_open)
            {
                playButton.onClick.AddListener(() => _onPlay?.Invoke());
            }
        }
        if (lockedBox != null)
        {
            lockedBox.SetActive(!_open);
        }
    }
}
