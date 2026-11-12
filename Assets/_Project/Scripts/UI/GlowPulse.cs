using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a label glow: the outline of the text pulses between two colors while the text breathes slightly.
/// Uses unscaled time so it also runs while the game is paused.
/// </summary>
public class GlowPulse : MonoBehaviour
{
    public Text label;
    public UnityEngine.UI.Outline glow;
    public Color glowA = new Color(1f, 0.85f, 0.2f, 0.25f);
    public Color glowB = new Color(1f, 0.95f, 0.5f, 1f);
    public float speed = 3f;
    public float scaleAmount = 0.06f;

    Vector3 baseScale;

    void Awake()
    {
        if (label != null) baseScale = label.rectTransform.localScale;
    }

    void Update()
    {
        float _t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed);
        if (glow != null)
        {
            glow.effectColor = Color.Lerp(glowA, glowB, _t);
            glow.effectDistance = new Vector2(1f, -1f) * Mathf.Lerp(1.5f, 3.5f, _t);
        }
        if (label != null)
        {
            label.rectTransform.localScale = baseScale * (1f + scaleAmount * _t);
        }
    }
}
