#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds the "Complete Level" button to the gameplay HUD in the Game Core prefab (every level scene spawns it, so the
/// button is in all levels). Does nothing when the button already exists, so edits made by hand are never lost.
/// </summary>
public static class CompleteLevelButtonSetup
{
    const string CorePath = "Assets/_Project/Prefabs/Game Core.prefab";
    const string ButtonName = "Complete Level Button";
    const string FontPath = "Assets/_Project/Art/UI/Fonts/BarlowCondensed-Black.ttf";

    [MenuItem("Banker Loot/Setup Complete Level Button")]
    public static void Setup()
    {
        var root = PrefabUtility.LoadPrefabContents(CorePath);
        try
        {
            var hud = root.GetComponentInChildren<UIController>(true);
            if (hud == null)
            {
                Debug.LogError("Complete level button: no UIController in " + CorePath);
                return;
            }
            // the buttons of the top left corner (pause, shop) are children of the safe area
            var pause = hud.pauseButtonParent();
            if (pause.Find(ButtonName) != null)
            {
                Debug.Log("Complete level button: '" + ButtonName + "' already exists, nothing changed.");
                return;
            }

            var button = new GameObject(ButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CompleteLevelButton));
            button.transform.SetParent(pause, false);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(190f, -252f);
            rect.sizeDelta = new Vector2(230f, 64f);
            var image = button.GetComponent<Image>();
            image.color = new Color(0.25f, 0.7f, 0.12f);
            button.GetComponent<Button>().targetGraphic = image;

            var label = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(button.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = label.GetComponent<Text>();
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "COMPLETE LEVEL";
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(root, CorePath);
            Debug.Log("Complete level button added to the gameplay HUD of " + CorePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>The safe area of the HUD (the parent of the pause button).</summary>
    static Transform pauseButtonParent(this UIController _hud)
    {
        var pause = _hud.transform.Find("Safe Area");
        return pause != null ? pause : _hud.transform;
    }
}
#endif
