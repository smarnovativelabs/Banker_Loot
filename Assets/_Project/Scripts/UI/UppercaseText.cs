using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the text of a Text in capital letters, whatever other scripts (localization, the main menu) write into it.
/// The text is changed after the other scripts ran, so nothing else needs to know about it.
/// </summary>
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Text))]
public class UppercaseText : MonoBehaviour
{
    Text label;

    void Awake()
    {
        label = GetComponent<Text>();
    }

    void OnEnable()
    {
        Apply();
    }

    void LateUpdate()
    {
        Apply();
    }

    void Apply()
    {
        if (label == null)
            label = GetComponent<Text>();
        if (label == null || string.IsNullOrEmpty(label.text))
            return;
        string _upper = label.text.ToUpperInvariant();
        if (_upper != label.text)
        {
            label.text = _upper;
        }
    }
}
