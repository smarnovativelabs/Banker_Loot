using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The "Displaying ad in N" countdown of the gameplay HUD. The GameController raises AdsTimerUpdated with the seconds left
/// during the last seconds before an interstitial, and with -1 when nothing is coming; this panel only shows that value.
/// </summary>
public class AdsTimerDisplay : MonoBehaviour
{
    [Tooltip("The panel that is shown during the countdown (hidden otherwise)")]
    public GameObject panel;
    public Text label;
    [Tooltip("{0} is replaced by the seconds left")]
    public string format = "DISPLAYING AD IN {0}";

    GameController subscribed;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        // the GameController of the level may be created after the HUD
        var _controller = GameController.instance;
        if (_controller != subscribed)
        {
            if (subscribed != null) subscribed.AdsTimerUpdated -= OnTimer;
            subscribed = _controller;
            if (subscribed != null) subscribed.AdsTimerUpdated += OnTimer;
        }
    }

    void OnDestroy()
    {
        if (subscribed != null) subscribed.AdsTimerUpdated -= OnTimer;
    }

    void OnTimer(int _seconds)
    {
        bool _show = _seconds > 0;
        if (panel != null && panel.activeSelf != _show) panel.SetActive(_show);
        if (_show && label != null) label.text = string.Format(format, _seconds);
    }
}
