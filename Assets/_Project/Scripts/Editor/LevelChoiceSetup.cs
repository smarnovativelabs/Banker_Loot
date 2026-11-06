using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time setup of the Continue / Restart popup of the level select panel (main menu scene). Does nothing when
/// the popup already exists, so edits made by hand are never lost.
/// </summary>
public static class LevelChoiceSetup
{
    const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    const string PopupName = "Level Choice Popup";
    const string BodySprite = "Assets/_Project/Art/UI/Sprites UI/Level Complete/Layer 2.png";

    static readonly Color DarkBlue = new Color(0.04f, 0.14f, 0.38f);
    static readonly Color Green = new Color(0.25f, 0.7f, 0.12f);
    static readonly Color Gold = new Color(0.96f, 0.67f, 0.1f);

    [MenuItem("Banker Loot/Setup Level Choice Popup")]
    public static void Setup()
    {
        var _scene = EditorSceneManager.GetActiveScene();
        if (_scene.path != MenuScenePath)
        {
            _scene = EditorSceneManager.OpenScene(MenuScenePath);
        }
        var _screen = Object.FindFirstObjectByType<LevelSelectScreen>(FindObjectsInactive.Include);
        if (_screen == null)
        {
            Debug.LogError("Level choice setup: run 'Setup Level Select Panel' first, no LevelSelectScreen found.");
            return;
        }
        if (_screen.transform.Find(PopupName) != null)
        {
            Debug.Log("Level choice setup: '" + PopupName + "' already exists, nothing changed.");
            return;
        }

        var _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/UI/Fonts/BarlowCondensed-Black.ttf");
        var _body = AssetDatabase.LoadAssetAtPath<Sprite>(BodySprite);

        var _popup = NewImage(PopupName, _screen.transform, null, new Color(0f, 0f, 0f, 0.7f));
        Stretch(_popup.rectTransform);
        _popup.transform.SetAsLastSibling();

        var _window = NewImage("Window", _popup.transform, _body, Color.white);
        Place(_window.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 440f));

        var _title = NewText("Title", _window.transform, "Level 1", 46, DarkBlue, _font);
        Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(400f, 60f));
        var _info = NewText("Info", _window.transform, "You have progress in this level", 28, DarkBlue, _font);
        Place(_info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(440f, 40f));

        var _continue = NewButton("Continue Button", _window.transform, "CONTINUE", Green, _font);
        Place(_continue.targetGraphic.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 135f), new Vector2(340f, 70f));
        var _restart = NewButton("Restart Button", _window.transform, "RESTART", Gold, _font);
        Place(_restart.targetGraphic.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(340f, 70f));

        // tapping outside the window closes the popup
        var _cancel = _popup.gameObject.AddComponent<Button>();
        _cancel.targetGraphic = _popup;
        _cancel.transition = Selectable.Transition.None;

        _screen.choicePanel = _popup.gameObject;
        _screen.choiceTitle = _title;
        _screen.continueButton = _continue;
        _screen.restartButton = _restart;
        _screen.choiceCancelButton = _cancel;
        _popup.gameObject.SetActive(false);

        EditorUtility.SetDirty(_screen);
        EditorSceneManager.MarkSceneDirty(_scene);
        EditorSceneManager.SaveScene(_scene);
        Debug.Log("Level choice setup done: '" + PopupName + "' is under " + _screen.name);
    }

    static Image NewImage(string _name, Transform _parent, Sprite _sprite, Color _color)
    {
        var _object = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _object.transform.SetParent(_parent, false);
        var _image = _object.GetComponent<Image>();
        _image.sprite = _sprite;
        _image.color = _color;
        return _image;
    }

    static Text NewText(string _name, Transform _parent, string _value, int _size, Color _color, Font _font)
    {
        var _object = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        _object.transform.SetParent(_parent, false);
        var _text = _object.GetComponent<Text>();
        _text.text = _value;
        _text.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = _size;
        _text.color = _color;
        _text.alignment = TextAnchor.MiddleCenter;
        _text.horizontalOverflow = HorizontalWrapMode.Overflow;
        _text.raycastTarget = false;
        return _text;
    }

    static Button NewButton(string _name, Transform _parent, string _label, Color _color, Font _font)
    {
        var _image = NewImage(_name, _parent, null, _color);
        var _button = _image.gameObject.AddComponent<Button>();
        _button.targetGraphic = _image;
        var _text = NewText("Text", _image.transform, _label, 36, Color.white, _font);
        Stretch(_text.rectTransform);
        return _button;
    }

    static void Stretch(RectTransform _rect)
    {
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform _rect, Vector2 _anchor, Vector2 _position, Vector2 _size)
    {
        _rect.anchorMin = _anchor;
        _rect.anchorMax = _anchor;
        _rect.pivot = new Vector2(0.5f, _anchor.y);
        _rect.anchoredPosition = _position;
        _rect.sizeDelta = _size;
    }
}
