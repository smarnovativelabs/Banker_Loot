using UnityEngine;

/// <summary>
/// Keeps a panel completely inside its parent (the safe area): when the parent is smaller than the panel, the panel is
/// scaled down (never up), so nothing is cut at any screen size or aspect ratio.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FitToParent : MonoBehaviour
{
    [Tooltip("Free space kept between the panel and the edge of the parent (pixels of the parent)")]
    public float margin = 16f;
    [Tooltip("Fraction of the panel rectangle that is really drawn (use 1 when the rectangle is the drawn size)")]
    public Vector2 visibleFraction = new Vector2(1f, 1f);

    RectTransform rect;
    Vector2 lastParentSize = new Vector2(-1f, -1f);
    Vector2 lastSize = new Vector2(-1f, -1f);

    void OnEnable()
    {
        Fit(true);
    }

    void LateUpdate()
    {
        Fit(false);
    }

    void Fit(bool force)
    {
        if (rect == null)
            rect = (RectTransform)transform;
        var _parent = rect.parent as RectTransform;
        if (_parent == null)
            return;

        Vector2 _parentSize = _parent.rect.size;
        Vector2 _size = rect.rect.size;
        if (!force && _parentSize == lastParentSize && _size == lastSize)
            return;
        lastParentSize = _parentSize;
        lastSize = _size;

        float _w = Mathf.Max(1f, _size.x * visibleFraction.x);
        float _h = Mathf.Max(1f, _size.y * visibleFraction.y);
        float _scale = Mathf.Min(1f, (_parentSize.x - margin * 2f) / _w, (_parentSize.y - margin * 2f) / _h);
        _scale = Mathf.Max(0.2f, _scale);
        rect.localScale = new Vector3(_scale, _scale, 1f);
    }
}
