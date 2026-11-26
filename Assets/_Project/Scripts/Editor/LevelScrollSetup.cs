#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puts the cards of the level select panel (main menu scene) in a horizontal scroll view, so more levels than fit on
/// the screen can be reached by dragging. The "Card Container" becomes the content of a new "Scroll View" object.
/// Does nothing when the scroll view already exists, so edits made by hand are never lost.
/// </summary>
public static class LevelScrollSetup
{
    const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    const string ViewName = "Scroll View";

    [MenuItem("Banker Loot/Setup Level Select Scroll")]
    public static void Setup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != MenuScenePath)
        {
            scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);
        }
        var screen = Object.FindFirstObjectByType<LevelSelectScreen>(FindObjectsInactive.Include);
        if (screen == null || screen.cardContainer == null)
        {
            Debug.LogError("Level scroll setup: run 'Setup Level Select Panel' first.");
            return;
        }
        if (screen.transform.Find(ViewName) != null)
        {
            Debug.Log("Level scroll setup: '" + ViewName + "' already exists, nothing changed.");
            return;
        }

        var content = screen.cardContainer;
        int index = content.GetSiblingIndex();

        var view = new GameObject(ViewName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        view.transform.SetParent(screen.transform, false);
        view.transform.SetSiblingIndex(index);
        var viewRect = view.GetComponent<RectTransform>();
        viewRect.anchorMin = new Vector2(0f, 0.5f);
        viewRect.anchorMax = new Vector2(1f, 0.5f);
        viewRect.pivot = new Vector2(0.5f, 0.5f);
        viewRect.anchoredPosition = content.anchoredPosition;
        viewRect.sizeDelta = new Vector2(0f, content.sizeDelta.y + 40f);

        // invisible, but it takes the drag on the gaps between the cards
        var image = view.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);

        content.SetParent(viewRect, false);
        content.anchorMin = new Vector2(0f, 0.5f);
        content.anchorMax = new Vector2(0f, 0.5f);
        content.pivot = new Vector2(0f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        var layout = content.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(60, 60, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        // the width is set when the screen opens (see LevelSelectScreen), this is the width of the cards at the start
        content.sizeDelta = new Vector2(layout.padding.left + layout.padding.right + screen.slotCount * 372f + (screen.slotCount - 1) * layout.spacing, content.sizeDelta.y);

        var scroll = view.GetComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewRect;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.1f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 30f;
        screen.scrollRect = scroll;

        EditorUtility.SetDirty(screen);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Level scroll setup done: the cards are in '" + ViewName + "'.");
    }
}
#endif
