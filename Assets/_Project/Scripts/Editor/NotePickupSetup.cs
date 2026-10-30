#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time setup for the note pickup system: adds the currency button to the gameplay HUD prefab (a copy of the
/// interact button, placed to its left) and the NotePickupManager to the open scene. Safe to run again.
/// </summary>
public static class NotePickupSetup
{
    const string HudPrefabPath = "Assets/_Project/Prefabs/UI/Gameplay HUD.prefab";
    const string ButtonName = "Currency Button";
    const string ManagerName = "Note Pickup Manager";

    [MenuItem("Banker Loot/Setup Note Pickup")]
    public static void Setup()
    {
        SetupHud();
        SetupManager();
    }

    static void SetupHud()
    {
        var hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            var controller = hud.GetComponent<UIController>();
            if (controller == null || controller.pickButton == null)
            {
                Debug.LogWarning("[BankerLoot] HUD prefab has no interact button, currency button skipped.");
                return;
            }

            var pick = (RectTransform)controller.pickButton.transform;
            if (pick.parent.Find(ButtonName) != null)
                return;

            var clone = Object.Instantiate(pick.gameObject, pick.parent);
            clone.name = ButtonName;
            clone.SetActive(true);

            // the currency button is driven by pointer down / up (tap and hold), not by a click
            var oldButton = clone.GetComponent<Button>();
            if (oldButton != null)
            {
                Object.DestroyImmediate(oldButton);
            }
            var rect = (RectTransform)clone.transform;
            rect.anchoredPosition = pick.anchoredPosition + new Vector2(-(pick.rect.width + 20f), 0f);
            clone.GetComponent<Image>().raycastTarget = true;

            var icon = clone.transform.GetChild(0).GetComponent<Image>();
            icon.raycastTarget = false;

            var labelGo = new GameObject("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(clone.transform, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 5f);
            labelRect.sizeDelta = new Vector2(200f, 50f);
            var label = labelGo.GetComponent<Text>();
            label.font = controller.XPText != null ? controller.XPText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = "0/0";
            labelGo.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;

            var currencyButton = clone.AddComponent<CurrencyInteractButton>();
            currencyButton.icon = icon;
            currencyButton.pickSprite = icon.sprite;
            currencyButton.throwSprite = icon.sprite;
            currencyButton.countText = label;

            PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
            Debug.Log("[BankerLoot] Currency button added to the gameplay HUD.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
    }

    static void SetupManager()
    {
        if (Object.FindFirstObjectByType<NotePickupManager>() != null)
            return;
        var go = new GameObject(ManagerName);
        go.AddComponent<NotePickupManager>();
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[BankerLoot] Added " + ManagerName + " to the scene.");
    }
}
#endif
