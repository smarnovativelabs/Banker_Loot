using UnityEngine;

/// <summary>
/// The picking capacity upgrade (how many notes the player can carry). It is bought with game cash: the first upgrade costs
/// 100 and every next one costs triple the last price. Each upgrade adds 5 notes to the hand, up to 50. The number of upgrades is
/// kept in the PlayerData, so it is saved with the rest of the player's progress.
/// </summary>
public static class PickingUpgrade
{
    public const int BaseCapacity = 10;
    public const int CapacityStep = 5;
    public const int BasePrice = 100;
    public const int MaxCapacity = 50;
    public const int PriceMultiplier = 3;
    const int MaxLevel = (MaxCapacity - BaseCapacity) / CapacityStep;   // 8 upgrades: 10 -> 50

    public static int Level
    {
        get
        {
            var _data = PlayerDataManager.instance != null ? PlayerDataManager.instance.playerData : null;
            return _data != null ? Mathf.Clamp(_data.pickingUpgrades, 0, MaxLevel) : 0;
        }
    }

    public static int CapacityAt(int _level) { return BaseCapacity + CapacityStep * _level; }
    public static int PriceAt(int _level)
    {
        int _price = BasePrice;
        for (int i = 0; i < Mathf.Clamp(_level, 0, MaxLevel); i++)
        {
            _price *= PriceMultiplier;
        }
        return _price;
    }

    public static int Capacity => CapacityAt(Level);
    public static int NextCapacity => CapacityAt(Level + 1);
    /// <summary>Set by the tutorial while it makes the player buy the first upgrade: that purchase is free.</summary>
    public static bool TutorialFree;
    public static int NextPrice => TutorialFree ? 0 : PriceAt(Level);
    public static bool IsMaxed => Level >= MaxLevel;

    public static bool CanAfford()
    {
        var _data = PlayerDataManager.instance != null ? PlayerDataManager.instance.playerData : null;
        return _data != null && _data.playerCash >= NextPrice;
    }

    /// <summary>Pays the price, saves and gives the player the new capacity. False when it cannot be bought.</summary>
    public static bool TryBuy()
    {
        var _manager = PlayerDataManager.instance;
        if (_manager == null || _manager.playerData == null || IsMaxed || !CanAfford())
            return false;
        int _price = NextPrice;
        _manager.UpdateCash(-_price);
        _manager.playerData.pickingUpgrades = Level + 1;
        _manager.SavePlayerData();
        ApplyToPlayer();
        if (_price > 0 && UIController.instance != null)
        {
            UIController.instance.UpdateCurrency(-_price);
        }
        return true;
    }

    /// <summary>Gives the picking manager of the scene the saved capacity.</summary>
    public static void ApplyToPlayer()
    {
        if (NotePickupManager.instance != null)
        {
            NotePickupManager.instance.SetMaxNotes(Capacity);
        }
    }
}
