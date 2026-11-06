using System;
using UnityEngine;

/// <summary>What the player unlocked so far. Saved in its own file, so a completed level stays unlocked for good.</summary>
[Serializable]
public class LevelProgressData
{
    /// <summary>The highest level the player can play. A new game starts with level 1.</summary>
    public int unlockedLevel = 1;
}

/// <summary>
/// Level unlocking. Level 1 is unlocked from the start. Completing a level unlocks the next one permanently.
/// The level select screen reads it, the level scene reports a completed level to it.
/// </summary>
public static class LevelProgress
{
    const string SaveFileName = "LevelProgress";
    const string GameDataFileName = "_gameData";

    /// <summary>The highest level the player can play (at least 1).</summary>
    public static int UnlockedLevel
    {
        get
        {
            var _data = SerializationManager.LoadFile(SaveFileName) as LevelProgressData;
            return _data != null ? Mathf.Max(1, _data.unlockedLevel) : 1;
        }
    }

    public static bool IsUnlocked(int _level)
    {
        return _level <= UnlockedLevel;
    }

    /// <summary>A level is completed once the next one is unlocked.</summary>
    public static bool IsCompleted(int _level)
    {
        return UnlockedLevel > _level;
    }

    /// <summary>Call when the player completes a level: the next level is unlocked (never locked again).</summary>
    public static void CompleteLevel(int _level)
    {
        if (_level + 1 <= UnlockedLevel)
            return;
        SerializationManager.Save(new LevelProgressData { unlockedLevel = _level + 1 }, SaveFileName);
    }

    /// <summary>Makes the given level the one the next game start loads (GameData.currentLevel).</summary>
    public static void SelectLevel(int _level)
    {
        var _game = SerializationManager.LoadFile(GameDataFileName) as GameData;
        if (_game == null)
        {
            _game = new GameData();
        }
        _game.currentLevel = _level;
        SerializationManager.Save(_game, GameDataFileName);
    }

    // save file base names of the level state (see NoteHeapManager and LockerManager)
    static readonly string[] LevelStateFiles = { "NoteHeapData", "LockerData" };

    /// <summary>True when the level has a saved state (notes / lockers), so it can be continued.</summary>
    public static bool HasProgress(int _level)
    {
        for (int i = 0; i < LevelStateFiles.Length; i++)
        {
            if (SerializationManager.LoadFile(NoteHeapManager.SaveNameFor(LevelStateFiles[i], _level)) != null)
                return true;
        }
        return false;
    }

    /// <summary>Deletes the saved state of the level, it starts from the beginning the next time.</summary>
    public static void ResetLevel(int _level)
    {
        for (int i = 0; i < LevelStateFiles.Length; i++)
        {
            SerializationManager.DeleteFile(NoteHeapManager.SaveNameFor(LevelStateFiles[i], _level));
        }
    }

    /// <summary>Resets the unlock progress (only level 1 is unlocked again).</summary>
    public static void ResetSave()
    {
        SerializationManager.Save(new LevelProgressData(), SaveFileName);
    }
}
