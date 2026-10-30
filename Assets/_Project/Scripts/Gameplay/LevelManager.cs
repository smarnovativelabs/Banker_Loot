using System;
using UnityEngine;

/// <summary>
/// XP / level progression (same formula as the food game). Add XP through AddXP, the HUD and
/// player data are updated, and LevelledUp is raised for every new level.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    /// <summary>Raised with the new level after the player levelled up.</summary>
    public event Action<int> LevelledUp;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Adds XP to the player, plays the HUD animation and checks for a level up.</summary>
    public void AddXP(int _amount)
    {
        PlayerDataManager.instance.UpdateXP(_amount);
        if (UIController.instance != null)
        {
            UIController.instance.UpdateXP(_amount);
        }
        else
        {
            CheckForLevelUp();
        }
    }

    /// <summary>Checks if the player qualifies for a level-up based on current XP.</summary>
    public void CheckForLevelUp()
    {
        while (PlayerDataManager.instance.playerData.playerXP >= GetRequiredXP())
        {
            int _playerCurrentLevel = PlayerDataManager.instance.playerData.playerLevel;
            PlayerDataManager.instance.UpdateLevel(1);
            GameManager.instance.CallFireBase("Level_" + _playerCurrentLevel.ToString() + "_Up");
            LevelledUp?.Invoke(PlayerDataManager.instance.playerData.playerLevel);
        }

        if (UIController.instance != null)
        {
            UIController.instance.UpdateLevelAndXPBar();
        }
    }

    public int GetRequiredXP()
    {
        return GetLevelRequiredXPs(Mathf.Max(1, PlayerDataManager.instance.playerData.playerLevel));
    }

    public int GetLevelRequiredXPs(int _level)
    {
        if (_level < 1)
        {
            return 0;
        }
        return (100 * _level) * (_level + 3);
    }

    public string GetCurrentAndNextLevelRequiredXPS()
    {
        return PlayerDataManager.instance.playerData.playerXP + "/" + GetRequiredXP();
    }

    public float GetLevelBarFillValue()
    {
        int _prevLevelXps = GetLevelRequiredXPs(PlayerDataManager.instance.playerData.playerLevel - 1);
        float _range = GetRequiredXP() - _prevLevelXps;
        if (_range <= 0)
        {
            return 0f;
        }
        return Mathf.Clamp01((PlayerDataManager.instance.playerData.playerXP - _prevLevelXps) / _range);
    }
}
