#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Sets up the note heap saving: gives every dummy note prefab its noteId, creates the level 1 LevelData asset
/// with one entry per note prefab, and adds the NoteHeapManager to the open scene pointing at the "Note Heap"
/// object. Safe to run again, counts already set on the level asset are kept.
/// </summary>
public static class NoteHeapSetup
{
    const string PrefabFolder = NotePrefabSetup.PrefabFolder;
    const string LevelFolder = "Assets/_Project/Data/Levels";
    const string Level1Path = LevelFolder + "/Level_1.asset";
    const string ManagerName = "Note Heap Manager";
    const string HeapName = "Note Heap";
    static readonly string[] NoteIds = { "1-Dollar", "5-Dollar", "10-Dollar", "20-Dollar", "50-Dollar" };

    [MenuItem("Banker Loot/Setup Note Heap Saving")]
    public static void Setup()
    {
        int notesLayer = EnsureLayer(NoteHeapManager.NotesLayerName);
        int playerLayer = EnsureLayer(NoteHeapManager.PlayerLayerName);
        SetupPlayerLayer(playerLayer);

        Directory.CreateDirectory(LevelFolder);
        var level = AssetDatabase.LoadAssetAtPath<LevelData>(Level1Path);
        if (level == null)
        {
            level = ScriptableObject.CreateInstance<LevelData>();
            level.level = 1;
            AssetDatabase.CreateAsset(level, Level1Path);
        }

        foreach (var id in NoteIds)
        {
            string path = PrefabFolder + "/" + id + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var note = prefab != null ? prefab.GetComponent<PickableNote>() : null;
            if (note == null)
            {
                Debug.LogWarning("[BankerLoot] Note prefab missing at " + path + ", run 'Build Note Prefabs' first.");
                continue;
            }

            if (note.noteId != id || prefab.layer != notesLayer)
            {
                note.noteId = id;
                prefab.layer = notesLayer;
                EditorUtility.SetDirty(prefab);
            }

            var entry = level.noteTypes.Find(e => e.noteId == id);
            if (entry == null)
            {
                entry = new LevelData.NoteTypeEntry { noteId = id, spawnCount = 28, maxCount = 100 };
                level.noteTypes.Add(entry);
            }
            entry.prefab = note;
        }
        EditorUtility.SetDirty(level);

        var manager = Object.FindFirstObjectByType<NoteHeapManager>();
        if (manager == null)
        {
            manager = new GameObject(ManagerName).AddComponent<NoteHeapManager>();
        }
        if (!manager.levels.Contains(level))
        {
            manager.levels.Add(level);
        }

        var heap = GameObject.Find(HeapName);
        if (heap == null)
        {
            heap = new GameObject(HeapName);
            heap.transform.position = new Vector3(-1.5f, 0f, 2.5f);
        }
        manager.heapRoot = heap.transform;

        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Debug.Log("[BankerLoot] Note heap manager set up with level data " + Level1Path);
    }

    /// <summary>Adds the layer to the first free user layer slot (6 and up) when it does not exist, returns its index.</summary>
    static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0)
            return existing;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 6; i < layers.arraySize; i++)
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        Debug.LogError("[BankerLoot] No free layer slot for '" + name + "'.");
        return 0;
    }

    /// <summary>Moves the player's character capsule to its own layer so physics can ignore it against the notes.</summary>
    static void SetupPlayerLayer(int playerLayer)
    {
        var controller = Object.FindFirstObjectByType<KinematicCharacterController.Examples.Charactercontroller>();
        if (controller == null)
        {
            Debug.LogWarning("[BankerLoot] No player character in the open scene, open the gameplay scene and run the setup again.");
            return;
        }
        var go = controller.Motor != null ? controller.Motor.gameObject : controller.gameObject;
        if (go.layer != playerLayer)
        {
            go.layer = playerLayer;
            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }
}
#endif
