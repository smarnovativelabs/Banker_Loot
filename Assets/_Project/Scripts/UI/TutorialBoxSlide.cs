using System.Collections;
using UnityEngine;

/// <summary>
/// Slide in / slide out for the tutorial box (the side instruction container). The position the box has in the
/// editor is its resting position: it slides in from outside the screen to that position and slides back out
/// when it is hidden, so moving the box in the editor is all that is needed to place it.
/// Show() and Hide() are called by the UIController, Hide() turns the object off when the slide is finished.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TutorialBoxSlide : MonoBehaviour
{
    public enum Side
    {
        Right,
        Left,
        Top,
        Bottom
    }

    [Tooltip("The side of the screen the box slides in from and out to")]
    public Side slideFrom = Side.Right;
    [Tooltip("Extra distance outside the screen edge so the box is fully hidden, on top of its own size")]
    public float extraDistance = 60f;
    [Min(0.05f)] public float showDuration = 0.4f;
    [Min(0.05f)] public float hideDuration = 0.3f;
    [Tooltip("How far the box overshoots its resting position when it comes in (0 = no bounce)")]
    public float overshoot = 1.0f;

    RectTransform rect;
    Vector2 restPosition;
    bool restKnown;
    Coroutine routine;
    bool hiding;

    void Awake()
    {
        Cache();
    }

    void Cache()
    {
        if (restKnown)
            return;
        rect = (RectTransform)transform;
        restPosition = rect.anchoredPosition;
        restKnown = true;
    }

    /// <summary>The position the box moves to (the one set in the editor).</summary>
    public Vector2 RestPosition
    {
        get
        {
            Cache();
            return restPosition;
        }
    }

    Vector2 HiddenPosition()
    {
        // the box is anchored somewhere in the canvas: move it by its own size plus a margin in the slide direction
        var _size = rect.rect.size;
        float _distance;
        switch (slideFrom)
        {
            case Side.Left:
                _distance = -(_size.x + extraDistance + RestDistanceToEdge(Side.Left));
                return restPosition + new Vector2(_distance, 0f);
            case Side.Top:
                _distance = _size.y + extraDistance + RestDistanceToEdge(Side.Top);
                return restPosition + new Vector2(0f, _distance);
            case Side.Bottom:
                _distance = -(_size.y + extraDistance + RestDistanceToEdge(Side.Bottom));
                return restPosition + new Vector2(0f, _distance);
            default:
                _distance = _size.x + extraDistance + RestDistanceToEdge(Side.Right);
                return restPosition + new Vector2(_distance, 0f);
        }
    }

    // how far the resting box is from the screen edge it slides to, so it always leaves the screen completely
    float RestDistanceToEdge(Side _side)
    {
        var _parent = rect.parent as RectTransform;
        if (_parent == null)
            return 0f;
        var _corners = new Vector3[4];
        rect.GetWorldCorners(_corners);
        var _canvas = rect.GetComponentInParent<Canvas>();
        float _scale = _canvas != null ? Mathf.Max(0.01f, _canvas.scaleFactor) : 1f;
        float _screenW = Screen.width;
        float _screenH = Screen.height;
        switch (_side)
        {
            case Side.Left:
                return Mathf.Max(0f, _corners[0].x) / _scale;
            case Side.Top:
                return Mathf.Max(0f, _screenH - _corners[1].y) / _scale;
            case Side.Bottom:
                return Mathf.Max(0f, _corners[0].y) / _scale;
            default:
                return Mathf.Max(0f, _screenW - _corners[2].x) / _scale;
        }
    }

    void OnDisable()
    {
        // never leave the box off its resting position (prefab and editor), and stop any slide
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        hiding = false;
        if (restKnown && rect != null)
            rect.anchoredPosition = restPosition;
    }

    /// <summary>Turns the box on and slides it in (also from the middle of a slide out).</summary>
    public void Show()
    {
        Cache();
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        else if (!hiding && routine == null)
        {
            return; // already visible, only the text changed
        }
        if (routine != null)
            StopCoroutine(routine);
        hiding = false;
        Vector2 _from = rect.anchoredPosition;
        if (routine == null && Vector2.Distance(_from, restPosition) < 0.5f)
            _from = HiddenPosition();
        routine = StartCoroutine(Slide(_from, restPosition, showDuration, true));
    }

    /// <summary>Slides the box out and turns it off when it is gone.</summary>
    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }
        Cache();
        if (hiding)
            return;
        if (routine != null)
            StopCoroutine(routine);
        hiding = true;
        routine = StartCoroutine(Slide(rect.anchoredPosition, HiddenPosition(), hideDuration, false));
    }

    void OnEnable()
    {
        Cache();
        if (routine == null && !hiding)
        {
            // switched on by something else than Show(): slide in as well
            rect.anchoredPosition = HiddenPosition();
            routine = StartCoroutine(Slide(rect.anchoredPosition, restPosition, showDuration, true));
        }
    }

    IEnumerator Slide(Vector2 _from, Vector2 _to, float _duration, bool _entering)
    {
        float _time = 0f;
        rect.anchoredPosition = _from;
        while (_time < _duration)
        {
            _time += Time.unscaledDeltaTime;
            float _t = Mathf.Clamp01(_time / _duration);
            float _eased = _entering ? EaseOutBack(_t, overshoot) : _t * _t * _t;
            rect.anchoredPosition = Vector2.LerpUnclamped(_from, _to, _eased);
            yield return null;
        }
        rect.anchoredPosition = _to;
        routine = null;
        if (!_entering)
        {
            hiding = false;
            gameObject.SetActive(false);
        }
    }

    static float EaseOutBack(float _t, float _overshoot)
    {
        float _c1 = 1.70158f * Mathf.Max(0f, _overshoot);
        float _c3 = _c1 + 1f;
        return 1f + _c3 * Mathf.Pow(_t - 1f, 3f) + _c1 * Mathf.Pow(_t - 1f, 2f);
    }
}
