#if UNITY_EDITOR
using System.IO;
using KinematicCharacterController.Examples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the Gameplay scene (empty ground + player rig + tutorial + GameController) and registers
/// MainMenu (index 0) and Gameplay (index 1) in the build settings. Runs once automatically when the
/// Gameplay scene does not exist yet; use the menu item to rebuild it.
/// </summary>
public static class BankerLootSceneSetup
{
    const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    const string GameplayScenePath = "Assets/_Project/Scenes/Gameplay.unity";
    const string ControlsPrefabPath = "Assets/_Project/Prefabs/Player/Controls.prefab";
    const string TutorialPrefabPath = "Assets/_Project/Prefabs/Tutorial/Tutorial Manager.prefab";
    const string HudPrefabPath = "Assets/_Project/Prefabs/UI/Gameplay HUD.prefab";
    const string GroundMaterialPath = "Assets/_Project/Materials/Ground.mat";

    [InitializeOnLoadMethod]
    static void AutoRun()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;
            if (!File.Exists(GameplayScenePath))
            {
                BuildGameplayScene();
            }
        };
    }

    [MenuItem("Banker Loot/Build Gameplay Scene")]
    public static void BuildGameplayScene()
    {
        var controlsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ControlsPrefabPath);
        var tutorialPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TutorialPrefabPath);
        var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
        if (controlsPrefab == null || tutorialPrefab == null || hudPrefab == null)
        {
            Debug.LogWarning("[BankerLoot] Player/Tutorial/HUD prefabs are not imported yet, scene setup skipped.");
            return;
        }
        PreparePrefabs();
        if (EditorSceneManager.GetActiveScene().isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- lighting
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // --- empty ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.isStatic = true;
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(100f, 1f, 100f);
        ground.GetComponent<Renderer>().sharedMaterial = GetOrCreateGroundMaterial();

        // --- player rig (KCC character + follow camera + joystick/touchpad canvas)
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(controlsPrefab);
        var controls = rig.GetComponentInChildren<Controlsmanager>(true);
        var follow = rig.GetComponentInChildren<Camerafollow>(true);
        var character = rig.GetComponentInChildren<Charactercontroller>(true);
        if (character != null)
        {
            // put the character on the ground at the origin (character pivot is at its feet)
            var offset = new Vector3(0f, 0.1f, 0f) - character.transform.position;
            rig.transform.position += offset;
        }

        // --- gameplay HUD (cash, blitz, level, calendar, interact button)
        PrefabUtility.InstantiatePrefab(hudPrefab);

        // --- tutorial overlay (starts with the movement / look-around tutorial)
        PrefabUtility.InstantiatePrefab(tutorialPrefab);

        // --- game controller + level progression
        var controllerGo = new GameObject("GameController");
        var controller = controllerGo.AddComponent<GameController>();
        controllerGo.AddComponent<LevelManager>();
        if (follow != null)
        {
            controller.gameMainCamera = follow.Camera;
        }

        // --- test pickup: look at it and press the interact button / tap to get cash and xp
        var pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pickup.name = "Interact Demo Pickup";
        pickup.transform.position = new Vector3(0f, 1.25f, 4f); // tall enough to be at eye level
        pickup.transform.localScale = new Vector3(1f, 2.5f, 1f);
        var interactable = pickup.AddComponent<SimpleInteractable>();
        interactable.hoverText = "Tap To Collect Cash";
        pickup.AddComponent<RewardInteractable>();

        EditorSceneManager.MarkSceneDirty(scene);
        Directory.CreateDirectory(Path.GetDirectoryName(GameplayScenePath));
        EditorSceneManager.SaveScene(scene, GameplayScenePath);
        RegisterBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[BankerLoot] Gameplay scene created and both scenes added to the build settings.");

        if (File.Exists(MenuScenePath))
        {
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        }
    }

    /// <summary>
    /// Adds the interaction components to the player rig (tap area + camera raycaster) and makes the HUD
    /// draw above the controls without blocking the touchpad / joystick.
    /// </summary>
    static void PreparePrefabs()
    {
        var controls = PrefabUtility.LoadPrefabContents(ControlsPrefabPath);
        try
        {
            var touchpad = controls.GetComponentInChildren<TouchControlsKit.TCKTouchpad>(true);
            if (touchpad != null && touchpad.GetComponent<EnvInteract>() == null)
            {
                touchpad.gameObject.AddComponent<EnvInteract>();
            }
            var follow = controls.GetComponentInChildren<Camerafollow>(true);
            if (follow != null && follow.GetComponent<PlayerInteraction>() == null)
            {
                follow.gameObject.AddComponent<PlayerInteraction>();
            }
            PrefabUtility.SaveAsPrefabAsset(controls, ControlsPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(controls);
        }

        var hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            var canvas = hud.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 1;
            }
            foreach (var rect in hud.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == null)
                    continue;
                if (rect.name == "crosshair")
                {
                    // the player rig (Controls prefab) already has a crosshair
                    Object.DestroyImmediate(rect.gameObject);
                    continue;
                }
                if (rect.name == "Safe Area" || rect.name == "FPS Gameplay Panel")
                {
                    var image = rect.GetComponent<UnityEngine.UI.Image>();
                    if (image != null)
                    {
                        image.raycastTarget = false;
                    }
                }
            }
            StyleNotificationPopup(hud);
            var controller = hud.GetComponent<UIController>();
            if (controller != null && controller.uiButtonSound == null)
            {
                foreach (var guid in AssetDatabase.FindAssets("button-sound t:AudioClip", new[] { "Assets/_Project/Audio" }))
                {
                    controller.uiButtonSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                    break;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
    }

    /// <summary>
    /// The popup is used for instructions and tutorial text: dark translucent bar at the top centre
    /// with white text, and it must never block touches while it is visible.
    /// </summary>
    static void StyleNotificationPopup(GameObject hud)
    {
        foreach (var rect in hud.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name != "Notification Popup")
                continue;

            var rootImage = rect.GetComponent<UnityEngine.UI.Image>();
            if (rootImage != null)
            {
                rootImage.raycastTarget = false;
            }
            var container = rect.Find("Container") as RectTransform;
            if (container == null)
                return;

            container.anchorMin = new Vector2(0.5f, 1f);
            container.anchorMax = new Vector2(0.5f, 1f);
            container.pivot = new Vector2(0.5f, 1f);
            container.anchoredPosition = new Vector2(0f, -90f);
            container.sizeDelta = new Vector2(560f, 110f);

            var background = container.GetComponent<UnityEngine.UI.Image>();
            if (background != null)
            {
                background.color = new Color(0.05f, 0.1f, 0.2f, 0.85f);
                background.raycastTarget = false;
            }
            foreach (var text in container.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                text.resizeTextForBestFit = false;
                text.fontSize = 24;
                var textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(12f, 6f);
                textRect.offsetMax = new Vector2(-12f, -6f);
            }
            return;
        }
    }

    static Material GetOrCreateGroundMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
        if (material != null)
            return material;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        material = new Material(shader);
        material.color = new Color(0.36f, 0.42f, 0.36f);
        Directory.CreateDirectory(Path.GetDirectoryName(GroundMaterialPath));
        AssetDatabase.CreateAsset(material, GroundMaterialPath);
        return material;
    }

    static void RegisterBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameplayScenePath, true),
        };
    }
}
#endif
