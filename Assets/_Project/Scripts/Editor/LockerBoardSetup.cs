#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puts the locker texts on the "Vault Room Lockers" board of the bank room: one cell per locker, laid out like the
/// mock (locker picture top left, locker name in yellow, the amount it still needs in white). Everything is a child of
/// the "RequireAmount" canvas. The pictures are empty white images until the locker sprite is assigned. Safe to run again.
/// </summary>
public static class LockerBoardSetup
{
    const string RootName = "RequireAmount";
    const string FontPath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Anton SDF.asset";

    // The board seen straight on from the front: 1000 x 560 pixels = 5 x 2.8 meters, centred on (-4.6, 1.2) of the wall.
    // The cell positions are the pixels of the five columns and the three rows of the blue cells.
    const float BoardCenterX = -4.6f;
    const float BoardCenterY = 1.2f;
    // the face of the board is at about z 4.51: the canvas must be in front of it or the board hides the texts
    const float BoardZ = 4.49f;
    const float MetersPerPixel = 0.005f;
    const int Columns = 5;
    static readonly float[] ColumnCenters = { 182f, 351.5f, 521.5f, 691.5f, 861.5f };
    static readonly float[] RowCenters = { 192.5f, 310.5f, 428f };
    static readonly Vector2 CellSize = new Vector2(153f, 107f);

    static readonly Color NameColor = new Color(1f, 0.78f, 0.05f);
    static readonly Color AmountColor = new Color(0.92f, 0.92f, 0.92f);

    [MenuItem("Banker Loot/Setup Locker Board")]
    public static void Setup()
    {
        Build(new Vector3(BoardCenterX, BoardCenterY, BoardZ), Quaternion.identity, ColumnCenters, RowCenters);
    }

    /// <summary>The casino board on the +X wall (faces -X): 7 columns x 3 rows = 21 cells.</summary>
    [MenuItem("Banker Loot/Setup Locker Board (Casino 21)")]
    public static void SetupCasino()
    {
        var columns = new float[7];
        for (int i = 0; i < columns.Length; i++)
        {
            columns[i] = 500f + (i - 3) * 160f;
        }
        Build(new Vector3(3.40f, 1.2f, 0.75f), Quaternion.Euler(0f, 90f, 0f), columns, RowCenters);
    }

    static void Build(Vector3 _boardPosition, Quaternion _boardRotation, float[] _columnCenters, float[] _rowCenters)
    {
        var managerObject = GameObject.Find("Locker_Manager");
        var manager = managerObject != null ? managerObject.GetComponent<LockerManager>() : null;
        if (manager == null)
        {
            Debug.LogWarning("[BankerLoot] Locker_Manager with a LockerManager is missing, run 'Setup Lockers' first.");
            return;
        }

        // the floating labels are replaced by the board
        foreach (var entry in manager.lockers)
        {
            var old = managerObject.transform.Find(entry.id + " Label");
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }
        }
        manager.billboardLabels = false;

        var root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, RootName);
        }
        var canvas = root.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = root.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.WorldSpace;
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.position = _boardPosition;
        rootRect.rotation = _boardRotation;
        rootRect.sizeDelta = new Vector2(1000f, 560f);
        rootRect.localScale = Vector3.one * MetersPerPixel;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        for (int i = 0; i < manager.lockers.Count; i++)
        {
            var entry = manager.lockers[i];
            int column = i % _columnCenters.Length;
            int row = i / _columnCenters.Length;
            if (row >= _rowCenters.Length)
            {
                Debug.LogWarning("[BankerLoot] The board only has " + _columnCenters.Length * _rowCenters.Length + " cells, " + entry.id + " got none.");
                continue;
            }

            string display = DisplayName(entry.id);
            var cell = GetOrCreateChild(rootRect, "Locker_" + display);
            cell.anchorMin = cell.anchorMax = cell.pivot = new Vector2(0.5f, 0.5f);
            cell.anchoredPosition = new Vector2(_columnCenters[column] - 500f, 280f - _rowCenters[row]);
            cell.sizeDelta = CellSize;
            cell.localScale = Vector3.one;
            cell.localRotation = Quaternion.identity;
            cell.localPosition = new Vector3(cell.localPosition.x, cell.localPosition.y, 0f);

            // locker picture: an empty white image until the sprite is assigned
            var icon = GetOrCreateChild(cell, "Icon");
            Place(icon, new Vector2(-53.6f, 31f), new Vector2(36f, 36f));
            var image = icon.GetComponent<Image>();
            if (image == null)
            {
                image = icon.gameObject.AddComponent<Image>();
            }
            image.sprite = null;
            image.color = Color.white;
            image.raycastTarget = false;

            var nameText = GetOrCreateText(cell, "Name", new Vector2(6f, 32.6f), new Vector2(92f, 38f), NameColor, font);
            nameText.text = display;
            var amountText = GetOrCreateText(cell, "Amount", new Vector2(4.6f, -17.7f), new Vector2(146f, 42f), AmountColor, font);
            amountText.text = "$" + entry.amount;

            entry.icon = image;
            entry.nameText = nameText;
            entry.label = amountText;
        }

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(managerObject.scene);
        Debug.Log("[BankerLoot] Locker board set up.");
    }

    /// <summary>The names of the lockers use a letter O instead of a zero in places (AO1), the board shows A01.</summary>
    static string DisplayName(string _id)
    {
        if (string.IsNullOrEmpty(_id) || _id.Length < 2)
            return _id;
        return _id.Substring(0, 1) + _id.Substring(1).Replace('O', '0');
    }

    static RectTransform GetOrCreateChild(RectTransform _parent, string _name)
    {
        var existing = _parent.Find(_name);
        if (existing != null)
            return existing as RectTransform;
        var go = new GameObject(_name, typeof(RectTransform));
        go.transform.SetParent(_parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Place(RectTransform _rect, Vector2 _position, Vector2 _size)
    {
        _rect.anchorMin = _rect.anchorMax = _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.anchoredPosition = _position;
        _rect.sizeDelta = _size;
        _rect.localScale = Vector3.one;
        _rect.localRotation = Quaternion.identity;
        _rect.localPosition = new Vector3(_rect.localPosition.x, _rect.localPosition.y, 0f);
    }

    static TMP_Text GetOrCreateText(RectTransform _parent, string _name, Vector2 _position, Vector2 _size, Color _color, TMP_FontAsset _font)
    {
        var rect = GetOrCreateChild(_parent, _name);
        Place(rect, _position, _size);
        var text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        }
        if (_font != null)
        {
            text.font = _font;
        }
        text.fontSize = 40f;
        text.color = _color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
#endif
