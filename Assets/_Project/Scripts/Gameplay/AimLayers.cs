using UnityEngine;

/// <summary>
/// Layers of the lockers that the player's aim treats in a special way:
/// - LockerColliders: block thrown notes (and the player), but the hover and placing rays go straight through, so the
///   player can pick up and place notes inside the lockers.
/// - LockerPlaceZone: a trigger volume that fills the inside of a locker. Only the placing ray looks for it, so a note
///   can be put down wherever the player aims inside the locker, not only on its floor.
/// </summary>
public static class AimLayers
{
    public const string LockerCollidersLayerName = "LockerColliders";
    public const string PlaceZoneLayerName = "LockerPlaceZone";

    static int lockerLayer = -2;
    static int zoneLayer = -2;

    /// <summary>Layer of the locker colliders, -1 when the layer does not exist.</summary>
    public static int LockerLayer
    {
        get
        {
            if (lockerLayer == -2)
            {
                lockerLayer = LayerMask.NameToLayer(LockerCollidersLayerName);
            }
            return lockerLayer;
        }
    }

    /// <summary>Layer of the locker place zones, -1 when the layer does not exist.</summary>
    public static int PlaceZoneLayer
    {
        get
        {
            if (zoneLayer == -2)
            {
                zoneLayer = LayerMask.NameToLayer(PlaceZoneLayerName);
            }
            return zoneLayer;
        }
    }

    /// <summary>Mask with only the place zones.</summary>
    public static int PlaceZoneMask => PlaceZoneLayer >= 0 ? 1 << PlaceZoneLayer : 0;

    /// <summary>The given mask without the locker colliders and the place zones.</summary>
    public static int WithoutLockerColliders(int _mask)
    {
        if (LockerLayer >= 0)
        {
            _mask &= ~(1 << LockerLayer);
        }
        if (PlaceZoneLayer >= 0)
        {
            _mask &= ~(1 << PlaceZoneLayer);
        }
        return _mask;
    }
}
