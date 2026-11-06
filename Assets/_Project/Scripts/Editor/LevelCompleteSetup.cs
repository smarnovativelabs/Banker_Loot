using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time setup of the "Level Completed" panel: builds it under the Gameplay HUD prefab and assigns it to the
/// UIController. Does nothing when the panel already exists, so edits made by hand are never lost.
/// </summary>
public static class LevelCompleteSetup
{
    const string HudPath = "Assets/_Project/Prefabs/UI/Gameplay HUD.prefab";
    const string SpriteFolder = "Assets/_Project/Art/UI/Sprites UI/Level Complete/";
    const string PanelName = "Level Complete Panel";

    static readonly Color DarkBlue = new Color(0.04f, 0.14f, 0.38f);

    [MenuItem("Banker Loot/Setup Level Complete Panel")]
    public static void Setup()
    {
        string[] _files = { "Layer 2", "Layer 3", "Layer 4", "Layer 7", "Layer 8", "Layer 9", "Reward Bar" };
        foreach (var _file in _files)
        {
            MakeSprite(SpriteFolder + _file + ".png");
        }
        string _cashPath = FindCashSprite();
        if (_cashPath == null)
        {
            Debug.LogError("Level complete setup: the cash sprite was not found in " + SpriteFolder);
            return;
        }
        MakeSprite(_cashPath);

        var _root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            if (_root.transform.Find(PanelName) != null)
            {
                Debug.Log("Level complete setup: '" + PanelName + "' already exists, nothing changed.");
                return;
            }
            var _ui = _root.GetComponentInChildren<UIController>(true);
            if (_ui == null)
            {
                Debug.LogError("Level complete setup: no UIController in " + HudPath);
                return;
            }
            var _panel = Build(_root.transform, _cashPath);
            _ui.levelCompletePanel = _panel;
            PrefabUtility.SaveAsPrefabAsset(_root, HudPath);
            Debug.Log("Level complete setup done: '" + PanelName + "' is under the Gameplay HUD.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(_root);
        }
    }

    static string FindCashSprite()
    {
        // the cash sprite is the 2nd of the ChatGPT exports (star, cash, bolt, frame, bar, home, replay, play)
        var _guids = AssetDatabase.FindAssets("ChatGPT t:Texture2D", new[] { SpriteFolder.TrimEnd('/') });
        var _paths = new System.Collections.Generic.List<string>();
        foreach (var _guid in _guids)
            _paths.Add(AssetDatabase.GUIDToAssetPath(_guid));
        _paths.Sort((a, b) => ExportNumber(a).CompareTo(ExportNumber(b)));
        return _paths.Count >= 2 ? _paths[1] : null;
    }

    static int ExportNumber(string _path)
    {
        // "ChatGPT Image ..., 02_59_58 PM-4.png" -> 4
        string _name = System.IO.Path.GetFileNameWithoutExtension(_path);
        int _dash = _name.LastIndexOf('-');
        int _n;
        return _dash >= 0 && int.TryParse(_name.Substring(_dash + 1), out _n) ? _n : 0;
    }

    static Sprite MakeSprite(string _path)
    {
        var _importer = AssetImporter.GetAtPath(_path) as TextureImporter;
        if (_importer != null && (_importer.textureType != TextureImporterType.Sprite || !_importer.alphaIsTransparency))
        {
            _importer.textureType = TextureImporterType.Sprite;
            _importer.spriteImportMode = SpriteImportMode.Single;
            _importer.alphaIsTransparency = true;
            _importer.mipmapEnabled = false;
            _importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(_path);
    }

    static Sprite Load(string _name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + _name + ".png");
    }

