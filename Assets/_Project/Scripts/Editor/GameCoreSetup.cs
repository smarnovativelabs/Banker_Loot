using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-time setup: moves the core objects of the open gameplay scene into the "Game Core" prefab and puts a
/// GameCoreSpawner in the scene instead, so the core is spawned at runtime.
/// </summary>
public static class GameCoreSetup
{
    const string PrefabPath = "Assets/_Project/Prefabs/Game Core.prefab";

    static readonly string[] CoreRoots =
    {
        "Controls", "Gameplay HUD", "Tutorial Manager", "GameController",
        "Note Pickup Manager", "Note Placement Manager", "Note Heap Manager"
    };

    [MenuItem("Banker Loot/Setup Game Core Prefab")]
    public static void Setup()
    {
        var _scene = EditorSceneManager.GetActiveScene();
        if (Object.FindFirstObjectByType<GameCoreSpawner>(FindObjectsInactive.Include) != null)
        {
            Debug.LogWarning("This scene already has a GameCoreSpawner.");
            return;
        }

        var _found = new List<GameObject>();
        foreach (var _root in _scene.GetRootGameObjects())
        {
            if (System.Array.IndexOf(CoreRoots, _root.name) >= 0)
            {
                _found.Add(_root);
            }
        }
        if (_found.Count != CoreRoots.Length)
        {
            Debug.LogError("Open the Gameplay scene: found " + _found.Count + " of " + CoreRoots.Length + " core objects.");
            return;
        }

        System.IO.Directory.CreateDirectory("Assets/_Project/Prefabs");
        var _core = new GameObject("Game Core");
        foreach (var _object in _found)
        {
            _object.transform.SetParent(_core.transform, true);
        }
        var _prefab = PrefabUtility.SaveAsPrefabAsset(_core, PrefabPath);
        Object.DestroyImmediate(_core);

        var _spawner = new GameObject("Game Core Spawner").AddComponent<GameCoreSpawner>();
        _spawner.corePrefab = _prefab;
        EditorSceneManager.MarkSceneDirty(_scene);
        Debug.Log("Game Core prefab created at " + PrefabPath + ", save the scene.");
    }
}
