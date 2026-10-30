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
        if (controlsPrefab == null || tutorialPrefab == null)
        {
            Debug.LogWarning("[BankerLoot] Player/Tutorial prefabs are not imported yet, scene setup skipped.");
            return;
        }
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

        // --- tutorial overlay (starts with the movement / look-around tutorial)
        PrefabUtility.InstantiatePrefab(tutorialPrefab);

        // --- game controller
        var controllerGo = new GameObject("GameController");
        var controller = controllerGo.AddComponent<GameController>();
        if (follow != null)
        {
            controller.gameMainCamera = follow.Camera;
        }

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