    static LevelCompletePanel Build(Transform _parent, string _cashPath)
    {
        var _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/UI/Fonts/BarlowCondensed-Black.ttf");

        // full screen dim, blocks the game behind
        var _rootImage = NewImage(PanelName, _parent, null, new Color(0f, 0f, 0f, 0.65f));
        Stretch(_rootImage.rectTransform);
        _rootImage.transform.SetAsLastSibling();
        var _panel = _rootImage.gameObject.AddComponent<LevelCompletePanel>();

        var _window = NewImage("Window", _rootImage.transform, null, Color.clear);
        _window.raycastTarget = false;
        Place(_window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 640f));
        _panel.window = _window.rectTransform;

        var _body = NewImage("Body", _window.transform, Load("Layer 2"), Color.white);
        Place(_body.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(620f, 560f));

        var _banner = NewImage("Banner", _window.transform, Load("Layer 4"), Color.white);
        Place(_banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(480f, 307f));

        var _title = NewText("Title", _banner.transform, "LEVEL COMPLETED!", 44, Color.white, _font);
        Place(_title.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(440f, 70f));
        var _outline = _title.gameObject.AddComponent<UnityEngine.UI.Outline>();
        _outline.effectColor = DarkBlue;
        _outline.effectDistance = new Vector2(2f, -2f);

        var _stars = NewImage("Stars", _window.transform, Load("Layer 3"), Color.white);
        Place(_stars.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(300f, 103f));

        // reward row (only shown when the reward was paid)
        var _row = new GameObject("Reward Row", typeof(RectTransform));
        _row.transform.SetParent(_window.transform, false);
        Place((RectTransform)_row.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -395f), new Vector2(420f, 120f));
        _panel.rewardRow = _row;

        var _label = NewText("Label", _row.transform, "REWARDS", 30, DarkBlue, _font);
        Place(_label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(300f, 40f));

        var _bar = NewImage("Reward Bar", _row.transform, Load("Reward Bar"), Color.white);
        Place(_bar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300f, 72f));

        var _cash = NewImage("Cash Icon", _bar.transform, AssetDatabase.LoadAssetAtPath<Sprite>(_cashPath), Color.white);
        _cash.preserveAspect = true;
        Place(_cash.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(70f, 56f));

        _panel.rewardText = NewText("Reward Text", _bar.transform, "+1,000", 38, Color.white, _font);
        Place(_panel.rewardText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 2f), new Vector2(190f, 56f));

        // buttons: home, replay, next (hidden when there is no next level, the others stay centred)
        var _buttons = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        _buttons.transform.SetParent(_window.transform, false);
        Place((RectTransform)_buttons.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(540f, 96f));
        var _layout = _buttons.GetComponent<HorizontalLayoutGroup>();
        _layout.spacing = 16f;
        _layout.childAlignment = TextAnchor.MiddleCenter;
        _layout.childControlWidth = false;
        _layout.childControlHeight = false;
        _layout.childForceExpandWidth = false;
        _layout.childForceExpandHeight = false;

        _panel.homeButton = NewButton("Home Button", _buttons.transform, Load("Layer 9"), new Vector2(104f, 88f));
        _panel.replayButton = NewButton("Replay Button", _buttons.transform, Load("Layer 8"), new Vector2(104f, 88f));
        _panel.nextButton = NewButton("Next Button", _buttons.transform, Load("Layer 7"), new Vector2(230f, 92f));

        _rootImage.gameObject.SetActive(false);
        return _panel;
    }

    #region UI helpers
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

    static Button NewButton(string _name, Transform _parent, Sprite _sprite, Vector2 _size)
    {
        var _image = NewImage(_name, _parent, _sprite, Color.white);
        _image.rectTransform.sizeDelta = _size;
        var _button = _image.gameObject.AddComponent<Button>();
        _button.targetGraphic = _image;
        return _button;
    }

    static void Stretch(RectTransform _rect)
    {
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform _rect, Vector2 _anchor, Vector2 _pivot, Vector2 _position, Vector2 _size)
    {
        _rect.anchorMin = _anchor;
        _rect.anchorMax = _anchor;
        _rect.pivot = _pivot;
        _rect.anchoredPosition = _position;
        _rect.sizeDelta = _size;
    }
    #endregion
}
