#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time setup for note placement: the transparent preview material, the placement panel in the gameplay HUD
/// prefab (a copy of the food game's "Place Item Container" with its rotate / place buttons and sprites), the
/// NotePlacementManager in the open scene and the NotePlaceSurface on the object named "Cube". Safe to run again.
/// </summary>
public static class NotePlacementSetup
{
    const string HudPrefabPath = "Assets/_Project/Prefabs/UI/Gameplay HUD.prefab";
    const string GhostMaterialPath = "Assets/_Project/Art/Materials/Note Ghost.mat";
    const string TexturePath = "Assets/_Project/Art/Textures/Notes_Atlas.png";
    const string ManagerName = "Note Placement Manager";
    const string SurfaceObjectName = "Cube";

    // sprites of the food game's placement panel (same sheets and ids as in the food game scene)
    const string PanelSheet = "Assets/_Project/Art/UI/Sprites/Pannel Sprite.png";
    const long PanelSpriteId = -884961832;
    const string RotateSheet = "Assets/_Project/Art/UI/Sprites/SS3 Food Processor.png";
    const long RotateSpriteId = -43256468;
    const long ClockIconId = -471594814;
    const long AntiClockIconId = -1080096492;
    const string PlaceSheet = "Assets/_Project/Art/UI/Sprites/SS1 Food Processor.png";
    const long PlaceSpriteId = -107521382;

    [MenuItem("Banker Loot/Setup Note Placement")]
    public static void Setup()
    {
        var ghostMaterial = GetOrCreateGhostMaterial();
        SetupHud();
        SetupScene(ghostMaterial);
    }

    static Material GetOrCreateGhostMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(GhostMaterialPath);
        if (material != null)
            return material;

        material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Note Ghost" };
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
        material.SetColor("_BaseColor", new Color(0.5f, 1f, 0.5f, 0.6f));
        NotePlacementManager.ConfigureTransparent(material);
        AssetDatabase.CreateAsset(material, GhostMaterialPath);
        return material;
    }

    static Sprite LoadSprite(string sheetPath, long localId)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
        {
            if (asset is Sprite sprite
                && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string _, out long id)
                && id == localId)
                return sprite;
        }
        Debug.LogWarning("[BankerLoot] Sprite " + localId + " not found in " + sheetPath);
        return null;
    }

    static void SetupHud()
    {
        var hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            var controller = hud.GetComponent<UIController>();
            if (controller == null || controller.pickButton == null)
            {
                Debug.LogWarning("[BankerLoot] HUD prefab has no interact button, placement panel skipped.");
                return;
            }
            var parent = controller.pickButton.transform.parent;

            // remove the first version of the placement buttons
            foreach (var oldName in new[] { "Place Button", "Rotate Button", "Make Pile Button" })
            {
                var old = parent.Find(oldName);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }
            }
            // rebuild the panel when the setup runs again
            var existing = parent.Find("Place Item Container");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var font = controller.XPText != null ? controller.XPText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // "Place Item Container": 300x70 panel, bottom centre, 130 above the screen bottom
            var container = new GameObject("Place Item Container", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            container.transform.SetParent(parent, false);
            var containerRect = (RectTransform)container.transform;
            containerRect.anchorMin = new Vector2(0.5f, 0f);
            containerRect.anchorMax = new Vector2(0.5f, 0f);
            containerRect.pivot = new Vector2(0.5f, 0f);
            containerRect.anchoredPosition = new Vector2(0f, 130f);
            containerRect.sizeDelta = new Vector2(300f, 70f);
            var panel = container.GetComponent<Image>();
            panel.sprite = LoadSprite(PanelSheet, PanelSpriteId);
            panel.raycastTarget = false;

            var rotateSprite = LoadSprite(RotateSheet, RotateSpriteId);
            var antiClock = CreateRotateButton(container.transform, "Rotate Anti Clock", rotateSprite,
                LoadSprite(RotateSheet, AntiClockIconId), new Vector2(0f, 0.5f));
            var clock = CreateRotateButton(container.transform, "Rotate Clock", rotateSprite,
                LoadSprite(RotateSheet, ClockIconId), new Vector2(1f, 0.5f));

            // "Place Btn": 160x60 in the middle with the text "Place"
            var placeGo = new GameObject("Place Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            placeGo.transform.SetParent(container.transform, false);
            var placeRect = (RectTransform)placeGo.transform;
            placeRect.anchorMin = placeRect.anchorMax = placeRect.pivot = new Vector2(0.5f, 0.5f);
            placeRect.sizeDelta = new Vector2(160f, 60f);
            var placeImage = placeGo.GetComponent<Image>();
            placeImage.sprite = LoadSprite(PlaceSheet, PlaceSpriteId);

            var labelGo = new GameObject("Text (Legacy)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(placeGo.transform, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.anchoredPosition = new Vector2(0f, 5.655f);
            labelRect.sizeDelta = new Vector2(0f, -11.31f);
            var label = labelGo.GetComponent<Text>();
            label.font = font;
            label.fontSize = 14;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 32;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "Place";
            label.raycastTarget = false;
            var shadowA = labelGo.AddComponent<Shadow>();
            shadowA.effectColor = new Color(0.0054734685f, 0.19861256f, 0.38679248f, 0.5f);
            shadowA.effectDistance = new Vector2(1f, -1f);
            var shadowB = labelGo.AddComponent<Shadow>();
            shadowB.effectColor = new Color(0.003921569f, 0.2f, 0.3882353f, 0.5f);
            shadowB.effectDistance = new Vector2(1f, -2.26f);

            var placementHud = hud.GetComponent<NotePlacementHud>();
            if (placementHud == null)
            {
                placementHud = hud.AddComponent<NotePlacementHud>();
            }
            placementHud.container = container;
            placementHud.rotateAntiClockButton = antiClock;
            placementHud.placeButton = placeGo.GetComponent<Button>();
            placementHud.rotateClockButton = clock;

            PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
            Debug.Log("[BankerLoot] Placement panel added to the gameplay HUD.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
    }

    static Button CreateRotateButton(Transform parent, string name, Sprite baseSprite, Sprite iconSprite, Vector2 anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(60f, 60f);
        go.GetComponent<Image>().sprite = baseSprite;

        // the arrow, stretched over the button with a small inset like in the food game
        var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        icon.transform.SetParent(go.transform, false);
        var iconRect = (RectTransform)icon.transform;
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.anchoredPosition = new Vector2(0f, 0.7f);
        iconRect.sizeDelta = new Vector2(-20f, -18f);
        var iconImage = icon.GetComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.raycastTarget = false;
        return go.GetComponent<Button>();
    }

    static void SetupScene(Material ghostMaterial)
    {
        var manager = Object.FindFirstObjectByType<NotePlacementManager>();
        if (manager == null)
        {
            manager = new GameObject(ManagerName).AddComponent<NotePlacementManager>();
        }
        manager.ghostMaterial = ghostMaterial;
        EditorUtility.SetDirty(manager);

        var cube = GameObject.Find(SurfaceObjectName);
        if (cube != null)
        {
            if (cube.GetComponent<NotePlaceSurface>() == null)
            {
                cube.AddComponent<NotePlaceSurface>();
            }
        }
        else
        {
            Debug.LogWarning("[BankerLoot] No object named '" + SurfaceObjectName + "' in the open scene, add NotePlaceSurface to your table by hand.");
        }
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
}
#endif
