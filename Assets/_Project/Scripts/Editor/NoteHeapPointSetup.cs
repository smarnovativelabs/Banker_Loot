using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-time setup: turns the scene's "Note Heap" object into the "Note Heap Point" prefab (position and rotation
/// kept, the baked notes dropped), assigns it to Level_1 and removes the scene object, the heap point is spawned
/// from the LevelData at runtime now.
/// </summary>
public static class NoteHeapPointSetup
{
    const string PrefabPath = "Assets/_Project/Prefabs/Note Heap Point.prefab";
    const string LevelPath = "Assets/_Project/Data/Level_1/Level_1.asset";

    const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player Spawn Point.prefab";

    /// <summary>Makes the "Player Spawn Point" prefab at the player's current position and rotation and assigns it to Level_1.</summary>
    [MenuItem("Banker Loot/Setup Player Spawn Point Prefab")]
    public static void SetupPlayerPoint()
    {
        var _controls = Object.FindFirstObjectByType<Controlsmanager>(FindObjectsInactive.Include);
        var _level = AssetDatabase.LoadAssetAtPath<LevelData>(LevelPath);
        if (_controls == null || _controls.kinematicCharacterMotor == null || _level == null)
        {
            Debug.LogError("Open the Gameplay scene first (needs the Controlsmanager and Level_1).");
            return;
        }

        var _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (_prefab == null)
        {
            System.IO.Directory.CreateDirectory("Assets/_Project/Prefabs");
            var _player = _controls.kinematicCharacterMotor.transform;
            var _point = new GameObject("Player Spawn Point");
            _point.transform.SetPositionAndRotation(_player.position, Quaternion.Euler(0f, _player.eulerAngles.y, 0f));
            _prefab = PrefabUtility.SaveAsPrefabAsset(_point, PlayerPrefabPath);
            Object.DestroyImmediate(_point);
        }
        _level.playerSpawnPointPrefab = _prefab;
        EditorUtility.SetDirty(_level);
        AssetDatabase.SaveAssets();
        Debug.Log("Player Spawn Point prefab ready and assigned to " + _level.name);
    }

    [MenuItem("Banker Loot/Setup Note Heap Point Prefab")]
    public static void Setup()
    {
        var _manager = Object.FindFirstObjectByType<NoteHeapManager>(FindObjectsInactive.Include);
        if (_manager == null)
        {
            Debug.LogError("Open the Gameplay scene first, there is no NoteHeapManager in the open scene.");
            return;
        }

        var _level = AssetDatabase.LoadAssetAtPath<LevelData>(LevelPath);
        if (_level == null)
        {
            Debug.LogError("Level_1 not found at " + LevelPath);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            if (_manager.heapRoot == null)
            {
                Debug.LogError("The NoteHeapManager has no heap root to make the prefab from.");
                return;
            }
            var _root = _manager.heapRoot.gameObject;
            System.IO.Directory.CreateDirectory("Assets/_Project/Prefabs");

            var _copy = new GameObject("Note Heap Point");
            _copy.transform.SetPositionAndRotation(_root.transform.position, _root.transform.rotation);
            _copy.transform.localScale = _root.transform.lossyScale;
            var _prefab = PrefabUtility.SaveAsPrefabAsset(_copy, PrefabPath);
            Object.DestroyImmediate(_copy);
            _level.heapPointPrefab = _prefab;

            // the heap is spawned at runtime now, the baked scene heap goes away
            _manager.heapRoot = null;
            Object.DestroyImmediate(_root);
            EditorUtility.SetDirty(_manager);
            EditorSceneManager.MarkSceneDirty(_manager.gameObject.scene);
        }
        else if (_level.heapPointPrefab == null)
        {
            _level.heapPointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        EditorUtility.SetDirty(_level);
        AssetDatabase.SaveAssets();
        Debug.Log("Note Heap Point prefab ready and assigned to " + _level.name + ", save the scene.");
    }
}
