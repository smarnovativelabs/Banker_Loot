using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Everything that differs from level to level. For now this is the note heap: which note types exist and how
/// many of each the heap holds. Create one per level (Create > Banker Loot > Level Data) and add it to the
/// NoteHeapManager's level list.
/// </summary>
[CreateAssetMenu(fileName = "Level_1", menuName = "Banker Loot/Level Data")]
public class LevelData : ScriptableObject
{
    [Serializable]
    public class NoteTypeEntry
    {
        [Tooltip("Unique id of the note type, the same as the noteId on the prefab (e.g. 1-Dollar)")]
        public string noteId;
        public PickableNote prefab;
        [Tooltip("Notes of this type in the heap when the level starts for the first time (no save yet)")]
        [Min(0)] public int spawnCount = 30;
        [Tooltip("The heap never spawns more notes of this type than this, also when the save holds more")]
        [Min(0)] public int maxCount = 100;
    }

    [Tooltip("The level number this data is used for (GameData.currentLevel)")]
    [Min(1)] public int level = 1;
    [Tooltip("Name shown on the level box in the gameplay HUD (empty shows 'Level N')")]
    public string displayName;
    [Tooltip("Cash paid every time the level is completed (shown on the Level Completed panel)")]
    [Min(0)] public int rewardCash = 100;
#if UNITY_EDITOR
    [Tooltip("The scene that is loaded for this level. It must be in the Build Settings.")]
    [SerializeField] UnityEditor.SceneAsset scene;
#endif
    // the editor-only SceneAsset above is stored by path so the built game can load the scene
    [SerializeField, HideInInspector] string scenePath;

    /// <summary>Name of the scene to load for this level, empty when no scene is assigned.</summary>
    public string SceneName => string.IsNullOrEmpty(scenePath) ? "" : System.IO.Path.GetFileNameWithoutExtension(scenePath);

#if UNITY_EDITOR
    void OnValidate()
    {
        scenePath = scene != null ? UnityEditor.AssetDatabase.GetAssetPath(scene) : "";
    }
#endif
    [Tooltip("The point the heap is built at. It is spawned at runtime at the position and rotation it has in the prefab, so each level can put its heap somewhere else.")]
    public GameObject heapPointPrefab;
    [Tooltip("Where the player starts the level. It is spawned at runtime and the player is moved to its position and rotation in the prefab.")]
    public GameObject playerSpawnPointPrefab;
    public List<NoteTypeEntry> noteTypes = new List<NoteTypeEntry>();
}
