using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time setup of the level selection screen: makes the "Level Card" prefab and the "Level Select Panel" in the
/// main menu scene (under the Main Menu canvas) and assigns it to the MainMenu. After that everything is a normal
/// scene object / prefab to restyle by hand. Does nothing when the panel already exists, so edits are never lost.
/// </summary>
public static class LevelSelectSetup
{
    const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    const string PrefabFolder = "Assets/_Project/Prefabs/UI";
    const string PrefabPath = PrefabFolder + "/Level Card.prefab";
    const string PanelName = "Level Select Panel";

    static readonly Color Backdrop = new Color(0.078f, 0.137f, 0.231f, 1f);
    static readonly Color Gold = new Color(0.96f, 0.77f, 0.26f);
    static readonly Color DarkGold = new Color(0.255f, 0.141f, 0.008f);
    static readonly Color ClosedBorder = new Color(0.2f, 0.267f, 0.373f);
    static readonly Color ClosedPreview = new Color(0.141f, 0.212f, 0.325f);
    static readonly Color ClosedText = new Color(0.42f, 0.486f, 0.588f);

    static Font font;

    [MenuItem("Banker Loot/Setup Level Select Panel")]
    public static void Setup()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var _scene = EditorSceneManager.GetActiveScene();
        if (_scene.path != MenuScenePath)
        {
            _scene = EditorSceneManager.OpenScene(MenuScenePath);
        }
        var _menu = Object.FindFirstObjectByType<MainMenu>();
        if (_menu == null || _menu.playButton == null)
        {
            Debug.LogError("Level select setup: the MainMenu (with its play button) was not found in " + MenuScenePath);
            return;
        }
        var _canvas = _menu.playButton.GetComponentInParent<Canvas>();
        if (_canvas.transform.Find(PanelName) != null)
        {
            Debug.Log("Level select setup: '" + PanelName + "' already exists, nothing changed.");
            return;
        }

        var _card = CreateCardPrefab();
        CreatePanel(_canvas.transform, _card, _menu);
        EditorSceneManager.MarkSceneDirty(_scene);
        EditorSceneManager.SaveScene(_scene);
        Debug.Log("Level select setup done: '" + PanelName + "' is under " + _canvas.name + ", the card is " + PrefabPath);
    }

    #region Card prefab
    static LevelCardUI CreateCardPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");
        }

        var _root = NewImage("Level Card", null, ClosedBorder);
        _root.rectTransform.sizeDelta = new Vector2(300f, 400f);
        var _ui = _root.gameObject.AddComponent<LevelCardUI>();
        _ui.border = _root;

        _ui.background = NewImage("Background", _root.transform, ClosedBorder);
        Stretch(_ui.background.rectTransform, 4f);

        _ui.preview = NewImage("Preview", _ui.background.transform, ClosedPreview);
        Place(_ui.preview.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(260f, 170f));
        _ui.symbolText = NewText("Symbol", _ui.preview.transform, "$", 96, Gold);
        Stretch(_ui.symbolText.rectTransform);

        _ui.nameText = NewText("Name", _ui.background.transform, "Level 1", 34, Color.white);
        Place(_ui.nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -215f), new Vector2(260f, 44f));
        _ui.statusText = NewText("Status", _ui.background.transform, "Ready", 22, ClosedText);
        Place(_ui.statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -258f), new Vector2(260f, 32f));

        _ui.playButton = NewButton("Play Button", _ui.background.transform, "PLAY", Gold, DarkGold, 28);
        Place(_ui.playButton.targetGraphic.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(220f, 64f));

        var _locked = NewImage("Locked Box", _ui.background.transform, ClosedPreview);
        Place(_locked.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(220f, 64f));
        var _lockedText = NewText("Text", _locked.transform, "LOCKED", 24, ClosedText);
        Stretch(_lockedText.rectTransform);
        _ui.lockedBox = _locked.gameObject;

        var _asset = PrefabUtility.SaveAsPrefabAsset(_root.gameObject, PrefabPath);
        Object.DestroyImmediate(_root.gameObject);
        return _asset.GetComponent<LevelCardUI>();
    }
    #endregion

    #region Panel
    static void CreatePanel(Transform _parent, LevelCardUI _card, MainMenu _menu)
    {
        var _panel = NewImage(PanelName, _parent, Backdrop);
        Stretch(_panel.rectTransform);
        _panel.transform.SetAsLastSibling();

        var _title = NewText("Title", _panel.transform, "SELECT LEVEL", 54, Gold);
        Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 90f));

        var _row = new GameObject("Card Container", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        _row.transform.SetParent(_panel.transform, false);
        var _rowRect = _row.GetComponent<RectTransform>();
        Place(_rowRect, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1000f, 420f));
        var _layout = _row.GetComponent<HorizontalLayoutGroup>();
        _layout.spacing = 40f;
        _layout.childAlignment = TextAnchor.MiddleCenter;
        _layout.childControlWidth = false;
        _layout.childControlHeight = false;
        _layout.childForceExpandWidth = false;
        _layout.childForceExpandHeight = false;

        var _back = NewButton("Back Button", _panel.transform, "BACK", ClosedBorder, Color.white, 26);
        Place(_back.targetGraphic.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(220f, 64f));

        var _screen = _panel.gameObject.AddComponent<LevelSelectScreen>();
        _screen.cardPrefab = _card;
        _screen.cardContainer = _rowRect;
        _screen.backButton = _back;

        _menu.levelScreen = _screen;
        EditorUtility.SetDirty(_menu);
        _panel.gameObject.SetActive(false);
    }
    #endregion

    #region Helpers
    static Image NewImage(string _name, Transform _parent, Color _color)
    {
        var _go = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (_parent != null)
        {
            _go.transform.SetParent(_parent, false);
        }
        var _image = _go.GetComponent<Image>();
        _image.color = _color;
        return _image;
    }

    static Text NewText(string _name, Transform _parent, string _text, int _size, Color _color)
    {
        var _go = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        _go.transform.SetParent(_parent, false);
        var _label = _go.GetComponent<Text>();
        _label.font = font;
        _label.text = _text;
        _label.fontSize = _size;
        _label.fontStyle = FontStyle.Bold;
        _label.color = _color;
        _label.alignment = TextAnchor.MiddleCenter;
        _label.raycastTarget = false;
        return _label;
    }

    static Button NewButton(string _name, Transform _parent, string _text, Color _color, Color _textColor, int _size)
    {
        var _image = NewImage(_name, _parent, _color);
        var _button = _image.gameObject.AddComponent<Button>();
        _button.targetGraphic = _image;
        var _label = NewText("Text", _image.transform, _text, _size, _textColor);
        Stretch(_label.rectTransform);
        return _button;
    }

    static void Stretch(RectTransform _rect, float _inset = 0f)
    {
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.offsetMin = new Vector2(_inset, _inset);
        _rect.offsetMax = new Vector2(-_inset, -_inset);
    }

    static void Place(RectTransform _rect, Vector2 _anchor, Vector2 _position, Vector2 _size)
    {
        _rect.anchorMin = _anchor;
        _rect.anchorMax = _anchor;
        _rect.pivot = new Vector2(0.5f, _anchor.y);
        _rect.anchoredPosition = _position;
        _rect.sizeDelta = _size;
    }
    #endregion
}
