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
    public List<NoteTypeEntry> noteTypes = new List<NoteTypeEntry>();
}
