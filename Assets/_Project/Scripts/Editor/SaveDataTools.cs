#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Editor helpers for the saved game data (it lives in Application.persistentDataPath + SerializationManager.projectName).</summary>
public static class SaveDataTools
{
    static string SaveFolder => Application.persistentDataPath + SerializationManager.projectName;

    [MenuItem("Banker Loot/Save Data/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        string folder = SaveFolder;
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        EditorUtility.RevealInFinder(folder);
        Debug.Log("[BankerLoot] Save folder: " + folder);
    }

    /// <summary>
    /// Deletes the saved notes (heap counts, placed notes) and the saved lockers, so the next start spawns the
    /// notes of the level data again and opens every locker. The other save files are kept.
    /// </summary>
    [MenuItem("Banker Loot/Save Data/Reset Notes And Lockers")]
    public static void ResetNotesAndLockers()
    {
        if (!EditorUtility.DisplayDialog("Reset notes and lockers",
                "Delete the saved notes (NoteHeapData) and the saved lockers (LockerData)?\n\nThe next start spawns the notes of the level data and opens all lockers. Player, tutorial and the other saves are kept.\n\n" + SaveFolder,
                "Delete", "Cancel"))
            return;

        foreach (var _suffix in new[] { "", "_Level2" })
        {
            SerializationManager.DeleteFile("NoteHeapData" + _suffix);
            SerializationManager.DeleteFile("LockerData" + _suffix);
        }
        Debug.Log("[BankerLoot] NoteHeapData and LockerData deleted.");
    }
}
#endif
